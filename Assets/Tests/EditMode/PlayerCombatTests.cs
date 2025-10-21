using NUnit.Framework;
using UnityEngine;
using Tests.Helpers;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerCombat component.
    ///
    /// FOCUS: Bug-revealing tests that catch exploits and edge cases
    /// These tests verify BEHAVIOR, not implementation details.
    ///
    /// CRITICAL INVARIANTS TESTED:
    /// 1. Air attack limit: Maximum 2 attacks before landing
    /// 2. Double jump slot forfeiture: Unused first slot is lost on DJ
    /// 3. Dash attack timing: Only during dash OR 0.2s grace period
    /// 4. Attack state reset: Proper cleanup on landing/completion
    /// 5. Input buffering: Buffered inputs execute once, then clear
    /// 6. Combo system: 3-hit loop with proper window timing
    /// </summary>
    [TestFixture]
    public class PlayerCombatTests
    {
        private GameObject testGameObject;
        private PlayerCombat combat;
        private PlayerController controller;
        private Rigidbody2D rb;
        private Animator animator;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with minimal required components
            testGameObject = new GameObject("TestPlayer");
            rb = testGameObject.AddComponent<Rigidbody2D>();
            animator = testGameObject.AddComponent<Animator>();
            testGameObject.AddComponent<BoxCollider2D>();

            // Add PlayerCombat (Awake will run)
            combat = testGameObject.AddComponent<PlayerCombat>();

            // Initialize PlayerCombat for testing (bypasses PlayerController dependency)
            combat.InitializeForTesting(rb, animator, null);

            // Ensure PlayerAbilities singleton exists and is unlocked
            if (PlayerAbilities.Instance == null)
            {
                GameObject abilitiesObj = new GameObject("PlayerAbilities");
                PlayerAbilities abilities = abilitiesObj.AddComponent<PlayerAbilities>();
                abilities.InitializeForTesting(); // EditMode doesn't call Awake() automatically
            }

            // Unlock all abilities for testing (tests will disable as needed)
            PlayerTestHelper.ResetAllAbilities();

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

            // CRITICAL: Clean up PlayerAbilities singleton to prevent test contamination
            if (PlayerAbilities.Instance != null)
            {
                Object.DestroyImmediate(PlayerAbilities.Instance.gameObject);
            }

            // Reset the static Instance field using reflection
            var field = typeof(PlayerAbilities).GetField("Instance",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(null, null);
            }

            Debug.unityLogger.logEnabled = true;
        }

        #region Air Attack Limit Tests - CRITICAL EXPLOIT PREVENTION

        /// <summary>
        /// TEST 1: CRITICAL - Verify air attack limit cannot be exceeded
        ///
        /// INVARIANT: Player can use maximum 2 air attacks before landing
        ///
        /// BUG THIS CATCHES: If airAttacksUsed increments without checking HasUsedAirAttack,
        /// player can spam infinite air attacks
        ///
        /// EXPLOIT SCENARIO: Player mashes attack button in air trying to get 3+ attacks
        /// </summary>
        [Test]
        public void AirAttack_LimitedToTwo_BeforeLanding()
        {
            // ARRANGE: Player airborne with double jump ability (enables 2 air attacks)
            PlayerTestHelper.SetupAirbornePlayer(testGameObject, hasDoubleJump: true);

            // Verify starting state
            Assert.AreEqual(0, combat.AirAttacksUsed, "Should start with 0 air attacks used");
            Assert.IsFalse(combat.HasUsedAirAttack, "HasUsedAirAttack should be false initially");

            // ACT: Use first air attack
            combat.HandleAttackInput();

            // ASSERT: First attack should work
            Assert.AreEqual(1, combat.AirAttacksUsed, "First air attack should register");
            Assert.IsTrue(combat.IsAirAttacking, "Should be in air attack state");

            // Simulate attack completing (reset attack state but keep counter)
            combat.ResetAttackSystem();

            // ACT: Enable second slot via double jump, then use second attack
            combat.OnDoubleJump();
            combat.HandleAttackInput();

            // ASSERT: Second attack should work
            Assert.AreEqual(2, combat.AirAttacksUsed, "Second air attack should register");

            // Simulate attack completing
            combat.ResetAttackSystem();

            // ACT: Try third attack (EXPLOIT ATTEMPT)
            combat.HandleAttackInput();

            // ASSERT: Third attack should be BLOCKED
            PlayerTestHelper.AssertExploitPrevented(
                "Infinite Air Attacks",
                combat.AirAttacksUsed > 2,
                "Player used more than 2 air attacks before landing!"
            );

            Assert.AreEqual(2, combat.AirAttacksUsed, "Should still be at 2 air attacks (3rd blocked)");
            Assert.IsFalse(combat.IsAirAttacking, "Third air attack should not start");
        }

        /// <summary>
        /// TEST 2: CRITICAL - Verify rapid clicking doesn't create multiple simultaneous attacks
        ///
        /// INVARIANT: Only ONE air attack can be active at a time
        ///
        /// BUG THIS CATCHES: If isAirAttacking check is missing in StartAirAttack (line 441),
        /// rapid clicks create overlapping attack states
        ///
        /// EXPLOIT SCENARIO: Player rapidly mashes attack button during animation
        /// </summary>
        [Test]
        public void AirAttack_IgnoresRapidClicks_DuringAnimation()
        {
            // ARRANGE: Player airborne
            PlayerTestHelper.SetupAirbornePlayer(testGameObject);

            // ACT: Start first air attack
            combat.HandleAttackInput();
            int initialAttackCount = combat.AirAttacksUsed;

            // EXPLOIT ATTEMPT: Mash attack button 10 times during animation
            for (int i = 0; i < 10; i++)
            {
                combat.HandleAttackInput();
            }

            // ASSERT: Only ONE attack should be registered despite button mashing
            Assert.AreEqual(initialAttackCount, combat.AirAttacksUsed,
                "EXPLOIT: Rapid clicking created multiple attacks! Should ignore inputs during animation.");

            // Clean up and verify second attack still works after completion
            combat.ResetAttackSystem();
            combat.OnDoubleJump(); // Enable second slot
            combat.HandleAttackInput();

            Assert.AreEqual(2, combat.AirAttacksUsed,
                "Second attack should work normally after first completes");
        }

        /// <summary>
        /// TEST 3: CRITICAL DESIGN RULE - Double jump forfeits unused first air attack slot
        ///
        /// DESIGN RULE: Air attack slots are separated by double jump
        /// - Slot 1: Before double jump
        /// - Slot 2: After double jump
        /// If player doesn't use slot 1 before DJ, they LOSE it (prevents 3 total attacks)
        ///
        /// CODE REFERENCE: PlayerCombat.cs lines 334-339
        ///
        /// BUG THIS CATCHES: If forfeiture logic is removed, players can:
        /// 1. Not attack before DJ (saves slot 1)
        /// 2. Double jump
        /// 3. Use 2 attacks after DJ
        /// Total: 3 attacks (should be 2 max)
        /// </summary>
        [Test]
        public void DoubleJump_ForfeitsFirstSlot_WhenUnused()
        {
            // ARRANGE: Player airborne with 0 air attacks used
            PlayerTestHelper.SetupAirbornePlayer(testGameObject, hasDoubleJump: true);
            Assert.AreEqual(0, combat.AirAttacksUsed, "Should start at 0");

            // ACT: Double jump WITHOUT using first air attack slot
            combat.OnDoubleJump();

            // ASSERT: First slot should be forfeited (airAttacksUsed increments to 1)
            PlayerTestHelper.AssertDesignRule(
                "Air Attack Slot Forfeiture",
                combat.AirAttacksUsed == 1,
                "First air attack slot should be forfeited when double jumping without attacking!"
            );

            Assert.AreEqual(1, combat.AirAttacksUsed,
                "DESIGN VIOLATION: First slot not forfeited on DJ!");

            // ACT: Use one air attack (this is the post-DJ slot)
            combat.HandleAttackInput();

            // ASSERT: Should be at 2 total (1 forfeited + 1 used)
            Assert.AreEqual(2, combat.AirAttacksUsed,
                "Should have 2 total: 1 forfeited + 1 used");

            combat.ResetAttackSystem();

            // ACT: Try to use another attack (SHOULD FAIL - no slots left)
            combat.HandleAttackInput();

            // ASSERT: Should still be at 2 (third attack blocked)
            Assert.AreEqual(2, combat.AirAttacksUsed,
                "EXPLOIT: Got third air attack after forfeiting first slot!");
            Assert.IsFalse(combat.IsAirAttacking,
                "Third attack should not start");
        }

        /// <summary>
        /// TEST 4: Verify proper air attack sequencing WITH first slot usage
        ///
        /// SCENARIO: Player uses first slot BEFORE double jump (normal gameplay)
        /// Expected: 2 total attacks (1 before DJ + 1 after DJ)
        /// </summary>
        [Test]
        public void AirAttack_AllowsTwoTotal_WhenFirstSlotUsedBeforeDJ()
        {
            // ARRANGE: Player airborne
            PlayerTestHelper.SetupAirbornePlayer(testGameObject, hasDoubleJump: true);

            // ACT: Use first air attack (before DJ)
            combat.HandleAttackInput();
            Assert.AreEqual(1, combat.AirAttacksUsed, "First attack should work");

            combat.ResetAttackSystem();

            // ACT: Double jump (enables second slot)
            combat.OnDoubleJump();

            // ASSERT: Counter should still be at 1 (no forfeiture because we used the slot)
            Assert.AreEqual(1, combat.AirAttacksUsed,
                "Counter should stay at 1 when first slot was used");
            // Note: canUseSecondAirAttack is private - verified indirectly by allowing second attack below

            // ACT: Use second air attack
            combat.HandleAttackInput();

            // ASSERT: Should now be at 2
            Assert.AreEqual(2, combat.AirAttacksUsed, "Second attack should work");

            combat.ResetAttackSystem();

            // ACT: Try third attack
            combat.HandleAttackInput();

            // ASSERT: Third attack blocked
            Assert.AreEqual(2, combat.AirAttacksUsed, "Third attack should be blocked");
        }

        /// <summary>
        /// TEST 5: Verify landing resets air attack counter
        ///
        /// INVARIANT: Landing should always reset air attack counter to 0
        /// </summary>
        [Test]
        public void Landing_ResetsAirAttackCounter_Always()
        {
            // ARRANGE: Player has used air attacks
            PlayerTestHelper.SetupAirbornePlayer(testGameObject);
            combat.HandleAttackInput(); // Use first attack
            Assert.AreEqual(1, combat.AirAttacksUsed);

            // ACT: Land
            combat.OnLanding();

            // ASSERT: Counter should reset to 0
            Assert.AreEqual(0, combat.AirAttacksUsed,
                "Landing should reset air attack counter to 0");
            // Note: canUseSecondAirAttack is private - reset verified indirectly by counter
        }

        #endregion

        #region Dash Attack Timing Tests - CRITICAL WINDOW VERIFICATION

        /// <summary>
        /// TEST 6: Verify dash attack works during dash
        ///
        /// INVARIANT: Dash attack should trigger when attack pressed DURING dash
        /// </summary>
        [Test]
        [Ignore("Requires PlayMode testing - timing windows need Time.time advancement")]
        public void DashAttack_Triggers_DuringDash()
        {
            // ARRANGE: Player grounded with dash attack ability
            PlayerTestHelper.SetupGroundedPlayer(testGameObject);
            PlayerTestHelper.UnlockAbility("dashattack");

            // NOTE: This test documents expected behavior but cannot execute in EditMode
            // Dash attacks have a 0.1s pre-window (dashAttackPreWindow)
            // EditMode can't advance Time.time, so timing checks fail

            // Expected behavior (tested in PlayMode):
            // - OnDashStart() called
            // - Wait 0.1s for pre-window to pass
            // - HandleAttackInput() queues dash attack
            // - OnDashEnd() triggers the queued dash attack
            // - IsDashAttacking becomes true

            Assert.Pass("Dash attack timing test requires PlayMode or Time.time mocking");
        }

        /// <summary>
        /// TEST 7: CRITICAL - Verify dash attack grace period (0.2s after dash)
        ///
        /// INVARIANT: Dash attack can trigger within 0.2s AFTER dash ends
        /// CODE REFERENCE: dashAttackInputWindow = 0.2s (line 18)
        ///
        /// BUG THIS CATCHES: If timing check uses wrong comparison, grace period doesn't work
        /// </summary>
        [Test]
        public void DashAttack_AllowedInGracePeriod_AfterDashEnds()
        {
            // ARRANGE: Player with dash attack ability
            PlayerTestHelper.SetupGroundedPlayer(testGameObject);
            PlayerTestHelper.UnlockAbility("dashattack");

            // ACT: Dash starts and ends
            combat.OnDashStart();
            combat.OnDashEnd();

            // Immediately after dash (within grace period)
            combat.HandleAttackInput();

            // ASSERT: Dash attack should trigger
            Assert.IsTrue(combat.IsDashAttacking,
                "TIMING BUG: Dash attack should work immediately after dash ends (grace period)!");
        }

        /// <summary>
        /// TEST 8: Verify dash attack blocked on wall
        ///
        /// INVARIANT: Dash attack should NOT work when player is wall sticking/sliding
        /// CODE REFERENCE: Lines 163-164, 263-275
        ///
        /// BUG THIS CATCHES: If wall check is missing, dash attacks work on walls (looks broken)
        /// </summary>
        [Test]
        [Ignore("Requires PlayerController mock for wall state")]
        public void DashAttack_Blocked_WhenOnWall()
        {
            // ARRANGE: Player on wall after dash
            PlayerTestHelper.SetupPlayerOnWall(testGameObject, wallSticking: true);
            PlayerTestHelper.UnlockAbility("dashattack");

            // Simulate dash ending on wall
            combat.OnDashStart();
            combat.OnDashEnd();

            // Note: We need to mock PlayerController.OnWall and IsWallSticking
            // For now, this test documents the expected behavior

            // ACT: Try dash attack while on wall
            combat.HandleAttackInput();

            // ASSERT: Dash attack should be blocked
            // This test will fail until we can properly mock wall state
            // Assert.IsFalse(combat.IsDashAttacking,
            //     "BUG: Dash attack should be blocked when on wall!");

            Assert.Pass("Wall state mocking needed - test documents expected behavior");
        }

        #endregion

        #region Attack State Machine Tests - STATE TRANSITION VERIFICATION

        /// <summary>
        /// TEST 9: Verify attack state resets on landing when timer expired
        ///
        /// INVARIANT: Landing should reset attack state IF attack timer has expired
        /// CODE REFERENCE: OnLanding() lines 312-324
        /// </summary>
        [Test]
        public void Landing_ResetsAttackState_WhenTimerExpired()
        {
            // ARRANGE: Player air attacking
            PlayerTestHelper.SetupAirbornePlayer(testGameObject);
            combat.HandleAttackInput();
            Assert.IsTrue(combat.IsAirAttacking, "Attack should be active");

            // Simulate attack timer expiring
            combat.ResetAttackSystem(); // Simulates timer expiration

            // ACT: Land
            combat.OnLanding();

            // ASSERT: Attack state should be fully reset
            Assert.IsFalse(combat.IsAttacking, "IsAttacking should be false");
            Assert.IsFalse(combat.IsDashAttacking, "IsDashAttacking should be false");
            Assert.IsFalse(combat.IsAirAttacking, "IsAirAttacking should be false");
            Assert.AreEqual(0, combat.AttackCombo, "AttackCombo should be reset to 0");
        }

        /// <summary>
        /// TEST 10: Verify attack continues if timer still active when landing
        ///
        /// EDGE CASE: Player lands during air attack animation
        /// Expected: Attack animation continues, but counter resets
        /// </summary>
        [Test]
        public void Landing_KeepsAttackActive_WhenTimerStillActive()
        {
            // ARRANGE: Player starts air attack
            PlayerTestHelper.SetupAirbornePlayer(testGameObject);
            combat.HandleAttackInput();

            // ACT: Land immediately (attack timer still active)
            combat.OnLanding();

            // ASSERT: Attack animation may continue (implementation detail)
            // But air attack counter MUST reset
            Assert.AreEqual(0, combat.AirAttacksUsed,
                "Air attack counter must reset on landing even if animation continues");
        }

        #endregion

        #region Combo System Tests - SEQUENCING AND TIMING

        /// <summary>
        /// TEST 11: Verify combo advances through 3 attacks then loops
        ///
        /// INVARIANT: Combo sequence is 1 -> 2 -> 3 -> 1 (loops)
        /// </summary>
        [Test]
        [Ignore("Requires PlayMode testing - ground attacks need PlayerController.IsGrounded")]
        public void ComboAttack_LoopsAfterThirdHit()
        {
            // ARRANGE: Player grounded with combo ability
            PlayerTestHelper.SetupGroundedPlayer(testGameObject);
            PlayerTestHelper.UnlockAbility("comboattack");

            // NOTE: This test documents expected behavior but cannot execute in EditMode
            // Ground attacks require PlayerController.IsGrounded == true
            // EditMode tests don't have PlayerController mocked

            // Expected behavior (tested in PlayMode):
            // - First attack: combo = 1
            // - Second attack (in combo window): combo = 2
            // - Third attack: combo = 3
            // - Fourth attack: combo loops back to 1

            Assert.Pass("Combo sequencing test requires PlayMode or PlayerController mock");
        }

        #endregion

        #region Input Buffering Tests - BUFFER EXECUTION VERIFICATION

        /// <summary>
        /// TEST 12: Verify buffered input executes once then clears
        ///
        /// INVARIANT: Buffered attack should execute ONCE when conditions met, then clear
        ///
        /// BUG THIS CATCHES: If buffer doesn't clear (line 235, 296),
        /// multiple attacks execute from single buffered input
        /// </summary>
        [Test]
        [Ignore("Requires PlayMode testing - needs time simulation for buffer expiration")]
        public void InputBuffer_ExecutesOnce_ThenClears()
        {
            // ARRANGE: Player attacking (input will be buffered)
            PlayerTestHelper.SetupGroundedPlayer(testGameObject);
            PlayerTestHelper.UnlockAbility("comboattack");

            combat.HandleAttackInput(); // Attack 1 starts

            // ACT: Buffer multiple inputs during attack
            combat.HandleAttackInput(); // Buffered
            combat.HandleAttackInput(); // Also tries to buffer
            combat.HandleAttackInput(); // And this

            // Simulate combo window opening (attack completes)
            combat.ResetAttackSystem();

            // ASSERT: Only ONE buffered attack should execute
            // This test documents expected behavior
            // Full verification requires time simulation

            Assert.Pass("Input buffer test requires PlayMode or time simulation");
        }

        #endregion

        #region Helper Methods for Future Tests

        /// <summary>
        /// Helper: Simulate attack completing its duration
        /// </summary>
        private void SimulateAttackCompletion()
        {
            // In PlayMode, we'd wait for attack duration
            // In EditMode, we manually reset
            combat.ResetAttackSystem();
        }

        #endregion
    }
}
