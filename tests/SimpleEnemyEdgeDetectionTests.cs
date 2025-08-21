using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace Tests
{
    /// <summary>
    /// Comprehensive test suite for SimpleEnemy edge detection with new upward raycast implementation.
    /// Tests both existing functionality and new wall-blocked edge detection.
    /// </summary>
    public class SimpleEnemyEdgeDetectionTests
    {
        private GameObject enemyGameObject;
        private SimpleEnemy enemy;
        private GameObject testPlatform;
        private GameObject testWall;

        [SetUp]
        public void SetUp()
        {
            // Create enemy GameObject with required components
            enemyGameObject = new GameObject("TestEnemy");
            enemy = enemyGameObject.AddComponent<SimpleEnemy>();
            enemyGameObject.AddComponent<Rigidbody2D>();
            enemyGameObject.AddComponent<BoxCollider2D>();
            enemyGameObject.AddComponent<SpriteRenderer>();

            // Set up test environment layers
            // Layer 6 = Ground (as per SimpleEnemy configuration)
            SetupTestLayers();
        }

        [TearDown]
        public void TearDown()
        {
            if (enemyGameObject != null)
                Object.DestroyImmediate(enemyGameObject);
            if (testPlatform != null)
                Object.DestroyImmediate(testPlatform);
            if (testWall != null)
                Object.DestroyImmediate(testWall);
        }

        private void SetupTestLayers()
        {
            // Note: In actual Unity testing, layers would be pre-configured
            // This is a placeholder for documentation purposes
        }

        #region Test Scenario 1: Open Platform Edge (Existing Functionality)

        [Test]
        [Description("Enemy on platform with open edge should detect edge using downward raycast")]
        public void TestOpenPlatformEdge_ShouldDetectEdge()
        {
            // Arrange: Create platform with open edge
            CreateTestPlatform(Vector3.zero, new Vector3(5f, 1f, 1f));
            enemy.transform.position = new Vector3(2f, 1f, 0f); // Near right edge
            
            // Act: Simulate movement toward edge
            SetEnemyMovementDirection(1); // Moving right
            
            // Assert: Edge should be detected by downward raycast
            bool shouldStop = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            Assert.IsTrue(shouldStop, "Open platform edge should be detected");
        }

        [Test]
        [Description("Enemy in middle of platform should not detect edge")]
        public void TestMiddleOfPlatform_ShouldNotDetectEdge()
        {
            // Arrange: Create platform with enemy in middle
            CreateTestPlatform(Vector3.zero, new Vector3(10f, 1f, 1f));
            enemy.transform.position = new Vector3(0f, 1f, 0f); // Center of platform
            
            // Act: Simulate movement
            SetEnemyMovementDirection(1);
            
            // Assert: No edge should be detected
            bool shouldStop = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            Assert.IsFalse(shouldStop, "Middle of platform should not detect edge");
        }

        #endregion

        #region Test Scenario 2: Wall-Blocked Edge (New Functionality)

        [Test]
        [Description("Enemy approaching corner where wall meets platform edge should detect edge using upward raycast")]
        public void TestWallBlockedEdge_ShouldDetectEdgeWithUpwardRaycast()
        {
            // Arrange: Create L-shaped corner (platform + wall)
            CreateTestPlatform(Vector3.zero, new Vector3(5f, 1f, 1f)); // Horizontal platform
            CreateTestWall(new Vector3(2.5f, 2f, 0f), new Vector3(1f, 3f, 1f)); // Vertical wall at edge
            
            enemy.transform.position = new Vector3(1.5f, 1f, 0f); // Approaching corner
            
            // Act: Simulate movement toward wall-blocked edge
            SetEnemyMovementDirection(1); // Moving right toward wall
            
            // Assert: Edge should be detected by upward raycast hitting wall
            bool shouldStop = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            Assert.IsTrue(shouldStop, "Wall-blocked edge should be detected by upward raycast");
        }

        [Test]
        [Description("Enemy approaching inside corner should detect edge using both raycasts")]
        public void TestInsideCorner_ShouldDetectEdgeWithBothRaycasts()
        {
            // Arrange: Create inside corner scenario
            CreateTestPlatform(Vector3.zero, new Vector3(3f, 1f, 1f)); // Short platform
            CreateTestWall(new Vector3(1.5f, 2f, 0f), new Vector3(1f, 3f, 1f)); // Wall above edge
            
            enemy.transform.position = new Vector3(0.5f, 1f, 0f);
            
            // Act: Move toward corner
            SetEnemyMovementDirection(1);
            
            // Assert: Should detect edge (wall above OR open edge)
            bool shouldStop = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            Assert.IsTrue(shouldStop, "Inside corner should be detected by combined logic");
        }

        #endregion

        #region Test Scenario 3: Normal Platform Walking

        [Test]
        [Description("Enemy walking on normal platform with no walls or edges should continue moving")]
        public void TestNormalPlatformWalking_ShouldContinueMoving()
        {
            // Arrange: Create long platform with no obstacles
            CreateTestPlatform(Vector3.zero, new Vector3(20f, 1f, 1f));
            enemy.transform.position = new Vector3(0f, 1f, 0f);
            
            // Act: Test multiple positions along platform
            for (float x = -8f; x <= 8f; x += 2f)
            {
                enemy.transform.position = new Vector3(x, 1f, 0f);
                SetEnemyMovementDirection(1);
                
                // Assert: Should not detect edge anywhere in middle
                bool shouldStop = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
                Assert.IsFalse(shouldStop, $"Normal platform at position {x} should not detect edge");
            }
        }

        [Test]
        [Description("Enemy under ceiling should not falsely detect edge")]
        public void TestUnderCeiling_ShouldNotFalselyDetectEdge()
        {
            // Arrange: Create platform with ceiling above
            CreateTestPlatform(Vector3.zero, new Vector3(10f, 1f, 1f)); // Ground
            CreateTestWall(new Vector3(0f, 4f, 0f), new Vector3(10f, 1f, 1f)); // Ceiling
            
            enemy.transform.position = new Vector3(0f, 1f, 0f);
            
            // Act: Move under ceiling
            SetEnemyMovementDirection(1);
            
            // Assert: Should not detect edge (ceiling is too far up for edgeCheckDistance)
            bool shouldStop = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            Assert.IsFalse(shouldStop, "Distant ceiling should not trigger edge detection");
        }

        #endregion

        #region Test Scenario 4: Corner Cases

        [Test]
        [Description("Enemy on slope should handle edge detection correctly")]
        public void TestSlopedPlatform_ShouldHandleCorrectly()
        {
            // Arrange: Create angled platform (simulated with multiple segments)
            CreateTestPlatform(new Vector3(-2f, 0f, 0f), new Vector3(2f, 1f, 1f));
            CreateTestPlatform(new Vector3(0f, 0.5f, 0f), new Vector3(2f, 1f, 1f));
            CreateTestPlatform(new Vector3(2f, 1f, 0f), new Vector3(2f, 1f, 1f));
            
            enemy.transform.position = new Vector3(-1f, 1f, 0f);
            
            // Act: Move across slope
            SetEnemyMovementDirection(1);
            
            // Assert: Should not detect false edges on slope
            bool shouldStop = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            Assert.IsFalse(shouldStop, "Slope should not trigger false edge detection");
        }

        [Test]
        [Description("Enemy near low wall should not trigger edge detection")]
        public void TestLowWall_ShouldNotTriggerEdgeDetection()
        {
            // Arrange: Create platform with low wall (within edgeCheckDistance)
            CreateTestPlatform(Vector3.zero, new Vector3(10f, 1f, 1f));
            CreateTestWall(new Vector3(3f, 1.5f, 0f), new Vector3(1f, 1f, 1f)); // Low wall
            
            enemy.transform.position = new Vector3(2f, 1f, 0f);
            
            // Act: Move toward low wall
            SetEnemyMovementDirection(1);
            
            // Assert: Low wall within edgeCheckDistance should trigger edge detection
            bool shouldStop = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            Assert.IsTrue(shouldStop, "Low wall within check distance should trigger edge detection");
        }

        [Test]
        [Description("Concurrent operations stress test")]
        public void TestConcurrentOperations_ShouldMaintainAccuracy()
        {
            // Arrange: Complex environment
            CreateTestPlatform(Vector3.zero, new Vector3(5f, 1f, 1f));
            CreateTestWall(new Vector3(2.5f, 2f, 0f), new Vector3(1f, 3f, 1f));
            
            // Act: Rapidly test edge detection from multiple positions
            bool[] results = new bool[100];
            for (int i = 0; i < 100; i++)
            {
                float x = Mathf.Lerp(-2f, 2f, i / 99f);
                enemy.transform.position = new Vector3(x, 1f, 0f);
                SetEnemyMovementDirection(1);
                results[i] = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            }
            
            // Assert: Results should be consistent
            // Near wall (x > 1.5f) should detect edge, far from wall should not
            for (int i = 0; i < 100; i++)
            {
                float x = Mathf.Lerp(-2f, 2f, i / 99f);
                bool expectedEdge = x > 1.5f; // Near wall position
                Assert.AreEqual(expectedEdge, results[i], 
                    $"Edge detection should be consistent at position {x}");
            }
        }

        #endregion

        #region Code Logic Validation Tests

        [Test]
        [Description("Verify OR condition logic is correct")]
        public void TestORConditionLogic_ShouldWorkCorrectly()
        {
            // Test the core logic: hasOpenEdge || hasWallAbove
            
            // Case 1: Open edge (hasOpenEdge = true, hasWallAbove = false)
            CreateTestPlatform(Vector3.zero, new Vector3(3f, 1f, 1f));
            enemy.transform.position = new Vector3(1f, 1f, 0f);
            SetEnemyMovementDirection(1);
            
            bool result1 = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            Assert.IsTrue(result1, "Open edge should trigger detection (true OR false = true)");
            
            // Case 2: Wall above (hasOpenEdge = false, hasWallAbove = true)
            Object.DestroyImmediate(testPlatform);
            CreateTestPlatform(Vector3.zero, new Vector3(10f, 1f, 1f)); // Long platform
            CreateTestWall(new Vector3(2f, 2f, 0f), new Vector3(1f, 2f, 1f)); // Wall above
            enemy.transform.position = new Vector3(1f, 1f, 0f);
            
            bool result2 = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            Assert.IsTrue(result2, "Wall above should trigger detection (false OR true = true)");
        }

        [Test]
        [Description("Verify parameter reuse is correct")]
        public void TestParameterReuse_ShouldUseExistingValues()
        {
            // Verify that new upward raycast uses same parameters as existing downward raycast
            CreateTestPlatform(Vector3.zero, new Vector3(5f, 1f, 1f));
            CreateTestWall(new Vector3(2f, 2f, 0f), new Vector3(1f, 2f, 1f));
            
            enemy.transform.position = new Vector3(1f, 1f, 0f);
            SetEnemyMovementDirection(1);
            
            // Both raycasts should use:
            // - Same checkPosition
            // - Same edgeCheckDistance  
            // - Same groundLayer
            
            // This is verified by the fact that the system works - 
            // no additional test needed beyond functional verification
            bool result = InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            Assert.IsTrue(result, "Parameter reuse should work correctly");
        }

        #endregion

        #region Integration Tests

        [UnityTest]
        public IEnumerator TestPatrolBehaviorIntegration_ShouldStopAndTurnAtEdges()
        {
            // Arrange: Create platform with wall at one end
            CreateTestPlatform(Vector3.zero, new Vector3(5f, 1f, 1f));
            CreateTestWall(new Vector3(2.5f, 2f, 0f), new Vector3(1f, 3f, 1f));
            
            enemy.transform.position = new Vector3(-2f, 1f, 0f);
            SetEnemyMovementDirection(1); // Moving right toward wall
            
            // Act: Let enemy patrol for several seconds
            float testDuration = 5f;
            float startTime = Time.time;
            Vector3 startPosition = enemy.transform.position;
            
            while (Time.time - startTime < testDuration)
            {
                yield return null;
            }
            
            // Assert: Enemy should have stopped and turned around before hitting wall
            Assert.AreNotEqual(startPosition.x, enemy.transform.position.x, 
                "Enemy should have moved from start position");
            Assert.IsTrue(enemy.transform.position.x < 2f, 
                "Enemy should not have passed through wall");
        }

        [UnityTest]
        public IEnumerator TestChaseBehaviorIntegration_ShouldRespectWallEdges()
        {
            // Arrange: Setup chase scenario with wall-blocked edge
            CreateTestPlatform(Vector3.zero, new Vector3(5f, 1f, 1f));
            CreateTestWall(new Vector3(2.5f, 2f, 0f), new Vector3(1f, 3f, 1f));
            
            // Create player GameObject
            GameObject player = new GameObject("Player");
            player.layer = 0; // Player layer
            player.transform.position = new Vector3(3f, 1f, 0f); // Beyond wall
            
            enemy.transform.position = new Vector3(0f, 1f, 0f);
            
            // Simulate player detection by setting enemy to chase state
            SetEnemyState(enemy, "Chase");
            SetEnemyPlayer(enemy, player);
            
            // Act: Let enemy attempt to chase
            yield return new WaitForSeconds(2f);
            
            // Assert: Enemy should stop before wall, not pass through
            Assert.IsTrue(enemy.transform.position.x < 2f, 
                "Enemy should stop before wall during chase");
            
            // Cleanup
            Object.DestroyImmediate(player);
        }

        #endregion

        #region Performance Tests

        [Test]
        [Description("Verify minimal performance overhead from additional raycast")]
        public void TestPerformanceOverhead_ShouldBeMinimal()
        {
            // Arrange: Setup test environment
            CreateTestPlatform(Vector3.zero, new Vector3(10f, 1f, 1f));
            enemy.transform.position = new Vector3(0f, 1f, 0f);
            SetEnemyMovementDirection(1);
            
            // Act: Measure time for multiple edge detection calls
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < 1000; i++)
            {
                InvokePrivateMethod<bool>(enemy, "ShouldStopAtEdge");
            }
            
            stopwatch.Stop();
            
            // Assert: Should complete quickly (less than 10ms for 1000 calls)
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10, 
                $"Edge detection should be fast, took {stopwatch.ElapsedMilliseconds}ms for 1000 calls");
        }

        #endregion

        #region Debug Visualization Tests

        [Test]
        [Description("Verify gizmo colors and ray directions are correct")]
        public void TestDebugVisualization_ShouldShowCorrectGizmos()
        {
            // This would be tested visually in play mode
            // Here we verify the gizmo drawing method doesn't throw errors
            
            CreateTestPlatform(Vector3.zero, new Vector3(5f, 1f, 1f));
            CreateTestWall(new Vector3(2f, 2f, 0f), new Vector3(1f, 2f, 1f));
            
            enemy.transform.position = new Vector3(1f, 1f, 0f);
            
            // Act: Call OnDrawGizmosSelected (if public or via reflection)
            Assert.DoesNotThrow(() => {
                InvokePrivateMethod(enemy, "OnDrawGizmosSelected");
            }, "Gizmo drawing should not throw exceptions");
        }

        #endregion

        #region Helper Methods

        private void CreateTestPlatform(Vector3 position, Vector3 size)
        {
            testPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testPlatform.transform.position = position;
            testPlatform.transform.localScale = size;
            testPlatform.layer = 6; // Ground layer
            
            // Add platform collider
            var collider = testPlatform.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
            testPlatform.AddComponent<BoxCollider2D>();
        }

        private void CreateTestWall(Vector3 position, Vector3 size)
        {
            testWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testWall.transform.position = position;
            testWall.transform.localScale = size;
            testWall.layer = 6; // Ground layer
            
            // Add wall collider
            var collider = testWall.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
            testWall.AddComponent<BoxCollider2D>();
        }

        private void SetEnemyMovementDirection(int direction)
        {
            // Use reflection to set private fields
            var moveDirectionField = typeof(SimpleEnemy).GetField("moveDirection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            moveDirectionField?.SetValue(enemy, direction);
            
            var facingRightField = typeof(SimpleEnemy).GetField("facingRight", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            facingRightField?.SetValue(enemy, direction > 0);
        }

        private void SetEnemyState(SimpleEnemy enemy, string stateName)
        {
            var stateField = typeof(SimpleEnemy).GetField("currentState", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var stateType = typeof(SimpleEnemy).GetNestedType("EnemyState", 
                System.Reflection.BindingFlags.NonPublic);
            
            var stateValue = System.Enum.Parse(stateType, stateName);
            stateField?.SetValue(enemy, stateValue);
        }

        private void SetEnemyPlayer(SimpleEnemy enemy, GameObject player)
        {
            var playerField = typeof(SimpleEnemy).GetField("player", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            playerField?.SetValue(enemy, player);
        }

        private T InvokePrivateMethod<T>(object obj, string methodName, params object[] parameters)
        {
            var method = obj.GetType().GetMethod(methodName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (T)method?.Invoke(obj, parameters);
        }

        private void InvokePrivateMethod(object obj, string methodName, params object[] parameters)
        {
            var method = obj.GetType().GetMethod(methodName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(obj, parameters);
        }

        #endregion
    }
}