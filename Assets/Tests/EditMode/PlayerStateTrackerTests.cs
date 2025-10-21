using NUnit.Framework;
using UnityEngine;
using Tests.Helpers;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerStateTracker component.
    /// Tests pure state calculation logic including running, jumping, falling, wall sticking/sliding.
    /// CRITICAL: These tests validate the core movement state machine.
    /// </summary>
    [TestFixture]
    public class PlayerStateTrackerTests
    {
        private GameObject testGameObject;
        private PlayerStateTracker stateTracker;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with PlayerStateTracker component
            testGameObject = new GameObject("TestStateTracker");
            stateTracker = testGameObject.AddComponent<PlayerStateTracker>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test objects
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        #region Running State Tests

        [Test]
        public void UpdateStates_SetsIsRunning_WhenGroundedAndMoving()
        {
            // Arrange: Player grounded, pressing right, not in any special state
            Vector2 moveInput = new Vector2(1f, 0f);
            bool isGrounded = true;
            bool isDashing = false;

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: moveInput,
                isGrounded: isGrounded,
                onWall: false,
                wallStickAllowed: false,
                isDashing: isDashing,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should be running
            Assert.IsTrue(stateTracker.IsRunning, "Player should be running when grounded and moving");
        }

        [Test]
        public void UpdateStates_SetsIsRunningFalse_WhenNotMoving()
        {
            // Arrange: Player grounded but not moving
            Vector2 moveInput = Vector2.zero;
            bool isGrounded = true;

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: moveInput,
                isGrounded: isGrounded,
                onWall: false,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should not be running
            Assert.IsFalse(stateTracker.IsRunning, "Player should not be running when not moving");
        }

        [Test]
        public void UpdateStates_SetsIsRunningFalse_WhenDashing()
        {
            // Arrange: Player moving but dashing
            Vector2 moveInput = new Vector2(1f, 0f);
            bool isDashing = true;

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: moveInput,
                isGrounded: true,
                onWall: false,
                wallStickAllowed: false,
                isDashing: isDashing,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should not be running during dash
            Assert.IsFalse(stateTracker.IsRunning, "Player should not be running when dashing");
        }

        [Test]
        public void UpdateStates_SetsIsRunningFalse_WhenDashAttacking()
        {
            // Arrange: Player moving but dash attacking
            Vector2 moveInput = new Vector2(1f, 0f);
            bool isDashAttacking = true;

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: moveInput,
                isGrounded: true,
                onWall: false,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: isDashAttacking,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should not be running during dash attack
            Assert.IsFalse(stateTracker.IsRunning, "Player should not be running when dash attacking");
        }

        [Test]
        public void UpdateStates_AllowsRunningOnSlope_EvenWithWallDetection()
        {
            // Arrange: Player on slope, moving, wall detected (common slope edge case)
            Vector2 moveInput = new Vector2(1f, 0f);
            bool isOnSlope = true;
            bool onWall = true; // Wall detected but we're on a slope

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: moveInput,
                isGrounded: true,
                onWall: onWall,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: isOnSlope,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: CRITICAL - Should still be running on slopes even with wall detection
            Assert.IsTrue(stateTracker.IsRunning, "Player MUST be able to run on slopes even when wall is detected");
        }

        #endregion

        #region Jumping/Falling State Tests

        [Test]
        public void UpdateStates_SetsIsJumping_WhenAirborneAndAscending()
        {
            // Arrange: Player airborne with upward velocity
            Vector2 velocity = new Vector2(0f, 5f); // Ascending
            bool isGrounded = false;

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: isGrounded,
                onWall: false,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: velocity,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should be jumping
            Assert.IsTrue(stateTracker.IsJumping, "Player should be jumping when airborne and ascending");
            Assert.IsFalse(stateTracker.IsFalling, "Player should not be falling when ascending");
        }

        [Test]
        public void UpdateStates_SetsIsFalling_WhenAirborneAndDescending()
        {
            // Arrange: Player airborne with downward velocity
            Vector2 velocity = new Vector2(0f, -3f); // Descending
            bool isGrounded = false;

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: isGrounded,
                onWall: false,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: velocity,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should be falling
            Assert.IsFalse(stateTracker.IsJumping, "Player should not be jumping when descending");
            Assert.IsTrue(stateTracker.IsFalling, "Player should be falling when airborne and descending");
        }

        [Test]
        public void UpdateStates_SetsNeitherJumpingNorFalling_WhenGrounded()
        {
            // Arrange: Player grounded (even with velocity)
            Vector2 velocity = new Vector2(5f, 0f);
            bool isGrounded = true;

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: new Vector2(1f, 0f),
                isGrounded: isGrounded,
                onWall: false,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: velocity,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Neither jumping nor falling when grounded
            Assert.IsFalse(stateTracker.IsJumping, "Player should not be jumping when grounded");
            Assert.IsFalse(stateTracker.IsFalling, "Player should not be falling when grounded");
        }

        [Test]
        public void UpdateStates_SetsJumpingFalse_WhenAirAttacking()
        {
            // Arrange: Player airborne ascending but air attacking
            Vector2 velocity = new Vector2(0f, 3f);
            bool isAirAttacking = true;

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: false,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: isAirAttacking,
                velocity: velocity,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should not be in jumping state during air attack
            Assert.IsFalse(stateTracker.IsJumping, "Player should not be jumping when air attacking");
        }

        #endregion

        #region Wall Sticking Tests

        [Test]
        public void UpdateStates_SetsIsWallSticking_WhenConditionsMet()
        {
            // Arrange: Player on wall, wall stick allowed, has ability, not in special state
            bool wallStickAllowed = true;
            bool hasWallStickAbility = true;

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: wallStickAllowed,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: hasWallStickAbility,
                showJumpDebug: false
            );

            // Assert: Should be wall sticking
            Assert.IsTrue(stateTracker.IsWallSticking, "Player should be wall sticking when conditions are met");
        }

        [Test]
        public void UpdateStates_FiresOnEnterWallStick_OnTransition()
        {
            // Arrange: Subscribe to wall stick event
            bool eventFired = false;
            stateTracker.OnEnterWallStick += () => eventFired = true;

            // First frame - not wall sticking
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: false,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Act: Second frame - enter wall stick
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: true,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Event should have fired on transition
            Assert.IsTrue(eventFired, "OnEnterWallStick event should fire when transitioning to wall stick");
        }

        [Test]
        public void UpdateStates_DoesNotFireWallStickEvent_WhenAlreadySticking()
        {
            // Arrange: Enter wall stick first
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: true,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Subscribe to event after already sticking
            bool eventFired = false;
            stateTracker.OnEnterWallStick += () => eventFired = true;

            // Act: Update again while still sticking
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: true,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Event should NOT fire when already sticking
            Assert.IsFalse(eventFired, "OnEnterWallStick should only fire on transition, not every frame");
        }

        [Test]
        public void UpdateStates_PreventsWallStick_WhenDashing()
        {
            // Arrange: Wall stick conditions met but player is dashing
            bool isDashing = true;

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: true,
                isDashing: isDashing,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should NOT wall stick while dashing
            Assert.IsFalse(stateTracker.IsWallSticking, "Player should not wall stick while dashing");
        }

        [Test]
        public void UpdateStates_PreventsWallStick_WhenNoAbility()
        {
            // Arrange: All conditions met except ability
            bool hasWallStickAbility = false;

            // Act: Update states
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: true,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: hasWallStickAbility,
                showJumpDebug: false
            );

            // Assert: Should NOT wall stick without ability
            Assert.IsFalse(stateTracker.IsWallSticking, "Player should not wall stick without the ability unlocked");
        }

        #endregion

        #region Wall Sliding Tests - CRITICAL Sequential Logic

        [Test]
        public void UpdateStates_RequiresWallStickFirst_BeforeWallSlide()
        {
            // CRITICAL TEST: Wall slide can ONLY happen after wall sticking first

            // Arrange: Player falling against wall WITHOUT having wall stuck first
            Vector2 velocity = new Vector2(0f, -5f); // Fast falling
            bool onWall = true;

            // Act: Try to wall slide without wall sticking first
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: onWall,
                wallStickAllowed: false, // NOT sticking
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: velocity,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should NOT wall slide without sticking first
            Assert.IsFalse(stateTracker.IsWallSliding, "CRITICAL: Wall sliding should REQUIRE wall sticking first");
        }

        [Test]
        public void UpdateStates_AllowsWallSlide_AfterWallSticking()
        {
            // CRITICAL TEST: Wall slide allowed after wall stick transition

            // Frame 1: Enter wall stick
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: true, // Sticking
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Frame 2: Start falling fast (transition to wall slide)
            Vector2 velocity = new Vector2(0f, -5f); // Faster than wallSlideSpeed
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: false, // No longer sticking
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: velocity,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: NOW wall slide should be allowed
            Assert.IsTrue(stateTracker.IsWallSliding, "Wall sliding should be allowed after wall sticking first");
        }

        [Test]
        public void UpdateStates_ResetsWallSlideHistory_WhenLeavingWall()
        {
            // CRITICAL TEST: Wall slide history resets when leaving wall

            // Frame 1: Wall stick
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: true,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Frame 2: Leave wall (jump away)
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: false, // No longer on wall
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: new Vector2(0f, 5f),
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Frame 3: Return to wall falling fast (without sticking again)
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: new Vector2(0f, -5f),
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should NOT wall slide - history was reset when leaving wall
            Assert.IsFalse(stateTracker.IsWallSliding, "CRITICAL: Wall slide history should reset when leaving wall");
        }

        [Test]
        public void UpdateStates_PreventsWallSlide_WhenNotFallingFastEnough()
        {
            // Arrange: Wall stuck first, then slow descent
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: true,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Act: Falling but not fast enough (slower than wallSlideSpeed)
            Vector2 slowVelocity = new Vector2(0f, -1f); // Slower than wallSlideSpeed (2f)
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: true,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: slowVelocity,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should NOT wall slide when falling too slowly
            Assert.IsFalse(stateTracker.IsWallSliding, "Wall slide should only trigger when falling faster than wallSlideSpeed");
        }

        #endregion

        #region Animation Parameters Tests

        [Test]
        public void UpdateStates_SetsHorizontalInput_Correctly()
        {
            // Arrange & Act: Update with horizontal input
            Vector2 moveInput = new Vector2(0.75f, 0f);
            stateTracker.UpdateStates(
                moveInput: moveInput,
                isGrounded: true,
                onWall: false,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: HorizontalInput should match moveInput.x
            AssertExtensions.AreApproximatelyEqual(0.75f, stateTracker.HorizontalInput, 0.001f,
                "HorizontalInput should match moveInput.x");
        }

        [Test]
        public void UpdateStates_SetsFacingDirection_BasedOnFacingRight()
        {
            // Test facing right
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: true,
                onWall: false,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            Assert.AreEqual(1f, stateTracker.FacingDirection, "FacingDirection should be 1 when facing right");

            // Test facing left
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: true,
                onWall: false,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: false,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            Assert.AreEqual(-1f, stateTracker.FacingDirection, "FacingDirection should be -1 when facing left");
        }

        [Test]
        public void UpdateStates_SetsIsDashingAnim_MatchingDashState()
        {
            // Test dashing
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: true,
                onWall: false,
                wallStickAllowed: false,
                isDashing: true,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: Vector2.zero,
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            Assert.IsTrue(stateTracker.IsDashingAnim, "IsDashingAnim should match isDashing state");
        }

        #endregion

        #region External State Setters Tests

        [Test]
        public void IsClimbing_CanBeSetExternally()
        {
            // Act: Set climbing state
            stateTracker.IsClimbing = true;

            // Assert: Should persist
            Assert.IsTrue(stateTracker.IsClimbing, "IsClimbing should be settable externally");

            stateTracker.IsClimbing = false;
            Assert.IsFalse(stateTracker.IsClimbing, "IsClimbing should update when set to false");
        }

        [Test]
        public void IsLedgeGrabbing_CanBeSetExternally()
        {
            // Act: Set ledge grabbing state
            stateTracker.IsLedgeGrabbing = true;

            // Assert: Should persist
            Assert.IsTrue(stateTracker.IsLedgeGrabbing, "IsLedgeGrabbing should be settable externally");
        }

        [Test]
        public void UpdateStates_RespectsExternallySetStates()
        {
            // Arrange: Set climbing externally
            stateTracker.IsClimbing = true;

            // Act: Update states (should not override external state)
            stateTracker.UpdateStates(
                moveInput: Vector2.zero,
                isGrounded: false,
                onWall: false,
                wallStickAllowed: false,
                isDashing: false,
                isDashAttacking: false,
                isAirAttacking: false,
                velocity: new Vector2(0f, 5f), // Ascending
                isOnSlope: false,
                facingRight: true,
                wallSlideSpeed: 2f,
                hasWallStickAbility: true,
                showJumpDebug: false
            );

            // Assert: Should not be jumping when climbing (even with upward velocity)
            Assert.IsFalse(stateTracker.IsJumping, "Should not be jumping when climbing");
            Assert.IsTrue(stateTracker.IsClimbing, "Climbing state should be preserved");
        }

        #endregion
    }
}
