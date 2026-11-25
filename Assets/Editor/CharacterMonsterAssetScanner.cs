using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Memindai seluruh aset karakter, monster, dan animasi di proyek,
/// mengatur BakeAxisConversion + Humanoid Rig (sesuai panduan Quaternius),
/// menyiapkan material URP/Lit + Normal + Emissive beserta varian warna,
/// dan menulis laporan ke Assets/Generated/AssetInventoryReport.txt.
/// </summary>
public static class CharacterMonsterAssetScanner
{
    private const string OutfitsFolder = "Assets/Modular Character Outfits - Fantasy[Standard]";
    private const string BaseCharsFolder = "Assets/Universal Base Characters[Standard]";
    private const string BestiaryFolder = "Assets/Bestiary - Dungeon Monsters Kit[Standard]";
    private const string ReportPath = "Assets/Generated/AssetInventoryReport.txt";

    [MenuItem("Tools/Project EAS/1. Scan & Setup Character, Monster & Animation Assets")]
    public static void ScanAndSetupAll()
    {
        AssetDatabase.Refresh();
        EnsureFolderExists("Assets/Generated");
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== PROJECT EAS ASSET INVENTORY REPORT ===");
        sb.AppendLine();

        SetupUniversalBaseCharacters(sb);
        SetupCharacterOutfits(sb);
        SetupBestiaryMonsters(sb);
        ScanAllAnimationsInAssets(sb);
        ScanAdditionalCharacterModels(sb);

        File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[EAS Setup] Scan & setup karakter/monster selesai! Cek '{ReportPath}' dan Console.");
    }

    private static void SetupCharacterOutfits(StringBuilder sb)
    {
        sb.AppendLine($"## CHARACTER OUTFITS: {OutfitsFolder}");
        if (!Directory.Exists(OutfitsFolder))
        {
            sb.AppendLine("  [FOLDER TIDAK DITEMUKAN]");
            sb.AppendLine();
            return;
        }

        string matDir = OutfitsFolder + "/Materials";
        EnsureFolderExists(matDir);

        // Pastikan normal map bertipe NormalMap (gunakan versi Unity/OpenGL, bukan UnrealEngine)
        FixNormalImporter(OutfitsFolder + "/Textures/Peasant/T_Peasant_Normal.png");
        FixNormalImporter(OutfitsFolder + "/Textures/Ranger/T_Ranger_Normal.png");
        FixNormalImporter(OutfitsFolder + "/Textures/Base/T_Regular_Male_Normal.png");
        FixNormalImporter(OutfitsFolder + "/Textures/Base/T_Regular_Female_Normal.png");

        var mats = new Dictionary<string, Material>
        {
            ["MI_Peasant"] = CreateOrUpdateUrpMat(matDir + "/MI_Peasant.mat",
                OutfitsFolder + "/Textures/Peasant/T_Peasant_BaseColor.png",
                OutfitsFolder + "/Textures/Peasant/T_Peasant_Normal.png", null),
            ["MI_Peasant_Alt"] = CreateOrUpdateUrpMat(matDir + "/MI_Peasant_Alt.mat",
                OutfitsFolder + "/Textures/Peasant/T_Peasant_2_BaseColor.png",
                OutfitsFolder + "/Textures/Peasant/T_Peasant_Normal.png", null),
            ["MI_Ranger"] = CreateOrUpdateUrpMat(matDir + "/MI_Ranger.mat",
                OutfitsFolder + "/Textures/Ranger/T_Ranger_BaseColor.png",
                OutfitsFolder + "/Textures/Ranger/T_Ranger_Normal.png", null),
            ["MI_Ranger_Alt"] = CreateOrUpdateUrpMat(matDir + "/MI_Ranger_Alt.mat",
                OutfitsFolder + "/Textures/Ranger/T_Ranger_3_BaseColor.png",
                OutfitsFolder + "/Textures/Ranger/T_Ranger_Normal.png", null),
            ["MI_Regular_Male"] = CreateOrUpdateUrpMat(matDir + "/MI_Regular_Male.mat",
                OutfitsFolder + "/Textures/Base/T_Regular_Male_Dark_BaseColor.png",
                OutfitsFolder + "/Textures/Base/T_Regular_Male_Normal.png", null),
            ["MI_Regular_Female"] = CreateOrUpdateUrpMat(matDir + "/MI_Regular_Female.mat",
                OutfitsFolder + "/Textures/Base/T_Regular_Female_Dark_BaseColor.png",
                OutfitsFolder + "/Textures/Base/T_Regular_Female_Normal.png", null),
        };

        string fbxRoot = OutfitsFolder + "/Exports/FBX (Unity)";
        if (!Directory.Exists(fbxRoot)) fbxRoot = OutfitsFolder;

        ConfigureModelsInFolder(sb, fbxRoot, mats, bakeAxis: true, humanoid: true);
    }

    private static void SetupBestiaryMonsters(StringBuilder sb)
    {
        sb.AppendLine($"## BESTIARY MONSTERS: {BestiaryFolder}");
        if (!Directory.Exists(BestiaryFolder))
        {
            sb.AppendLine("  [FOLDER TIDAK DITEMUKAN]");
            sb.AppendLine();
            return;
        }

        string matDir = BestiaryFolder + "/Materials";
        EnsureFolderExists(matDir);

        FixNormalImporter(BestiaryFolder + "/Textures/T_Imp_Normal.png");
        FixNormalImporter(BestiaryFolder + "/Textures/T_Puglin_Normal.png");

        var mats = new Dictionary<string, Material>
        {
            ["MI_Imp"] = CreateOrUpdateUrpMat(matDir + "/MI_Imp_1.mat",
                BestiaryFolder + "/Textures/T_Imp_BaseColor_1.png",
                BestiaryFolder + "/Textures/T_Imp_Normal.png",
                BestiaryFolder + "/Textures/T_Imp_Emissive.png"),
            ["MI_Imp_2"] = CreateOrUpdateUrpMat(matDir + "/MI_Imp_2.mat",
                BestiaryFolder + "/Textures/T_Imp_BaseColor_2.png",
                BestiaryFolder + "/Textures/T_Imp_Normal.png",
                BestiaryFolder + "/Textures/T_Imp_Emissive.png"),
            ["MI_Imp_3"] = CreateOrUpdateUrpMat(matDir + "/MI_Imp_3.mat",
                BestiaryFolder + "/Textures/T_Imp_BaseColor_3.png",
                BestiaryFolder + "/Textures/T_Imp_Normal.png",
                BestiaryFolder + "/Textures/T_Imp_Emissive.png"),

            ["MI_Puglin"] = CreateOrUpdateUrpMat(matDir + "/MI_Puglin_1.mat",
                BestiaryFolder + "/Textures/T_Puglin_BaseColor_1.png",
                BestiaryFolder + "/Textures/T_Puglin_Normal.png",
                BestiaryFolder + "/Textures/T_Puglin_Emissive.png"),
            ["MI_Puglin_2"] = CreateOrUpdateUrpMat(matDir + "/MI_Puglin_2.mat",
                BestiaryFolder + "/Textures/T_Puglin_BaseColor_2.png",
                BestiaryFolder + "/Textures/T_Puglin_Normal.png",
                BestiaryFolder + "/Textures/T_Puglin_Emissive.png"),
            ["MI_Puglin_3"] = CreateOrUpdateUrpMat(matDir + "/MI_Puglin_3.mat",
                BestiaryFolder + "/Textures/T_Puglin_BaseColor_3.png",
                BestiaryFolder + "/Textures/T_Puglin_Normal.png",
                BestiaryFolder + "/Textures/T_Puglin_Emissive.png"),
        };

        string fbxRoot = BestiaryFolder + "/Exports/FBX (Unity)";
        if (!Directory.Exists(fbxRoot)) fbxRoot = BestiaryFolder;

        ConfigureModelsInFolder(sb, fbxRoot, mats, bakeAxis: true, humanoid: true);
    }

    private static void ConfigureModelsInFolder(
        StringBuilder sb,
        string searchRoot,
        Dictionary<string, Material> mats,
        bool bakeAxis,
        bool humanoid)
    {
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { searchRoot });
        sb.AppendLine($"  Models ({modelGuids.Length}) in '{searchRoot}':");

        foreach (string guid in modelGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.IndexOf("glTF", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("Unreal", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("Godot", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            bool dirty = false;

            if (importer != null)
            {
                if (humanoid && importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    dirty = true;
                }
                if (bakeAxis && !importer.bakeAxisConversion)
                {
                    importer.bakeAxisConversion = true;
                    dirty = true;
                }
                if (dirty) importer.SaveAndReimport();
            }

            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<string> matNames = new List<string>();
            foreach (Object obj in subAssets)
            {
                if (obj is Material m) matNames.Add(m.name);
            }

            if (importer != null && mats.Count > 0 && matNames.Count > 0)
            {
                var extMap = importer.GetExternalObjectMap();
                bool matDirty = false;
                foreach (string slotName in matNames)
                {
                    if (mats.TryGetValue(slotName, out Material mapped) && mapped != null)
                    {
                        var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), slotName);
                        if (!extMap.TryGetValue(id, out Object cur) || cur != mapped)
                        {
                            importer.AddRemap(id, mapped);
                            matDirty = true;
                        }
                    }
                }
                if (matDirty) importer.SaveAndReimport();
            }

            sb.AppendLine($"    - {path} | Mats: [{string.Join(", ", matNames)}]");
        }
        sb.AppendLine();
    }

    private static void ScanAllAnimationsInAssets(StringBuilder sb)
    {
        string animRoot = Directory.Exists("Assets/Animation") ? "Assets/Animation" : "Assets";
        sb.AppendLine($"## ANIMATION DISCOVERY ({animRoot})");

        string[] allClipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animRoot });
        List<string> foundClips = new List<string>();
        HashSet<string> animModelsToHumanoid = new HashSet<string>();

        foreach (string guid in allClipGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Polytope Studio") || path.Contains("Medieval Village MegaKit")) continue;
            if (path.Contains("glTF")) continue; // Hindari duplikat glTF bila ada versi FBX

            Object[] subs = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object o in subs)
            {
                if (o is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    foundClips.Add($"{path} -> {clip.name} ({clip.length:F2}s, loop={clip.isLooping})");
                    if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                    {
                        animModelsToHumanoid.Add(path);
                    }
                }
            }
        }

        int converted = 0;
        foreach (string fbxPath in animModelsToHumanoid)
        {
            ModelImporter imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp != null)
            {
                bool changed = false;
                if (imp.animationType != ModelImporterAnimationType.Human)
                {
                    imp.animationType = ModelImporterAnimationType.Human;
                    changed = true;
                }
                if (!imp.bakeAxisConversion)
                {
                    imp.bakeAxisConversion = true;
                    changed = true;
                }
                if (changed)
                {
                    imp.SaveAndReimport();
                    converted++;
                }
            }
        }

        sb.AppendLine($"  Animation Clips found outside environment packs ({foundClips.Count}, humanoidUpdated={converted}):");
        foreach (string line in foundClips)
        {
            sb.AppendLine($"    - {line}");
        }
        sb.AppendLine();
    }

    private static void SetupUniversalBaseCharacters(StringBuilder sb)
    {
        sb.AppendLine($"## UNIVERSAL BASE CHARACTERS: {BaseCharsFolder}");
        if (!Directory.Exists(BaseCharsFolder))
        {
            sb.AppendLine("  [FOLDER TIDAK DITEMUKAN]");
            sb.AppendLine();
            return;
        }

        string matDir = BaseCharsFolder + "/Materials";
        EnsureFolderExists(matDir);

        string[] allPngs = Directory.GetFiles(BaseCharsFolder, "*.png", SearchOption.AllDirectories);
        var normalMapByKey = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        var baseColorByKey = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < allPngs.Length; i++)
        {
            string p = allPngs[i].Replace('\\', '/');
            string file = Path.GetFileNameWithoutExtension(p);
            if (p.IndexOf("Unreal", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;

            if (file.IndexOf("Normal", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                FixNormalImporter(p);
                string key = file.Replace("_Normal", "").Replace("Normal", "").Trim('_');
                normalMapByKey[key] = p;
            }
            else if (file.IndexOf("BaseColor", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     file.IndexOf("Albedo", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     file.IndexOf("Diffuse", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string key = file.Replace("_BaseColor", "").Replace("_Albedo", "").Replace("_Diffuse", "").Trim('_');
                baseColorByKey[key] = p;
            }
        }

        var mats = new Dictionary<string, Material>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var kv in baseColorByKey)
        {
            string texKey = kv.Key;
            string normPath = null;
            foreach (var nk in normalMapByKey)
            {
                if (texKey.IndexOf(nk.Key, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    nk.Key.IndexOf("Male", System.StringComparison.OrdinalIgnoreCase) >= 0 && texKey.IndexOf("Male", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (nk.Key.IndexOf("Female", System.StringComparison.OrdinalIgnoreCase) >= 0) == (texKey.IndexOf("Female", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    normPath = nk.Value;
                    break;
                }
            }

            string matName = texKey.StartsWith("T_") ? "MI_" + texKey.Substring(2) : "MI_" + texKey;
            Material m = CreateOrUpdateUrpMat(matDir + "/" + matName + ".mat", kv.Value, normPath, null);
            if (m != null) mats[matName] = m;
        }

        // Ikut daftarkan material bawaan dari Modular Character Outfits sebagai fallback slot
        string[] outfitMatGuids = AssetDatabase.FindAssets("t:Material", new[] { OutfitsFolder });
        for (int i = 0; i < outfitMatGuids.Length; i++)
        {
            string mp = AssetDatabase.GUIDToAssetPath(outfitMatGuids[i]);
            Material om = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (om != null && !mats.ContainsKey(om.name)) mats[om.name] = om;
        }

        string fbxRoot = BaseCharsFolder + "/Exports/FBX (Unity)";
        if (!Directory.Exists(fbxRoot)) fbxRoot = BaseCharsFolder;
        ConfigureModelsInFolder(sb, fbxRoot, mats, bakeAxis: true, humanoid: true);
    }

    private static void ScanAdditionalCharacterModels(StringBuilder sb)
    {
        sb.AppendLine("## EXTRA BASE CHARACTER / HEAD MODELS IN ASSETS");
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        int found = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Polytope Studio") ||
                path.Contains("Medieval Village MegaKit") ||
                path.Contains("Modular Character Outfits") ||
                path.Contains("Universal Base Characters") ||
                path.Contains("Bestiary") ||
                path.Contains("Universal Animation Library"))
            {
                continue;
            }

            sb.AppendLine($"    - {path}");
            found++;
        }

        if (found == 0)
        {
            sb.AppendLine("    (Tidak ditemukan model Base Character / Kepala terpisah di luar folder Outfits/Bestiary/Animation)");
        }
        sb.AppendLine();
    }

    private static Material CreateOrUpdateUrpMat(string matPath, string baseTexPath, string normalTexPath, string emissiveTexPath)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return null;

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

        Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(baseTexPath);
        if (baseTex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex);

        if (!string.IsNullOrEmpty(normalTexPath))
        {
            Texture2D normTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalTexPath);
            if (normTex != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normTex);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        if (!string.IsNullOrEmpty(emissiveTexPath))
        {
            Texture2D emTex = AssetDatabase.LoadAssetAtPath<Texture2D>(emissiveTexPath);
            if (emTex != null && mat.HasProperty("_EmissionMap"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", emTex);
                mat.SetColor("_EmissionColor", Color.white * 1.4f);
            }
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.28f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void FixNormalImporter(string texPath)
    {
        TextureImporter imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.NormalMap)
        {
            imp.textureType = TextureImporterType.NormalMap;
            imp.SaveAndReimport();
        }
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
