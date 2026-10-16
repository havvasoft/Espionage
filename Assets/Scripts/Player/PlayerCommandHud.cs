using UnityEngine;

namespace Espionage.Player
{
    /// <summary>Shows the latest companion message near the top of the screen for a few seconds.</summary>
    [RequireComponent(typeof(PlayerCompanionCommander))]
    public sealed class PlayerCommandHud : MonoBehaviour
    {
        [SerializeField] float showSeconds = 3f;
        [SerializeField] float fontSize = 0.03f;

        PlayerCompanionCommander commander;
        GUIStyle style;
        string message;
        float hideAt;

        void Awake() => commander = GetComponent<PlayerCompanionCommander>();

        void OnEnable() => commander.MessageRaised += OnMessage;

        void OnDisable() => commander.MessageRaised -= OnMessage;

        void OnMessage(string text)
        {
            message = text;
            hideAt = Time.unscaledTime + showSeconds;
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(message) || Time.unscaledTime > hideAt) return;

            style ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            int size = Mathf.Max(12, Mathf.RoundToInt(Screen.height * fontSize));
            style.fontSize = size;
            GUI.Label(new Rect(Screen.width * 0.25f, Screen.height * 0.08f, Screen.width * 0.5f, size * 2f),
                message, style);
        }
    }
}
