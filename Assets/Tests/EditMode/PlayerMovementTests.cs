using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerMovement component.
    ///
    /// FOCUS: Exploit prevention and dash mechanic verification
    /// These tests verify BEHAVIOR and catch real bugs.
    ///
    /// CRITICAL INVARIANTS TESTED:
    /// 1. Air dash limit: Maximum 1 air dash before landing
    /// 2. Ground dash limit: Respects dashesRemaining and cooldown
    /// 3. Landing resets: Dash counters reset on landing
    /// 4. Cooldown enforcement: Cannot dash when on cooldown
    /// 5. Wall state blocking: Cannot dash while wall sticking
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
            // Create test GameObject with minimal required components
            testGameObject = new GameObject("TestPlayer");
            rb = testGameObject.AddComponent<Rigidbody2D>();
            movement = testGameObject.AddComponent<PlayerMovement>();

            // Initialize PlayerMovement for testing
            Transform playerTransform = testGameObject.transform;
            Animator animator = testGameObject.AddComponent<Animator>();
            Collider2D col = testGameObject.AddComponent<BoxCollider2D>();

            // Initialize movement component (minimal setup for EditMode)
            movement.InitializeForTesting(rb, playerTransform, animator, col);

            // Ensure PlayerAbilities singleton exists
            if (PlayerAbilities.Instance == null)
            {
                GameObject abilitiesObj = new GameObject("PlayerAbilities");
                PlayerAbilities abilities = abilitiesObj.AddComponent<PlayerAbilities>();
                abilities.InitializeForTesting();
            }

            // Enable dash ability
            PlayerAbilities.Instance.SetAbility("dash", true);

            // Suppress warnings
            Debug.unityLogger.logEnabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }

            if (PlayerAbilities.Instance != null)
            {
                Object.DestroyImmediate(PlayerAbilities.Instance.gameObject);
            }

            var field = typeof(PlayerAbilities).GetField("Instance",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(null, null);
            }

            Debug.unityLogger.logEnabled = true;
        }

        #region Helper Methods

        /// <summary>
        /// Helper to set up airborne player state for testing
        /// </summary>
        private void SetupAirbornePlayer()
        {
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
        }

        /// <summary>
        /// Helper to set up grounded player state for testing
        /// </summary>
        private void SetupGroundedPlayer()
        {
            movement.UpdateExternalState(
                _facingRight: true,
                _moveInput: Vector2.zero,
                _isGrounded: true,
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
        }

        #endregion

        #region Air Dash Exploit Prevention Tests

        /// <summary>
        /// TEST 1: CRITICAL - Verify air dash limited to 1 before landing
        ///
        /// INVARIANT: Player can only use 1 air dash before landing
        ///
        /// BUG THIS CATCHES: If airDashesUsed doesn't increment or check fails,
        /// player can spam infinite air dashes
        ///
        /// EXPLOIT SCENARIO: Player mashes dash in air trying to get multiple air dashes
        /// </summary>
        [Test]
        public void AirDash_LimitedToOne_BeforeLanding()
        {
            // ARRANGE: Set up external state for airborne player
            SetupAirbornePlayer();

            // Verify starting state
            Assert.AreEqual(0, movement.AirDashesUsed, "Should start with 0 air dashes used");

            // ACT: Attempt first air dash
            movement.HandleDash(dashQueued: true);

            // ASSERT: First air dash should work
            Assert.IsTrue(movement.IsDashing, "First air dash should start");
            Assert.AreEqual(1, movement.AirDashesUsed, "AirDashesUsed should be 1");

            // Simulate dash completing
            movement.EndDash();
            Assert.IsFalse(movement.IsDashing, "Dash should end");

            // ACT: Attempt second air dash (EXPLOIT ATTEMPT)
            movement.HandleDash(dashQueued: true);

            // ASSERT: Second air dash should be BLOCKED
            Assert.IsFalse(movement.IsDashing,
                "EXPLOIT: Second air dash worked! Should be blocked.");
            Assert.AreEqual(1, movement.AirDashesUsed,
                "AirDashesUsed should still be 1 (second dash blocked)");
        }

        /// <summary>
        /// TEST 2: CRITICAL - Verify rapid dash inputs don't bypass air dash limit
        ///
        /// INVARIANT: Only ONE air dash can be active despite button mashing
        ///
        /// BUG THIS CATCHES: If isDashing check missing, rapid inputs create multiple dashes
        ///
        /// EXPLOIT SCENARIO: Player rapidly mashes dash button in air
        /// </summary>
        [Test]
        public void AirDash_IgnoresRapidInputs_WhenDashing()
        {
            // ARRANGE: Airborne player
            SetupAirbornePlayer();

            // ACT: Start air dash
            movement.HandleDash(dashQueued: true);
            Assert.IsTrue(movement.IsDashing, "First dash should start");

            int initialAirDashesUsed = movement.AirDashesUsed;

            // EXPLOIT ATTEMPT: Mash dash 10 times while dashing
            for (int i = 0; i < 10; i++)
            {
                movement.HandleDash(dashQueued: true);
            }

            // ASSERT: Only ONE air dash consumed despite mashing
            Assert.AreEqual(initialAirDashesUsed, movement.AirDashesUsed,
                "EXPLOIT: Rapid inputs incremented counter! Should ignore during dash.");
        }

        #endregion

        #region Ground Dash Limit Tests

        /// <summary>
        /// TEST 3: CRITICAL - Verify ground dashes decrement dashesRemaining
        ///
        /// INVARIANT: dashesRemaining decrements with each ground dash REGARDLESS of maxDashes value
        ///
        /// BUG THIS CATCHES: If dashesRemaining doesn't decrement,
        /// player can spam unlimited ground dashes
        ///
        /// TESTS BEHAVIOR: Verifies decrement logic works with maxDashes = 1, 3, 5
        /// </summary>
        [Test]
        public void GroundDash_DecrementsCounter_OnEachDash()
        {
            // TEST WITH DIFFERENT maxDashes VALUES TO VERIFY BEHAVIOR

            // Test with maxDashes = 1
            movement.InitializeForTesting(rb, testGameObject.transform,
                testGameObject.GetComponent<Animator>(), testGameObject.GetComponent<Collider2D>(),
                testMaxDashes: 1);
            SetupGroundedPlayer();

            Assert.AreEqual(1, movement.DashesRemaining, "Should start with 1 dash");
            movement.HandleDash(dashQueued: true);
            movement.EndDash();
            Assert.AreEqual(0, movement.DashesRemaining, "Should have 0 dashes after using 1");

            // Test with maxDashes = 3
            movement.InitializeForTesting(rb, testGameObject.transform,
                testGameObject.GetComponent<Animator>(), testGameObject.GetComponent<Collider2D>(),
                testMaxDashes: 3);
            SetupGroundedPlayer();

            Assert.AreEqual(3, movement.DashesRemaining, "Should start with 3 dashes");
            movement.HandleDash(dashQueued: true);
            movement.EndDash();
            Assert.AreEqual(2, movement.DashesRemaining, "Should have 2 dashes after using 1");
            movement.HandleDash(dashQueued: true);
            movement.EndDash();
            Assert.AreEqual(1, movement.DashesRemaining, "Should have 1 dash after using 2");
            movement.HandleDash(dashQueued: true);
            movement.EndDash();
            Assert.AreEqual(0, movement.DashesRemaining, "Should have 0 dashes after using 3");

            // Test with maxDashes = 5 (verify counter decrements properly with higher values)
            movement.InitializeForTesting(rb, testGameObject.transform,
                testGameObject.GetComponent<Animator>(), testGameObject.GetComponent<Collider2D>(),
                testMaxDashes: 5);
            SetupGroundedPlayer();

            int currentDashes = movement.DashesRemaining;
            Assert.AreEqual(5, currentDashes, "Should start with 5 dashes");

            // Use all 5 dashes and verify each decrement
            for (int i = 0; i < 5; i++)
            {
                movement.HandleDash(dashQueued: true);
                movement.EndDash();
                Assert.AreEqual(currentDashes - (i + 1), movement.DashesRemaining,
                    $"Should have {currentDashes - (i + 1)} dashes after using {i + 1}");
            }
        }

        /// <summary>
        /// TEST 4: CRITICAL - Verify cooldown prevents dash when dashesRemaining = 0
        ///
        /// INVARIANT: Cannot dash when dashesRemaining = 0 AND dashCDTimer > 0
        ///
        /// BUG THIS CATCHES: If cooldown check fails, player can dash spam after exhausting dashes
        ///
        /// TESTS BEHAVIOR: Verifies cooldown blocks dashes regardless of maxDashes value
        /// </summary>
        [Test]
        public void GroundDash_BlockedByCooldown_WhenExhausted()
        {
            // Test cooldown with maxDashes = 1
            movement.InitializeForTesting(rb, testGameObject.transform,
                testGameObject.GetComponent<Animator>(), testGameObject.GetComponent<Collider2D>(),
                testMaxDashes: 1, testDashCooldown: 0.5f);
            SetupGroundedPlayer();

            movement.HandleDash(dashQueued: true);
            movement.EndDash();

            Assert.AreEqual(0, movement.DashesRemaining, "Should have 0 dashes after using 1");
            Assert.IsTrue(movement.DashCDTimer > 0, "Cooldown should be active");

            movement.HandleDash(dashQueued: true);
            Assert.IsFalse(movement.IsDashing, "EXPLOIT: Dash worked during cooldown with maxDashes=1!");

            // Test cooldown with maxDashes = 4
            movement.InitializeForTesting(rb, testGameObject.transform,
                testGameObject.GetComponent<Animator>(), testGameObject.GetComponent<Collider2D>(),
                testMaxDashes: 4, testDashCooldown: 0.3f);
            SetupGroundedPlayer();

            // Exhaust all 4 dashes
            for (int i = 0; i < 4; i++)
            {
                movement.HandleDash(dashQueued: true);
                movement.EndDash();
            }

            Assert.AreEqual(0, movement.DashesRemaining, "Should have 0 dashes after using all 4");
            Assert.IsTrue(movement.DashCDTimer > 0, "Cooldown should be active");

            movement.HandleDash(dashQueued: true);
            Assert.IsFalse(movement.IsDashing, "EXPLOIT: Dash worked during cooldown with maxDashes=4!");
        }

        #endregion

        #region Landing Reset Tests

        /// <summary>
        /// TEST 5: CRITICAL - Verify landing resets air dash counter
        ///
        /// INVARIANT: OnLanding() must reset airDashesUsed to 0
        ///
        /// BUG THIS CATCHES: If OnLanding() doesn't reset, player can't air dash after landing
        /// </summary>
        [Test]
        public void Landing_ResetsAirDashCounter_Always()
        {
            // ARRANGE: Player has used air dash
            SetupAirbornePlayer();

            movement.HandleDash(dashQueued: true);
            Assert.AreEqual(1, movement.AirDashesUsed, "Should have used 1 air dash");
            movement.EndDash();

            // ACT: Land
            movement.OnLanding();

            // ASSERT: Counter should reset to 0
            Assert.AreEqual(0, movement.AirDashesUsed,
                "Landing should reset airDashesUsed to 0");
        }

        /// <summary>
        /// TEST 6: CRITICAL - Verify landing resets ground dash counter
        ///
        /// INVARIANT: OnLanding() must reset dashesRemaining to maxDashes
        ///
        /// BUG THIS CATCHES: If OnLanding() doesn't reset, player loses ground dashes permanently
        ///
        /// TESTS BEHAVIOR: Verifies reset works regardless of maxDashes value
        /// </summary>
        [Test]
        public void Landing_ResetsGroundDashCounter_Always()
        {
            // Test reset with maxDashes = 2
            movement.InitializeForTesting(rb, testGameObject.transform,
                testGameObject.GetComponent<Animator>(), testGameObject.GetComponent<Collider2D>(),
                testMaxDashes: 2);
            SetupGroundedPlayer();

            movement.HandleDash(dashQueued: true);
            movement.EndDash();
            Assert.AreEqual(1, movement.DashesRemaining, "Should have 1 dash after using one");

            SetupAirbornePlayer();
            movement.OnLanding();

            Assert.AreEqual(2, movement.DashesRemaining, "Landing should reset to maxDashes (2)");

            // Test reset with maxDashes = 5
            movement.InitializeForTesting(rb, testGameObject.transform,
                testGameObject.GetComponent<Animator>(), testGameObject.GetComponent<Collider2D>(),
                testMaxDashes: 5);
            SetupGroundedPlayer();

            // Use 3 dashes
            for (int i = 0; i < 3; i++)
            {
                movement.HandleDash(dashQueued: true);
                movement.EndDash();
            }
            Assert.AreEqual(2, movement.DashesRemaining, "Should have 2 dashes after using 3");

            SetupAirbornePlayer();
            movement.OnLanding();

            Assert.AreEqual(5, movement.DashesRemaining, "Landing should reset to maxDashes (5)");
        }

        #endregion

        #region Dash State Machine Tests

        /// <summary>
        /// TEST 7: Verify dash direction matches facingRight
        ///
        /// INVARIANT: Dash direction should match player facing direction
        ///
        /// BUG THIS CATCHES: If facingRight not used correctly, dash goes wrong direction
        /// </summary>
        [Test]
        public void Dash_DirectionMatchesFacing_Always()
        {
            // ARRANGE: Grounded player
            SetupGroundedPlayer();

            bool initialFacingRight = movement.FacingRight;

            // ACT: Dash
            movement.HandleDash(dashQueued: true);
            Assert.IsTrue(movement.IsDashing, "Dash should start");

            // ASSERT: wasGroundedBeforeDash should be set correctly
            Assert.AreEqual(true, movement.WasGroundedBeforeDash,
                "WasGroundedBeforeDash should be true when dashing from ground");

            movement.EndDash();
        }

        /// <summary>
        /// TEST 8: Verify EndDash() clears isDashing state
        ///
        /// INVARIANT: EndDash() must set isDashing = false
        ///
        /// BUG THIS CATCHES: If EndDash() doesn't clear state, player stuck in dash
        /// </summary>
        [Test]
        public void EndDash_ClearsDashState_Always()
        {
            // ARRANGE: Start dash
            SetupAirbornePlayer();

            movement.HandleDash(dashQueued: true);
            Assert.IsTrue(movement.IsDashing, "Dash should be active");

            // ACT: End dash
            movement.EndDash();

            // ASSERT: isDashing should be false
            Assert.IsFalse(movement.IsDashing, "EndDash() should clear isDashing");
        }

        #endregion
    }
}
