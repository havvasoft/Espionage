using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Espionage.Core;
using Espionage.Gameplay;
using UnityEngine;
using UnityEngine.AI;

namespace Espionage.Testing
{
    /// <summary>
    /// Repeatable companion test: from each start point, order the companion to disable the laser, record the
    /// outcome and time, reset, repeat. Uses only the public dispatcher/companion interfaces, so it measures the
    /// same path the player's command takes. Run it from the component's context menu while the game is playing.
    /// </summary>
    public sealed class CompanionTestRunner : MonoBehaviour
    {
        const float IdleWaitSeconds = 5f;
        const float SnapDistance = 2f;

        [Serializable]
        public sealed class StartPoint
        {
            public string label;
            public Transform point;
            [Tooltip("Negative control: the task is impossible from here, so giving up (Unreachable) is the right outcome.")]
            public bool expectUnreachable;
        }

        [Header("Scene references")]
        [Tooltip("Object with the command dispatcher (anything implementing ICommandDispatcher).")]
        [SerializeField] MonoBehaviour dispatcherComponent;
        [Tooltip("The companion. Needs a NavMeshAgent and an ICompanion component.")]
        [SerializeField] Transform companion;
        [SerializeField] Transform player;
        [Tooltip("Where the player stands for every trial; must be inside the laser's command zone.")]
        [SerializeField] Transform playerStart;
        [SerializeField] LaserBarrier laser;

        [Header("Trials")]
        [SerializeField] List<StartPoint> startPoints = new List<StartPoint>();
        [SerializeField] int trialsPerPoint = 5;
        [Tooltip("Game seconds before a trial is counted as a timeout.")]
        [SerializeField] float trialTimeout = 45f;
        [Tooltip("Pause between placing the actors and giving the order.")]
        [SerializeField] float settleSeconds = 0.75f;
        [Tooltip("1 = real time. Use 1 for the numbers in the report.")]
        [SerializeField, Range(1f, 5f)] float timeScale = 1f;

        sealed class Trial
        {
            public string Point;
            public int Number;
            public bool Negative;
            public string Outcome;
            public float Seconds;
            public bool LaserDisabled;
            public bool Passed;
        }

        ICommandDispatcher dispatcher;
        ICompanion companionCore;
        NavMeshAgent companionAgent;
        CharacterController playerController;
        bool running;

        [ContextMenu("Run companion tests")]
        void RunFromMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{nameof(CompanionTestRunner)}: enter Play mode first.", this);
                return;
            }

            if (running)
            {
                Debug.LogWarning($"{nameof(CompanionTestRunner)}: a run is already in progress.", this);
                return;
            }

            if (!Resolve()) return;
            StartCoroutine(RunAll());
        }

        bool Resolve()
        {
            if (dispatcherComponent != null)
                dispatcher = dispatcherComponent as ICommandDispatcher ?? dispatcherComponent.GetComponent<ICommandDispatcher>();

            if (companion != null)
            {
                companionCore = companion.GetComponent<ICompanion>();
                companionAgent = companion.GetComponent<NavMeshAgent>();
            }

            if (player != null) playerController = player.GetComponent<CharacterController>();

            if (dispatcher == null || companionCore == null || companionAgent == null
                || player == null || playerStart == null || laser == null || startPoints.Count == 0)
            {
                Debug.LogError($"{nameof(CompanionTestRunner)}: dispatcher, companion (with NavMeshAgent), player, " +
                               "player start, laser and at least one start point must be assigned.", this);
                return false;
            }

            return true;
        }

        IEnumerator RunAll()
        {
            running = true;
            float previousScale = Time.timeScale;
            Time.timeScale = timeScale;

            var trials = new List<Trial>();
            Debug.Log($"Companion test started: {startPoints.Count} points x {trialsPerPoint} trials.");

            try
            {
                foreach (var start in startPoints)
                {
                    if (start == null || start.point == null)
                    {
                        Debug.LogWarning("Skipping a start point with no transform.");
                        continue;
                    }

                    for (int n = 1; n <= trialsPerPoint; n++)
                        yield return RunTrial(start, n, trials);
                }
            }
            finally
            {
                Time.timeScale = previousScale;
                running = false;
            }

            Report(trials);
        }

        IEnumerator RunTrial(StartPoint start, int number, List<Trial> trials)
        {
            yield return WaitUntilIdle();

            laser.ResetBarrier();
            PlaceCompanion(start.point);
            PlacePlayer();
            yield return new WaitForSeconds(settleSeconds);

            var trial = new Trial { Point = start.label, Number = number, Negative = start.expectUnreachable };

            bool finished = false;
            CommandResult finishedResult = CommandResult.Interrupted;
            Action<CompanionCommandType, CommandResult> onFinished = (type, result) =>
            {
                finished = true;
                finishedResult = result;
            };

            dispatcher.CommandFinished += onFinished;
            DispatchResult dispatch = dispatcher.TryDispatch(CompanionCommandType.DisableLaser);

            float elapsed = 0f;
            if (dispatch == DispatchResult.Accepted)
            {
                while (!finished && elapsed < trialTimeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                trial.Outcome = finished ? finishedResult.ToString() : "Timeout";
            }
            else
            {
                trial.Outcome = "Rejected_" + dispatch;
            }

            dispatcher.CommandFinished -= onFinished;

            trial.Seconds = elapsed;
            trial.LaserDisabled = !laser.IsActive;
            trial.Passed = start.expectUnreachable
                ? trial.Outcome == nameof(CommandResult.Unreachable) && !trial.LaserDisabled
                : trial.Outcome == nameof(CommandResult.Succeeded) && trial.LaserDisabled;

            trials.Add(trial);
            Debug.Log($"[{start.label} #{number}] {trial.Outcome}, {trial.Seconds:0.0}s, " +
                      $"laser disabled: {trial.LaserDisabled} -> {(trial.Passed ? "PASS" : "FAIL")}");
        }

        IEnumerator WaitUntilIdle()
        {
            float waited = 0f;
            while (companionCore.IsBusy && waited < IdleWaitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (companionCore.IsBusy)
                Debug.LogWarning("The companion was still busy before a trial; this trial may be rejected.");
        }

        void PlaceCompanion(Transform point)
        {
            Vector3 position = NavMesh.SamplePosition(point.position, out var hit, SnapDistance, NavMesh.AllAreas)
                ? hit.position
                : point.position;

            companionAgent.Warp(position);
            companion.rotation = point.rotation;
        }

        void PlacePlayer()
        {
            // A CharacterController overrides direct position changes unless it is switched off first.
            if (playerController != null) playerController.enabled = false;
            player.SetPositionAndRotation(playerStart.position, playerStart.rotation);
            if (playerController != null) playerController.enabled = true;
        }

        void Report(List<Trial> trials)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string summary = BuildSummary(trials, out string summaryCsv);
            Debug.Log(summary);

            try
            {
                string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "TestResults");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, $"companion_trials_{stamp}.csv"), BuildTrialsCsv(trials), Encoding.UTF8);
                File.WriteAllText(Path.Combine(folder, $"companion_summary_{stamp}.csv"), summaryCsv, Encoding.UTF8);
                Debug.Log($"Companion test results saved to: {folder}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not write the test result files: {e.Message}");
            }
        }

        static string BuildTrialsCsv(List<Trial> trials)
        {
            var sb = new StringBuilder("point,trial,kind,outcome,seconds,laser_disabled,passed\n");
            foreach (var t in trials)
            {
                sb.Append(t.Point).Append(',').Append(t.Number).Append(',')
                  .Append(t.Negative ? "negative_control" : "normal").Append(',')
                  .Append(t.Outcome).Append(',')
                  .Append(t.Seconds.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                  .Append(t.LaserDisabled).Append(',').Append(t.Passed).Append('\n');
            }

            return sb.ToString();
        }

        static string BuildSummary(List<Trial> trials, out string csv)
        {
            var text = new StringBuilder("Companion test summary\n");
            var sheet = new StringBuilder("point,kind,trials,passed,pass_rate_percent,avg_seconds,min_seconds,max_seconds\n");

            var order = new List<string>();
            foreach (var t in trials)
                if (!order.Contains(t.Point)) order.Add(t.Point);

            int normalTrials = 0, normalPassed = 0;
            float normalTime = 0f;

            foreach (string point in order)
            {
                int count = 0, passed = 0;
                float sum = 0f, min = float.MaxValue, max = 0f;
                bool negative = false;

                foreach (var t in trials)
                {
                    if (t.Point != point) continue;
                    negative = t.Negative;
                    count++;
                    if (!t.Passed) continue;

                    passed++;
                    sum += t.Seconds;
                    min = Mathf.Min(min, t.Seconds);
                    max = Mathf.Max(max, t.Seconds);
                }

                float avg = passed > 0 ? sum / passed : 0f;
                float rate = count > 0 ? 100f * passed / count : 0f;
                if (passed == 0) min = 0f;

                text.AppendLine($"{point}{(negative ? " (negative control)" : "")}: {passed}/{count} " +
                                $"({rate:0}%), avg {avg:0.0}s, min {min:0.0}s, max {max:0.0}s");
                sheet.Append(point).Append(',').Append(negative ? "negative_control" : "normal").Append(',')
                     .Append(count).Append(',').Append(passed).Append(',')
                     .Append(rate.ToString("0", CultureInfo.InvariantCulture)).Append(',')
                     .Append(avg.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                     .Append(min.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                     .Append(max.ToString("0.00", CultureInfo.InvariantCulture)).Append('\n');

                if (!negative)
                {
                    normalTrials += count;
                    normalPassed += passed;
                    normalTime += sum;
                }
            }

            if (normalTrials > 0)
            {
                float overall = 100f * normalPassed / normalTrials;
                float overallAvg = normalPassed > 0 ? normalTime / normalPassed : 0f;
                text.AppendLine($"OVERALL (excluding negative controls): {normalPassed}/{normalTrials} " +
                                $"({overall:0.0}%), average completion {overallAvg:0.0}s");
                sheet.Append("OVERALL,normal,").Append(normalTrials).Append(',').Append(normalPassed).Append(',')
                     .Append(overall.ToString("0.0", CultureInfo.InvariantCulture)).Append(',')
                     .Append(overallAvg.ToString("0.00", CultureInfo.InvariantCulture)).Append(",,\n");
            }

            csv = sheet.ToString();
            return text.ToString();
        }
    }
}
