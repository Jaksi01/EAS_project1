using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menyiapkan material URP/Lit untuk Medieval Village MegaKit[Standard] dan
/// merangkai hampir seluruh potongan modular FBX menjadi 10 prefab bangunan utuh
/// (termasuk teras tangga batu, overhang lantai 2, skybridge, ornamen atap besi,
/// dan paviliun pasar) di Assets/Prefabs/Village/.
/// </summary>
public static class MegaKitPrefabAssembler
{
    public const string MegaKitRoot = "Assets/Medieval Village MegaKit[Standard]/";
    public const string FbxFolder = MegaKitRoot + "FBX/";
    public const string TextureFolder = MegaKitRoot + "Textures/";
    public const string NormalFolder = TextureFolder + "Normals Godot-Unity/";
    public const string MaterialFolder = MegaKitRoot + "Materials/";
    public const string OutputPrefabFolder = "Assets/Prefabs/Village/";

    private const float StoryHeight = 3.0f;

    private struct MaterialSpec
    {
        public string name;
        public string baseColorTex;
        public string normalTex;
        public Color tint;
        public float metallic;
        public float smoothness;
        public bool alphaClip;
        public bool emissiveWindow;
    }

    private static readonly MaterialSpec[] MaterialSpecs =
    {
        new MaterialSpec { name = "MI_Brick",          baseColorTex = "T_Brick_BaseColor.png",        normalTex = "T_Brick_Normal.png",        tint = Color.white,                        metallic = 0f,    smoothness = 0.20f },
        new MaterialSpec { name = "MI_RedBrick",       baseColorTex = "T_RedBrick_BaseColor.png",     normalTex = "T_Brick_Normal.png",        tint = Color.white,                        metallic = 0f,    smoothness = 0.22f },
        new MaterialSpec { name = "MI_UnevenBrick",    baseColorTex = "T_UnevenBrick_BaseColor.png",  normalTex = "T_UnevenBrick_Normal.png",  tint = Color.white,                        metallic = 0f,    smoothness = 0.20f },
        new MaterialSpec { name = "MI_Plaster",        baseColorTex = "T_Plaster_BaseColor.png",      normalTex = "T_Plaster_Normal.png",      tint = new Color(0.96f, 0.94f, 0.89f),     metallic = 0f,    smoothness = 0.15f },
        new MaterialSpec { name = "MI_RockTrim",       baseColorTex = "T_RockTrim_BaseColor.png",     normalTex = "T_RockTrim_Normal.png",     tint = Color.white,                        metallic = 0f,    smoothness = 0.25f },
        new MaterialSpec { name = "MI_RoundTiles",     baseColorTex = "T_RoundTiles_BaseColor.png",   normalTex = "T_RoundTiles_Normal.png",   tint = Color.white,                        metallic = 0f,    smoothness = 0.28f },
        new MaterialSpec { name = "MI_WoodTrim",       baseColorTex = "T_WoodTrim_BaseColor.png",     normalTex = "T_WoodTrim_Normal.png",     tint = Color.white,                        metallic = 0f,    smoothness = 0.25f },
        new MaterialSpec { name = "MI_WoodTrim_Wear",  baseColorTex = "T_WoodTrim_BaseColor.png",     normalTex = "T_WoodTrim_Normal.png",     tint = new Color(0.86f, 0.82f, 0.78f),     metallic = 0f,    smoothness = 0.20f },
        new MaterialSpec { name = "MI_Vine",           baseColorTex = "T_VineLeaf.png",               normalTex = null,                        tint = new Color(0.28f, 0.56f, 0.22f),     metallic = 0f,    smoothness = 0.25f, alphaClip = true },
        new MaterialSpec { name = "MI_MetalOrnaments", baseColorTex = null,                           normalTex = null,                        tint = new Color(0.18f, 0.20f, 0.23f),     metallic = 0.85f, smoothness = 0.55f },
        new MaterialSpec { name = "MI_WindowGlass",    baseColorTex = null,                           normalTex = null,                        tint = new Color(0.85f, 0.70f, 0.42f),     metallic = 0.10f, smoothness = 0.75f, emissiveWindow = true },
    };

    [MenuItem("Tools/Village Builder/1. Assemble MegaKit Materials & Building Prefabs")]
    public static void AssembleAllMenu()
    {
        Dictionary<string, Material> mats = EnsureMaterials();
        RemapFbxMaterials(mats);
        List<GameObject> prefabs = AssembleAllBuildingPrefabs(mats);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MegaKit] Berhasil menyiapkan {mats.Count} material URP dan {prefabs.Count} prefab bangunan utuh di '{OutputPrefabFolder}'.");
    }

    public static Dictionary<string, Material> EnsureMaterials()
    {
        EnsureFolderExists(MaterialFolder);
        FixNormalMapImporters();

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[MegaKit] Shader 'Universal Render Pipeline/Lit' tidak ditemukan.");
            return new Dictionary<string, Material>();
        }

        Dictionary<string, Material> mats = new Dictionary<string, Material>();
        foreach (MaterialSpec spec in MaterialSpecs)
        {
            string matPath = MaterialFolder + spec.name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != urpLit)
            {
                mat.shader = urpLit;
            }

            if (!string.IsNullOrEmpty(spec.baseColorTex))
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + spec.baseColorTex);
                if (tex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }

            if (!string.IsNullOrEmpty(spec.normalTex))
            {
                Texture2D nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalFolder + spec.normalTex);
                if (nrm == null) nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + spec.normalTex);
                if (nrm != null && mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", nrm);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", spec.tint);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", spec.metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", spec.smoothness);

            if (spec.alphaClip)
            {
                if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
                if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.35f);
                if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
                mat.EnableKeyword("_ALPHATEST_ON");
            }

            if (spec.emissiveWindow)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", new Color(0.55f, 0.38f, 0.14f) * 0.85f);
                }
            }

            EditorUtility.SetDirty(mat);
            mats[spec.name] = mat;
        }

        AssetDatabase.SaveAssets();
        return mats;
    }

    private static void FixNormalMapImporters()
    {
        if (!Directory.Exists(NormalFolder)) return;
        foreach (string file in Directory.GetFiles(NormalFolder, "*.png"))
        {
            string assetPath = file.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }
    }

    public static void RemapFbxMaterials(Dictionary<string, Material> mats)
    {
        if (mats == null || mats.Count == 0) return;
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { FbxFolder.TrimEnd('/') });
        int updated = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            var existingMap = importer.GetExternalObjectMap();
            bool changed = false;

            foreach (var kv in mats)
            {
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key);
                if (!existingMap.TryGetValue(id, out Object currentObj) || currentObj != kv.Value)
                {
                    importer.AddRemap(id, kv.Value);
                    changed = true;
                }
            }

            if (changed)
            {
                importer.SaveAndReimport();
                updated++;
            }
        }

        if (updated > 0)
        {
            Debug.Log($"[MegaKit] Remap material URP pada {updated} file FBX selesai.");
        }
    }

    public static List<GameObject> AssembleAllBuildingPrefabs(Dictionary<string, Material> mats)
    {
        EnsureFolderExists(OutputPrefabFolder);

        var result = new List<GameObject>
        {
            BuildCottageSmall(mats),
            BuildCottageTwoStory(mats),
            BuildTavernHall(mats),
            BuildBlacksmithShop(mats),
            BuildWatchtower(mats),
            BuildLonghouse(mats),
            BuildManorOverhang(mats),
            BuildSkybridgeComplex(mats),
            BuildBarnGranary(mats),
            BuildMarketPavilion(mats),
        };

        AssetDatabase.SaveAssets();
        return result;
    }

    public static GameObject LoadVillagePrefab(string prefabName)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabFolder + prefabName + ".prefab");
    }

    public static GameObject LoadPiece(string pieceName)
    {
        string path = FbxFolder + pieceName + ".fbx";
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
        {
            Debug.LogWarning($"[MegaKit] Piece FBX tidak ditemukan: {path}");
        }
        return asset;
    }

    public static void ApplyMaterialsRecursive(GameObject root, Dictionary<string, Material> mats)
    {
        if (root == null || mats == null) return;
        foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] shared = mr.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < shared.Length; i++)
            {
                if (shared[i] == null) continue;
                if (mats.TryGetValue(shared[i].name, out Material mapped))
                {
                    shared[i] = mapped;
                    changed = true;
                }
            }
            if (changed) mr.sharedMaterials = shared;
        }
    }

    // =========================================================================
    // 1. COTTAGE SMALL (4m x 6m)
    // =========================================================================
    private static GameObject BuildCottageSmall(Dictionary<string, Material> mats)
    {
        GameObject root = new GameObject("Village_Cottage_Small");
        const int w = 4, d = 6;

        AddFloorGrid(root.transform, w, d, 0f, "Floor_WoodDark");
        AddSkirtingBorder(root.transform, w, d, 0f, skipFrontCenter: true);

        AddDoorWall(root.transform, new Vector3(-1f, 0f, 3f), 0f, "Wall_Plaster_Door_Round", "DoorFrame_Round_Brick", "Door_2_Round");
        AddWindowWall(root.transform, new Vector3(1f, 0f, 3f), 0f, "Wall_Plaster_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");

        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-1f, 0f, -3f), 180f);
        AddWindowWall(root.transform, new Vector3(1f, 0f, -3f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");

        AddPiece(root.transform, "Wall_Plaster_Straight_L", new Vector3(2f, 0f, 2f), 90f);
        AddWindowWall(root.transform, new Vector3(2f, 0f, 0f), 90f, "Wall_Plaster_Window_Wide_Flat2", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");
        AddPiece(root.transform, "Wall_Plaster_Straight_R", new Vector3(2f, 0f, -2f), 90f);

        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-2f, 0f, 2f), -90f);
        AddWindowWall(root.transform, new Vector3(-2f, 0f, 0f), -90f, "Wall_UnevenBrick_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Closed");
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-2f, 0f, -2f), -90f);

        AddFourCorners(root.transform, w, d, 0f, "Corner_Exterior_Brick");

        AddGableRoof(root.transform, w, d, StoryHeight, "Roof_RoundTiles_4x6", "Roof_Front_Brick4");
        AddPiece(root.transform, "Roof_FrontSupports", new Vector3(0f, StoryHeight, 3f), 0f);
        AddPiece(root.transform, "Prop_Chimney", new Vector3(-1.1f, StoryHeight + 1.4f, -1.2f), 0f);
        AddPiece(root.transform, "Prop_Vine1", new Vector3(1.2f, 2.5f, 3.12f), 0f);
        AddPiece(root.transform, "Prop_Crate", new Vector3(1.3f, 0f, 3.85f), 15f);

        return FinalizeBuildingPrefab(root, mats);
    }

    // =========================================================================
    // 2. COTTAGE TWO STORY (4m x 6m + teras batu + dormer + balkon)
    // =========================================================================
    private static GameObject BuildCottageTwoStory(Dictionary<string, Material> mats)
    {
        GameObject root = new GameObject("Village_Cottage_TwoStory");
        const int w = 4, d = 6;
        const float plinthY = 1.0f;

        AddStonePlinthWithFrontStairs(root.transform, w, d, frontStairStartX: -2, frontStairEndX: 2);
        AddFloorGrid(root.transform, w, d, plinthY, "Floor_Brick");

        AddDoorWall(root.transform, new Vector3(-1f, plinthY, 3f), 0f, "Wall_UnevenBrick_Door_Flat", "DoorFrame_Flat_WoodDark", "Door_4_Flat");
        AddWindowWall(root.transform, new Vector3(1f, plinthY, 3f), 0f, "Wall_UnevenBrick_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");

        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-1f, plinthY, -3f), 180f);
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(1f, plinthY, -3f), 180f);

        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(2f, plinthY, 2f), 90f);
        AddWindowWall(root.transform, new Vector3(2f, plinthY, 0f), 90f, "Wall_UnevenBrick_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(2f, plinthY, -2f), 90f);

        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-2f, plinthY, 2f), -90f);
        AddWindowWall(root.transform, new Vector3(-2f, plinthY, 0f), -90f, "Wall_UnevenBrick_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Closed");
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-2f, plinthY, -2f), -90f);

        AddFourCorners(root.transform, w, d, plinthY, "Corner_ExteriorWide_Brick");

        float y2 = plinthY + StoryHeight;
        AddFloorGrid(root.transform, w, d, y2, "Floor_WoodLight");
        AddWallBottomCovers(root.transform, w, d, y2);

        AddDoorWall(root.transform, new Vector3(-1f, y2, 3f), 0f, "Wall_Plaster_Door_Flat", "DoorFrame_Flat_WoodDark", "Door_1_Flat");
        AddWindowWall(root.transform, new Vector3(1f, y2, 3f), 0f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");

        AddBalconyStrip(root.transform, -1f, y2, 3f, 0f, crossStyle: false);
        AddBalconyStrip(root.transform, 1f, y2, 3f, 0f, crossStyle: false);

        AddWindowWall(root.transform, new Vector3(-1f, y2, -3f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddPiece(root.transform, "Wall_Plaster_Straight", new Vector3(1f, y2, -3f), 180f);

        AddPiece(root.transform, "Wall_Plaster_Straight", new Vector3(2f, y2, 2f), 90f);
        AddWindowWall(root.transform, new Vector3(2f, y2, 0f), 90f, "Wall_Plaster_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");
        AddPiece(root.transform, "Wall_Plaster_Straight", new Vector3(2f, y2, -2f), 90f);

        AddPiece(root.transform, "Wall_Plaster_Straight", new Vector3(-2f, y2, 2f), -90f);
        AddWindowWall(root.transform, new Vector3(-2f, y2, 0f), -90f, "Wall_Plaster_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Closed");
        AddPiece(root.transform, "Wall_Plaster_Straight", new Vector3(-2f, y2, -2f), -90f);

        AddFourCorners(root.transform, w, d, y2, "Corner_Exterior_Wood");

        float yRoof = y2 + StoryHeight;
        AddGableRoof(root.transform, w, d, yRoof, "Roof_RoundTiles_4x6", "Roof_Front_Brick4");
        AddPiece(root.transform, "Roof_Dormer_RoundTile", new Vector3(0.85f, yRoof + 0.55f, 0f), 0f);
        AddPiece(root.transform, "Window_Roof_Thin", new Vector3(-1.2f, yRoof + 0.45f, 1.4f), 180f);
        AddPiece(root.transform, "Prop_Chimney2", new Vector3(-1.15f, yRoof + 1.4f, -1.5f), 0f);
        AddPiece(root.transform, "Prop_Vine5", new Vector3(2.05f, y2 + 0.2f, 1.4f), 90f);
        AddRoofRidgeOrnaments(root.transform, yRoof + 3.45f, d);

        return FinalizeBuildingPrefab(root, mats);
    }

    // =========================================================================
    // 3. TAVERN / GUILD HALL (6m x 8m, 2 lantai + balkon + ornamen atap)
    // =========================================================================
    private static GameObject BuildTavernHall(Dictionary<string, Material> mats)
    {
        GameObject root = new GameObject("Village_Tavern_Hall");
        const int w = 6, d = 8;

        AddFloorGrid(root.transform, w, d, 0f, "Floor_UnevenBrick");
        AddSkirtingBorder(root.transform, w, d, 0f, skipFrontCenter: true);

        AddWindowWall(root.transform, new Vector3(-2f, 0f, 4f), 0f, "Wall_UnevenBrick_Window_Wide_Round", "Window_Wide_Round1", "WindowShutters_Wide_Round_Open");
        AddDoorWall(root.transform, new Vector3(0f, 0f, 4f), 0f, "Wall_UnevenBrick_Door_Round", "DoorFrame_Round_Brick", "Door_8_Round");
        AddWindowWall(root.transform, new Vector3(2f, 0f, 4f), 0f, "Wall_UnevenBrick_Window_Wide_Round", "Window_Wide_Round1", "WindowShutters_Wide_Round_Open");

        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-2f, 0f, -4f), 180f);
        AddWindowWall(root.transform, new Vector3(0f, 0f, -4f), 180f, "Wall_UnevenBrick_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Closed");
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(2f, 0f, -4f), 180f);

        for (int z = -3; z <= 3; z += 2)
        {
            if (z == -1 || z == 1)
            {
                AddWindowWall(root.transform, new Vector3(3f, 0f, z), 90f, "Wall_UnevenBrick_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");
                AddWindowWall(root.transform, new Vector3(-3f, 0f, z), -90f, "Wall_UnevenBrick_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");
            }
            else
            {
                AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(3f, 0f, z), 90f);
                AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-3f, 0f, z), -90f);
            }
        }
        AddFourCorners(root.transform, w, d, 0f, "Corner_Exterior_Brick");

        float y2 = StoryHeight;
        AddFloorGrid(root.transform, w, d, y2, "Floor_WoodDark");
        AddPiece(root.transform, "Stair_Interior_Solid", new Vector3(1.8f, 0f, -1.0f), 0f);
        AddPiece(root.transform, "HoleCover_Straight", new Vector3(1.8f, y2, -1.0f), 0f);

        AddWindowWall(root.transform, new Vector3(-2f, y2, 4f), 0f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddDoorWall(root.transform, new Vector3(0f, y2, 4f), 0f, "Wall_Plaster_Door_Round", "DoorFrame_Round_WoodDark", "Door_1_Round");
        AddWindowWall(root.transform, new Vector3(2f, y2, 4f), 0f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");

        AddBalconyStrip(root.transform, -2f, y2, 4f, 0f, crossStyle: true);
        AddBalconyStrip(root.transform, 0f, y2, 4f, 0f, crossStyle: true);
        AddBalconyStrip(root.transform, 2f, y2, 4f, 0f, crossStyle: true);

        AddWindowWall(root.transform, new Vector3(-2f, y2, -4f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddPiece(root.transform, "Wall_Plaster_Straight", new Vector3(0f, y2, -4f), 180f);
        AddWindowWall(root.transform, new Vector3(2f, y2, -4f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");

        for (int z = -3; z <= 3; z += 2)
        {
            if (z == -1 || z == 1)
            {
                AddWindowWall(root.transform, new Vector3(3f, y2, z), 90f, "Wall_Plaster_Window_Wide_Round", "Window_Wide_Round1", "WindowShutters_Wide_Round_Open");
                AddWindowWall(root.transform, new Vector3(-3f, y2, z), -90f, "Wall_Plaster_Window_Wide_Round", "Window_Wide_Round1", "WindowShutters_Wide_Round_Open");
            }
            else
            {
                AddPiece(root.transform, "Wall_Plaster_Straight", new Vector3(3f, y2, z), 90f);
                AddPiece(root.transform, "Wall_Plaster_Straight", new Vector3(-3f, y2, z), -90f);
            }
        }
        AddFourCorners(root.transform, w, d, y2, "Corner_ExteriorWide_Wood");

        float yRoof = StoryHeight * 2f;
        AddGableRoof(root.transform, w, d, yRoof, "Roof_RoundTiles_6x8", "Roof_Front_Brick6");
        AddPiece(root.transform, "Roof_Dormer_RoundTile", new Vector3(1.8f, yRoof + 0.8f, 1.5f), 0f);
        AddPiece(root.transform, "Roof_Dormer_RoundTile", new Vector3(-1.8f, yRoof + 0.8f, -1.5f), 180f);
        AddPiece(root.transform, "Prop_Chimney", new Vector3(-1.6f, yRoof + 2.2f, 2.0f), 0f);
        AddPiece(root.transform, "Prop_Vine6", new Vector3(0f, 3.0f, 4.1f), 0f);
        AddPiece(root.transform, "Prop_Crate", new Vector3(-2.3f, 0f, 4.85f), 10f);
        AddPiece(root.transform, "Prop_Crate", new Vector3(2.3f, 0f, 4.85f), -22f);
        AddRoofRidgeOrnaments(root.transform, yRoof + 4.65f, d);

        return FinalizeBuildingPrefab(root, mats);
    }

    // =========================================================================
    // 4. BLACKSMITH / WORKSHOP (4m x 4m + kanopi genteng & cerobong tempa)
    // =========================================================================
    private static GameObject BuildBlacksmithShop(Dictionary<string, Material> mats)
    {
        GameObject root = new GameObject("Village_Blacksmith_Shop");
        const int w = 4, d = 4;

        AddFloorGrid(root.transform, w, d, 0f, "Floor_RedBrick");
        AddSkirtingBorder(root.transform, w, d, 0f, skipFrontCenter: true);

        AddDoorWall(root.transform, new Vector3(-1f, 0f, 2f), 0f, "Wall_UnevenBrick_Door_Flat", "DoorFrame_Flat_WoodDark", "Door_2_Flat");
        AddPiece(root.transform, "Wall_Plaster_WoodGrid", new Vector3(1f, 0f, 2f), 0f);
        AddPiece(root.transform, "Roof_RoundTile_2x1", new Vector3(1f, 2.3f, 2.1f), 0f, new Vector3(0.65f, 0.7f, 0.7f));

        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-1f, 0f, -2f), 180f);
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(1f, 0f, -2f), 180f);

        AddPiece(root.transform, "Wall_Plaster_WoodGrid", new Vector3(2f, 0f, 1f), 90f);
        AddWindowWall(root.transform, new Vector3(2f, 0f, -1f), 90f, "Wall_UnevenBrick_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");

        AddWindowWall(root.transform, new Vector3(-2f, 0f, 1f), -90f, "Wall_UnevenBrick_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-2f, 0f, -1f), -90f);

        AddFourCorners(root.transform, w, d, 0f, "Corner_Exterior_Brick");

        AddGableRoof(root.transform, w, d, StoryHeight, "Roof_RoundTiles_4x4", "Roof_Front_Brick4");
        AddPiece(root.transform, "Prop_Chimney", new Vector3(-1.2f, 0f, -1.2f), 0f, new Vector3(1.1f, 1.1f, 2.25f));
        AddPiece(root.transform, "Prop_Crate", new Vector3(1.5f, 0f, 2.9f), 28f);
        AddPiece(root.transform, "Prop_Crate", new Vector3(2.4f, 0f, 2.6f), -10f);

        return FinalizeBuildingPrefab(root, mats);
    }

    // =========================================================================
    // 5. WATCHTOWER (4m x 4m, 3 lantai + menara kerucut)
    // =========================================================================
    private static GameObject BuildWatchtower(Dictionary<string, Material> mats)
    {
        GameObject root = new GameObject("Village_Watchtower");
        const int w = 4, d = 4;

        AddFloorGrid(root.transform, w, d, 0f, "Floor_Brick");
        AddSkirtingBorder(root.transform, w, d, 0f, skipFrontCenter: true);

        AddDoorWall(root.transform, new Vector3(-1f, 0f, 2f), 0f, "Wall_UnevenBrick_Door_Round", "DoorFrame_Round_Brick", "Door_8_Round");
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(1f, 0f, 2f), 0f);
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-1f, 0f, -2f), 180f);
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(1f, 0f, -2f), 180f);
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(2f, 0f, 1f), 90f);
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(2f, 0f, -1f), 90f);
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-2f, 0f, 1f), -90f);
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-2f, 0f, -1f), -90f);
        AddFourCorners(root.transform, w, d, 0f, "Corner_Exterior_Brick");

        float y2 = StoryHeight;
        AddFloorGrid(root.transform, w, d, y2, "Floor_WoodDark");
        AddWindowWall(root.transform, new Vector3(-1f, y2, 2f), 0f, "Wall_UnevenBrick_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Closed");
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(1f, y2, 2f), 0f);
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-1f, y2, -2f), 180f);
        AddWindowWall(root.transform, new Vector3(1f, y2, -2f), 180f, "Wall_UnevenBrick_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Closed");
        AddWindowWall(root.transform, new Vector3(2f, y2, 1f), 90f, "Wall_UnevenBrick_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(2f, y2, -1f), 90f);
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-2f, y2, 1f), -90f);
        AddWindowWall(root.transform, new Vector3(-2f, y2, -1f), -90f, "Wall_UnevenBrick_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddFourCorners(root.transform, w, d, y2, "Corner_Exterior_Brick");

        float y3 = StoryHeight * 2f;
        AddFloorGrid(root.transform, w, d, y3, "Floor_WoodDark");
        AddWindowWall(root.transform, new Vector3(-1f, y3, 2f), 0f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddWindowWall(root.transform, new Vector3(1f, y3, 2f), 0f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddWindowWall(root.transform, new Vector3(-1f, y3, -2f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddWindowWall(root.transform, new Vector3(1f, y3, -2f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddWindowWall(root.transform, new Vector3(2f, y3, 1f), 90f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddWindowWall(root.transform, new Vector3(2f, y3, -1f), 90f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddWindowWall(root.transform, new Vector3(-2f, y3, 1f), -90f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddWindowWall(root.transform, new Vector3(-2f, y3, -1f), -90f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddFourCorners(root.transform, w, d, y3, "Corner_Exterior_Wood");

        AddPiece(root.transform, "Roof_Tower_RoundTiles", new Vector3(0f, StoryHeight * 3f, 0f), 0f);
        AddPiece(root.transform, "Prop_Vine4", new Vector3(1.4f, 4.5f, 2.08f), 0f);

        return FinalizeBuildingPrefab(root, mats);
    }

    // =========================================================================
    // 6. LONGHOUSE / BARRACKS (6m x 10m + Roof_Log + Roof_Support2 + Ornamen)
    // =========================================================================
    private static GameObject BuildLonghouse(Dictionary<string, Material> mats)
    {
        GameObject root = new GameObject("Village_Longhouse");
        const int w = 6, d = 10;

        AddFloorGrid(root.transform, w, d, 0f, "Floor_RedBrick");
        AddSkirtingBorder(root.transform, w, d, 0f, skipFrontCenter: true);

        AddWindowWall(root.transform, new Vector3(-2f, 0f, 5f), 0f, "Wall_Plaster_Window_Wide_Round", "Window_Wide_Round1", "WindowShutters_Wide_Round_Open");
        AddDoorWall(root.transform, new Vector3(0f, 0f, 5f), 0f, "Wall_Plaster_Door_RoundInset", "DoorFrame_Round_Brick", "Door_4_Round");
        AddWindowWall(root.transform, new Vector3(2f, 0f, 5f), 0f, "Wall_Plaster_Window_Wide_Round", "Window_Wide_Round1", "WindowShutters_Wide_Round_Open");

        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-2f, 0f, -5f), 180f);
        AddWindowWall(root.transform, new Vector3(0f, 0f, -5f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Closed");
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(2f, 0f, -5f), 180f);

        for (int z = -4; z <= 4; z += 2)
        {
            if (z == -2 || z == 2)
            {
                AddWindowWall(root.transform, new Vector3(3f, 0f, z), 90f, "Wall_Plaster_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");
                AddWindowWall(root.transform, new Vector3(-3f, 0f, z), -90f, "Wall_Plaster_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");
            }
            else if (z == 0)
            {
                AddPiece(root.transform, "Wall_Plaster_WoodGrid", new Vector3(3f, 0f, z), 90f);
                AddPiece(root.transform, "Wall_Plaster_Straight_Base", new Vector3(-3f, 0f, z), -90f);
            }
            else
            {
                AddPiece(root.transform, "Wall_Plaster_Straight_Base", new Vector3(3f, 0f, z), 90f);
                AddPiece(root.transform, "Wall_Plaster_Straight_Base", new Vector3(-3f, 0f, z), -90f);
            }
        }
        AddFourCorners(root.transform, w, d, 0f, "Corner_Exterior_Brick");

        AddGableRoof(root.transform, w, d, StoryHeight, "Roof_RoundTiles_6x10", "Roof_Front_Brick6");
        AddPiece(root.transform, "Roof_Log", new Vector3(0f, StoryHeight, 0f), 0f);
        AddPiece(root.transform, "Roof_Support2", new Vector3(3.05f, StoryHeight, 2f), 90f);
        AddPiece(root.transform, "Roof_Support2", new Vector3(3.05f, StoryHeight, -2f), 90f);
        AddPiece(root.transform, "Roof_Dormer_RoundTile", new Vector3(1.8f, StoryHeight + 0.8f, 2.0f), 0f);
        AddPiece(root.transform, "Roof_Dormer_RoundTile", new Vector3(1.8f, StoryHeight + 0.8f, -2.0f), 0f);
        AddPiece(root.transform, "Prop_Chimney2", new Vector3(-1.5f, StoryHeight + 2.2f, -2.8f), 0f);
        AddPiece(root.transform, "Prop_Vine2", new Vector3(-2.5f, 2.6f, 5.1f), 0f);
        AddRoofRidgeOrnaments(root.transform, StoryHeight + 4.65f, d);

        return FinalizeBuildingPrefab(root, mats);
    }

    // =========================================================================
    // 7. MANOR WITH OVERHANG & STONE TERRACE (6m x 8m + tangga batu + overhang)
    // =========================================================================
    private static GameObject BuildManorOverhang(Dictionary<string, Material> mats)
    {
        GameObject root = new GameObject("Village_Manor_Overhang");
        const int w = 6, d = 8;
        const float plinthY = 1.0f;

        AddStonePlinthWithFrontStairs(root.transform, w, d, frontStairStartX: -2, frontStairEndX: 2);
        AddFloorGrid(root.transform, w, d, plinthY, "Floor_Brick");

        AddWindowWall(root.transform, new Vector3(-2f, plinthY, 4f), 0f, "Wall_UnevenBrick_Window_Wide_Round", "Window_Wide_Round1", "WindowShutters_Wide_Round_Open");
        AddDoorWall(root.transform, new Vector3(0f, plinthY, 4f), 0f, "Wall_UnevenBrick_Door_Round", "DoorFrame_Round_Brick", "Door_8_Round");
        AddWindowWall(root.transform, new Vector3(2f, plinthY, 4f), 0f, "Wall_UnevenBrick_Window_Wide_Round", "Window_Wide_Round1", "WindowShutters_Wide_Round_Open");

        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-2f, plinthY, -4f), 180f);
        AddWindowWall(root.transform, new Vector3(0f, plinthY, -4f), 180f, "Wall_UnevenBrick_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(2f, plinthY, -4f), 180f);

        for (int z = -3; z <= 3; z += 2)
        {
            if (z == -1 || z == 1)
            {
                AddWindowWall(root.transform, new Vector3(3f, plinthY, z), 90f, "Wall_UnevenBrick_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");
                AddWindowWall(root.transform, new Vector3(-3f, plinthY, z), -90f, "Wall_UnevenBrick_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");
            }
            else
            {
                AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(3f, plinthY, z), 90f);
                AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(-3f, plinthY, z), -90f);
            }
        }
        AddFourCorners(root.transform, w, d, plinthY, "Corner_ExteriorWide_Brick");

        float y2 = plinthY + StoryHeight;
        AddFloorGrid(root.transform, w, d, y2, "Floor_WoodLight");
        AddWallBottomCovers(root.transform, w, d, y2);

        // Overhang depan (+Z = +4 -> +5.1m) memakai Overhang_Plaster_Short & Overhang_Roof_Plaster
        AddPiece(root.transform, "Overhang_Plaster_Short", new Vector3(-2f, y2, 4f), 0f);
        AddPiece(root.transform, "Overhang_Plaster_Short", new Vector3(0f, y2, 4f), 0f);
        AddPiece(root.transform, "Overhang_Plaster_Short", new Vector3(2f, y2, 4f), 0f);
        AddPiece(root.transform, "Overhang_Roof_Plaster", new Vector3(-2f, y2, 4.5f), 0f);
        AddPiece(root.transform, "Overhang_Roof_Plaster", new Vector3(0f, y2, 4.5f), 0f);
        AddPiece(root.transform, "Overhang_Roof_Plaster", new Vector3(2f, y2, 4.5f), 0f);

        AddPiece(root.transform, "Floor_WoodLight_OverhangCorner", new Vector3(-2f, y2, 4.6f), 0f);
        AddPiece(root.transform, "Floor_WoodLight_OverhangCorner2", new Vector3(3f, y2, 4.6f), 0f);
        AddBalconyStrip(root.transform, 0f, y2, 5f, 0f, crossStyle: true);

        AddWindowWall(root.transform, new Vector3(-2f, y2, -4f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddPiece(root.transform, "Wall_Plaster_Straight", new Vector3(0f, y2, -4f), 180f);
        AddWindowWall(root.transform, new Vector3(2f, y2, -4f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");

        for (int z = -3; z <= 3; z += 2)
        {
            if (z == -1 || z == 1)
            {
                AddWindowWall(root.transform, new Vector3(3f, y2, z), 90f, "Wall_Plaster_Window_Wide_Round", "Window_Wide_Round1", "WindowShutters_Wide_Round_Open");
                AddWindowWall(root.transform, new Vector3(-3f, y2, z), -90f, "Wall_Plaster_Window_Wide_Round", "Window_Wide_Round1", "WindowShutters_Wide_Round_Open");
            }
            else
            {
                AddPiece(root.transform, "Wall_Plaster_Straight", new Vector3(3f, y2, z), 90f);
                AddPiece(root.transform, "Wall_Plaster_Straight", new Vector3(-3f, y2, z), -90f);
            }
        }
        AddFourCorners(root.transform, w, d, y2, "Corner_Exterior_TopDown");

        float yRoof = y2 + StoryHeight;
        AddGableRoof(root.transform, w, d, yRoof, "Roof_RoundTiles_6x8", "Roof_Front_Brick6");
        AddPiece(root.transform, "Roof_Dormer_RoundTile", new Vector3(1.8f, yRoof + 0.8f, 1.5f), 0f);
        AddPiece(root.transform, "Window_Roof_Wide", new Vector3(-1.8f, yRoof + 0.65f, -1.5f), 180f);
        AddPiece(root.transform, "Prop_Chimney", new Vector3(-1.6f, yRoof + 2.2f, 2.0f), 0f);
        AddPiece(root.transform, "Prop_Vine9", new Vector3(3.15f, y2 + 0.5f, 0f), 90f);
        AddRoofRidgeOrnaments(root.transform, yRoof + 4.65f, d);

        return FinalizeBuildingPrefab(root, mats);
    }

    // =========================================================================
    // 8. SKYBRIDGE COMPLEX (2 bangunan 4x6 dihubungkan jembatan udara di Lantai 2)
    // =========================================================================
    private static GameObject BuildSkybridgeComplex(Dictionary<string, Material> mats)
    {
        GameObject root = new GameObject("Village_Skybridge_Complex");
        const float plinthY = 1.0f;

        AddStonePlinthWithFrontStairs(root.transform, 12, 6, frontStairStartX: -4, frontStairEndX: 2);

        BuildWing4x6(root.transform, offsetX: -4f, plinthY: plinthY, twoStory: true, isLeftWing: true);
        BuildWing4x6(root.transform, offsetX: 4f, plinthY: plinthY, twoStory: true, isLeftWing: false);

        float sbY = plinthY + StoryHeight;
        AddPiece(root.transform, "Floor_WoodDark", new Vector3(-1f, sbY, 0f), 0f);
        AddPiece(root.transform, "Floor_WoodDark", new Vector3(1f, sbY, 0f), 0f);

        AddWindowWall(root.transform, new Vector3(-1f, sbY, 1f), 0f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddWindowWall(root.transform, new Vector3(1f, sbY, 1f), 0f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddWindowWall(root.transform, new Vector3(-1f, sbY, -1f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
        AddWindowWall(root.transform, new Vector3(1f, sbY, -1f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");

        AddPiece(root.transform, "Wall_Arch", new Vector3(-1f, plinthY, 1f), 0f);
        AddPiece(root.transform, "Wall_Arch", new Vector3(1f, plinthY, 1f), 0f);
        AddPiece(root.transform, "Wall_Arch", new Vector3(-1f, plinthY, -1f), 180f);
        AddPiece(root.transform, "Wall_Arch", new Vector3(1f, plinthY, -1f), 180f);

        AddPiece(root.transform, "Roof_2x4_RoundTile", new Vector3(0f, sbY + StoryHeight, 0f), 90f);
        AddPiece(root.transform, "Roof_Front_Brick2", new Vector3(-2f, sbY + StoryHeight, 0f), -90f);
        AddPiece(root.transform, "Roof_Front_Brick2", new Vector3(2f, sbY + StoryHeight, 0f), 90f);

        return FinalizeBuildingPrefab(root, mats);
    }

    private static void BuildWing4x6(Transform parent, float offsetX, float plinthY, bool twoStory, bool isLeftWing)
    {
        GameObject wing = new GameObject(isLeftWing ? "Wing_Left" : "Wing_Right");
        wing.transform.SetParent(parent);
        wing.transform.localPosition = new Vector3(offsetX, 0f, 0f);

        const int w = 4, d = 6;
        AddFloorGrid(wing.transform, w, d, plinthY, "Floor_UnevenBrick");

        AddDoorWall(wing.transform, new Vector3(isLeftWing ? -1f : 1f, plinthY, 3f), 0f, "Wall_UnevenBrick_Door_Flat", "DoorFrame_Flat_Brick", isLeftWing ? "Door_2_Flat" : "Door_8_Flat");
        AddWindowWall(wing.transform, new Vector3(isLeftWing ? 1f : -1f, plinthY, 3f), 0f, "Wall_UnevenBrick_Window_Wide_Round", "Window_Wide_Round1", "WindowShutters_Wide_Round_Open");
        AddPiece(wing.transform, "Wall_UnevenBrick_Straight", new Vector3(-1f, plinthY, -3f), 180f);
        AddPiece(wing.transform, "Wall_UnevenBrick_Straight", new Vector3(1f, plinthY, -3f), 180f);

        for (int z = -2; z <= 2; z += 2)
        {
            AddPiece(wing.transform, z == 0 ? "Wall_Plaster_WoodGrid" : "Wall_UnevenBrick_Straight", new Vector3(2f, plinthY, z), 90f);
            AddPiece(wing.transform, z == 0 ? "Wall_Plaster_WoodGrid" : "Wall_UnevenBrick_Straight", new Vector3(-2f, plinthY, z), -90f);
        }
        AddFourCorners(wing.transform, w, d, plinthY, "Corner_Exterior_Brick");

        if (twoStory)
        {
            float y2 = plinthY + StoryHeight;
            AddFloorGrid(wing.transform, w, d, y2, "Floor_WoodDark");

            AddDoorWall(wing.transform, new Vector3(-1f, y2, 3f), 0f, "Wall_Plaster_Door_Flat", "DoorFrame_Flat_WoodDark", "Door_1_Flat");
            AddWindowWall(wing.transform, new Vector3(1f, y2, 3f), 0f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
            if (isLeftWing)
            {
                AddBalconyStrip(wing.transform, -1f, y2, 3f, 0f, crossStyle: false);
                AddBalconyStrip(wing.transform, 1f, y2, 3f, 0f, crossStyle: false);
            }

            AddWindowWall(wing.transform, new Vector3(-1f, y2, -3f), 180f, "Wall_Plaster_Window_Thin_Round", "Window_Thin_Round1", "WindowShutters_Thin_Round_Open");
            AddPiece(wing.transform, "Wall_Plaster_Straight", new Vector3(1f, y2, -3f), 180f);

            for (int z = -2; z <= 2; z += 2)
            {
                bool innerSideRight = isLeftWing && z == 0;
                bool innerSideLeft = !isLeftWing && z == 0;
                if (!innerSideRight) AddPiece(wing.transform, "Wall_Plaster_Straight", new Vector3(2f, y2, z), 90f);
                if (!innerSideLeft) AddPiece(wing.transform, "Wall_Plaster_Straight", new Vector3(-2f, y2, z), -90f);
            }
            AddFourCorners(wing.transform, w, d, y2, "Corner_Exterior_TopOnly");

            float yRoof = y2 + StoryHeight;
            AddGableRoof(wing.transform, w, d, yRoof, "Roof_RoundTiles_4x6", "Roof_Front_Brick4");
            AddPiece(wing.transform, "Prop_Chimney", new Vector3(isLeftWing ? -1.1f : 1.1f, yRoof + 1.4f, -1.2f), 0f);
            AddRoofRidgeOrnaments(wing.transform, yRoof + 3.45f, d);
        }
    }

    // =========================================================================
    // 9. BARN / GRANARY (8m x 10m — memakai Roof_RoundTiles_8x10 + Roof_Front_Brick8)
    // =========================================================================
    private static GameObject BuildBarnGranary(Dictionary<string, Material> mats)
    {
        GameObject root = new GameObject("Village_Barn_Granary");
        const int w = 8, d = 10;

        AddFloorGrid(root.transform, w, d, 0f, "Floor_WoodLight");
        AddSkirtingBorder(root.transform, w, d, 0f, skipFrontCenter: true);

        AddPiece(root.transform, "Wall_Plaster_Straight_Base", new Vector3(-3f, 0f, 5f), 0f);
        AddWindowWall(root.transform, new Vector3(-1f, 0f, 5f), 0f, "Wall_Plaster_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Open");
        AddDoorWall(root.transform, new Vector3(1f, 0f, 5f), 0f, "Wall_UnevenBrick_Door_Flat", "DoorFrame_Flat_WoodDark", "Door_4_Flat");
        AddPiece(root.transform, "Wall_Plaster_WoodGrid", new Vector3(3f, 0f, 5f), 0f);

        for (int x = -3; x <= 3; x += 2)
        {
            AddPiece(root.transform, "Wall_UnevenBrick_Straight", new Vector3(x, 0f, -5f), 180f);
        }

        for (int z = -4; z <= 4; z += 2)
        {
            if (z == -2 || z == 2)
            {
                AddWindowWall(root.transform, new Vector3(4f, 0f, z), 90f, "Wall_Plaster_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Closed");
                AddWindowWall(root.transform, new Vector3(-4f, 0f, z), -90f, "Wall_Plaster_Window_Wide_Flat", "Window_Wide_Flat1", "WindowShutters_Wide_Flat_Closed");
            }
            else
            {
                AddPiece(root.transform, "Wall_Plaster_Straight_Base", new Vector3(4f, 0f, z), 90f);
                AddPiece(root.transform, "Wall_Plaster_Straight_Base", new Vector3(-4f, 0f, z), -90f);
            }
        }
        AddFourCorners(root.transform, w, d, 0f, "Corner_ExteriorWide_Wood");

        AddPiece(root.transform, "Stair_Interior_Rails", new Vector3(-2.5f, 0f, 1.0f), 0f);
        AddPiece(root.transform, "HoleCover_90Angle", new Vector3(-2.5f, StoryHeight, 1.0f), 0f);

        AddGableRoof(root.transform, w, d, StoryHeight, "Roof_RoundTiles_8x10", "Roof_Front_Brick8");
        AddPiece(root.transform, "Roof_Modular_RoundTiles", new Vector3(0f, StoryHeight + 5.7f, 0f), 0f);
        AddPiece(root.transform, "Prop_Chimney2", new Vector3(2.2f, StoryHeight + 2.8f, -2.5f), 0f);
        AddPiece(root.transform, "Prop_Vine2", new Vector3(-3.3f, 2.5f, 5.12f), 0f);
        AddPiece(root.transform, "Prop_Crate", new Vector3(2.8f, 0f, 5.8f), 12f);
        AddPiece(root.transform, "Prop_Crate", new Vector3(2.8f, 1.06f, 5.8f), -18f);

        return FinalizeBuildingPrefab(root, mats);
    }

    // =========================================================================
    // 10. MARKET PAVILION / KIOS PASAR (4m x 6m terbuka dengan atap kayu modular)
    // =========================================================================
    private static GameObject BuildMarketPavilion(Dictionary<string, Material> mats)
    {
        GameObject root = new GameObject("Village_Market_Pavilion");
        const int w = 4, d = 6;

        AddFloorGrid(root.transform, w, d, 0f, "Floor_Brick");

        AddPiece(root.transform, "Wall_Arch", new Vector3(-1f, 0f, 3f), 0f);
        AddPiece(root.transform, "Wall_Arch", new Vector3(1f, 0f, 3f), 0f);
        AddPiece(root.transform, "Wall_Plaster_Straight_Base", new Vector3(-1f, 0f, -3f), 180f);
        AddPiece(root.transform, "Wall_Plaster_Straight_Base", new Vector3(1f, 0f, -3f), 180f);

        for (int z = -2; z <= 2; z += 2)
        {
            AddPiece(root.transform, z == 2 ? "Wall_Arch" : "Wall_Plaster_WoodGrid", new Vector3(2f, 0f, z), 90f);
            AddPiece(root.transform, z == 2 ? "Wall_Arch" : "Wall_Plaster_WoodGrid", new Vector3(-2f, 0f, z), -90f);
        }
        AddFourCorners(root.transform, w, d, 0f, "Corner_Exterior_Wood");

        // Atap kayu modular simetris menutup penuh z[-3.0, +3.0]
        AddPiece(root.transform, "Roof_Wooden_2x1_L", new Vector3(-1f, StoryHeight, 1.5f), 0f);
        AddPiece(root.transform, "Roof_Wooden_2x1_R", new Vector3(1f, StoryHeight, 1.5f), 0f);
        AddPiece(root.transform, "Roof_Wooden_2x1_Center_Mirror", new Vector3(-1f, StoryHeight, -1.5f), 0f);
        AddPiece(root.transform, "Roof_Wooden_2x1_Center_Mirror", new Vector3(1f, StoryHeight, -1.5f), 0f);
        AddPiece(root.transform, "Roof_Wooden_2x1_Middle", new Vector3(-1f, StoryHeight, 0f), 0f, new Vector3(1f, 1.5f, 1f));
        AddPiece(root.transform, "Roof_Wooden_2x1_Middle", new Vector3(1f, StoryHeight, 0f), 0f, new Vector3(1f, 1.5f, 1f));

        AddPiece(root.transform, "Prop_Crate", new Vector3(-1.1f, 0f, 1.5f), 8f);
        AddPiece(root.transform, "Prop_Crate", new Vector3(1.1f, 0f, 1.5f), -14f);
        AddPiece(root.transform, "Prop_Crate", new Vector3(1.1f, 0f, -1.2f), 25f);

        return FinalizeBuildingPrefab(root, mats);
    }

    // =========================================================================
    // MODULAR SNAPPING HELPERS
    // =========================================================================
    private static void AddFloorGrid(Transform parent, int width, int depth, float y, string floorPiece)
    {
        for (int x = -width / 2 + 1; x <= width / 2 - 1; x += 2)
        {
            for (int z = -depth / 2 + 1; z <= depth / 2 - 1; z += 2)
            {
                AddPiece(parent, floorPiece, new Vector3(x, y, z), 0f);
            }
        }
    }

    private static void AddStonePlinthWithFrontStairs(Transform parent, int width, int depth, int frontStairStartX, int frontStairEndX)
    {
        float frontZ = depth * 0.5f;
        for (int x = -width / 2 + 1; x <= width / 2 - 1; x += 2)
        {
            for (int z = -depth / 2 + 1; z <= depth / 2 - 1; z += 2)
            {
                bool onEdge = (x == -width / 2 + 1) || (x == width / 2 - 1) || (z == -depth / 2 + 1) || (z == depth / 2 - 1);
                AddPiece(parent, onEdge ? "Stairs_Exterior_Sides" : "Stairs_Exterior_Platform", new Vector3(x, 0f, z), 0f);
            }
        }

        for (int x = frontStairStartX + 1; x <= frontStairEndX - 1; x += 2)
        {
            string stairPiece = "Stairs_Exterior_Straight_Center";
            if (x == frontStairStartX + 1 && x == frontStairEndX - 1) stairPiece = "Stairs_Exterior_Straight";
            else if (x == frontStairStartX + 1) stairPiece = "Stairs_Exterior_Straight_R";
            else if (x == frontStairEndX - 1) stairPiece = "Stairs_Exterior_Straight_L";

            AddPiece(parent, stairPiece, new Vector3(x, 0f, frontZ + 1f), 180f);
        }
    }

    private static void AddSkirtingBorder(Transform parent, int width, int depth, float y, bool skipFrontCenter)
    {
        float hx = width * 0.5f;
        float hz = depth * 0.5f;

        for (int x = -width / 2 + 1; x <= width / 2 - 1; x += 2)
        {
            if (!(skipFrontCenter && x <= 0 && x >= -1))
            {
                AddPiece(parent, "Prop_ExteriorBorder_Straight1", new Vector3(x, y, hz), 0f);
            }
            AddPiece(parent, "Prop_ExteriorBorder_Straight2", new Vector3(x, y, -hz), 180f);
        }

        for (int z = -depth / 2 + 1; z <= depth / 2 - 1; z += 2)
        {
            AddPiece(parent, "Prop_ExteriorBorder_Straight1", new Vector3(hx, y, z), 90f);
            AddPiece(parent, "Prop_ExteriorBorder_Straight2", new Vector3(-hx, y, z), -90f);
        }

        AddPiece(parent, "Prop_ExteriorBorder_Corner", new Vector3(-hx, y, hz), 0f);
        AddPiece(parent, "Prop_ExteriorBorder_Corner", new Vector3(hx, y, hz), 90f);
        AddPiece(parent, "Prop_ExteriorBorder_Corner", new Vector3(hx, y, -hz), 180f);
        AddPiece(parent, "Prop_ExteriorBorder_Corner", new Vector3(-hx, y, -hz), -90f);
    }

    private static void AddWallBottomCovers(Transform parent, int width, int depth, float y)
    {
        float hx = width * 0.5f;
        float hz = depth * 0.5f;
        for (int x = -width / 2 + 1; x <= width / 2 - 1; x += 2)
        {
            AddPiece(parent, "Wall_BottomCover", new Vector3(x, y, hz), 0f);
            AddPiece(parent, "Wall_BottomCover", new Vector3(x, y, -hz), 180f);
        }
        for (int z = -depth / 2 + 1; z <= depth / 2 - 1; z += 2)
        {
            AddPiece(parent, "Wall_BottomCover", new Vector3(hx, y, z), 90f);
            AddPiece(parent, "Wall_BottomCover", new Vector3(-hx, y, z), -90f);
        }
    }

    private static void AddFourCorners(Transform parent, int width, int depth, float y, string cornerPiece)
    {
        float hx = width * 0.5f;
        float hz = depth * 0.5f;
        AddPiece(parent, cornerPiece, new Vector3(-hx, y, hz), 0f);
        AddPiece(parent, cornerPiece, new Vector3(hx, y, hz), 90f);
        AddPiece(parent, cornerPiece, new Vector3(hx, y, -hz), 180f);
        AddPiece(parent, cornerPiece, new Vector3(-hx, y, -hz), -90f);
    }

    private static void AddDoorWall(Transform parent, Vector3 pos, float rotY, string wallPiece, string framePiece, string doorPiece)
    {
        AddPiece(parent, wallPiece, pos, rotY);
        AddPiece(parent, framePiece, pos, rotY);

        GameObject doorGo = AddPiece(parent, doorPiece, pos, rotY);
        if (doorGo != null)
        {
            Vector3 localShift = Quaternion.Euler(0f, rotY, 0f) * new Vector3(-0.51f, 0f, -0.05f);
            doorGo.transform.localPosition += localShift;
        }
    }

    private static void AddWindowWall(Transform parent, Vector3 pos, float rotY, string wallPiece, string windowPiece, string shutterPiece)
    {
        AddPiece(parent, wallPiece, pos, rotY);
        AddPiece(parent, windowPiece, pos, rotY);
        if (!string.IsNullOrEmpty(shutterPiece))
        {
            AddPiece(parent, shutterPiece, pos, rotY);
        }
    }

    private static void AddBalconyStrip(Transform parent, float x, float y, float z, float rotY, bool crossStyle)
    {
        AddPiece(parent, "Floor_WoodDark_Half3", new Vector3(x, y, z), rotY + 180f);
        AddPiece(parent, crossStyle ? "Balcony_Cross_Straight" : "Balcony_Simple_Straight", new Vector3(x, y, z), rotY);
        Vector3 supLeft = Quaternion.Euler(0f, rotY, 0f) * new Vector3(-0.85f, 0f, 0f);
        Vector3 supRight = Quaternion.Euler(0f, rotY, 0f) * new Vector3(0.85f, 0f, 0f);
        AddPiece(parent, "Prop_Support", new Vector3(x, y - StoryHeight, z) + supLeft, rotY, new Vector3(1f, 0.55f, 1f));
        AddPiece(parent, "Prop_Support", new Vector3(x, y - StoryHeight, z) + supRight, rotY, new Vector3(1f, 0.55f, 1f));
    }

    private static void AddGableRoof(Transform parent, int width, int depth, float roofY, string roofPiece, string frontBrickPiece)
    {
        AddPiece(parent, roofPiece, new Vector3(0f, roofY, 0f), 0f);
        AddPiece(parent, frontBrickPiece, new Vector3(0f, roofY, depth * 0.5f), 0f);
        AddPiece(parent, frontBrickPiece, new Vector3(0f, roofY, -depth * 0.5f), 180f);
    }

    private static void AddRoofRidgeOrnaments(Transform parent, float ridgeY, int depth)
    {
        for (int z = -depth / 2 + 1; z <= depth / 2 - 1; z += 2)
        {
            AddPiece(parent, "Prop_MetalFence_Ornament", new Vector3(0f, ridgeY, z), 90f, new Vector3(1f, 1f, 0.28f));
        }
    }

    private static GameObject AddPiece(Transform parent, string pieceName, Vector3 localPos, float rotY)
    {
        return AddPiece(parent, pieceName, localPos, rotY, Vector3.one);
    }

    private static GameObject AddPiece(Transform parent, string pieceName, Vector3 localPos, float rotY, Vector3 scale)
    {
        GameObject fbx = LoadPiece(pieceName);
        if (fbx == null) return null;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(fbx, parent);
        go.name = pieceName;
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f) * go.transform.localRotation;
        go.transform.localScale = Vector3.Scale(go.transform.localScale, scale);
        return go;
    }

    private static GameObject FinalizeBuildingPrefab(GameObject root, Dictionary<string, Material> mats)
    {
        ApplyMaterialsRecursive(root, mats);

        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            GameObject go = mf.gameObject;
            string n = go.name;
            go.isStatic = true;

            if (n.StartsWith("Prop_Vine")) continue;

            if (go.GetComponent<Collider>() == null && mf.sharedMesh != null)
            {
                if (n.StartsWith("Prop_Crate") || n.StartsWith("Prop_WoodenFence") || n.StartsWith("Door_"))
                {
                    go.AddComponent<BoxCollider>();
                }
                else
                {
                    go.AddComponent<MeshCollider>();
                }
            }
        }

        string prefabPath = OutputPrefabFolder + root.name + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return saved;
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
