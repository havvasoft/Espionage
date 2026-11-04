using Espionage.Core;
using UnityEngine;

namespace Espionage.Gameplay
{
    /// <summary>
    /// The laser's control panel. The player cannot switch the laser off from it: using it only explains that the
    /// companion has to do it. Its status light is red while the laser is on and green once it is off.
    /// </summary>
    public sealed class LaserControlPanel : MonoBehaviour, IInteractable
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] LaserBarrier barrier;
        [Tooltip("The small light that shows the laser state (optional).")]
        [SerializeField] Renderer indicator;
        [SerializeField] Color lockedColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField] Color openColor = new Color(0.2f, 1f, 0.4f);
        [SerializeField] string deniedMessage = "ACCESS DENIED. Only your companion can unlock this laser: look at it and press [C].";

        MaterialPropertyBlock block;
        bool? shownActive;

        public string Prompt => "Use control panel";

        public bool CanInteract(IInteractor interactor) => barrier != null && barrier.IsActive;

        public void Interact(IInteractor interactor)
        {
            var sink = interactor.Origin.GetComponentInParent<IPlayerMessageSink>();
            if (sink != null) sink.ShowMessage(deniedMessage);
            else Debug.Log(deniedMessage);
        }

        void Update()
        {
            if (barrier == null || indicator == null) return;

            bool active = barrier.IsActive;
            if (shownActive == active) return;
            shownActive = active;

            Color color = active ? lockedColor : openColor;
            block ??= new MaterialPropertyBlock();
            indicator.GetPropertyBlock(block);
            block.SetColor(BaseColor, color * 0.3f);
            block.SetColor(EmissionColor, color * 2.5f);
            indicator.SetPropertyBlock(block);
        }
    }
}
