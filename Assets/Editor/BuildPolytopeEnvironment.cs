using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BuildPolytopeEnvironment : EditorWindow
{
    private const string BasePath = "Assets/Scenes/Polytope Studio/";
    private const string OutputScenePath = "Assets/Scenes/PolytopeGameEnvironment.unity";

    [MenuItem("Tools/Polytope Studio/Fix Skybox & River Water")]
    public static void FixSkyboxAndWater()
    {
        Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(BasePath + "Lowpoly_Environments/Sources/Materials/PT_Skybox_mat.mat");
        if (skyboxMat != null)
        {
            Shader skyboxShader = Shader.Find("Skybox/Cubemap");
            if (skyboxShader != null)
            {
                skyboxMat.shader = skyboxShader;
                Texture skyTex = AssetDatabase.LoadAssetAtPath<Texture>(BasePath + "Lowpoly_Environments/Sources/Textures/PT_Skybox_Texture_01.png");
                if (skyTex != null && skyboxMat.HasProperty("_Tex"))
                {
                    skyboxMat.SetTexture("_Tex", skyTex);
                }
                EditorUtility.SetDirty(skyboxMat);
            }
            RenderSettings.skybox = skyboxMat;
        }

        Material waterMat = AssetDatabase.LoadAssetAtPath<Material>(BasePath + "Lowpoly_Environments/Sources/Materials/PT_Water_mat.mat");
        if (waterMat != null)
        {
            Shader waterUrpShader = Shader.Find("Polytope Studio/PT_Water_URP");
            if (waterUrpShader != null)
            {
                waterMat.shader = waterUrpShader;
                Texture waterNm = AssetDatabase.LoadAssetAtPath<Texture>(BasePath + "Lowpoly_Environments/Sources/Textures/PT_Water_NM_01.png");
                if (waterNm != null && waterMat.HasProperty("_NormalMapTexture"))
                {
                    waterMat.SetTexture("_NormalMapTexture", waterNm);
                }
                EditorUtility.SetDirty(waterMat);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Fixed Polytope Studio Skybox (Clouds) and River Water (URP) materials.");
    }

    [MenuItem("Tools/Polytope Studio/Fix Pink Materials (Convert to URP)")]
    public static void ConvertPolytopeMaterialsToURP()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("URP Lit shader not found.");
            return;
        }

        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Scenes/Polytope Studio" });
        int converted = 0;

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string nameLower = mat.name.ToLowerInvariant();
            if (nameLower.Contains("skybox") || nameLower.Contains("water"))
            {
                continue;
            }

            if (mat.shader == null || mat.shader != urpLit)
            {
                mat.shader = urpLit;
            }

            string texPath = null;
            bool alphaClip = false;

            Color tint = Color.white;
            if (nameLower.Contains("trunk") || nameLower.Contains("opaque")) { texPath = BasePath + "Lowpoly_Environments/Sources/Textures/PT_Tree_Trunk_01.png"; tint = new Color(0.45f, 0.30f, 0.18f); }
            else if (nameLower.Contains("pine")) { texPath = BasePath + "Lowpoly_Environments/Sources/Textures/PT_Pine_Tree_01.png"; alphaClip = true; tint = new Color(0.16f, 0.42f, 0.20f); }
            else if (nameLower.Contains("fruit")) { texPath = BasePath + "Lowpoly_Environments/Sources/Textures/PT_Fruit_Tree_Leaves_01.png"; alphaClip = true; tint = new Color(0.26f, 0.58f, 0.20f); }
            else if (nameLower.Contains("tree_leaves")) { texPath = BasePath + "Lowpoly_Environments/Sources/Textures/PT_Generic_Tree_Leaves_01.png"; alphaClip = true; tint = new Color(0.26f, 0.58f, 0.20f); }
            else if (nameLower.Contains("leaf") || nameLower.Contains("leaves")) { texPath = BasePath + "Lowpoly_Environments/Sources/Textures/PT_Generic_Leaves_01.png"; alphaClip = true; tint = new Color(0.26f, 0.58f, 0.20f); }
            else if (nameLower.Contains("high_grass")) { texPath = BasePath + "Lowpoly_Environments/Sources/Textures/PT_Grass_High_01.png"; alphaClip = true; tint = new Color(0.28f, 0.62f, 0.22f); }
            else if (nameLower.Contains("grass")) { texPath = BasePath + "Lowpoly_Environments/Sources/Textures/PT_Grass_01.png"; alphaClip = true; tint = new Color(0.28f, 0.62f, 0.22f); }
            else if (nameLower.Contains("poppy")) { texPath = BasePath + "Lowpoly_Environments/Sources/Textures/PT_Poppy_01.png"; alphaClip = true; tint = new Color(0.85f, 0.18f, 0.15f); }
            else if (nameLower.Contains("mushroom")) { texPath = BasePath + "Lowpoly_Environments/Sources/Textures/PT_Mushrooms_01.png"; tint = new Color(0.85f, 0.35f, 0.18f); }
            else if (nameLower.Contains("rock")) { texPath = BasePath + "Lowpoly_Environments/Sources/Textures/PT_Rocks_01.png"; tint = new Color(0.55f, 0.55f, 0.58f); }
            else if (nameLower.Contains("building")) { texPath = BasePath + "Lowpoly_Village/Sources/Textures/PT_Medieval_Buildings_Texture_01.png"; tint = new Color(0.55f, 0.38f, 0.24f); }

            if (texPath != null)
            {
                Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                if (tex != null && mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", tex);
                }
            }
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", tint);
            }

            if (alphaClip && mat.HasProperty("_AlphaClip"))
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.EnableKeyword("_ALPHATEST_ON");
            }

            EditorUtility.SetDirty(mat);
            converted++;
        }

        FixSkyboxAndWater();
        AssetDatabase.SaveAssets();
        Debug.Log($"Converted {converted} Polytope Studio materials to URP/Lit.");
    }

    [MenuItem("Tools/Polytope Studio/Randomize Tree Leaf Colors")]
    public static void RandomizeTreeLeafColors()
    {
        Color[] seasonalColors =
        {
            new Color(0.26f, 0.58f, 0.20f), // Summer Green
            new Color(0.16f, 0.42f, 0.20f), // Deep Forest Green
            new Color(0.85f, 0.58f, 0.18f), // Autumn Gold
            new Color(0.82f, 0.35f, 0.20f), // Autumn Orange-Red
            new Color(0.92f, 0.62f, 0.72f)  // Spring Blossom Pink
        };

        MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        int updated = 0;

        foreach (MeshRenderer mr in renderers)
        {
            Material[] mats = mr.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string n = mats[i].name.ToLowerInvariant();
                if ((n.Contains("leaf") || n.Contains("leaves") || n.Contains("foliage") || n.Contains("pine")) && !n.Contains("trunk"))
                {
                    Material variant = new Material(mats[i]);
                    Color c = seasonalColors[Random.Range(0, seasonalColors.Length)];
                    if (variant.HasProperty("_BaseColor")) variant.SetColor("_BaseColor", c);
                    mats[i] = variant;
                    changed = true;
                }
            }

            if (changed)
            {
                mr.sharedMaterials = mats;
                EditorUtility.SetDirty(mr);
                updated++;
            }
        }

        // Also tint Terrain Trees
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null && terrain.terrainData != null)
        {
            TreeInstance[] instances = terrain.terrainData.treeInstances;
            for (int i = 0; i < instances.Length; i++)
            {
                instances[i].color = seasonalColors[Random.Range(0, seasonalColors.Length)];
            }
            terrain.terrainData.treeInstances = instances;
            terrain.Flush();
            EditorUtility.SetDirty(terrain.terrainData);
            updated += instances.Length;
        }

        Debug.Log($"Applied seasonal leaf color variations to {updated} trees/foliage.");
    }

    [MenuItem("Tools/Polytope Studio/Build Complete Game Environment")]
    public static void BuildEnvironment()
    {
        ConvertPolytopeMaterialsToURP();
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Root organization
        GameObject envRoot = new GameObject("--- ENVIRONMENT ---");
        GameObject groundRoot = CreateChild(envRoot, "Ground_And_River");
        GameObject villageRoot = CreateChild(envRoot, "Village_Area");
        GameObject forestRoot = CreateChild(envRoot, "Forest_Trees");
        GameObject rocksRoot = CreateChild(envRoot, "Rocks_And_Landmarks");
        GameObject foliageRoot = CreateChild(envRoot, "Foliage_Details");

        // 1. Ground & River
        CreateGroundAndRiver(groundRoot);

        // 2. Bridge crossing the river
        SpawnPrefab(BasePath + "Lowpoly_Village/Prefabs/Modular/Bridge/PT_Wooden_Bridge_02.prefab",
            new Vector3(0f, 0.35f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one, villageRoot.transform);

        // 3. Village Fences & Gate (West side: X < -6)
        BuildVillagePerimeter(villageRoot.transform);

        // 4. Woodcutting / Camp area inside village
        SpawnPrefab(BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_logs.prefab",
            new Vector3(-14f, 0f, -6f), Quaternion.Euler(0f, 25f, 0f), Vector3.one, villageRoot.transform);
        SpawnPrefab(BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_logs.prefab",
            new Vector3(-16f, 0f, -4f), Quaternion.Euler(0f, 110f, 0f), Vector3.one, villageRoot.transform);
        SpawnPrefab(BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_stump.prefab",
            new Vector3(-13f, 0f, -8f), Quaternion.identity, Vector3.one, villageRoot.transform);
        SpawnPrefab(BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_stump.prefab",
            new Vector3(-17f, 0f, -7f), Quaternion.identity, Vector3.one, villageRoot.transform);

        // 5. Village Orchard (Fruit Trees)
        string[] fruitTrees =
        {
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_apples.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_pears.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_plums.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green.prefab"
        };

        Vector3[] orchardPositions =
        {
            new Vector3(-14f, 0f, 8f),
            new Vector3(-20f, 0f, 11f),
            new Vector3(-16f, 0f, 15f),
            new Vector3(-22f, 0f, 5f)
        };

        for (int i = 0; i < orchardPositions.Length; i++)
        {
            SpawnPrefab(fruitTrees[i % fruitTrees.Length], orchardPositions[i],
                Quaternion.Euler(0f, i * 75f, 0f), Vector3.one * Random.Range(0.95f, 1.1f), villageRoot.transform);
        }

        // 6. Forest & Wilderness (East side: X > 8, plus outer perimeter)
        string[] forestTreePrefabs =
        {
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_dead.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_dead.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green_cut.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green_cut.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_dead_cut.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_dead_cut.prefab"
        };

        Random.InitState(42);
        for (int i = 0; i < 45; i++)
        {
            float x = Random.Range(10f, 45f);
            float z = Random.Range(-38f, 38f);
            string prefab = forestTreePrefabs[Random.Range(0, forestTreePrefabs.Length)];
            float scale = Random.Range(0.85f, 1.35f);
            SpawnPrefab(prefab, new Vector3(x, 0f, z),
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * scale, forestRoot.transform);
        }

        // Outer west forest backdrop
        for (int i = 0; i < 20; i++)
        {
            float x = Random.Range(-45f, -28f);
            float z = Random.Range(-38f, 38f);
            string prefab = forestTreePrefabs[Random.Range(0, 4)];
            float scale = Random.Range(0.9f, 1.3f);
            SpawnPrefab(prefab, new Vector3(x, 0f, z),
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * scale, forestRoot.transform);
        }

        // 7. Rocks & Landmarks
        string[] rockPrefabs =
        {
            BasePath + "Lowpoly_Environments/Prefabs/Rocks/PT_Generic_Rock_01.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Rocks/PT_Ore_Rock_01.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Rocks/PT_Ore_Rock_01_split.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Rocks/PT_River_Rock_Pile_02.prefab",
            BasePath + "Lowpoly_Demos/Environment_Free/Helpers/PT_River_Rock_Pile_02_v1.prefab"
        };

        // Menhir landmark in forest clearing
        SpawnPrefab(BasePath + "Lowpoly_Environments/Prefabs/Rocks/PT_Menhir_Rock_02.prefab",
            new Vector3(22f, 0f, 12f), Quaternion.Euler(0f, 30f, 0f), Vector3.one * 1.2f, rocksRoot.transform);
        SpawnPrefab(BasePath + "Lowpoly_Environments/Prefabs/Rocks/PT_Menhir_Rock_02.prefab",
            new Vector3(25f, 0f, 15f), Quaternion.Euler(0f, 140f, 0f), Vector3.one * 0.9f, rocksRoot.transform);

        // Riverbank rocks
        for (int i = 0; i < 18; i++)
        {
            float x = (i % 2 == 0) ? Random.Range(-5.5f, -3.5f) : Random.Range(3.5f, 5.5f);
            float z = Random.Range(-35f, 35f);
            if (Mathf.Abs(z) < 4f) continue; // Keep bridge clear
            string prefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
            SpawnPrefab(prefab, new Vector3(x, 0f, z),
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(0.8f, 1.4f), rocksRoot.transform);
        }

        // 8. Foliage Details (Shrubs, Grass, Poppies, Mushrooms)
        string[] foliagePrefabs =
        {
            BasePath + "Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab",
            BasePath + "Lowpoly_Demos/Environment_Free/Helpers/PT_Grass_02_v1.prefab",
            BasePath + "Lowpoly_Demos/Environment_Free/Helpers/PT_Grass_02_v2.prefab",
            BasePath + "Lowpoly_Demos/Environment_Free/Helpers/PT_High_Grass_02_v1.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Flowers/PT_Poppy_02.prefab",
            BasePath + "Lowpoly_Demos/Environment_Free/Helpers/PT_Poppy_02_v1.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_dead.prefab",
            BasePath + "Lowpoly_Environments/Prefabs/Mushrooms/PT_Caesars_Mushroom_01.prefab"
        };

        for (int i = 0; i < 90; i++)
        {
            float x = Random.Range(-40f, 40f);
            float z = Random.Range(-38f, 38f);
            if (Mathf.Abs(x) < 4.5f) continue; // Keep river clear
            string prefab = foliagePrefabs[Random.Range(0, foliagePrefabs.Length)];
            SpawnPrefab(prefab, new Vector3(x, 0f, z),
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), Vector3.one * Random.Range(0.8f, 1.3f), foliageRoot.transform);
        }

        // 9. Lighting & Camera
        SetupLightingAndCamera();

        EditorSceneManager.SaveScene(newScene, OutputScenePath);
        Debug.Log("Polytope Studio Game Environment built and saved to: " + OutputScenePath);
    }

    private static void CreateGroundAndRiver(GameObject parent)
    {
        // Main Grass Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground_Plane";
        ground.transform.SetParent(parent.transform);
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(100f, 1f, 100f);

        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMat.color = new Color(0.28f, 0.52f, 0.24f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // River Water Plane
        GameObject river = GameObject.CreatePrimitive(PrimitiveType.Cube);
        river.name = "River_Water";
        river.transform.SetParent(parent.transform);
        river.transform.position = new Vector3(0f, -0.15f, 0f);
        river.transform.localScale = new Vector3(7f, 0.4f, 100f);

        Material waterMat = AssetDatabase.LoadAssetAtPath<Material>(BasePath + "Lowpoly_Environments/Sources/Materials/PT_Water_mat.mat");
        if (waterMat == null)
        {
            waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            waterMat.color = new Color(0.18f, 0.55f, 0.78f);
        }
        river.GetComponent<Renderer>().sharedMaterial = waterMat;
    }

    private static void BuildVillagePerimeter(Transform parent)
    {
        string fence1 = BasePath + "Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
        string fence2 = BasePath + "Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_02.prefab";
        string gate = BasePath + "Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Gate_Wood_01.prefab";

        // East-facing fence line near bridge with gate at Z = 0
        for (int z = -12; z <= 12; z += 4)
        {
            if (z == 0)
            {
                SpawnPrefab(gate, new Vector3(-7f, 0f, z), Quaternion.Euler(0f, 90f, 0f), Vector3.one, parent);
            }
            else
            {
                string f = (Mathf.Abs(z) % 8 == 0) ? fence1 : fence2;
                SpawnPrefab(f, new Vector3(-7f, 0f, z), Quaternion.Euler(0f, 90f, 0f), Vector3.one, parent);
            }
        }
    }

    private static void SetupLightingAndCamera()
    {
        Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(BasePath + "Lowpoly_Environments/Sources/Materials/PT_Skybox_mat.mat");
        if (skyboxMat != null)
        {
            RenderSettings.skybox = skyboxMat;
        }

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.75f, 0.85f, 0.92f);
        RenderSettings.fogDensity = 0.008f;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(-22f, 14f, -20f);
            mainCam.transform.LookAt(new Vector3(4f, 1f, 2f));
        }

        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight != null)
        {
            dirLight.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            dirLight.color = new Color(1f, 0.95f, 0.85f);
            dirLight.intensity = 1.2f;
        }
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        return child;
    }

    private static void SpawnPrefab(string assetPath, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning("Prefab not found: " + assetPath);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        instance.transform.localScale = scale;
    }
}
