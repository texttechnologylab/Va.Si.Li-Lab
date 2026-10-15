using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Runs an operation on every GameObject in all prefabs and scenes under the given folders.
/// The operation returns how many issues it found/fixed on that object; assets are only saved when save is true.
/// </summary>
public static class AssetBatch
{
    public static int Run(string[] folders, bool save, Func<GameObject, string, int> process)
    {
        int total = 0;

        // Prefabs first, so scene instances of fixed prefabs are already clean
        foreach (string path in Find("t:Prefab", folders))
        {
            // Model files (.fbx etc.) show up as prefabs too but can't be edited
            if (PrefabUtility.GetPrefabAssetType(AssetDatabase.LoadMainAssetAtPath(path)) == PrefabAssetType.Model)
                continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int count = Walk(root, path, process);
            if (save && count > 0)
                PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            total += count;
        }

        string activeScene = EditorSceneManager.GetActiveScene().path;
        foreach (string path in Find("t:Scene", folders))
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int count = scene.GetRootGameObjects().Sum(go => Walk(go, path, process));
            if (save && count > 0)
                EditorSceneManager.SaveScene(scene);
            total += count;
        }
        if (!string.IsNullOrEmpty(activeScene))
            EditorSceneManager.OpenScene(activeScene);

        return total;
    }

    public static string GetPath(GameObject go) =>
        go.transform.parent ? GetPath(go.transform.parent.gameObject) + "/" + go.name : go.name;

    private static int Walk(GameObject go, string assetPath, Func<GameObject, string, int> process)
    {
        int count = process(go, assetPath);
        foreach (Transform child in go.transform)
            count += Walk(child.gameObject, assetPath, process);
        return count;
    }

    private static string[] Find(string filter, string[] folders) =>
        AssetDatabase.FindAssets(filter, folders).Select(AssetDatabase.GUIDToAssetPath).ToArray();
}
