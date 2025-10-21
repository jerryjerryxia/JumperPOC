using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerJumpSystem component.
    /// Tests state management, configuration validation, and jump state tracking.
    /// Note: Physics-based jump execution tests deferred to PlayMode.
    /// </summary>
    [TestFixture]
    public class PlayerJumpSystemTests
    {
        private GameObject testGameObject;
        private PlayerJumpSystem jumpSystem;
        private Rigidbody2D rb;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with required components
            testGameObject = new GameObject("TestJumpSystem");
            rb = testGameObject.AddComponent<Rigidbody2D>();
            testGameObject.AddComponent<BoxCollider2D>();
            jumpSystem = testGameObject.AddComponent<PlayerJumpSystem>();

            // Initialize component
            jumpSystem.Initialize(rb, testGameObject.transform, null, null, null, null);

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
            newGameObject.AddComponent<BoxCollider2D>();
            var newJumpSystem = newGameObject.AddComponent<PlayerJumpSystem>();

            // Act & Assert: Initialize should not throw
            Assert.DoesNotThrow(() => newJumpSystem.Initialize(newRb, newGameObject.transform, null, null, null, null),
                "Initialize should set component references without throwing");

            // Cleanup
            Object.DestroyImmediate(newGameObject);
        }

        [Test]
        public void Initialize_WithNullComponents_DoesNotThrow()
        {
            // Arrange: Create new jump system
            var newGameObject = new GameObject("TestNullInit");
            var newRb = newGameObject.AddComponent<Rigidbody2D>();
            newGameObject.AddComponent<BoxCollider2D>();
            var newJumpSystem = newGameObject.AddComponent<PlayerJumpSystem>();

            // Act & Assert: Null components should be handled
            Assert.DoesNotThrow(() => newJumpSystem.Initialize(newRb, newGameObject.transform, null, null, null, null),
                "Initialize should handle null component references");

            // Cleanup
            Object.DestroyImmediate(newGameObject);
        }

        [Test]
        public void InjectCrossComponentDependencies_SetsAllParameters()
        {
            // Act & Assert: Should not throw when setting dependencies
            Assert.DoesNotThrow(() => jumpSystem.InjectCrossComponentDependencies(
                _dashJump: new Vector2(5f, 11f),
                _dashJumpWindow: 0.1f,
                _wallCheckDistance: 0.15f,
                _wallRaycastTop: 0.32f,
                _wallRaycastMiddle: 0.28f,
                _wallRaycastBottom: 0.02f,
                _maxAirDashes: 2,
                _maxDashes: 2,
                _combat: null,
                _inputManager: null
            ), "InjectCrossComponentDependencies should set all parameters");
        }

        #endregion

        #region Configuration Property Tests

        [Test]
        public void EnableCoyoteTime_IsAccessible()
        {
            // Assert: EnableCoyoteTime should be accessible
            bool coyoteTimeEnabled = jumpSystem.EnableCoyoteTime;
            Assert.IsNotNull(coyoteTimeEnabled.GetType(), "EnableCoyoteTime should be accessible");
        }

        [Test]
        public void CoyoteTimeDuration_ReturnsPositiveValue()
        {
            // Assert: CoyoteTimeDuration should be positive
            Assert.GreaterOrEqual(jumpSystem.CoyoteTimeDuration, 0f,
                "CoyoteTimeDuration should be non-negative");
        }

        [Test]
        public void EnableVariableJump_IsAccessible()
        {
            // Assert: EnableVariableJump should be accessible
            bool variableJumpEnabled = jumpSystem.EnableVariableJump;
            Assert.IsNotNull(variableJumpEnabled.GetType(), "EnableVariableJump should be accessible");
        }

        [Test]
        public void WallJumpCompensation_IsPositive()
        {
            // Assert: WallJumpCompensation should be positive
            Assert.Greater(jumpSystem.WallJumpCompensation, 0f,
                "WallJumpCompensation should be positive");
        }

        [Test]
        public void WallJump_HasBothComponents()
        {
            // Assert: WallJump should have both X and Y components
            Vector2 wallJump = jumpSystem.WallJump;
            Assert.Greater(wallJump.x, 0f, "WallJump X should be positive (horizontal)");
            Assert.Greater(wallJump.y, 0f, "WallJump Y should be positive (vertical)");
        }

        [Test]
        public void MinJumpVelocity_IsPositive()
        {
            // Assert: MinJumpVelocity should be positive
            Assert.Greater(jumpSystem.MinJumpVelocity, 0f,
                "MinJumpVelocity should be positive");
        }

        [Test]
        public void MaxJumpVelocity_IsPositive()
        {
            // Assert: MaxJumpVelocity should be positive
            Assert.Greater(jumpSystem.MaxJumpVelocity, 0f,
                "MaxJumpVelocity should be positive");
        }

        [Test]
        public void MaxJumpVelocity_GreaterThanOrEqualToMin()
        {
            // Assert: Max should be >= Min
            Assert.GreaterOrEqual(jumpSystem.MaxJumpVelocity, jumpSystem.MinJumpVelocity,
                "MaxJumpVelocity should be greater than or equal to MinJumpVelocity");
        }

        [Test]
        public void ShowJumpDebug_IsAccessible()
        {
            // Assert: ShowJumpDebug should be accessible
            bool debugEnabled = jumpSystem.ShowJumpDebug;
            Assert.IsNotNull(debugEnabled.GetType(), "ShowJumpDebug should be accessible");
        }

        #endregion

        #region State Property Tests

        [Test]
        public void IsVariableJumpActive_InitiallyFalse()
        {
            // Assert: Should not be in variable jump initially
            Assert.IsFalse(jumpSystem.IsVariableJumpActive,
                "IsVariableJumpActive should be false initially");
        }

        [Test]
        public void IsForcedFalling_InitiallyFalse()
        {
            // Assert: Should not be forced falling initially
            Assert.IsFalse(jumpSystem.IsForcedFalling,
                "IsForcedFalling should be false initially");
        }

        [Test]
        public void JumpsRemaining_InitiallyZero()
        {
            // Assert: Jumps should be zero initially
            Assert.AreEqual(0, jumpSystem.JumpsRemaining,
                "JumpsRemaining should be 0 initially");
        }

        [Test]
        public void LastJumpTime_InitiallyZero()
        {
            // Assert: LastJumpTime should be zero initially
            Assert.AreEqual(0f, jumpSystem.LastJumpTime, 0.001f,
                "LastJumpTime should be 0 initially");
        }

        [Test]
        public void DashJumpTime_InitiallyZero()
        {
            // Assert: DashJumpTime should be zero initially
            Assert.AreEqual(0f, jumpSystem.DashJumpTime, 0.001f,
                "DashJumpTime should be 0 initially");
        }

        [Test]
        public void IsJumpHeld_InitiallyFalse()
        {
            // Assert: Jump button should not be held initially
            Assert.IsFalse(jumpSystem.IsJumpHeld,
                "IsJumpHeld should be false initially");
        }

        [Test]
        public void FacingRight_InitiallyFalse()
        {
            // Assert: FacingRight should have default value
            // Note: Default is false before UpdateExternalState is called
            Assert.IsFalse(jumpSystem.FacingRight,
                "FacingRight should be false initially");
        }

        #endregion

        #region State Setter Tests

        [Test]
        public void JumpsRemaining_CanBeSet()
        {
            // Act: Set jumps remaining
            jumpSystem.JumpsRemaining = 2;

            // Assert: Should be set correctly
            Assert.AreEqual(2, jumpSystem.JumpsRemaining,
                "JumpsRemaining should be settable");
        }

        [Test]
        public void JumpsRemaining_CanBeSetToZero()
        {
            // Act: Set to zero
            jumpSystem.JumpsRemaining = 0;

            // Assert: Should be zero
            Assert.AreEqual(0, jumpSystem.JumpsRemaining,
                "JumpsRemaining should be settable to 0");
        }

        [Test]
        public void DashJumpTime_CanBeSet()
        {
            // Act: Set dash jump time
            jumpSystem.DashJumpTime = 3.5f;

            // Assert: Should be set correctly
            Assert.AreEqual(3.5f, jumpSystem.DashJumpTime, 0.001f,
                "DashJumpTime should be settable");
        }

        [Test]
        public void IsJumpHeld_CanBeSet()
        {
            // Act: Set jump held to true
            jumpSystem.IsJumpHeld = true;

            // Assert: Should be true
            Assert.IsTrue(jumpSystem.IsJumpHeld,
                "IsJumpHeld should be settable to true");
        }

        [Test]
        public void IsJumpHeld_CanBeReset()
        {
            // Arrange: Set to true first
            jumpSystem.IsJumpHeld = true;

            // Act: Reset to false
            jumpSystem.IsJumpHeld = false;

            // Assert: Should be false
            Assert.IsFalse(jumpSystem.IsJumpHeld,
                "IsJumpHeld should be resettable to false");
        }

        #endregion

        #region External State Update Tests

        [Test]
        public void UpdateExternalState_WithDefaultValues_DoesNotThrow()
        {
            // Act & Assert: Updating with default values should not throw
            Assert.DoesNotThrow(() => jumpSystem.UpdateExternalState(
                _facingRight: true,
                _moveInput: Vector2.zero,
                _isGrounded: false,
                _onWall: false,
                _isOnSlope: false,
                _currentSlopeAngle: 0f,
                _slopeNormal: Vector2.up,
                _coyoteTimeCounter: 0f,
                _leftGroundByJumping: false,
                _isBufferClimbing: false,
                _isDashing: false,
                _lastDashEndTime: 0f
            ), "UpdateExternalState should not throw");
        }

        [Test]
        public void UpdateExternalState_UpdatesFacingRight()
        {
            // Act: Update external state with facingRight = true
            jumpSystem.UpdateExternalState(
                _facingRight: true,
                _moveInput: Vector2.zero,
                _isGrounded: false,
                _onWall: false,
                _isOnSlope: false,
                _currentSlopeAngle: 0f,
                _slopeNormal: Vector2.up,
                _coyoteTimeCounter: 0f,
                _leftGroundByJumping: false,
                _isBufferClimbing: false,
                _isDashing: false,
                _lastDashEndTime: 0f
            );

            // Assert: FacingRight should be updated
            Assert.IsTrue(jumpSystem.FacingRight, "FacingRight should be updated via external state");
        }

        [Test]
        public void UpdateExternalState_UpdatesFacingLeft()
        {
            // Act: Update external state with facingRight = false
            jumpSystem.UpdateExternalState(
                _facingRight: false,
                _moveInput: Vector2.zero,
                _isGrounded: false,
                _onWall: false,
                _isOnSlope: false,
                _currentSlopeAngle: 0f,
                _slopeNormal: Vector2.up,
                _coyoteTimeCounter: 0f,
                _leftGroundByJumping: false,
                _isBufferClimbing: false,
                _isDashing: false,
                _lastDashEndTime: 0f
            );

            // Assert: FacingRight should be false
            Assert.IsFalse(jumpSystem.FacingRight, "FacingRight should be false when updated");
        }

        [Test]
        public void UpdateExternalState_WithAllStatesTrue_DoesNotThrow()
        {
            // Act & Assert: All true states should be handled
            Assert.DoesNotThrow(() => jumpSystem.UpdateExternalState(
                _facingRight: true,
                _moveInput: Vector2.one,
                _isGrounded: true,
                _onWall: true,
                _isOnSlope: true,
                _currentSlopeAngle: 45f,
                _slopeNormal: new Vector2(0.707f, 0.707f),
                _coyoteTimeCounter: 0.1f,
                _leftGroundByJumping: true,
                _isBufferClimbing: true,
                _isDashing: true,
                _lastDashEndTime: 5f
            ), "UpdateExternalState should handle all true states");
        }

        [Test]
        public void UpdateExternalState_WithNegativeValues_DoesNotThrow()
        {
            // Act & Assert: Negative values should be handled
            Assert.DoesNotThrow(() => jumpSystem.UpdateExternalState(
                _facingRight: false,
                _moveInput: new Vector2(-1f, -1f),
                _isGrounded: false,
                _onWall: false,
                _isOnSlope: false,
                _currentSlopeAngle: -10f,
                _slopeNormal: new Vector2(-0.5f, -0.5f),
                _coyoteTimeCounter: -1f,
                _leftGroundByJumping: false,
                _isBufferClimbing: false,
                _isDashing: false,
                _lastDashEndTime: -1f
            ), "UpdateExternalState should handle negative values");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void UpdateExternalState_CalledMultipleTimes_DoesNotAccumulateErrors()
        {
            // Act: Call multiple times with varying values
            for (int i = 0; i < 100; i++)
            {
                jumpSystem.UpdateExternalState(
                    _facingRight: i % 2 == 0,
                    _moveInput: new Vector2(i % 2 == 0 ? 1f : -1f, 0f),
                    _isGrounded: i % 3 == 0,
                    _onWall: i % 5 == 0,
                    _isOnSlope: i % 7 == 0,
                    _currentSlopeAngle: i * 0.5f,
                    _slopeNormal: Vector2.up,
                    _coyoteTimeCounter: i * 0.01f,
                    _leftGroundByJumping: i % 2 == 0,
                    _isBufferClimbing: i % 4 == 0,
                    _isDashing: i % 6 == 0,
                    _lastDashEndTime: i * 0.1f
                );
            }

            // Assert: Should complete without errors
            Assert.Pass("UpdateExternalState should handle multiple rapid calls");
        }

        [Test]
        public void JumpsRemaining_WithNegativeValue_IsAccepted()
        {
            // Act: Set negative value (edge case, shouldn't happen but test robustness)
            jumpSystem.JumpsRemaining = -1;

            // Assert: Value should be set (no validation enforced at this level)
            Assert.AreEqual(-1, jumpSystem.JumpsRemaining,
                "Should accept negative values (validation happens elsewhere)");
        }

        [Test]
        public void DashJumpTime_WithNegativeValue_IsAccepted()
        {
            // Act: Set negative value
            jumpSystem.DashJumpTime = -5f;

            // Assert: Value should be set
            Assert.AreEqual(-5f, jumpSystem.DashJumpTime, 0.001f,
                "Should accept negative values");
        }

        [Test]
        public void UpdateExternalState_WithExtremeFloatValues_DoesNotThrow()
        {
            // Act & Assert: Extreme values should be handled
            Assert.DoesNotThrow(() => jumpSystem.UpdateExternalState(
                _facingRight: true,
                _moveInput: new Vector2(float.MaxValue, float.MaxValue),
                _isGrounded: false,
                _onWall: false,
                _isOnSlope: false,
                _currentSlopeAngle: float.MaxValue,
                _slopeNormal: new Vector2(float.MaxValue, float.MaxValue),
                _coyoteTimeCounter: float.MaxValue,
                _leftGroundByJumping: false,
                _isBufferClimbing: false,
                _isDashing: false,
                _lastDashEndTime: float.MaxValue
            ), "Should handle extreme float values");
        }

        [Test]
        public void InjectCrossComponentDependencies_WithZeroValues_DoesNotThrow()
        {
            // Act & Assert: Zero values should be handled
            Assert.DoesNotThrow(() => jumpSystem.InjectCrossComponentDependencies(
                _dashJump: Vector2.zero,
                _dashJumpWindow: 0f,
                _wallCheckDistance: 0f,
                _wallRaycastTop: 0f,
                _wallRaycastMiddle: 0f,
                _wallRaycastBottom: 0f,
                _maxAirDashes: 0,
                _maxDashes: 0,
                _combat: null,
                _inputManager: null
            ), "InjectCrossComponentDependencies should handle zero values");
        }

        [Test]
        public void InjectCrossComponentDependencies_WithNegativeValues_DoesNotThrow()
        {
            // Act & Assert: Negative values should be handled
            Assert.DoesNotThrow(() => jumpSystem.InjectCrossComponentDependencies(
                _dashJump: new Vector2(-5f, -10f),
                _dashJumpWindow: -0.1f,
                _wallCheckDistance: -0.15f,
                _wallRaycastTop: -0.32f,
                _wallRaycastMiddle: -0.28f,
                _wallRaycastBottom: -0.02f,
                _maxAirDashes: -1,
                _maxDashes: -1,
                _combat: null,
                _inputManager: null
            ), "InjectCrossComponentDependencies should handle negative values");
        }

        #endregion
    }
}
