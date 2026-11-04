using UnityEngine;

namespace Espionage.Guards
{
    /// <summary>Tints the guard from calm blue through yellow to red as its suspicion rises.</summary>
    [RequireComponent(typeof(GuardDetector))]
    public sealed class GuardIndicator : MonoBehaviour
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int Color_ = Shader.PropertyToID("_Color");

        [SerializeField] Color calmColor = new Color(0.25f, 0.45f, 0.9f);
        [SerializeField] Color suspiciousColor = Color.yellow;
        [SerializeField] Color alertColor = Color.red;

        IGuardSuspicion suspicion;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        float lastShown = -1f;

        void Awake()
        {
            suspicion = GetComponent<IGuardSuspicion>();
            renderers = FindTintRenderers();
            block = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Only parts whose name starts with "Tint" change colour, so a multi-part robot keeps its eyes and details.
        /// A guard without such parts (e.g. a single capsule) is tinted as a whole.
        /// </summary>
        Renderer[] FindTintRenderers()
        {
            var all = GetComponentsInChildren<Renderer>();
            var tinted = new System.Collections.Generic.List<Renderer>();
            foreach (var r in all)
                if (r.gameObject.name.StartsWith("Tint")) tinted.Add(r);

            return tinted.Count > 0 ? tinted.ToArray() : all;
        }

        void Update()
        {
            float value = suspicion.Suspicion;
            if (Mathf.Abs(value - lastShown) < 0.01f) return;
            lastShown = value;

            Color color = value < 0.5f
                ? Color.Lerp(calmColor, suspiciousColor, value / 0.5f)
                : Color.Lerp(suspiciousColor, alertColor, (value - 0.5f) / 0.5f);

            foreach (var r in renderers)
            {
                r.GetPropertyBlock(block);
                block.SetColor(BaseColor, color);
                block.SetColor(Color_, color);
                r.SetPropertyBlock(block);
            }
        }
    }
}
