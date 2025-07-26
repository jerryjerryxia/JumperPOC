using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class InputManagerFix : EditorWindow
{
    [MenuItem("Tools/Fix Input Issues")]
    public static void FixInputIssues()
    {
        // Check if InputManager already exists
        InputManager existingInputManager = GameObject.FindFirstObjectByType<InputManager>();
        if (existingInputManager != null)
        {
            EditorUtility.DisplayDialog("Input Manager Found", 
                $"InputManager already exists on GameObject '{existingInputManager.gameObject.name}'", "OK");
            return;
        }
        
        // Create InputManager GameObject
        GameObject inputManagerGO = new GameObject("InputManager");
        InputManager inputManager = inputManagerGO.AddComponent<InputManager>();
        
        // Enable input debugging by default to help verify it's working
        var serializedObject = new SerializedObject(inputManager);
        var enableInputDebugging = serializedObject.FindProperty("enableInputDebugging");
        if (enableInputDebugging != null)
        {
            enableInputDebugging.boolValue = true;
            serializedObject.ApplyModifiedProperties();
        }
        
        // Mark scene as dirty
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        
        // Select the created GameObject
        Selection.activeGameObject = inputManagerGO;
        
        Debug.Log("[InputManagerFix] Created InputManager GameObject in current scene. Input debugging enabled.");
        EditorUtility.DisplayDialog("Input Fixed!", 
            "InputManager has been added to the scene. Player input should now work.\n\nInput debugging is enabled - you'll see debug info in the top-right of the Game view.", "OK");
    }
    
    [MenuItem("Tools/Check Input System Status")]
    public static void CheckInputSystemStatus()
    {
        bool hasInputManager = GameObject.FindFirstObjectByType<InputManager>() != null;
        bool hasPlayerController = GameObject.FindFirstObjectByType<PlayerController>() != null;
        bool hasControlsFile = System.IO.File.Exists(Application.dataPath + "/Input/Controls.inputactions");
        bool hasControlsScript = System.IO.File.Exists(Application.dataPath + "/Input/Controls.cs");
        
        string status = "Input System Status:\n\n";
        status += $"✓ InputManager in scene: {(hasInputManager ? "YES" : "NO")}\n";
        status += $"✓ PlayerController in scene: {(hasPlayerController ? "YES" : "NO")}\n";
        status += $"✓ Controls.inputactions file: {(hasControlsFile ? "YES" : "NO")}\n";
        status += $"✓ Controls.cs script: {(hasControlsScript ? "YES" : "NO")}\n\n";
        
        if (!hasInputManager)
        {
            status += "⚠ Missing InputManager! Use 'Tools > Fix Input Issues' to add one.\n";
        }
        
        if (hasInputManager && hasPlayerController && hasControlsFile && hasControlsScript)
        {
            status += "✅ All input components are present. Input should work correctly.";
        }
        else
        {
            status += "❌ Missing components detected. Input may not work properly.";
        }
        
        EditorUtility.DisplayDialog("Input System Status", status, "OK");
    }
}