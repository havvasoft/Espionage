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
            sendButton.interactable = !busy;
            playerInput.interactable = !busy;
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