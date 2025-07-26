using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class PlayerMigrationTool : EditorWindow
{
    private Vector2 scrollPosition;
    private List<MigrationResult> migrationResults = new List<MigrationResult>();
    private bool showOnlyIssues = true;
    
    private class MigrationResult
    {
        public string scenePath;
        public string objectPath;
        public GameObject gameObject;
        public List<string> issues = new List<string>();
        public List<string> fixes = new List<string>();
        public bool hasIssues => issues.Count > 0;
    }
    
    [MenuItem("Tools/Player Migration Tool")]
    public static void ShowWindow()
    {
        GetWindow<PlayerMigrationTool>("Player Migration Tool");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("Player Component Migration Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool helps migrate Player GameObjects to the new component structure (PlayerController + PlayerCombat).", MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Scan All Scenes", GUILayout.Height(30)))
        {
            ScanAllScenes();
        }
        
        if (GUILayout.Button("Scan Current Scene", GUILayout.Height(30)))
        {
            ScanCurrentScene();
        }
        
        EditorGUILayout.Space();
        
        if (migrationResults.Count > 0)
        {
            EditorGUILayout.LabelField($"Found {migrationResults.Count} Player objects", EditorStyles.boldLabel);
            showOnlyIssues = EditorGUILayout.Toggle("Show Only Issues", showOnlyIssues);
            
            if (GUILayout.Button("Fix All Issues"))
            {
                FixAllIssues();
            }
            
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var result in migrationResults)
            {
                if (showOnlyIssues && !result.hasIssues) continue;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField($"Scene: {result.scenePath}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Object: {result.objectPath}");
                
                if (result.hasIssues)
                {
                    EditorGUILayout.LabelField("Issues:", EditorStyles.boldLabel);
                    foreach (var issue in result.issues)
                    {
                        EditorGUILayout.LabelField($"  • {issue}", EditorStyles.wordWrappedLabel);
                    }
                    
                    if (GUILayout.Button("Fix This Object"))
                    {
                        FixGameObject(result);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("✓ No issues found", EditorStyles.wordWrappedLabel);
                }
                
                if (result.fixes.Count > 0)
                {
                    EditorGUILayout.LabelField("Applied Fixes:", EditorStyles.boldLabel);
                    foreach (var fix in result.fixes)
                    {
                        EditorGUILayout.LabelField($"  ✓ {fix}", EditorStyles.wordWrappedLabel);
                    }
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
        }
    }
    
    private void ScanAllScenes()
    {
        migrationResults.Clear();
        
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in guids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            ScanScene(scenePath);
        }
        
        Debug.Log($"Scanned {guids.Length} scenes, found {migrationResults.Count} Player objects");
    }
    
    private void ScanCurrentScene()
    {
        migrationResults.Clear();
        
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.IsValid())
        {
            ScanSceneObjects(currentScene.path);
        }
    }
    
    private void ScanScene(string scenePath)
    {
        var originalScene = EditorSceneManager.GetActiveScene();
        var originalScenePath = originalScene.path;
        
        // Open the scene
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        ScanSceneObjects(scenePath);
        
        // Restore original scene if different
        if (!string.IsNullOrEmpty(originalScenePath) && originalScenePath != scenePath)
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }
    }
    
    private void ScanSceneObjects(string scenePath)
    {
        // Find all GameObjects with PlayerController
        PlayerController[] players = GameObject.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        
        foreach (var player in players)
        {
            var result = new MigrationResult
            {
                scenePath = scenePath,
                objectPath = GetGameObjectPath(player.gameObject),
                gameObject = player.gameObject
            };
            
            // Check for PlayerCombat component
            var combat = player.GetComponent<PlayerCombat>();
            if (combat == null)
            {
                result.issues.Add("Missing PlayerCombat component");
            }
            else
            {
                // Check PlayerCombat setup
                if (combat.AttackHitbox == null)
                {
                    result.issues.Add("PlayerCombat.AttackHitbox is not assigned");
                }
            }
            
            // Check for InputManager in scene
            if (GameObject.FindFirstObjectByType<InputManager>() == null)
            {
                result.issues.Add("No InputManager found in scene");
            }
            
            // Check animator setup
            var animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                result.issues.Add("Missing Animator component");
            }
            else if (animator.runtimeAnimatorController == null)
            {
                result.issues.Add("Animator Controller is not assigned");
            }
            
            // Check Rigidbody2D setup
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                result.issues.Add("Missing Rigidbody2D component");
            }
            
            // Check layer setup
            if (player.gameObject.layer != LayerMask.NameToLayer("Default") && 
                player.gameObject.layer != LayerMask.NameToLayer("Player"))
            {
                // Player can be on Default or Player layer
            }
            
            migrationResults.Add(result);
        }
        
        // Also check for old PlayerController_Backup instances
        var backupType = System.Type.GetType("PlayerController_Backup");
        if (backupType != null)
        {
            UnityEngine.Object[] backups = GameObject.FindObjectsByType(backupType, FindObjectsSortMode.None);
            foreach (var backup in backups)
            {
                var backupComponent = backup as Component;
                if (backupComponent == null) continue;
                
                var result = new MigrationResult
                {
                    scenePath = scenePath,
                    objectPath = GetGameObjectPath(backupComponent.gameObject),
                    gameObject = backupComponent.gameObject
                };
                
                result.issues.Add("Still using old PlayerController_Backup component");
                migrationResults.Add(result);
            }
        }
    }
    
    private void FixAllIssues()
    {
        foreach (var result in migrationResults)
        {
            if (result.hasIssues)
            {
                FixGameObject(result);
            }
        }
        
        // Rescan to update results
        if (migrationResults.Count > 0)
        {
            ScanAllScenes();
        }
    }
    
    private void FixGameObject(MigrationResult result)
    {
        if (result.gameObject == null) return;
        
        bool madeChanges = false;
        
        // Add PlayerCombat if missing
        if (result.issues.Contains("Missing PlayerCombat component"))
        {
            var combat = result.gameObject.AddComponent<PlayerCombat>();
            result.fixes.Add("Added PlayerCombat component");
            madeChanges = true;
            
            // Try to find and assign attack hitbox
            var hitbox = result.gameObject.GetComponentInChildren<AttackHitbox>();
            if (hitbox != null)
            {
                combat.AttackHitbox = hitbox;
                result.fixes.Add($"Auto-assigned AttackHitbox: {hitbox.gameObject.name}");
            }
            else
            {
                // Fallback: look for a collider with "attack" in name and add AttackHitbox component
                var hitboxCollider = result.gameObject.GetComponentInChildren<Collider2D>();
                if (hitboxCollider != null && hitboxCollider.gameObject.name.ToLower().Contains("attack"))
                {
                    var attackHitbox = hitboxCollider.gameObject.AddComponent<AttackHitbox>();
                    combat.AttackHitbox = attackHitbox;
                    result.fixes.Add($"Added AttackHitbox component to: {hitboxCollider.gameObject.name}");
                }
            }
        }
        
        // Add InputManager to scene if missing
        if (result.issues.Contains("No InputManager found in scene"))
        {
            var inputManagerGO = new GameObject("InputManager");
            inputManagerGO.AddComponent<InputManager>();
            result.fixes.Add("Added InputManager to scene");
            madeChanges = true;
        }
        
        // Handle old backup component
        if (result.issues.Contains("Still using old PlayerController_Backup component"))
        {
            var backupType = System.Type.GetType("PlayerController_Backup");
            if (backupType != null)
            {
                var backup = result.gameObject.GetComponent(backupType);
                if (backup != null)
                {
                    DestroyImmediate(backup);
                    result.fixes.Add("Removed old PlayerController_Backup component");
                    
                    // Add new components if needed
                    if (result.gameObject.GetComponent<PlayerController>() == null)
                    {
                        result.gameObject.AddComponent<PlayerController>();
                        result.fixes.Add("Added new PlayerController component");
                    }
                    
                    if (result.gameObject.GetComponent<PlayerCombat>() == null)
                    {
                        result.gameObject.AddComponent<PlayerCombat>();
                        result.fixes.Add("Added PlayerCombat component");
                    }
                    
                    madeChanges = true;
                }
            }
        }
        
        if (madeChanges)
        {
            EditorUtility.SetDirty(result.gameObject);
            EditorSceneManager.MarkSceneDirty(result.gameObject.scene);
            
            // Clear issues that were fixed
            result.issues.Clear();
            
            // Re-validate
            var player = result.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                var combat = player.GetComponent<PlayerCombat>();
                if (combat != null && combat.AttackHitbox == null)
                {
                    result.issues.Add("PlayerCombat.AttackHitbox still needs to be assigned manually");
                }
            }
        }
    }
    
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
}