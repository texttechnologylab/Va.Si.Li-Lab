using UnityEditor;
using UnityEngine;
using VaSiLi.Networking;

/// <summary>
/// Warns on editor start when no VasiliEndpoints config exists and offers a menu item to create one.
/// The config holds the project's server addresses, so it is not part of the public repository.
/// </summary>
[InitializeOnLoad]
public static class EndpointsSetup
{
    private const string DefaultPath = "Assets/Resources/" + VasiliEndpoints.ResourceName + ".asset";

    static EndpointsSetup()
    {
        // Wait until the asset database is ready
        EditorApplication.delayCall += () =>
        {
            if (Find() == null)
                Debug.LogWarning("[Va.Si.Li] No endpoints configured: all services will use localhost. " +
                                 "Create a config via Tools > Va.Si.Li > Create Endpoints Config and enter your server addresses.");
        };
    }

    [MenuItem("Tools/Va.Si.Li/Create Endpoints Config")]
    private static void Create()
    {
        var existing = Find();
        if (existing == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            existing = ScriptableObject.CreateInstance<VasiliEndpoints>();
            AssetDatabase.CreateAsset(existing, DefaultPath);
            AssetDatabase.SaveAssets();
        }
        Selection.activeObject = existing;
        EditorGUIUtility.PingObject(existing);
    }

    // Only assets inside a Resources folder are picked up at runtime
    private static VasiliEndpoints Find()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(VasiliEndpoints)))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/Resources/") && System.IO.Path.GetFileNameWithoutExtension(path) == VasiliEndpoints.ResourceName)
                return AssetDatabase.LoadAssetAtPath<VasiliEndpoints>(path);
        }
        return null;
    }
}
