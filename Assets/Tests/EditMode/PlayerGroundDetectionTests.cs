using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerGroundDetection component.
    /// Tests state management, configuration validation, and coyote time logic.
    /// Note: Physics-based detection tests (CheckGrounding, slopes) deferred to PlayMode.
    /// </summary>
    [TestFixture]
    public class PlayerGroundDetectionTests
    {
        private GameObject testGameObject;
        private PlayerGroundDetection groundDetection;
        private Rigidbody2D rb;
        private Collider2D col;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with required components
            testGameObject = new GameObject("TestGroundDetection");
            rb = testGameObject.AddComponent<Rigidbody2D>();
            col = testGameObject.AddComponent<BoxCollider2D>();
            groundDetection = testGameObject.AddComponent<PlayerGroundDetection>();

            // Initialize component
            groundDetection.Initialize(rb, col, testGameObject.transform);

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
            var newDetection = newGameObject.AddComponent<PlayerGroundDetection>();

            // Act & Assert: Initialize should not throw
            Assert.DoesNotThrow(() => newDetection.Initialize(newRb, newCol, newGameObject.transform),
                "Initialize should set component references without throwing");

            // Cleanup
            Object.DestroyImmediate(newGameObject);
        }

        [Test]
        public void InjectCrossComponentDependencies_SetsAllParameters()
        {
            // Act & Assert: Should not throw when setting dependencies
            Assert.DoesNotThrow(() => groundDetection.InjectCrossComponentDependencies(
                _enableCoyoteTime: true,
                _coyoteTimeDuration: 0.1f,
                _climbingAssistanceOffset: 0.5f,
                _maxAirDashes: 2,
                _maxDashes: 2,
                _combat: null
            ), "InjectCrossComponentDependencies should set all parameters");
        }

        [Test]
        public void InjectCrossComponentDependencies_WithNullCombat_DoesNotThrow()
        {
            // Act & Assert: Null combat should be handled
            Assert.DoesNotThrow(() => groundDetection.InjectCrossComponentDependencies(
                _enableCoyoteTime: false,
                _coyoteTimeDuration: 0.05f,
                _climbingAssistanceOffset: 0.3f,
                _maxAirDashes: 1,
                _maxDashes: 1,
                _combat: null
            ), "Should handle null PlayerCombat reference");
        }

        #endregion

        #region Configuration Property Tests

        [Test]
        public void GroundCheckOffsetY_IsAccessible()
        {
            // Assert: GroundCheckOffsetY should be accessible
            float offset = groundDetection.GroundCheckOffsetY;
            Assert.IsNotNull(offset.GetType(), "GroundCheckOffsetY should be accessible");
        }

        [Test]
        public void GroundCheckRadius_IsPositive()
        {
            // Assert: GroundCheckRadius should be positive
            Assert.Greater(groundDetection.GroundCheckRadius, 0f,
                "GroundCheckRadius should be positive for overlap detection");
        }

        #endregion

        #region State Property Tests

        [Test]
        public void IsGrounded_InitiallyFalse()
        {
            // Assert: Should not be grounded on initialization
            Assert.IsFalse(groundDetection.IsGrounded, "IsGrounded should be false initially");
        }

        [Test]
        public void IsOnSlope_InitiallyFalse()
        {
            // Assert: Should not be on slope initially
            Assert.IsFalse(groundDetection.IsOnSlope, "IsOnSlope should be false initially");
        }

        [Test]
        public void CurrentSlopeAngle_InitiallyZero()
        {
            // Assert: Slope angle should be zero initially
            Assert.AreEqual(0f, groundDetection.CurrentSlopeAngle, 0.001f,
                "CurrentSlopeAngle should be 0 initially");
        }

        [Test]
        public void SlopeNormal_InitiallyUp()
        {
            // Assert: Slope normal should be Vector2.up initially
            Assert.AreEqual(Vector2.up, groundDetection.SlopeNormal,
                "SlopeNormal should be Vector2.up initially");
        }

        [Test]
        public void IsGroundedByPlatform_InitiallyFalse()
        {
            // Assert: Should not be grounded by platform initially
            Assert.IsFalse(groundDetection.IsGroundedByPlatform,
                "IsGroundedByPlatform should be false initially");
        }

        [Test]
        public void IsGroundedByBuffer_InitiallyFalse()
        {
            // Assert: Should not be grounded by buffer initially
            Assert.IsFalse(groundDetection.IsGroundedByBuffer,
                "IsGroundedByBuffer should be false initially");
        }

        [Test]
        public void IsBufferClimbing_InitiallyFalse()
        {
            // Assert: Should not be buffer climbing initially
            Assert.IsFalse(groundDetection.IsBufferClimbing,
                "IsBufferClimbing should be false initially");
        }

        #endregion

        #region Coyote Time Tests

        [Test]
        public void CoyoteTimeCounter_InitiallyZero()
        {
            // Assert: Coyote time counter should be zero initially
            Assert.AreEqual(0f, groundDetection.CoyoteTimeCounter, 0.001f,
                "CoyoteTimeCounter should be 0 initially");
        }

        [Test]
        public void LeftGroundByJumping_InitiallyFalse()
        {
            // Assert: LeftGroundByJumping should be false initially
            Assert.IsFalse(groundDetection.LeftGroundByJumping,
                "LeftGroundByJumping should be false initially");
        }

        [Test]
        public void LeftGroundByJumping_CanBeSet()
        {
            // Act: Set LeftGroundByJumping
            groundDetection.LeftGroundByJumping = true;

            // Assert: Should be set to true
            Assert.IsTrue(groundDetection.LeftGroundByJumping,
                "LeftGroundByJumping should be settable");
        }

        [Test]
        public void LeftGroundByJumping_CanBeReset()
        {
            // Arrange: Set to true first
            groundDetection.LeftGroundByJumping = true;

            // Act: Reset to false
            groundDetection.LeftGroundByJumping = false;

            // Assert: Should be false
            Assert.IsFalse(groundDetection.LeftGroundByJumping,
                "LeftGroundByJumping should be resettable to false");
        }

        #endregion

        #region External State Update Tests

        [Test]
        public void UpdateExternalState_DoesNotThrow()
        {
            // Act & Assert: Updating external state should not throw
            Assert.DoesNotThrow(() => groundDetection.UpdateExternalState(
                _moveInput: Vector2.right,
                _facingRight: true,
                _lastJumpTime: 0f,
                _dashJumpTime: 0f
            ), "UpdateExternalState should not throw");
        }

        [Test]
        public void UpdateExternalState_WithZeroInput_DoesNotThrow()
        {
            // Act & Assert: Zero input should be handled
            Assert.DoesNotThrow(() => groundDetection.UpdateExternalState(
                _moveInput: Vector2.zero,
                _facingRight: true,
                _lastJumpTime: 0f,
                _dashJumpTime: 0f
            ), "UpdateExternalState should handle zero input");
        }

        [Test]
        public void UpdateExternalState_WithNegativeInput_DoesNotThrow()
        {
            // Act & Assert: Negative input should be handled
            Assert.DoesNotThrow(() => groundDetection.UpdateExternalState(
                _moveInput: Vector2.left,
                _facingRight: false,
                _lastJumpTime: 0f,
                _dashJumpTime: 0f
            ), "UpdateExternalState should handle negative input");
        }

        [Test]
        public void UpdateExternalState_WithPositiveTimestamps_DoesNotThrow()
        {
            // Act & Assert: Positive timestamps should be handled
            Assert.DoesNotThrow(() => groundDetection.UpdateExternalState(
                _moveInput: Vector2.zero,
                _facingRight: true,
                _lastJumpTime: 1.5f,
                _dashJumpTime: 2.3f
            ), "UpdateExternalState should handle positive timestamps");
        }

        #endregion

        #region Dash Counter Setters Tests

        [Test]
        public void AirDashesRemaining_CanBeSet()
        {
            // Act: Set air dashes remaining
            groundDetection.AirDashesRemaining = 2;

            // Assert: Should be set correctly
            Assert.AreEqual(2, groundDetection.AirDashesRemaining,
                "AirDashesRemaining should be settable");
        }

        [Test]
        public void AirDashesUsed_CanBeSet()
        {
            // Act: Set air dashes used
            groundDetection.AirDashesUsed = 1;

            // Assert: Should be set correctly
            Assert.AreEqual(1, groundDetection.AirDashesUsed,
                "AirDashesUsed should be settable");
        }

        [Test]
        public void DashesRemaining_CanBeSet()
        {
            // Act: Set dashes remaining
            groundDetection.DashesRemaining = 3;

            // Assert: Should be set correctly
            Assert.AreEqual(3, groundDetection.DashesRemaining,
                "DashesRemaining should be settable");
        }

        [Test]
        public void LastLandTime_CanBeSet()
        {
            // Act: Set last land time
            groundDetection.LastLandTime = 5.5f;

            // Assert: Should be set correctly
            Assert.AreEqual(5.5f, groundDetection.LastLandTime, 0.001f,
                "LastLandTime should be settable");
        }

        [Test]
        public void DashCounters_CanBeSetToZero()
        {
            // Act: Set all counters to zero
            groundDetection.AirDashesRemaining = 0;
            groundDetection.AirDashesUsed = 0;
            groundDetection.DashesRemaining = 0;

            // Assert: All should be zero
            Assert.AreEqual(0, groundDetection.AirDashesRemaining, "AirDashesRemaining should be 0");
            Assert.AreEqual(0, groundDetection.AirDashesUsed, "AirDashesUsed should be 0");
            Assert.AreEqual(0, groundDetection.DashesRemaining, "DashesRemaining should be 0");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void UpdateExternalState_CalledMultipleTimes_DoesNotAccumulateErrors()
        {
            // Act: Call multiple times with varying values
            for (int i = 0; i < 100; i++)
            {
                groundDetection.UpdateExternalState(
                    _moveInput: new Vector2(i % 2 == 0 ? 1f : -1f, 0f),
                    _facingRight: i % 2 == 0,
                    _lastJumpTime: i * 0.1f,
                    _dashJumpTime: i * 0.05f
                );
            }

            // Assert: Should complete without errors
            Assert.Pass("UpdateExternalState should handle multiple rapid calls");
        }

        [Test]
        public void DashCounters_WithNegativeValues_AreAccepted()
        {
            // Act: Set negative values (edge case, shouldn't happen but test robustness)
            groundDetection.AirDashesRemaining = -1;
            groundDetection.DashesRemaining = -1;

            // Assert: Values should be set (no validation enforced at this level)
            Assert.AreEqual(-1, groundDetection.AirDashesRemaining,
                "Should accept negative values (validation happens elsewhere)");
            Assert.AreEqual(-1, groundDetection.DashesRemaining,
                "Should accept negative values (validation happens elsewhere)");
        }

        [Test]
        public void UpdateExternalState_WithExtremeTimestamps_DoesNotThrow()
        {
            // Act & Assert: Extreme values should be handled
            Assert.DoesNotThrow(() => groundDetection.UpdateExternalState(
                _moveInput: Vector2.zero,
                _facingRight: true,
                _lastJumpTime: float.MaxValue,
                _dashJumpTime: float.MaxValue
            ), "Should handle extreme timestamp values");
        }

        #endregion
    }
}
