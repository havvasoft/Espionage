using UnityEngine;

namespace Espionage.Guards
{
    /// <summary>Tints the guard red while it sees the target, so the player (and the audience) can tell.</summary>
    [RequireComponent(typeof(GuardVision))]
    public sealed class GuardIndicator : MonoBehaviour
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int Color_ = Shader.PropertyToID("_Color");

        [SerializeField] Color calmColor = new Color(0.25f, 0.45f, 0.9f);
        [SerializeField] Color alertColor = Color.red;

        IGuardVision vision;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        bool? lastState;

        void Awake()
        {
            vision = GetComponent<IGuardVision>();
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
        }

        void Update()
        {
            bool seen = vision.CanSeeTarget;
            if (lastState == seen) return;
            lastState = seen;

            Color color = seen ? alertColor : calmColor;
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
