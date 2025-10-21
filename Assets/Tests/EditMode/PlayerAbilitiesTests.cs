using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerAbilities component.
    /// Tests ability unlock system logic, state queries, and dependency rules.
    /// </summary>
    [TestFixture]
    public class PlayerAbilitiesTests
    {
        private GameObject testGameObject;
        private PlayerAbilities abilities;

        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestPlayer");
            abilities = testGameObject.AddComponent<PlayerAbilities>();

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

        #region Ability Logic Tests

        [Test]
        public void HasWallStick_EnablesWallSlideAndWallJump()
        {
            // INVARIANT: Unlocking wall stick automatically enables wall slide and wall jump
            // BUG THIS CATCHES: Missing dependency logic breaking wall mechanics
            abilities.SetAbility("wallstick", true);

            Assert.IsTrue(abilities.HasWallStick, "Wall stick should be unlocked");
            Assert.IsTrue(abilities.HasWallSlide,
                "BUG: Wall slide not enabled when wall stick unlocked");
            Assert.IsTrue(abilities.HasWallJump,
                "BUG: Wall jump not enabled when wall stick unlocked");
        }

        [Test]
        public void GetAbility_ReturnsFalse_ForUnknownAbility()
        {
            // INVARIANT: Unknown abilities return false (safe default)
            // BUG THIS CATCHES: Crash or exception on typos/invalid ability names
            bool result = abilities.GetAbility("unknownability");

            Assert.IsFalse(result,
                "BUG: Unknown ability should return false, not crash or return true");
        }

        [Test]
        public void GetAbility_IsCaseInsensitive()
        {
            // INVARIANT: Ability queries must be case-insensitive
            // BUG THIS CATCHES: Case sensitivity breaking ability checks in code
            abilities.SetAbility("doublejump", true);

            bool lowerCase = abilities.GetAbility("doublejump");
            bool upperCase = abilities.GetAbility("DOUBLEJUMP");
            bool mixedCase = abilities.GetAbility("DoubleJump");

            Assert.IsTrue(lowerCase && upperCase && mixedCase,
                "BUG: Case sensitivity breaks ability queries");
        }

        [Test]
        public void ToggleAbility_FlipsState_Correctly()
        {
            // INVARIANT: Toggle must flip state each time
            // BUG THIS CATCHES: Toggle implementation using = instead of !=
            bool initialState = abilities.HasComboAttack;

            abilities.ToggleAbility("comboattack");
            bool afterFirstToggle = abilities.HasComboAttack;

            abilities.ToggleAbility("comboattack");
            bool afterSecondToggle = abilities.HasComboAttack;

            Assert.AreNotEqual(initialState, afterFirstToggle,
                "BUG: First toggle didn't flip state");
            Assert.AreEqual(initialState, afterSecondToggle,
                "BUG: Second toggle didn't restore original state");
        }

        [Test]
        public void LegacyAbilityNames_MapCorrectly()
        {
            // INVARIANT: Legacy names (wallslide, walljump) must map to wallstick
            // BUG THIS CATCHES: Removing legacy mapping breaks save compatibility
            abilities.SetAbility("wallstick", true);

            bool wallSlideQuery = abilities.GetAbility("wallslide");
            bool wallJumpQuery = abilities.GetAbility("walljump");

            Assert.IsTrue(wallSlideQuery,
                "BUG: 'wallslide' legacy name doesn't map to wallstick");
            Assert.IsTrue(wallJumpQuery,
                "BUG: 'walljump' legacy name doesn't map to wallstick");
        }

        #endregion

        #region Save/Load Tests

        [Test]
        public void GetAllAbilities_ReturnsNewDictionary_NotReference()
        {
            // INVARIANT: GetAllAbilities must return a copy, not internal reference
            // BUG THIS CATCHES: External code modifying internal ability state
            Dictionary<string, bool> dict1 = abilities.GetAllAbilities();
            Dictionary<string, bool> dict2 = abilities.GetAllAbilities();

            Assert.AreNotSame(dict1, dict2,
                "BUG: Returning same dictionary allows external state modification");

            // Modify first dictionary
            dict1["doublejump"] = !dict1["doublejump"];

            // Second dictionary should be unchanged
            Assert.AreNotEqual(dict1["doublejump"], dict2["doublejump"],
                "BUG: Modifying returned dictionary affects internal state");
        }

        [Test]
        public void LoadAbilities_RestoresState_FromDictionary()
        {
            // INVARIANT: LoadAbilities must restore all ability states from dictionary
            // BUG THIS CATCHES: Save/load not working, breaking game persistence
            var savedAbilities = new Dictionary<string, bool>
            {
                {"doublejump", false},
                {"dash", true},
                {"wallstick", false},
                {"airattack", true}
            };

            abilities.LoadAbilities(savedAbilities);

            Assert.IsFalse(abilities.HasDoubleJump,
                "BUG: LoadAbilities didn't restore doublejump state");
            Assert.IsTrue(abilities.HasDash,
                "BUG: LoadAbilities didn't restore dash state");
            Assert.IsFalse(abilities.HasWallStick,
                "BUG: LoadAbilities didn't restore wallstick state");
            Assert.IsTrue(abilities.HasAirAttack,
                "BUG: LoadAbilities didn't restore airattack state");
        }

        #endregion

        #region Smoke Tests

        [Test]
        public void AllMovementAbilities_CanBeUnlocked()
        {
            // SMOKE TEST: Verify all movement abilities can be unlocked
            // BUG THIS CATCHES: Typos or missing abilities in unlock code
            abilities.SetAbility("doublejump", true);
            abilities.SetAbility("dash", true);
            abilities.SetAbility("wallstick", true);
            abilities.SetAbility("ledgegrab", true);
            abilities.SetAbility("dashjump", true);

            Assert.IsTrue(abilities.HasDoubleJump, "Double jump unlock failed");
            Assert.IsTrue(abilities.HasDash, "Dash unlock failed");
            Assert.IsTrue(abilities.HasWallStick, "Wall stick unlock failed");
            Assert.IsTrue(abilities.HasLedgeGrab, "Ledge grab unlock failed");
            Assert.IsTrue(abilities.HasDashJump, "Dash jump unlock failed");
        }

        [Test]
        public void AllCombatAbilities_CanBeUnlocked()
        {
            // SMOKE TEST: Verify all combat abilities can be unlocked
            // BUG THIS CATCHES: Typos or missing abilities in unlock code
            abilities.SetAbility("airattack", true);
            abilities.SetAbility("dashattack", true);
            abilities.SetAbility("comboattack", true);

            Assert.IsTrue(abilities.HasAirAttack, "Air attack unlock failed");
            Assert.IsTrue(abilities.HasDashAttack, "Dash attack unlock failed");
            Assert.IsTrue(abilities.HasComboAttack, "Combo attack unlock failed");
        }

        #endregion
    }
}
