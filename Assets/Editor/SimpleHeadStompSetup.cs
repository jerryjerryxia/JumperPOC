using UnityEngine;
using UnityEditor;
using Enemies;

/// <summary>
/// Simple, clean automation tool for head stomp setup.
/// Adds SimpleHeadStomp to enemies and removes old complex components.
/// </summary>
public class SimpleHeadStompSetup : EditorWindow
{
    [MenuItem("Tools/Simple Head Stomp Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<SimpleHeadStompSetup>();
        window.titleContent = new GUIContent("Simple Head Stomp");
        window.minSize = new Vector2(350, 300);
        window.Show();
    }
    
    // Configuration
    private float bounceForce = 18f;
    private float minimumFallSpeed = -2f;
    private Vector2 triggerPosition = new Vector2(0f, 0.6f);
    private Vector2 triggerSize = new Vector2(0.2f, 0.01f);
    private bool removeOldComponents = true;
    
    void OnGUI()
    {
        GUILayout.Label("Simple Head Stomp Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Configuration section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Head Stomp Configuration", EditorStyles.boldLabel);
        bounceForce = EditorGUILayout.FloatField("Bounce Force", bounceForce);
        minimumFallSpeed = EditorGUILayout.FloatField("Min Fall Speed", minimumFallSpeed);
        
        GUILayout.Space(5);
        GUILayout.Label("Trigger Setup", EditorStyles.boldLabel);
        triggerPosition = EditorGUILayout.Vector2Field("Trigger Position", triggerPosition);
        triggerSize = EditorGUILayout.Vector2Field("Trigger Size", triggerSize);
        
        EditorGUILayout.HelpBox("Head stomp creates child trigger objects on HeadStompTrigger layer.\nTriggers automatically follow enemy movement.\nPlayer gets automatic velocity boost when falling into trigger.\nNever damages enemies.", MessageType.Info);
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Action buttons
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Add to All Enemies in Scene", GUILayout.Height(30)))
        {
            AddToAllEnemies();
        }
        
        if (GUILayout.Button("Add to Selected Enemies", GUILayout.Height(25)))
        {
            AddToSelectedEnemies();
        }
        
        GUILayout.Space(5);
        removeOldComponents = EditorGUILayout.Toggle("Remove Old Components", removeOldComponents);
        
        if (removeOldComponents)
        {
            EditorGUILayout.HelpBox("Old system has been removed - cleanup not needed", MessageType.Info);
        }
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Prefab update section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Prefab Management", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Update Enemy Prefabs", GUILayout.Height(25)))
        {
            UpdateEnemyPrefabs();
        }
        
        EditorGUILayout.HelpBox("Updates all enemy prefabs in Assets folder", MessageType.None);
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Status section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Current Status", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Check Setup Status", GUILayout.Height(25)))
        {
            CheckStatus();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Remove All Head Stomp Triggers", GUILayout.Height(25)))
        {
            RemoveAllHeadStompTriggers();
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void AddToAllEnemies()
    {
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        int added = 0;
        int updated = 0;
        
        foreach (var enemy in enemies)
        {
            if (AddSimpleHeadStomp(enemy.gameObject))
                added++;
            else
                updated++;
        }
        
        EditorUtility.DisplayDialog("Complete", 
            $"Added SimpleHeadStomp to {added} enemies.\nUpdated {updated} existing components.", 
            "OK");
        
        if (added > 0 || updated > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
    
    private void AddToSelectedEnemies()
    {
        GameObject[] selected = Selection.gameObjects;
        int added = 0;
        int updated = 0;
        
        foreach (var obj in selected)
        {
            if (obj.GetComponent<EnemyBase>() != null)
            {
                if (AddSimpleHeadStomp(obj))
                    added++;
                else
                    updated++;
            }
        }
        
        if (added == 0 && updated == 0)
        {
            EditorUtility.DisplayDialog("No Enemies Selected", 
                "Please select enemy GameObjects with EnemyBase component.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Complete", 
                $"Added SimpleHeadStomp to {added} enemies.\nUpdated {updated} existing components.", 
                "OK");
            
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
    
    private bool AddSimpleHeadStomp(GameObject enemy)
    {
        // Check if component already exists
        SimpleHeadStomp existing = enemy.GetComponent<SimpleHeadStomp>();
        
        if (existing == null)
        {
            // Add new component
            SimpleHeadStomp stomp = enemy.AddComponent<SimpleHeadStomp>();
            ConfigureComponent(stomp);
            EditorUtility.SetDirty(enemy);
            Debug.Log($"Added SimpleHeadStomp to {enemy.name}");
            return true;
        }
        else
        {
            // Update existing component
            ConfigureComponent(existing);
            EditorUtility.SetDirty(enemy);
            Debug.Log($"Updated SimpleHeadStomp on {enemy.name}");
            return false;
        }
    }
    
    private void ConfigureComponent(SimpleHeadStomp stomp)
    {
        SerializedObject so = new SerializedObject(stomp);
        
        var bounceForceProperty = so.FindProperty("bounceForce");
        if (bounceForceProperty != null)
            bounceForceProperty.floatValue = bounceForce;
        
        var minFallSpeedProperty = so.FindProperty("minimumFallSpeed");
        if (minFallSpeedProperty != null)
            minFallSpeedProperty.floatValue = minimumFallSpeed;
        
        var positionProperty = so.FindProperty("triggerPosition");
        if (positionProperty != null)
            positionProperty.vector2Value = triggerPosition;
        
        var sizeProperty = so.FindProperty("triggerSize");
        if (sizeProperty != null)
            sizeProperty.vector2Value = triggerSize;
        
        // Auto-configure player layer
        var playerLayerProperty = so.FindProperty("playerLayer");
        if (playerLayerProperty != null && playerLayerProperty.intValue == -1)
        {
            // Try to find player layer
            int playerLayerNum = LayerMask.NameToLayer("Player");
            if (playerLayerNum == -1)
                playerLayerNum = LayerMask.NameToLayer("PlayerHitbox");
            if (playerLayerNum == -1)
                playerLayerNum = 0; // Default layer
                
            playerLayerProperty.intValue = 1 << playerLayerNum;
            Debug.Log($"Auto-configured player layer for {stomp.name}: Layer {playerLayerNum} (mask: {1 << playerLayerNum})");
        }
        
        so.ApplyModifiedProperties();
    }
    
    private void CleanupOldComponents()
    {
        if (!EditorUtility.DisplayDialog("Confirm Cleanup", 
            "This will remove all old head stomp components. Make sure to save your scene first!\n\nContinue?", 
            "Yes, Clean Up", "Cancel"))
        {
            return;
        }
        
        int removed = 0;
        
        try
        {
            // Step 1: Disable components first to prevent Unity graph errors
            DisableOldComponents();
            
            // Old components have been removed - cleanup already complete
            Debug.Log("Old head stomp system already cleaned up");
            
            // Step 5: Force refresh
            AssetDatabase.Refresh();
            EditorApplication.delayCall += () => {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            };
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during cleanup: {ex.Message}");
            EditorUtility.DisplayDialog("Cleanup Error", 
                $"An error occurred during cleanup:\n{ex.Message}\n\nSome components may need to be removed manually.", 
                "OK");
            return;
        }
        
        EditorUtility.DisplayDialog("Cleanup Complete", 
            $"Safely removed {removed} old head stomp components.\n\nRestart Unity Editor if you experience any issues.", 
            "OK");
    }
    
    private void DisableOldComponents()
    {
        // Old components have been removed - this is now a no-op
        Debug.Log("Old components already removed - cleanup not needed");
    }
    
    private int SafeRemoveComponent<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component != null)
        {
            try
            {
                // Disable first
                if (component is MonoBehaviour mb)
                    mb.enabled = false;
                
                // Use delayed removal to avoid graph errors
                EditorApplication.delayCall += () => {
                    if (component != null)
                        Object.DestroyImmediate(component);
                };
                
                Debug.Log($"Scheduled removal of {typeof(T).Name} from {obj.name}");
                return 1;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not remove {typeof(T).Name} from {obj.name}: {ex.Message}");
                return 0;
            }
        }
        return 0;
    }
    
    private bool SafeRemoveGameObject(GameObject obj)
    {
        if (obj != null)
        {
            try
            {
                // Disable first
                obj.SetActive(false);
                
                // Use delayed removal
                EditorApplication.delayCall += () => {
                    if (obj != null)
                        Object.DestroyImmediate(obj);
                };
                
                Debug.Log($"Scheduled removal of GameObject {obj.name}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not remove GameObject {obj.name}: {ex.Message}");
                return false;
            }
        }
        return false;
    }
    
    private void UpdateEnemyPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int updated = 0;
        
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null && prefab.GetComponent<EnemyBase>() != null)
            {
                // Load prefab for editing
                GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);
                
                try
                {
                    // Add or update SimpleHeadStomp
                    bool modified = AddSimpleHeadStomp(prefabInstance);
                    
                    // Remove old components if requested
                    if (removeOldComponents)
                    {
                        modified |= RemoveOldComponentsFromPrefab(prefabInstance);
                    }
                    
                    if (modified)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
                        updated++;
                        Debug.Log($"Updated prefab: {path}");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabInstance);
                }
            }
        }
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Prefab Update Complete", 
            $"Updated {updated} enemy prefabs with SimpleHeadStomp.", 
            "OK");
    }
    
    private bool RemoveOldComponentsFromPrefab(GameObject prefab)
    {
        // Old components already removed
        return false;
    }
    
    private void CheckStatus()
    {
        int enemiesWithSimple = 0;
        int enemiesWithOld = 0;
        int enemiesWithout = 0;
        
        EnemyBase[] enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        
        foreach (var enemy in enemies)
        {
            bool hasSimple = enemy.GetComponent<SimpleHeadStomp>() != null;
            
            if (hasSimple)
                enemiesWithSimple++;
            else
                enemiesWithout++;
        }
        
        // Player status - old components removed
        string playerStatus = "Clean";
        
        string message = $"Enemy Status:\n" +
                        $"• {enemiesWithSimple} with SimpleHeadStomp ✓\n" +
                        $"• {enemiesWithOld} with old system\n" +
                        $"• {enemiesWithout} without head stomp\n\n" +
                        $"Player Status: {playerStatus}\n\n" +
                        $"Total Enemies: {enemies.Length}";
        
        EditorUtility.DisplayDialog("Head Stomp Status", message, "OK");
    }
    
    private void RemoveAllHeadStompTriggers()
    {
        if (!EditorUtility.DisplayDialog("Remove All Head Stomp Triggers", 
            "This will remove ALL SimpleHeadStomp components and their child triggers from the scene.\n\nThis action cannot be undone!\n\nContinue?", 
            "Yes, Remove All", "Cancel"))
        {
            return;
        }
        
        int removedComponents = 0;
        int removedTriggers = 0;
        
        try
        {
            // Find all SimpleHeadStomp components in scene
            SimpleHeadStomp[] headStomps = FindObjectsByType<SimpleHeadStomp>(FindObjectsSortMode.None);
            
            foreach (var headStomp in headStomps)
            {
                if (headStomp != null)
                {
                    // Find and remove child trigger
                    Transform childTrigger = headStomp.transform.Find("HeadStompTrigger");
                    if (childTrigger != null)
                    {
                        DestroyImmediate(childTrigger.gameObject);
                        removedTriggers++;
                    }
                    
                    // Remove the SimpleHeadStomp component
                    DestroyImmediate(headStomp);
                    removedComponents++;
                }
            }
            
            // Also check for any standalone trigger objects that might exist
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name == "HeadStompTrigger" && obj.GetComponent<HeadStompTriggerHandler>() != null)
                {
                    DestroyImmediate(obj);
                    removedTriggers++;
                }
            }
            
            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                
            EditorUtility.DisplayDialog("Removal Complete", 
                $"Successfully removed:\n\u2022 {removedComponents} SimpleHeadStomp components\n\u2022 {removedTriggers} head stomp triggers\n\nScene has been marked as dirty.", 
                "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during head stomp removal: {ex.Message}");
            EditorUtility.DisplayDialog("Removal Error", 
                $"An error occurred during removal:\n{ex.Message}\n\nSome components may need to be removed manually.", 
                "OK");
        }
    }
}