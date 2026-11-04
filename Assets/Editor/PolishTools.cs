using System.Collections.Generic;
using Espionage.Gameplay;
using Espionage.Player;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Espionage.EditorTools
{
    /// <summary>
    /// One-click visual polish for the level (editor only; not part of the game build).
    /// Each step is safe to run again: it rebuilds what it created before. Undo with Git or Ctrl+Z.
    /// Run with the LevelTest scene open, one step at a time, and play-test in between.
    /// </summary>
    public static class PolishTools
    {
        const string MaterialFolder = "Assets/Materials/Polish";
        const string EnvRoot = "Decor_Env";
        const string PropsRoot = "Decor_Props";
        const string DetailsRoot = "Decor_Details";
        const string VisualName = "Visual";
        const string PartPrefix = "PolishPart_";

        struct Room
        {
            public string Name;
            public float X0, X1, Z0, Z1;
            public Color Floor;
            public Room(string name, float x0, float x1, float z0, float z1, Color floor)
            {
                Name = name; X0 = x0; X1 = x1; Z0 = z0; Z1 = z1; Floor = floor;
            }
            public Vector3 Center(float y) => new Vector3((X0 + X1) * 0.5f, y, (Z0 + Z1) * 0.5f);
        }

        static readonly Color Warm = new Color(1f, 0.85f, 0.6f);
        static readonly Color Cool = new Color(0.7f, 0.85f, 1f);
        static readonly Color Red = new Color(1f, 0.3f, 0.25f);
        static readonly Color White = new Color(0.8f, 0.9f, 1f);
        static readonly Color Teal = new Color(0.3f, 1f, 0.9f);

        // Rectangles run to the wall centre lines, so neighbouring ceilings touch without overlapping.
        static readonly Room[] Rooms =
        {
            new Room("R1", -4, 4, 0, 8,   new Color(0.30f, 0.28f, 0.25f)),
            new Room("R2", -10, 10, 8, 16, new Color(0.20f, 0.25f, 0.31f)),
            new Room("Corridor", -2, 2, 16, 22, new Color(0.26f, 0.18f, 0.18f)),
            new Room("Hall", -12, 6, 22, 34, new Color(0.17f, 0.18f, 0.20f)),
            new Room("E1", 6, 18, 30, 34, new Color(0.16f, 0.17f, 0.19f)),
            new Room("N1", 14, 18, 34, 38, new Color(0.16f, 0.17f, 0.19f)),
            new Room("W1", 4, 14, 34, 38, new Color(0.16f, 0.17f, 0.19f)),
            new Room("Mainframe", -4, 4, 34, 42, new Color(0.15f, 0.25f, 0.26f)),
        };

        // ------------------------------------------------------------------------------------------------
        // Step 1: environment
        // ------------------------------------------------------------------------------------------------
        [MenuItem("Espionage/Polish/1 Environment (colours, ceiling, lights)")]
        static void Environment()
        {
            var level = GameObject.Find("Level");
            if (level == null) { Debug.LogError("Polish: open the LevelTest scene first (no 'Level' object)."); return; }

            var wallMats = new Dictionary<string, Material>
            {
                { "R1_", Mat("Wall_R1", new Color(0.50f, 0.47f, 0.42f)) },
                { "R2_", Mat("Wall_R2", new Color(0.36f, 0.42f, 0.50f)) },
                { "C_", Mat("Wall_Corridor", new Color(0.45f, 0.30f, 0.30f)) },
                { "H_", Mat("Wall_Hall", new Color(0.28f, 0.31f, 0.36f)) },
                { "E1_", Mat("Wall_Hall", new Color(0.28f, 0.31f, 0.36f)) },
                { "N1_", Mat("Wall_Hall", new Color(0.28f, 0.31f, 0.36f)) },
                { "W1_", Mat("Wall_Hall", new Color(0.28f, 0.31f, 0.36f)) },
                { "M_", Mat("Wall_Mainframe", new Color(0.22f, 0.38f, 0.40f)) },
            };
            var floor = Mat("Floor", new Color(0.20f, 0.21f, 0.23f), 0f, 0.35f);
            var wood = Mat("Wood", new Color(0.42f, 0.27f, 0.15f), 0f, 0.4f);
            var metal = Mat("Metal", new Color(0.33f, 0.38f, 0.45f), 0.7f, 0.5f);
            var crate = Mat("Crate", new Color(0.55f, 0.42f, 0.18f));
            var cover = Mat("Cover", new Color(0.18f, 0.33f, 0.42f), 0.3f, 0.45f);
            var dark = Mat("Dark", new Color(0.07f, 0.08f, 0.10f), 0.2f, 0.6f);
            var card = Mat("Card", new Color(0.1f, 0.5f, 0.2f), 0f, 0.5f, new Color(0.2f, 2.0f, 0.5f));

            foreach (var r in level.GetComponentsInChildren<MeshRenderer>(true))
            {
                string n = r.gameObject.name;
                Material m = null;

                if (n.StartsWith("Floor")) m = floor;
                else if (n == "R2_Partition" || n.Contains("_Wall_"))
                {
                    foreach (var pair in wallMats)
                        if (n.StartsWith(pair.Key)) { m = pair.Value; break; }
                }
                else if (n.StartsWith("Cover_")) m = cover;
                else if (n.StartsWith("H_Crate_")) m = crate;
                else if (n == "R1_Table" || n == "R2_Desk") m = wood;
                else if (n == "R1_Cabinet") m = metal;
                else if (n == "R2_Terminal" || n == "M_Mainframe" || n == "LaserControlPanel") m = dark;
                else if (n == "EmployeeCard") m = card;

                if (m == null) continue;
                Undo.RecordObject(r, "Polish materials");
                r.sharedMaterial = m;
            }

            var root = FreshRoot(EnvRoot);
            var ceiling = Mat("Ceiling", new Color(0.11f, 0.12f, 0.14f), 0f, 0.2f);
            var hazard = Mat("Hazard", new Color(0.8f, 0.65f, 0.05f), 0f, 0.4f, new Color(0.9f, 0.7f, 0.05f));

            foreach (var room in Rooms)
            {
                // Ceiling: no collider, casts no shadow (so the sun still lights the rooms), ignored by the NavMesh bake.
                Box(root, $"Ceiling_{room.Name}", room.Center(3.1f), new Vector3(room.X1 - room.X0, 0.2f, room.Z1 - room.Z0),
                    ceiling, collider: false, ignoreNav: true, shadows: false);

                var tile = Mat($"Tile_{room.Name}", room.Floor, 0f, 0.4f);
                Box(root, $"Tile_{room.Name}", room.Center(0.012f),
                    new Vector3(room.X1 - room.X0 - 0.3f, 0.02f, room.Z1 - room.Z0 - 0.3f), tile, false, true, false);
            }

            Box(root, "Hazard_South", new Vector3(0f, 0.016f, 18.0f), new Vector3(3.7f, 0.02f, 0.15f), hazard, false, true, false);
            Box(root, "Hazard_North", new Vector3(0f, 0.016f, 20.0f), new Vector3(3.7f, 0.02f, 0.15f), hazard, false, true, false);

            Lamp(root, "R1_A", new Vector3(0f, 2.6f, 4f), Warm, 3f, 9f);
            Lamp(root, "R2_A", new Vector3(-5f, 2.6f, 12f), Cool, 3f, 10f);
            Lamp(root, "R2_B", new Vector3(5f, 2.6f, 12f), Cool, 3f, 10f);
            Lamp(root, "Corridor_A", new Vector3(0f, 2.6f, 19f), Red, 3f, 8f);
            Lamp(root, "Hall_A", new Vector3(-8f, 2.6f, 28f), White, 3f, 11f);
            Lamp(root, "Hall_B", new Vector3(-2f, 2.6f, 28f), White, 3f, 11f);
            Lamp(root, "Hall_C", new Vector3(3f, 2.6f, 28f), White, 3f, 11f);
            Lamp(root, "E1_A", new Vector3(12f, 2.6f, 32f), White, 2.5f, 8f);
            Lamp(root, "N1_A", new Vector3(16f, 2.6f, 36f), White, 2f, 6f);
            Lamp(root, "W1_A", new Vector3(9f, 2.6f, 36f), White, 2.5f, 8f);
            Lamp(root, "Mainframe_A", new Vector3(0f, 2.6f, 38f), Teal, 3f, 9f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f);
            var sun = GameObject.Find("Directional Light");
            if (sun != null && sun.TryGetComponent<Light>(out var sunLight))
            {
                Undo.RecordObject(sunLight, "Polish sun");
                sunLight.intensity = 0.7f;
            }

            Finish("Environment polish applied. Play to check it; no NavMesh re-bake needed for this step.");
        }

        // ------------------------------------------------------------------------------------------------
        // Step 2: robots
        // ------------------------------------------------------------------------------------------------
        [MenuItem("Espionage/Polish/2 Robots (companion, guards, NPC)")]
        static void Robots()
        {
            var white = Mat("RobotWhite", new Color(0.85f, 0.88f, 0.92f), 0.2f, 0.6f);
            var dark = Mat("RobotDark", new Color(0.08f, 0.09f, 0.11f), 0.3f, 0.6f);
            var cyan = Mat("EyeCyan", new Color(0.05f, 0.3f, 0.35f), 0f, 0.5f, new Color(0.1f, 1.6f, 2.0f));
            var orange = Mat("LampOrange", new Color(0.5f, 0.25f, 0.05f), 0f, 0.5f, new Color(2.0f, 0.8f, 0.1f));
            var guardBody = Mat("GuardBody", new Color(0.25f, 0.45f, 0.9f), 0.4f, 0.55f);
            var guardMetal = Mat("GuardMetal", new Color(0.18f, 0.20f, 0.24f), 0.7f, 0.5f);
            var red = Mat("EyeRed", new Color(0.4f, 0.02f, 0.02f), 0f, 0.5f, new Color(2.5f, 0.1f, 0.1f));
            var npcBody = Mat("NpcBody", new Color(0.20f, 0.40f, 0.45f), 0.3f, 0.55f);
            var npcLight = Mat("NpcLight", new Color(0.05f, 0.3f, 0.25f), 0f, 0.5f, new Color(0.2f, 2.0f, 1.6f));

            var companion = GameObject.Find("Companion");
            if (companion != null)
            {
                var v = BeginVisual(companion);
                Part(v, PrimitiveType.Cylinder, "Base", new Vector3(0f, -0.46f, 0f), new Vector3(0.44f, 0.03f, 0.44f), dark);
                Part(v, PrimitiveType.Cylinder, "BaseRing", new Vector3(0f, -0.49f, 0f), new Vector3(0.5f, 0.01f, 0.5f), cyan);
                Part(v, PrimitiveType.Sphere, "Body", new Vector3(0f, -0.15f, 0f), new Vector3(0.46f, 0.5f, 0.46f), white);
                Part(v, PrimitiveType.Cube, "ChestLight", new Vector3(0f, -0.15f, 0.225f), new Vector3(0.18f, 0.1f, 0.02f), cyan);
                Part(v, PrimitiveType.Sphere, "Head", new Vector3(0f, 0.17f, 0f), new Vector3(0.34f, 0.30f, 0.34f), white);
                Part(v, PrimitiveType.Cube, "Visor", new Vector3(0f, 0.18f, 0.14f), new Vector3(0.24f, 0.09f, 0.06f), dark);
                Part(v, PrimitiveType.Cube, "EyeL", new Vector3(-0.05f, 0.18f, 0.175f), new Vector3(0.06f, 0.04f, 0.02f), cyan);
                Part(v, PrimitiveType.Cube, "EyeR", new Vector3(0.05f, 0.18f, 0.175f), new Vector3(0.06f, 0.04f, 0.02f), cyan);
                Part(v, PrimitiveType.Cylinder, "Antenna", new Vector3(0f, 0.40f, 0f), new Vector3(0.02f, 0.08f, 0.02f), dark);
                Part(v, PrimitiveType.Sphere, "AntennaTip", new Vector3(0f, 0.49f, 0f), new Vector3(0.05f, 0.05f, 0.05f), orange);
                Part(v, PrimitiveType.Capsule, "ArmL", new Vector3(-0.27f, -0.12f, 0f), new Vector3(0.08f, 0.12f, 0.08f), white);
                Part(v, PrimitiveType.Capsule, "ArmR", new Vector3(0.27f, -0.12f, 0f), new Vector3(0.08f, 0.12f, 0.08f), white);
                HideRootRenderer(companion);
            }
            else Debug.LogWarning("Polish: no 'Companion' object found.");

            foreach (var name in new[] { "GuardPlaceholder_1", "GuardPlaceholder_2" })
            {
                var guard = GameObject.Find(name);
                if (guard == null) { Debug.LogWarning($"Polish: no '{name}' object found."); continue; }

                var v = BeginVisual(guard);
                Part(v, PrimitiveType.Cube, "BaseFoot", new Vector3(0f, -0.95f, 0f), new Vector3(0.7f, 0.1f, 0.6f), guardMetal);
                Part(v, PrimitiveType.Cylinder, "LegL", new Vector3(-0.18f, -0.65f, 0f), new Vector3(0.18f, 0.25f, 0.18f), guardMetal);
                Part(v, PrimitiveType.Cylinder, "LegR", new Vector3(0.18f, -0.65f, 0f), new Vector3(0.18f, 0.25f, 0.18f), guardMetal);
                Part(v, PrimitiveType.Cube, "Tint_Torso", new Vector3(0f, -0.1f, 0f), new Vector3(0.7f, 0.6f, 0.4f), guardBody);
                Part(v, PrimitiveType.Sphere, "Tint_ShoulderL", new Vector3(-0.45f, 0.1f, 0f), new Vector3(0.28f, 0.28f, 0.28f), guardBody);
                Part(v, PrimitiveType.Sphere, "Tint_ShoulderR", new Vector3(0.45f, 0.1f, 0f), new Vector3(0.28f, 0.28f, 0.28f), guardBody);
                Part(v, PrimitiveType.Capsule, "ArmL", new Vector3(-0.5f, -0.2f, 0f), new Vector3(0.16f, 0.3f, 0.16f), guardMetal);
                Part(v, PrimitiveType.Capsule, "ArmR", new Vector3(0.5f, -0.2f, 0f), new Vector3(0.16f, 0.3f, 0.16f), guardMetal);
                Part(v, PrimitiveType.Cube, "Tint_Head", new Vector3(0f, 0.45f, 0f), new Vector3(0.4f, 0.32f, 0.38f), guardBody);
                Part(v, PrimitiveType.Cube, "Visor", new Vector3(0f, 0.47f, 0.2f), new Vector3(0.32f, 0.1f, 0.04f), red);
                Part(v, PrimitiveType.Cylinder, "Antenna", new Vector3(0.12f, 0.72f, 0f), new Vector3(0.02f, 0.1f, 0.02f), guardMetal);
                HideRootRenderer(guard);
            }

            var npc = GameObject.Find("NPC_Placeholder");
            if (npc != null)
            {
                var v = BeginVisual(npc);
                Part(v, PrimitiveType.Cube, "BaseFoot", new Vector3(0f, -0.95f, 0f), new Vector3(0.7f, 0.1f, 0.6f), guardMetal);
                Part(v, PrimitiveType.Cylinder, "LegL", new Vector3(-0.18f, -0.65f, 0f), new Vector3(0.18f, 0.25f, 0.18f), guardMetal);
                Part(v, PrimitiveType.Cylinder, "LegR", new Vector3(0.18f, -0.65f, 0f), new Vector3(0.18f, 0.25f, 0.18f), guardMetal);
                Part(v, PrimitiveType.Cube, "Torso", new Vector3(0f, -0.1f, 0f), new Vector3(0.7f, 0.6f, 0.4f), npcBody);
                Part(v, PrimitiveType.Cube, "Badge", new Vector3(0.2f, 0.05f, 0.205f), new Vector3(0.14f, 0.09f, 0.02f), npcLight);
                Part(v, PrimitiveType.Sphere, "ShoulderL", new Vector3(-0.45f, 0.1f, 0f), new Vector3(0.28f, 0.28f, 0.28f), npcBody);
                Part(v, PrimitiveType.Sphere, "ShoulderR", new Vector3(0.45f, 0.1f, 0f), new Vector3(0.28f, 0.28f, 0.28f), npcBody);
                Part(v, PrimitiveType.Capsule, "ArmL", new Vector3(-0.5f, -0.2f, 0f), new Vector3(0.16f, 0.3f, 0.16f), guardMetal);
                Part(v, PrimitiveType.Capsule, "ArmR", new Vector3(0.5f, -0.2f, 0f), new Vector3(0.16f, 0.3f, 0.16f), guardMetal);
                Part(v, PrimitiveType.Cube, "Head", new Vector3(0f, 0.45f, 0f), new Vector3(0.4f, 0.32f, 0.38f), npcBody);
                Part(v, PrimitiveType.Cube, "Visor", new Vector3(0f, 0.47f, 0.2f), new Vector3(0.32f, 0.1f, 0.04f), npcLight);
                HideRootRenderer(npc);
            }
            else Debug.LogWarning("Polish: no 'NPC_Placeholder' object found.");

            Finish("Robots built. Re-bake the NavMesh (NavMeshSurface > Bake): the old capsules no longer block it.");
        }

        // ------------------------------------------------------------------------------------------------
        // Step 3: props
        // ------------------------------------------------------------------------------------------------
        [MenuItem("Espionage/Polish/3 Props (furniture, racks, screens)")]
        static void Props()
        {
            var root = FreshRoot(PropsRoot);

            var woodLight = Mat("WoodLight", new Color(0.55f, 0.38f, 0.22f), 0f, 0.45f);
            var metal = Mat("Metal", new Color(0.33f, 0.38f, 0.45f), 0.7f, 0.5f);
            var dark = Mat("Dark", new Color(0.07f, 0.08f, 0.10f), 0.2f, 0.6f);
            var crate = Mat("Crate", new Color(0.55f, 0.42f, 0.18f));
            var rack = Mat("Rack", new Color(0.10f, 0.11f, 0.13f), 0.6f, 0.5f);
            var screenGreen = Mat("ScreenGreen", new Color(0.02f, 0.25f, 0.05f), 0f, 0.6f, new Color(0.1f, 1.8f, 0.4f));
            var led = Mat("Led", new Color(0.02f, 0.25f, 0.05f), 0f, 0.5f, new Color(0.2f, 2.0f, 0.5f));
            var lampWarm = Mat("LampWarm", new Color(0.5f, 0.4f, 0.1f), 0f, 0.5f, new Color(2.0f, 1.5f, 0.6f));

            Box(root, "R1_Shelf", new Vector3(-3.65f, 1.0f, 6.8f), new Vector3(0.5f, 2.0f, 1.6f), metal, true, false, true);
            for (int i = 0; i < 4; i++)
                Box(root, $"R1_ShelfBoard_{i}", new Vector3(-3.38f, 0.45f + i * 0.5f, 6.8f), new Vector3(0.04f, 0.05f, 1.5f), dark, false, false, true);
            Box(root, "R1_CrateA", new Vector3(3.4f, 0.3f, 1.2f), new Vector3(0.6f, 0.6f, 0.6f), crate, true, false, true);
            Box(root, "R1_CrateB", new Vector3(3.4f, 0.85f, 1.2f), new Vector3(0.5f, 0.5f, 0.5f), woodLight, true, false, true);
            Box(root, "R1_CrateC", new Vector3(2.65f, 0.25f, 0.8f), new Vector3(0.5f, 0.5f, 0.5f), crate, true, false, true);

            Box(root, "R2_Filing_A", new Vector3(-9.55f, 0.9f, 9.3f), new Vector3(0.6f, 1.8f, 0.8f), metal, true, false, true);
            Box(root, "R2_Filing_B", new Vector3(-9.55f, 0.9f, 10.3f), new Vector3(0.6f, 1.8f, 0.8f), metal, true, false, true);
            Box(root, "R2_Filing_A_Handle", new Vector3(-9.24f, 1.3f, 9.3f), new Vector3(0.03f, 0.04f, 0.3f), dark, false, false, true);
            Box(root, "R2_Filing_B_Handle", new Vector3(-9.24f, 1.3f, 10.3f), new Vector3(0.03f, 0.04f, 0.3f), dark, false, false, true);

            float[] rackZ = { 36.4f, 37.9f, 39.4f };
            for (int i = 0; i < rackZ.Length; i++)
            {
                Box(root, $"Rack_W{i}", new Vector3(-3.45f, 1.1f, rackZ[i]), new Vector3(0.8f, 2.2f, 1.2f), rack, true, false, true);
                for (int k = 0; k < 4; k++)
                    Box(root, $"Rack_W{i}_Led{k}", new Vector3(-3.04f, 0.6f + k * 0.4f, rackZ[i]), new Vector3(0.02f, 0.06f, 0.9f), led, false, false, false);
            }

            Box(root, "Rack_E0", new Vector3(3.45f, 1.1f, 40.3f), new Vector3(0.8f, 2.2f, 1.4f), rack, true, false, true);
            for (int k = 0; k < 4; k++)
                Box(root, $"Rack_E0_Led{k}", new Vector3(3.04f, 0.6f + k * 0.4f, 40.3f), new Vector3(0.02f, 0.06f, 1.1f), led, false, false, false);

            var table = GameObject.Find("R1_Table");
            if (table != null)
            {
                ClearParts(table.transform);
                var t = table.transform;
                Part(t, PrimitiveType.Cube, PartPrefix + "Top", new Vector3(0f, 0.375f, 0f), new Vector3(1.8f, 0.05f, 1.0f), woodLight);
                Part(t, PrimitiveType.Cylinder, PartPrefix + "LampBase", new Vector3(0.6f, 0.41f, -0.2f), new Vector3(0.14f, 0.01f, 0.14f), dark);
                Part(t, PrimitiveType.Sphere, PartPrefix + "LampBulb", new Vector3(0.6f, 0.55f, -0.2f), new Vector3(0.14f, 0.14f, 0.14f), lampWarm);
            }

            var cabinet = GameObject.Find("R1_Cabinet");
            if (cabinet != null)
            {
                ClearParts(cabinet.transform);
                var t = cabinet.transform;
                Part(t, PrimitiveType.Cube, PartPrefix + "Seam", new Vector3(0.505f, 0f, 0f), new Vector3(0.01f, 1.8f, 0.02f), dark);
                Part(t, PrimitiveType.Cube, PartPrefix + "HandleL", new Vector3(0.52f, 0f, -0.08f), new Vector3(0.03f, 0.25f, 0.03f), dark);
                Part(t, PrimitiveType.Cube, PartPrefix + "HandleR", new Vector3(0.52f, 0f, 0.08f), new Vector3(0.03f, 0.25f, 0.03f), dark);
            }

            var desk = GameObject.Find("R2_Desk");
            if (desk != null)
            {
                ClearParts(desk.transform);
                Part(desk.transform, PrimitiveType.Cube, PartPrefix + "Keyboard", new Vector3(0f, 0.415f, -0.15f), new Vector3(0.5f, 0.03f, 0.18f), dark);
            }

            var terminal = GameObject.Find("R2_Terminal");
            if (terminal != null)
            {
                ClearParts(terminal.transform);
                Part(terminal.transform, PrimitiveType.Cube, PartPrefix + "Screen", new Vector3(0f, 0f, -0.06f), new Vector3(0.7f, 0.4f, 0.02f), screenGreen);
            }

            Finish("Props placed. Re-bake the NavMesh (NavMeshSurface > Bake) because the furniture now has colliders.");
        }

        // ------------------------------------------------------------------------------------------------
        // Step 4: the player's own arms and shadow (first person)
        // ------------------------------------------------------------------------------------------------
        [MenuItem("Espionage/Polish/4 Player arms and shadow")]
        static void PlayerArms()
        {
            var cameraObject = GameObject.Find("Main Camera");
            var player = GameObject.Find("Player");
            if (cameraObject == null || player == null)
            {
                Debug.LogError("Polish: no 'Main Camera' or 'Player' object found.");
                return;
            }

            var sleeve = Mat("PlayerSleeve", new Color(0.12f, 0.16f, 0.24f), 0.1f, 0.35f);
            var glove = Mat("PlayerGlove", new Color(0.13f, 0.13f, 0.15f), 0.2f, 0.4f);
            var accent = Mat("PlayerAccent", new Color(0.05f, 0.3f, 0.35f), 0f, 0.5f, new Color(0.1f, 1.2f, 1.5f));

            // Arms are children of the camera. Distances are in front of the lens; the view only reaches
            // about 0.3 m up/down at 0.5 m away, so the hands sit in the lower part of the picture.
            var camT = cameraObject.transform;
            var old = camT.Find("Arms");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            var arms = new GameObject("Arms");
            Undo.RegisterCreatedObjectUndo(arms, "Polish arms");
            arms.transform.SetParent(camT, false);

            var skin = Mat("PlayerSkin", new Color(0.80f, 0.60f, 0.47f), 0f, 0.35f);
            var watchStrap = Mat("PlayerWatchStrap", new Color(0.08f, 0.08f, 0.09f), 0.3f, 0.4f);
            var watchFace = Mat("PlayerWatchFace", new Color(0.02f, 0.2f, 0.25f), 0f, 0.5f, new Color(0.2f, 1.6f, 2.0f));
            var gloveDetail = Mat("PlayerGloveDetail", new Color(0.22f, 0.22f, 0.25f), 0.2f, 0.4f);

            foreach (float side in new[] { -1f, 1f })
            {
                string tag = side < 0f ? "L" : "R";
                // The arm reaches forward at a shallow angle (about 21 degrees up) and the hand continues straight
                // ahead, so wrist and hand line up instead of looking bent.
                var forearmEuler = new Vector3(68.7f, -12.8f * side, 0f);

                // Bare forearm in two pieces (thicker near the elbow, slimmer at the wrist), rising from the bottom edge.
                LocalPart(arms.transform, PrimitiveType.Cylinder, "ForeUpper" + tag,
                    new Vector3(0.299f * side, -0.3438f, 0.1431f), forearmEuler, new Vector3(0.088f, 0.1025f, 0.088f), skin);
                LocalPart(arms.transform, PrimitiveType.Cylinder, "ForeLower" + tag,
                    new Vector3(0.259f * side, -0.2737f, 0.3233f), forearmEuler, new Vector3(0.068f, 0.095f, 0.068f), skin);

                // A wristwatch on the left arm: strap and a small glowing face.
                if (side < 0f)
                {
                    LocalPart(arms.transform, PrimitiveType.Cylinder, "WatchStrap",
                        new Vector3(0.2481f * side, -0.2542f, 0.3735f), forearmEuler, new Vector3(0.074f, 0.012f, 0.074f), watchStrap);
                    LocalPart(arms.transform, PrimitiveType.Cube, "WatchFace",
                        new Vector3(0.248f * side, -0.2169f, 0.359f), forearmEuler, new Vector3(0.04f, 0.045f, 0.008f), watchFace);
                }

                // Hand: everything below is positioned in the hand's own axes (z forward, y up).
                var hand = new GameObject("Hand" + tag);
                Undo.RegisterCreatedObjectUndo(hand, "Polish hand");
                hand.transform.SetParent(arms.transform, false);
                hand.transform.localPosition = new Vector3(0.24f * side, -0.24f, 0.52f);
                hand.transform.localRotation = Quaternion.Euler(0f, -8f * side, 0f);
                var h = hand.transform;

                // The glove starts with a longer cuff over the wrist.
                LocalPart(h, PrimitiveType.Cylinder, "GloveCuff", new Vector3(0f, 0f, -0.06f), new Vector3(90f, 0f, 0f), new Vector3(0.088f, 0.045f, 0.088f), glove);
                LocalPart(h, PrimitiveType.Cylinder, "GloveCuffEdge", new Vector3(0f, 0f, -0.105f), new Vector3(90f, 0f, 0f), new Vector3(0.094f, 0.008f, 0.094f), gloveDetail);
                LocalPart(h, PrimitiveType.Cube, "Palm", new Vector3(0f, 0f, 0.02f), Vector3.zero, new Vector3(0.095f, 0.032f, 0.085f), glove);
                LocalPart(h, PrimitiveType.Cube, "KnucklePad", new Vector3(0f, 0.018f, 0.05f), Vector3.zero, new Vector3(0.085f, 0.012f, 0.025f), gloveDetail);

                // Four fingers, side by side, the index finger on the inner side and the little finger shortest.
                float[] length = { 0.075f, 0.085f, 0.08f, 0.065f };
                for (int i = 0; i < length.Length; i++)
                {
                    float x = (-0.034f + 0.023f * i) * side;
                    LocalPart(h, PrimitiveType.Capsule, "Finger" + i, new Vector3(x, -0.004f, 0.06f + length[i] * 0.5f),
                        new Vector3(90f, 0f, 0f), new Vector3(0.03f, length[i] * 0.5f, 0.03f), glove);
                }

                LocalPart(h, PrimitiveType.Capsule, "Thumb", new Vector3(-0.055f * side, -0.005f, 0.035f),
                    new Vector3(75f, 0f, 30f * side), new Vector3(0.033f, 0.032f, 0.033f), glove);
            }

            if (arms.GetComponent<PlayerArmsBob>() == null) arms.AddComponent<PlayerArmsBob>();

            // Shadow-only body: invisible to the player, but it casts a human-shaped shadow on the floor.
            var oldBody = player.transform.Find("BodyShadow");
            if (oldBody != null) Undo.DestroyObjectImmediate(oldBody.gameObject);

            var body = new GameObject("BodyShadow");
            Undo.RegisterCreatedObjectUndo(body, "Polish body shadow");
            body.transform.SetParent(player.transform, false);

            ShadowPart(body.transform, PrimitiveType.Sphere, "Head", new Vector3(0f, 0.68f, 0f), Vector3.zero, new Vector3(0.24f, 0.26f, 0.24f), sleeve);
            ShadowPart(body.transform, PrimitiveType.Cube, "Torso", new Vector3(0f, 0.22f, 0f), Vector3.zero, new Vector3(0.5f, 0.62f, 0.26f), sleeve);
            ShadowPart(body.transform, PrimitiveType.Cube, "Pelvis", new Vector3(0f, -0.2f, 0f), Vector3.zero, new Vector3(0.42f, 0.2f, 0.24f), sleeve);
            ShadowPart(body.transform, PrimitiveType.Cylinder, "LegL", new Vector3(-0.11f, -0.5f, 0f), Vector3.zero, new Vector3(0.17f, 0.4f, 0.17f), sleeve);
            ShadowPart(body.transform, PrimitiveType.Cylinder, "LegR", new Vector3(0.11f, -0.5f, 0f), Vector3.zero, new Vector3(0.17f, 0.4f, 0.17f), sleeve);
            ShadowPart(body.transform, PrimitiveType.Capsule, "ArmL", new Vector3(-0.33f, 0.15f, 0f), new Vector3(0f, 0f, 6f), new Vector3(0.13f, 0.32f, 0.13f), sleeve);
            ShadowPart(body.transform, PrimitiveType.Capsule, "ArmR", new Vector3(0.33f, 0.15f, 0f), new Vector3(0f, 0f, -6f), new Vector3(0.13f, 0.32f, 0.13f), sleeve);

            Finish("Player arms and body shadow added. Play and look down/around; walk to see the arm sway.");
        }

        // ------------------------------------------------------------------------------------------------
        // Step 5: details (wall patterns, door, cameras, laser panel, mainframe, NPC)
        // ------------------------------------------------------------------------------------------------
        sealed class ZoneStyle
        {
            public Material Panel, Inset, Accent, Dark, Metal;
            public bool Pipe;
        }

        static int signCounter;

        [MenuItem("Espionage/Polish/5 Details (walls, door, cameras, panel, mainframe)")]
        static void Details()
        {
            var root = FreshRoot(DetailsRoot);
            signCounter = 0;

            DecorateAllWalls(root);
            BuildDoor(root);
            BuildCameras(root);
            BuildLaserPanel();
            BuildMainframe(root);
            SetUpNpcFacing();

            Finish("Details built. Re-bake the NavMesh (the mainframe console has a collider), then play-test.");
        }

        static ZoneStyle Zone(string name, Color panel, Color accent, bool pipe)
        {
            return new ZoneStyle
            {
                Panel = Mat("WallPanel_" + name, panel, 0.1f, 0.35f),
                Inset = Mat("WallInset_" + name, panel * 0.78f, 0.1f, 0.3f),
                Accent = Mat("WallAccent_" + name, accent * 0.3f, 0f, 0.4f, accent * 1.6f),
                Dark = Mat("WallTrim", new Color(0.07f, 0.08f, 0.10f), 0.3f, 0.5f),
                Metal = Mat("Metal", new Color(0.33f, 0.38f, 0.45f), 0.7f, 0.5f),
                Pipe = pipe
            };
        }

        static void DecorateAllWalls(Transform root)
        {
            var r1 = Zone("R1", new Color(0.58f, 0.54f, 0.48f), Warm, false);
            var r2 = Zone("R2", new Color(0.42f, 0.49f, 0.58f), Cool, false);
            var cor = Zone("Corridor", new Color(0.52f, 0.35f, 0.35f), Red, true);
            var hall = Zone("Hall", new Color(0.33f, 0.37f, 0.43f), new Color(0.4f, 0.8f, 1f), true);
            var mf = Zone("Mainframe", new Color(0.26f, 0.45f, 0.48f), Teal, false);

            // Each call: zone, wall segment start (x, z), end (x, z), and the direction the wall face looks (x, z).
            Seg(root, r1, -3.9f, 0.2f, -3.9f, 7.8f, 1, 0);
            Seg(root, r1, 3.9f, 0.2f, 3.9f, 7.8f, -1, 0);
            Seg(root, r1, -3.8f, 0.1f, 3.8f, 0.1f, 0, 1);
            Seg(root, r1, -3.8f, 7.9f, -1.2f, 7.9f, 0, -1);
            Seg(root, r1, 1.2f, 7.9f, 3.8f, 7.9f, 0, -1);

            Seg(root, r2, -9.9f, 8.2f, -9.9f, 15.8f, 1, 0);
            Seg(root, r2, 9.9f, 8.2f, 9.9f, 15.8f, -1, 0);
            Seg(root, r2, -9.8f, 8.1f, -1.2f, 8.1f, 0, 1);
            Seg(root, r2, 1.2f, 8.1f, 9.8f, 8.1f, 0, 1);
            Seg(root, r2, -9.8f, 15.9f, -2.2f, 15.9f, 0, -1);
            Seg(root, r2, 2.2f, 15.9f, 9.8f, 15.9f, 0, -1);
            Seg(root, r2, -9.8f, 12.12f, 3.9f, 12.12f, 0, 1);
            Seg(root, r2, -9.8f, 11.88f, 3.9f, 11.88f, 0, -1);

            Seg(root, cor, -1.9f, 16.2f, -1.9f, 21.8f, 1, 0);
            Seg(root, cor, 1.9f, 18.2f, 1.9f, 21.8f, -1, 0);

            Seg(root, hall, -11.8f, 22.1f, -2.2f, 22.1f, 0, 1);
            Seg(root, hall, 2.2f, 22.1f, 5.8f, 22.1f, 0, 1);
            Seg(root, hall, -11.9f, 22.2f, -11.9f, 33.8f, 1, 0);
            Seg(root, hall, -11.8f, 33.9f, 5.8f, 33.9f, 0, -1);
            Seg(root, hall, 5.9f, 22.2f, 5.9f, 29.8f, -1, 0);

            Seg(root, hall, 6.2f, 30.1f, 17.8f, 30.1f, 0, 1);
            Seg(root, hall, 6.2f, 33.9f, 13.8f, 33.9f, 0, -1);
            Seg(root, hall, 17.9f, 30.2f, 17.9f, 37.8f, -1, 0);
            Seg(root, hall, 4.2f, 37.9f, 17.8f, 37.9f, 0, -1);
            Seg(root, hall, 6.2f, 34.1f, 13.8f, 34.1f, 0, 1);

            Seg(root, mf, -3.9f, 34.2f, -3.9f, 41.8f, 1, 0);
            Seg(root, mf, -3.8f, 41.9f, 3.8f, 41.9f, 0, -1);
            Seg(root, mf, 3.9f, 38.2f, 3.9f, 41.8f, -1, 0);
            Seg(root, mf, -3.8f, 34.1f, 3.8f, 34.1f, 0, 1);
        }

        static void Seg(Transform root, ZoneStyle z, float ax, float az, float bx, float bz, float nx, float nz)
        {
            var a = new Vector3(ax, 0f, az);
            var b = new Vector3(bx, 0f, bz);
            var n = new Vector3(nx, 0f, nz);
            float len = Vector3.Distance(a, b);
            if (len < 0.9f) return;

            Vector3 dir = (b - a) / len;
            Vector3 mid = (a + b) * 0.5f;
            Quaternion rot = Quaternion.LookRotation(n, Vector3.up);
            Vector3 up = Vector3.up;

            Shape(PrimitiveType.Cube, root, "Base", mid + n * 0.025f + up * 0.125f, rot, new Vector3(len, 0.25f, 0.05f), z.Dark, false, true, false);
            Shape(PrimitiveType.Cube, root, "Trim", mid + n * 0.03f + up * 2.92f, rot, new Vector3(len, 0.16f, 0.06f), z.Dark, false, true, false);
            Shape(PrimitiveType.Cube, root, "AccentLine", mid + n * 0.02f + up * 2.5f, rot, new Vector3(len, 0.035f, 0.03f), z.Accent, false, true, false);

            for (float s = 1.0f; s <= len - 1.0f; s += 2.0f)
            {
                Vector3 p = a + dir * s;
                Shape(PrimitiveType.Cube, root, "Panel", p + n * 0.015f + up * 0.95f, rot, new Vector3(1.5f, 1.2f, 0.03f), z.Panel, false, true, false);
                Shape(PrimitiveType.Cube, root, "PanelInset", p + n * 0.032f + up * 0.95f, rot, new Vector3(1.28f, 0.98f, 0.02f), z.Inset, false, true, false);
            }

            for (float s = 2.0f; s < len - 0.5f; s += 4.0f)
                Shape(PrimitiveType.Cube, root, "Pilaster", a + dir * s + n * 0.04f + up * 1.45f, rot, new Vector3(0.2f, 2.9f, 0.08f), z.Dark, false, true, false);

            if (z.Pipe)
                Shape(PrimitiveType.Cylinder, root, "Pipe", mid + n * 0.12f + up * 2.72f,
                    Quaternion.FromToRotation(Vector3.up, dir), new Vector3(0.1f, len * 0.5f, 0.1f), z.Metal, false, true, false);

            if (len >= 6f)
            {
                var signColors = new[]
                {
                    new Color(1f, 0.8f, 0.1f), new Color(0.2f, 1f, 0.4f), new Color(1f, 0.3f, 0.2f), new Color(0.3f, 0.9f, 1f)
                };
                Color sc = signColors[signCounter++ % signColors.Length];
                var signMat = Mat("Sign_" + ColorKey(sc), sc * 0.3f, 0f, 0.4f, sc * 1.4f);
                Shape(PrimitiveType.Cube, root, "SignFrame", mid + n * 0.04f + up * 1.85f, rot, new Vector3(0.62f, 0.47f, 0.03f), z.Dark, false, true, false);
                Shape(PrimitiveType.Cube, root, "Sign", mid + n * 0.062f + up * 1.85f, rot, new Vector3(0.5f, 0.35f, 0.02f), signMat, false, true, false);
            }
        }

        static void BuildDoor(Transform root)
        {
            var dark = Mat("WallTrim", new Color(0.07f, 0.08f, 0.10f), 0.3f, 0.5f);
            var steel = Mat("DoorSteel", new Color(0.22f, 0.24f, 0.28f), 0.8f, 0.5f);
            var darkSteel = Mat("DoorSteelDark", new Color(0.12f, 0.13f, 0.16f), 0.8f, 0.45f);
            var stripe = Mat("DoorStripe", new Color(0.8f, 0.65f, 0.05f), 0f, 0.4f, new Color(0.9f, 0.7f, 0.05f) * 0.8f);
            var ledRed = Mat("ReaderRed", new Color(0.4f, 0.02f, 0.02f), 0f, 0.5f, new Color(2.5f, 0.1f, 0.1f));
            var metal = Mat("Metal", new Color(0.33f, 0.38f, 0.45f), 0.7f, 0.5f);

            // Frame around the opening, on both sides of the wall.
            Box(root, "DoorPostL", new Vector3(-1.08f, 1.5f, 8f), new Vector3(0.16f, 3f, 0.36f), dark, false, true, true);
            Box(root, "DoorPostR", new Vector3(1.08f, 1.5f, 8f), new Vector3(0.16f, 3f, 0.36f), dark, false, true, true);
            Box(root, "DoorHeader", new Vector3(0f, 2.9f, 8f), new Vector3(2.32f, 0.2f, 0.36f), dark, false, true, true);
            for (int i = 0; i < 5; i++)
            {
                Box(root, $"DoorBandL_{i}", new Vector3(-1.08f, 0.35f + i * 0.5f, 8f), new Vector3(0.17f, 0.1f, 0.37f), stripe, false, true, false);
                Box(root, $"DoorBandR_{i}", new Vector3(1.08f, 0.35f + i * 0.5f, 8f), new Vector3(0.17f, 0.1f, 0.37f), stripe, false, true, false);
            }

            // Card readers beside the door (one on each side).
            Box(root, "ReaderS", new Vector3(1.45f, 1.25f, 7.86f), new Vector3(0.16f, 0.28f, 0.06f), dark, false, true, true);
            Box(root, "ReaderS_Led", new Vector3(1.45f, 1.33f, 7.82f), new Vector3(0.06f, 0.06f, 0.02f), ledRed, false, true, false);
            Box(root, "ReaderN", new Vector3(-1.45f, 1.25f, 8.14f), new Vector3(0.16f, 0.28f, 0.06f), dark, false, true, true);
            Box(root, "ReaderN_Led", new Vector3(-1.45f, 1.33f, 8.18f), new Vector3(0.06f, 0.06f, 0.02f), ledRed, false, true, false);

            var leaf = GameObject.Find("DoorLeaf");
            if (leaf == null) { Debug.LogWarning("Polish: no 'DoorLeaf' object found."); return; }

            if (leaf.TryGetComponent<MeshRenderer>(out var lr))
            {
                Undo.RecordObject(lr, "Polish door");
                lr.sharedMaterial = steel;
            }

            ClearParts(leaf.transform);
            var c = UnitContainer(leaf, PartPrefix + "Visual");
            foreach (float side in new[] { -1f, 1f })
            {
                Part(c, PrimitiveType.Cube, "Inset", new Vector3(0f, 0f, 0.113f * side), new Vector3(1.6f, 2.6f, 0.03f), darkSteel);
                Part(c, PrimitiveType.Cube, "StripeTop", new Vector3(0f, 0.95f, 0.13f * side), new Vector3(1.4f, 0.07f, 0.01f), stripe);
                Part(c, PrimitiveType.Cube, "StripeMid", new Vector3(0f, 0.8f, 0.13f * side), new Vector3(1.4f, 0.03f, 0.01f), stripe);
                Part(c, PrimitiveType.Cube, "StripeBottom", new Vector3(0f, -0.95f, 0.13f * side), new Vector3(1.4f, 0.07f, 0.01f), stripe);
                Part(c, PrimitiveType.Cube, "Handle", new Vector3(0.65f, 0f, 0.15f * side), new Vector3(0.06f, 0.55f, 0.06f), metal);
            }
        }

        static void BuildCameras(Transform root)
        {
            var body = Mat("CamBody", new Color(0.18f, 0.19f, 0.22f), 0.6f, 0.5f);
            var glass = Mat("CamGlass", new Color(0.02f, 0.03f, 0.05f), 0.9f, 0.9f);
            var metal = Mat("Metal", new Color(0.33f, 0.38f, 0.45f), 0.7f, 0.5f);

            var targets = new Dictionary<string, Vector3>
            {
                { "SecurityCamera_1", new Vector3(3f, 1f, 13f) },
                { "SecurityCamera_2", new Vector3(-3f, 1f, 11f) },
            };

            foreach (var pair in targets)
            {
                var cam = GameObject.Find(pair.Key);
                if (cam == null) { Debug.LogWarning($"Polish: no '{pair.Key}' object found."); continue; }

                // Face into the room (the rotation was never set after the wall moved).
                Undo.RecordObject(cam.transform, "Polish camera");
                cam.transform.rotation = Quaternion.LookRotation(pair.Value - cam.transform.position, Vector3.up);

                HideRootRenderer(cam);
                ClearParts(cam.transform);
                var c = UnitContainer(cam, PartPrefix + "Visual");
                Part(c, PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(0.2f, 0.17f, 0.42f), body);
                Part(c, PrimitiveType.Cylinder, "Lens", new Vector3(0f, 0f, 0.24f), new Vector3(0.13f, 0.04f, 0.13f), glass, new Vector3(90f, 0f, 0f));
                Part(c, PrimitiveType.Cylinder, "Hood", new Vector3(0f, 0f, 0.2f), new Vector3(0.16f, 0.03f, 0.16f), body, new Vector3(90f, 0f, 0f));

                // A vertical ceiling mount, in world space so it stays upright however the camera tilts.
                Vector3 p = cam.transform.position;
                Shape(PrimitiveType.Cylinder, root, $"CamRod_{pair.Key}", new Vector3(p.x, p.y + 0.12f, p.z), Quaternion.identity,
                    new Vector3(0.04f, 0.1f, 0.04f), metal, false, true, false);
                Shape(PrimitiveType.Cylinder, root, $"CamPlate_{pair.Key}", new Vector3(p.x, 2.99f, p.z), Quaternion.identity,
                    new Vector3(0.22f, 0.01f, 0.22f), metal, false, true, false);

                // The red light also casts a cone while the camera is active (it switches off with the camera).
                var al = cam.transform.Find("ActiveLight");
                if (al != null)
                {
                    var l = al.GetComponent<Light>();
                    if (l == null) l = Undo.AddComponent<Light>(al.gameObject);
                    l.type = LightType.Spot;
                    l.spotAngle = 55f;
                    l.range = 11f;
                    l.color = new Color(1f, 0.2f, 0.15f);
                    l.intensity = 6f;
                    l.shadows = LightShadows.None;
                }
            }
        }

        static void BuildLaserPanel()
        {
            var panel = GameObject.Find("LaserControlPanel");
            if (panel == null) { Debug.LogWarning("Polish: no 'LaserControlPanel' object found."); return; }

            var frame = Mat("PanelFrame", new Color(0.10f, 0.11f, 0.13f), 0.7f, 0.5f);
            var screen = Mat("ScreenCyan", new Color(0.02f, 0.2f, 0.25f), 0f, 0.6f, new Color(0.1f, 1.4f, 1.8f));
            var bar = Mat("ScreenBar", new Color(0.6f, 0.9f, 1f), 0f, 0.5f, new Color(1.2f, 2.0f, 2.4f));
            var btnRed = Mat("BtnRed", new Color(0.4f, 0.02f, 0.02f), 0f, 0.5f, new Color(2.5f, 0.1f, 0.1f));
            var btnYellow = Mat("BtnYellow", new Color(0.5f, 0.4f, 0.02f), 0f, 0.5f, new Color(2.5f, 1.8f, 0.1f));
            var btnGreen = Mat("BtnGreen", new Color(0.02f, 0.3f, 0.05f), 0f, 0.5f, new Color(0.1f, 2.2f, 0.4f));
            var stripe = Mat("DoorStripe", new Color(0.8f, 0.65f, 0.05f), 0f, 0.4f, new Color(0.9f, 0.7f, 0.05f) * 0.8f);
            var status = Mat("PanelStatus", new Color(0.3f, 0.05f, 0.03f), 0f, 0.5f, new Color(2.5f, 0.4f, 0.3f));

            ClearParts(panel.transform);
            var c = UnitContainer(panel, PartPrefix + "Visual");

            // The panel faces west (-x). Offsets are metres from the panel's centre.
            Part(c, PrimitiveType.Cube, "Frame", new Vector3(-0.115f, 0f, 0f), new Vector3(0.03f, 0.95f, 1.25f), frame);
            Part(c, PrimitiveType.Cube, "Screen", new Vector3(-0.135f, 0.12f, 0f), new Vector3(0.012f, 0.45f, 0.9f), screen);

            float[] widths = { 0.7f, 0.5f, 0.8f, 0.4f, 0.6f, 0.3f };
            for (int i = 0; i < widths.Length; i++)
                Part(c, PrimitiveType.Cube, "Bar" + i, new Vector3(-0.143f, 0.28f - i * 0.065f, -0.4f + widths[i] * 0.5f),
                    new Vector3(0.008f, 0.032f, widths[i]), bar);

            Material[] btn = { btnRed, btnYellow, btnGreen };
            for (int i = 0; i < 3; i++)
                Part(c, PrimitiveType.Cylinder, "Button" + i, new Vector3(-0.145f, -0.25f, -0.3f + i * 0.3f),
                    new Vector3(0.09f, 0.02f, 0.09f), btn[i], new Vector3(0f, 0f, 90f));

            var statusPart = Part(c, PrimitiveType.Cube, "StatusLight", new Vector3(-0.145f, 0.42f, 0f), new Vector3(0.02f, 0.07f, 0.55f), status);

            foreach (float sy in new[] { -0.43f, 0.43f })
                foreach (float sz in new[] { -0.55f, 0.55f })
                    Part(c, PrimitiveType.Cube, "Hazard", new Vector3(-0.135f, sy, sz), new Vector3(0.012f, 0.1f, 0.1f), stripe);

            var controlPanel = panel.GetComponent<LaserControlPanel>();
            if (controlPanel == null) controlPanel = Undo.AddComponent<LaserControlPanel>(panel);

            var barrier = Object.FindFirstObjectByType<LaserBarrier>();
            SetReference(controlPanel, "barrier", barrier);
            SetReference(controlPanel, "indicator", statusPart.GetComponent<Renderer>());
        }

        static void BuildMainframe(Transform root)
        {
            var mf = GameObject.Find("M_Mainframe");
            if (mf == null) { Debug.LogWarning("Polish: no 'M_Mainframe' object found."); return; }

            var body = Mat("MainframeBody", new Color(0.09f, 0.10f, 0.12f), 0.7f, 0.5f);
            var screenBg = Mat("MfScreen", new Color(0.02f, 0.18f, 0.2f), 0f, 0.6f, new Color(0.05f, 0.6f, 0.7f));
            var text = Mat("MfText", new Color(0.6f, 1f, 0.9f), 0f, 0.5f, new Color(0.8f, 2.4f, 2.0f));
            var greenScreen = Mat("ScreenGreen", new Color(0.02f, 0.25f, 0.05f), 0f, 0.6f, new Color(0.1f, 1.8f, 0.4f));
            var dark = Mat("Dark", new Color(0.07f, 0.08f, 0.10f), 0.2f, 0.6f);
            var metal = Mat("Metal", new Color(0.33f, 0.38f, 0.45f), 0.7f, 0.5f);
            var ledGreen = Mat("Led", new Color(0.02f, 0.25f, 0.05f), 0f, 0.5f, new Color(0.2f, 2.0f, 0.5f));
            var ledAmber = Mat("LedAmber", new Color(0.4f, 0.25f, 0.02f), 0f, 0.5f, new Color(2.4f, 1.4f, 0.1f));
            var ledRed = Mat("ReaderRed", new Color(0.4f, 0.02f, 0.02f), 0f, 0.5f, new Color(2.5f, 0.1f, 0.1f));
            var ledCyan = Mat("EyeCyan", new Color(0.05f, 0.3f, 0.35f), 0f, 0.5f, new Color(0.1f, 1.6f, 2.0f));

            if (mf.TryGetComponent<MeshRenderer>(out var mr))
            {
                Undo.RecordObject(mr, "Polish mainframe");
                mr.sharedMaterial = body;
            }

            ClearParts(mf.transform);
            var c = UnitContainer(mf, PartPrefix + "Visual");

            // The front faces south (-z). Offsets are metres from the centre (the box is 3 x 2.4 x 0.8).
            const float F = -0.41f;
            Part(c, PrimitiveType.Cube, "ScreenBg", new Vector3(0f, 0.5f, F - 0.01f), new Vector3(1.8f, 0.95f, 0.02f), screenBg);
            float[] widths = { 1.5f, 1.0f, 1.6f, 0.7f, 1.3f, 0.9f, 1.55f, 0.6f, 1.2f };
            for (int i = 0; i < widths.Length; i++)
                Part(c, PrimitiveType.Cube, "Line" + i, new Vector3(-0.85f + widths[i] * 0.5f, 0.84f - i * 0.085f, F - 0.025f),
                    new Vector3(widths[i], 0.035f, 0.01f), text);

            foreach (float sx in new[] { -1.12f, 1.12f })
            {
                Part(c, PrimitiveType.Cube, "SideScreen", new Vector3(sx, 0.55f, F - 0.01f), new Vector3(0.4f, 0.8f, 0.02f), greenScreen);
                for (int i = 0; i < 4; i++)
                    Part(c, PrimitiveType.Cube, "SideLine" + i, new Vector3(sx, 0.85f - i * 0.17f, F - 0.025f), new Vector3(0.3f, 0.03f, 0.01f), text);
            }

            Part(c, PrimitiveType.Cube, "ControlStrip", new Vector3(0f, -0.15f, F - 0.01f), new Vector3(2.6f, 0.34f, 0.03f), dark);
            Material[] cols = { ledRed, ledAmber, ledGreen, ledCyan };
            for (int i = 0; i < 12; i++)
                Part(c, PrimitiveType.Cube, "Button" + i, new Vector3(-1.1f + i * 0.2f, -0.15f, F - 0.035f), new Vector3(0.1f, 0.07f, 0.02f), cols[i % cols.Length]);

            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 16; i++)
                    Part(c, PrimitiveType.Cube, $"Led{row}_{i}", new Vector3(-1.2f + i * 0.16f, -0.43f - row * 0.09f, F - 0.02f),
                        new Vector3(0.05f, 0.05f, 0.02f), (i * 7 + row * 3) % 5 == 0 ? ledAmber : ledGreen);

            for (int i = 0; i < 7; i++)
                Part(c, PrimitiveType.Cube, "Vent" + i, new Vector3(0f, -0.72f - i * 0.07f, F - 0.015f), new Vector3(2.4f, 0.03f, 0.02f), metal);

            Part(c, PrimitiveType.Cube, "Title", new Vector3(0f, 1.05f, F - 0.015f), new Vector3(1.4f, 0.12f, 0.02f), ledRed);

            foreach (float sx in new[] { -1.38f, 1.38f })
                Part(c, PrimitiveType.Cube, "Tower" + (sx < 0 ? "L" : "R"), new Vector3(sx, 0.1f, F - 0.015f), new Vector3(0.05f, 1.9f, 0.02f), ledCyan);

            // A console desk in front of it, with two monitors and a keyboard.
            Box(root, "MainframeConsole", new Vector3(0f, 0.5f, 39.8f), new Vector3(2.4f, 1.0f, 0.55f), body, true, false, true);
            Box(root, "ConsoleTop", new Vector3(0f, 1.02f, 39.8f), new Vector3(2.5f, 0.05f, 0.62f), metal, false, false, true);
            Box(root, "Keyboard", new Vector3(0f, 1.06f, 39.65f), new Vector3(0.9f, 0.03f, 0.28f), dark, false, false, true);
            Box(root, "MonitorL", new Vector3(-0.75f, 1.35f, 39.95f), new Vector3(0.7f, 0.45f, 0.05f), dark, false, false, true);
            Box(root, "MonitorL_Screen", new Vector3(-0.75f, 1.35f, 39.92f), new Vector3(0.62f, 0.37f, 0.02f), greenScreen, false, false, false);
            Box(root, "MonitorR", new Vector3(0.75f, 1.35f, 39.95f), new Vector3(0.7f, 0.45f, 0.05f), dark, false, false, true);
            Box(root, "MonitorR_Screen", new Vector3(0.75f, 1.35f, 39.92f), new Vector3(0.62f, 0.37f, 0.02f), screenBg, false, false, false);
        }

        static void SetUpNpcFacing()
        {
            var npc = GameObject.Find("NPC_Placeholder");
            var player = GameObject.Find("Player");
            if (npc == null || player == null) { Debug.LogWarning("Polish: NPC or Player not found; the NPC will not face the player."); return; }

            var face = npc.GetComponent<FaceTarget>();
            if (face == null) face = Undo.AddComponent<FaceTarget>(npc);
            SetReference(face, "target", player.transform);
        }

        // ------------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------------
        static void SetReference(Object component, string field, Object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"Polish: field '{field}' not found on {component.GetType().Name}."); return; }

            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Polish");
        }

        static Material Mat(string name, Color color, float metallic = 0f, float smoothness = 0.3f, Color? emission = null)
        {
            EnsureFolders();
            string path = $"{MaterialFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new System.InvalidOperationException("URP Lit shader not found.");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }

            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(m);
            return m;
        }

        static Transform FreshRoot(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Polish root");
            return go.transform;
        }

        /// <summary>A world-space primitive (parent must be at the origin with scale 1).</summary>
        static GameObject Shape(PrimitiveType type, Transform parent, string name, Vector3 center, Quaternion rotation,
            Vector3 size, Material mat, bool collider, bool ignoreNav, bool shadows)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(center, rotation);
            go.transform.localScale = size;

            var col = go.GetComponent<Collider>();
            if (!collider && col != null) Object.DestroyImmediate(col);

            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;

            if (ignoreNav)
            {
                var mod = go.AddComponent<NavMeshModifier>();
                mod.ignoreFromBuild = true;
            }

            Undo.RegisterCreatedObjectUndo(go, "Polish shape");
            return go;
        }

        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat,
            bool collider, bool ignoreNav, bool shadows)
            => Shape(PrimitiveType.Cube, parent, name, center, Quaternion.identity, size, mat, collider, ignoreNav, shadows);

        static void Lamp(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject("Light_" + name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            Undo.RegisterCreatedObjectUndo(go, "Polish light");

            var strip = Mat("Strip_" + ColorKey(color), color * 0.3f, 0f, 0.4f, color * 2f);
            Box(parent, "Strip_" + name, new Vector3(pos.x, 2.97f, pos.z), new Vector3(2f, 0.04f, 0.3f), strip,
                collider: false, ignoreNav: true, shadows: false);
        }

        static string ColorKey(Color c) => $"{Mathf.RoundToInt(c.r * 10)}{Mathf.RoundToInt(c.g * 10)}{Mathf.RoundToInt(c.b * 10)}";

        /// <summary>Starts a fresh "Visual" child holding a robot's parts; the old one is removed.</summary>
        static Transform BeginVisual(GameObject owner)
        {
            var old = owner.transform.Find(VisualName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            var go = new GameObject(VisualName);
            Undo.RegisterCreatedObjectUndo(go, "Polish visual");
            go.transform.SetParent(owner.transform, false);
            return go.transform;
        }

        /// <summary>
        /// A child whose own scale cancels the owner's, so parts inside it are sized in plain metres and can be
        /// rotated without being skewed by a stretched owner.
        /// </summary>
        static Transform UnitContainer(GameObject owner, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Polish container");
            go.transform.SetParent(owner.transform, false);
            Vector3 s = owner.transform.lossyScale;
            go.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
            return go.transform;
        }

        /// <summary>
        /// Adds a primitive under <paramref name="parent"/>. Offset and size are in metres in the parent's own axes,
        /// whatever the parent's scale (use a <see cref="UnitContainer"/> when rotating). The primitive gets no collider.
        /// </summary>
        static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 offset, Vector3 size, Material mat,
            Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            go.transform.SetParent(parent, false);
            Vector3 s = parent.lossyScale;
            go.transform.localPosition = new Vector3(offset.x / s.x, offset.y / s.y, offset.z / s.z);
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = new Vector3(size.x / s.x, size.y / s.y, size.z / s.z);
            go.GetComponent<Renderer>().sharedMaterial = mat;

            Undo.RegisterCreatedObjectUndo(go, "Polish part");
            return go;
        }

        /// <summary>A primitive with plain local position, rotation and scale (parent assumed to have scale 1), no collider.</summary>
        static GameObject LocalPart(Transform parent, PrimitiveType type, string name, Vector3 localPosition,
            Vector3 localEuler, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = localScale;

            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;

            Undo.RegisterCreatedObjectUndo(go, "Polish arm part");
            return go;
        }

        static GameObject ShadowPart(Transform parent, PrimitiveType type, string name, Vector3 localPosition,
            Vector3 localEuler, Vector3 localScale, Material mat)
        {
            var go = LocalPart(parent, type, name, localPosition, localEuler, localScale, mat);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            return go;
        }

        static void ClearParts(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i);
                if (child.name.StartsWith(PartPrefix)) Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        static void HideRootRenderer(GameObject go)
        {
            if (go.TryGetComponent<MeshRenderer>(out var r))
            {
                Undo.RecordObject(r, "Polish hide capsule");
                r.enabled = false;
            }
        }

        static void Finish(string message)
        {
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Polish: " + message);
        }
    }
}
