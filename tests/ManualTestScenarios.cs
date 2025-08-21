using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Manual test scenarios for validating SimpleEnemy edge detection in Unity Editor.
    /// These scenarios should be set up manually in test scenes for visual verification.
    /// </summary>
    public class ManualTestScenarios : MonoBehaviour
    {
        [Header("Test Scenario Controls")]
        [SerializeField] private bool showTestInstructions = true;
        [SerializeField] private bool enableGizmoVisualization = true;
        
        [Header("Test Configuration")]
        [SerializeField] private LayerMask groundLayer = 1 << 6;
        [SerializeField] private Material platformMaterial;
        [SerializeField] private Material wallMaterial;

        private void OnValidate()
        {
            if (showTestInstructions)
            {
                Debug.Log("[Manual Test] Use the methods below to create test scenarios in the Editor");
            }
        }

        #region Test Scenario Creation Methods

        /// <summary>
        /// Creates Test Scenario 1: Open Platform Edge
        /// Expected: Enemy should stop and turn at platform edges using downward raycast
        /// </summary>
        [ContextMenu("Create Test 1: Open Platform Edge")]
        public void CreateOpenPlatformEdgeTest()
        {
            // Create platform
            GameObject platform = CreatePlatform("Test1_Platform", Vector3.zero, new Vector3(8f, 1f, 1f));
            
            // Create enemy
            GameObject enemy = CreateEnemy("Test1_Enemy", new Vector3(-3f, 1f, 0f));
            
            // Position camera
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(0f, 2f, -10f);
            }
            
            Debug.Log("[Test 1] Open Platform Edge - Enemy should patrol and stop at platform edges");
            Debug.Log("Expected: Enemy moves left/right, stops before falling off, turns around");
        }

        /// <summary>
        /// Creates Test Scenario 2: Wall-Blocked Edge (New Functionality)
        /// Expected: Enemy should detect corner where wall meets platform using upward raycast
        /// </summary>
        [ContextMenu("Create Test 2: Wall-Blocked Edge")]
        public void CreateWallBlockedEdgeTest()
        {
            // Create main platform
            GameObject platform = CreatePlatform("Test2_Platform", Vector3.zero, new Vector3(6f, 1f, 1f));
            
            // Create wall at right edge
            GameObject wall = CreateWall("Test2_Wall", new Vector3(3f, 2.5f, 0f), new Vector3(1f, 4f, 1f));
            
            // Create enemy
            GameObject enemy = CreateEnemy("Test2_Enemy", new Vector3(-2f, 1f, 0f));
            
            // Position camera
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(0f, 3f, -10f);
            }
            
            Debug.Log("[Test 2] Wall-Blocked Edge - Enemy should detect wall corner using upward raycast");
            Debug.Log("Expected: Enemy approaches wall, stops before corner (not at platform edge), turns around");
        }

        /// <summary>
        /// Creates Test Scenario 3: L-Shaped Corner
        /// Expected: Both raycasts should trigger at inside corner
        /// </summary>
        [ContextMenu("Create Test 3: L-Shaped Corner")]
        public void CreateLShapedCornerTest()
        {
            // Create horizontal platform
            GameObject platform1 = CreatePlatform("Test3_Platform1", Vector3.zero, new Vector3(6f, 1f, 1f));
            
            // Create vertical platform (wall)
            GameObject platform2 = CreatePlatform("Test3_Platform2", new Vector3(3f, 3f, 0f), new Vector3(1f, 5f, 1f));
            
            // Create enemy
            GameObject enemy = CreateEnemy("Test3_Enemy", new Vector3(-2f, 1f, 0f));
            
            Debug.Log("[Test 3] L-Shaped Corner - Enemy should detect corner using combined raycast logic");
            Debug.Log("Expected: Enemy stops at corner where platform meets wall");
        }

        /// <summary>
        /// Creates Test Scenario 4: Normal Platform Walking
        /// Expected: No edge detection in middle of long platform
        /// </summary>
        [ContextMenu("Create Test 4: Normal Platform Walking")]
        public void CreateNormalPlatformTest()
        {
            // Create long platform
            GameObject platform = CreatePlatform("Test4_Platform", Vector3.zero, new Vector3(20f, 1f, 1f));
            
            // Create enemy
            GameObject enemy = CreateEnemy("Test4_Enemy", new Vector3(0f, 1f, 0f));
            
            Debug.Log("[Test 4] Normal Platform Walking - Enemy should patrol without false edge detection");
            Debug.Log("Expected: Enemy walks back and forth across entire platform length");
        }

        /// <summary>
        /// Creates Test Scenario 5: Ceiling vs Wall Test
        /// Expected: High ceiling ignored, low wall detected
        /// </summary>
        [ContextMenu("Create Test 5: Ceiling vs Wall")]
        public void CreateCeilingVsWallTest()
        {
            // Create platform
            GameObject platform = CreatePlatform("Test5_Platform", Vector3.zero, new Vector3(10f, 1f, 1f));
            
            // Create high ceiling (should be ignored)
            GameObject ceiling = CreateWall("Test5_Ceiling", new Vector3(0f, 6f, 0f), new Vector3(10f, 1f, 1f));
            
            // Create low wall at edge (should be detected)
            GameObject wall = CreateWall("Test5_Wall", new Vector3(4f, 2f, 0f), new Vector3(1f, 3f, 1f));
            
            // Create enemy
            GameObject enemy = CreateEnemy("Test5_Enemy", new Vector3(-3f, 1f, 0f));
            
            Debug.Log("[Test 5] Ceiling vs Wall - High ceiling ignored, low wall detected");
            Debug.Log("Expected: Enemy walks under ceiling, stops at wall");
        }

        /// <summary>
        /// Creates Test Scenario 6: Chase Behavior with Wall
        /// Expected: Enemy chases but respects wall boundaries
        /// </summary>
        [ContextMenu("Create Test 6: Chase Behavior")]
        public void CreateChaseBehaviorTest()
        {
            // Create platform
            GameObject platform = CreatePlatform("Test6_Platform", Vector3.zero, new Vector3(8f, 1f, 1f));
            
            // Create wall barrier
            GameObject wall = CreateWall("Test6_Wall", new Vector3(2f, 2f, 0f), new Vector3(1f, 3f, 1f));
            
            // Create enemy
            GameObject enemy = CreateEnemy("Test6_Enemy", new Vector3(-3f, 1f, 0f));
            
            // Create player (for chase testing)
            GameObject player = CreatePlayer("Test6_Player", new Vector3(3f, 1f, 0f));
            
            Debug.Log("[Test 6] Chase Behavior - Enemy should chase but not pass through wall");
            Debug.Log("Expected: Enemy detects player, chases, stops at wall barrier");
            Debug.Log("Manual: Move player to different positions to test chase logic");
        }

        /// <summary>
        /// Creates Test Scenario 7: Multiple Enemies Stress Test
        /// Expected: All enemies should behave correctly simultaneously
        /// </summary>
        [ContextMenu("Create Test 7: Multiple Enemies")]
        public void CreateMultipleEnemiesTest()
        {
            // Create complex platform layout
            GameObject platform1 = CreatePlatform("Test7_Platform1", new Vector3(-5f, 0f, 0f), new Vector3(8f, 1f, 1f));
            GameObject platform2 = CreatePlatform("Test7_Platform2", new Vector3(5f, 2f, 0f), new Vector3(6f, 1f, 1f));
            GameObject platform3 = CreatePlatform("Test7_Platform3", new Vector3(0f, -2f, 0f), new Vector3(10f, 1f, 1f));
            
            // Create walls
            GameObject wall1 = CreateWall("Test7_Wall1", new Vector3(-1f, 1.5f, 0f), new Vector3(1f, 2f, 1f));
            GameObject wall2 = CreateWall("Test7_Wall2", new Vector3(8f, 3.5f, 0f), new Vector3(1f, 2f, 1f));
            
            // Create multiple enemies
            CreateEnemy("Test7_Enemy1", new Vector3(-7f, 1f, 0f));
            CreateEnemy("Test7_Enemy2", new Vector3(3f, 3f, 0f));
            CreateEnemy("Test7_Enemy3", new Vector3(-3f, -1f, 0f));
            CreateEnemy("Test7_Enemy4", new Vector3(7f, 3f, 0f));
            
            Debug.Log("[Test 7] Multiple Enemies - All should behave correctly in complex environment");
            Debug.Log("Expected: All enemies patrol their platforms, respect walls and edges");
        }

        /// <summary>
        /// Creates Test Scenario 8: Edge Case Configurations
        /// Expected: Handle unusual platform configurations correctly
        /// </summary>
        [ContextMenu("Create Test 8: Edge Cases")]
        public void CreateEdgeCasesTest()
        {
            // Angled platform simulation (multiple segments)
            CreatePlatform("Test8_Slope1", new Vector3(-3f, 0f, 0f), new Vector3(2f, 1f, 1f));
            CreatePlatform("Test8_Slope2", new Vector3(-1f, 0.5f, 0f), new Vector3(2f, 1f, 1f));
            CreatePlatform("Test8_Slope3", new Vector3(1f, 1f, 0f), new Vector3(2f, 1f, 1f));
            
            // Narrow platform
            CreatePlatform("Test8_Narrow", new Vector3(5f, 0f, 0f), new Vector3(1f, 1f, 1f));
            
            // Very low wall
            CreateWall("Test8_LowWall", new Vector3(6f, 1.2f, 0f), new Vector3(0.5f, 0.5f, 1f));
            
            // Create enemies
            CreateEnemy("Test8_Enemy1", new Vector3(-3f, 1f, 0f)); // On slope
            CreateEnemy("Test8_Enemy2", new Vector3(5f, 1f, 0f));  // On narrow platform
            
            Debug.Log("[Test 8] Edge Cases - Handle unusual configurations");
            Debug.Log("Expected: Enemies handle slopes, narrow platforms, and low walls correctly");
        }

        #endregion

        #region Object Creation Helpers

        private GameObject CreatePlatform(string name, Vector3 position, Vector3 scale)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.position = position;
            platform.transform.localScale = scale;
            platform.layer = 6; // Ground layer
            
            // Replace 3D collider with 2D
            DestroyImmediate(platform.GetComponent<BoxCollider>());
            platform.AddComponent<BoxCollider2D>();
            
            // Apply material if available
            if (platformMaterial != null)
            {
                platform.GetComponent<Renderer>().material = platformMaterial;
            }
            else
            {
                // Default green color for platforms
                platform.GetComponent<Renderer>().material.color = Color.green;
            }
            
            return platform;
        }

        private GameObject CreateWall(string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.layer = 6; // Ground layer
            
            // Replace 3D collider with 2D
            DestroyImmediate(wall.GetComponent<BoxCollider>());
            wall.AddComponent<BoxCollider2D>();
            
            // Apply material if available
            if (wallMaterial != null)
            {
                wall.GetComponent<Renderer>().material = wallMaterial;
            }
            else
            {
                // Default red color for walls
                wall.GetComponent<Renderer>().material.color = Color.red;
            }
            
            return wall;
        }

        private GameObject CreateEnemy(string name, Vector3 position)
        {
            // Create enemy GameObject
            GameObject enemy = new GameObject(name);
            enemy.transform.position = position;
            enemy.layer = 7; // Enemy layer
            
            // Add required components
            enemy.AddComponent<Rigidbody2D>();
            enemy.AddComponent<BoxCollider2D>();
            
            // Add sprite renderer with simple sprite
            SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSimpleSprite(Color.blue);
            sr.sortingOrder = 1;
            
            // Add SimpleEnemy component
            SimpleEnemy simpleEnemy = enemy.AddComponent<SimpleEnemy>();
            
            // Configure Rigidbody2D
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.mass = 10f;
            
            // Configure Collider2D
            BoxCollider2D col = enemy.GetComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
            
            Debug.Log($"[Created] Enemy: {name} at position {position}");
            return enemy;
        }

        private GameObject CreatePlayer(string name, Vector3 position)
        {
            GameObject player = new GameObject(name);
            player.transform.position = position;
            player.layer = 0; // Player layer
            
            // Add collider for detection
            player.AddComponent<BoxCollider2D>();
            
            // Add sprite renderer
            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSimpleSprite(Color.yellow);
            sr.sortingOrder = 1;
            
            Debug.Log($"[Created] Player: {name} at position {position} (for chase testing)");
            return player;
        }

        private Sprite CreateSimpleSprite(Color color)
        {
            // Create simple colored texture
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            // Create sprite from texture
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }

        #endregion

        #region Cleanup and Utilities

        [ContextMenu("Clean Up All Test Objects")]
        public void CleanUpTestObjects()
        {
            // Find and destroy all test objects
            string[] testPrefixes = { "Test1_", "Test2_", "Test3_", "Test4_", "Test5_", "Test6_", "Test7_", "Test8_" };
            
            foreach (string prefix in testPrefixes)
            {
                GameObject[] objects = GameObject.FindObjectsOfType<GameObject>();
                foreach (GameObject obj in objects)
                {
                    if (obj.name.StartsWith(prefix))
                    {
                        DestroyImmediate(obj);
                    }
                }
            }
            
            Debug.Log("[Cleanup] All test objects removed");
        }

        [ContextMenu("Show Test Instructions")]
        public void ShowTestInstructions()
        {
            Debug.Log("=== EDGE DETECTION MANUAL TEST INSTRUCTIONS ===");
            Debug.Log("1. Right-click this component and select test scenarios from context menu");
            Debug.Log("2. Enter Play mode to observe enemy behavior");
            Debug.Log("3. Watch for proper edge detection with gizmo visualization");
            Debug.Log("4. Enemy should show blue (down) and magenta/yellow (up) raycasts");
            Debug.Log("5. Verify enemies stop at edges and corners correctly");
            Debug.Log("6. Use 'Clean Up All Test Objects' when done testing");
            Debug.Log("=================================================");
        }

        #endregion

        #region Gizmo Visualization

        private void OnDrawGizmos()
        {
            if (!enableGizmoVisualization || !Application.isPlaying) return;
            
            // Draw test scenario labels
            DrawTestLabels();
        }

        private void DrawTestLabels()
        {
            // This would draw UI labels for each test scenario
            // In a full implementation, you might use GUI.Label or Text components
        }

        #endregion
    }

    /// <summary>
    /// Instructions for using manual test scenarios:
    /// 
    /// 1. Add this script to an empty GameObject in your test scene
    /// 2. Right-click the script component in Inspector
    /// 3. Select test scenarios from the context menu
    /// 4. Enter Play mode to observe enemy behavior
    /// 5. Watch gizmo visualization to see raycast behavior
    /// 6. Verify that enemies stop at edges and corners correctly
    /// 7. Test both patrol and chase behaviors
    /// 8. Use "Clean Up All Test Objects" when done
    /// 
    /// Expected Behaviors:
    /// - Blue gizmo rays: Downward edge detection (existing)
    /// - Magenta gizmo rays: Upward edge detection (new) - patrol mode
    /// - Yellow gizmo rays: Upward edge detection (new) - chase mode
    /// - Enemies should stop at both open edges and wall-blocked corners
    /// - No false positives during normal platform walking
    /// - Proper chase behavior that respects wall boundaries
    /// </summary>
}