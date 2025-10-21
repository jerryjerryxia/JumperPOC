using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerJumpSystem component.
    /// Tests configuration validation and meaningful state invariants.
    /// Physics-based jump execution tests deferred to PlayMode.
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
            testGameObject = new GameObject("TestJumpSystem");
            rb = testGameObject.AddComponent<Rigidbody2D>();
            testGameObject.AddComponent<BoxCollider2D>();
            jumpSystem = testGameObject.AddComponent<PlayerJumpSystem>();
            jumpSystem.Initialize(rb, testGameObject.transform, null, null, null, null);

            Debug.unityLogger.logEnabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }

            Debug.unityLogger.logEnabled = true;
        }

        #region Configuration Validation Tests

        [Test]
        public void MaxJumpVelocity_GreaterThanOrEqualToMin()
        {
            // INVARIANT: Max jump velocity must be >= min jump velocity
            // BUG THIS CATCHES: Configuration error that makes variable jump impossible
            Assert.GreaterOrEqual(jumpSystem.MaxJumpVelocity, jumpSystem.MinJumpVelocity,
                "BUG: MaxJumpVelocity < MinJumpVelocity breaks variable jump");
        }

        [Test]
        public void MinJumpVelocity_IsPositive()
        {
            // INVARIANT: Min jump velocity must be > 0 for player to leave ground
            // BUG THIS CATCHES: Configuration that prevents jumping
            Assert.Greater(jumpSystem.MinJumpVelocity, 0f,
                "BUG: MinJumpVelocity <= 0 prevents player from jumping");
        }

        [Test]
        public void MaxJumpVelocity_IsPositive()
        {
            // INVARIANT: Max jump velocity must be > 0
            // BUG THIS CATCHES: Configuration that prevents high jumps
            Assert.Greater(jumpSystem.MaxJumpVelocity, 0f,
                "BUG: MaxJumpVelocity <= 0 prevents player from jumping high");
        }

        [Test]
        public void WallJumpCompensation_IsPositive()
        {
            // INVARIANT: Wall jump compensation must be > 0 to push away from wall
            // BUG THIS CATCHES: Configuration that makes wall jumps stick to wall
            Assert.Greater(jumpSystem.WallJumpCompensation, 0f,
                "BUG: WallJumpCompensation <= 0 makes wall jumps stick to wall");
        }

        [Test]
        public void WallJump_HasPositiveXComponent()
        {
            // INVARIANT: Wall jump must have horizontal component to push away
            // BUG THIS CATCHES: Configuration with no horizontal push (player falls along wall)
            Vector2 wallJump = jumpSystem.WallJump;
            Assert.Greater(wallJump.x, 0f,
                "BUG: WallJump.x <= 0 means no horizontal push away from wall");
        }

        [Test]
        public void WallJump_HasPositiveYComponent()
        {
            // INVARIANT: Wall jump must have vertical component to go up
            // BUG THIS CATCHES: Configuration with no upward force
            Vector2 wallJump = jumpSystem.WallJump;
            Assert.Greater(wallJump.y, 0f,
                "BUG: WallJump.y <= 0 means player doesn't jump upward");
        }

        [Test]
        public void CoyoteTimeDuration_IsNonNegative()
        {
            // INVARIANT: Coyote time cannot be negative
            // BUG THIS CATCHES: Negative time value that breaks coyote time logic
            Assert.GreaterOrEqual(jumpSystem.CoyoteTimeDuration, 0f,
                "BUG: Negative coyote time breaks jump buffering");
        }

        #endregion

        #region State Initialization Tests

        [Test]
        public void JumpsRemaining_InitiallyZero()
        {
            // INVARIANT: Player starts grounded with no jumps available
            // BUG THIS CATCHES: Uninitialized state allowing mid-air jumps on spawn
            Assert.AreEqual(0, jumpSystem.JumpsRemaining,
                "BUG: Player spawns with jumps available (should be grounded)");
        }

        [Test]
        public void IsVariableJumpActive_InitiallyFalse()
        {
            // INVARIANT: Variable jump should not be active on initialization
            // BUG THIS CATCHES: Uninitialized state that forces early jump release
            Assert.IsFalse(jumpSystem.IsVariableJumpActive,
                "BUG: Variable jump active on spawn breaks jump hold");
        }

        [Test]
        public void UpdateExternalState_UpdatesFacingDirection()
        {
            // INVARIANT: UpdateExternalState must update facingRight
            // BUG THIS CATCHES: Wall jump direction using stale facing value

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
            Assert.IsTrue(jumpSystem.FacingRight, "Should update to facing right");

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
            Assert.IsFalse(jumpSystem.FacingRight,
                "BUG: FacingRight not updated - wall jumps go wrong direction");
        }

        #endregion
    }
}
