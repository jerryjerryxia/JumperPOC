using UnityEngine;
using UnityEditor;

public class EnemyTestSceneSetup : EditorWindow
{
    [MenuItem("Tools/Setup Enemy Test Scene")]
    public static void SetupTestScene()
    {
        // Clear existing objects
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.name != "Main Camera" && obj.name != "Directional Light")
            {
                DestroyImmediate(obj);
            }
        }
        
        // Create ground platforms at different heights
        CreatePlatform(new Vector3(0, -2, 0), new Vector3(20, 1, 1), "Main Platform");
        CreatePlatform(new Vector3(-15, 0, 0), new Vector3(8, 1, 1), "Left Platform");
        CreatePlatform(new Vector3(15, 1, 0), new Vector3(8, 1, 1), "Right Platform");
        CreatePlatform(new Vector3(0, 3, 0), new Vector3(10, 1, 1), "Upper Platform");
        
        // Create walls to test boundary detection
        CreateWall(new Vector3(-10, -0.5f, 0), new Vector3(0.5f, 2, 1), "Left Wall");
        CreateWall(new Vector3(10, -0.5f, 0), new Vector3(0.5f, 2, 1), "Right Wall");
        
        // Spawn player
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (playerPrefab != null)
        {
            GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            player.transform.position = new Vector3(0, 0, 0);
            player.name = "Player";
        }
        else
        {
            Debug.LogWarning("Player prefab not found at Assets/Prefabs/Player.prefab");
        }
        
        // Spawn enemies on different platforms
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy1.prefab");
        if (enemyPrefab != null)
        {
            // Enemy on main platform
            GameObject enemy1 = PrefabUtility.InstantiatePrefab(enemyPrefab) as GameObject;
            enemy1.transform.position = new Vector3(-5, -1, 0);
            enemy1.name = "Enemy1_MainPlatform";
            
            // Enemy on left platform
            GameObject enemy2 = PrefabUtility.InstantiatePrefab(enemyPrefab) as GameObject;
            enemy2.transform.position = new Vector3(-15, 1, 0);
            enemy2.name = "Enemy2_LeftPlatform";
            
            // Enemy on right platform
            GameObject enemy3 = PrefabUtility.InstantiatePrefab(enemyPrefab) as GameObject;
            enemy3.transform.position = new Vector3(15, 2, 0);
            enemy3.name = "Enemy3_RightPlatform";
            
            // Enemy on upper platform
            GameObject enemy4 = PrefabUtility.InstantiatePrefab(enemyPrefab) as GameObject;
            enemy4.transform.position = new Vector3(0, 4, 0);
            enemy4.name = "Enemy4_UpperPlatform";
        }
        else
        {
            Debug.LogWarning("Enemy prefab not found at Assets/Prefabs/Enemy1.prefab");
        }
        
        Debug.Log("Enemy test scene setup complete!");
        Debug.Log("Test the following behaviors:");
        Debug.Log("1. Enemies patrol their platforms without falling off");
        Debug.Log("2. Enemies only chase when player is on their platform");
        Debug.Log("3. Enemies stop at platform edges during chase");
        Debug.Log("4. Enemies return to patrol when player leaves platform");
    }
    
    private static void CreatePlatform(Vector3 position, Vector3 scale, string name)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.transform.position = position;
        platform.transform.localScale = scale;
        platform.name = name;
        platform.layer = LayerMask.NameToLayer("Ground");
        
        // Add a different color for visual distinction
        Renderer renderer = platform.GetComponent<Renderer>();
        renderer.material.color = new Color(0.5f, 0.5f, 0.5f);
    }
    
    private static void CreateWall(Vector3 position, Vector3 scale, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.name = name;
        wall.layer = LayerMask.NameToLayer("Ground");
        
        // Add a different color for visual distinction
        Renderer renderer = wall.GetComponent<Renderer>();
        renderer.material.color = new Color(0.3f, 0.3f, 0.3f);
    }
}