using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Espionage.UI
{
    public sealed class DialogueView : MonoBehaviour
    {
        [SerializeField] private TMP_Text npcReplyText;
        [SerializeField] private TMP_InputField playerInput;
        [SerializeField] private Button sendButton;

        private const string ThinkingMessage = "Görevli düşünüyor";
        private const float DotInterval = 0.4f;

        private bool _busy;
        private float _dotTimer;
        private int _dotCount;

        public event Action<string> MessageSubmitted;

        private void OnEnable()
        {
            sendButton.onClick.AddListener(OnSendClicked);
        }

        private void OnDisable()
        {
            sendButton.onClick.RemoveListener(OnSendClicked);
        }

        public void ShowReply(string text)
        {
            npcReplyText.text = text;
        }

        public void SetBusy(bool busy)
        {
            _busy = busy;
            sendButton.interactable = !busy;
            playerInput.interactable = !busy;

            if (busy)
            {
                _dotTimer = 0f;
                _dotCount = 0;
                npcReplyText.text = ThinkingMessage;
            }
        }

        private void Update()
        {
            if (!_busy)
                return;

            _dotTimer += Time.unscaledDeltaTime;
            if (_dotTimer < DotInterval)
                return;

            _dotTimer = 0f;
            _dotCount = (_dotCount + 1) % 4;
            npcReplyText.text = ThinkingMessage + new string('.', _dotCount);
        }

        private void OnSendClicked()
        {
            string text = playerInput.text;
            if (string.IsNullOrWhiteSpace(text))
                return;

            playerInput.text = string.Empty;
            MessageSubmitted?.Invoke(text);
        }
    }
}