using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// Automatically adds all scenes in Assets/Scenes/ to the build settings.
/// Unity 6 compatible - works with Build Profiles.
/// </summary>
public class AddScenesToBuild : Editor
{
    [MenuItem("Tools/Add All Scenes to Build")]
    public static void AddAllScenesToBuild()
    {
        // Find all scene files in Assets/Scenes/
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

        if (guids.Length == 0)
        {
            Debug.LogWarning("[AddScenesToBuild] No scenes found in Assets/Scenes/");
            return;
        }

        // Convert GUIDs to scene paths
        var scenePaths = guids
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .OrderBy(path => path)
            .ToArray();

        Debug.Log($"[AddScenesToBuild] Found {scenePaths.Length} scenes:");
        foreach (var path in scenePaths)
        {
            Debug.Log($"  - {path}");
        }

        // Create EditorBuildSettingsScene array
        var scenes = scenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();

        // Set the scenes in build settings
        EditorBuildSettings.scenes = scenes;

        Debug.Log($"[AddScenesToBuild] ✓ Successfully added {scenes.Length} scenes to build settings:");
        for (int i = 0; i < scenes.Length; i++)
        {
            Debug.Log($"  [{i}] {scenes[i].path}");
        }
    }
}
