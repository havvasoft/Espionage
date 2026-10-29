using System;
using Espionage.Core;
using Espionage.LLM;
using Espionage.NPC;
using UnityEngine;

namespace Espionage.UI
{
    public sealed class DialogueController : MonoBehaviour
    {
        private const string UnavailableMessage = "Görevli şu an cevap veremiyor. Biraz sonra tekrar dene.";

        [SerializeField] private DialogueView view;
        [SerializeField] private OllamaSettings settings;

        private NpcDialogue _dialogue;
        private MainframeAccessGate _gate;
        private IPlayerControl _playerControl;
        private IDisposable _inputLock;
        private bool _busy;

        private void Awake()
        {
            _dialogue = new NpcDialogue(settings.CreateClient());
        }

        private async void Start()
        {
            await _dialogue.WarmUpAsync(destroyCancellationToken);
        }

        public void Bind(IObjectiveState objectives, IPlayerControl playerControl)
        {
            _gate = objectives != null ? new MainframeAccessGate(objectives) : null;
            _playerControl = playerControl;
        }

        public void Open()
        {
            view.gameObject.SetActive(true);
            _inputLock ??= _playerControl?.RequestLock("NPC dialogue");
        }

        public void Close()
        {
            ReleaseLock();
            view.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            view.MessageSubmitted += OnMessageSubmitted;
        }

        private void OnDisable()
        {
            view.MessageSubmitted -= OnMessageSubmitted;
        }

        private void OnDestroy()
        {
            ReleaseLock();
        }

        private void ReleaseLock()
        {
            _inputLock?.Dispose();
            _inputLock = null;
        }

        private async void OnMessageSubmitted(string playerText)
        {
            if (_busy)
                return;

            _busy = true;
            view.SetBusy(true);

            var result = await _dialogue.AskAsync(playerText, destroyCancellationToken);

            if (destroyCancellationToken.IsCancellationRequested)
                return;

            _busy = false;
            view.SetBusy(false);

            if (!result.Success)
            {
                Debug.LogWarning($"NPC dialogue failed: {result.Error}");
                view.ShowReply(UnavailableMessage);
                return;
            }

            view.ShowReply(result.Reply.Reply);
            Debug.Log($"NPC decision: {result.Reply.Decision}");
            _gate?.Apply(result);
        }
    }
}