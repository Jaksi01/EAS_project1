using UnityEditor;
using UnityEngine;

public static class EASAnimatorDiagnostics
{
    [MenuItem("Tools/Project EAS/Diagnose Animators")]
    public static void Run()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Characters", "Assets/Prefabs/Monsters" });
        foreach (string g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            foreach (var a in go.GetComponentsInChildren<Animator>(true))
            {
                string av = a.avatar == null ? "NULL" : $"{a.avatar.name} valid={a.avatar.isValid} human={a.avatar.isHuman}";
                string c = a.runtimeAnimatorController == null ? "NULL" : a.runtimeAnimatorController.name;
                Debug.Log($"[EAS Diag] {p} -> Animator on '{a.name}' enabled={a.enabled} controller={c} avatar={av}");
            }
        }

        foreach (string f in new[] {
            "Assets/Animation/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx",
            "Assets/Animation/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx" })
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(f))
                if (o is Avatar a) Debug.Log($"[EAS Diag] {f} avatar '{a.name}' valid={a.isValid} human={a.isHuman}");
        }
    }
}
