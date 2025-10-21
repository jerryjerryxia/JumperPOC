using NUnit.Framework;
using UnityEngine;
using Tests.Helpers;

namespace Tests.EditMode
{
    /// <summary>
    /// DIAGNOSTIC TESTS - These tests help us understand WHY the main tests are failing
    /// by checking each condition individually
    /// </summary>
    [TestFixture]
    public class PlayerCombatDiagnosticTests
    {
        private GameObject testGameObject;
        private PlayerCombat combat;
        private Rigidbody2D rb;
        private Animator animator;

        [SetUp]
        public void SetUp()
        {
            // Same setup as PlayerCombatTests
            testGameObject = new GameObject("TestPlayer");
            rb = testGameObject.AddComponent<Rigidbody2D>();
            animator = testGameObject.AddComponent<Animator>();
            testGameObject.AddComponent<BoxCollider2D>();

            combat = testGameObject.AddComponent<PlayerCombat>();
            combat.InitializeForTesting(rb, animator, null);

            if (PlayerAbilities.Instance == null)
            {
                GameObject abilitiesObj = new GameObject("PlayerAbilities");
                PlayerAbilities abilities = abilitiesObj.AddComponent<PlayerAbilities>();
                abilities.InitializeForTesting();
            }

            PlayerTestHelper.ResetAllAbilities();
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
                var field = typeof(PlayerAbilities).GetField("Instance",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(null, null);
                }
            }

            Debug.unityLogger.logEnabled = true;
        }

        [Test]
        public void Diagnostic_PlayerAbilitiesExists()
        {
            Assert.IsNotNull(PlayerAbilities.Instance, "PlayerAbilities.Instance is NULL!");
            Assert.IsTrue(PlayerAbilities.Instance.HasAirAttack,
                $"HasAirAttack is FALSE! Value: {PlayerAbilities.Instance.HasAirAttack}");
        }

        [Test]
        public void Diagnostic_CombatInitialState()
        {
            Assert.IsFalse(combat.IsAttacking, $"IsAttacking should be false, but is: {combat.IsAttacking}");
            Assert.IsFalse(combat.HasUsedAirAttack, $"HasUsedAirAttack should be false, but is: {combat.HasUsedAirAttack}");
            Assert.AreEqual(0, combat.AirAttacksUsed, $"AirAttacksUsed should be 0, but is: {combat.AirAttacksUsed}");
        }

        [Test]
        public void Diagnostic_HandleAttackInput_DoesItRunAtAll()
        {
            // This test just calls HandleAttackInput and sees if ANYTHING changes
            int initialAttackCount = combat.AirAttacksUsed;
            bool initialAttackingState = combat.IsAttacking;

            combat.HandleAttackInput();

            bool attackCountChanged = combat.AirAttacksUsed != initialAttackCount;
            bool attackStateChanged = combat.IsAttacking != initialAttackingState;

            Assert.IsTrue(attackCountChanged || attackStateChanged,
                $"HandleAttackInput() did NOTHING! " +
                $"AirAttacksUsed: {initialAttackCount} -> {combat.AirAttacksUsed}, " +
                $"IsAttacking: {initialAttackingState} -> {combat.IsAttacking}");
        }

        [Test]
        public void Diagnostic_WhyIsAirAttackNotWorking()
        {
            // Check each condition that HandleAttackInput checks for air attacks
            // This will tell us WHICH condition is failing

            string failureReason = "";

            // The actual conditions from HandleAttackInput (lines 212-219)
            bool actuallyOnWall = false; // playerController is null, so this is false
            bool isGrounded = false; // playerController is null, so this is false
            bool isAttacking = combat.IsAttacking;
            bool isDashAttacking = combat.IsDashAttacking;
            bool isDashing = false; // playerController is null, so this is false
            bool hasUsedAirAttack = combat.HasUsedAirAttack;

            // Calculate airAttackSlotAvailable (line 207)
            int airAttacksUsed = combat.AirAttacksUsed;
            bool canUseSecondAirAttack = false; // This is private, we'll assume false initially
            bool airAttackSlotAvailable = (airAttacksUsed < 1) || (airAttacksUsed == 1 && canUseSecondAirAttack);

            bool hasPlayerAbilities = PlayerAbilities.Instance != null;
            bool hasAirAttackAbility = hasPlayerAbilities && PlayerAbilities.Instance.HasAirAttack;

            // Build the full condition
            bool fullCondition = !actuallyOnWall && !isGrounded && !isAttacking && !isDashAttacking && !isDashing &&
                !hasUsedAirAttack && airAttackSlotAvailable &&
                hasPlayerAbilities && hasAirAttackAbility;

            // Report which conditions failed
            if (actuallyOnWall) failureReason += "✗ actuallyOnWall is TRUE (should be false)\n";
            if (isGrounded) failureReason += "✗ isGrounded is TRUE (should be false)\n";
            if (isAttacking) failureReason += "✗ isAttacking is TRUE (should be false)\n";
            if (isDashAttacking) failureReason += "✗ isDashAttacking is TRUE (should be false)\n";
            if (isDashing) failureReason += "✗ isDashing is TRUE (should be false)\n";
            if (hasUsedAirAttack) failureReason += "✗ hasUsedAirAttack is TRUE (should be false)\n";
            if (!airAttackSlotAvailable) failureReason += $"✗ airAttackSlotAvailable is FALSE (airAttacksUsed={airAttacksUsed}, canUseSecondAirAttack={canUseSecondAirAttack})\n";
            if (!hasPlayerAbilities) failureReason += "✗ PlayerAbilities.Instance is NULL\n";
            if (hasPlayerAbilities && !hasAirAttackAbility) failureReason += "✗ PlayerAbilities.Instance.HasAirAttack is FALSE\n";

            string successConditions = "";
            if (!actuallyOnWall) successConditions += "✓ actuallyOnWall is false\n";
            if (!isGrounded) successConditions += "✓ isGrounded is false\n";
            if (!isAttacking) successConditions += "✓ isAttacking is false\n";
            if (!isDashAttacking) successConditions += "✓ isDashAttacking is false\n";
            if (!isDashing) successConditions += "✓ isDashing is false\n";
            if (!hasUsedAirAttack) successConditions += "✓ hasUsedAirAttack is false\n";
            if (airAttackSlotAvailable) successConditions += $"✓ airAttackSlotAvailable is true (airAttacksUsed={airAttacksUsed})\n";
            if (hasPlayerAbilities) successConditions += "✓ PlayerAbilities.Instance exists\n";
            if (hasAirAttackAbility) successConditions += "✓ HasAirAttack is true\n";

            Assert.IsTrue(fullCondition,
                $"Air attack condition CHECK FAILED!\n\n" +
                $"SUCCESS:\n{successConditions}\n" +
                $"FAILURES:\n{(string.IsNullOrEmpty(failureReason) ? "None - but full condition is still false?\n" : failureReason)}");
        }
    }
}
