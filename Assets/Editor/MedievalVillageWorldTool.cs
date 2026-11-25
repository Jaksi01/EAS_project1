using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tool Editor untuk membangun pemukiman pedesaan multi-area (4 distrik, 22 bangunan utuh)
/// menggunakan hampir seluruh aset Medieval Village MegaKit[Standard], meratakan tiap area
/// secara non-destruktif, mengurangi/memindahkan pohon penghalang, dan menghubungkan antar-area
/// dengan jalan batu, tepian trotoar, pagar kayu/besi, serta kebun buah.
/// </summary>
public class MedievalVillageWorldTool : EditorWindow
{
    private const string RootName = "--- VILLAGE_GENERATED ---";
    private const string TerrainBackupFolder = "Assets/Generated/VillageTerrainData/";
    private const string PolytopeBase = "Assets/Scenes/Polytope Studio/";

    private Vector2 _mainTownCenterXZ = new Vector2(10.5f, 69.2f);
    private float _villageYaw = 0f;
    private float _blendMargin = 12f;
    [Range(0f, 1f)] private float _perimeterTreeKeepRatio = 0.35f;
    private bool _autoAnchorToBridge = true;
    private bool _movePlayerToVillage = true;

    private struct DistrictConfig
    {
        public string name;
        public Vector2 centerXZ;
        public float baseHeight;
        public float plateauRadius;
        public float plazaHalfSize;
        public bool isWestBank;
        public BuildingPlacement[] buildings;
    }

    private struct BuildingPlacement
    {
        public string prefabName;
        public string label;
        public Vector2 localOffset;
        public float localYaw;
        public float clearanceRadius;
    }

    private struct RoadSegment
    {
        public Vector2 startWorld;
        public Vector2 endWorld;
        public float halfWidth;
        public bool addRoadsideFences;
        public bool addScatteredBricks;
    }

    // 1. Area Pusat Kota (Barat Jembatan)
    private static readonly BuildingPlacement[] CentralTownBuildings =
    {
        new BuildingPlacement { prefabName = "Village_Tavern_Hall",       label = "01_Town_Tavern_Hall",       localOffset = new Vector2(-12.5f,  0.0f), localYaw =   90f, clearanceRadius = 6.2f },
        new BuildingPlacement { prefabName = "Village_Skybridge_Complex", label = "02_Town_Skybridge_Complex", localOffset = new Vector2( -1.0f, 14.5f), localYaw =  180f, clearanceRadius = 7.5f },
        new BuildingPlacement { prefabName = "Village_Watchtower",        label = "03_Town_Bridge_Watchtower", localOffset = new Vector2( 12.5f,  6.5f), localYaw =  180f, clearanceRadius = 4.2f },
        new BuildingPlacement { prefabName = "Village_Blacksmith_Shop",   label = "04_Town_Blacksmith",        localOffset = new Vector2(  8.0f, -7.8f), localYaw =  -35f, clearanceRadius = 4.2f },
        new BuildingPlacement { prefabName = "Village_Market_Pavilion",   label = "05_Town_Market_Pavilion",   localOffset = new Vector2( -1.5f,-13.5f), localYaw =    0f, clearanceRadius = 4.6f },
        new BuildingPlacement { prefabName = "Village_Cottage_TwoStory",  label = "06_Town_TwoStory_House",    localOffset = new Vector2(-12.0f,-12.0f), localYaw =   45f, clearanceRadius = 5.0f },
        new BuildingPlacement { prefabName = "Village_Cottage_Small",     label = "07_Town_North_Cottage",     localOffset = new Vector2( 13.5f, 17.5f), localYaw = -125f, clearanceRadius = 4.8f },
    };

    // 2. Area Pemukiman Kebun Selatan
    private static readonly BuildingPlacement[] SouthHamletBuildings =
    {
        new BuildingPlacement { prefabName = "Village_Barn_Granary",      label = "08_South_Barn_Granary",     localOffset = new Vector2(-10.0f,  0.0f), localYaw =   90f, clearanceRadius = 7.2f },
        new BuildingPlacement { prefabName = "Village_Cottage_TwoStory",  label = "09_South_TwoStory_House",   localOffset = new Vector2(  0.0f, 10.5f), localYaw =  180f, clearanceRadius = 5.0f },
        new BuildingPlacement { prefabName = "Village_Cottage_Small",     label = "10_South_East_Cottage",     localOffset = new Vector2( 10.5f,  1.5f), localYaw =  -90f, clearanceRadius = 4.8f },
        new BuildingPlacement { prefabName = "Village_Cottage_Small",     label = "11_South_Cottage",          localOffset = new Vector2(  1.5f,-10.0f), localYaw =    0f, clearanceRadius = 4.8f },
        new BuildingPlacement { prefabName = "Village_Market_Pavilion",   label = "12_South_Orchard_Stall",    localOffset = new Vector2(  9.0f, -8.0f), localYaw =  -45f, clearanceRadius = 4.6f },
    };

    // 3. Area Bukit Manor Utara
    private static readonly BuildingPlacement[] NorthManorBuildings =
    {
        new BuildingPlacement { prefabName = "Village_Manor_Overhang",    label = "13_North_Grand_Manor",      localOffset = new Vector2(-10.5f,  0.0f), localYaw =   90f, clearanceRadius = 6.5f },
        new BuildingPlacement { prefabName = "Village_Longhouse",         label = "14_North_Council_Hall",     localOffset = new Vector2(  0.0f, 11.5f), localYaw =  180f, clearanceRadius = 6.8f },
        new BuildingPlacement { prefabName = "Village_Watchtower",        label = "15_North_Lookout_Tower",    localOffset = new Vector2( 11.5f,  5.5f), localYaw = -120f, clearanceRadius = 4.2f },
        new BuildingPlacement { prefabName = "Village_Cottage_TwoStory",  label = "16_North_TwoStory_Villa",   localOffset = new Vector2( 10.5f, -6.5f), localYaw =  -65f, clearanceRadius = 5.0f },
        new BuildingPlacement { prefabName = "Village_Cottage_Small",     label = "17_North_Gate_Cottage",     localOffset = new Vector2( -2.5f,-11.0f), localYaw =   15f, clearanceRadius = 4.8f },
    };

    // 4. Area Pos Penjagaan Seberang Jembatan (Timur Sungai)
    private static readonly BuildingPlacement[] EastOutpostBuildings =
    {
        new BuildingPlacement { prefabName = "Village_Watchtower",        label = "18_East_Bridge_Watchtower", localOffset = new Vector2( -9.5f, -6.5f), localYaw =   45f, clearanceRadius = 4.2f },
        new BuildingPlacement { prefabName = "Village_Longhouse",         label = "19_East_Garrison_Hall",     localOffset = new Vector2(  0.0f, 10.5f), localYaw =  180f, clearanceRadius = 6.8f },
        new BuildingPlacement { prefabName = "Village_Blacksmith_Shop",   label = "20_East_Field_Blacksmith",  localOffset = new Vector2(  9.0f, -7.5f), localYaw =  -45f, clearanceRadius = 4.2f },
        new BuildingPlacement { prefabName = "Village_Market_Pavilion",   label = "21_East_Supply_Depot",      localOffset = new Vector2( 10.5f,  4.0f), localYaw =  -90f, clearanceRadius = 4.6f },
        new BuildingPlacement { prefabName = "Village_Cottage_Small",     label = "22_East_Scout_Quarters",    localOffset = new Vector2( -9.5f,  7.5f), localYaw =  135f, clearanceRadius = 4.8f },
    };

    [MenuItem("Tools/Village Builder/2. Build Complete Village & Rearrange World")]
    public static void BuildDefaultVillageMenu()
    {
        var tool = CreateInstance<MedievalVillageWorldTool>();
        tool.AutoDetectAnchor();
        tool.BuildVillageAndRearrangeWorld();
        DestroyImmediate(tool);
    }

    [MenuItem("Tools/Village Builder/3. Open Village Layout Window")]
    public static void ShowWindow()
    {
        var win = GetWindow<MedievalVillageWorldTool>("Village Layout Tool");
        win.minSize = new Vector2(360f, 320f);
        win.AutoDetectAnchor();
        win.Show();
    }

    [MenuItem("Tools/Village Builder/4. Restore Original World (Clear Village)")]
    public static void RestoreOriginalWorldMenu()
    {
        RestoreExistingBuild();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Village Builder] World asli berhasil dipulihkan dan seluruh distrik desa telah dihapus.");
    }

    private void OnGUI()
    {
        GUILayout.Label("Multi-Area Medieval Village & World Rearranger", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Membangun 4 distrik pemukiman (22 bangunan utuh dari Medieval Village MegaKit), " +
            "menghubungkannya dengan jalan batu & pagar, meratakan terrain per area, serta merapikan pohon.",
            MessageType.Info);

        _autoAnchorToBridge = EditorGUILayout.Toggle("Auto-Anchor dari Jembatan", _autoAnchorToBridge);
        if (_autoAnchorToBridge && GUILayout.Button("Deteksi Ulang Posisi Jembatan"))
        {
            AutoDetectAnchor();
        }

        _mainTownCenterXZ = EditorGUILayout.Vector2Field("Central Town Center (X, Z)", _mainTownCenterXZ);
        _villageYaw = EditorGUILayout.Slider("Orientasi Global (Yaw)", _villageYaw, -45f, 45f);
        _blendMargin = EditorGUILayout.Slider("Terrain Blend Margin", _blendMargin, 6f, 20f);
        _perimeterTreeKeepRatio = EditorGUILayout.Slider("Sisa Pohon Sekitar Desa", _perimeterTreeKeepRatio, 0f, 1f);
        _movePlayerToVillage = EditorGUILayout.Toggle("Pindahkan Player ke Plaza Utama", _movePlayerToVillage);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("1. Rebuild MegaKit Materials & 10 Prefabs", GUILayout.Height(28)))
        {
            MegaKitPrefabAssembler.AssembleAllMenu();
        }
        if (GUILayout.Button("2. Build Multi-Area Village (4 Distrik / 22 Bangunan)", GUILayout.Height(36)))
        {
            if (_autoAnchorToBridge) AutoDetectAnchor();
            BuildVillageAndRearrangeWorld();
        }
        if (GUILayout.Button("Restore Original World (Clear)", GUILayout.Height(26)))
        {
            RestoreOriginalWorldMenu();
        }
    }

    private void AutoDetectAnchor()
    {
        GameObject bridge = GameObject.Find("PT_Wooden_Bridge_02");
        if (bridge != null)
        {
            Vector3 bPos = bridge.transform.position;
            _mainTownCenterXZ = new Vector2(bPos.x - 22.3f, bPos.z);
        }
    }

    private void BuildVillageAndRearrangeWorld()
    {
        RestoreExistingBuild();

        // 1. Pastikan material URP & 10 prefab bangunan terbaru tersedia
        Dictionary<string, Material> mats = MegaKitPrefabAssembler.EnsureMaterials();
        MegaKitPrefabAssembler.RemapFbxMaterials(mats);
        MegaKitPrefabAssembler.AssembleAllBuildingPrefabs(mats);

        GameObject bridge = GameObject.Find("PT_Wooden_Bridge_02");
        float waterY = FindWaterLevel();
        Vector3 bridgePos = bridge != null ? bridge.transform.position : new Vector3(32.78f, 2.96f, 69.21f);
        Vector3 bridgeWestLanding = bridgePos + new Vector3(-3.2f, 0.1f, 0f);
        Vector3 bridgeEastLanding = bridgePos + new Vector3(3.4f, 0.1f, 0f);

        // 2. Definisikan 4 distrik desa tersebar di world
        List<DistrictConfig> districts = BuildDistrictConfigs(bridgePos, waterY, bridgeWestLanding, bridgeEastLanding);

        // 3. Definisikan jaringan jalan batu penghubung antar-area + jembatan
        List<RoadSegment> roads = BuildRoadNetwork(districts, bridgeWestLanding, bridgeEastLanding);

        GameObject root = new GameObject(RootName);
        VillageBuildRecord record = root.AddComponent<VillageBuildRecord>();
        record.villageCenter = new Vector3(districts[0].centerXZ.x, districts[0].baseHeight, districts[0].centerXZ.y);
        record.villageYaw = _villageYaw;
        record.baseHeight = districts[0].baseHeight;

        GameObject buildingsRoot = CreateChild(root, "01_District_Buildings");
        GameObject roadsRoot = CreateChild(root, "02_Plazas_And_Connecting_Roads");
        GameObject propsRoot = CreateChild(root, "03_Fences_Orchard_And_Props");
        GameObject wildernessRoot = CreateChild(root, "04_East_Frontier_Landmarks");

        // 4. Kumpulkan seluruh zona steril (bangunan + plaza) agar pohon/rumput bersih
        List<(Vector3 center, float radius)> clearZones = new List<(Vector3, float)>();
        foreach (DistrictConfig d in districts)
        {
            clearZones.Add((new Vector3(d.centerXZ.x, d.baseHeight, d.centerXZ.y), d.plazaHalfSize + 2.5f));
            foreach (BuildingPlacement bp in d.buildings)
            {
                Vector3 wp = DistrictLocalToWorld(d, bp.localOffset);
                clearZones.Add((wp, bp.clearanceRadius + 2.2f));
            }
        }

        // 5. Ratakan terrain tiap distrik, ramp jembatan Barat & Timur, dan rapikan pohon/rumput
        RearrangeTerrainAndTrees(record, districts, roads, clearZones, bridgePos, bridgeWestLanding, bridgeEastLanding);

        // 6. Perluas batas collider world agar pemain bebas menjelajah seluruh 9 terrain (300x300m)
        ExpandWorldBoundaries(record);

        // 7. Nonaktifkan objek scene lama yang tabrakan dengan area desa/jalan
        RearrangeLooseSceneObjects(record, districts, bridge);

        // 7. Spawn 22 bangunan di 4 distrik berbeda
        foreach (DistrictConfig d in districts)
        {
            GameObject dGroup = CreateChild(buildingsRoot, d.name);
            foreach (BuildingPlacement bp in d.buildings)
            {
                GameObject prefab = MegaKitPrefabAssembler.LoadVillagePrefab(bp.prefabName);
                if (prefab == null)
                {
                    Debug.LogWarning($"[Village Builder] Prefab '{bp.prefabName}' tidak ditemukan.");
                    continue;
                }

                Vector3 pos = DistrictLocalToWorld(d, bp.localOffset);
                pos.y = d.baseHeight;
                Quaternion rot = Quaternion.Euler(0f, _villageYaw + bp.localYaw, 0f);

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, dGroup.transform);
                inst.name = bp.label;
                inst.transform.position = pos;
                inst.transform.rotation = rot;
            }
        }

        // 8. Bangun Plaza batu tiap distrik & seluruh jalan penghubung + pagar kayu/besi + bata sebaran
        BuildAllPlazasAndRoads(districts, roads, roadsRoot.transform, mats);

        // 9. Tambahkan gerbang jembatan, kebun buah selatan, gerobak, peti, bunga, dan pinus tepian
        BuildDistrictPropsAndOrchard(districts, propsRoot.transform, mats);

        // 10. Landmark frontier di antara jembatan dan East Bastion
        BuildEastFrontierLandmarks(bridgePos, wildernessRoot.transform);

        // 11. Posisikan Player di jalan masuk Plaza Utama menghadap Tavern & Skybridge
        PositionPlayer(record, districts[0]);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log($"[Village Builder] Selesai! 4 village terpisah (22 bangunan) tersebar di world 300x300m, batas collider diperluas, dan kontroler pemain diperbaiki.");
    }

    private List<DistrictConfig> BuildDistrictConfigs(
        Vector3 bridgePos,
        float waterY,
        Vector3 bridgeWestLanding,
        Vector3 bridgeEastLanding)
    {
        // Village 1: Home Village tepat di Barat jembatan (terhubung ke jembatan)
        float townH = ComputeDistrictHeight(_mainTownCenterXZ, waterY, bridgeWestLanding.y + 0.9f);

        // Village 2: Selatan-Barat jauh (~105m dari Home Village)
        Vector2 southXZ = new Vector2(_mainTownCenterXZ.x - 48.0f, _mainTownCenterXZ.y - 93.0f);
        float southH = ComputeNaturalDistrictHeight(southXZ, waterY);

        // Village 3: Utara jauh (~105m dari Home Village)
        Vector2 northXZ = new Vector2(_mainTownCenterXZ.x - 5.5f, _mainTownCenterXZ.y + 104.8f);
        float northH = ComputeNaturalDistrictHeight(northXZ, waterY);

        // Village 4: Timur jauh seberang jembatan & hutan (~124m dari Home Village)
        Vector2 eastXZ = new Vector2(bridgePos.x + 101.0f, bridgePos.z);
        float eastH = ComputeNaturalDistrictHeight(eastXZ, waterY);

        return new List<DistrictConfig>
        {
            new DistrictConfig
            {
                name = "Village_01_Riverside_Haven_Home",
                centerXZ = _mainTownCenterXZ,
                baseHeight = townH,
                plateauRadius = 21.5f,
                plazaHalfSize = 5.0f,
                isWestBank = true,
                buildings = CentralTownBuildings
            },
            new DistrictConfig
            {
                name = "Village_02_Orchard_Hold_South",
                centerXZ = southXZ,
                baseHeight = southH,
                plateauRadius = 18.5f,
                plazaHalfSize = 3.0f,
                isWestBank = true,
                buildings = SouthHamletBuildings
            },
            new DistrictConfig
            {
                name = "Village_03_Hilltop_Keep_North",
                centerXZ = northXZ,
                baseHeight = northH,
                plateauRadius = 19.0f,
                plazaHalfSize = 3.0f,
                isWestBank = true,
                buildings = NorthManorBuildings
            },
            new DistrictConfig
            {
                name = "Village_04_Eastwood_Bastion_East",
                centerXZ = eastXZ,
                baseHeight = eastH,
                plateauRadius = 18.5f,
                plazaHalfSize = 3.0f,
                isWestBank = false,
                buildings = EastOutpostBuildings
            }
        };
    }

    private float ComputeDistrictHeight(Vector2 xz, float waterY, float maxAllowed)
    {
        float sampled = SampleWorldTerrainHeight(new Vector3(xz.x, 0f, xz.y));
        float minH = waterY + 1.15f;
        return Mathf.Clamp(sampled, minH, Mathf.Max(minH + 0.5f, maxAllowed));
    }

    private float ComputeNaturalDistrictHeight(Vector2 xz, float waterY)
    {
        float sampled = SampleWorldTerrainHeight(new Vector3(xz.x, 0f, xz.y));
        return Mathf.Max(sampled, waterY + 1.4f);
    }

    private List<RoadSegment> BuildRoadNetwork(
        List<DistrictConfig> districts,
        Vector3 bridgeWestLanding,
        Vector3 bridgeEastLanding)
    {
        Vector2 cTown = districts[0].centerXZ;
        Vector2 cSouth = districts[1].centerXZ;
        Vector2 cNorth = districts[2].centerXZ;
        Vector2 cEast = districts[3].centerXZ;

        return new List<RoadSegment>
        {
            // Village 1 (Home): Jalan Plaza -> Jembatan Barat + akses Tavern & Skybridge
            new RoadSegment { startWorld = cTown + new Vector2(4.5f, 0f), endWorld = new Vector2(bridgeWestLanding.x, bridgeWestLanding.z), halfWidth = 2.1f, addRoadsideFences = true, addScatteredBricks = true },
            new RoadSegment { startWorld = cTown + new Vector2(-8.0f, 0f), endWorld = cTown + new Vector2(-4.5f, 0f), halfWidth = 1.6f },
            new RoadSegment { startWorld = cTown + new Vector2(-1.0f, 4.5f), endWorld = cTown + new Vector2(-1.0f, 9.5f), halfWidth = 1.6f },
            new RoadSegment { startWorld = cTown + new Vector2(3.0f, -5.0f), endWorld = cTown + new Vector2(3.0f, -13.5f), halfWidth = 1.5f, addScatteredBricks = true },

            // Jalan keluar dari Jembatan Timur menuju hutan Timur
            new RoadSegment { startWorld = new Vector2(bridgeEastLanding.x, bridgeEastLanding.z), endWorld = new Vector2(bridgeEastLanding.x + 12.0f, bridgeEastLanding.z), halfWidth = 2.0f, addRoadsideFences = true, addScatteredBricks = true },

            // Village 2 (South): Jalan internal & gerbang masuk
            new RoadSegment { startWorld = cSouth + new Vector2(-5.5f, 0f), endWorld = cSouth + new Vector2(12.5f, 0f), halfWidth = 1.6f, addRoadsideFences = true, addScatteredBricks = true },
            new RoadSegment { startWorld = cSouth + new Vector2(0f, -6.5f), endWorld = cSouth + new Vector2(0f, 6.5f), halfWidth = 1.5f },

            // Village 3 (North): Jalan internal Manor & Watchtower
            new RoadSegment { startWorld = cNorth + new Vector2(-6.0f, 0f), endWorld = cNorth + new Vector2(13.0f, 0f), halfWidth = 1.6f, addRoadsideFences = true, addScatteredBricks = true },
            new RoadSegment { startWorld = cNorth + new Vector2(0f, -7.5f), endWorld = cNorth + new Vector2(0f, 7.0f), halfWidth = 1.5f },

            // Village 4 (East Bastion): Jalan masuk Barat -> Plaza -> Gerbang Timur
            new RoadSegment { startWorld = cEast + new Vector2(-14.5f, 0f), endWorld = cEast + new Vector2(14.5f, 0f), halfWidth = 1.8f, addRoadsideFences = true, addScatteredBricks = true },
            new RoadSegment { startWorld = cEast + new Vector2(0f, -5.0f), endWorld = cEast + new Vector2(0f, 6.5f), halfWidth = 1.5f },
        };
    }

    // =========================================================================
    // TERRAIN LEVELING MULTI-AREA + TREE THINNING (NON-DESTRUKTIF)
    // =========================================================================
    private void RearrangeTerrainAndTrees(
        VillageBuildRecord record,
        List<DistrictConfig> districts,
        List<RoadSegment> roads,
        List<(Vector3 center, float radius)> clearZones,
        Vector3 bridgePos,
        Vector3 bridgeWestLanding,
        Vector3 bridgeEastLanding)
    {
        EnsureFolderExists(TerrainBackupFolder);

        float westBankFadeStart = bridgeWestLanding.x - 5.2f;
        float westBankFadeEnd = bridgeWestLanding.x + 0.3f;
        float eastBankFadeStart = bridgeEastLanding.x + 5.2f;
        float eastBankFadeEnd = bridgeEastLanding.x - 0.3f;

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null) continue;

            Bounds tBounds = GetTerrainWorldBounds(terrain);
            bool touchesAny = false;
            foreach (DistrictConfig d in districts)
            {
                float reach = d.plateauRadius + _blendMargin + 10f;
                Vector2 closest = new Vector2(
                    Mathf.Clamp(d.centerXZ.x, tBounds.min.x, tBounds.max.x),
                    Mathf.Clamp(d.centerXZ.y, tBounds.min.z, tBounds.max.z));
                if (Vector2.Distance(closest, d.centerXZ) <= reach)
                {
                    touchesAny = true;
                    break;
                }
            }
            if (!touchesAny) continue;

            TerrainData origData = terrain.terrainData;
            record.terrains.Add(terrain);
            record.originalTerrainData.Add(origData);

            string clonePath = $"{TerrainBackupFolder}{terrain.gameObject.name}_VillageCopy.asset";
            if (AssetDatabase.LoadAssetAtPath<TerrainData>(clonePath) != null)
            {
                AssetDatabase.DeleteAsset(clonePath);
            }

            TerrainData workingData = Object.Instantiate(origData);
            AssetDatabase.CreateAsset(workingData, clonePath);
            terrain.terrainData = workingData;
            TerrainCollider tc = terrain.GetComponent<TerrainCollider>();
            if (tc != null) tc.terrainData = workingData;

            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = workingData.size;

            // A. Leveling Heightmap per Distrik + Ramp Jembatan Barat/Timur
            int hmRes = workingData.heightmapResolution;
            float[,] heights = workingData.GetHeights(0, 0, hmRes, hmRes);

            for (int z = 0; z < hmRes; z++)
            {
                float worldZ = tPos.z + (z / (float)(hmRes - 1)) * tSize.z;
                for (int x = 0; x < hmRes; x++)
                {
                    float worldX = tPos.x + (x / (float)(hmRes - 1)) * tSize.x;

                    // Lindungi palung sungai di bawah jembatan
                    if (worldX > westBankFadeEnd && worldX < eastBankFadeEnd) continue;

                    float bestWeight = 0f;
                    float targetY = 0f;

                    foreach (DistrictConfig d in districts)
                    {
                        if (d.isWestBank && worldX > westBankFadeEnd) continue;
                        if (!d.isWestBank && worldX < eastBankFadeEnd) continue;

                        float dist = Vector2.Distance(new Vector2(worldX, worldZ), d.centerXZ);
                        if (dist > d.plateauRadius + _blendMargin) continue;

                        float radialW = dist <= d.plateauRadius
                            ? 1f
                            : 1f - SmoothStep01(0f, 1f, (dist - d.plateauRadius) / _blendMargin);

                        float bankFade = 1f;
                        if (d.isWestBank && worldX > westBankFadeStart)
                            bankFade = 1f - SmoothStep01(westBankFadeStart, westBankFadeEnd, worldX);
                        else if (!d.isWestBank && worldX < eastBankFadeStart)
                            bankFade = 1f - SmoothStep01(eastBankFadeStart, eastBankFadeEnd, worldX);

                        float w = radialW * bankFade;
                        if (w > bestWeight)
                        {
                            bestWeight = w;
                            targetY = d.baseHeight;
                        }
                    }

                    // Ramp halus menuju ujung Barat & Timur jembatan
                    float lateralBridge = Mathf.Abs(worldZ - bridgePos.z);
                    if (lateralBridge < 5.0f)
                    {
                        float lateralFactor = 1f - SmoothStep01(2.2f, 5.0f, lateralBridge);
                        if (worldX >= westBankFadeStart - 1.5f && worldX <= westBankFadeEnd)
                        {
                            float tRamp = Mathf.InverseLerp(westBankFadeStart - 1.5f, bridgeWestLanding.x, worldX);
                            float rampY = Mathf.Lerp(districts[0].baseHeight, bridgeWestLanding.y, SmoothStep01(0f, 1f, tRamp));
                            float rw = lateralFactor * (1f - SmoothStep01(bridgeWestLanding.x - 0.5f, westBankFadeEnd, worldX));
                            if (rw > 0.01f)
                            {
                                targetY = Mathf.Lerp(targetY, rampY, lateralFactor);
                                bestWeight = Mathf.Max(bestWeight, rw);
                            }
                        }
                        else if (worldX >= eastBankFadeEnd && worldX <= eastBankFadeStart + 1.5f)
                        {
                            float tRamp = Mathf.InverseLerp(eastBankFadeStart + 1.5f, bridgeEastLanding.x, worldX);
                            float currentEastBankH = SampleWorldTerrainHeight(new Vector3(eastBankFadeStart + 1.5f, 0f, bridgePos.z));
                            float rampY = Mathf.Lerp(currentEastBankH, bridgeEastLanding.y, SmoothStep01(0f, 1f, tRamp));
                            float rw = lateralFactor * (1f - SmoothStep01(bridgeEastLanding.x + 0.5f, eastBankFadeEnd, worldX));
                            if (rw > 0.01f)
                            {
                                targetY = Mathf.Lerp(targetY, rampY, lateralFactor);
                                bestWeight = Mathf.Max(bestWeight, rw);
                            }
                        }
                    }

                    if (bestWeight <= 0.001f) continue;

                    float currentY = tPos.y + heights[z, x] * tSize.y;
                    float finalY = Mathf.Lerp(currentY, targetY, bestWeight);
                    heights[z, x] = Mathf.Clamp01((finalY - tPos.y) / tSize.y);
                }
            }

            workingData.SetHeights(0, 0, heights);

            // B. Kurangi & rapikan pohon pada TerrainData di semua area desa + koridor jalan
            TreeInstance[] trees = workingData.treeInstances;
            List<TreeInstance> keptTrees = new List<TreeInstance>(trees.Length);
            Random.InitState(2026);

            foreach (TreeInstance tree in trees)
            {
                float wx = tPos.x + tree.position.x * tSize.x;
                float wz = tPos.z + tree.position.z * tSize.z;
                Vector2 wp2 = new Vector2(wx, wz);

                bool blocked = false;
                foreach (var cz in clearZones)
                {
                    if (Vector2.Distance(wp2, new Vector2(cz.center.x, cz.center.z)) <= cz.radius)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked && IsNearAnyRoad(roads, wp2, 2.8f)) blocked = true;
                if (!blocked && Mathf.Abs(wz - bridgePos.z) < 4.8f && wx >= bridgeWestLanding.x - 4f && wx <= bridgeEastLanding.x + 4f)
                {
                    blocked = true;
                }

                if (blocked) continue;

                // Kurangi kepadatan pohon di pinggiran tiap distrik
                bool nearDistrict = false;
                foreach (DistrictConfig d in districts)
                {
                    if (Vector2.Distance(wp2, d.centerXZ) <= d.plateauRadius + _blendMargin + 2f)
                    {
                        nearDistrict = true;
                        break;
                    }
                }
                if (nearDistrict && Random.value > _perimeterTreeKeepRatio) continue;

                TreeInstance updated = tree;
                float newH = workingData.GetInterpolatedHeight(tree.position.x, tree.position.z);
                updated.position = new Vector3(tree.position.x, newH / tSize.y, tree.position.z);
                keptTrees.Add(updated);
            }

            workingData.SetTreeInstances(keptTrees.ToArray(), false);

            // C. Bersihkan rumput di bawah bangunan, plaza, dan jalan
            ClearGrassDetails(workingData, tPos, tSize, roads, clearZones);

            EditorUtility.SetDirty(workingData);
            terrain.Flush();
        }
    }

    private static void ClearGrassDetails(
        TerrainData data,
        Vector3 tPos,
        Vector3 tSize,
        List<RoadSegment> roads,
        List<(Vector3 center, float radius)> clearZones)
    {
        int dW = data.detailWidth;
        int dH = data.detailHeight;
        int layerCount = data.detailPrototypes.Length;
        if (dW <= 0 || dH <= 0 || layerCount == 0) return;

        for (int layer = 0; layer < layerCount; layer++)
        {
            int[,] map = data.GetDetailLayer(0, 0, dW, dH, layer);
            bool modified = false;

            for (int z = 0; z < dH; z++)
            {
                float wz = tPos.z + (z / (float)dH) * tSize.z;
                for (int x = 0; x < dW; x++)
                {
                    if (map[z, x] == 0) continue;
                    Vector2 wp2 = new Vector2(tPos.x + (x / (float)dW) * tSize.x, wz);

                    bool clear = false;
                    foreach (var cz in clearZones)
                    {
                        if (Vector2.Distance(wp2, new Vector2(cz.center.x, cz.center.z)) <= cz.radius - 0.5f)
                        {
                            clear = true;
                            break;
                        }
                    }

                    if (!clear && IsNearAnyRoad(roads, wp2, 2.2f)) clear = true;

                    if (clear)
                    {
                        map[z, x] = 0;
                        modified = true;
                    }
                }
            }

            if (modified) data.SetDetailLayer(0, 0, layer, map);
        }
    }

    // =========================================================================
    // OBJEK SCENE LAMA YANG TABRAKAN
    // =========================================================================
    private void RearrangeLooseSceneObjects(VillageBuildRecord record, List<DistrictConfig> districts, GameObject bridge)
    {
        string[] targetNames = { "PT_Pine_Tree_03_green", "PT_Caesars_Mushroom_01", "Fence" };
        GameObject[] allObjects = FindObjectsByType<GameObject>();

        foreach (GameObject go in allObjects)
        {
            if (go == null || !go.activeInHierarchy) continue;
            if (go.transform.root.name == RootName) continue;
            if (bridge != null && go == bridge) continue;

            bool matches = false;
            foreach (string prefix in targetNames)
            {
                if (go.name.StartsWith(prefix)) { matches = true; break; }
            }
            if (!matches) continue;

            Vector2 p2 = new Vector2(go.transform.position.x, go.transform.position.z);
            bool hide = go.transform.position.sqrMagnitude < 4f;
            if (!hide)
            {
                foreach (DistrictConfig d in districts)
                {
                    if (Vector2.Distance(p2, d.centerXZ) < d.plateauRadius + 8f)
                    {
                        hide = true;
                        break;
                    }
                }
            }

            if (hide)
            {
                record.disabledObjects.Add(go);
                go.SetActive(false);
            }
        }
    }

    // =========================================================================
    // PLAZA TIAP DISTRIK + JALAN BATU + PAGAR TEPI JALAN + BATA SEBARAN
    // =========================================================================
    private void BuildAllPlazasAndRoads(
        List<DistrictConfig> districts,
        List<RoadSegment> roads,
        Transform parent,
        Dictionary<string, Material> mats)
    {
        string menhirPath = PolytopeBase + "Lowpoly_Environments/Prefabs/Rocks/PT_Menhir_Rock_02.prefab";
        GameObject menhirPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(menhirPath);

        // 1. Bangun Plaza untuk ke-4 distrik
        for (int idx = 0; idx < districts.Count; idx++)
        {
            DistrictConfig d = districts[idx];
            GameObject plazaGroup = CreateChild(parent.gameObject, d.name + "_Plaza");
            int halfSpan = Mathf.RoundToInt(d.plazaHalfSize) - 1; // 4 untuk 10x10, 2 untuk 6x6

            for (int gx = -halfSpan; gx <= halfSpan; gx += 2)
            {
                for (int gz = -halfSpan; gz <= halfSpan; gz += 2)
                {
                    bool inner = Mathf.Abs(gx) <= 2 && Mathf.Abs(gz) <= 2;
                    string tile = inner ? "Floor_Brick" : "Floor_UnevenBrick";
                    Vector3 pos = DistrictLocalToWorld(d, new Vector2(gx, gz));
                    pos.y = d.baseHeight + 0.03f;
                    SpawnMegaKitPiece(tile, pos, Quaternion.Euler(0f, _villageYaw, 0f), plazaGroup.transform, mats, addCollider: true);
                }
            }

            float edge = d.plazaHalfSize;
            for (int i = -halfSpan; i <= halfSpan; i += 2)
            {
                if (i == 0) continue; // Biarkan bukaan jalan
                SpawnDistrictCurb(d, plazaGroup.transform, mats, new Vector2(i, edge), 0f);
                SpawnDistrictCurb(d, plazaGroup.transform, mats, new Vector2(i, -edge), 180f);
                SpawnDistrictCurb(d, plazaGroup.transform, mats, new Vector2(edge, i), 90f);
                SpawnDistrictCurb(d, plazaGroup.transform, mats, new Vector2(-edge, i), -90f);

                // Distrik Manor Utara diberi pagar besi tempa elegan di sisi plaza
                if (idx == 2)
                {
                    Vector3 fp = DistrictLocalToWorld(d, new Vector2(i, edge));
                    fp.y = d.baseHeight + 0.03f;
                    SpawnMegaKitPiece("Prop_MetalFence_Simple", fp, Quaternion.Euler(0f, _villageYaw, 0f), plazaGroup.transform, mats, addCollider: true, scale: new Vector3(1f, 1f, 0.42f));
                }
            }

            SpawnDistrictCurbCorner(d, plazaGroup.transform, mats, new Vector2(-edge, edge), 0f);
            SpawnDistrictCurbCorner(d, plazaGroup.transform, mats, new Vector2(edge, edge), 90f);
            SpawnDistrictCurbCorner(d, plazaGroup.transform, mats, new Vector2(edge, -edge), 180f);
            SpawnDistrictCurbCorner(d, plazaGroup.transform, mats, new Vector2(-edge, -edge), -90f);

            if (menhirPrefab != null)
            {
                GameObject shrine = (GameObject)PrefabUtility.InstantiatePrefab(menhirPrefab, plazaGroup.transform);
                shrine.name = idx == 0 ? "Home_Beacon_Menhir" : $"Territory_Capture_Menhir_{idx}";
                shrine.transform.position = new Vector3(d.centerXZ.x, d.baseHeight + 0.04f, d.centerXZ.y);
                shrine.transform.rotation = Quaternion.Euler(0f, _villageYaw + 35f * (idx + 1), 0f);
                shrine.transform.localScale = Vector3.one * (idx == 0 ? 0.48f : 0.65f);
            }
        }

        // 2. Bangun jaringan jalan batu penghubung antar-distrik
        GameObject roadsGroup = CreateChild(parent.gameObject, "Connecting_Roads");
        string[] brickProps = { "Prop_Brick1", "Prop_Brick2", "Prop_Brick3", "Prop_Brick4" };
        string[] woodFences = { "Prop_WoodenFence_Single", "Prop_WoodenFence_Extension1", "Prop_WoodenFence_Extension2" };

        for (int rIdx = 0; rIdx < roads.Count; rIdx++)
        {
            RoadSegment seg = roads[rIdx];
            Vector2 delta = seg.endWorld - seg.startWorld;
            float len = delta.magnitude;
            if (len < 0.5f) continue;

            Vector2 dir = delta.normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float roadYaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

            int steps = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
            int widthTiles = seg.halfWidth >= 1.9f ? 2 : 1;

            for (int s = 0; s <= steps; s++)
            {
                float t = steps == 0 ? 0f : s / (float)steps;
                Vector2 center2D = Vector2.Lerp(seg.startWorld, seg.endWorld, t);

                for (int w = 0; w < widthTiles; w++)
                {
                    float latOffset = widthTiles == 2 ? (w == 0 ? -1f : 1f) : 0f;
                    Vector2 tile2D = center2D + perp * latOffset;
                    Vector3 worldPos = new Vector3(tile2D.x, 0f, tile2D.y);
                    worldPos.y = SampleWorldTerrainHeight(worldPos) + 0.04f;

                    Vector3 fwdWorld = Quaternion.Euler(0f, roadYaw, 0f) * Vector3.forward;
                    float hAhead = SampleWorldTerrainHeight(worldPos + fwdWorld * 1.2f);
                    float hBehind = SampleWorldTerrainHeight(worldPos - fwdWorld * 1.2f);
                    float pitch = -Mathf.Atan2(hAhead - hBehind, 2.4f) * Mathf.Rad2Deg;

                    string piece = (s % 2 == 0) ? "Floor_UnevenBrick" : "Floor_Brick";
                    SpawnMegaKitPiece(piece, worldPos, Quaternion.Euler(pitch, roadYaw, 0f), roadsGroup.transform, mats, addCollider: true);
                }

                // Pagar kayu tepi jalan & tepian batu di beberapa segmen
                if (seg.addRoadsideFences && s > 1 && s < steps - 1 && s % 2 == 0)
                {
                    float sideOffset = seg.halfWidth + 0.35f;
                    Vector2 fence2D = center2D + perp * sideOffset;
                    Vector3 fp = new Vector3(fence2D.x, 0f, fence2D.y);
                    fp.y = SampleWorldTerrainHeight(fp);
                    SpawnMegaKitPiece(woodFences[(s / 2) % woodFences.Length], fp, Quaternion.Euler(0f, roadYaw + 90f, 0f), roadsGroup.transform, mats, addCollider: true);
                }

                // Pecahan bata/batu dekoratif di tepi jalan (seperti gambar referensi Quaternius!)
                if (seg.addScatteredBricks && s % 3 == 1)
                {
                    float side = (s % 2 == 0) ? 1f : -1f;
                    Vector2 brick2D = center2D + perp * side * (seg.halfWidth - 0.15f);
                    Vector3 bp = new Vector3(brick2D.x, 0f, brick2D.y);
                    bp.y = SampleWorldTerrainHeight(bp) + 0.08f;
                    SpawnMegaKitPiece(brickProps[(s + rIdx) % brickProps.Length], bp, Quaternion.Euler(0f, roadYaw + s * 37f, 0f), roadsGroup.transform, mats, addCollider: false);
                }
            }
        }
    }

    // =========================================================================
    // GERBANG JEMBATAN, KEBUN BUAH, GEROBAK, BUNGA, & POHON TEPIAN
    // =========================================================================
    private void BuildDistrictPropsAndOrchard(List<DistrictConfig> districts, Transform parent, Dictionary<string, Material> mats)
    {
        string fence1 = PolytopeBase + "Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
        string fence2 = PolytopeBase + "Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_02.prefab";
        string gate = PolytopeBase + "Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Gate_Wood_01.prefab";

        // 1. Gerbang Desa Utama & Gerbang Pertahanan di tiap Village terpisah
        SpawnOpenGateLine(districts[0], new Vector2( 11.5f, 0f), _villageYaw + 90f, gate, fence1, fence2, parent);
        SpawnOpenGateLine(districts[1], new Vector2( 12.5f, 0f), _villageYaw + 90f, gate, fence1, fence2, parent);
        SpawnOpenGateLine(districts[2], new Vector2( 13.0f, 0f), _villageYaw + 90f, gate, fence1, fence2, parent);
        SpawnOpenGateLine(districts[3], new Vector2(-14.5f, 0f), _villageYaw + 90f, gate, fence1, fence2, parent);
        SpawnOpenGateLine(districts[3], new Vector2( 14.5f, 0f), _villageYaw + 90f, gate, fence1, fence2, parent);

        // 2. Gerobak & peti tersebar di tiap distrik
        Vector2[] wagonOffsets =
        {
            new Vector2( 5.2f, -3.5f), // Central Town
            new Vector2( 5.5f, -5.0f), // South Orchard
            new Vector2(-4.5f, -5.5f), // North Manor
            new Vector2(-4.5f,  4.5f), // East Outpost
        };
        for (int i = 0; i < districts.Count; i++)
        {
            Vector3 wp = DistrictLocalToWorld(districts[i], wagonOffsets[i]);
            wp.y = districts[i].baseHeight + 0.03f;
            SpawnMegaKitPiece("Prop_Wagon", wp, Quaternion.Euler(0f, _villageYaw + i * 75f - 90f, 0f), parent, mats, addCollider: true);

            Vector3 cp1 = DistrictLocalToWorld(districts[i], new Vector2(-3.4f, 3.6f));
            cp1.y = districts[i].baseHeight + 0.03f;
            SpawnMegaKitPiece("Prop_Crate", cp1, Quaternion.Euler(0f, _villageYaw + 18f, 0f), parent, mats, addCollider: true);

            Vector3 cp2 = DistrictLocalToWorld(districts[i], new Vector2(3.4f, -3.6f));
            cp2.y = districts[i].baseHeight + 0.03f;
            SpawnMegaKitPiece("Prop_Crate", cp2, Quaternion.Euler(0f, _villageYaw - 24f, 0f), parent, mats, addCollider: true);
        }

        // 3. Kebun Buah Luas di South Orchard Hamlet & belakang Central Town
        string[] fruitTrees =
        {
            PolytopeBase + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_apples.prefab",
            PolytopeBase + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_pears.prefab",
            PolytopeBase + "Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_plums.prefab",
        };
        Vector2[] centralOrchard =
        {
            new Vector2(-19.5f,  6.5f),
            new Vector2(-21.0f, -6.5f),
            new Vector2(-17.5f, 16.5f),
        };
        for (int i = 0; i < centralOrchard.Length; i++)
        {
            SpawnPolytopeGrounded(fruitTrees[i % fruitTrees.Length], DistrictLocalToWorld(districts[0], centralOrchard[i]), i * 67f, Vector3.one, parent);
        }

        Vector2[] southOrchard =
        {
            new Vector2(-17.0f, -11.0f),
            new Vector2(-17.5f,  10.0f),
            new Vector2( -8.0f, -15.5f),
            new Vector2( 14.5f, -14.0f),
            new Vector2( 16.0f,   9.5f),
            new Vector2( -8.5f,  15.5f),
        };
        for (int i = 0; i < southOrchard.Length; i++)
        {
            SpawnPolytopeGrounded(fruitTrees[i % fruitTrees.Length], DistrictLocalToWorld(districts[1], southOrchard[i]), i * 53f, Vector3.one * 1.05f, parent);
        }

        // 4. Bunga, semak & jamur hias di plaza tiap distrik
        string poppy = PolytopeBase + "Lowpoly_Environments/Prefabs/Flowers/PT_Poppy_02.prefab";
        string mushroom = PolytopeBase + "Lowpoly_Environments/Prefabs/Mushrooms/PT_Caesars_Mushroom_01.prefab";
        string shrub = PolytopeBase + "Lowpoly_Environments/Prefabs/Shrubs/PT_Generic_Shrub_01_green.prefab";

        foreach (DistrictConfig d in districts)
        {
            float c = d.plazaHalfSize + 0.9f;
            Vector2[] decoSpots = { new Vector2(-c, c), new Vector2(c, -c), new Vector2(-c, -c), new Vector2(c, c) };
            for (int i = 0; i < decoSpots.Length; i++)
            {
                string p = i == 0 ? poppy : (i == 1 ? mushroom : shrub);
                SpawnPolytopeGrounded(p, DistrictLocalToWorld(d, decoSpots[i]), i * 45f, Vector3.one * 0.85f, parent);
            }
        }

        // 5. Pohon pinus hasil relokasi membingkai tepian luar desa
        string pine = PolytopeBase + "Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_green.prefab";
        Vector2[] pineOffsets =
        {
            new Vector2(-24f,  19f), new Vector2(-24f, -18f),
            new Vector2( 16f,  25f), new Vector2( 16f, -21f),
        };
        foreach (DistrictConfig d in districts)
        {
            for (int i = 0; i < pineOffsets.Length; i++)
            {
                SpawnPolytopeGrounded(pine, DistrictLocalToWorld(d, pineOffsets[i]), i * 59f, Vector3.one * 1.1f, parent);
            }
        }
    }

    private void SpawnOpenGateLine(DistrictConfig d, Vector2 localPos, float yaw, string gatePath, string fence1, string fence2, Transform parent)
    {
        GameObject gateGo = SpawnPolytopeGrounded(gatePath, DistrictLocalToWorld(d, localPos), yaw, Vector3.one, parent);
        if (gateGo != null)
        {
            Transform leftDoor = gateGo.transform.Find("PT_Modular_Gate_Wood_01_left");
            Transform rightDoor = gateGo.transform.Find("PT_Modular_Gate_Wood_01_right");
            if (leftDoor != null) leftDoor.localRotation = Quaternion.Euler(0f, -75f, 0f);
            if (rightDoor != null) rightDoor.localRotation = Quaternion.Euler(0f, 75f, 0f);
        }

        for (int i = 1; i <= 2; i++)
        {
            float zOff = 3.8f * i;
            SpawnPolytopeGrounded((i % 2 == 0) ? fence1 : fence2, DistrictLocalToWorld(d, localPos + new Vector2(0f, zOff)), yaw, Vector3.one, parent);
            SpawnPolytopeGrounded((i % 2 == 0) ? fence2 : fence1, DistrictLocalToWorld(d, localPos + new Vector2(0f, -zOff)), yaw, Vector3.one, parent);
        }
    }

    private void BuildEastFrontierLandmarks(Vector3 bridgePos, Transform parent)
    {
        string menhir = PolytopeBase + "Lowpoly_Environments/Prefabs/Rocks/PT_Menhir_Rock_02.prefab";
        string ore = PolytopeBase + "Lowpoly_Environments/Prefabs/Rocks/PT_Ore_Rock_01.prefab";

        Vector2 baseXZ = new Vector2(bridgePos.x + 22f, bridgePos.z);
        SpawnPolytopeGrounded(menhir, new Vector3(baseXZ.x, 0f, baseXZ.y + 8f), 25f, Vector3.one * 1.1f, parent);
        SpawnPolytopeGrounded(ore, new Vector3(baseXZ.x - 4f, 0f, baseXZ.y - 5f), 110f, Vector3.one, parent);
        SpawnPolytopeGrounded(ore, new Vector3(baseXZ.x + 6f, 0f, baseXZ.y + 3f), 40f, Vector3.one * 0.9f, parent);
    }

    private void ExpandWorldBoundaries(VillageBuildRecord record)
    {
        GameObject collidersRoot = GameObject.Find("colliders");
        if (collidersRoot == null) return;

        // Memperluas 4 dinding pembatas tak terlihat dari demo Environment_Free ke seluruh peta 9 Terrain (300x300m)
        Dictionary<string, (Vector3 pos, Vector3 scale)> targets = new Dictionary<string, (Vector3, Vector3)>
        {
            { "Cube",     (new Vector3( 50f, 15f,  220f), new Vector3(310f, 45f, 2f)) }, // Utara
            { "Cube (1)", (new Vector3( 50f, 15f,  -66f), new Vector3(310f, 45f, 2f)) }, // Selatan
            { "Cube (2)", (new Vector3(-93f, 15f,   77f), new Vector3(310f, 45f, 2f)) }, // Barat (rotY=90)
            { "Cube (3)", (new Vector3(193f, 15f,   77f), new Vector3(310f, 45f, 2f)) }, // Timur (rotY=90)
        };

        foreach (Transform child in collidersRoot.transform)
        {
            if (targets.TryGetValue(child.name, out var cfg))
            {
                record.boundaryTransforms.Add(child);
                record.originalBoundaryPositions.Add(child.position);
                record.originalBoundaryScales.Add(child.localScale);

                child.position = cfg.pos;
                child.localScale = cfg.scale;
            }
        }
    }

    private void PositionPlayer(VillageBuildRecord record, DistrictConfig centralTown)
    {
        if (!_movePlayerToVillage) return;
        GameObject player = GameObject.Find("Player");
        if (player == null) return;

        record.playerMoved = true;
        record.playerOriginalPosition = player.transform.position;
        record.playerOriginalRotation = player.transform.rotation;

        Vector3 spawnPos = DistrictLocalToWorld(centralTown, new Vector2(9.5f, 0f));
        spawnPos.y = Mathf.Max(centralTown.baseHeight, SampleWorldTerrainHeight(spawnPos)) + 1.15f;
        player.transform.position = spawnPos;

        Vector3 lookTarget = new Vector3(centralTown.centerXZ.x, spawnPos.y, centralTown.centerXZ.y);
        player.transform.rotation = Quaternion.LookRotation((lookTarget - spawnPos).normalized, Vector3.up);
    }

    // =========================================================================
    // RESTORE & HELPERS
    // =========================================================================
    private static void RestoreExistingBuild()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing == null) return;

        VillageBuildRecord record = existing.GetComponent<VillageBuildRecord>();
        if (record != null)
        {
            for (int i = 0; i < record.terrains.Count; i++)
            {
                Terrain t = record.terrains[i];
                TerrainData orig = i < record.originalTerrainData.Count ? record.originalTerrainData[i] : null;
                if (t != null && orig != null)
                {
                    t.terrainData = orig;
                    TerrainCollider tc = t.GetComponent<TerrainCollider>();
                    if (tc != null) tc.terrainData = orig;
                    t.Flush();
                }
            }

            foreach (GameObject go in record.disabledObjects)
            {
                if (go != null) go.SetActive(true);
            }

            if (record.playerMoved)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    player.transform.position = record.playerOriginalPosition;
                    player.transform.rotation = record.playerOriginalRotation;
                }
            }

            for (int i = 0; i < record.boundaryTransforms.Count; i++)
            {
                Transform bt = record.boundaryTransforms[i];
                if (bt != null)
                {
                    bt.position = record.originalBoundaryPositions[i];
                    bt.localScale = record.originalBoundaryScales[i];
                }
            }
        }

        DestroyImmediate(existing);
    }

    private void SpawnDistrictCurb(DistrictConfig d, Transform parent, Dictionary<string, Material> mats, Vector2 localOffset, float localYaw)
    {
        Vector3 p = DistrictLocalToWorld(d, localOffset);
        p.y = d.baseHeight + 0.02f;
        SpawnMegaKitPiece("Prop_ExteriorBorder_Straight1", p, Quaternion.Euler(0f, _villageYaw + localYaw, 0f), parent, mats, addCollider: false);
    }

    private void SpawnDistrictCurbCorner(DistrictConfig d, Transform parent, Dictionary<string, Material> mats, Vector2 localOffset, float localYaw)
    {
        Vector3 p = DistrictLocalToWorld(d, localOffset);
        p.y = d.baseHeight + 0.02f;
        SpawnMegaKitPiece("Prop_ExteriorBorder_Corner", p, Quaternion.Euler(0f, _villageYaw + localYaw, 0f), parent, mats, addCollider: false);
    }

    private static GameObject SpawnMegaKitPiece(
        string pieceName,
        Vector3 worldPos,
        Quaternion rotation,
        Transform parent,
        Dictionary<string, Material> mats,
        bool addCollider,
        Vector3? scale = null)
    {
        GameObject fbx = MegaKitPrefabAssembler.LoadPiece(pieceName);
        if (fbx == null) return null;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(fbx, parent);
        go.name = pieceName;
        Quaternion baseRot = go.transform.localRotation;
        go.transform.position = worldPos;
        go.transform.rotation = rotation * baseRot;
        if (scale.HasValue)
        {
            go.transform.localScale = Vector3.Scale(go.transform.localScale, scale.Value);
        }
        go.isStatic = true;

        MegaKitPrefabAssembler.ApplyMaterialsRecursive(go, mats);
        if (addCollider && go.GetComponentInChildren<Collider>() == null)
        {
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null) mf.gameObject.AddComponent<BoxCollider>();
            }
        }
        return go;
    }

    private static GameObject SpawnPolytopeGrounded(string assetPath, Vector3 xzPos, float yaw, Vector3 scale, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) return null;

        float y = SampleWorldTerrainHeight(xzPos);
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.transform.position = new Vector3(xzPos.x, y, xzPos.z);
        inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        inst.transform.localScale = scale;
        return inst;
    }

    private Vector3 DistrictLocalToWorld(DistrictConfig d, Vector2 localOffset)
    {
        Vector3 rotated = Quaternion.Euler(0f, _villageYaw, 0f) * new Vector3(localOffset.x, 0f, localOffset.y);
        return new Vector3(d.centerXZ.x + rotated.x, d.baseHeight, d.centerXZ.y + rotated.z);
    }

    private static bool IsNearAnyRoad(List<RoadSegment> roads, Vector2 wp2, float margin)
    {
        foreach (RoadSegment seg in roads)
        {
            if (DistancePointToSegment(wp2, seg.startWorld, seg.endWorld) <= seg.halfWidth + margin)
            {
                return true;
            }
        }
        return false;
    }

    private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
        return Vector2.Distance(p, a + ab * t);
    }

    private static float SampleWorldTerrainHeight(Vector3 worldPos)
    {
        foreach (Terrain t in Terrain.activeTerrains)
        {
            if (t == null || t.terrainData == null) continue;
            Bounds b = GetTerrainWorldBounds(t);
            if (worldPos.x >= b.min.x && worldPos.x <= b.max.x && worldPos.z >= b.min.z && worldPos.z <= b.max.z)
            {
                return t.SampleHeight(worldPos) + t.transform.position.y;
            }
        }
        return 3.0f;
    }

    private static Bounds GetTerrainWorldBounds(Terrain t)
    {
        Vector3 size = t.terrainData.size;
        Vector3 center = t.transform.position + size * 0.5f;
        return new Bounds(center, size);
    }

    private static float FindWaterLevel()
    {
        GameObject plane = GameObject.Find("Plane");
        if (plane != null) return plane.transform.position.y;
        return 2.5f;
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        return child;
    }

    private static float SmoothStep01(float edge0, float edge1, float x)
    {
        if (Mathf.Approximately(edge0, edge1)) return x >= edge1 ? 1f : 0f;
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    private static void EnsureFolderExists(string assetFolderPath)
    {
        string trimmed = assetFolderPath.TrimEnd('/');
        if (AssetDatabase.IsValidFolder(trimmed)) return;

        string[] parts = trimmed.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
