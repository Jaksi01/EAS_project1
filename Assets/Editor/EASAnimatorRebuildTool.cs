using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Membangun ulang Player_Animator & Monster_Animator memakai clip UAL1 + UAL2
/// (walk/jog/sprint biasa, combo pedang 3 hit, roll, spell, hit, zombie untuk monster).
/// Controller ditulis ulang di tempat (GUID tetap), sehingga prefab tidak kehilangan referensi.
/// </summary>
public static class EASAnimatorRebuildTool
{
    private const string Ual1 = "Assets/Animation/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx";
    private const string Ual2 = "Assets/Animation/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx";
    private const string Dir = "Assets/Animations/Controllers";

    [MenuItem("Tools/Project EAS/Rebuild Animators (UAL1 + UAL2 Combo)")]
    public static void Rebuild()
    {
        AssetDatabase.Refresh();
        SetLoops(Ual1);
        SetLoops(Ual2);

        var clips = new Dictionary<string, AnimationClip>();
        Load(Ual2, clips);
        Load(Ual1, clips); // UAL1 menimpa nama yang sama

        if (!clips.ContainsKey("Walk_Loop"))
        {
            EditorUtility.DisplayDialog("UAL1 belum ter-import",
                "Clip UAL1 (Walk_Loop dll) tidak ditemukan. Tunggu import Unity selesai lalu jalankan lagi.", "OK");
            return;
        }

        BuildPlayer(clips);
        BuildMonster(clips);
        AssetDatabase.SaveAssets();
        Debug.Log("[EAS] Player_Animator & Monster_Animator dibangun ulang (combo 3 hit).");
    }

    private static void SetLoops(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as ModelImporter;
        if (imp == null) return;
        var arr = imp.clipAnimations;
        if (arr == null || arr.Length == 0) arr = imp.defaultClipAnimations;
        bool dirty = false;
        for (int i = 0; i < arr.Length; i++)
        {
            string n = arr[i].name;
            bool loop = n.Contains("Loop");
            if (arr[i].loopTime != loop)
            {
                arr[i].loopTime = loop;
                dirty = true;
            }
            if (loop)
            {
                arr[i].lockRootRotation = true;
                arr[i].lockRootHeightY = true;
                arr[i].lockRootPositionXZ = true;
            }
        }
        if (dirty)
        {
            imp.clipAnimations = arr;
            imp.SaveAndReimport();
        }
    }

    private static void Load(string path, Dictionary<string, AnimationClip> dict)
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (o is AnimationClip c && !c.name.StartsWith("__preview__"))
                dict[c.name.Replace("Armature|", "")] = c;
        }
    }

    private static AnimationClip C(Dictionary<string, AnimationClip> d, string name)
    {
        if (d.TryGetValue(name, out var c)) return c;
        Debug.LogWarning("[EAS] Clip tidak ditemukan: " + name);
        return null;
    }

    private static AnimatorController Reset(string file, params (string, AnimatorControllerParameterType)[] ps)
    {
        string path = Dir + "/" + file;
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path)
                   ?? AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = ctrl.layers[0].stateMachine;
        foreach (var s in sm.states) sm.RemoveState(s.state);
        foreach (var t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);
        for (int i = ctrl.parameters.Length - 1; i >= 0; i--) ctrl.RemoveParameter(i);
        foreach (var p in ps) ctrl.AddParameter(p.Item1, p.Item2);
        return ctrl;
    }

    private static AnimatorState Action(AnimatorStateMachine sm, AnimatorState back, string name,
        AnimationClip clip, string trigger, float exit, float blend, params (string, int)[] ints)
    {
        var st = sm.AddState(name);
        st.motion = clip;
        var any = sm.AddAnyStateTransition(st);
        any.hasExitTime = false;
        any.duration = blend;
        any.canTransitionToSelf = false;
        any.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        foreach (var i in ints) any.AddCondition(AnimatorConditionMode.Equals, i.Item2, i.Item1);
        var ret = st.AddTransition(back);
        ret.hasExitTime = true;
        ret.exitTime = exit;
        ret.duration = 0.12f;
        return st;
    }

    private static void BuildPlayer(Dictionary<string, AnimationClip> c)
    {
        var ctrl = Reset("Player_Animator.controller",
            ("Speed", AnimatorControllerParameterType.Float),
            ("IsGrounded", AnimatorControllerParameterType.Bool),
            ("ComboStep", AnimatorControllerParameterType.Int),
            ("Jump", AnimatorControllerParameterType.Trigger),
            ("Dash", AnimatorControllerParameterType.Trigger),
            ("Attack", AnimatorControllerParameterType.Trigger),
            ("Skill", AnimatorControllerParameterType.Trigger),
            ("Burst", AnimatorControllerParameterType.Trigger),
            ("Hit", AnimatorControllerParameterType.Trigger));
        var sm = ctrl.layers[0].stateMachine;

        var idle = sm.AddState("Idle");
        idle.motion = C(c, "Idle_Loop");
        sm.defaultState = idle;

        // Locomotion: walk / jog / sprint berdasarkan Speed
        var move = sm.AddState("Move");
        var tree = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
        tree.AddChild(C(c, "Walk_Loop"), 0.3f);
        tree.AddChild(C(c, "Jog_Fwd_Loop"), 1.0f);
        tree.AddChild(C(c, "Sprint_Loop"), 1.5f);
        AssetDatabase.AddObjectToAsset(tree, ctrl);
        move.motion = tree;

        var toMove = idle.AddTransition(move);
        toMove.hasExitTime = false; toMove.duration = 0.12f;
        toMove.AddCondition(AnimatorConditionMode.Greater, 0.25f, "Speed");
        var toIdle = move.AddTransition(idle);
        toIdle.hasExitTime = false; toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");

        // Combo pedang 3 hit
        Action(sm, idle, "Attack1", C(c, "Sword_Regular_A"), "Attack", 0.85f, 0.06f, ("ComboStep", 0));
        Action(sm, idle, "Attack2", C(c, "Sword_Regular_B"), "Attack", 0.85f, 0.06f, ("ComboStep", 1));
        Action(sm, idle, "Attack3", C(c, "Sword_Regular_C"), "Attack", 0.90f, 0.06f, ("ComboStep", 2));

        Action(sm, idle, "Skill", C(c, "Spell_Simple_Shoot"), "Skill", 0.85f, 0.06f);
        Action(sm, idle, "Burst", C(c, "Sword_Heavy_Combo"), "Burst", 0.88f, 0.08f);
        Action(sm, idle, "Dash", C(c, "Roll"), "Dash", 0.85f, 0.05f);
        Action(sm, idle, "Jump", C(c, "Jump_Start"), "Jump", 0.85f, 0.06f);
        Action(sm, idle, "Hit", C(c, "Hit_Chest"), "Hit", 0.80f, 0.05f);
        EditorUtility.SetDirty(ctrl);
    }

    private static void BuildMonster(Dictionary<string, AnimationClip> c)
    {
        var ctrl = Reset("Monster_Animator.controller",
            ("Speed", AnimatorControllerParameterType.Float),
            ("Attack", AnimatorControllerParameterType.Trigger),
            ("Hit", AnimatorControllerParameterType.Trigger));
        var sm = ctrl.layers[0].stateMachine;

        var idle = sm.AddState("Idle");
        idle.motion = C(c, "Zombie_Idle_Loop");
        sm.defaultState = idle;
        var chase = sm.AddState("Chase");
        chase.motion = C(c, "Zombie_Walk_Fwd_Loop");

        var a = idle.AddTransition(chase);
        a.hasExitTime = false; a.duration = 0.12f;
        a.AddCondition(AnimatorConditionMode.Greater, 0.25f, "Speed");
        var b = chase.AddTransition(idle);
        b.hasExitTime = false; b.duration = 0.15f;
        b.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");

        Action(sm, idle, "Attack", C(c, "Zombie_Scratch"), "Attack", 0.85f, 0.08f);
        Action(sm, idle, "Hit", C(c, "Hit_Knockback"), "Hit", 0.82f, 0.05f);
        EditorUtility.SetDirty(ctrl);
    }
}
