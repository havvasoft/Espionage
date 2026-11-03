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
    /// Repeatable companion test: order the companion to disable the laser from a given start, record the
    /// outcome and time, reset, repeat. Uses only the public dispatcher/companion interfaces, so it measures the
    /// same path the player's command takes. Two modes, both started from the component's context menu while
    /// the game is playing: fixed start points, and seeded random starts (the same seed repeats the same run).
    /// </summary>
    public sealed class CompanionTestRunner : MonoBehaviour
    {
        const float IdleWaitSeconds = 5f;
        const float SnapDistance = 2f;
        const float RandomSnapDistance = 1.5f;
        const int RandomSampleAttempts = 30;

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
        [Tooltip("Where the player stands in the fixed run; must be inside the laser's command zone.")]
        [SerializeField] Transform playerStart;
        [SerializeField] LaserBarrier laser;

        [Header("Fixed run")]
        [SerializeField] List<StartPoint> startPoints = new List<StartPoint>();
        [SerializeField] int trialsPerPoint = 5;

        [Header("Random run")]
        [Tooltip("Volumes (BoxColliders) the companion may start in. Trials rotate through them evenly.")]
        [SerializeField] List<BoxCollider> randomStartRegions = new List<BoxCollider>();
        [Tooltip("Volume the player may stand in; keep it inside the laser's command zone.")]
        [SerializeField] BoxCollider playerRegion;
        [SerializeField] int randomTrials = 40;
        [Tooltip("The same seed gives the same positions, so a random run can be repeated exactly.")]
        [SerializeField] int randomSeed = 12345;

        [Header("Both runs")]
        [Tooltip("Game seconds before a trial is counted as a timeout.")]
        [SerializeField] float trialTimeout = 45f;
        [Tooltip("Pause between placing the actors and giving the order.")]
        [SerializeField] float settleSeconds = 0.75f;
        [Tooltip("1 = real time. Use 1 for the numbers in the report.")]
        [SerializeField, Range(1f, 5f)] float timeScale = 1f;

        struct Setup
        {
            public string Label;
            public int Number;
            public bool Negative;
            public Vector3 CompanionPosition;
            public Quaternion CompanionRotation;
            public Vector3 PlayerPosition;
            public Quaternion PlayerRotation;
        }

        sealed class Trial
        {
            public string Point;
            public int Number;
            public bool Negative;
            public string Outcome;
            public float Seconds;
            public bool LaserDisabled;
            public bool Passed;
            public Vector3 Start;
        }

        ICommandDispatcher dispatcher;
        ICompanion companionCore;
        NavMeshAgent companionAgent;
        CharacterController playerController;
        bool running;

        [ContextMenu("Run companion tests (fixed points)")]
        void RunFixedFromMenu()
        {
            if (!CanStart() || !Resolve()) return;

            if (startPoints.Count == 0 || playerStart == null)
            {
                Debug.LogError($"{nameof(CompanionTestRunner)}: assign at least one start point and the player start.", this);
                return;
            }

            var setups = new List<Setup>();
            foreach (var start in startPoints)
            {
                if (start == null || start.point == null)
                {
                    Debug.LogWarning("Skipping a start point with no transform.");
                    continue;
                }

                for (int n = 1; n <= trialsPerPoint; n++)
                {
                    setups.Add(new Setup
                    {
                        Label = start.label,
                        Number = n,
                        Negative = start.expectUnreachable,
                        CompanionPosition = start.point.position,
                        CompanionRotation = start.point.rotation,
                        PlayerPosition = playerStart.position,
                        PlayerRotation = playerStart.rotation
                    });
                }
            }

            StartCoroutine(RunSession("fixed", "companion", setups));
        }

        [ContextMenu("Run companion tests (random starts)")]
        void RunRandomFromMenu()
        {
            if (!CanStart() || !Resolve()) return;

            if (randomStartRegions.Count == 0 || playerRegion == null || playerStart == null || randomTrials < 1)
            {
                Debug.LogError($"{nameof(CompanionTestRunner)}: assign the random start regions, the player region, " +
                               "the player start (for its height) and a positive trial count.", this);
                return;
            }

            StartCoroutine(RunSession($"random (seed {randomSeed})", $"companion_random_seed{randomSeed}", BuildRandomSetups()));
        }

        bool CanStart()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{nameof(CompanionTestRunner)}: enter Play mode first.", this);
                return false;
            }

            if (running)
            {
                Debug.LogWarning($"{nameof(CompanionTestRunner)}: a run is already in progress.", this);
                return false;
            }

            return true;
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

            if (dispatcher == null || companionCore == null || companionAgent == null || player == null || laser == null)
            {
                Debug.LogError($"{nameof(CompanionTestRunner)}: dispatcher, companion (with NavMeshAgent), player and " +
                               "laser must be assigned.", this);
                return false;
            }

            return true;
        }

        List<Setup> BuildRandomSetups()
        {
            var rng = new System.Random(randomSeed);
            var setups = new List<Setup>();
            var perRegion = new int[randomStartRegions.Count];

            for (int i = 0; i < randomTrials; i++)
            {
                int index = i % randomStartRegions.Count;
                var region = randomStartRegions[index];
                perRegion[index]++;

                setups.Add(new Setup
                {
                    Label = "Random_" + region.name,
                    Number = perRegion[index],
                    Negative = false,
                    CompanionPosition = SampleOnNavMesh(region, rng),
                    CompanionRotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f),
                    PlayerPosition = RandomInside(playerRegion, rng, playerStart.position.y),
                    PlayerRotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360.0), 0f)
                });
            }

            return setups;
        }

        /// <summary>A random point inside the box, snapped to the NavMesh (retries a few times, then the box center).</summary>
        static Vector3 SampleOnNavMesh(BoxCollider box, System.Random rng)
        {
            for (int attempt = 0; attempt < RandomSampleAttempts; attempt++)
            {
                Vector3 candidate = RandomInside(box, rng, null);
                if (NavMesh.SamplePosition(candidate, out var hit, RandomSnapDistance, NavMesh.AllAreas)
                    && box.bounds.Contains(new Vector3(hit.position.x, box.bounds.center.y, hit.position.z)))
                    return hit.position;
            }

            return box.bounds.center;
        }

        static Vector3 RandomInside(BoxCollider box, System.Random rng, float? fixedHeight)
        {
            Vector3 local = box.center + new Vector3(
                (float)(rng.NextDouble() - 0.5) * box.size.x,
                (float)(rng.NextDouble() - 0.5) * box.size.y,
                (float)(rng.NextDouble() - 0.5) * box.size.z);

            Vector3 world = box.transform.TransformPoint(local);
            if (fixedHeight.HasValue) world.y = fixedHeight.Value;
            return world;
        }

        IEnumerator RunSession(string title, string filePrefix, List<Setup> setups)
        {
            running = true;
            float previousScale = Time.timeScale;
            Time.timeScale = timeScale;

            var trials = new List<Trial>();
            Debug.Log($"Companion test started ({title}): {setups.Count} trials.");

            try
            {
                foreach (var setup in setups)
                    yield return RunTrial(setup, trials);
            }
            finally
            {
                Time.timeScale = previousScale;
                running = false;
            }

            Report(trials, filePrefix);
        }

        IEnumerator RunTrial(Setup setup, List<Trial> trials)
        {
            yield return WaitUntilIdle();

            laser.ResetBarrier();
            PlaceCompanion(setup.CompanionPosition, setup.CompanionRotation);
            PlacePlayer(setup.PlayerPosition, setup.PlayerRotation);
            yield return new WaitForSeconds(settleSeconds);

            var trial = new Trial
            {
                Point = setup.Label,
                Number = setup.Number,
                Negative = setup.Negative,
                Start = setup.CompanionPosition
            };

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
            trial.Passed = setup.Negative
                ? trial.Outcome == nameof(CommandResult.Unreachable) && !trial.LaserDisabled
                : trial.Outcome == nameof(CommandResult.Succeeded) && trial.LaserDisabled;

            trials.Add(trial);
            Debug.Log($"[{setup.Label} #{setup.Number}] {trial.Outcome}, {trial.Seconds:0.0}s, " +
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

        void PlaceCompanion(Vector3 position, Quaternion rotation)
        {
            Vector3 onMesh = NavMesh.SamplePosition(position, out var hit, SnapDistance, NavMesh.AllAreas)
                ? hit.position
                : position;

            companionAgent.Warp(onMesh);
            companion.rotation = rotation;
        }

        void PlacePlayer(Vector3 position, Quaternion rotation)
        {
            // A CharacterController overrides direct position changes unless it is switched off first.
            if (playerController != null) playerController.enabled = false;
            player.SetPositionAndRotation(position, rotation);
            if (playerController != null) playerController.enabled = true;
        }

        void Report(List<Trial> trials, string filePrefix)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string summary = BuildSummary(trials, out string summaryCsv);
            Debug.Log(summary);

            try
            {
                string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "TestResults");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, $"{filePrefix}_trials_{stamp}.csv"), BuildTrialsCsv(trials), Encoding.UTF8);
                File.WriteAllText(Path.Combine(folder, $"{filePrefix}_summary_{stamp}.csv"), summaryCsv, Encoding.UTF8);
                Debug.Log($"Companion test results saved to: {folder}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not write the test result files: {e.Message}");
            }
        }

        static string BuildTrialsCsv(List<Trial> trials)
        {
            var sb = new StringBuilder("point,trial,kind,outcome,seconds,laser_disabled,passed,start_x,start_z\n");
            foreach (var t in trials)
            {
                sb.Append(t.Point).Append(',').Append(t.Number).Append(',')
                  .Append(t.Negative ? "negative_control" : "normal").Append(',')
                  .Append(t.Outcome).Append(',')
                  .Append(t.Seconds.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                  .Append(t.LaserDisabled).Append(',').Append(t.Passed).Append(',')
                  .Append(t.Start.x.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                  .Append(t.Start.z.ToString("0.00", CultureInfo.InvariantCulture)).Append('\n');
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
