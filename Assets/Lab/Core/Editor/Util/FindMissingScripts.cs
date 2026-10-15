using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Finds (and optionally removes) components whose script no longer exists,
/// across every prefab and scene in the Lab and LabPrivate folders.
/// </summary>
public static class FindMissingScripts
{
    // Only our own content; Samples and ThirdParty are left untouched
    private static readonly string[] Folders = { "Assets/Lab", "Assets/LabPrivate" };

    [MenuItem("Tools/Va.Si.Li/Missing Scripts/Report")]
    private static void Report() => Run(remove: false);

    [MenuItem("Tools/Va.Si.Li/Missing Scripts/Remove")]
    private static void Remove()
    {
        if (!EditorUtility.DisplayDialog("Remove missing scripts",
                "Removes every missing script component from all prefabs and scenes in Assets/Lab and Assets/LabPrivate and saves them.",
                "Remove", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        Run(remove: true);
    }

    private static void Run(bool remove)
    {
        int total = AssetBatch.Run(Folders, remove, (go, assetPath) =>
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count == 0)
                return 0;

            Debug.Log($"[MissingScripts] {count} on '{AssetBatch.GetPath(go)}' in {assetPath}");

            // Components that come from a nested prefab can only be removed in that prefab itself
            if (remove && !PrefabUtility.IsPartOfPrefabInstance(go))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            return count;
        });

        Debug.Log($"[MissingScripts] {(remove ? "Removed" : "Found")} {total} missing script(s).");
    }
}
