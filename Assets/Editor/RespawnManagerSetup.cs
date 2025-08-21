using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor helper to set up the SimpleRespawnManager in the scene
/// </summary>
public class RespawnManagerSetup : Editor
{
    [MenuItem("GameObject/2D Object/Respawn Manager", false, 10)]
    public static void CreateRespawnManager()
    {
        // Check if SimpleRespawnManager already exists
        SimpleRespawnManager existingManager = FindObjectOfType<SimpleRespawnManager>();
        if (existingManager != null)
        {
            Debug.LogWarning("SimpleRespawnManager already exists in the scene!");
            Selection.activeGameObject = existingManager.gameObject;
            return;
        }
        
        // Create new GameObject with SimpleRespawnManager
        GameObject respawnManagerGO = new GameObject("SimpleRespawnManager");
        SimpleRespawnManager manager = respawnManagerGO.AddComponent<SimpleRespawnManager>();
        
        // Position at world origin
        respawnManagerGO.transform.position = Vector3.zero;
        
        // Select the newly created object
        Selection.activeGameObject = respawnManagerGO;
        
        // Mark scene as dirty for saving
        EditorUtility.SetDirty(respawnManagerGO);
        
        Debug.Log("SimpleRespawnManager created successfully! This object will persist across scenes.");
    }
    
    [MenuItem("Tools/Setup Save Point System")]
    public static void SetupSavePointSystem()
    {
        // Ensure SimpleRespawnManager exists
        SimpleRespawnManager manager = FindObjectOfType<SimpleRespawnManager>();
        if (manager == null)
        {
            CreateRespawnManager();
            Debug.Log("✓ SimpleRespawnManager created");
        }
        else
        {
            Debug.Log("✓ SimpleRespawnManager already exists");
        }
        
        // Check for player
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("No PlayerController found in scene - make sure to add player!");
        }
        else
        {
            // Verify player has correct tag
            if (!player.CompareTag("Player"))
            {
                player.tag = "Player";
                EditorUtility.SetDirty(player.gameObject);
                Debug.Log("✓ Player tag set to 'Player'");
            }
            else
            {
                Debug.Log("✓ Player tag already correct");
            }
        }
        
        // Find all save points
        SavePoint[] savePoints = FindObjectsOfType<SavePoint>();
        if (savePoints.Length == 0)
        {
            Debug.LogWarning("No SavePoints found in scene");
        }
        else
        {
            Debug.Log($"✓ Found {savePoints.Length} SavePoint(s) in scene");
            
            // Verify each save point has a trigger collider
            foreach (SavePoint sp in savePoints)
            {
                Collider2D col = sp.GetComponent<Collider2D>();
                if (col == null)
                {
                    BoxCollider2D newCol = sp.gameObject.AddComponent<BoxCollider2D>();
                    newCol.isTrigger = true;
                    newCol.size = new Vector2(2f, 3f);
                    EditorUtility.SetDirty(sp.gameObject);
                    Debug.Log($"  Added trigger collider to {sp.name}");
                }
                else if (!col.isTrigger)
                {
                    col.isTrigger = true;
                    EditorUtility.SetDirty(sp.gameObject);
                    Debug.Log($"  Set collider to trigger on {sp.name}");
                }
            }
        }
        
        Debug.Log("\n=== Save Point System Setup Complete ===");
        Debug.Log("Test by: 1) Playing scene, 2) Touching a save point, 3) Dying to respawn at save point");
    }
    
    // Add validation in play mode
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ValidateRespawnManager()
    {
        if (Application.isPlaying)
        {
            SimpleRespawnManager manager = FindObjectOfType<SimpleRespawnManager>();
            if (manager == null)
            {
                Debug.LogError("SimpleRespawnManager is missing! Save points will not work. Use Tools > Setup Save Point System to fix.");
            }
        }
    }
}