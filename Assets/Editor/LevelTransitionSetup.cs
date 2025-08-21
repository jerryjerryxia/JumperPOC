using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Simple editor tool for setting up level transitions between scenes.
/// Creates transition triggers and spawn points with minimal configuration.
/// </summary>
public class LevelTransitionSetup : EditorWindow
{
    [SerializeField] private string targetSceneName = "Level2_CommercialArea";
    [SerializeField] private Vector3 spawnPosition = new Vector3(-8.5f, 0f, 0f);
    [SerializeField] private string spawnPointId = "LevelEntry";
    [SerializeField] private Vector2 triggerSize = new Vector2(1f, 4f);
    
    private bool showHelp = false;
    
    [MenuItem("Tools/Level Transition Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<LevelTransitionSetup>("Level Transition Setup");
        window.minSize = new Vector2(400, 350);
    }
    
    void OnGUI()
    {
        GUILayout.Label("Level Transition Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Help section
        showHelp = EditorGUILayout.Foldout(showHelp, "Help & Instructions");
        if (showHelp)
        {
            EditorGUILayout.HelpBox(
                "This tool creates seamless level transitions:\n\n" +
                "1. Set target scene and spawn position\n" +
                "2. Click 'Create Transition Zone' to place trigger\n" +
                "3. Position the trigger at level edge in Scene View\n" +
                "4. Go to target scene and click 'Create Spawn Point'\n" +
                "5. Test in Play Mode!", 
                MessageType.Info);
        }
        
        GUILayout.Space(10);
        
        // Configuration section
        GUILayout.Label("Transition Configuration", EditorStyles.boldLabel);
        
        targetSceneName = EditorGUILayout.TextField("Target Scene Name", targetSceneName);
        spawnPosition = EditorGUILayout.Vector3Field("Spawn Position", spawnPosition);
        spawnPointId = EditorGUILayout.TextField("Spawn Point ID", spawnPointId);
        triggerSize = EditorGUILayout.Vector2Field("Trigger Size", triggerSize);
        
        GUILayout.Space(15);
        
        // Action buttons
        GUILayout.Label("Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create Transition Zone", GUILayout.Height(30)))
        {
            CreateTransitionZone();
        }
        
        if (GUILayout.Button("Create Spawn Point", GUILayout.Height(30)))
        {
            CreateSpawnPoint();
        }
        
        GUILayout.Space(10);
        
        // Utility buttons
        if (GUILayout.Button("Add Target Scene to Build Settings"))
        {
            AddSceneToBuildSettings();
        }
        
        if (GUILayout.Button("Validate All Transitions"))
        {
            ValidateTransitions();
        }
        
        GUILayout.Space(15);
        
        // Status
        EditorGUILayout.LabelField("Status: Ready to create transitions", EditorStyles.miniLabel);
    }
    
    private void CreateTransitionZone()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            EditorUtility.DisplayDialog("Error", "Please specify a target scene name.", "OK");
            return;
        }
        
        // Check if a transition zone with this name already exists
        string objectName = $"TransitionTo_{targetSceneName}";
        GameObject existingZone = GameObject.Find(objectName);
        if (existingZone != null)
        {
            EditorUtility.DisplayDialog("Warning", $"A transition zone to '{targetSceneName}' already exists. Select it to modify or delete it first.", "OK");
            Selection.activeGameObject = existingZone;
            return;
        }
        
        // Create transition zone GameObject
        GameObject transitionZone = new GameObject(objectName);
        
        // Add BoxCollider2D as trigger FIRST (required component)
        var collider = transitionZone.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = triggerSize;
        
        // Now add LevelTransition component (after collider exists)
        var transition = transitionZone.AddComponent<LevelTransition>();
        transition.SetTransitionData(targetSceneName, spawnPointId);
        
        // Position at scene view camera focus or default
        if (SceneView.lastActiveSceneView != null)
        {
            transitionZone.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
            transitionZone.transform.position = new Vector3(
                transitionZone.transform.position.x,
                transitionZone.transform.position.y,
                0f
            );
        }
        
        // Select the created object for easy positioning
        Selection.activeGameObject = transitionZone;
        
        // Mark scene dirty
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        Debug.Log($"Created transition zone to '{targetSceneName}' with spawn point '{spawnPointId}'");
    }
    
    private void CreateSpawnPoint()
    {
        if (string.IsNullOrEmpty(spawnPointId))
        {
            EditorUtility.DisplayDialog("Error", "Please specify a spawn point ID.", "OK");
            return;
        }
        
        // Check if a spawn point with this ID already exists
        string objectName = $"SpawnPoint_{spawnPointId}";
        GameObject existingSpawn = GameObject.Find(objectName);
        if (existingSpawn != null)
        {
            EditorUtility.DisplayDialog("Warning", $"A spawn point with ID '{spawnPointId}' already exists. Select it to modify or delete it first.", "OK");
            Selection.activeGameObject = existingSpawn;
            return;
        }
        
        // Create spawn point GameObject
        GameObject spawnPoint = new GameObject(objectName);
        
        // Add LevelSpawnPoint component
        var spawn = spawnPoint.AddComponent<LevelSpawnPoint>();
        spawn.SetSpawnData(spawnPointId, spawnPosition);
        
        // Position at specified spawn position
        spawnPoint.transform.position = spawnPosition;
        
        // Select the created object
        Selection.activeGameObject = spawnPoint;
        
        // Mark scene dirty
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        
        Debug.Log($"Created spawn point '{spawnPointId}' at position {spawnPosition}");
    }
    
    private void AddSceneToBuildSettings()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            EditorUtility.DisplayDialog("Error", "Please specify a target scene name.", "OK");
            return;
        }
        
        // Find scene asset
        string scenePath = FindScenePath(targetSceneName);
        if (string.IsNullOrEmpty(scenePath))
        {
            EditorUtility.DisplayDialog("Error", $"Scene '{targetSceneName}' not found in project.", "OK");
            return;
        }
        
        // Check if already in build settings
        var buildScenes = EditorBuildSettings.scenes;
        foreach (var scene in buildScenes)
        {
            if (scene.path == scenePath)
            {
                EditorUtility.DisplayDialog("Info", $"Scene '{targetSceneName}' is already in build settings.", "OK");
                return;
            }
        }
        
        // Add to build settings
        var newBuildScenes = new EditorBuildSettingsScene[buildScenes.Length + 1];
        for (int i = 0; i < buildScenes.Length; i++)
        {
            newBuildScenes[i] = buildScenes[i];
        }
        newBuildScenes[buildScenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        
        EditorBuildSettings.scenes = newBuildScenes;
        
        Debug.Log($"Added '{targetSceneName}' to build settings");
        EditorUtility.DisplayDialog("Success", $"Added '{targetSceneName}' to build settings.", "OK");
    }
    
    private void ValidateTransitions()
    {
        var transitions = FindObjectsByType<LevelTransition>(FindObjectsSortMode.None);
        var spawnPoints = FindObjectsByType<LevelSpawnPoint>(FindObjectsSortMode.None);
        
        Debug.Log($"Found {transitions.Length} transition zones and {spawnPoints.Length} spawn points in current scene");
        
        foreach (var transition in transitions)
        {
            Debug.Log($"Transition: {transition.name} -> Scene: {transition.GetTargetScene()}, Spawn: {transition.GetSpawnPointId()}");
        }
        
        foreach (var spawn in spawnPoints)
        {
            Debug.Log($"Spawn Point: {spawn.name} -> ID: {spawn.GetSpawnPointId()}, Position: {spawn.transform.position}");
        }
        
        EditorUtility.DisplayDialog("Validation Complete", 
            $"Found {transitions.Length} transitions and {spawnPoints.Length} spawn points.\nCheck Console for details.", "OK");
    }
    
    private string FindScenePath(string sceneName)
    {
        string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains(sceneName + ".unity"))
            {
                return path;
            }
        }
        return null;
    }
}