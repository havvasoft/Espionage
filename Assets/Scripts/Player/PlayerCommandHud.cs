using Espionage.Core;
using UnityEngine;

namespace Espionage.Player
{
    /// <summary>
    /// Shows the latest message (companion, respawn, or from world objects through <see cref="IPlayerMessageSink"/>)
    /// near the top of the screen for a few seconds.
    /// </summary>
    [RequireComponent(typeof(PlayerCompanionCommander))]
    public sealed class PlayerCommandHud : MonoBehaviour, IPlayerMessageSink
    {
        [SerializeField] float showSeconds = 3f;
        [SerializeField] float fontSize = 0.03f;

        PlayerCompanionCommander commander;
        GUIStyle style;
        string message;
        float hideAt;

        PlayerRespawner respawner; // optional second message source

        void Awake()
        {
            commander = GetComponent<PlayerCompanionCommander>();
            respawner = GetComponent<PlayerRespawner>();
        }

        void OnEnable()
        {
            commander.MessageRaised += OnMessage;
            if (respawner != null) respawner.MessageRaised += OnMessage;
        }

        void OnDisable()
        {
            commander.MessageRaised -= OnMessage;
            if (respawner != null) respawner.MessageRaised -= OnMessage;
        }

        public void ShowMessage(string text) => OnMessage(text);

        void OnMessage(string text)
        {
            message = text;
            hideAt = Time.unscaledTime + showSeconds;
        }

        void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            int size = Mathf.Max(12, Mathf.RoundToInt(Screen.height * fontSize));
            style.fontSize = size;

            if (commander.IsLookingAtCompanion)
            {
                GUI.Label(new Rect(Screen.width * 0.25f, Screen.height * 0.5f + size, Screen.width * 0.5f, size * 2f),
                    "[C] Order companion", style);
            }

            if (string.IsNullOrEmpty(message) || Time.unscaledTime > hideAt) return;

            GUI.Label(new Rect(Screen.width * 0.2f, Screen.height * 0.08f, Screen.width * 0.6f, size * 3f),
                message, style);
        }
    }
}
