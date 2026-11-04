using System.Collections;
using Espionage.Core;
using Espionage.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Espionage.Integration
{
    /// <summary>
    /// Lets the player start the mainframe operator's conversation by interacting with the NPC.
    /// Only wires the scene together: the conversation itself lives in the dialogue controller.
    /// Esc leaves the conversation; once access is granted it closes by itself after a short delay.
    /// </summary>
    public sealed class NpcConversation : MonoBehaviour, IInteractable
    {
        [SerializeField] DialogueController dialogue;
        [SerializeField] ObjectiveId grantedObjective = ObjectiveId.MainframeAccessGranted;
        [Tooltip("Object with the scene's objective state (anything implementing IObjectiveState).")]
        [SerializeField] MonoBehaviour objectiveState;
        [Tooltip("Seconds the last reply stays readable after access is granted, before the panel closes.")]
        [SerializeField] float closeDelayAfterGrant = 3f;

        IObjectiveState objectives;
        bool isOpen;

        public string Prompt => "Talk to the operator";

        void Awake()
        {
            if (objectiveState != null)
                objectives = objectiveState as IObjectiveState ?? objectiveState.GetComponent<IObjectiveState>();

            if (objectives == null)
                Debug.LogError($"{name}: 'objectiveState' is missing or has no IObjectiveState; the NPC cannot talk.", this);
            if (dialogue == null)
                Debug.LogError($"{name}: 'dialogue' (DialogueController) is not assigned; the NPC cannot talk.", this);
        }

        void Start()
        {
            // The dialogue panel starts hidden; the player opens it by talking to the NPC.
            if (dialogue != null) dialogue.Close();
        }

        void OnEnable()
        {
            if (objectives != null) objectives.ObjectiveCompleted += OnObjectiveCompleted;
        }

        void OnDisable()
        {
            if (objectives != null) objectives.ObjectiveCompleted -= OnObjectiveCompleted;
        }

        void Update()
        {
            if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseDialogue();
        }

        public bool CanInteract(IInteractor interactor) =>
            dialogue != null && objectives != null && !isOpen && !objectives.IsComplete(grantedObjective);

        public void Interact(IInteractor interactor)
        {
            var control = interactor.Origin.GetComponentInParent<IPlayerControl>();
            dialogue.Bind(objectives, control);
            dialogue.Open();
            isOpen = true;
        }

        void OnObjectiveCompleted(ObjectiveId id)
        {
            if (id == grantedObjective && isOpen) StartCoroutine(CloseAfterDelay());
        }

        IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSecondsRealtime(closeDelayAfterGrant);
            if (isOpen) CloseDialogue();
        }

        void CloseDialogue()
        {
            dialogue.Close();
            isOpen = false;
        }
    }
}
