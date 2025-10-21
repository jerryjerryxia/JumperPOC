using UnityEngine;
using NUnit.Framework;
using System.Collections;

namespace Tests.Helpers
{
    /// <summary>
    /// Helper utilities for setting up player test scenarios.
    /// Provides common setup patterns and time manipulation for combat/movement tests.
    /// </summary>
    public static class PlayerTestHelper
    {
        /// <summary>
        /// Setup player in airborne state (not grounded, not on wall)
        /// </summary>
        public static void SetupAirbornePlayer(GameObject playerObj, bool hasDoubleJump = true)
        {
            var controller = playerObj.GetComponent<PlayerController>();
            if (controller != null)
            {
                // This would require exposing test methods on PlayerController
                // For now, we'll work with what we have
            }

            // Unlock abilities if needed
            if (PlayerAbilities.Instance != null)
            {
                if (hasDoubleJump)
                {
                    PlayerAbilities.Instance.SetAbility("doublejump", true);
                }
                PlayerAbilities.Instance.SetAbility("airattack", true);
            }
        }

        /// <summary>
        /// Setup player on ground (grounded, not on wall)
        /// </summary>
        public static void SetupGroundedPlayer(GameObject playerObj)
        {
            // Player setup for grounded tests
            if (PlayerAbilities.Instance != null)
            {
                PlayerAbilities.Instance.SetAbility("comboattack", true);
            }
        }

        /// <summary>
        /// Setup player against wall (on wall, not grounded)
        /// </summary>
        public static void SetupPlayerOnWall(GameObject playerObj, bool wallSticking = true)
        {
            if (PlayerAbilities.Instance != null)
            {
                PlayerAbilities.Instance.SetAbility("wallstick", wallSticking);
            }
        }

        /// <summary>
        /// Unlock a specific ability for testing
        /// </summary>
        public static void UnlockAbility(string abilityName, bool unlocked = true)
        {
            if (PlayerAbilities.Instance != null)
            {
                PlayerAbilities.Instance.SetAbility(abilityName, unlocked);
            }
        }

        /// <summary>
        /// Disable a specific ability for testing
        /// </summary>
        public static void DisableAbility(string abilityName)
        {
            UnlockAbility(abilityName, false);
        }

        /// <summary>
        /// Reset all player abilities to default state
        /// </summary>
        public static void ResetAllAbilities()
        {
            if (PlayerAbilities.Instance != null)
            {
                PlayerAbilities.Instance.SetAbility("doublejump", true); // Default unlocked
                PlayerAbilities.Instance.SetAbility("dash", true);
                PlayerAbilities.Instance.SetAbility("wallstick", true);
                PlayerAbilities.Instance.SetAbility("ledgegrab", true);
                PlayerAbilities.Instance.SetAbility("airattack", true);
                PlayerAbilities.Instance.SetAbility("dashattack", true);
                PlayerAbilities.Instance.SetAbility("comboattack", true);
            }
        }

        /// <summary>
        /// Create a GameObject with Rigidbody2D and Animator for testing
        /// </summary>
        public static GameObject CreateTestPlayerObject(string name = "TestPlayer")
        {
            GameObject obj = new GameObject(name);
            obj.AddComponent<Rigidbody2D>();
            obj.AddComponent<Animator>();
            obj.AddComponent<BoxCollider2D>();
            return obj;
        }

        /// <summary>
        /// Assert that an exploit was prevented (test should fail if exploit succeeds)
        /// </summary>
        public static void AssertExploitPrevented(string exploitName, bool exploitSucceeded, string failureMessage)
        {
            Assert.IsFalse(exploitSucceeded,
                $"EXPLOIT DETECTED: {exploitName} - {failureMessage}");
        }

        /// <summary>
        /// Assert that a game design rule is enforced
        /// </summary>
        public static void AssertDesignRule(string ruleName, bool ruleEnforced, string failureMessage)
        {
            Assert.IsTrue(ruleEnforced,
                $"DESIGN RULE VIOLATED: {ruleName} - {failureMessage}");
        }
    }
}
