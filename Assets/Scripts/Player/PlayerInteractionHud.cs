using UnityEngine;

namespace Espionage.Player
{
    /// <summary>Minimal crosshair and interaction prompt (immediate-mode GUI, no scene setup needed).</summary>
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class PlayerInteractionHud : MonoBehaviour
    {
        [Tooltip("Sizes are fractions of the screen height so the HUD scales with resolution.")]
        [SerializeField] float dotSize = 0.008f;
        [SerializeField] float fontSize = 0.035f;

        PlayerInteractor interactor;
        GUIStyle promptStyle;

        void Awake() => interactor = GetComponent<PlayerInteractor>();

        void OnGUI()
        {
            if (interactor == null) return;

            var target = interactor.Current;
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            float dot = Mathf.Max(4f, Screen.height * dotSize);
            var previous = GUI.color;
            GUI.color = target != null ? Color.green : Color.white;
            GUI.DrawTexture(new Rect(center.x - dot * 0.5f, center.y - dot * 0.5f, dot, dot),
                Texture2D.whiteTexture);
            GUI.color = previous;

            if (target == null || string.IsNullOrEmpty(target.Prompt)) return;

            promptStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            int size = Mathf.Max(12, Mathf.RoundToInt(Screen.height * fontSize));
            promptStyle.fontSize = size;
            GUI.Label(new Rect(center.x - Screen.width * 0.25f, center.y + dot * 2f, Screen.width * 0.5f, size * 2f),
                $"[E] {target.Prompt}", promptStyle);
        }
    }
}
