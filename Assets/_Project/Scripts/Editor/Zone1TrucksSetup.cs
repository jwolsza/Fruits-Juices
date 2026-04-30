using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project.Core;
using Project.Data;
using Project.Zone1.FruitWall;
using Project.Zone1.Trucks;

namespace Project.Editor
{
    public static class Zone1TrucksSetup
    {
        const string TruckPrefabPath = "Assets/_Project/Prefabs/TruckPrefab.prefab";
        const string TruckBoxMaterialPath = "Assets/_Project/Materials/TruckBoxMaterial.mat";
        const string ConveyorMaterialPath = "Assets/_Project/Materials/ConveyorMaterial.mat";
        const string GameBalancePath = "Assets/_Project/Settings/GameBalance.asset";
        const string OnRefillingChangedPath = "Assets/_Project/Settings/Events/OnRefillingChanged.asset";

        [MenuItem("Tools/Project/Setup Zone1 Trucks")]
        public static void Setup()
        {
            // 1. Verify required assets
            var balance = AssetDatabase.LoadAssetAtPath<GameBalanceSO>(GameBalancePath);
            if (balance == null) { ErrorAndExit($"Missing {GameBalancePath}"); return; }

            var refillEvent = AssetDatabase.LoadAssetAtPath<BoolEventChannelSO>(OnRefillingChangedPath);
            if (refillEvent == null) { ErrorAndExit($"Missing {OnRefillingChangedPath}"); return; }

            var firstZone = GameObject.Find("FirstZone");
            if (firstZone == null) { ErrorAndExit("FirstZone GameObject not found in active scene"); return; }

            var zone1Manager = firstZone.GetComponentInChildren<Zone1Manager>();
            if (zone1Manager == null) { ErrorAndExit("Zone1Manager not found under FirstZone"); return; }

            var mainCamera = Camera.main;
            if (mainCamera == null) { ErrorAndExit("Main Camera not found"); return; }

            // 2. Materials (idempotent)
            var truckBoxMat = LoadOrCreateMaterial(TruckBoxMaterialPath, "Universal Render Pipeline/Lit", Color.white);
            var conveyorMat = LoadOrCreateMaterial(ConveyorMaterialPath, "Universal Render Pipeline/Unlit", new Color(0.4f, 0.4f, 0.45f));

            // 3. TruckPrefab (idempotent: skip if exists)
            var truckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TruckPrefabPath);
            if (truckPrefab == null)
            {
                truckPrefab = CreateTruckPrefab(truckBoxMat);
            }

            // 4. [Zone1Trucks] (idempotent: destroy existing, recreate)
            var existing = firstZone.transform.Find("[Zone1Trucks]");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var zone1Trucks = new GameObject("[Zone1Trucks]");
            zone1Trucks.transform.SetParent(firstZone.transform, false);
            zone1Trucks.transform.localPosition = Vector3.zero;
            var manager = zone1Trucks.AddComponent<Zone1TrucksManager>();

            // 4a. Conveyor (LineRenderer + ConveyorView)
            var conveyor = new GameObject("Conveyor");
            conveyor.transform.SetParent(zone1Trucks.transform, false);
            conveyor.transform.localPosition = Vector3.zero;
            var lineRenderer = conveyor.AddComponent<LineRenderer>();
            lineRenderer.material = conveyorMat;
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            var conveyorView = conveyor.AddComponent<ConveyorView>();

            // 4b. Garage (GarageView with parking slots)
            var garage = new GameObject("Garage");
            garage.transform.SetParent(zone1Trucks.transform, false);
            garage.transform.localPosition = new Vector3(-2.5f, 0f, 0.2f);
            var garageView = garage.AddComponent<GarageView>();

            ConfigureGarageView(garageView);
            ConfigureZone1TrucksManager(manager, balance, zone1Manager, refillEvent,
                conveyorView, garageView, truckPrefab, mainCamera);

            // 5. Mark scene dirty + save
            EditorUtility.SetDirty(firstZone);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(firstZone.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(firstZone.scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Setup Zone1 Trucks",
                "Done.\n\n" +
                "Created/updated:\n" +
                $"- TruckPrefab: {TruckPrefabPath}\n" +
                "- [Zone1Trucks] GameObject under FirstZone\n" +
                "- Conveyor (LineRenderer + ConveyorView)\n" +
                "- Garage (GarageView with 3 parking slots)\n" +
                "- Zone1TrucksManager fully configured\n\n" +
                "Press Play to test.", "OK");
        }

        // Indexes of LineRenderer points that should be active slots.
        // Last one in this list is the "stop slot" (truck pauses here to collect).
        static readonly int[] ActiveSlotPointIndexes = { 4, 5, 6 };

        [MenuItem("Tools/Project/Sync Conveyor Waypoints From LineRenderer")]
        public static void SyncWaypointsFromLineRenderer()
        {
            var firstZone = GameObject.Find("FirstZone");
            if (firstZone == null) { ErrorAndExit("FirstZone not found"); return; }

            var zone1Trucks = firstZone.transform.Find("[Zone1Trucks]");
            if (zone1Trucks == null) { ErrorAndExit("[Zone1Trucks] not found"); return; }

            var manager = zone1Trucks.GetComponent<Zone1TrucksManager>();
            if (manager == null) { ErrorAndExit("Zone1TrucksManager missing"); return; }

            var conveyor = zone1Trucks.Find("Conveyor");
            if (conveyor == null) { ErrorAndExit("Conveyor GameObject missing"); return; }

            var lr = conveyor.GetComponent<LineRenderer>();
            if (lr == null) { ErrorAndExit("LineRenderer missing on Conveyor"); return; }

            int count = lr.positionCount;
            if (count < 2) { ErrorAndExit($"LineRenderer has only {count} points; need at least 2"); return; }

            var positions = new Vector3[count];
            lr.GetPositions(positions);

            if (!lr.useWorldSpace)
            {
                for (int i = 0; i < count; i++)
                    positions[i] = conveyor.TransformPoint(positions[i]);
            }

            var so = new SerializedObject(manager);

            // Conveyor waypoints
            var waypointsProp = so.FindProperty("conveyorWaypoints");
            waypointsProp.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                bool isActive = false;
                int slotIdx = -1;
                for (int s = 0; s < ActiveSlotPointIndexes.Length; s++)
                {
                    if (ActiveSlotPointIndexes[s] == i)
                    {
                        isActive = true;
                        slotIdx = s;
                        break;
                    }
                }

                var elem = waypointsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("Position").vector3Value = positions[i];
                elem.FindPropertyRelative("IsActiveSlot").boolValue = isActive;
                elem.FindPropertyRelative("SlotIndex").intValue = slotIdx;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

            EditorUtility.DisplayDialog("Sync Conveyor Waypoints",
                $"Synced {count} waypoints from LineRenderer.\n" +
                $"Active slots set at LineRenderer indexes [{string.Join(", ", ActiveSlotPointIndexes)}].\n" +
                $"Wall slots updated to {ActiveSlotPointIndexes.Length} positions; " +
                $"last one (idx {ActiveSlotPointIndexes[^1]}) marked IsStopSlot=true.\n\n" +
                "Click Save Scene (Cmd+S) when satisfied.", "OK");
        }

        static GameObject CreateTruckPrefab(Material boxMat)
        {
            var root = new GameObject("TruckPrefab");

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Box";
            box.transform.SetParent(root.transform, false);
            box.transform.localScale = new Vector3(1.0f, 0.4f, 0.6f);
            box.GetComponent<MeshRenderer>().sharedMaterial = boxMat;

            var cab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cab.name = "Cab";
            cab.transform.SetParent(root.transform, false);
            cab.transform.localPosition = new Vector3(0f, 0.05f, 0.5f);
            cab.transform.localScale = new Vector3(0.5f, 0.3f, 0.4f);
            cab.GetComponent<MeshRenderer>().sharedMaterial = boxMat;

            CreateWheel(root.transform, "Wheel_FL", new Vector3(-0.4f, -0.2f, 0.25f));
            CreateWheel(root.transform, "Wheel_FR", new Vector3(0.4f, -0.2f, 0.25f));
            CreateWheel(root.transform, "Wheel_BL", new Vector3(-0.4f, -0.2f, -0.25f));
            CreateWheel(root.transform, "Wheel_BR", new Vector3(0.4f, -0.2f, -0.25f));

            var view = root.AddComponent<TruckView>();
            var soView = new SerializedObject(view);
            soView.FindProperty("boxRenderer").objectReferenceValue = box.GetComponent<MeshRenderer>();
            soView.ApplyModifiedPropertiesWithoutUndo();

            // Ensure prefabs folder exists
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, TruckPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void CreateWheel(Transform parent, string name, Vector3 pos)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = name;
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = pos;
            wheel.transform.localScale = new Vector3(0.18f, 0.05f, 0.18f);
        }

        static void ConfigureGarageView(GarageView garageView)
        {
            var so = new SerializedObject(garageView);
            var slotsProp = so.FindProperty("parkingSlots");
            slotsProp.arraySize = 3;
            slotsProp.GetArrayElementAtIndex(0).vector3Value = new Vector3(0f, 0f, 0f);
            slotsProp.GetArrayElementAtIndex(1).vector3Value = new Vector3(-0.7f, 0f, 0.6f);
            slotsProp.GetArrayElementAtIndex(2).vector3Value = new Vector3(0f, 0f, 0.6f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureZone1TrucksManager(
            Zone1TrucksManager manager,
            GameBalanceSO balance,
            Zone1Manager zone1Manager,
            BoolEventChannelSO refillEvent,
            ConveyorView conveyorView,
            GarageView garageView,
            GameObject truckPrefab,
            Camera mainCamera)
        {
            var so = new SerializedObject(manager);

            so.FindProperty("balance").objectReferenceValue = balance;
            so.FindProperty("zone1Manager").objectReferenceValue = zone1Manager;
            so.FindProperty("conveyorView").objectReferenceValue = conveyorView;
            so.FindProperty("truckSpeedUnitsPerSec").floatValue = 1.5f;
            so.FindProperty("garageView").objectReferenceValue = garageView;
            so.FindProperty("truckViewPrefab").objectReferenceValue = truckPrefab;
            so.FindProperty("mainCamera").objectReferenceValue = mainCamera;

            // Conveyor waypoints (7)
            var waypointsProp = so.FindProperty("conveyorWaypoints");
            var waypoints = new (Vector3 pos, bool active, int slot)[]
            {
                (new Vector3(-1.5f, 0f, -0.5f), false, -1),
                (new Vector3(4f, 0f, -0.5f),    true,   0),
                (new Vector3(8f, 0f, -0.5f),    true,   1),
                (new Vector3(12f, 0f, -0.5f),   true,   2),
                (new Vector3(17f, 0f, -0.5f),   false, -1),
                (new Vector3(17f, 0f, 0.8f),    false, -1),
                (new Vector3(-1.5f, 0f, 0.8f),  false, -1),
            };
            waypointsProp.arraySize = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
            {
                var elem = waypointsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("Position").vector3Value = waypoints[i].pos;
                elem.FindPropertyRelative("IsActiveSlot").boolValue = waypoints[i].active;
                elem.FindPropertyRelative("SlotIndex").intValue = waypoints[i].slot;
            }


            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material LoadOrCreateMaterial(string path, string shaderName, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");

            var shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void ErrorAndExit(string message)
        {
            Debug.LogError($"[Zone1TrucksSetup] {message}");
            EditorUtility.DisplayDialog("Setup Zone1 Trucks failed", message, "OK");
        }
    }
}
