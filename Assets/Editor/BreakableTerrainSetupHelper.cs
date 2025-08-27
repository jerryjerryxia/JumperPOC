using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using Environment;

public class BreakableTerrainSetupHelper : EditorWindow
{
    [MenuItem("Tools/Breakable Terrain Setup Helper")]
    public static void ShowWindow()
    {
        GetWindow<BreakableTerrainSetupHelper>("Breakable Terrain Helper");
    }

    private BreakableTerrain.BreakDirection selectedBreakDirection = BreakableTerrain.BreakDirection.All;
    private bool requireMinimumVelocity = true;
    private float minimumVelocity = 2f;
    private bool requireDashState = false;
    private bool canRestore = false;
    private float restoreTime = 30f;
    private GameObject breakEffectPrefab;
    private AudioClip breakSound;
    
    private Vector2 scrollPosition;

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Breakable Terrain Setup Helper", EditorStyles.boldLabel);
        
        // Show composite collider detection info
        ShowCompositeColliderInfo();
        GUILayout.Space(10);

        // Configuration Section
        EditorGUILayout.LabelField("Default Configuration", EditorStyles.boldLabel);
        
        selectedBreakDirection = (BreakableTerrain.BreakDirection)EditorGUILayout.EnumFlagsField(
            "Break Directions", selectedBreakDirection);
        
        requireMinimumVelocity = EditorGUILayout.Toggle("Require Minimum Velocity", requireMinimumVelocity);
        if (requireMinimumVelocity)
        {
            EditorGUI.indentLevel++;
            minimumVelocity = EditorGUILayout.FloatField("Minimum Velocity", minimumVelocity);
            EditorGUI.indentLevel--;
        }
        
        requireDashState = EditorGUILayout.Toggle("Require Dash State", requireDashState);
        
        GUILayout.Space(10);
        
        canRestore = EditorGUILayout.Toggle("Can Restore", canRestore);
        if (canRestore)
        {
            EditorGUI.indentLevel++;
            restoreTime = EditorGUILayout.FloatField("Restore Time", restoreTime);
            EditorGUI.indentLevel--;
        }
        
        GUILayout.Space(10);
        
        breakEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Break Effect Prefab", breakEffectPrefab, typeof(GameObject), false);
        breakSound = (AudioClip)EditorGUILayout.ObjectField(
            "Break Sound", breakSound, typeof(AudioClip), false);

        GUILayout.Space(20);

        // Setup Actions
        EditorGUILayout.LabelField("Setup Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add Breakable Terrain to Selected Objects", GUILayout.Height(30)))
        {
            AddBreakableTerrainToSelected();
        }
        
        if (GUILayout.Button("Setup Secret Wall (Side Break)", GUILayout.Height(25)))
        {
            SetupSecretWall();
        }
        
        if (GUILayout.Button("Setup Floor Collapse (Top Break)", GUILayout.Height(25)))
        {
            SetupFloorCollapse();
        }
        
        if (GUILayout.Button("Setup Dash Wall (High Speed)", GUILayout.Height(25)))
        {
            SetupDashWall();
        }

        GUILayout.Space(20);

        // Utility Actions
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create Breakable Wall Prefab", GUILayout.Height(25)))
        {
            CreateBreakableWallPrefab();
        }
        
        if (GUILayout.Button("Find All Breakable Terrain in Scene", GUILayout.Height(25)))
        {
            FindAllBreakableTerrainInScene();
        }
        
        if (GUILayout.Button("Reset All Broken Terrain", GUILayout.Height(25)))
        {
            ResetAllBrokenTerrain();
        }

        GUILayout.Space(20);

        // Help Information
        EditorGUILayout.HelpBox(
            "BREAKABLE TERRAIN SYSTEM\n\n" +
            "• Select ANY GameObjects to add breakable terrain\n" +
            "• Works with existing colliders or creates simple triggers\n" +
            "• COMPOSITE COLLIDER: Automatically detected and handled\n" +
            "• Direction flags control which sides can be broken from\n" +
            "• Velocity requirements prevent accidental breaking\n" +
            "• State requirements allow ability-gated secrets\n\n" +
            "COMMON SETUPS:\n" +
            "• Secret Wall: Sides only, no velocity requirement\n" +
            "• Floor Collapse: Top only, moderate velocity\n" +
            "• Dash Wall: All directions, high velocity requirement\n\n" +
            "INTEGRATION:\n" +
            "• Works with ANY collider type (Box, Composite, etc.)\n" +
            "• Automatically removes tilemap tiles when applicable\n" +
            "• Handles composite collider regeneration\n" +
            "• Cleans up landing buffers\n" +
            "• Simple and automatic setup",
            MessageType.Info
        );

        EditorGUILayout.EndScrollView();
    }

    private void ShowCompositeColliderInfo()
    {
        // Show selected objects info
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length > 0)
        {
            int withColliders = 0;
            int withCompositeColliders = 0;
            
            foreach (GameObject obj in selected)
            {
                if (obj.GetComponent<Collider2D>() != null)
                    withColliders++;
                if (obj.GetComponent<CompositeCollider2D>() != null)
                    withCompositeColliders++;
            }
            
            string info = $"SELECTED: {selected.Length} objects\n";
            if (withColliders > 0) info += $"• {withColliders} with colliders\n";
            if (withCompositeColliders > 0) info += $"• {withCompositeColliders} with composite colliders\n";
            info += "The system will work with existing colliders or create simple triggers as needed.";
            
            EditorGUILayout.HelpBox(info, MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Select GameObjects to add breakable terrain to. The system will automatically handle collider setup.",
                MessageType.None
            );
        }
    }
    
    private void AddBreakableTerrainToSelected()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select GameObjects to add breakable terrain to.", "OK");
            return;
        }

        int addedCount = 0;
        
        foreach (GameObject obj in selectedObjects)
        {
            // Add BreakableTerrain component if not already present
            BreakableTerrain breakableTerrain = obj.GetComponent<BreakableTerrain>();
            if (breakableTerrain == null)
            {
                breakableTerrain = obj.AddComponent<BreakableTerrain>();
                addedCount++;
                Debug.Log($"Added BreakableTerrain to {obj.name}");
            }
            else
            {
                Debug.Log($"Updated existing BreakableTerrain on {obj.name}");
            }

            // Configure with current settings
            ConfigureBreakableTerrain(breakableTerrain);
        }

        string message = $"Configured BreakableTerrain on {addedCount} objects (updated {selectedObjects.Length - addedCount} existing)";
        
        Debug.Log(message);
        EditorUtility.DisplayDialog("Setup Complete", message, "OK");
    }

    private void ConfigureBreakableTerrain(BreakableTerrain terrain)
    {
        SerializedObject so = new SerializedObject(terrain);
        
        so.FindProperty("allowedBreakDirections").intValue = (int)selectedBreakDirection;
        so.FindProperty("requireMinimumVelocity").boolValue = requireMinimumVelocity;
        so.FindProperty("minimumVelocity").floatValue = minimumVelocity;
        so.FindProperty("requireDashState").boolValue = requireDashState;
        so.FindProperty("canRestore").boolValue = canRestore;
        so.FindProperty("restoreTime").floatValue = restoreTime;
        
        if (breakEffectPrefab != null)
            so.FindProperty("breakEffectPrefab").objectReferenceValue = breakEffectPrefab;
        if (breakSound != null)
            so.FindProperty("breakSound").objectReferenceValue = breakSound;
        
        so.ApplyModifiedProperties();
    }

    private void SetupSecretWall()
    {
        selectedBreakDirection = BreakableTerrain.BreakDirection.Sides;
        requireMinimumVelocity = false;
        requireDashState = false;
        canRestore = false;
        
        Debug.Log("Configured for Secret Wall (touch from sides to break)");
    }

    private void SetupFloorCollapse()
    {
        selectedBreakDirection = BreakableTerrain.BreakDirection.Top;
        requireMinimumVelocity = true;
        minimumVelocity = 5f;
        requireDashState = false;
        canRestore = false;
        
        Debug.Log("Configured for Floor Collapse (land on top to break)");
    }

    private void SetupDashWall()
    {
        selectedBreakDirection = BreakableTerrain.BreakDirection.All;
        requireMinimumVelocity = true;
        minimumVelocity = 15f; // Assuming dash speed
        requireDashState = true;
        canRestore = false;
        
        Debug.Log("Configured for Dash Wall (dash into to break)");
    }

    // Removed unnecessary complexity - let the component handle detection
    
    private void CreateBreakableWallPrefab()
    {
        // Create a simple breakable wall prefab
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "BreakableWall";
        
        // Configure for 2D
        wall.transform.localScale = new Vector3(1, 1, 0.1f);
        
        // Remove 3D collider and add 2D collider
        Destroy(wall.GetComponent<Collider>());
        BoxCollider2D collider2D = wall.AddComponent<BoxCollider2D>();
        collider2D.isTrigger = true; // Make it a trigger for breakable terrain
        
        // Add BreakableTerrain component
        BreakableTerrain breakableTerrain = wall.AddComponent<BreakableTerrain>();
        ConfigureBreakableTerrain(breakableTerrain);
        
        // Set to appropriate layer
        int wallLayer = LayerMask.NameToLayer("Ground");
        if (wallLayer != -1)
        {
            wall.layer = wallLayer;
        }
        
        // Create prefab
        string prefabPath = "Assets/Prefabs/Environment/BreakableWall.prefab";
        
        // Ensure directory exists
        string directory = System.IO.Path.GetDirectoryName(prefabPath);
        if (!AssetDatabase.IsValidFolder(directory))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Environment");
        }
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wall, prefabPath);
        
        // Clean up scene object
        DestroyImmediate(wall);
        
        // Select the prefab
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        Debug.Log($"Created BreakableWall prefab at {prefabPath}");
    }

    private void FindAllBreakableTerrainInScene()
    {
        BreakableTerrain[] allBreakable = FindObjectsOfType<BreakableTerrain>();
        
        if (allBreakable.Length == 0)
        {
            EditorUtility.DisplayDialog("Search Results", "No BreakableTerrain components found in scene.", "OK");
            return;
        }
        
        // Select all found objects
        GameObject[] gameObjects = new GameObject[allBreakable.Length];
        for (int i = 0; i < allBreakable.Length; i++)
        {
            gameObjects[i] = allBreakable[i].gameObject;
        }
        
        Selection.objects = gameObjects;
        
        Debug.Log($"Found {allBreakable.Length} BreakableTerrain components in scene");
        EditorUtility.DisplayDialog("Search Results", $"Found and selected {allBreakable.Length} BreakableTerrain objects", "OK");
    }

    private void ResetAllBrokenTerrain()
    {
        BreakableTerrain[] allBreakable = FindObjectsOfType<BreakableTerrain>();
        int resetCount = 0;
        
        foreach (BreakableTerrain terrain in allBreakable)
        {
            if (terrain.IsBroken)
            {
                terrain.ResetState();
                resetCount++;
            }
        }
        
        if (resetCount == 0)
        {
            EditorUtility.DisplayDialog("Reset Complete", "No broken terrain found to reset.", "OK");
        }
        else
        {
            Debug.Log($"Reset {resetCount} broken terrain objects");
            EditorUtility.DisplayDialog("Reset Complete", $"Reset {resetCount} broken terrain objects", "OK");
        }
    }
}

// Custom property drawer for better inspector display
[CustomEditor(typeof(BreakableTerrain))]
public class BreakableTerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BreakableTerrain terrain = (BreakableTerrain)target;
        
        // Show current state
        EditorGUILayout.Space();
        GUI.enabled = false;
        EditorGUILayout.Toggle("Is Broken", terrain.IsBroken);
        EditorGUILayout.Toggle("Has Been Broken", terrain.HasBeenBroken);
        EditorGUILayout.Toggle("Composite Collider Setup", terrain.IsCompositeColliderSetup);
        if (terrain.IsCompositeColliderSetup)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.ObjectField("Composite Collider", terrain.GetCompositeCollider(), typeof(CompositeCollider2D), true);
            EditorGUILayout.ObjectField("Trigger Collider", terrain.GetTriggerCollider(), typeof(Collider2D), true);
            EditorGUI.indentLevel--;
        }
        GUI.enabled = true;
        EditorGUILayout.Space();
        
        // Show default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        // Component handles all setup automatically
        
        // Runtime controls
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Break"))
            {
                terrain.ForceBreak();
            }
            if (GUILayout.Button("Force Restore"))
            {
                terrain.ForceRestore();
            }
            GUILayout.EndHorizontal();
        }
        
        // Simplified - component handles setup automatically
    }
    
    // Removed - component handles all detection automatically
}