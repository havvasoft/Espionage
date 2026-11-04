using System.Collections;
using Espionage.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Espionage.Gameplay
{
    /// <summary>
    /// Ends the level once the mainframe access is granted: completes the final objective, locks the player
    /// and shows a completion screen with Restart and Quit.
    /// </summary>
    public sealed class LevelCompletion : MonoBehaviour
    {
        [SerializeField] MonoBehaviour objectiveState; // must implement IObjectiveState
        [Tooltip("The player object (anything on it implementing IPlayerControl).")]
        [SerializeField] MonoBehaviour playerControlComponent;
        [SerializeField] ObjectiveId triggeredBy = ObjectiveId.MainframeAccessGranted;
        [SerializeField] ObjectiveId completes = ObjectiveId.LevelCompleted;
        [Tooltip("Seconds between the access being granted and the completion screen.")]
        [SerializeField] float delaySeconds = 4.5f;

        IObjectiveState objectives;
        IPlayerControl playerControl;
        System.IDisposable inputLock;
        GUIStyle titleStyle;
        GUIStyle bodyStyle;
        bool finished;

        void Awake()
        {
            objectives = SceneDependency.Resolve<IObjectiveState>(objectiveState, this);

            if (playerControlComponent != null)
                playerControl = playerControlComponent as IPlayerControl
                                ?? playerControlComponent.GetComponent<IPlayerControl>();
        }

        void OnEnable()
        {
            if (objectives == null) return;
            objectives.ObjectiveCompleted += OnObjectiveCompleted;
            if (objectives.IsComplete(triggeredBy)) StartCoroutine(Finish());
        }

        void OnDisable()
        {
            if (objectives != null) objectives.ObjectiveCompleted -= OnObjectiveCompleted;
        }

        void OnDestroy()
        {
            inputLock?.Dispose();
        }

        void OnObjectiveCompleted(ObjectiveId id)
        {
            if (id == triggeredBy && !finished) StartCoroutine(Finish());
        }

        IEnumerator Finish()
        {
            finished = true;
            yield return new WaitForSecondsRealtime(delaySeconds);

            objectives.TryComplete(completes);
            inputLock ??= playerControl?.RequestLock("Level completed");
        }

        void OnGUI()
        {
            if (objectives == null || !objectives.IsComplete(completes)) return;

            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            titleStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            bodyStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            titleStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.08f);
            bodyStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.035f);

            float w = Screen.width;
            float h = Screen.height;
            GUI.Label(new Rect(0f, h * 0.25f, w, h * 0.12f), "MISSION COMPLETE", titleStyle);
            GUI.Label(new Rect(0f, h * 0.38f, w, h * 0.08f), "Mainframe access granted.", bodyStyle);

            float buttonW = w * 0.2f;
            float buttonH = h * 0.08f;
            if (GUI.Button(new Rect(w * 0.5f - buttonW - 10f, h * 0.55f, buttonW, buttonH), "Restart"))
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);

            if (GUI.Button(new Rect(w * 0.5f + 10f, h * 0.55f, buttonW, buttonH), "Quit"))
                Quit();
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
