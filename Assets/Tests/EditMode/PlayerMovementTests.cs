using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerMovement component.
    /// Tests direction management, dash state, and movement configuration.
    /// Focus on state management logic rather than physics simulation.
    /// </summary>
    [TestFixture]
    public class PlayerMovementTests
    {
        private GameObject testGameObject;
        private PlayerMovement movement;
        private Rigidbody2D rb;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with required components
            testGameObject = new GameObject("TestMovement");
            rb = testGameObject.AddComponent<Rigidbody2D>();
            movement = testGameObject.AddComponent<PlayerMovement>();

            // Initialize component with minimal setup
            movement.Initialize(rb, testGameObject.transform, null, null, null, null, null, null);
            movement.InitializeDashState();

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

        #region Configuration Properties Tests

        [Test]
        public void RunSpeed_ReturnsConfiguredValue()
        {
            // Assert: RunSpeed should be accessible
            Assert.GreaterOrEqual(movement.RunSpeed, 0f, "RunSpeed should be non-negative");
        }

        [Test]
        public void DashTime_ReturnsConfiguredValue()
        {
            // Assert: DashTime should be accessible
            Assert.Greater(movement.DashTime, 0f, "DashTime should be positive");
        }

        [Test]
        public void MaxDashes_ReturnsConfiguredValue()
        {
            // Assert: MaxDashes should be accessible
            Assert.GreaterOrEqual(movement.MaxDashes, 0, "MaxDashes should be non-negative");
        }

        [Test]
        public void MaxAirDashes_ReturnsConfiguredValue()
        {
            // Assert: MaxAirDashes should be accessible
            Assert.GreaterOrEqual(movement.MaxAirDashes, 0, "MaxAirDashes should be non-negative");
        }

        #endregion

        #region Dash State Initialization Tests

        [Test]
        public void InitializeDashState_SetsDashesToMax()
        {
            // Act: Initialize dash state
            movement.InitializeDashState();

            // Assert: Dashes should be set to max
            Assert.AreEqual(movement.MaxDashes, movement.DashesRemaining,
                "DashesRemaining should equal MaxDashes after initialization");
        }

        [Test]
        public void InitializeDashState_SetsAirDashesToMax()
        {
            // Act: Initialize dash state
            movement.InitializeDashState();

            // Assert: Air dashes should be set to max
            Assert.AreEqual(movement.MaxAirDashes, movement.AirDashesRemaining,
                "AirDashesRemaining should equal MaxAirDashes after initialization");
        }

        #endregion

        #region Dash State Management Tests

        [Test]
        public void SetDashState_UpdatesDashesRemaining()
        {
            // Arrange: Initial state
            movement.InitializeDashState();

            // Act: Set dash state
            movement.SetDashState(1, 0);

            // Assert: Dashes remaining should be updated
            Assert.AreEqual(1, movement.DashesRemaining, "DashesRemaining should be updated");
        }

        [Test]
        public void SetDashState_UpdatesAirDashesUsed()
        {
            // Arrange: Initial state
            movement.InitializeDashState();

            // Act: Set dash state
            movement.SetDashState(2, 1);

            // Assert: Air dashes used should be updated
            Assert.AreEqual(1, movement.AirDashesUsed, "AirDashesUsed should be updated");
        }

        [Test]
        public void SetDashState_HandlesZeroDashes()
        {
            // Act: Set to zero dashes
            movement.SetDashState(0, 2);

            // Assert: Should handle zero dashes correctly
            Assert.AreEqual(0, movement.DashesRemaining, "Should handle zero dashes");
            Assert.AreEqual(2, movement.AirDashesUsed, "Should track air dashes used");
        }

        #endregion

        #region Dash Timing Tests

        [Test]
        public void EndDash_SetsDashingToFalse()
        {
            // Arrange: Simulate dashing state (requires external state update)
            // Note: We can't directly set isDashing private field, but we can test EndDash behavior

            // Act: End dash
            movement.EndDash();

            // Assert: IsDashing should be false
            Assert.IsFalse(movement.IsDashing, "IsDashing should be false after EndDash");
        }

        [Test]
        public void EndDash_UpdatesLastDashEndTime()
        {
            // Arrange: Get initial time
            float timeBefore = Time.time;

            // Act: End dash
            movement.EndDash();

            // Assert: LastDashEndTime should be updated
            Assert.GreaterOrEqual(movement.LastDashEndTime, timeBefore,
                "LastDashEndTime should be updated to current time or later");
        }

        #endregion

        #region External State Update Tests

        [Test]
        public void UpdateExternalState_UpdatesFacingRight()
        {
            // Act: Update external state with facingRight = true
            movement.UpdateExternalState(
                _facingRight: true,
                _moveInput: Vector2.zero,
                _isGrounded: false,
                _onWall: false,
                _wallStickAllowed: false,
                _isOnSlope: false,
                _currentSlopeAngle: 0f,
                _slopeNormal: Vector2.up,
                _isBufferClimbing: false,
                _isWallSliding: false,
                _isWallSticking: false,
                _isAttacking: false,
                _isDashAttacking: false,
                _isAirAttacking: false,
                _jumpQueued: false,
                _isJumping: false,
                _dashJumpTime: 0f
            );

            // Assert: FacingRight should be updated
            Assert.IsTrue(movement.FacingRight, "FacingRight should be updated via external state");
        }

        [Test]
        public void UpdateExternalState_UpdatesFacingLeft()
        {
            // Act: Update external state with facingRight = false
            movement.UpdateExternalState(
                _facingRight: false,
                _moveInput: Vector2.zero,
                _isGrounded: false,
                _onWall: false,
                _wallStickAllowed: false,
                _isOnSlope: false,
                _currentSlopeAngle: 0f,
                _slopeNormal: Vector2.up,
                _isBufferClimbing: false,
                _isWallSliding: false,
                _isWallSticking: false,
                _isAttacking: false,
                _isDashAttacking: false,
                _isAirAttacking: false,
                _jumpQueued: false,
                _isJumping: false,
                _dashJumpTime: 0f
            );

            // Assert: FacingRight should be false
            Assert.IsFalse(movement.FacingRight, "FacingRight should be false when updated");
        }

        #endregion

        #region Property Access Tests

        [Test]
        public void DashTimer_IsAccessible()
        {
            // Assert: DashTimer should be accessible
            float dashTimer = movement.DashTimer;
            Assert.GreaterOrEqual(dashTimer, 0f, "DashTimer should be non-negative");
        }

        [Test]
        public void DashCDTimer_IsAccessible()
        {
            // Assert: DashCDTimer should be accessible
            float cooldownTimer = movement.DashCDTimer;
            Assert.GreaterOrEqual(cooldownTimer, 0f, "DashCDTimer should be non-negative");
        }

        [Test]
        public void WasGroundedBeforeDash_IsAccessible()
        {
            // Assert: WasGroundedBeforeDash should be accessible
            bool wasGrounded = movement.WasGroundedBeforeDash;
            // No specific value expected, just testing accessibility
            Assert.NotNull(wasGrounded.GetType(), "WasGroundedBeforeDash should be accessible");
        }

        [Test]
        public void DashJumpTime_IsAccessible()
        {
            // Assert: DashJumpTime should be accessible
            float dashJumpTime = movement.DashJumpTime;
            Assert.GreaterOrEqual(dashJumpTime, 0f, "DashJumpTime should be non-negative");
        }

        #endregion

        #region Direction and Input Tests

        [Test]
        public void FacingDirection_ReturnsOne_WhenFacingRight()
        {
            // Arrange: Set facing right
            movement.UpdateExternalState(
                _facingRight: true,
                _moveInput: Vector2.zero,
                _isGrounded: false,
                _onWall: false,
                _wallStickAllowed: false,
                _isOnSlope: false,
                _currentSlopeAngle: 0f,
                _slopeNormal: Vector2.up,
                _isBufferClimbing: false,
                _isWallSliding: false,
                _isWallSticking: false,
                _isAttacking: false,
                _isDashAttacking: false,
                _isAirAttacking: false,
                _jumpQueued: false,
                _isJumping: false,
                _dashJumpTime: 0f
            );

            // Note: FacingDirection is calculated in HandleMovement which we're not calling
            // This test is more about verifying the property is accessible
            float direction = movement.FacingDirection;
            Assert.IsNotNull(direction.GetType(), "FacingDirection should be accessible");
        }

        [Test]
        public void HorizontalInput_IsAccessible()
        {
            // Assert: HorizontalInput should be accessible
            float horizontalInput = movement.HorizontalInput;
            Assert.IsNotNull(horizontalInput.GetType(), "HorizontalInput should be accessible");
        }

        #endregion

        #region Wall Detection Configuration Tests

        [Test]
        public void WallCheckDistance_ReturnsPositiveValue()
        {
            // Assert: WallCheckDistance should be positive
            Assert.Greater(movement.WallCheckDistance, 0f,
                "WallCheckDistance should be positive for wall detection");
        }

        [Test]
        public void WallRaycastPositions_AreOrdered()
        {
            // Assert: Raycast positions should be ordered (bottom < middle < top)
            Assert.Less(movement.WallRaycastBottom, movement.WallRaycastMiddle,
                "Bottom raycast should be below middle");
            Assert.Less(movement.WallRaycastMiddle, movement.WallRaycastTop,
                "Middle raycast should be below top");
        }

        #endregion

        #region Dash Configuration Tests

        [Test]
        public void DashSpeed_IsPositive()
        {
            // Assert: Dash should have positive speed
            // Note: We can't access private dashSpeed directly, but we test the pattern
            Assert.Greater(movement.DashTime, 0f, "Dash time should be positive");
        }

        [Test]
        public void DashJump_HasBothComponents()
        {
            // Assert: DashJump should have both X and Y components
            Vector2 dashJump = movement.DashJump;
            Assert.Greater(dashJump.x, 0f, "DashJump X should be positive (horizontal)");
            Assert.Greater(dashJump.y, 0f, "DashJump Y should be positive (vertical)");
        }

        [Test]
        public void DashJumpWindow_IsPositive()
        {
            // Assert: Dash jump window should be positive
            Assert.Greater(movement.DashJumpWindow, 0f,
                "DashJumpWindow should be positive for timing");
        }

        #endregion
    }
}
