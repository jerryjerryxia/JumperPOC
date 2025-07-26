using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SceneSetupHelper : EditorWindow
{
    [MenuItem("Tools/Scene Setup Helper")]
    public static void ShowWindow()
    {
        GetWindow<SceneSetupHelper>("Scene Setup Helper");
    }

    private void OnGUI()
    {
        GUILayout.Label("Scene Setup Helper", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Setup Layers", GUILayout.Height(30)))
        {
            SetupLayers();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Test Platform", GUILayout.Height(30)))
        {
            CreateTestPlatform();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Setup Player GameObject", GUILayout.Height(30)))
        {
            SetupPlayerGameObject();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Setup InputManager", GUILayout.Height(30)))
        {
            SetupInputManager();
        }
        
        GUILayout.Space(20);
        
        EditorGUILayout.HelpBox(
            "This tool helps set up your scene for player movement and interaction:\n\n" +
            "1. Setup Layers - Creates the necessary layers (Ground, Ledge)\n" +
            "2. Create Test Platform - Makes a simple platform to test with\n" +
            "3. Setup Player GameObject - Ensures your player has all required components\n" +
            "4. Setup InputManager - Creates InputManager for handling player input\n\n" +
            "Make sure to assign your ground objects to Layer 6 (Ground) and ledge objects to Layer 7 (Ledge).",
            MessageType.Info
        );
    }

    private void SetupLayers()
    {
        // Check if layers already exist
        bool groundLayerExists = LayerMask.NameToLayer("Ground") != -1;
        bool ledgeLayerExists = LayerMask.NameToLayer("Ledge") != -1;
        
        if (!groundLayerExists)
        {
            // Try to add Ground layer to slot 6
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            
            if (layers.arraySize > 6)
            {
                layers.GetArrayElementAtIndex(6).stringValue = "Ground";
                tagManager.ApplyModifiedProperties();
                Debug.Log("Added Ground layer to slot 6");
            }
        }
        
        if (!ledgeLayerExists)
        {
            // Try to add Ledge layer to slot 7
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            
            if (layers.arraySize > 7)
            {
                layers.GetArrayElementAtIndex(7).stringValue = "Ledge";
                tagManager.ApplyModifiedProperties();
                Debug.Log("Added Ledge layer to slot 7");
            }
        }
        
        Debug.Log("Layer setup complete! Remember to assign your ground objects to Layer 6 (Ground).");
    }

    private void CreateTestPlatform()
    {
        // Create a simple platform for testing
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "TestPlatform";
        platform.transform.position = new Vector3(0, -2, 0);
        platform.transform.localScale = new Vector3(10, 1, 1);
        
        // Set to Ground layer
        platform.layer = LayerMask.NameToLayer("Ground");
        
        // Create a ledge for testing
        GameObject ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ledge.name = "TestLedge";
        ledge.transform.position = new Vector3(5, 0, 0);
        ledge.transform.localScale = new Vector3(2, 0.5f, 1);
        
        // Set to Ground layer
        ledge.layer = LayerMask.NameToLayer("Ground");
        
        // Create a wall for testing
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "TestWall";
        wall.transform.position = new Vector3(8, 2, 0);
        wall.transform.localScale = new Vector3(1, 4, 1);
        
        // Set to Ground layer
        wall.layer = LayerMask.NameToLayer("Ground");
        
        Debug.Log("Created test platform, ledge, and wall. They are set to Ground layer (6).");
    }

    private void SetupPlayerGameObject()
    {
        // Find or create player GameObject
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            player.transform.position = new Vector3(0, 0, 0);
        }
        
        // Add required components
        if (player.GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            Debug.Log("Added Rigidbody2D to Player");
        }
        
        if (player.GetComponent<PlayerController>() == null)
        {
            player.AddComponent<PlayerController>();
            Debug.Log("Added PlayerController to Player");
        }
        
        if (player.GetComponent<PlayerInteractionDetector>() == null)
        {
            player.AddComponent<PlayerInteractionDetector>();
            Debug.Log("Added PlayerInteractionDetector to Player");
        }
        
        if (player.GetComponent<Animator>() == null)
        {
            Animator animator = player.AddComponent<Animator>();
            // Try to find and assign the PlayerAnimator controller
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/PlayerAnimator.controller");
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                Debug.Log("Assigned PlayerAnimator controller to Player");
            }
            else
            {
                Debug.LogWarning("Could not find PlayerAnimator.controller. Please assign it manually.");
            }
        }
        
        if (player.GetComponent<SpriteRenderer>() == null)
        {
            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateDefaultSprite();
            Debug.Log("Added SpriteRenderer with default sprite to Player");
        }
        
        if (player.GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.6f);
            Debug.Log("Added BoxCollider2D to Player");
        }
        
        // Select the player in the hierarchy
        Selection.activeGameObject = player;
        
        Debug.Log("Player GameObject setup complete!");
    }

    private Sprite CreateDefaultSprite()
    {
        // Create a simple colored square sprite
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.blue; // Blue color for player
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        return sprite;
    }
    
    private void SetupInputManager()
    {
        // Check if InputManager already exists in the scene
        InputManager existingManager = Object.FindFirstObjectByType<InputManager>();
        if (existingManager != null)
        {
            Debug.Log("InputManager already exists in the scene.");
            Selection.activeGameObject = existingManager.gameObject;
            return;
        }
        
        // Create InputManager GameObject
        GameObject inputManagerGO = new GameObject("InputManager");
        inputManagerGO.AddComponent<InputManager>();
        
        // Position it at origin (it doesn't need to be positioned specifically)
        inputManagerGO.transform.position = Vector3.zero;
        
        // Select it in the hierarchy
        Selection.activeGameObject = inputManagerGO;
        
        Debug.Log("InputManager GameObject created and added to the scene!");
        Debug.Log("The InputManager will now handle all player input using the new Input System.");
    }
} 