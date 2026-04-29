using UnityEditor;
using UnityEngine;
using Project.Data;
using Project.Zone1.Trucks;
using Project.Zone2.Bottling;

namespace Project.Editor
{
    public static class Zone2BottlingSetup
    {
        const string BigBottlePrefabPath = "Assets/_Project/Prefabs/BigBottlePrefab.prefab";
        const string SmallBottlePrefabPath = "Assets/_Project/Prefabs/SmallBottlePrefab.prefab";
        const string JuiceMaterialPath = "Assets/_Project/Materials/JuiceMaterial.mat";
        const string GlassMaterialPath = "Assets/_Project/Materials/GlassBottleMaterial.mat";
        const string SmallBottleMaterialPath = "Assets/_Project/Materials/SmallBottleMaterial.mat";
        const string GameBalancePath = "Assets/_Project/Settings/GameBalance.asset";

        [MenuItem("Tools/Project/Setup Zone2 Bottling")]
        public static void Setup()
        {
            var balance = AssetDatabase.LoadAssetAtPath<GameBalanceSO>(GameBalancePath);
            if (balance == null) { ErrorAndExit($"Missing {GameBalancePath}"); return; }

            var secondZone = GameObject.Find("SecondZone");
            if (secondZone == null) { ErrorAndExit("SecondZone GameObject not found"); return; }

            var firstZone = GameObject.Find("FirstZone");
            if (firstZone == null) { ErrorAndExit("FirstZone not found"); return; }

            var zone1Trucks = firstZone.transform.Find("[Zone1Trucks]");
            if (zone1Trucks == null) { ErrorAndExit("[Zone1Trucks] not found under FirstZone"); return; }

            var zone1TrucksManager = zone1Trucks.GetComponent<Zone1TrucksManager>();
            if (zone1TrucksManager == null) { ErrorAndExit("Zone1TrucksManager component missing on [Zone1Trucks]"); return; }

            var mainCamera = Camera.main;
            if (mainCamera == null) { ErrorAndExit("Main Camera not found"); return; }

            // Materials
            var juiceMat = LoadOrCreateMaterial(JuiceMaterialPath, "Universal Render Pipeline/Lit", Color.white);
            var glassMat = LoadOrCreateMaterial(GlassMaterialPath, "Universal Render Pipeline/Lit", new Color(0.85f, 0.95f, 1f, 0.5f));
            var smallBottleMat = LoadOrCreateMaterial(SmallBottleMaterialPath, "Universal Render Pipeline/Lit", Color.white);

            // Prefabs
            var bigBottlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BigBottlePrefabPath);
            if (bigBottlePrefab == null) bigBottlePrefab = CreateBigBottlePrefab(juiceMat, glassMat);

            var smallBottlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmallBottlePrefabPath);
            if (smallBottlePrefab == null) smallBottlePrefab = CreateSmallBottlePrefab(smallBottleMat);

            // Tear down existing [Zone2Manager] under SecondZone (idempotent)
            var existingMgr = secondZone.transform.Find("[Zone2Manager]");
            if (existingMgr != null) Object.DestroyImmediate(existingMgr.gameObject);
            var existingBottles = secondZone.transform.Find("Bottles");
            if (existingBottles != null) Object.DestroyImmediate(existingBottles.gameObject);
            var existingRacks = secondZone.transform.Find("Racks");
            if (existingRacks != null) Object.DestroyImmediate(existingRacks.gameObject);

            // Bottles container
            var bottlesRoot = new GameObject("Bottles");
            bottlesRoot.transform.SetParent(secondZone.transform, false);
            var bottleViews = new BigBottleView[3];
            for (int i = 0; i < 3; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(bigBottlePrefab, bottlesRoot.transform);
                go.name = $"BigBottle{i}";
                go.transform.localPosition = new Vector3((i - 1) * 0.8f, 0f, 0f);
                bottleViews[i] = go.GetComponent<BigBottleView>();
            }

            // Racks container
            var racksRoot = new GameObject("Racks");
            racksRoot.transform.SetParent(secondZone.transform, false);
            var rackViews = new SmallBottleRackView[3];
            for (int i = 0; i < 3; i++)
            {
                var rackGo = new GameObject($"Rack{i}");
                rackGo.transform.SetParent(racksRoot.transform, false);
                rackGo.transform.localPosition = new Vector3((i - 1) * 0.8f, 0f, 0.7f); // slightly forward toward zone3
                var view = rackGo.AddComponent<SmallBottleRackView>();
                ConfigureRackView(view, smallBottlePrefab, balance.RackCapacity);
                rackViews[i] = view;
            }

            // [Zone2Manager]
            var managerGo = new GameObject("[Zone2Manager]");
            managerGo.transform.SetParent(secondZone.transform, false);
            managerGo.transform.localPosition = Vector3.zero;
            var zone2Manager = managerGo.AddComponent<Zone2Manager>();
            ConfigureZone2Manager(zone2Manager, balance, bottleViews, rackViews, mainCamera);

            // Wire Zone1TrucksManager.zone2Manager
            var z1so = new SerializedObject(zone1TrucksManager);
            z1so.FindProperty("zone2Manager").objectReferenceValue = zone2Manager;
            z1so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone1TrucksManager);

            EditorUtility.SetDirty(secondZone);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(secondZone.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(secondZone.scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Setup Zone2 Bottling",
                "Done.\n\nCreated:\n" +
                "- BigBottlePrefab + SmallBottlePrefab + 3 materials\n" +
                "- 3 BigBottle children of SecondZone (X = -0.8, 0, +0.8)\n" +
                "- 3 Rack children of SecondZone (Z forward 0.7)\n" +
                "- [Zone2Manager] GameObject with all references\n" +
                "- Zone1TrucksManager.zone2Manager wired\n\n" +
                "Press Play to test the truck → bottle → rack flow.", "OK");
        }

        static GameObject CreateBigBottlePrefab(Material juiceMat, Material glassMat)
        {
            EnsurePrefabFolder();
            var root = new GameObject("BigBottlePrefab");

            // Body (cylinder)
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            body.transform.localPosition = new Vector3(0, 0.6f, 0);
            body.GetComponent<MeshRenderer>().sharedMaterial = glassMat;

            // Juice pivot + inner cylinder (scale.y animated)
            var pivot = new GameObject("JuicePivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0, 0f, 0);
            pivot.transform.localScale = new Vector3(1f, 0f, 1f); // start empty

            var juice = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            juice.name = "Juice";
            juice.transform.SetParent(pivot.transform, false);
            juice.transform.localScale = new Vector3(0.45f, 0.6f, 0.45f);
            juice.transform.localPosition = new Vector3(0, 0.6f, 0);
            // Remove collider on juice (only body needs collider for tap)
            var juiceCollider = juice.GetComponent<Collider>();
            if (juiceCollider != null) Object.DestroyImmediate(juiceCollider);
            var juiceMr = juice.GetComponent<MeshRenderer>();
            juiceMr.sharedMaterial = juiceMat;

            // BigBottleView
            var view = root.AddComponent<BigBottleView>();
            var so = new SerializedObject(view);
            so.FindProperty("juiceFillPivot").objectReferenceValue = pivot.transform;
            so.FindProperty("juiceRenderer").objectReferenceValue = juiceMr;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BigBottlePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject CreateSmallBottlePrefab(Material smallBottleMat)
        {
            EnsurePrefabFolder();
            var root = new GameObject("SmallBottlePrefab");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.08f, 0.1f, 0.08f);
            body.GetComponent<MeshRenderer>().sharedMaterial = smallBottleMat;
            var col = body.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, SmallBottlePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void ConfigureRackView(SmallBottleRackView view, GameObject smallBottlePrefab, int capacity)
        {
            var so = new SerializedObject(view);
            so.FindProperty("smallBottleTemplate").objectReferenceValue = smallBottlePrefab;
            so.FindProperty("firstSlotOffset").vector3Value = new Vector3(-0.3f, 0f, 0f);
            so.FindProperty("stepX").vector3Value = new Vector3(0.12f, 0f, 0f);
            so.FindProperty("stepY").vector3Value = new Vector3(0f, 0f, 0.12f);
            so.FindProperty("columns").intValue = 5;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureZone2Manager(
            Zone2Manager manager,
            GameBalanceSO balance,
            BigBottleView[] bottleViews,
            SmallBottleRackView[] rackViews,
            Camera mainCamera)
        {
            var so = new SerializedObject(manager);
            so.FindProperty("balance").objectReferenceValue = balance;
            so.FindProperty("mainCamera").objectReferenceValue = mainCamera;

            var bottleViewsProp = so.FindProperty("bottleViews");
            bottleViewsProp.arraySize = bottleViews.Length;
            for (int i = 0; i < bottleViews.Length; i++)
                bottleViewsProp.GetArrayElementAtIndex(i).objectReferenceValue = bottleViews[i];

            var rackViewsProp = so.FindProperty("rackViews");
            rackViewsProp.arraySize = rackViews.Length;
            for (int i = 0; i < rackViews.Length; i++)
                rackViewsProp.GetArrayElementAtIndex(i).objectReferenceValue = rackViews[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material LoadOrCreateMaterial(string path, string shaderName, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            EnsureMaterialsFolder();
            var shader = Shader.Find(shaderName) ?? Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void EnsurePrefabFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        }

        static void EnsureMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
        }

        static void ErrorAndExit(string message)
        {
            Debug.LogError($"[Zone2BottlingSetup] {message}");
            EditorUtility.DisplayDialog("Setup Zone2 Bottling failed", message, "OK");
        }
    }
}
