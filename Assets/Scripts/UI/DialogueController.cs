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
        private bool _busy;

        private void Awake()
        {
            _dialogue = new NpcDialogue(settings.CreateClient());
        }

        public void Bind(IObjectiveState objectives)
        {
            _gate = new MainframeAccessGate(objectives);
        }

        private void OnEnable()
        {
            view.MessageSubmitted += OnMessageSubmitted;
        }

        private void OnDisable()
        {
            view.MessageSubmitted -= OnMessageSubmitted;
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