using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerWallDetection component.
    /// Tests state management, configuration validation, and external state updates.
    /// Note: Physics-based wall detection tests (CheckWallDetection raycasts) deferred to PlayMode.
    /// </summary>
    [TestFixture]
    public class PlayerWallDetectionTests
    {
        private GameObject testGameObject;
        private PlayerWallDetection wallDetection;
        private Rigidbody2D rb;
        private Collider2D col;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with required components
            testGameObject = new GameObject("TestWallDetection");
            rb = testGameObject.AddComponent<Rigidbody2D>();
            col = testGameObject.AddComponent<BoxCollider2D>();
            wallDetection = testGameObject.AddComponent<PlayerWallDetection>();

            // Initialize component
            wallDetection.Initialize(rb, col, testGameObject.transform, null);

            // Suppress warnings in tests
            Debug.unityLogger.logEnabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test objects
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }

            Debug.unityLogger.logEnabled = true;
        }

        #region Initialization Tests

        [Test]
        public void Initialize_WithValidComponents_SetsReferences()
        {
            // Arrange: Create new objects
            var newGameObject = new GameObject("TestInit");
            var newRb = newGameObject.AddComponent<Rigidbody2D>();
            var newCol = newGameObject.AddComponent<BoxCollider2D>();
            var newDetection = newGameObject.AddComponent<PlayerWallDetection>();

            // Act & Assert: Initialize should not throw
            Assert.DoesNotThrow(() => newDetection.Initialize(newRb, newCol, newGameObject.transform, null),
                "Initialize should set component references without throwing");

            // Cleanup
            Object.DestroyImmediate(newGameObject);
        }

        [Test]
        public void Initialize_WithNullAbilities_DoesNotThrow()
        {
            // Arrange: Create new detection
            var newGameObject = new GameObject("TestNullAbilities");
            var newRb = newGameObject.AddComponent<Rigidbody2D>();
            var newCol = newGameObject.AddComponent<BoxCollider2D>();
            var newDetection = newGameObject.AddComponent<PlayerWallDetection>();

            // Act & Assert: Null abilities should be handled
            Assert.DoesNotThrow(() => newDetection.Initialize(newRb, newCol, newGameObject.transform, null),
                "Initialize should handle null PlayerAbilities");

            // Cleanup
            Object.DestroyImmediate(newGameObject);
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void SetConfiguration_WithValidValues_DoesNotThrow()
        {
            // Act & Assert: Setting configuration should not throw
            Assert.DoesNotThrow(() => wallDetection.SetConfiguration(
                _wallCheckDistance: 0.15f,
                _wallRaycastTop: 0.32f,
                _wallRaycastMiddle: 0.28f,
                _wallRaycastBottom: 0.02f
            ), "SetConfiguration should not throw with valid values");
        }

        [Test]
        public void SetConfiguration_WithZeroValues_DoesNotThrow()
        {
            // Act & Assert: Zero values should be handled (edge case)
            Assert.DoesNotThrow(() => wallDetection.SetConfiguration(
                _wallCheckDistance: 0f,
                _wallRaycastTop: 0f,
                _wallRaycastMiddle: 0f,
                _wallRaycastBottom: 0f
            ), "SetConfiguration should handle zero values");
        }

        [Test]
        public void SetConfiguration_WithNegativeValues_DoesNotThrow()
        {
            // Act & Assert: Negative values should be handled (edge case)
            Assert.DoesNotThrow(() => wallDetection.SetConfiguration(
                _wallCheckDistance: -0.1f,
                _wallRaycastTop: -0.5f,
                _wallRaycastMiddle: -0.3f,
                _wallRaycastBottom: -0.1f
            ), "SetConfiguration should handle negative values");
        }

        [Test]
        public void SetConfiguration_CanBeCalledMultipleTimes()
        {
            // Act: Call multiple times with different values
            wallDetection.SetConfiguration(0.1f, 0.3f, 0.2f, 0.1f);
            wallDetection.SetConfiguration(0.2f, 0.4f, 0.3f, 0.2f);
            wallDetection.SetConfiguration(0.15f, 0.35f, 0.25f, 0.05f);

            // Assert: Should complete without errors
            Assert.Pass("SetConfiguration should allow multiple updates");
        }

        #endregion

        #region State Property Tests

        [Test]
        public void OnWall_InitiallyFalse()
        {
            // Assert: Should not be on wall initially
            Assert.IsFalse(wallDetection.OnWall, "OnWall should be false initially");
        }

        [Test]
        public void WallStickAllowed_InitiallyFalse()
        {
            // Assert: Wall stick should not be allowed initially
            Assert.IsFalse(wallDetection.WallStickAllowed,
                "WallStickAllowed should be false initially");
        }

        #endregion

        #region External State Update Tests

        [Test]
        public void UpdateExternalState_WithDefaultValues_DoesNotThrow()
        {
            // Act & Assert: Updating with default values should not throw
            Assert.DoesNotThrow(() => wallDetection.UpdateExternalState(
                _facingRight: true,
                _moveInput: Vector2.zero,
                _isGrounded: false,
                _isBufferClimbing: false
            ), "UpdateExternalState should not throw");
        }

        [Test]
        public void UpdateExternalState_FacingRight_DoesNotThrow()
        {
            // Act & Assert: Facing right should be handled
            Assert.DoesNotThrow(() => wallDetection.UpdateExternalState(
                _facingRight: true,
                _moveInput: Vector2.right,
                _isGrounded: false,
                _isBufferClimbing: false
            ), "UpdateExternalState should handle facing right");
        }

        [Test]
        public void UpdateExternalState_FacingLeft_DoesNotThrow()
        {
            // Act & Assert: Facing left should be handled
            Assert.DoesNotThrow(() => wallDetection.UpdateExternalState(
                _facingRight: false,
                _moveInput: Vector2.left,
                _isGrounded: false,
                _isBufferClimbing: false
            ), "UpdateExternalState should handle facing left");
        }

        [Test]
        public void UpdateExternalState_WithPositiveInput_DoesNotThrow()
        {
            // Act & Assert: Positive input should be handled
            Assert.DoesNotThrow(() => wallDetection.UpdateExternalState(
                _facingRight: true,
                _moveInput: new Vector2(1f, 0f),
                _isGrounded: false,
                _isBufferClimbing: false
            ), "UpdateExternalState should handle positive horizontal input");
        }

        [Test]
        public void UpdateExternalState_WithNegativeInput_DoesNotThrow()
        {
            // Act & Assert: Negative input should be handled
            Assert.DoesNotThrow(() => wallDetection.UpdateExternalState(
                _facingRight: false,
                _moveInput: new Vector2(-1f, 0f),
                _isGrounded: false,
                _isBufferClimbing: false
            ), "UpdateExternalState should handle negative horizontal input");
        }

        [Test]
        public void UpdateExternalState_WhenGrounded_DoesNotThrow()
        {
            // Act & Assert: Grounded state should be handled
            Assert.DoesNotThrow(() => wallDetection.UpdateExternalState(
                _facingRight: true,
                _moveInput: Vector2.zero,
                _isGrounded: true,
                _isBufferClimbing: false
            ), "UpdateExternalState should handle grounded state");
        }

        [Test]
        public void UpdateExternalState_WhenBufferClimbing_DoesNotThrow()
        {
            // Act & Assert: Buffer climbing state should be handled
            Assert.DoesNotThrow(() => wallDetection.UpdateExternalState(
                _facingRight: true,
                _moveInput: Vector2.right,
                _isGrounded: false,
                _isBufferClimbing: true
            ), "UpdateExternalState should handle buffer climbing state");
        }

        [Test]
        public void UpdateExternalState_WithAllStatesTrue_DoesNotThrow()
        {
            // Act & Assert: All true states should be handled
            Assert.DoesNotThrow(() => wallDetection.UpdateExternalState(
                _facingRight: true,
                _moveInput: Vector2.one,
                _isGrounded: true,
                _isBufferClimbing: true
            ), "UpdateExternalState should handle all true states");
        }

        [Test]
        public void UpdateExternalState_WithAllStatesFalse_DoesNotThrow()
        {
            // Act & Assert: All false states should be handled
            Assert.DoesNotThrow(() => wallDetection.UpdateExternalState(
                _facingRight: false,
                _moveInput: Vector2.zero,
                _isGrounded: false,
                _isBufferClimbing: false
            ), "UpdateExternalState should handle all false states");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void UpdateExternalState_CalledMultipleTimes_DoesNotAccumulateErrors()
        {
            // Act: Call multiple times with varying values
            for (int i = 0; i < 100; i++)
            {
                wallDetection.UpdateExternalState(
                    _facingRight: i % 2 == 0,
                    _moveInput: new Vector2(i % 2 == 0 ? 1f : -1f, 0f),
                    _isGrounded: i % 3 == 0,
                    _isBufferClimbing: i % 5 == 0
                );
            }

            // Assert: Should complete without errors
            Assert.Pass("UpdateExternalState should handle multiple rapid calls");
        }

        [Test]
        public void SetConfiguration_WithExtremeValues_DoesNotThrow()
        {
            // Act & Assert: Extreme values should be handled
            Assert.DoesNotThrow(() => wallDetection.SetConfiguration(
                _wallCheckDistance: float.MaxValue,
                _wallRaycastTop: float.MaxValue,
                _wallRaycastMiddle: float.MaxValue,
                _wallRaycastBottom: float.MaxValue
            ), "SetConfiguration should handle extreme values");
        }

        [Test]
        public void UpdateExternalState_WithExtremeInput_DoesNotThrow()
        {
            // Act & Assert: Extreme input values should be handled
            Assert.DoesNotThrow(() => wallDetection.UpdateExternalState(
                _facingRight: true,
                _moveInput: new Vector2(float.MaxValue, float.MaxValue),
                _isGrounded: false,
                _isBufferClimbing: false
            ), "UpdateExternalState should handle extreme input values");
        }

        [Test]
        public void SetConfiguration_WithMixedSignValues_DoesNotThrow()
        {
            // Act & Assert: Mixed positive and negative values should be handled
            Assert.DoesNotThrow(() => wallDetection.SetConfiguration(
                _wallCheckDistance: 0.15f,
                _wallRaycastTop: -0.1f,
                _wallRaycastMiddle: 0.2f,
                _wallRaycastBottom: -0.3f
            ), "SetConfiguration should handle mixed sign values");
        }

        #endregion

        #region Configuration Order Tests

        [Test]
        public void SetConfiguration_BeforeUpdateExternalState_DoesNotThrow()
        {
            // Act: Set configuration first, then update state
            wallDetection.SetConfiguration(0.15f, 0.32f, 0.28f, 0.02f);
            wallDetection.UpdateExternalState(true, Vector2.zero, false, false);

            // Assert: Should work in this order
            Assert.Pass("SetConfiguration should work before UpdateExternalState");
        }

        [Test]
        public void UpdateExternalState_BeforeSetConfiguration_DoesNotThrow()
        {
            // Act: Update state first, then set configuration
            wallDetection.UpdateExternalState(true, Vector2.zero, false, false);
            wallDetection.SetConfiguration(0.15f, 0.32f, 0.28f, 0.02f);

            // Assert: Should work in this order
            Assert.Pass("UpdateExternalState should work before SetConfiguration");
        }

        #endregion
    }
}
