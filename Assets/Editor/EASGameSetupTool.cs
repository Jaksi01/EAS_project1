using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Setup otomatis Player (Pyro & Hydro Ranger), NPC Home Village,
/// Avatar Humanoid (Hips = pelvis), AnimatorController (UAL2_Standard),
/// Prefab Monster Bestiary, PCG World Ecosystem, DDA Manager, dan Territory Capture Menhirs.
/// Jalankan via Tools -> Project EAS -> 2. Setup Player, NPCs, Monsters & Adaptive Game.
/// </summary>
public static class EASGameSetupTool
{
    private const string Anim1FbxPath = "Assets/Animation/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx";
    private const string AnimFbxPath = "Assets/Animation/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx";

    private const string MaleRangerFbx = "Assets/Modular Character Outfits - Fantasy[Standard]/Exports/FBX (Unity)/Outfits/Male_Ranger.fbx";
    private const string FemaleRangerFbx = "Assets/Modular Character Outfits - Fantasy[Standard]/Exports/FBX (Unity)/Outfits/Female_Ranger.fbx";
    private const string MalePeasantFbx = "Assets/Modular Character Outfits - Fantasy[Standard]/Exports/FBX (Unity)/Outfits/Male_Peasant.fbx";
    private const string FemalePeasantFbx = "Assets/Modular Character Outfits - Fantasy[Standard]/Exports/FBX (Unity)/Outfits/Female_Peasant.fbx";

    private const string ImpFbx = "Assets/Bestiary - Dungeon Monsters Kit[Standard]/Exports/FBX (Unity)/Imp.fbx";
    private const string PuglinFbx = "Assets/Bestiary - Dungeon Monsters Kit[Standard]/Exports/FBX (Unity)/Puglin.fbx";

    private const string OutfitMatDir = "Assets/Modular Character Outfits - Fantasy[Standard]/Materials/";
    private const string OutfitBaseTexDir = "Assets/Modular Character Outfits - Fantasy[Standard]/Textures/Base";
    private const string BaseCharsDir = "Assets/Universal Base Characters[Standard]";
    private const string BaseHairRiggedDir = "Assets/Universal Base Characters[Standard]/Hairstyles/Rigged to Head Bone/FBX (Unity)";
    private const string BaseCharsMatDir = "Assets/Universal Base Characters[Standard]/Materials/";
    private const string BestiaryMatDir = "Assets/Bestiary - Dungeon Monsters Kit[Standard]/Materials/";
    private const string WeaponsDir = "Assets/Weapons";
    private const string WeaponsFbxDir = "Assets/Weapons/Unity FBX";
    private const string WeaponsMatDir = "Assets/Weapons/Materials";
    private const string PyroSwordFbxPath = WeaponsFbxDir + "/Sword_1_A.fbx";
    private const string HydroSwordFbxPath = WeaponsFbxDir + "/Sword_1_B.fbx";
    private const string AltSwordFbxPath = WeaponsFbxDir + "/Sword_1_C.fbx";
    private const string MilitiaAxeFbxPath = WeaponsFbxDir + "/BattleAxe_1_1_A.fbx";
    private const string MilitiaClubFbxPath = WeaponsFbxDir + "/SpikedClub_1_A.fbx";
    private const string SkyAssetsDir = "Assets/SkyAssets";
    private const string SkyMatOutDir = "Assets/SkyAssets/Materials";
    private const string DefaultPolytopeSkyGuid = "59573b088aaea0442bf29e0a0e9d1339";

    private const string MushroomPrefabPath = "Assets/Scenes/Polytope Studio/Lowpoly_Environments/Prefabs/Mushrooms/PT_Caesars_Mushroom_01.prefab";
    private const string OrePrefabPath = "Assets/Scenes/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Ore_Rock_01.prefab";

    private const string ControllerDir = "Assets/Animations/Controllers";
    private const string CharPrefabDir = "Assets/Prefabs/Characters";
    private const string MonsterPrefabDir = "Assets/Prefabs/Monsters";
    private const string GeneratedMeshDir = "Assets/Generated/Meshes";

    private static readonly (string humanName, string[] boneCandidates)[] HumanoidBoneMap =
    {
        ("Hips", new[] { "pelvis", "Hips", "hips" }),
        ("LeftUpperLeg", new[] { "thigh_l" }),
        ("RightUpperLeg", new[] { "thigh_r" }),
        ("LeftLowerLeg", new[] { "calf_l" }),
        ("RightLowerLeg", new[] { "calf_r" }),
        ("LeftFoot", new[] { "foot_l" }),
        ("RightFoot", new[] { "foot_r" }),
        ("LeftToes", new[] { "ball_l", "ball_leaf_l" }),
        ("RightToes", new[] { "ball_r", "ball_leaf_r" }),
        ("Spine", new[] { "spine_01" }),
        ("Chest", new[] { "spine_02" }),
        ("UpperChest", new[] { "spine_03" }),
        ("Neck", new[] { "neck_01" }),
        ("Head", new[] { "Head", "head" }),
        ("LeftShoulder", new[] { "clavicle_l" }),
        ("RightShoulder", new[] { "clavicle_r" }),
        ("LeftUpperArm", new[] { "upperarm_l" }),
        ("RightUpperArm", new[] { "upperarm_r" }),
        ("LeftLowerArm", new[] { "lowerarm_l" }),
        ("RightLowerArm", new[] { "lowerarm_r" }),
        ("LeftHand", new[] { "hand_l" }),
        ("RightHand", new[] { "hand_r" }),
        ("Left Thumb Proximal", new[] { "thumb_01_l" }),
        ("Left Thumb Intermediate", new[] { "thumb_02_l" }),
        ("Left Thumb Distal", new[] { "thumb_03_l" }),
        ("Left Index Proximal", new[] { "index_01_l" }),
        ("Left Index Intermediate", new[] { "index_02_l" }),
        ("Left Index Distal", new[] { "index_03_l" }),
        ("Left Middle Proximal", new[] { "middle_01_l" }),
        ("Left Middle Intermediate", new[] { "middle_02_l" }),
        ("Left Middle Distal", new[] { "middle_03_l" }),
        ("Left Ring Proximal", new[] { "ring_01_l" }),
        ("Left Ring Intermediate", new[] { "ring_02_l" }),
        ("Left Ring Distal", new[] { "ring_03_l" }),
        ("Left Little Proximal", new[] { "pinky_01_l" }),
        ("Left Little Intermediate", new[] { "pinky_02_l" }),
        ("Left Little Distal", new[] { "pinky_03_l" }),
        ("Right Thumb Proximal", new[] { "thumb_01_r" }),
        ("Right Thumb Intermediate", new[] { "thumb_02_r" }),
        ("Right Thumb Distal", new[] { "thumb_03_r" }),
        ("Right Index Proximal", new[] { "index_01_r" }),
        ("Right Index Intermediate", new[] { "index_02_r" }),
        ("Right Index Distal", new[] { "index_03_r" }),
        ("Right Middle Proximal", new[] { "middle_01_r" }),
        ("Right Middle Intermediate", new[] { "middle_02_r" }),
        ("Right Middle Distal", new[] { "middle_03_r" }),
        ("Right Ring Proximal", new[] { "ring_01_r" }),
        ("Right Ring Intermediate", new[] { "ring_02_r" }),
        ("Right Ring Distal", new[] { "ring_03_r" }),
        ("Right Little Proximal", new[] { "pinky_01_r" }),
        ("Right Little Intermediate", new[] { "pinky_02_r" }),
        ("Right Little Distal", new[] { "pinky_03_r" }),
    };

    [MenuItem("Tools/Project EAS/4. Upgrade Animations (BlendTree, Jump, Combo, Death)")]
    public static void UpgradeAnimationsOnly()
    {
        AssetDatabase.Refresh();
        EnsureFolder("Assets/Animations");
        EnsureFolder(ControllerDir);

        if (!FileExistsAsAsset(AnimFbxPath))
        {
            EditorUtility.DisplayDialog(
                "Asset Animasi Tidak Ditemukan",
                "Tidak menemukan UAL2_Standard.fbx di:\n" + AnimFbxPath,
                "OK");
            return;
        }

        // 1. Pastikan loopBlend & root baking rapi di UAL1 dan UAL2
        ConfigureAnimationLoops(Anim1FbxPath);
        ConfigureAnimationLoops(AnimFbxPath);

        Dictionary<string, AnimationClip> clips = LoadAnimationClips(Anim1FbxPath);
        foreach (var kv in LoadAnimationClips(AnimFbxPath))
        {
            if (!clips.TryGetValue(kv.Key, out AnimationClip cur) || (!cur.isHumanMotion && kv.Value.isHumanMotion))
            {
                clips[kv.Key] = kv.Value;
            }
        }

        // 2. Bangun ulang 3 AnimatorController in-place (GUID tetap sama, semua prefab & scene langsung ter-upgrade!)
        BuildPlayerAnimatorController(clips);
        BuildNpcAnimatorController(clips);
        BuildMonsterAnimatorController(clips);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Project EAS Anim] Upgrade animasi selesai! BlendTree locomotion, Jump Start/Loop/Land, 3-Hit Sword Combo + Heavy Dash-Slash, Variasi NPC & Monster Death aktif tanpa mengubah layout scene.");
    }

    [MenuItem("Tools/Project EAS/2. Setup Player, NPCs, Monsters & Adaptive Game")]
    public static void SetupCompleteGame()
    {
        AssetDatabase.Refresh();
        EnsureFolder("Assets/Animations");
        EnsureFolder(ControllerDir);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(CharPrefabDir);
        EnsureFolder(MonsterPrefabDir);
        EnsureFolder("Assets/Generated");
        EnsureFolder(GeneratedMeshDir);

        if (AssetDatabase.IsValidFolder("Assets/Materials/GeneratedChars"))
        {
            AssetDatabase.DeleteAsset("Assets/Materials/GeneratedChars");
        }

        if (!FileExistsAsAsset(AnimFbxPath))
        {
            EditorUtility.DisplayDialog(
                "Asset Animasi Tidak Ditemukan",
                "Tidak menemukan UAL2_Standard.fbx di:\n" + AnimFbxPath +
                "\n\nJalankan menu 1 (Scan & Setup) terlebih dahulu atau cek folder Assets/Animation.",
                "OK");
            return;
        }

        // 1. Kalibrasi Humanoid Avatar native (CreateFromThisModel + auto-map, tanpa skeleton manual atau CopyFromOther)
        Avatar masterAvatar = ConfigureMasterAnimationAvatar(AnimFbxPath);
        ConfigureMasterAnimationAvatar(Anim1FbxPath);
        ConfigureOutfitHumanoidAvatar(MaleRangerFbx, masterAvatar);
        ConfigureOutfitHumanoidAvatar(FemaleRangerFbx, masterAvatar);
        ConfigureOutfitHumanoidAvatar(MalePeasantFbx, masterAvatar);
        ConfigureOutfitHumanoidAvatar(FemalePeasantFbx, masterAvatar);
        EnsureMonsterHumanoidAvatar(ImpFbx);
        EnsureMonsterHumanoidAvatar(PuglinFbx);
        AssetDatabase.SaveAssets();

        // 2. Pastikan loopTime aktif pada clip loop di UAL1 & UAL2, lalu gabungkan semua clip
        ConfigureAnimationLoops(Anim1FbxPath);
        ConfigureAnimationLoops(AnimFbxPath);
        Dictionary<string, AnimationClip> clips = LoadAnimationClips(Anim1FbxPath);
        foreach (var kv in LoadAnimationClips(AnimFbxPath))
        {
            if (!clips.TryGetValue(kv.Key, out AnimationClip cur) || (!cur.isHumanMotion && kv.Value.isHumanMotion))
            {
                clips[kv.Key] = kv.Value;
            }
        }

        // 3. Bangun 3 AnimatorController (Player, NPC, Monster) lalu simpan agar siap di-bind prefab
        AnimatorController playerCtrl = BuildPlayerAnimatorController(clips);
        AnimatorController npcCtrl = BuildNpcAnimatorController(clips);
        AnimatorController monsterCtrl = BuildMonsterAnimatorController(clips);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 4. Muat material URP asli dari pack Outfits & Universal Base Characters
        Material matRanger = LoadMat(OutfitMatDir + "MI_Ranger.mat");
        Material matRangerAlt = LoadMat(OutfitMatDir + "MI_Ranger_Alt.mat");
        Material matPeasant = LoadMat(OutfitMatDir + "MI_Peasant.mat");
        Material matPeasantAlt = LoadMat(OutfitMatDir + "MI_Peasant_Alt.mat");
        Material matSkinMale = LoadMat(OutfitMatDir + "MI_Regular_Male.mat");
        Material matSkinFemale = LoadMat(OutfitMatDir + "MI_Regular_Female.mat");
        TryLightenSkinMaterial(matSkinMale, "Male");
        TryLightenSkinMaterial(matSkinFemale, "Female");

        Material matHair1 = LoadMat(BaseCharsMatDir + "MI_Hair_1.mat");
        Material matHair2 = LoadMat(BaseCharsMatDir + "MI_Hair_2.mat") ?? matHair1;

        Material matImp1 = LoadMat(BestiaryMatDir + "MI_Imp_1.mat");
        Material matImp2 = LoadMat(BestiaryMatDir + "MI_Imp_2.mat");
        Material matPuglin1 = LoadMat(BestiaryMatDir + "MI_Puglin_1.mat");
        Material matPuglin3 = LoadMat(BestiaryMatDir + "MI_Puglin_3.mat");

        // 5. Bangun Prefab Karakter Player & NPC (100% aset asli + variasi gaya rambut + senjata asli!)
        string pyroSwordPath = ResolveExistingWeaponPath(PyroSwordFbxPath, AltSwordFbxPath);
        string hydroSwordPath = ResolveExistingWeaponPath(HydroSwordFbxPath, AltSwordFbxPath);

        GameObject playerMalePrefab = BuildCharacterVisualPrefab(
            CharPrefabDir + "/Player_Ranger_Male.prefab",
            MaleRangerFbx, playerCtrl, masterAvatar,
            matRanger, matSkinMale, null, matHair1,
            isFemale: false, hasHood: true, uniformScale: 1.0f,
            weaponFbxPath: pyroSwordPath);

        GameObject playerFemalePrefab = BuildCharacterVisualPrefab(
            CharPrefabDir + "/Player_Ranger_Female.prefab",
            FemaleRangerFbx, playerCtrl, masterAvatar,
            matRangerAlt ?? matRanger, matSkinFemale, null, matHair2,
            isFemale: true, hasHood: true, uniformScale: 1.0f,
            weaponFbxPath: hydroSwordPath);

        string axeWeaponPath = ResolveExistingWeaponPath(MilitiaAxeFbxPath, AltSwordFbxPath);
        string clubWeaponPath = ResolveExistingWeaponPath(MilitiaClubFbxPath, AltSwordFbxPath);
        string guardSwordPath = ResolveExistingWeaponPath(AltSwordFbxPath, PyroSwordFbxPath);

        GameObject npcMalePrefab = BuildNpcPrefab(
            CharPrefabDir + "/NPC_Villager_Male.prefab",
            MalePeasantFbx, npcCtrl, masterAvatar,
            matPeasant, matSkinMale, null, matHair1,
            isFemale: false, hasHood: false, uniformScale: 1.0f,
            hairFbxNames: new[] { "Hair_SimpleParted.fbx" },
            weaponFbxPath: axeWeaponPath);

        GameObject npcFarmerPrefab = BuildNpcPrefab(
            CharPrefabDir + "/NPC_Farmer_Male.prefab",
            MalePeasantFbx, npcCtrl, masterAvatar,
            matPeasantAlt ?? matPeasant, matSkinMale, null, matHair2,
            isFemale: false, hasHood: false, uniformScale: 1.0f,
            hairFbxNames: new[] { "Hair_Buzzed.fbx", "Hair_Beard.fbx" },
            weaponFbxPath: axeWeaponPath);

        GameObject npcFemalePrefab = BuildNpcPrefab(
            CharPrefabDir + "/NPC_Villager_Female.prefab",
            FemalePeasantFbx, npcCtrl, masterAvatar,
            matPeasantAlt ?? matPeasant, matSkinFemale, null, matHair2,
            isFemale: true, hasHood: false, uniformScale: 1.0f,
            hairFbxNames: new[] { "Hair_Long.fbx" },
            weaponFbxPath: clubWeaponPath);

        GameObject npcFemaleBunsPrefab = BuildNpcPrefab(
            CharPrefabDir + "/NPC_Villager_Female_Buns.prefab",
            FemalePeasantFbx, npcCtrl, masterAvatar,
            matPeasant, matSkinFemale, null, matHair1,
            isFemale: true, hasHood: false, uniformScale: 1.0f,
            hairFbxNames: new[] { "Hair_Buns.fbx" },
            weaponFbxPath: clubWeaponPath);

        GameObject npcGuardPrefab = BuildNpcPrefab(
            CharPrefabDir + "/NPC_Ranger_Guard.prefab",
            FemaleRangerFbx, npcCtrl, masterAvatar,
            matRanger, matSkinFemale, null, matHair1,
            isFemale: true, hasHood: true, uniformScale: 1.0f,
            weaponFbxPath: guardSwordPath);

        // 6. Bangun 4 Prefab Monster Bestiary (dikompensasi tinggi asli FBX: Puglin pelvis ~0.39m vs Imp pelvis ~0.81m)
        GameObject impScoutPrefab = BuildMonsterPrefab(
            MonsterPrefabDir + "/Monster_Imp_Scout.prefab",
            ImpFbx, monsterCtrl, matImp1, 1.65f,
            EnemyAIController.EnemyArchetype.ImpScout,
            65f, 11f, 2.75f, 11f, 3.8f, 0.55f, 2.35f, 2.15f);

        GameObject impCasterPrefab = BuildMonsterPrefab(
            MonsterPrefabDir + "/Monster_Imp_Caster.prefab",
            ImpFbx, monsterCtrl, matImp2 ?? matImp1, 1.80f,
            EnemyAIController.EnemyArchetype.ImpCaster,
            55f, 14f, 12.0f, 10f, 3.2f, 0.60f, 2.55f, 7.5f);

        GameObject puglinBrutePrefab = BuildMonsterPrefab(
            MonsterPrefabDir + "/Monster_Puglin_Brute.prefab",
            PuglinFbx, monsterCtrl, matPuglin1, 2.90f,
            EnemyAIController.EnemyArchetype.PuglinBrute,
            115f, 18f, 3.20f, 10f, 3.1f, 0.85f, 2.90f, 2.55f);

        GameObject puglinElitePrefab = BuildMonsterPrefab(
            MonsterPrefabDir + "/Monster_Puglin_Elite.prefab",
            PuglinFbx, monsterCtrl, matPuglin3 ?? matPuglin1, 3.45f,
            EnemyAIController.EnemyArchetype.PuglinBrute,
            175f, 24f, 3.60f, 9f, 3.3f, 1.00f, 3.45f, 2.85f);

        // 7. Wire Scene: Player, ThirdPersonCamera, Home Village NPCs, Menhir Zones, Game Systems
        GameObject playerObj = SetupPlayerInScene(playerMalePrefab, playerFemalePrefab);
        SetupCameraInScene(playerObj);
        GameLoopManager.ApplyVisualQualityAndLighting();
        int npcCount = SetupHomeVillageNpcs(npcMalePrefab, npcFarmerPrefab, npcFemalePrefab, npcFemaleBunsPrefab, npcGuardPrefab);
        int menhirCount = SetupTerritoryMenhirs();
        SetupGameSystems(impScoutPrefab, impCasterPrefab, puglinBrutePrefab, puglinElitePrefab, monsterCtrl);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        string msg = "[Project EAS] Setup selesai!\n" +
                     "- Humanoid Avatar terkalibrasi (Hips -> pelvis) untuk Outfits, Monsters & UAL2\n" +
                     "- Animator Controllers: 3 (Player, NPC, Monster)\n" +
                     "- NPC Home Village terpasang di plaza: " + npcCount + " karakter\n" +
                     "- Territory Capture Menhirs terhubung: " + menhirCount + "/3\n" +
                     "Tekan Ctrl+S untuk menyimpan scene lalu tekan PLAY!";
        Debug.Log(msg);

        if (menhirCount < 3)
        {
            Debug.LogWarning("[Project EAS] Kurang dari 3 Territory_Capture_Menhir ditemukan di scene. " +
                             "Pastikan Tools -> Village Builder -> 2. Build Complete Village & Rearrange World sudah dijalankan.");
        }
    }

    // =========================================================================
    // 1. Humanoid Avatar Calibration (Native CreateFromThisModel untuk semua FBX)
    // =========================================================================
    private static Avatar ConfigureMasterAnimationAvatar(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return null;

        if (NeedsCleanHumanoidReset(importer, fbxPath))
        {
            ResetToCleanNativeHumanoid(importer, fbxPath);
        }

        Avatar av = LoadAvatarFromFbx(fbxPath);
        Debug.Log($"[Project EAS Avatar] Master '{Path.GetFileName(fbxPath)}': valid={(av != null && av.isValid)}, human={(av != null && av.isHuman)}");
        return av;
    }

    private static bool NeedsCleanHumanoidReset(ModelImporter importer, string fbxPath)
    {
        if (importer.animationType != ModelImporterAnimationType.Human) return true;
        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) return true;
        if (!importer.bakeAxisConversion) return true;
        if (!importer.autoGenerateAvatarMappingIfUnspecified) return true;
        if (HasCloneInHumanDescription(importer)) return true;

        Avatar av = LoadAvatarFromFbx(fbxPath);
        return av == null || !av.isValid || !av.isHuman;
    }

    private static bool HasCloneInHumanDescription(ModelImporter importer)
    {
        HumanDescription desc = importer.humanDescription;
        if (desc.skeleton != null)
        {
            for (int i = 0; i < desc.skeleton.Length; i++)
            {
                string n = desc.skeleton[i].name ?? string.Empty;
                if (n.Contains("(Clone)")) return true;
            }
        }
        return false;
    }

    private static void ConfigureOutfitHumanoidAvatar(string fbxPath, Avatar masterAvatar)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        if (NeedsCleanHumanoidReset(importer, fbxPath))
        {
            ResetToCleanNativeHumanoid(importer, fbxPath);
        }

        Avatar av = LoadAvatarFromFbx(fbxPath);
        Debug.Log($"[Project EAS Avatar] Outfit '{Path.GetFileName(fbxPath)}': valid={(av != null && av.isValid)}, human={(av != null && av.isHuman)}");
    }

    private static void EnsureMonsterHumanoidAvatar(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        if (NeedsCleanHumanoidReset(importer, fbxPath))
        {
            ResetToCleanNativeHumanoid(importer, fbxPath);
        }
    }

    private static void ResetToCleanNativeHumanoid(ModelImporter importer, string fbxPath)
    {
        importer.bakeAxisConversion = true;
        importer.sourceAvatar = null;
        importer.animationType = ModelImporterAnimationType.None;
        importer.SaveAndReimport();

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.autoGenerateAvatarMappingIfUnspecified = true;
        HumanDescription cleanDesc = importer.humanDescription;
        cleanDesc.human = Array.Empty<HumanBone>();
        cleanDesc.skeleton = Array.Empty<SkeletonBone>();
        importer.humanDescription = cleanDesc;
        importer.SaveAndReimport();

        Avatar builtAvatar = LoadAvatarFromFbx(fbxPath);
        if (builtAvatar == null || !builtAvatar.isValid || !builtAvatar.isHuman)
        {
            HumanBone[] mapped = BuildMappedHumanBones(fbxPath, out _);
            importer.autoGenerateAvatarMappingIfUnspecified = false;
            HumanDescription fallbackDesc = importer.humanDescription;
            fallbackDesc.human = mapped;
            fallbackDesc.skeleton = Array.Empty<SkeletonBone>();
            importer.humanDescription = fallbackDesc;
            importer.SaveAndReimport();
            builtAvatar = LoadAvatarFromFbx(fbxPath);
        }

        Debug.Log($"[Project EAS Avatar] Calibrated '{Path.GetFileName(fbxPath)}': valid={(builtAvatar != null && builtAvatar.isValid)}, human={(builtAvatar != null && builtAvatar.isHuman)}, hipsPelvis={VerifyFbxAvatarHipsIsPelvis(fbxPath, null)}");
    }

    private static HumanDescription CreateDefaultHumanDescription(HumanBone[] humanBones, SkeletonBone[] skeletonBones = null)
    {
        return new HumanDescription
        {
            human = humanBones ?? Array.Empty<HumanBone>(),
            skeleton = skeletonBones ?? Array.Empty<SkeletonBone>(),
            upperArmTwist = 0.5f,
            lowerArmTwist = 0.5f,
            upperLegTwist = 0.5f,
            lowerLegTwist = 0.5f,
            armStretch = 0.05f,
            legStretch = 0.05f,
            feetSpacing = 0f,
            hasTranslationDoF = false
        };
    }

    private static HumanBone[] BuildMappedHumanBones(string fbxPath, out SkeletonBone[] skeletonBones)
    {
        GameObject fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxRoot == null)
        {
            skeletonBones = Array.Empty<SkeletonBone>();
            return Array.Empty<HumanBone>();
        }

        Transform[] allTransforms = fbxRoot.GetComponentsInChildren<Transform>(true);
        var boneSet = new HashSet<string>(StringComparer.Ordinal);
        var skelList = new List<SkeletonBone>(allTransforms.Length);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];
            boneSet.Add(t.name);
            skelList.Add(new SkeletonBone
            {
                name = t.name,
                position = t.localPosition,
                rotation = t.localRotation,
                scale = t.localScale
            });
        }
        skeletonBones = skelList.ToArray();

        var mappedHuman = new List<HumanBone>();
        for (int i = 0; i < HumanoidBoneMap.Length; i++)
        {
            var entry = HumanoidBoneMap[i];
            for (int c = 0; c < entry.boneCandidates.Length; c++)
            {
                string cand = entry.boneCandidates[c];
                if (boneSet.Contains(cand))
                {
                    mappedHuman.Add(new HumanBone
                    {
                        humanName = entry.humanName,
                        boneName = cand,
                        limit = new HumanLimit { useDefaultValues = true }
                    });
                    break;
                }
            }
        }
        return mappedHuman.ToArray();
    }

    private static bool VerifyFbxAvatarHipsIsPelvis(string fbxPath, Avatar fallbackAvatar)
    {
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null) return false;

        Animator animAsset = fbxAsset.GetComponent<Animator>();
        Avatar av = (animAsset != null && animAsset.avatar != null) ? animAsset.avatar : (LoadAvatarFromFbx(fbxPath) ?? fallbackAvatar);
        if (av == null || !av.isValid || !av.isHuman) return false;

        bool hasPelvisTransform = false;
        bool hasSpineTransform = false;
        Transform[] transforms = fbxAsset.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            string tName = transforms[i].name;
            if (string.Equals(tName, "pelvis", StringComparison.Ordinal)) hasPelvisTransform = true;
            else if (string.Equals(tName, "spine_01", StringComparison.Ordinal) ||
                     string.Equals(tName, "spine", StringComparison.OrdinalIgnoreCase)) hasSpineTransform = true;
        }
        if (!hasPelvisTransform || !hasSpineTransform) return false;

        HumanDescription desc = av.humanDescription;
        if (desc.skeleton != null)
        {
            for (int i = 0; i < desc.skeleton.Length; i++)
            {
                string n = desc.skeleton[i].name ?? string.Empty;
                if (n.Contains("(Clone)")) return false;
            }
        }

        if (desc.human != null && desc.human.Length > 0)
        {
            bool mappedHipsToPelvis = false;
            bool mappedSpine = false;
            for (int i = 0; i < desc.human.Length; i++)
            {
                if (string.Equals(desc.human[i].humanName, "Hips", StringComparison.Ordinal) &&
                    string.Equals(desc.human[i].boneName, "pelvis", StringComparison.Ordinal))
                {
                    mappedHipsToPelvis = true;
                }
                else if (string.Equals(desc.human[i].humanName, "Spine", StringComparison.Ordinal) &&
                         !string.IsNullOrEmpty(desc.human[i].boneName))
                {
                    mappedSpine = true;
                }
            }
            if (!mappedHipsToPelvis || !mappedSpine) return false;
        }

        return true;
    }

    // =========================================================================
    // 2. Animation Clip Configuration & Loading
    // =========================================================================
    private static void ConfigureAnimationLoops(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        bool dirty = false;
        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.defaultClipAnimations;
            dirty = true;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            string n = clips[i].name;
            bool shouldLoop = n.EndsWith("_Loop") || n.Contains("Loop") || n.EndsWith("Idle_No_Loop") ||
                              n.EndsWith("Sword_Idle") || n.EndsWith("Farm_Harvest") ||
                              n.EndsWith("Farm_PlantSeed") || n.EndsWith("Farm_Watering");
            bool isAction = n.Contains("Sword_") || n.Contains("Spell_") || n.Contains("Melee_") ||
                            n.Contains("Punch_") || n.Contains("Zombie_") || n.Contains("Hit_") ||
                            n.EndsWith("Roll") || n.Contains("Jump_") || n.Contains("NinjaJump_") ||
                            n.Contains("OverhandThrow") || n.EndsWith("Death01") ||
                            n.EndsWith("LayToIdle") || n.EndsWith("Yes") || n.EndsWith("Interact");

            if (shouldLoop && (!clips[i].loopTime || !clips[i].loopPose || !clips[i].lockRootRotation || !clips[i].keepOriginalOrientation || !clips[i].lockRootPositionXZ))
            {
                clips[i].loopTime = true;
                clips[i].loopPose = true;
                clips[i].lockRootRotation = true;
                clips[i].keepOriginalOrientation = true;
                clips[i].lockRootHeightY = true;
                clips[i].lockRootPositionXZ = true;
                clips[i].keepOriginalPositionY = true;
                dirty = true;
            }
            else if (!shouldLoop && isAction && (!clips[i].lockRootRotation || !clips[i].keepOriginalOrientation || !clips[i].lockRootPositionXZ))
            {
                clips[i].lockRootRotation = true;
                clips[i].keepOriginalOrientation = true;
                clips[i].lockRootHeightY = true;
                clips[i].lockRootPositionXZ = true;
                clips[i].keepOriginalPositionY = true;
                dirty = true;
            }
        }

        if (dirty)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    private static Dictionary<string, AnimationClip> LoadAnimationClips(string fbxPath)
    {
        var dict = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        int nonHumanCount = 0;
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                if (!clip.isHumanMotion) nonHumanCount++;
                if (!dict.TryGetValue(clip.name, out AnimationClip existing) || (!existing.isHumanMotion && clip.isHumanMotion))
                {
                    dict[clip.name] = clip;
                }
            }
        }
        if (nonHumanCount > 0)
        {
            Debug.LogError($"[Project EAS Anim] '{Path.GetFileName(fbxPath)}' memiliki {nonHumanCount} clip BUKAN Humanoid! Pastikan Avatar/Rig FBX ini Humanoid.");
        }
        return dict;
    }

    private static AnimationClip FindClip(Dictionary<string, AnimationClip> clips, params string[] candidates)
    {
        for (int pass = 0; pass < 2; pass++)
        {
            bool requireHuman = pass == 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                string cand = candidates[i];
                if (clips.TryGetValue(cand, out AnimationClip exact) && (!requireHuman || exact.isHumanMotion)) return exact;
                if (clips.TryGetValue("Armature|" + cand, out AnimationClip arm) && (!requireHuman || arm.isHumanMotion)) return arm;
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                string cand = candidates[i];
                foreach (var kv in clips)
                {
                    if (kv.Key.EndsWith(cand, StringComparison.OrdinalIgnoreCase) && (!requireHuman || kv.Value.isHumanMotion))
                    {
                        return kv.Value;
                    }
                }
            }
        }

        foreach (var kv in clips)
        {
            if (kv.Value.isHumanMotion) return kv.Value;
        }
        foreach (var kv in clips)
        {
            return kv.Value;
        }
        return null;
    }

    private static string DescribeClip(AnimationClip clip)
    {
        return clip == null ? "null" : $"{clip.name}(human={clip.isHumanMotion},loop={clip.isLooping})";
    }

    // =========================================================================
    // 3. Animator Controllers (BlendTree + Foot IK + Jump Cycle + Combo + Death)
    // =========================================================================
    private static AnimatorController CreateOrResetController(string path)
    {
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (ctrl == null)
        {
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        for (int i = ctrl.layers.Length - 1; i > 0; i--)
        {
            ctrl.RemoveLayer(i);
        }

        if (ctrl.layers.Length == 0)
        {
            ctrl.AddLayer("Base Layer");
        }
        else
        {
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            var anyTransitions = sm.anyStateTransitions;
            for (int i = 0; i < anyTransitions.Length; i++) sm.RemoveAnyStateTransition(anyTransitions[i]);
            var states = sm.states;
            for (int i = 0; i < states.Length; i++) sm.RemoveState(states[i].state);
            var subMachines = sm.stateMachines;
            for (int i = 0; i < subMachines.Length; i++) sm.RemoveStateMachine(subMachines[i].stateMachine);
        }

        // Bersihkan sub-asset BlendTree lama agar file .controller tidak menumpuk
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is BlendTree)
            {
                Object.DestroyImmediate(subAssets[i], true);
            }
        }

        for (int i = ctrl.parameters.Length - 1; i >= 0; i--)
        {
            ctrl.RemoveParameter(i);
        }
        return ctrl;
    }

    private static AnimatorState Create1DBlendTreeState(
        AnimatorController ctrl,
        string stateName,
        string blendParam,
        bool iKOnFeet,
        string cycleOffsetParam,
        params (AnimationClip clip, float threshold)[] children)
    {
        AnimatorState state = ctrl.CreateBlendTreeInController(stateName, out BlendTree tree, 0);
        tree.name = stateName + "_Tree";
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = blendParam;
        tree.useAutomaticThresholds = false;

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].clip != null)
            {
                tree.AddChild(children[i].clip, children[i].threshold);
            }
        }

        state.iKOnFeet = iKOnFeet;
        if (!string.IsNullOrEmpty(cycleOffsetParam))
        {
            state.cycleOffsetParameterActive = true;
            state.cycleOffsetParameter = cycleOffsetParam;
        }
        return state;
    }

    private static AnimatorController BuildPlayerAnimatorController(Dictionary<string, AnimationClip> clips)
    {
        AnimatorController ctrl = CreateOrResetController(ControllerDir + "/Player_Animator.controller");
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("VerticalVel", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Skill", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Burst", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("ComboStep", AnimatorControllerParameterType.Int);
        ctrl.AddParameter("HitType", AnimatorControllerParameterType.Int);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        AnimationClip idleClip = FindClip(clips, "Sword_Idle", "Idle_Shield_Loop", "Idle_Loop");
        AnimationClip walkClip = FindClip(clips, "Walk_Loop", "Walk_Formal_Loop", "Jog_Fwd_Loop");
        AnimationClip jogClip = FindClip(clips, "Jog_Fwd_Loop", "Walk_Loop");
        AnimationClip sprintClip = FindClip(clips, "Sprint_Loop", "Jog_Fwd_Loop");

        AnimationClip attackClip1 = FindClip(clips, "Sword_Attack", "Sword_Regular_A", "Punch_Cross");
        AnimationClip attackClip2 = FindClip(clips, "Sword_Regular_C", "Sword_Regular_B", "Sword_Attack");
        AnimationClip attackClip3 = FindClip(clips, "Sword_Regular_Combo", "Sword_Regular_C", "Sword_Attack");
        AnimationClip attackClip4 = FindClip(clips, "Sword_Heavy_Combo", "Sword_Regular_Combo", "Sword_Attack");
        AnimationClip heavySlashClip = FindClip(clips, "Sword_Dash", "Sword_Heavy_Combo", "Sword_Attack");

        AnimationClip dashClip = FindClip(clips, "Roll", "Sprint_Loop");
        AnimationClip jumpStartClip = FindClip(clips, "Jump_Start", "NinjaJump_Start");
        AnimationClip jumpLoopClip = FindClip(clips, "Jump_Loop", "NinjaJump_Idle_Loop", "Jump_Start");
        AnimationClip jumpLandClip = FindClip(clips, "Jump_Land", "NinjaJump_Land", "Sword_Idle");

        AnimationClip hitChestClip = FindClip(clips, "Hit_Chest", "Hit_Knockback", "Idle_Loop");
        AnimationClip hitHeadClip = FindClip(clips, "Hit_Head", "Hit_Chest", "Idle_Loop");
        AnimationClip deathClip = FindClip(clips, "Death01", "Hit_Knockback", "Idle_Loop");

        // 1. Smooth 1D Locomotion BlendTree (Idle -> Walk -> Jog -> Sprint) + Foot IK
        AnimatorState locoState = Create1DBlendTreeState(
            ctrl, "Locomotion", "Speed", iKOnFeet: true, cycleOffsetParam: null,
            (idleClip, 0.0f),
            (walkClip, 0.45f),
            (jogClip, 1.0f),
            (sprintClip, 1.5f));
        sm.defaultState = locoState;

        // 2. Full Jump Cycle: Jump_Start -> Jump_Loop (mid-air) -> Jump_Land -> Locomotion
        AnimatorState jumpStartState = sm.AddState("Jump_Start");
        jumpStartState.motion = jumpStartClip;
        jumpStartState.speed = 1.35f;
        jumpStartState.iKOnFeet = true;

        AnimatorState jumpLoopState = sm.AddState("Jump_Loop");
        jumpLoopState.motion = jumpLoopClip;
        jumpLoopState.iKOnFeet = false;

        AnimatorState jumpLandState = sm.AddState("Jump_Land");
        jumpLandState.motion = jumpLandClip;
        jumpLandState.speed = 1.45f;
        jumpLandState.iKOnFeet = true;

        // Trigger Jump dari AnyState (selama tidak mati)
        AnimatorStateTransition anyToJump = sm.AddAnyStateTransition(jumpStartState);
        anyToJump.hasExitTime = false;
        anyToJump.duration = 0.06f;
        anyToJump.canTransitionToSelf = false;
        anyToJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
        anyToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

        // Jatuh dari tebing/tepian tanpa tekan Jump
        AnimatorStateTransition locoToFall = locoState.AddTransition(jumpLoopState);
        locoToFall.hasExitTime = false;
        locoToFall.duration = 0.14f;
        locoToFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
        locoToFall.AddCondition(AnimatorConditionMode.Less, -2.6f, "VerticalVel");

        AnimatorStateTransition jumpStartToAir = jumpStartState.AddTransition(jumpLoopState);
        jumpStartToAir.hasExitTime = true;
        jumpStartToAir.exitTime = 0.50f;
        jumpStartToAir.duration = 0.10f;
        jumpStartToAir.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");

        AnimatorStateTransition jumpStartToLand = jumpStartState.AddTransition(jumpLandState);
        jumpStartToLand.hasExitTime = true;
        jumpStartToLand.exitTime = 0.62f;
        jumpStartToLand.duration = 0.08f;
        jumpStartToLand.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");

        // Safety exit dari Jump_Start
        AnimatorStateTransition jumpStartSafety = jumpStartState.AddTransition(locoState);
        jumpStartSafety.hasExitTime = true;
        jumpStartSafety.exitTime = 0.92f;
        jumpStartSafety.duration = 0.10f;

        AnimatorStateTransition airToLand = jumpLoopState.AddTransition(jumpLandState);
        airToLand.hasExitTime = false;
        airToLand.duration = 0.07f;
        airToLand.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");

        // Mendarat langsung lari (fast run-cancel) atau mendarat penuh saat diam
        AnimatorStateTransition landToRun = jumpLandState.AddTransition(locoState);
        landToRun.hasExitTime = true;
        landToRun.exitTime = 0.25f;
        landToRun.duration = 0.10f;
        landToRun.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");

        AnimatorStateTransition landToIdle = jumpLandState.AddTransition(locoState);
        landToIdle.hasExitTime = true;
        landToIdle.exitTime = 0.78f;
        landToIdle.duration = 0.12f;

        // 3. Full 4-Hit Sword Combo (Sword_Attack -> Sword_Regular_C -> Sword_Regular_Combo -> Sword_Heavy_Combo) + Heavy Dash-Slash (Q)
        AddAnyStateAction(sm, locoState, "Attack", attackClip1, 0.06f, 0.88f, stateName: "Attack_1", speed: 1.25f, intParamName: "ComboStep", intParamValue: 0, boolGuardParam: "IsDead", boolGuardExpected: false);
        AddAnyStateAction(sm, locoState, "Attack", attackClip2, 0.06f, 0.88f, stateName: "Attack_2", speed: 1.25f, intParamName: "ComboStep", intParamValue: 1, boolGuardParam: "IsDead", boolGuardExpected: false);
        AddAnyStateAction(sm, locoState, "Attack", attackClip3, 0.06f, 0.90f, stateName: "Attack_3", speed: 1.20f, intParamName: "ComboStep", intParamValue: 2, boolGuardParam: "IsDead", boolGuardExpected: false);
        AddAnyStateAction(sm, locoState, "Attack", attackClip4, 0.06f, 0.90f, stateName: "Attack_4", speed: 1.30f, intParamName: "ComboStep", intParamValue: 3, boolGuardParam: "IsDead", boolGuardExpected: false);
        AddAnyStateAction(sm, locoState, "Burst", heavySlashClip, 0.05f, 0.88f, stateName: "HeavySlash", speed: 1.25f, boolGuardParam: "IsDead", boolGuardExpected: false);
        AddAnyStateAction(sm, locoState, "Dash", dashClip, 0.05f, 0.84f, stateName: "Dash", speed: 1.75f, boolGuardParam: "IsDead", boolGuardExpected: false);
        AddAnyStateAction(sm, locoState, "Hit", hitChestClip, 0.04f, 0.80f, stateName: "Hit_Chest", speed: 1.25f, intParamName: "HitType", intParamValue: 0, boolGuardParam: "IsDead", boolGuardExpected: false);
        AddAnyStateAction(sm, locoState, "Hit", hitHeadClip, 0.04f, 0.80f, stateName: "Hit_Head", speed: 1.25f, intParamName: "HitType", intParamValue: 1, boolGuardParam: "IsDead", boolGuardExpected: false);

        // 4. Death & Respawn State
        AnimatorState deadState = sm.AddState("Dead");
        deadState.motion = deathClip;
        deadState.iKOnFeet = false;

        AnimatorStateTransition anyToDead = sm.AddAnyStateTransition(deadState);
        anyToDead.hasExitTime = false;
        anyToDead.duration = 0.10f;
        anyToDead.canTransitionToSelf = false;
        anyToDead.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

        AnimatorStateTransition deadToLoco = deadState.AddTransition(locoState);
        deadToLoco.hasExitTime = false;
        deadToLoco.duration = 0.18f;
        deadToLoco.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");

        Debug.Log($"[Project EAS Anim] Player Upgraded -> BlendTree(Idle={DescribeClip(idleClip)}, Walk={DescribeClip(walkClip)}, Jog={DescribeClip(jogClip)}, Sprint={DescribeClip(sprintClip)}), " +
                  $"Jump({DescribeClip(jumpStartClip)}->{DescribeClip(jumpLoopClip)}->{DescribeClip(jumpLandClip)}), Combo({DescribeClip(attackClip1)}/{DescribeClip(attackClip2)}/{DescribeClip(attackClip3)}/{DescribeClip(attackClip4)}), Heavy={DescribeClip(heavySlashClip)}, Death={DescribeClip(deathClip)}");
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    private static AnimatorController BuildNpcAnimatorController(Dictionary<string, AnimationClip> clips)
    {
        AnimatorController ctrl = CreateOrResetController(ControllerDir + "/NPC_Animator.controller");
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("CycleOffset", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("WorkMode", AnimatorControllerParameterType.Int);
        ctrl.AddParameter("IsCompanion", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsDowned", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("AttackVariant", AnimatorControllerParameterType.Int);
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Cheer", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        AnimationClip idleClip = FindClip(clips, "Idle_FoldArms_Loop", "Idle_Loop", "Idle_No_Loop");
        AnimationClip talkClip = FindClip(clips, "Idle_Talking_Loop", "Idle_FoldArms_Loop", "Idle_Loop");
        AnimationClip companionIdleClip = FindClip(clips, "Sword_Idle", "Idle_Loop", "Idle_No_Loop");
        AnimationClip walkClip = FindClip(clips, "Walk_Loop", "Walk_Formal_Loop", "Jog_Fwd_Loop");
        AnimationClip jogClip = FindClip(clips, "Jog_Fwd_Loop", "Walk_Loop");
        AnimationClip sprintClip = FindClip(clips, "Sprint_Loop", "Jog_Fwd_Loop");
        AnimationClip harvestClip = FindClip(clips, "Farm_Harvest", "Farm_PlantSeed", "Idle_Loop");
        AnimationClip chopClip = FindClip(clips, "TreeChopping_Loop", "Farm_Watering", "Farm_Harvest");
        AnimationClip cheerClip = FindClip(clips, "Yes", "Idle_Talking_Loop", "Idle_Loop");
        AnimationClip atk1Clip = FindClip(clips, "Sword_Attack", "Sword_Regular_A", "Melee_Hook");
        AnimationClip atk2Clip = FindClip(clips, "Sword_Regular_C", "Sword_Regular_B", "Sword_Attack");
        AnimationClip atk3Clip = FindClip(clips, "Sword_Dash", "Sword_Attack", "Melee_Hook");
        AnimationClip hitClip = FindClip(clips, "Hit_Chest", "Hit_Knockback", "Idle_Loop");
        AnimationClip deathClip = FindClip(clips, "Death01", "Hit_Knockback", "Idle_Loop");
        AnimationClip reviveClip = FindClip(clips, "LayToIdle", "Sword_Idle", "Idle_Loop");

        // 1. Smooth BlendTree untuk Warga Desa & Companion Tempur
        AnimatorState villageLoco = Create1DBlendTreeState(
            ctrl, "VillageLocomotion", "Speed", iKOnFeet: true, cycleOffsetParam: "CycleOffset",
            (idleClip, 0.0f),
            (walkClip, 1.85f),
            (jogClip, 4.2f));
        sm.defaultState = villageLoco;

        AnimatorState compLoco = Create1DBlendTreeState(
            ctrl, "CompanionLocomotion", "Speed", iKOnFeet: true, cycleOffsetParam: "CycleOffset",
            (companionIdleClip, 0.0f),
            (walkClip, 2.0f),
            (jogClip, 6.0f),
            (sprintClip, 9.6f));

        AnimatorStateTransition villageToComp = villageLoco.AddTransition(compLoco);
        villageToComp.hasExitTime = false;
        villageToComp.duration = 0.14f;
        villageToComp.AddCondition(AnimatorConditionMode.If, 0f, "IsCompanion");

        AnimatorStateTransition compToVillage = compLoco.AddTransition(villageLoco);
        compToVillage.hasExitTime = false;
        compToVillage.duration = 0.18f;
        compToVillage.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCompanion");

        // 2. Variasi Aktivitas Desa (WorkMode 1 = Panen, 2 = Tebang/Siram, 3 = Ngobrol)
        AddNpcWorkState(sm, villageLoco, compLoco, "Work_Harvest", harvestClip, 1);
        AddNpcWorkState(sm, villageLoco, compLoco, "Work_Chop", chopClip, 2);
        AddNpcWorkState(sm, villageLoco, compLoco, "Work_Talk", talkClip, 3);

        // 3. Cheer saat direkrut + 3 Variasi Serangan Pedang + Hit
        AddAnyStateAction(sm, compLoco, "Cheer", cheerClip, 0.08f, 0.85f, stateName: "Cheer", speed: 1.35f, boolGuardParam: "IsDowned", boolGuardExpected: false);
        AddAnyStateAction(sm, compLoco, "Attack", atk1Clip, 0.05f, 0.86f, stateName: "Attack_1", speed: 1.15f, intParamName: "AttackVariant", intParamValue: 0, boolGuardParam: "IsDowned", boolGuardExpected: false);
        AddAnyStateAction(sm, compLoco, "Attack", atk2Clip, 0.05f, 0.86f, stateName: "Attack_2", speed: 1.15f, intParamName: "AttackVariant", intParamValue: 1, boolGuardParam: "IsDowned", boolGuardExpected: false);
        AddAnyStateAction(sm, compLoco, "Attack", atk3Clip, 0.06f, 0.84f, stateName: "Attack_3", speed: 1.25f, intParamName: "AttackVariant", intParamValue: 2, boolGuardParam: "IsDowned", boolGuardExpected: false);
        AddAnyStateAction(sm, compLoco, "Hit", hitClip, 0.04f, 0.80f, stateName: "Hit", speed: 1.2f, boolGuardParam: "IsDowned", boolGuardExpected: false);

        // 4. Downed (Death01) -> Revive (LayToIdle) -> CompanionLocomotion
        AnimatorState downedState = sm.AddState("Downed");
        downedState.motion = deathClip;
        downedState.iKOnFeet = false;

        AnimatorState reviveState = sm.AddState("Revive");
        reviveState.motion = reviveClip;
        reviveState.speed = 1.25f;
        reviveState.iKOnFeet = true;

        AnimatorStateTransition anyToDowned = sm.AddAnyStateTransition(downedState);
        anyToDowned.hasExitTime = false;
        anyToDowned.duration = 0.10f;
        anyToDowned.canTransitionToSelf = false;
        anyToDowned.AddCondition(AnimatorConditionMode.If, 0f, "IsDowned");

        AnimatorStateTransition downedToRevive = downedState.AddTransition(reviveState);
        downedToRevive.hasExitTime = false;
        downedToRevive.duration = 0.12f;
        downedToRevive.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDowned");

        AnimatorStateTransition reviveToComp = reviveState.AddTransition(compLoco);
        reviveToComp.hasExitTime = true;
        reviveToComp.exitTime = 0.88f;
        reviveToComp.duration = 0.14f;

        Debug.Log($"[Project EAS Anim] NPC Upgraded -> VillageBlend({DescribeClip(idleClip)}/{DescribeClip(walkClip)}), CompBlend({DescribeClip(companionIdleClip)}/{DescribeClip(jogClip)}/{DescribeClip(sprintClip)}), Attacks({DescribeClip(atk1Clip)}/{DescribeClip(atk2Clip)}/{DescribeClip(atk3Clip)}), Downed={DescribeClip(deathClip)}, Revive={DescribeClip(reviveClip)}");
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    private static void AddNpcWorkState(
        AnimatorStateMachine sm,
        AnimatorState villageLoco,
        AnimatorState compLoco,
        string stateName,
        AnimationClip clip,
        int modeValue)
    {
        AnimatorState workState = sm.AddState(stateName);
        workState.motion = clip;
        workState.iKOnFeet = true;
        workState.cycleOffsetParameterActive = true;
        workState.cycleOffsetParameter = "CycleOffset";

        AnimatorStateTransition toWork = villageLoco.AddTransition(workState);
        toWork.hasExitTime = false;
        toWork.duration = 0.22f;
        toWork.AddCondition(AnimatorConditionMode.Equals, modeValue, "WorkMode");
        toWork.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsCompanion");
        toWork.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

        AnimatorStateTransition workModeChanged = workState.AddTransition(villageLoco);
        workModeChanged.hasExitTime = false;
        workModeChanged.duration = 0.18f;
        workModeChanged.AddCondition(AnimatorConditionMode.NotEqual, modeValue, "WorkMode");

        AnimatorStateTransition workStartedMoving = workState.AddTransition(villageLoco);
        workStartedMoving.hasExitTime = false;
        workStartedMoving.duration = 0.15f;
        workStartedMoving.AddCondition(AnimatorConditionMode.Greater, 0.25f, "Speed");

        AnimatorStateTransition workToComp = workState.AddTransition(compLoco);
        workToComp.hasExitTime = false;
        workToComp.duration = 0.14f;
        workToComp.AddCondition(AnimatorConditionMode.If, 0f, "IsCompanion");
    }

    private static AnimatorController BuildMonsterAnimatorController(Dictionary<string, AnimationClip> clips)
    {
        AnimatorController ctrl = CreateOrResetController(ControllerDir + "/Monster_Animator.controller");
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("CycleOffset", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("AttackType", AnimatorControllerParameterType.Int);
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        AnimationClip idleClip = FindClip(clips, "Zombie_Idle_Loop", "Idle_Loop");
        AnimationClip walkClip = FindClip(clips, "Zombie_Walk_Fwd_Loop", "Walk_Loop");
        AnimationClip runClip = FindClip(clips, "Jog_Fwd_Loop", "Sprint_Loop", "Zombie_Walk_Fwd_Loop");
        AnimationClip scratchClip = FindClip(clips, "Zombie_Scratch", "Melee_Hook", "Punch_Cross");
        AnimationClip heavyClip = FindClip(clips, "Melee_Hook", "Punch_Cross", "Zombie_Scratch");
        AnimationClip castClip = FindClip(clips, "Spell_Simple_Shoot", "OverhandThrow", "Zombie_Scratch");
        AnimationClip hitClip = FindClip(clips, "Hit_Knockback", "Hit_Chest", "Idle_Loop");
        AnimationClip deathClip = FindClip(clips, "Death01", "Hit_Knockback", "Idle_Loop");

        // 1. Smooth 1D BlendTree (Idle -> Patrol Walk -> Chase Run) + Foot IK + CycleOffset
        AnimatorState locoState = Create1DBlendTreeState(
            ctrl, "Locomotion", "Speed", iKOnFeet: true, cycleOffsetParam: "CycleOffset",
            (idleClip, 0.0f),
            (walkClip, 2.0f),
            (runClip, 4.3f));
        sm.defaultState = locoState;

        // 2. Variasi Serangan per Archetype (0=Scratch, 1=Heavy Brute Hook, 2=Caster Spell)
        AddAnyStateAction(sm, locoState, "Attack", scratchClip, 0.07f, 0.86f, stateName: "Attack_Scratch", speed: 1.25f, intParamName: "AttackType", intParamValue: 0, boolGuardParam: "IsDead", boolGuardExpected: false);
        AddAnyStateAction(sm, locoState, "Attack", heavyClip, 0.07f, 0.86f, stateName: "Attack_Heavy", speed: 1.05f, intParamName: "AttackType", intParamValue: 1, boolGuardParam: "IsDead", boolGuardExpected: false);
        AddAnyStateAction(sm, locoState, "Attack", castClip, 0.06f, 0.86f, stateName: "Attack_Cast", speed: 1.15f, intParamName: "AttackType", intParamValue: 2, boolGuardParam: "IsDead", boolGuardExpected: false);
        AddAnyStateAction(sm, locoState, "Hit", hitClip, 0.05f, 0.80f, stateName: "Hit", speed: 1.25f, boolGuardParam: "IsDead", boolGuardExpected: false);

        // 3. Real Humanoid Death Animation (menggantikan efek miring kaku)
        AnimatorState deadState = sm.AddState("Dead");
        deadState.motion = deathClip;
        deadState.iKOnFeet = false;

        AnimatorStateTransition anyToDead = sm.AddAnyStateTransition(deadState);
        anyToDead.hasExitTime = false;
        anyToDead.duration = 0.08f;
        anyToDead.canTransitionToSelf = false;
        anyToDead.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

        Debug.Log($"[Project EAS Anim] Monster Upgraded -> BlendTree({DescribeClip(idleClip)}/{DescribeClip(walkClip)}/{DescribeClip(runClip)}), Attacks({DescribeClip(scratchClip)}/{DescribeClip(heavyClip)}/{DescribeClip(castClip)}), Death={DescribeClip(deathClip)}");
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    private static void AddAnyStateAction(
        AnimatorStateMachine sm,
        AnimatorState returnState,
        string triggerParam,
        AnimationClip clip,
        float transitionDuration,
        float exitTime,
        string stateName = null,
        float speed = 1f,
        string intParamName = null,
        int? intParamValue = null,
        string boolGuardParam = null,
        bool? boolGuardExpected = null)
    {
        AnimatorState state = sm.AddState(string.IsNullOrEmpty(stateName) ? triggerParam : stateName);
        state.motion = clip;
        state.speed = speed;
        state.iKOnFeet = true;

        AnimatorStateTransition fromAny = sm.AddAnyStateTransition(state);
        fromAny.hasExitTime = false;
        fromAny.duration = transitionDuration;
        fromAny.canTransitionToSelf = false;
        fromAny.AddCondition(AnimatorConditionMode.If, 0f, triggerParam);
        if (!string.IsNullOrEmpty(intParamName) && intParamValue.HasValue)
        {
            fromAny.AddCondition(AnimatorConditionMode.Equals, intParamValue.Value, intParamName);
        }
        if (!string.IsNullOrEmpty(boolGuardParam) && boolGuardExpected.HasValue)
        {
            fromAny.AddCondition(boolGuardExpected.Value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, boolGuardParam);
        }

        AnimatorStateTransition toReturn = state.AddTransition(returnState);
        toReturn.hasExitTime = true;
        toReturn.exitTime = exitTime;
        toReturn.duration = 0.10f;
    }

    // =========================================================================
    // 4. Prefab Builders (Characters, NPCs, Monsters + Kepala/Rambut Asli)
    // =========================================================================
    private static GameObject BuildCharacterVisualPrefab(
        string prefabPath,
        string fbxPath,
        AnimatorController ctrl,
        Avatar masterAvatar,
        Material outfitMat,
        Material skinMat,
        Material altMat,
        Material hairMat,
        bool isFemale,
        bool hasHood,
        float uniformScale,
        string[] hairFbxNames = null,
        string weaponFbxPath = null)
    {
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxPrefab == null)
        {
            Debug.LogError("[EAS Setup] FBX tidak ditemukan: " + fbxPath);
            return null;
        }

        GameObject inst = Object.Instantiate(fbxPrefab);
        inst.transform.localScale = Vector3.one * uniformScale;

        AssignMaterialsRecursive(inst, outfitMat, skinMat, altMat);
        AttachBaseCharacterHeadAndHair(inst, skinMat, hairMat, isFemale, hasHood, hairFbxNames);
        if (!string.IsNullOrEmpty(weaponFbxPath))
        {
            AttachWeaponToRightHand(inst, weaponFbxPath);
        }
        ConfigureAnimator(inst, fbxPath, ctrl, masterAvatar);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
        Object.DestroyImmediate(inst);
        return saved;
    }

    // =========================================================================
    // 4b. Weapon Loading, URP Material Resolution & Right-Hand Bone Attachment
    // =========================================================================
    private static string ResolveExistingWeaponPath(string primaryPath, string fallbackPath)
    {
        if (!string.IsNullOrEmpty(primaryPath) && FileExistsAsAsset(primaryPath)) return primaryPath;
        if (!string.IsNullOrEmpty(fallbackPath) && FileExistsAsAsset(fallbackPath)) return fallbackPath;
        return primaryPath;
    }

    /// <summary>
    /// Memuat mesh senjata asli dari Assets/Weapons, memperbaiki shader ke URP dengan tekstur/material asli,
    /// dan memasangkannya ke bone hand_r sesuai orientasi buku jari dan bounds mesh senjata.
    /// </summary>
    private static void AttachWeaponToRightHand(GameObject characterRoot, string weaponFbxPath)
    {
        if (characterRoot == null || string.IsNullOrEmpty(weaponFbxPath)) return;

        GameObject weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(weaponFbxPath);
        if (weaponPrefab == null)
        {
            Debug.LogWarning($"[Project EAS Weapon] FBX senjata tidak ditemukan: '{weaponFbxPath}' (tidak memakai senjata primitif).");
            return;
        }

        Transform handR = FindBoneRecursive(characterRoot.transform, "hand_r") ??
                          FindBoneRecursive(characterRoot.transform, "RightHand");
        if (handR == null)
        {
            Debug.LogWarning($"[Project EAS Weapon] Bone 'hand_r' tidak ditemukan pada '{characterRoot.name}'.");
            return;
        }

        DestroyChildIfExists(handR, "WeaponGrip_RightHand");

        GameObject gripPivot = new GameObject("WeaponGrip_RightHand");
        GameObject weaponInst = Object.Instantiate(weaponPrefab);
        weaponInst.name = Path.GetFileNameWithoutExtension(weaponFbxPath);
        weaponInst.transform.localPosition = Vector3.zero;
        weaponInst.transform.localRotation = Quaternion.identity;
        weaponInst.transform.localScale = Vector3.one;

        // Bersihkan komponen non-visual pada instance senjata agar tidak mengganggu physics/Animator player
        Animator[] weaponAnims = weaponInst.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < weaponAnims.Length; i++) Object.DestroyImmediate(weaponAnims[i]);
        Collider[] weaponCols = weaponInst.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < weaponCols.Length; i++) Object.DestroyImmediate(weaponCols[i]);
        Rigidbody[] weaponRbs = weaponInst.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < weaponRbs.Length; i++) Object.DestroyImmediate(weaponRbs[i]);

        int matCount = ResolveWeaponMaterials(weaponInst, weaponFbxPath);

        if (!TryComputeCombinedLocalBounds(weaponInst, out Bounds localBounds))
        {
            Debug.LogWarning($"[Project EAS Weapon] '{weaponFbxPath}' tidak memiliki MeshFilter/Renderer valid.");
            Object.DestroyImmediate(gripPivot);
            Object.DestroyImmediate(weaponInst);
            return;
        }

        // Deteksi sumbu panjang bilah (largest), sumbu crossguard/edge (medium), dan ketebalan (smallest)
        int bladeAxis = 0;
        if (localBounds.size.y >= localBounds.size.x && localBounds.size.y >= localBounds.size.z) bladeAxis = 1;
        else if (localBounds.size.z >= localBounds.size.x && localBounds.size.z >= localBounds.size.y) bladeAxis = 2;

        int edgeAxis = (bladeAxis + 1) % 3;
        int otherAxis = (bladeAxis + 2) % 3;
        if (GetAxisComponent(localBounds.size, otherAxis) > GetAxisComponent(localBounds.size, edgeAxis))
        {
            edgeAxis = otherAxis;
        }

        float bMin = GetAxisComponent(localBounds.min, bladeAxis);
        float bMax = GetAxisComponent(localBounds.max, bladeAxis);
        float tipSign = Mathf.Abs(bMin) > Mathf.Abs(bMax) * 1.15f ? -1f : 1f;
        float rawLength = Mathf.Max(0.001f, bMax - bMin);
        float unitScale = 1f;
        if (rawLength > 5.0f) unitScale = 0.01f;
        else if (rawLength < 0.15f) unitScale = 100f;

        Vector3 weaponBladeLocal = AxisVector(bladeAxis) * tipSign;
        Vector3 weaponEdgeLocal = AxisVector(edgeAxis);

        // Hitung arah genggaman anatomis di hand_r: bilah mengarah dari kelingking ke telunjuk/ibu jari,
        // sedangkan lebar crossguard/mata bilah sejajar arah buku jari (wrist -> middle_01_r).
        Transform indexBone = FindBoneRecursive(handR, "index_01_r") ?? FindBoneRecursive(handR, "index_02_r");
        Transform pinkyBone = FindBoneRecursive(handR, "pinky_01_r") ?? FindBoneRecursive(handR, "pinky_02_r");
        Transform middleBone = FindBoneRecursive(handR, "middle_01_r") ?? indexBone;

        Vector3 desiredBladeWorld;
        Vector3 desiredKnuckleWorld;
        if (indexBone != null && pinkyBone != null && middleBone != null &&
            (indexBone.position - pinkyBone.position).sqrMagnitude > 0.00001f)
        {
            desiredBladeWorld = (indexBone.position - pinkyBone.position).normalized;
            desiredKnuckleWorld = (middleBone.position - handR.position).normalized;
        }
        else
        {
            desiredBladeWorld = characterRoot.transform.forward;
            desiredKnuckleWorld = -handR.forward;
        }

        Vector3 desiredEdgeWorld = Vector3.ProjectOnPlane(desiredKnuckleWorld, desiredBladeWorld);
        if (desiredEdgeWorld.sqrMagnitude < 0.0001f) desiredEdgeWorld = desiredKnuckleWorld;
        desiredEdgeWorld.Normalize();

        Vector3 palmInwardWorld = Vector3.Cross(desiredKnuckleWorld, desiredBladeWorld).normalized;
        if (Vector3.Dot(palmInwardWorld, -characterRoot.transform.up) < 0f)
        {
            palmInwardWorld = -palmInwardWorld;
        }

        Quaternion targetWorldRot =
            Quaternion.LookRotation(desiredBladeWorld, desiredEdgeWorld) *
            Quaternion.Inverse(Quaternion.LookRotation(weaponBladeLocal, weaponEdgeLocal));

        gripPivot.transform.position = handR.position + desiredKnuckleWorld * 0.065f + palmInwardWorld * 0.022f;
        gripPivot.transform.rotation = targetWorldRot;
        gripPivot.transform.SetParent(handR, true);

        weaponInst.transform.SetParent(gripPivot.transform, false);
        weaponInst.transform.localScale = Vector3.one * unitScale;

        float scaledLength = rawLength * unitScale;
        float projMin = (tipSign > 0f ? bMin : -bMax) * unitScale;
        float projMax = (tipSign > 0f ? bMax : -bMin) * unitScale;
        bool hasGripPivot = projMin <= -0.05f && projMin >= -0.35f * scaledLength && projMax >= 0.40f * scaledLength;
        float gripShift = hasGripPivot ? 0f : -(projMin + Mathf.Clamp(scaledLength * 0.14f, 0.09f, 0.20f));
        weaponInst.transform.localPosition = weaponBladeLocal * gripShift;

        Debug.Log($"[Project EAS Weapon] '{weaponInst.name}' terpasang ke '{characterRoot.name}/hand_r' " +
                  $"(len={scaledLength:F2}m, mats={matCount}, gripShift={gripShift:F3}).");
    }

    private static bool TryComputeCombinedLocalBounds(GameObject root, out Bounds combined)
    {
        combined = default;
        bool hasAny = false;
        Matrix4x4 rootInv = root.transform.worldToLocalMatrix;

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i] == null || filters[i].sharedMesh == null) continue;
            Matrix4x4 toRoot = rootInv * filters[i].transform.localToWorldMatrix;
            EncapsulateTransformedBounds(ref combined, ref hasAny, filters[i].sharedMesh.bounds, toRoot);
        }

        SkinnedMeshRenderer[] skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinned.Length; i++)
        {
            if (skinned[i] == null || skinned[i].sharedMesh == null) continue;
            Matrix4x4 toRoot = rootInv * skinned[i].transform.localToWorldMatrix;
            EncapsulateTransformedBounds(ref combined, ref hasAny, skinned[i].sharedMesh.bounds, toRoot);
        }

        return hasAny;
    }

    private static void EncapsulateTransformedBounds(ref Bounds combined, ref bool hasAny, Bounds localB, Matrix4x4 toRoot)
    {
        Vector3 c = localB.center;
        Vector3 e = localB.extents;
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sy = -1; sy <= 1; sy += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Vector3 pt = toRoot.MultiplyPoint3x4(c + new Vector3(e.x * sx, e.y * sy, e.z * sz));
                    if (!hasAny)
                    {
                        combined = new Bounds(pt, Vector3.zero);
                        hasAny = true;
                    }
                    else
                    {
                        combined.Encapsulate(pt);
                    }
                }
            }
        }
    }

    private static float GetAxisComponent(Vector3 v, int axis) => axis == 0 ? v.x : (axis == 1 ? v.y : v.z);

    private static Vector3 AxisVector(int axis) => axis == 0 ? Vector3.right : (axis == 1 ? Vector3.up : Vector3.forward);

    private static int ResolveWeaponMaterials(GameObject weaponRoot, string weaponFbxPath)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Renderer[] renderers = weaponRoot.GetComponentsInChildren<Renderer>(true);
        int resolvedSlots = 0;

        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer rend = renderers[r];
            if (rend == null) continue;
            rend.enabled = true;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            rend.receiveShadows = true;

            Material[] current = rend.sharedMaterials;
            int count = Mathf.Max(1, current.Length);
            Material[] next = new Material[count];

            for (int i = 0; i < count; i++)
            {
                Material srcMat = i < current.Length ? current[i] : null;
                Material resolved = FindOrCreateWeaponUrpMaterial(srcMat, weaponFbxPath, urpLit);
                next[i] = resolved != null ? resolved : srcMat;
                if (next[i] != null) resolvedSlots++;
            }

            rend.sharedMaterials = next;
        }
        return resolvedSlots;
    }

    private static Material FindOrCreateWeaponUrpMaterial(Material srcMat, string weaponFbxPath, Shader urpLit)
    {
        if (urpLit == null) return srcMat;
        if (srcMat != null && srcMat.shader == urpLit) return srcMat;

        string weaponName = Path.GetFileNameWithoutExtension(weaponFbxPath);
        string srcName = srcMat != null && !string.IsNullOrEmpty(srcMat.name) ? srcMat.name : weaponName;

        // 1. Cari material URP asli yang sudah ada di Assets/Weapons
        if (AssetDatabase.IsValidFolder(WeaponsDir))
        {
            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { WeaponsDir });
            for (int i = 0; i < matGuids.Length; i++)
            {
                Material cand = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(matGuids[i]));
                if (cand == null || cand.shader != urpLit) continue;
                if (string.Equals(cand.name, srcName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(cand.name, srcName + "_URP", StringComparison.OrdinalIgnoreCase))
                {
                    return cand;
                }
            }
        }

        // 2. Upgrade material Standard bawaan FBX ke URP Material aset nyata (tanpa warna/tekstur generated)
        EnsureFolder(WeaponsMatDir);
        string cleanName = srcName.Replace("/", "_").Replace("\\", "_").Trim();
        if (string.IsNullOrEmpty(cleanName)) cleanName = weaponName;
        string savePath = WeaponsMatDir + "/" + cleanName + "_URP.mat";

        Material urpMat = AssetDatabase.LoadAssetAtPath<Material>(savePath);
        bool isNew = urpMat == null;
        if (isNew)
        {
            urpMat = new Material(urpLit);
        }
        else if (urpMat.shader != urpLit)
        {
            urpMat.shader = urpLit;
        }

        Texture baseTex = srcMat != null ? (srcMat.HasProperty("_MainTex") ? srcMat.GetTexture("_MainTex") : null) : null;
        if (baseTex == null && srcMat != null && srcMat.HasProperty("_BaseMap")) baseTex = srcMat.GetTexture("_BaseMap");
        Texture normalTex = srcMat != null && srcMat.HasProperty("_BumpMap") ? srcMat.GetTexture("_BumpMap") : null;
        Texture metalTex = srcMat != null && srcMat.HasProperty("_MetallicGlossMap") ? srcMat.GetTexture("_MetallicGlossMap") : null;
        Texture occTex = srcMat != null && srcMat.HasProperty("_OcclusionMap") ? srcMat.GetTexture("_OcclusionMap") : null;
        Texture emisTex = srcMat != null && srcMat.HasProperty("_EmissionMap") ? srcMat.GetTexture("_EmissionMap") : null;

        if (baseTex == null && Directory.Exists(WeaponsDir))
        {
            string[] texFiles = Directory.GetFiles(WeaponsDir, "*.*", System.IO.SearchOption.AllDirectories);
            baseTex = FindBestWeaponTexture(texFiles, weaponName, srcName, "BaseColor", "Albedo", "Diffuse", "Color", "Palette", "Atlas");
            if (normalTex == null) normalTex = FindBestWeaponTexture(texFiles, weaponName, srcName, "Normal", "NRM", "Bump");
            if (metalTex == null) metalTex = FindBestWeaponTexture(texFiles, weaponName, srcName, "Metallic", "Metal", "Roughness", "Mask");
        }

        if (baseTex != null && urpMat.HasProperty("_BaseMap")) urpMat.SetTexture("_BaseMap", baseTex);
        if (srcMat != null && srcMat.HasProperty("_Color") && urpMat.HasProperty("_BaseColor"))
        {
            urpMat.SetColor("_BaseColor", srcMat.GetColor("_Color"));
        }
        if (normalTex != null && urpMat.HasProperty("_BumpMap"))
        {
            urpMat.SetTexture("_BumpMap", normalTex);
            urpMat.EnableKeyword("_NORMALMAP");
        }
        if (metalTex != null && urpMat.HasProperty("_MetallicGlossMap"))
        {
            urpMat.SetTexture("_MetallicGlossMap", metalTex);
            urpMat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        if (occTex != null && urpMat.HasProperty("_OcclusionMap")) urpMat.SetTexture("_OcclusionMap", occTex);
        if (emisTex != null && urpMat.HasProperty("_EmissionMap"))
        {
            urpMat.SetTexture("_EmissionMap", emisTex);
            if (srcMat != null && srcMat.HasProperty("_EmissionColor") && urpMat.HasProperty("_EmissionColor"))
            {
                urpMat.SetColor("_EmissionColor", srcMat.GetColor("_EmissionColor"));
            }
            urpMat.EnableKeyword("_EMISSION");
        }
        if (srcMat != null && srcMat.HasProperty("_Metallic") && urpMat.HasProperty("_Metallic"))
        {
            urpMat.SetFloat("_Metallic", srcMat.GetFloat("_Metallic"));
        }
        if (srcMat != null && srcMat.HasProperty("_Glossiness") && urpMat.HasProperty("_Smoothness"))
        {
            urpMat.SetFloat("_Smoothness", srcMat.GetFloat("_Glossiness"));
        }

        if (baseTex == null && (srcMat == null || !srcMat.HasProperty("_Color")))
        {
            Debug.LogWarning($"[Project EAS Weapon] Tekstur untuk '{weaponFbxPath}' (material '{srcName}') tidak ditemukan di '{WeaponsDir}'.");
        }

        if (isNew) AssetDatabase.CreateAsset(urpMat, savePath);
        else EditorUtility.SetDirty(urpMat);

        return urpMat;
    }

    private static Texture2D FindBestWeaponTexture(string[] allFiles, string weaponName, string matName, params string[] roleKeywords)
    {
        string bestPath = null;
        int bestScore = 0;

        for (int i = 0; i < allFiles.Length; i++)
        {
            string p = allFiles[i].Replace('\\', '/');
            string ext = Path.GetExtension(p).ToLowerInvariant();
            if (ext != ".png" && ext != ".tga" && ext != ".jpg" && ext != ".jpeg" && ext != ".psd" && ext != ".tif") continue;

            string fileNoExt = Path.GetFileNameWithoutExtension(p);
            int score = 0;

            if (fileNoExt.IndexOf(weaponName, StringComparison.OrdinalIgnoreCase) >= 0) score += 20;
            if (!string.IsNullOrEmpty(matName) && fileNoExt.IndexOf(matName, StringComparison.OrdinalIgnoreCase) >= 0) score += 15;

            bool matchedRole = false;
            for (int k = 0; k < roleKeywords.Length; k++)
            {
                if (fileNoExt.IndexOf(roleKeywords[k], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 10;
                    matchedRole = true;
                    break;
                }
            }

            // Hindari tekstur normal/metallic tertukar menjadi BaseColor
            bool looksNonAlbedo = fileNoExt.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  fileNoExt.IndexOf("Bump", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  fileNoExt.IndexOf("Metallic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  fileNoExt.IndexOf("Roughness", StringComparison.OrdinalIgnoreCase) >= 0;
            bool seekingAlbedo = Array.IndexOf(roleKeywords, "BaseColor") >= 0;
            if (seekingAlbedo && looksNonAlbedo) continue;

            // Izinkan tekstur palette umum di folder Weapons jika mencari BaseColor dan nama mengandung weapon/tex/palette
            if (seekingAlbedo && !matchedRole && score == 0)
            {
                if (fileNoExt.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fileNoExt.IndexOf("Texture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fileNoExt.IndexOf("Tex", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    score += 4;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPath = p;
            }
        }

        return !string.IsNullOrEmpty(bestPath) ? AssetDatabase.LoadAssetAtPath<Texture2D>(bestPath) : null;
    }

    private static GameObject BuildNpcPrefab(
        string prefabPath,
        string fbxPath,
        AnimatorController ctrl,
        Avatar masterAvatar,
        Material outfitMat,
        Material skinMat,
        Material altMat,
        Material hairMat,
        bool isFemale,
        bool hasHood,
        float uniformScale,
        string[] hairFbxNames = null,
        string weaponFbxPath = null)
    {
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxPrefab == null) return null;

        GameObject inst = Object.Instantiate(fbxPrefab);
        inst.transform.localScale = Vector3.one * uniformScale;

        AssignMaterialsRecursive(inst, outfitMat, skinMat, altMat);
        AttachBaseCharacterHeadAndHair(inst, skinMat, hairMat, isFemale, hasHood, hairFbxNames);
        if (!string.IsNullOrEmpty(weaponFbxPath))
        {
            AttachWeaponToRightHand(inst, weaponFbxPath);
        }
        ConfigureAnimator(inst, fbxPath, ctrl, masterAvatar);

        CapsuleCollider col = inst.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.92f, 0f);
        col.radius = 0.35f;
        col.height = 1.84f;

        NavMeshAgent agent = inst.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f;
        agent.height = 1.84f;
        agent.speed = 1.85f;
        agent.angularSpeed = 360f;
        agent.stoppingDistance = 0.5f;

        inst.AddComponent<VillageNPCController>();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
        Object.DestroyImmediate(inst);
        return saved;
    }

    private static GameObject BuildMonsterPrefab(
        string prefabPath,
        string fbxPath,
        AnimatorController ctrl,
        Material monsterMat,
        float uniformScale,
        EnemyAIController.EnemyArchetype archetype,
        float maxHp,
        float damage,
        float attackRange,
        float patrolRadius,
        float moveSpeed,
        float agentRadius,
        float agentHeight,
        float stoppingDistance)
    {
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxPrefab == null)
        {
            Debug.LogError("[EAS Setup] Monster FBX tidak ditemukan: " + fbxPath);
            return null;
        }

        GameObject inst = Object.Instantiate(fbxPrefab);
        inst.transform.localScale = Vector3.one * uniformScale;

        AssignMaterialsRecursive(inst, monsterMat, monsterMat, monsterMat);
        ConfigureAnimator(inst, fbxPath, ctrl, null);

        float safeScale = Mathf.Max(0.1f, uniformScale);
        CapsuleCollider col = inst.AddComponent<CapsuleCollider>();
        col.radius = agentRadius / safeScale;
        col.height = agentHeight / safeScale;
        col.center = new Vector3(0f, col.height * 0.5f, 0f);

        NavMeshAgent agent = inst.AddComponent<NavMeshAgent>();
        agent.radius = agentRadius;
        agent.height = agentHeight;
        agent.speed = moveSpeed;
        agent.angularSpeed = 420f;
        agent.acceleration = 12f;
        agent.stoppingDistance = stoppingDistance;

        EnemyAIController ai = inst.AddComponent<EnemyAIController>();
        ai.archetype = archetype;
        ai.maxHp = maxHp;
        ai.attackDamage = damage;
        ai.attackRange = attackRange;
        ai.patrolRadius = patrolRadius;
        ai.moveSpeed = moveSpeed;
        ai.fallbackController = ctrl;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
        Object.DestroyImmediate(inst);

        Animator savedAnim = saved != null ? saved.GetComponentInChildren<Animator>() : null;
        if (savedAnim == null || savedAnim.runtimeAnimatorController == null)
        {
            Debug.LogError($"[Project EAS Monster] Prefab '{prefabPath}' tersimpan TANPA AnimatorController!");
        }
        return saved;
    }

    /// <summary>
    /// Memasang mesh kepala, mata, alis, dan variasi gaya rambut asli dari Universal Base Characters[Standard].
    /// Tidak membuat/meng-generate mesh primitif buatan jika aset kepala belum tersedia.
    /// </summary>
    private static void AttachBaseCharacterHeadAndHair(
        GameObject characterRoot,
        Material skinMat,
        Material hairMat,
        bool isFemale,
        bool hasHood,
        string[] hairFbxNames)
    {
        Transform headBone = FindBoneRecursive(characterRoot.transform, "Head") ??
                             FindBoneRecursive(characterRoot.transform, "head");
        if (headBone == null) return;

        Transform oldHead = headBone.Find("Stylized_Head_Visual");
        if (oldHead != null) Object.DestroyImmediate(oldHead.gameObject);

        if (!TryAttachRealBaseCharacterHead(characterRoot, headBone, skinMat, hairMat, isFemale, hasHood, hairFbxNames))
        {
            Debug.LogError($"[Project EAS Head] Aset kepala dari '{BaseCharsDir}' tidak ditemukan untuk '{characterRoot.name}'. " +
                           "Pastikan folder Universal Base Characters[Standard] sudah ter-import di Assets/ (tidak memakai kepala generated).");
        }
    }

    private static Transform FindBoneRecursive(Transform current, string targetName)
    {
        if (current == null) return null;
        if (string.Equals(current.name, targetName, StringComparison.Ordinal)) return current;
        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindBoneRecursive(current.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
    }

    private static bool TryAttachRealBaseCharacterHead(
        GameObject characterRoot,
        Transform headBone,
        Material fallbackSkinMat,
        Material hairMat,
        bool isFemale,
        bool hasHood,
        string[] hairFbxNames)
    {
        FindBaseHeadAndHairPaths(isFemale, out string headPath, out string defaultHairPath);
        if (string.IsNullOrEmpty(headPath)) return false;

        var targetBones = new Dictionary<string, Transform>(StringComparer.Ordinal);
        Transform[] charTransforms = characterRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < charTransforms.Length; i++)
        {
            if (!targetBones.ContainsKey(charTransforms[i].name))
            {
                targetBones.Add(charTransforms[i].name, charTransforms[i]);
            }
        }

        bool attachedHair = false;
        bool attachedHead = AttachBaseModelParts(
            headPath, characterRoot, headBone, targetBones,
            fallbackSkinMat, null, hairMat,
            isFemale: isFemale,
            isDedicatedHeadFile: IsDedicatedHeadFile(headPath),
            allowHair: !hasHood,
            ref attachedHair);

        if (attachedHead && !hasHood && !attachedHair)
        {
            if (hairFbxNames != null && hairFbxNames.Length > 0)
            {
                for (int h = 0; h < hairFbxNames.Length; h++)
                {
                    string explicitHairPath = ResolveHairAssetPath(hairFbxNames[h]);
                    if (string.IsNullOrEmpty(explicitHairPath))
                    {
                        Debug.LogWarning($"[Project EAS Hair] File rambut '{hairFbxNames[h]}' tidak ditemukan di '{BaseHairRiggedDir}'.");
                        continue;
                    }
                    bool dummy = false;
                    AttachBaseModelParts(
                        explicitHairPath, characterRoot, headBone, targetBones,
                        hairMat ?? fallbackSkinMat, null, hairMat,
                        isFemale: isFemale,
                        isDedicatedHeadFile: true,
                        allowHair: true,
                        ref dummy);
                }
            }
            else if (!string.IsNullOrEmpty(defaultHairPath))
            {
                bool dummy = false;
                AttachBaseModelParts(
                    defaultHairPath, characterRoot, headBone, targetBones,
                    hairMat ?? fallbackSkinMat, null, hairMat,
                    isFemale: isFemale,
                    isDedicatedHeadFile: true,
                    allowHair: true,
                    ref dummy);
            }
        }

        if (attachedHead)
        {
            string hairLabel = hasHood ? "Hooded" : (hairFbxNames != null ? string.Join("+", hairFbxNames) : Path.GetFileName(defaultHairPath));
            Debug.Log($"[Project EAS Head] Memakai kepala asli '{Path.GetFileName(headPath)}' ({hairLabel}) untuk '{characterRoot.name}'.");
        }
        return attachedHead;
    }

    private static string ResolveHairAssetPath(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        string riggedPath = BaseHairRiggedDir + "/" + fileName;
        if (FileExistsAsAsset(riggedPath)) return riggedPath;

        string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName) + " t:Model", new[] { BaseCharsDir });
        for (int i = 0; i < guids.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (p.IndexOf("Rigged to Head Bone", StringComparison.OrdinalIgnoreCase) >= 0 &&
                p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }
        return guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
    }

    private static bool AttachBaseModelParts(
        string modelPath,
        GameObject characterRoot,
        Transform headBone,
        Dictionary<string, Transform> targetBones,
        Material fallbackSkinMat,
        Material fallbackEyeMat,
        Material fallbackHairMat,
        bool isFemale,
        bool isDedicatedHeadFile,
        bool allowHair,
        ref bool attachedHair)
    {
        ModelImporter imp = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (imp != null)
        {
            bool impDirty = false;
            if (!imp.bakeAxisConversion)
            {
                imp.bakeAxisConversion = true;
                impDirty = true;
            }
            if (!isDedicatedHeadFile && !imp.isReadable)
            {
                imp.isReadable = true;
                impDirty = true;
            }
            if (impDirty) imp.SaveAndReimport();
        }

        GameObject baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (baseAsset == null) return false;

        GameObject tempBase = Object.Instantiate(baseAsset);
        tempBase.transform.position = Vector3.zero;
        tempBase.transform.rotation = Quaternion.identity;
        tempBase.transform.localScale = Vector3.one;

        bool attachedAny = false;
        try
        {
            SkinnedMeshRenderer[] smrs = tempBase.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < smrs.Length; i++)
            {
                SkinnedMeshRenderer srcSmr = smrs[i];
                if (srcSmr == null || srcSmr.sharedMesh == null) continue;

                string n = srcSmr.gameObject.name;
                bool looksHair = n.IndexOf("hair", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 n.IndexOf("beard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 n.IndexOf("mustache", StringComparison.OrdinalIgnoreCase) >= 0;
                if (looksHair && !allowHair) continue;

                bool looksBrow = n.IndexOf("brow", StringComparison.OrdinalIgnoreCase) >= 0;
                bool looksEye = n.IndexOf("eye", StringComparison.OrdinalIgnoreCase) >= 0 && !looksBrow;
                bool looksBodyOnly = n.IndexOf("torso", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     n.IndexOf("leg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     n.IndexOf("arm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     n.IndexOf("hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     n.IndexOf("foot", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     n.IndexOf("feet", StringComparison.OrdinalIgnoreCase) >= 0;

                bool isFullBodyCharacterMesh = !isDedicatedHeadFile &&
                                                (n.IndexOf("superhero", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                 n.IndexOf("fullbody", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                 n.IndexOf("regular", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                 n.IndexOf("body", StringComparison.OrdinalIgnoreCase) >= 0);

                bool isHeadPart = isDedicatedHeadFile
                    ? !looksBodyOnly
                    : (looksBrow || looksEye || looksHair || isFullBodyCharacterMesh ||
                       n.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       n.IndexOf("face", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       n.IndexOf("ear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       n.IndexOf("mouth", StringComparison.OrdinalIgnoreCase) >= 0);

                if (!isHeadPart) continue;

                Mesh meshToAttach = srcSmr.sharedMesh;
                int[] keptSubmeshIndices = null;
                if (isFullBodyCharacterMesh)
                {
                    string slicePath = GeneratedMeshDir + "/BaseHead_" + (isFemale ? "Female" : "Male") + ".asset";
                    meshToAttach = SliceHeadOnlyMesh(srcSmr, slicePath, out keptSubmeshIndices);
                    if (meshToAttach == null) continue;
                }

                GameObject partGo = new GameObject("Base_" + (isFullBodyCharacterMesh ? "Head" : n));
                partGo.transform.SetParent(characterRoot.transform, false);

                SkinnedMeshRenderer dstSmr = partGo.AddComponent<SkinnedMeshRenderer>();
                dstSmr.sharedMesh = meshToAttach;

                Material[] resolvedAll = ResolveBaseMaterials(
                    srcSmr.sharedMaterials,
                    looksEye ? fallbackEyeMat : (looksHair || looksBrow ? (fallbackHairMat ?? fallbackSkinMat) : fallbackSkinMat),
                    fallbackSkinMat,
                    fallbackEyeMat,
                    fallbackHairMat);

                if (keptSubmeshIndices != null && keptSubmeshIndices.Length > 0)
                {
                    Material[] filteredMats = new Material[keptSubmeshIndices.Length];
                    for (int m = 0; m < keptSubmeshIndices.Length; m++)
                    {
                        int subIdx = keptSubmeshIndices[m];
                        filteredMats[m] = subIdx < resolvedAll.Length ? resolvedAll[subIdx] : fallbackSkinMat;
                    }
                    dstSmr.sharedMaterials = filteredMats;
                }
                else
                {
                    dstSmr.sharedMaterials = resolvedAll;
                }

                Transform[] srcBones = srcSmr.bones;
                Transform[] remappedBones = new Transform[srcBones.Length];
                for (int b = 0; b < srcBones.Length; b++)
                {
                    string bName = srcBones[b] != null ? srcBones[b].name : string.Empty;
                    remappedBones[b] = targetBones.TryGetValue(bName, out Transform foundBone)
                        ? foundBone
                        : (headBone != null ? headBone : characterRoot.transform);
                }
                dstSmr.bones = remappedBones;

                string rootName = srcSmr.rootBone != null ? srcSmr.rootBone.name : "root";
                dstSmr.rootBone = targetBones.TryGetValue(rootName, out Transform rb)
                    ? rb
                    : (headBone != null ? headBone : characterRoot.transform);

                if (looksHair) attachedHair = true;
                attachedAny = true;
            }
        }
        finally
        {
            Object.DestroyImmediate(tempBase);
        }

        return attachedAny;
    }

    private static Mesh SliceHeadOnlyMesh(SkinnedMeshRenderer srcSmr, string saveAssetPath, out int[] keptSubmeshIndices)
    {
        keptSubmeshIndices = null;
        Mesh src = srcSmr != null ? srcSmr.sharedMesh : null;
        if (src == null || !src.isReadable) return null;

        Transform[] bones = srcSmr.bones;
        int headIdx = Array.FindIndex(bones, b => b != null && string.Equals(b.name, "Head", StringComparison.OrdinalIgnoreCase));
        int neckIdx = Array.FindIndex(bones, b => b != null && string.Equals(b.name, "neck_01", StringComparison.OrdinalIgnoreCase));
        if (headIdx < 0) return null;

        float neckWorldY = neckIdx >= 0 ? bones[neckIdx].position.y : (bones[headIdx].position.y - 0.08f);

        Vector3[] verts = src.vertices;
        Vector3[] norms = src.normals;
        Vector4[] tans = src.tangents;
        Vector2[] uvs = src.uv;
        Color[] cols = src.colors;
        BoneWeight[] bws = src.boneWeights;
        Matrix4x4[] bindposes = src.bindposes;

        bool hasNorms = norms != null && norms.Length == verts.Length;
        bool hasTans = tans != null && tans.Length == verts.Length;
        bool hasUvs = uvs != null && uvs.Length == verts.Length;
        bool hasCols = cols != null && cols.Length == verts.Length;
        bool hasBws = bws != null && bws.Length == verts.Length;

        bool[] keepVert = new bool[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            float headW = 0f;
            float neckW = 0f;
            Vector3 worldPos = Vector3.zero;
            float wSum = 0f;

            if (hasBws && bindposes != null && bindposes.Length == bones.Length)
            {
                BoneWeight bw = bws[i];
                AccumulateVertex(bw.boneIndex0, bw.weight0, headIdx, neckIdx, bones, bindposes, verts[i], ref headW, ref neckW, ref worldPos, ref wSum);
                AccumulateVertex(bw.boneIndex1, bw.weight1, headIdx, neckIdx, bones, bindposes, verts[i], ref headW, ref neckW, ref worldPos, ref wSum);
                AccumulateVertex(bw.boneIndex2, bw.weight2, headIdx, neckIdx, bones, bindposes, verts[i], ref headW, ref neckW, ref worldPos, ref wSum);
                AccumulateVertex(bw.boneIndex3, bw.weight3, headIdx, neckIdx, bones, bindposes, verts[i], ref headW, ref neckW, ref worldPos, ref wSum);
            }

            if (wSum <= 0.0001f)
            {
                worldPos = srcSmr.transform.TransformPoint(verts[i]);
            }

            bool isHead = headW >= 0.35f;
            bool isUpperNeck = (headW + neckW) >= 0.72f && worldPos.y >= (neckWorldY + 0.015f);
            keepVert[i] = isHead || isUpperNeck;
        }

        var oldToNew = new Dictionary<int, int>(1024);
        var newVerts = new List<Vector3>(1024);
        var newNorms = hasNorms ? new List<Vector3>(1024) : null;
        var newTans = hasTans ? new List<Vector4>(1024) : null;
        var newUvs = hasUvs ? new List<Vector2>(1024) : null;
        var newCols = hasCols ? new List<Color>(1024) : null;
        var newBws = hasBws ? new List<BoneWeight>(1024) : null;

        var survivingSubIndices = new List<int>();
        var survivingSubTris = new List<List<int>>();

        for (int s = 0; s < src.subMeshCount; s++)
        {
            int[] tris = src.GetTriangles(s);
            var outTris = new List<int>();

            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                int i0 = tris[t];
                int i1 = tris[t + 1];
                int i2 = tris[t + 2];
                if (!keepVert[i0] || !keepVert[i1] || !keepVert[i2]) continue;

                outTris.Add(RemapVert(i0, oldToNew, verts, norms, tans, uvs, cols, bws, newVerts, newNorms, newTans, newUvs, newCols, newBws));
                outTris.Add(RemapVert(i1, oldToNew, verts, norms, tans, uvs, cols, bws, newVerts, newNorms, newTans, newUvs, newCols, newBws));
                outTris.Add(RemapVert(i2, oldToNew, verts, norms, tans, uvs, cols, bws, newVerts, newNorms, newTans, newUvs, newCols, newBws));
            }

            if (outTris.Count > 0)
            {
                survivingSubIndices.Add(s);
                survivingSubTris.Add(outTris);
            }
        }

        if (newVerts.Count == 0 || survivingSubTris.Count == 0) return null;

        Mesh dst = new Mesh { name = Path.GetFileNameWithoutExtension(saveAssetPath) };
        dst.SetVertices(newVerts);
        if (newNorms != null) dst.SetNormals(newNorms);
        if (newTans != null) dst.SetTangents(newTans);
        if (newUvs != null) dst.SetUVs(0, newUvs);
        if (newCols != null) dst.SetColors(newCols);
        if (newBws != null) dst.boneWeights = newBws.ToArray();
        dst.bindposes = bindposes;
        dst.subMeshCount = survivingSubTris.Count;

        for (int s = 0; s < survivingSubTris.Count; s++)
        {
            dst.SetTriangles(survivingSubTris[s], s);
        }
        dst.RecalculateBounds();

        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(saveAssetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(dst, existing);
            Object.DestroyImmediate(dst);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            keptSubmeshIndices = survivingSubIndices.ToArray();
            return existing;
        }

        AssetDatabase.CreateAsset(dst, saveAssetPath);
        AssetDatabase.SaveAssets();
        keptSubmeshIndices = survivingSubIndices.ToArray();
        return AssetDatabase.LoadAssetAtPath<Mesh>(saveAssetPath);
    }

    private static void AccumulateVertex(
        int boneIndex,
        float weight,
        int headIdx,
        int neckIdx,
        Transform[] bones,
        Matrix4x4[] bindposes,
        Vector3 localVert,
        ref float headW,
        ref float neckW,
        ref Vector3 worldPos,
        ref float wSum)
    {
        if (weight <= 0f || boneIndex < 0 || boneIndex >= bones.Length) return;
        if (boneIndex == headIdx) headW += weight;
        if (boneIndex == neckIdx) neckW += weight;
        if (bones[boneIndex] != null)
        {
            Matrix4x4 m = bones[boneIndex].localToWorldMatrix * bindposes[boneIndex];
            worldPos += m.MultiplyPoint3x4(localVert) * weight;
            wSum += weight;
        }
    }

    private static int RemapVert(
        int oldIdx,
        Dictionary<int, int> oldToNew,
        Vector3[] verts,
        Vector3[] norms,
        Vector4[] tans,
        Vector2[] uvs,
        Color[] cols,
        BoneWeight[] bws,
        List<Vector3> newVerts,
        List<Vector3> newNorms,
        List<Vector4> newTans,
        List<Vector2> newUvs,
        List<Color> newCols,
        List<BoneWeight> newBws)
    {
        if (oldToNew.TryGetValue(oldIdx, out int existing)) return existing;
        int next = newVerts.Count;
        oldToNew[oldIdx] = next;
        newVerts.Add(verts[oldIdx]);
        if (newNorms != null) newNorms.Add(norms[oldIdx]);
        if (newTans != null) newTans.Add(tans[oldIdx]);
        if (newUvs != null) newUvs.Add(uvs[oldIdx]);
        if (newCols != null) newCols.Add(cols[oldIdx]);
        if (newBws != null) newBws.Add(bws[oldIdx]);
        return next;
    }

    private static Material[] ResolveBaseMaterials(
        Material[] srcMats,
        Material defaultMat,
        Material fallbackSkinMat,
        Material fallbackEyeMat,
        Material fallbackHairMat)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        int count = srcMats != null && srcMats.Length > 0 ? srcMats.Length : 1;
        Material[] result = new Material[count];

        for (int i = 0; i < count; i++)
        {
            Material m = srcMats != null && i < srcMats.Length ? srcMats[i] : null;
            string matName = m != null ? m.name : string.Empty;

            // Ganti baju Superhero dengan kulit polos Regular agar cocok dengan pakaian Medieval
            if (matName.IndexOf("Superhero", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result[i] = fallbackSkinMat != null ? fallbackSkinMat : defaultMat;
                continue;
            }
            if (matName.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Material eyeFound = FindMaterialInBasePack("MI_Eyes");
                result[i] = eyeFound != null ? eyeFound : (fallbackEyeMat ?? defaultMat);
                continue;
            }

            if (matName.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0 && fallbackHairMat != null)
            {
                result[i] = fallbackHairMat;
                continue;
            }

            if (m == null || (urpLit != null && m.shader != urpLit))
            {
                Material found = !string.IsNullOrEmpty(matName) ? FindMaterialInBasePack(matName) : null;
                if (found != null) m = found;
                else if (matName.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0) m = fallbackHairMat ?? defaultMat;
                else m = defaultMat;
            }
            result[i] = m;
        }
        return result;
    }

    private static Material FindMaterialInBasePack(string matName)
    {
        const string baseFolder = "Assets/Universal Base Characters[Standard]";
        string[] roots = Directory.Exists(baseFolder) ? new[] { baseFolder, "Assets" } : new[] { "Assets" };
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        for (int r = 0; r < roots.Length; r++)
        {
            string[] guids = AssetDatabase.FindAssets(matName + " t:Material", new[] { roots[r] });
            for (int i = 0; i < guids.Length; i++)
            {
                Material cand = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (cand != null && (urpLit == null || cand.shader == urpLit))
                {
                    return cand;
                }
            }
        }
        return null;
    }

    private static bool IsDedicatedHeadFile(string path)
    {
        string f = Path.GetFileNameWithoutExtension(path);
        return f.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0 ||
               f.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void FindBaseHeadAndHairPaths(bool isFemale, out string bestHeadPath, out string bestHairPath)
    {
        bestHeadPath = null;
        bestHairPath = null;

        const string basePack = "Assets/Universal Base Characters[Standard]";
        string[] searchRoots = Directory.Exists(basePack) ? new[] { basePack } : new[] { "Assets" };
        string[] guids = AssetDatabase.FindAssets("t:Model", searchRoots);

        int bestHeadScore = -1;
        int bestHairScore = -1;

        for (int i = 0; i < guids.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (p.Contains("Polytope Studio") ||
                p.Contains("Medieval Village MegaKit") ||
                p.Contains("Modular Character Outfits") ||
                p.Contains("Bestiary") ||
                p.Contains("Universal Animation Library") ||
                p.Contains("Head_Hood") ||
                p.Contains("Outfits/") ||
                p.IndexOf("glTF", StringComparison.OrdinalIgnoreCase) >= 0 ||
                p.IndexOf("Unreal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                p.IndexOf("Godot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            string ext = Path.GetExtension(p);
            if (!string.Equals(ext, ".fbx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ext, ".blend", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string file = Path.GetFileNameWithoutExtension(p);
            bool hasFemale = file.IndexOf("Female", StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasMale = file.IndexOf("Male", StringComparison.OrdinalIgnoreCase) >= 0 && !hasFemale;
            bool matchesGender = isFemale ? hasFemale : hasMale;
            bool wrongGender = isFemale ? hasMale : hasFemale;

            int unityBonus = (p.IndexOf("FBX (Unity)", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              p.IndexOf("/Unity/", StringComparison.OrdinalIgnoreCase) >= 0) ? 40 :
                             (string.Equals(ext, ".fbx", StringComparison.OrdinalIgnoreCase) ? 20 : 0);

            if (file.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (wrongGender) continue;
                int styleBonus = isFemale
                    ? (file.IndexOf("Long", StringComparison.OrdinalIgnoreCase) >= 0 ? 45 :
                       file.IndexOf("Buns", StringComparison.OrdinalIgnoreCase) >= 0 ? 35 : 0)
                    : (file.IndexOf("SimpleParted", StringComparison.OrdinalIgnoreCase) >= 0 ? 45 :
                       file.IndexOf("Buzzed", StringComparison.OrdinalIgnoreCase) >= 0 ? 25 : 0);
                int rigBonus = p.IndexOf("Rigged to Head Bone", StringComparison.OrdinalIgnoreCase) >= 0 ? 30 : 0;
                int hairScore = unityBonus + rigBonus + styleBonus + (matchesGender ? 100 : 10);
                if (hairScore > bestHairScore)
                {
                    bestHairScore = hairScore;
                    bestHairPath = p;
                }
                continue;
            }

            if (wrongGender) continue;

            if (file.IndexOf("Arms", StringComparison.OrdinalIgnoreCase) >= 0 ||
                file.IndexOf("Legs", StringComparison.OrdinalIgnoreCase) >= 0 ||
                file.IndexOf("Feet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                file.IndexOf("Torso", StringComparison.OrdinalIgnoreCase) >= 0 ||
                file.IndexOf("Eyebrows", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            bool looksLikeHeadOrBase = file.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       file.IndexOf("Regular", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       file.IndexOf("FullBody", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       file.IndexOf("Base", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       file.IndexOf("Superhero", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       matchesGender;
            if (!looksLikeHeadOrBase) continue;

            int score = unityBonus +
                        (matchesGender ? 100 : 0) +
                        (file.IndexOf("Head", StringComparison.OrdinalIgnoreCase) >= 0 ? 50 : 0) +
                        (file.IndexOf("Regular", StringComparison.OrdinalIgnoreCase) >= 0 ? 30 : 0) +
                        (file.IndexOf("FullBody", StringComparison.OrdinalIgnoreCase) >= 0 ? 25 : 0);

            if (score > bestHeadScore)
            {
                bestHeadScore = score;
                bestHeadPath = p;
            }
        }
    }

    private static void ConfigureAnimator(GameObject target, string fbxPath, AnimatorController ctrl, Avatar overrideAvatar)
    {
        Animator anim = target.GetComponent<Animator>();
        if (anim == null) anim = target.AddComponent<Animator>();

        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        Avatar chosen = LoadAvatarFromFbx(fbxPath);
        if (chosen == null || !chosen.isValid || !chosen.isHuman) chosen = overrideAvatar;
        if (chosen == null) chosen = LoadAvatarFromFbx(AnimFbxPath);
        if (chosen != null) anim.avatar = chosen;

        if (anim.runtimeAnimatorController == null || anim.avatar == null || !anim.avatar.isValid || !anim.avatar.isHuman)
        {
            Debug.LogError($"[Project EAS Animator] '{target.name}' tidak valid: ctrl={anim.runtimeAnimatorController != null}, " +
                           $"avatar={anim.avatar != null}, valid={anim.avatar != null && anim.avatar.isValid}, human={anim.avatar != null && anim.avatar.isHuman}");
        }
    }

    private static Avatar LoadAvatarFromFbx(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Avatar av && av.isHuman) return av;
        }
        return null;
    }

    private static void TryLightenSkinMaterial(Material skinMat, string genderKeyword)
    {
        if (skinMat == null || !Directory.Exists(OutfitBaseTexDir)) return;

        // Cari tekstur BaseColor non-dark bila tersedia di pack
        string[] files = Directory.GetFiles(OutfitBaseTexDir, "*" + genderKeyword + "*BaseColor*.png");
        string preferred = null;
        for (int i = 0; i < files.Length; i++)
        {
            string f = files[i].Replace('\\', '/');
            if (!f.Contains("Dark", StringComparison.OrdinalIgnoreCase))
            {
                preferred = f;
                break;
            }
        }

        if (!string.IsNullOrEmpty(preferred))
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(preferred);
            if (tex != null && skinMat.HasProperty("_BaseMap"))
            {
                skinMat.SetTexture("_BaseMap", tex);
            }
        }
        if (skinMat.HasProperty("_Smoothness")) skinMat.SetFloat("_Smoothness", 0.2f);
        EditorUtility.SetDirty(skinMat);
    }

    private static void AssignMaterialsRecursive(GameObject root, Material primaryMat, Material skinMat, Material altMat)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer rend = renderers[r];
            Material[] current = rend.sharedMaterials;
            int count = Mathf.Max(1, current.Length);
            Material[] next = new Material[count];

            for (int i = 0; i < count; i++)
            {
                string oldName = i < current.Length && current[i] != null ? current[i].name : string.Empty;
                Material chosen = primaryMat;

                if (oldName.Contains("Regular_Male") && skinMat != null)
                {
                    chosen = skinMat;
                }
                else if (oldName.Contains("Regular_Female") && (altMat != null || skinMat != null))
                {
                    chosen = altMat != null ? altMat : skinMat;
                }
                else if (i > 0 && skinMat != null && primaryMat != skinMat)
                {
                    chosen = skinMat;
                }

                if (chosen != null) next[i] = chosen;
                else if (i < current.Length) next[i] = current[i];
            }

            rend.sharedMaterials = next;
        }
    }

    // =========================================================================
    // 5. Scene Wiring
    // =========================================================================
    private static GameObject SetupPlayerInScene(GameObject malePrefab, GameObject femalePrefab)
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
        }

        Vector3 homeCenter = GetHomeVillageCenter();
        Vector3 clearSpawn = FindClearGroundPosition(homeCenter + new Vector3(0f, 0f, -3.0f), 0.6f, player.transform);
        player.transform.position = clearSpawn + Vector3.up * 0.08f;
        player.transform.rotation = Quaternion.identity;

        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.transform.IsChildOf(player.transform))
        {
            mainCam.transform.SetParent(null, true);
        }

        if (player.GetComponent("PT_PlayerMovement") is Behaviour legacyMove)
        {
            legacyMove.enabled = false;
        }
        Animator legacyRootAnim = player.GetComponent<Animator>();
        if (legacyRootAnim != null)
        {
            Object.DestroyImmediate(legacyRootAnim);
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.95f, 0f);
        cc.radius = 0.38f;
        cc.height = 1.85f;
        cc.stepOffset = 0.55f;
        cc.slopeLimit = 52f;

        MeshRenderer[] oldMeshes = player.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < oldMeshes.Length; i++)
        {
            oldMeshes[i].enabled = false;
        }

        DestroyChildIfExists(player.transform, "Visual_Pyro_Ranger");
        DestroyChildIfExists(player.transform, "Visual_Hydro_Ranger");

        GameObject pyroGo = malePrefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(malePrefab, player.transform)
            : new GameObject("Visual_Pyro_Ranger");
        pyroGo.name = "Visual_Pyro_Ranger";
        pyroGo.transform.localPosition = Vector3.zero;
        pyroGo.transform.localRotation = Quaternion.identity;

        GameObject hydroGo = femalePrefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(femalePrefab, player.transform)
            : new GameObject("Visual_Hydro_Ranger");
        hydroGo.name = "Visual_Hydro_Ranger";
        hydroGo.transform.localPosition = Vector3.zero;
        hydroGo.transform.localRotation = Quaternion.identity;
        hydroGo.SetActive(false);

        ThirdPersonPlayerController ctrl = player.GetComponent<ThirdPersonPlayerController>();
        if (ctrl == null) ctrl = player.AddComponent<ThirdPersonPlayerController>();
        ctrl.pyroVisual = pyroGo;
        ctrl.hydroVisual = hydroGo;
        ctrl.activeAnimator = pyroGo.GetComponentInChildren<Animator>();

        EditorUtility.SetDirty(player);
        return player;
    }

    private static void SetupCameraInScene(GameObject playerObj)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }

        if (cam.GetComponent("PT_MouseLook") is Behaviour legacyLook)
        {
            legacyLook.enabled = false;
        }

        ThirdPersonCamera tpCam = cam.GetComponent<ThirdPersonCamera>();
        if (tpCam == null) tpCam = cam.gameObject.AddComponent<ThirdPersonCamera>();
        tpCam.target = playerObj.transform;

        EditorUtility.SetDirty(cam.gameObject);
    }

    private static int SetupHomeVillageNpcs(
        GameObject npcMalePrefab,
        GameObject npcFarmerPrefab,
        GameObject npcFemalePrefab,
        GameObject npcFemaleBunsPrefab,
        GameObject npcGuardPrefab)
    {
        GameObject existing = GameObject.Find("--- HOME_VILLAGE_NPCS ---");
        if (existing != null) Object.DestroyImmediate(existing);

        GameObject container = new GameObject("--- HOME_VILLAGE_NPCS ---");
        Vector3 anchor = GetHomeVillageCenter();

        int count = 0;
        if (SpawnHomeNpc(container.transform, npcMalePrefab, "NPC_Villager_Patrol_A", "Village Guard A", anchor + new Vector3(2.2f, 0f, -1.6f), 205f, false, 4.2f, 0, 140f, 16f) != null) count++;
        if (SpawnHomeNpc(container.transform, npcFemalePrefab, "NPC_Villager_Patrol_B", "Village Scout B", anchor + new Vector3(-2.3f, 0f, 1.8f), 145f, false, 4.2f, 0, 130f, 15f) != null) count++;
        if (SpawnHomeNpc(container.transform, npcFemaleBunsPrefab != null ? npcFemaleBunsPrefab : npcFemalePrefab,
            "NPC_Villager_Patrol_C", "Village Scout C", anchor + new Vector3(1.6f, 0f, 2.8f), 175f, false, 4.0f, 0, 130f, 15f) != null) count++;
        if (SpawnHomeNpc(container.transform, npcFarmerPrefab != null ? npcFarmerPrefab : npcMalePrefab,
            "NPC_Orchard_Farmer", "Orchard Fighter", anchor + new Vector3(-2.9f, 0f, -2.5f), 45f, true, 0f, 1, 145f, 17f) != null) count++;
        if (SpawnHomeNpc(container.transform, npcGuardPrefab != null ? npcGuardPrefab : npcFemalePrefab,
            "NPC_Gate_Ranger_Guard", "Ranger Guard", anchor + new Vector3(7.6f, 0f, 2.2f), -90f, true, 0f, 0, 185f, 22f) != null) count++;

        EditorUtility.SetDirty(container);
        Debug.Log($"[Project EAS NPCs] Spawned {count} NPCs dengan variasi gaya rambut & companion AI di Home Village plaza pusat {anchor}.");
        return count;
    }

    private static GameObject SpawnHomeNpc(
        Transform parent,
        GameObject prefab,
        string npcName,
        string displayName,
        Vector3 desiredPos,
        float yaw,
        bool stationary,
        float patrolRadius,
        int workMode,
        float companionHp,
        float companionDmg)
    {
        if (prefab == null) return null;

        Vector3 clearPos = FindClearGroundPosition(desiredPos, 0.55f);

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = npcName;
        go.transform.position = clearPos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        VillageNPCController npc = go.GetComponent<VillageNPCController>();
        if (npc != null)
        {
            npc.displayName = displayName;
            npc.stationaryWorker = stationary;
            npc.patrolRadius = patrolRadius;
            npc.workMode = workMode;
            npc.maxHp = companionHp;
            npc.attackDamage = companionDmg;
        }
        return go;
    }

    private static int SetupTerritoryMenhirs()
    {
        string[] menhirNames =
        {
            "Territory_Capture_Menhir_1",
            "Territory_Capture_Menhir_2",
            "Territory_Capture_Menhir_3"
        };
        string[] territoryLabels =
        {
            "Orchard Hold (South)",
            "Hilltop Keep (North)",
            "Eastwood Bastion (East)"
        };

        int count = 0;
        for (int i = 0; i < menhirNames.Length; i++)
        {
            GameObject menhir = GameObject.Find(menhirNames[i]);
            if (menhir == null) continue;

            TerritoryCaptureZone zone = menhir.GetComponent<TerritoryCaptureZone>();
            if (zone == null) zone = menhir.AddComponent<TerritoryCaptureZone>();
            zone.territoryName = territoryLabels[i];

            EditorUtility.SetDirty(menhir);
            count++;
        }
        return count;
    }

    private static void SetupGameSystems(
        GameObject impScoutPrefab,
        GameObject impCasterPrefab,
        GameObject puglinBrutePrefab,
        GameObject puglinElitePrefab,
        AnimatorController monsterCtrl)
    {
        GameObject sys = GameObject.Find("--- EAS_GAME_SYSTEMS ---");
        if (sys == null) sys = new GameObject("--- EAS_GAME_SYSTEMS ---");

        DDAManager dda = sys.GetComponent<DDAManager>();
        if (dda == null) dda = sys.AddComponent<DDAManager>();

        PCGWorldEcosystemManager pcg = sys.GetComponent<PCGWorldEcosystemManager>();
        if (pcg == null) pcg = sys.AddComponent<PCGWorldEcosystemManager>();

        pcg.impScoutPrefab = impScoutPrefab;
        pcg.impCasterPrefab = impCasterPrefab;
        pcg.puglinBrutePrefab = puglinBrutePrefab;
        pcg.puglinElitePrefab = puglinElitePrefab;
        pcg.monsterAnimatorController = monsterCtrl;
        pcg.healMushroomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MushroomPrefabPath);
        pcg.energyOrePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OrePrefabPath);

        if (sys.GetComponent<GameLoopManager>() == null)
        {
            sys.AddComponent<GameLoopManager>();
        }

        SetupDayNightCycleInScene(sys);
        EditorUtility.SetDirty(sys);
    }

    // =========================================================================
    // 5b. Day-Night Sky Cycle Setup (Siang = Bawaan, Pagi/Sore/Malam = SkyAssets)
    // =========================================================================
    [MenuItem("Tools/Project EAS/3. Setup Day-Night Sky Cycle (Assets/SkyAssets)")]
    public static void SetupDayNightSkyCycleMenu()
    {
        AssetDatabase.Refresh();
        GameObject sys = GameObject.Find("--- EAS_GAME_SYSTEMS ---");
        if (sys == null) sys = new GameObject("--- EAS_GAME_SYSTEMS ---");
        if (sys.GetComponent<GameLoopManager>() == null) sys.AddComponent<GameLoopManager>();

        SetupDayNightCycleInScene(sys);
        AssetDatabase.SaveAssets();
        Scene activeScene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            EditorSceneManager.SaveScene(activeScene);
        }
    }

    private static void SetupDayNightCycleInScene(GameObject sys)
    {
        if (sys == null) return;

        DayNightCycleManager cycle = sys.GetComponent<DayNightCycleManager>();
        if (cycle == null) cycle = sys.AddComponent<DayNightCycleManager>();

        // 1. Siang memakai Skybox bawaan Polytope
        Material daySky = null;
        string polytopeSkyPath = AssetDatabase.GUIDToAssetPath(DefaultPolytopeSkyGuid);
        if (!string.IsNullOrEmpty(polytopeSkyPath))
        {
            daySky = AssetDatabase.LoadAssetAtPath<Material>(polytopeSkyPath);
        }
        if (daySky == null) daySky = RenderSettings.skybox;
        cycle.daySkybox = daySky;
        if (daySky != null) RenderSettings.skybox = daySky;

        // 2. Temukan Directional Light utama
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            Light[] lights = Object.FindObjectsByType<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    sun = lights[i];
                    break;
                }
            }
        }
        cycle.sunLight = sun;

        // 3. Scan Assets/SkyAssets untuk Pagi, Sore, dan Malam (hanya memakai aset asli siap Unity)
        CleanupAutoGeneratedSkyMaterials();
        List<(string path, Material mat)> candidates = CollectSkyAssetMaterials();
        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Material morningSky = PickSkyCandidate(candidates, usedPaths,
            new[] { "pagi", "morning", "dawn", "sunrise", "subuh", "fajar", "golden_hour", "golden" },
            new[] { "malam", "night", "midnight", "twilight", "moon", "thunder", "volcanic", "space", "nebula", "supernova", "siang", "day", "noon" });

        Material eveningSky = PickSkyCandidate(candidates, usedPaths,
            new[] { "sore", "sunset", "dusk", "evening", "senja", "afternoon", "petang", "twilight", "golden" },
            new[] { "malam", "night", "midnight", "moon", "pagi", "morning", "dawn", "sunrise", "thunder", "space", "nebula", "supernova", "siang", "day", "noon" });

        Material nightSky = PickSkyCandidate(candidates, usedPaths,
            new[] { "malam", "night", "moonlit", "midnight", "moon", "star", "dark", "lunar" },
            new[] { "pagi", "morning", "sunrise", "siang", "day", "noon", "sore", "sunset", "twilight", "golden", "thunder", "space", "nebula", "supernova" });

        // Fallback urutan alfabetis dari kandidat tersisa di Assets/SkyAssets jika penamaan file tidak mengandung keyword
        if (morningSky == null) morningSky = PickFirstUnusedSky(candidates, usedPaths);
        if (eveningSky == null) eveningSky = PickFirstUnusedSky(candidates, usedPaths);
        if (nightSky == null) nightSky = PickFirstUnusedSky(candidates, usedPaths);

        cycle.morningSkybox = morningSky;
        cycle.eveningSkybox = eveningSky;
        cycle.nightSkybox = nightSky;

        EditorUtility.SetDirty(cycle);
        EditorUtility.SetDirty(sys);

        Debug.Log($"[Project EAS Sky] Day-Night Cycle siap ({candidates.Count} aset ditemukan di '{SkyAssetsDir}'):\n" +
                  $"  - Pagi  : {(morningSky != null ? AssetDatabase.GetAssetPath(morningSky) : "BELUM DITEMUKAN (fallback Siang)")}\n" +
                  $"  - Siang : {(daySky != null ? AssetDatabase.GetAssetPath(daySky) : "RenderSettings.skybox")}\n" +
                  $"  - Sore  : {(eveningSky != null ? AssetDatabase.GetAssetPath(eveningSky) : "BELUM DITEMUKAN (fallback Siang)")}\n" +
                  $"  - Malam : {(nightSky != null ? AssetDatabase.GetAssetPath(nightSky) : "BELUM DITEMUKAN (fallback Siang)")}");
    }

    private static void CleanupAutoGeneratedSkyMaterials()
    {
        if (!AssetDatabase.IsValidFolder(SkyMatOutDir)) return;
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { SkyMatOutDir });
        for (int i = 0; i < guids.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(guids[i]);
            string lower = p.Replace('\\', '/').ToLowerInvariant();
            // Hanya hapus material rusak dari folder non-Unity agar GUID material unity_panoramas / cubemaps_unity tetap stabil di scene!
            bool isBadSource = lower.Contains("cubemaps_godot") ||
                               lower.Contains("_godot_") ||
                               lower.Contains("allskiesdemo") ||
                               lower.Contains("panoramas_2048");
            if (isBadSource)
            {
                AssetDatabase.DeleteAsset(p);
            }
        }
    }

    private static bool IsUnsupportedSkyFolder(string assetPath)
    {
        string lower = assetPath.Replace('\\', '/').ToLowerInvariant();
        // Abaikan aset khusus Godot / demo / panorama non-Unity agar tidak menimbulkan garis sambungan vertikal
        return lower.Contains("/godot/") ||
               lower.Contains("/cubemaps_godot/") ||
               lower.Contains("/allskiesdemo/") ||
               lower.Contains("/panoramas_2048/");
    }

    private static List<(string path, Material mat)> CollectSkyAssetMaterials()
    {
        var results = new List<(string path, Material mat)>();
        if (!AssetDatabase.IsValidFolder(SkyAssetsDir))
        {
            Debug.LogWarning($"[Project EAS Sky] Folder '{SkyAssetsDir}' belum ditemukan di project Unity.");
            return results;
        }

        // A. Material Skybox yang sudah ada di Assets/SkyAssets (di luar folder auto-generated dulu)
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { SkyAssetsDir });
        var matDirsCovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < matGuids.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(matGuids[i]);
            if (p.StartsWith(SkyMatOutDir + "/", StringComparison.OrdinalIgnoreCase)) continue;
            if (IsUnsupportedSkyFolder(p)) continue;
            Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null || m.shader == null) continue;
            if (m.shader.name.IndexOf("Sky", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                results.Add((p, m));
                string dir = Path.GetDirectoryName(p)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir)) matDirsCovered.Add(dir);
            }
        }

        // B. Cubemap asli -> bungkus ke Skybox/Cubemap material
        string[] cubeGuids = AssetDatabase.FindAssets("t:Cubemap", new[] { SkyAssetsDir });
        for (int i = 0; i < cubeGuids.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(cubeGuids[i]);
            if (IsUnsupportedSkyFolder(p)) continue;
            Cubemap cube = AssetDatabase.LoadAssetAtPath<Cubemap>(p);
            if (cube == null) continue;
            Material m = CreateOrUpdateCubemapSkyMat(p, cube);
            if (m != null) results.Add((p, m));
        }

        // C. Texture2D asli (6-Sided Unity per folder atau Panoramic 360 Unity)
        string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { SkyAssetsDir });
        var texByDir = new Dictionary<string, List<(string path, Texture2D tex)>>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < texGuids.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(texGuids[i]);
            if (p.StartsWith(SkyMatOutDir + "/", StringComparison.OrdinalIgnoreCase)) continue;
            if (IsUnsupportedSkyFolder(p)) continue;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (tex == null) continue;

            string dir = Path.GetDirectoryName(p)?.Replace('\\', '/') ?? SkyAssetsDir;
            if (!texByDir.TryGetValue(dir, out var list))
            {
                list = new List<(string path, Texture2D tex)>();
                texByDir[dir] = list;
            }
            list.Add((p, tex));
        }

        foreach (var kv in texByDir)
        {
            string dir = kv.Key;
            if (matDirsCovered.Contains(dir)) continue;

            if (TryBuildSixSidedSkyMat(dir, kv.Value, out Material sixMat))
            {
                results.Add((dir, sixMat));
                continue;
            }

            // Jika bukan 6-sided, perlakukan tekstur panoramik 2:1 sebagai Skybox/Panoramic (tolak sisi kubus satuan!)
            for (int i = 0; i < kv.Value.Count; i++)
            {
                var entry = kv.Value[i];
                if (IsSkyBoxSideTextureName(Path.GetFileNameWithoutExtension(entry.path))) continue;
                if (entry.tex.width <= entry.tex.height) continue; // Panorama 360 harus landscape 2:1, bukan sisi kubus 1:1
                Material panoMat = CreateOrUpdatePanoramicSkyMat(entry.path, entry.tex);
                if (panoMat != null) results.Add((entry.path, panoMat));
            }
        }

        results.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    private static Texture2D ConfigureSkyTextureImporter(string assetPath, Texture2D fallbackTex, bool isPanoramic)
    {
        TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp == null) return fallbackTex;

        bool changed = false;
        if (imp.mipmapEnabled)
        {
            // Mipmap pada skybox panoramic/6-sided menimbulkan garis seam tipis di sambungan
            imp.mipmapEnabled = false;
            changed = true;
        }
        if (imp.filterMode != FilterMode.Bilinear)
        {
            imp.filterMode = FilterMode.Bilinear;
            changed = true;
        }
        int targetMaxSize = isPanoramic ? 2048 : 1024;
        if (imp.maxTextureSize < targetMaxSize)
        {
            imp.maxTextureSize = targetMaxSize;
            changed = true;
        }
        if (imp.textureCompression != TextureImporterCompression.CompressedHQ &&
            imp.textureCompression != TextureImporterCompression.Uncompressed)
        {
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            changed = true;
        }

        if (isPanoramic)
        {
            if (imp.wrapModeU != TextureWrapMode.Repeat)
            {
                imp.wrapModeU = TextureWrapMode.Repeat;
                changed = true;
            }
            if (imp.wrapModeV != TextureWrapMode.Clamp)
            {
                imp.wrapModeV = TextureWrapMode.Clamp;
                changed = true;
            }
        }
        else
        {
            if (imp.wrapMode != TextureWrapMode.Clamp)
            {
                imp.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }
        }

        if (changed)
        {
            imp.SaveAndReimport();
            Texture2D reloaded = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (reloaded != null) return reloaded;
        }
        return fallbackTex;
    }

    private static Material CreateOrUpdateCubemapSkyMat(string assetPath, Cubemap cube)
    {
        Shader shader = Shader.Find("Skybox/Cubemap");
        if (shader == null) return null;

        EnsureFolder(SkyMatOutDir);
        string safeName = BuildSafeSkyMatName(assetPath);
        string savePath = SkyMatOutDir + "/" + safeName + "_CubeSky.mat";

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(savePath);
        bool isNew = mat == null;
        if (isNew) mat = new Material(shader);
        else mat.shader = shader;

        mat.SetTexture("_Tex", cube);
        if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 1f);
        if (isNew) AssetDatabase.CreateAsset(mat, savePath);
        else EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateOrUpdatePanoramicSkyMat(string assetPath, Texture2D tex)
    {
        Shader shader = Shader.Find("Skybox/Panoramic");
        if (shader == null) return null;

        tex = ConfigureSkyTextureImporter(assetPath, tex, isPanoramic: true);

        EnsureFolder(SkyMatOutDir);
        string safeName = BuildSafeSkyMatName(assetPath);
        string savePath = SkyMatOutDir + "/" + safeName + "_PanoSky.mat";

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(savePath);
        bool isNew = mat == null;
        if (isNew) mat = new Material(shader);
        else mat.shader = shader;

        mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_Mapping")) mat.SetFloat("_Mapping", 1f);
        if (mat.HasProperty("_ImageType")) mat.SetFloat("_ImageType", 0f);
        if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 1f);
        mat.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");

        if (isNew) AssetDatabase.CreateAsset(mat, savePath);
        else EditorUtility.SetDirty(mat);
        return mat;
    }

    private static bool TryBuildSixSidedSkyMat(string dir, List<(string path, Texture2D tex)> textures, out Material mat)
    {
        mat = null;
        if (textures.Count < 6) return false;

        string frontPath = null, backPath = null, leftPath = null, rightPath = null, upPath = null, downPath = null;
        Texture2D front = null, back = null, left = null, right = null, up = null, down = null;
        for (int i = 0; i < textures.Count; i++)
        {
            string n = Path.GetFileNameWithoutExtension(textures[i].path).ToLowerInvariant();
            if (front == null && (n == "front" || n == "pz" || n.Contains("front") || n.EndsWith("_pz") || n.EndsWith("posz"))) { front = textures[i].tex; frontPath = textures[i].path; }
            else if (back == null && (n == "back" || n == "nz" || n.Contains("back") || n.EndsWith("_nz") || n.EndsWith("negz"))) { back = textures[i].tex; backPath = textures[i].path; }
            else if (left == null && (n == "left" || n == "nx" || n.Contains("left") || n.EndsWith("_nx") || n.EndsWith("negx"))) { left = textures[i].tex; leftPath = textures[i].path; }
            else if (right == null && (n == "right" || n == "px" || n.Contains("right") || n.EndsWith("_px") || n.EndsWith("posx"))) { right = textures[i].tex; rightPath = textures[i].path; }
            else if (up == null && (n == "up" || n == "top" || n == "py" || n.Contains("up") || n.Contains("top") || n.EndsWith("_py") || n.EndsWith("posy"))) { up = textures[i].tex; upPath = textures[i].path; }
            else if (down == null && (n == "down" || n == "bottom" || n == "ny" || n.Contains("down") || n.Contains("bottom") || n.EndsWith("_ny") || n.EndsWith("negy"))) { down = textures[i].tex; downPath = textures[i].path; }
        }

        if (front == null || back == null || left == null || right == null || up == null || down == null)
        {
            return false;
        }

        Shader shader = Shader.Find("Skybox/6 Sided");
        if (shader == null) return false;

        front = ConfigureSkyTextureImporter(frontPath, front, isPanoramic: false);
        back = ConfigureSkyTextureImporter(backPath, back, isPanoramic: false);
        left = ConfigureSkyTextureImporter(leftPath, left, isPanoramic: false);
        right = ConfigureSkyTextureImporter(rightPath, right, isPanoramic: false);
        up = ConfigureSkyTextureImporter(upPath, up, isPanoramic: false);
        down = ConfigureSkyTextureImporter(downPath, down, isPanoramic: false);

        EnsureFolder(SkyMatOutDir);
        string safeName = BuildSafeSkyMatName(dir);
        string savePath = SkyMatOutDir + "/" + safeName + "_6SidedSky.mat";

        mat = AssetDatabase.LoadAssetAtPath<Material>(savePath);
        bool isNew = mat == null;
        if (isNew) mat = new Material(shader);
        else mat.shader = shader;

        mat.SetTexture("_FrontTex", front);
        mat.SetTexture("_BackTex", back);
        mat.SetTexture("_LeftTex", left);
        mat.SetTexture("_RightTex", right);
        mat.SetTexture("_UpTex", up);
        mat.SetTexture("_DownTex", down);
        if (mat.HasProperty("_Exposure")) mat.SetFloat("_Exposure", 1f);

        if (isNew) AssetDatabase.CreateAsset(mat, savePath);
        else EditorUtility.SetDirty(mat);
        return true;
    }

    private static bool IsSkyBoxSideTextureName(string fileNoExt)
    {
        string n = fileNoExt.ToLowerInvariant();
        if (n == "front" || n == "back" || n == "left" || n == "right" ||
            n == "up" || n == "down" || n == "top" || n == "bottom" ||
            n == "px" || n == "nx" || n == "py" || n == "ny" || n == "pz" || n == "nz" ||
            n == "posx" || n == "negx" || n == "posy" || n == "negy" || n == "posz" || n == "negz")
        {
            return true;
        }

        return n.EndsWith("_front") || n.EndsWith("_back") || n.EndsWith("_left") ||
               n.EndsWith("_right") || n.EndsWith("_up") || n.EndsWith("_down") ||
               n.EndsWith("_top") || n.EndsWith("_bottom") ||
               n.EndsWith("_px") || n.EndsWith("_nx") || n.EndsWith("_py") ||
               n.EndsWith("_ny") || n.EndsWith("_pz") || n.EndsWith("_nz");
    }

    private static string BuildSafeSkyMatName(string assetPath)
    {
        string rel = assetPath.StartsWith(SkyAssetsDir + "/", StringComparison.OrdinalIgnoreCase)
            ? assetPath.Substring(SkyAssetsDir.Length + 1)
            : Path.GetFileNameWithoutExtension(assetPath);
        rel = Path.ChangeExtension(rel, null);
        return rel.Replace('/', '_').Replace('\\', '_').Replace(' ', '_').Trim('_');
    }

    private static string GetSkyBaseKey(string assetPath)
    {
        string normalized = assetPath.Replace('\\', '/').TrimEnd('/');
        return Path.GetFileNameWithoutExtension(normalized).ToLowerInvariant();
    }

    private static Material PickSkyCandidate(
        List<(string path, Material mat)> candidates,
        HashSet<string> usedPaths,
        string[] positiveKeywords,
        string[] negativeKeywords)
    {
        string bestPath = null;
        Material bestMat = null;
        int bestScore = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            string baseKey = GetSkyBaseKey(c.path);
            if (usedPaths.Contains(c.path) || usedPaths.Contains(baseKey)) continue;

            string lower = c.path.Replace('\\', '/').ToLowerInvariant();
            int score = 0;
            bool matchedPositive = false;
            for (int k = 0; k < positiveKeywords.Length; k++)
            {
                if (lower.Contains(positiveKeywords[k]))
                {
                    score += 20;
                    matchedPositive = true;
                }
            }
            for (int k = 0; k < negativeKeywords.Length; k++)
            {
                if (lower.Contains(negativeKeywords[k])) score -= 12;
            }

            // Prioritaskan Unity panoramic 360 resolusi tinggi (2048x1024 seamless) di atas cubemap 512x512
            if (matchedPositive)
            {
                if (lower.Contains("/unity_panoramas/")) score += 15;
                else if (lower.Contains("/cubemaps_unity/")) score += 5;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPath = c.path;
                bestMat = c.mat;
            }
        }

        if (bestMat != null)
        {
            usedPaths.Add(bestPath);
            usedPaths.Add(GetSkyBaseKey(bestPath));
        }
        return bestMat;
    }

    private static Material PickFirstUnusedSky(List<(string path, Material mat)> candidates, HashSet<string> usedPaths)
    {
        // Utamakan unity_panoramas terlebih dulu pada fallback
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                string baseKey = GetSkyBaseKey(c.path);
                if (usedPaths.Contains(c.path) || usedPaths.Contains(baseKey)) continue;

                bool isUnityPano = c.path.Replace('\\', '/').ToLowerInvariant().Contains("/unity_panoramas/");
                if (pass == 0 && !isUnityPano) continue;

                usedPaths.Add(c.path);
                usedPaths.Add(baseKey);
                return c.mat;
            }
        }
        return null;
    }

    // =========================================================================
    // Helpers
    // =========================================================================
    private static Vector3 GetHomeVillageCenter()
    {
        VillageBuildRecord record = Object.FindAnyObjectByType<VillageBuildRecord>();
        if (record != null) return record.villageCenter;
        return new Vector3(10.5f, 4f, 69.2f);
    }

    private static float SampleWorldGroundY(Vector3 worldPos)
    {
        float y = worldPos.y;
        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain t = terrains[i];
            if (t == null || t.terrainData == null) continue;
            Vector3 tp = t.transform.position;
            Vector3 sz = t.terrainData.size;
            if (worldPos.x >= tp.x && worldPos.x <= tp.x + sz.x &&
                worldPos.z >= tp.z && worldPos.z <= tp.z + sz.z)
            {
                y = t.SampleHeight(worldPos) + tp.y;
                break;
            }
        }

        // Gunakan ketinggian lantai batu plaza Home Village (hindari raycast ke pagar/prop!)
        VillageBuildRecord record = Object.FindAnyObjectByType<VillageBuildRecord>();
        if (record != null)
        {
            Vector2 pos2 = new Vector2(worldPos.x, worldPos.z);
            Vector2 home2 = new Vector2(record.villageCenter.x, record.villageCenter.z);
            if (Vector2.Distance(pos2, home2) <= 21f)
            {
                y = Mathf.Max(y, record.baseHeight + 0.05f);
            }
        }
        return y;
    }

    private static Vector3 FindClearGroundPosition(Vector3 desiredPos, float clearanceRadius = 0.55f, Transform ignoredRoot = null)
    {
        Vector3 baseSpot = new Vector3(desiredPos.x, SampleWorldGroundY(desiredPos), desiredPos.z);
        if (IsSpotClearOfObstacles(baseSpot, clearanceRadius, ignoredRoot))
        {
            return baseSpot;
        }

        float[] radii = { 1.1f, 2.0f, 3.0f, 4.0f };
        for (int r = 0; r < radii.Length; r++)
        {
            for (int a = 0; a < 8; a++)
            {
                float rad = a * 45f * Mathf.Deg2Rad;
                Vector3 cand = new Vector3(
                    desiredPos.x + Mathf.Cos(rad) * radii[r],
                    0f,
                    desiredPos.z + Mathf.Sin(rad) * radii[r]);
                cand.y = SampleWorldGroundY(cand);
                if (IsSpotClearOfObstacles(cand, clearanceRadius, ignoredRoot))
                {
                    return cand;
                }
            }
        }
        return baseSpot;
    }

    private static bool IsSpotClearOfObstacles(Vector3 groundPos, float radius, Transform ignoredRoot)
    {
        Vector3 p1 = groundPos + Vector3.up * 0.45f;
        Vector3 p2 = groundPos + Vector3.up * 1.65f;
        Collider[] hits = Physics.OverlapCapsule(p1, p2, radius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i];
            if (col == null || col is TerrainCollider) continue;
            if (ignoredRoot != null && (col.transform == ignoredRoot || col.transform.IsChildOf(ignoredRoot))) continue;

            string n = col.gameObject.name;
            if (n.Contains("Floor_") || n.Contains("Terrain")) continue;
            return false;
        }
        return true;
    }

    private static Material LoadMat(string path) => AssetDatabase.LoadAssetAtPath<Material>(path);

    private static bool FileExistsAsAsset(string assetPath) => AssetDatabase.LoadMainAssetAtPath(assetPath) != null;

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string[] parts = folderPath.Split('/');
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

    private static void DestroyChildIfExists(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
    }
}
