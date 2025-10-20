using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;


namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerAbilities component.
    /// Tests ability unlock system, state queries, events, and dictionary operations.
    /// </summary>
    [TestFixture]
    public class PlayerAbilitiesTests
    {
        private GameObject testGameObject;
        private PlayerAbilities abilities;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with PlayerAbilities component
            testGameObject = new GameObject("TestPlayer");
            abilities = testGameObject.AddComponent<PlayerAbilities>();

            // Suppress singleton warnings in tests
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

        #region Basic Unlock/Query Tests

        [Test]
        public void SetAbility_UnlocksDoubleJump_WhenCalled()
        {
            // Arrange: Start with double jump locked
            abilities.SetAbility("doublejump", false);

            // Act: Unlock double jump
            abilities.SetAbility("doublejump", true);

            // Assert: Double jump should be unlocked
            Assert.IsTrue(abilities.HasDoubleJump, "Double jump should be unlocked");
            Assert.IsTrue(abilities.GetAbility("doublejump"), "GetAbility should return true for double jump");
        }

        [Test]
        public void SetAbility_LocksDash_WhenCalledWithFalse()
        {
            // Arrange: Start with dash unlocked
            abilities.SetAbility("dash", true);

            // Act: Lock dash
            abilities.SetAbility("dash", false);

            // Assert: Dash should be locked
            Assert.IsFalse(abilities.HasDash, "Dash should be locked");
            Assert.IsFalse(abilities.GetAbility("dash"), "GetAbility should return false for dash");
        }

        [Test]
        public void HasWallStick_EnablesWallSlideAndWallJump()
        {
            // Arrange & Act: Unlock wall stick
            abilities.SetAbility("wallstick", true);

            // Assert: Wall slide and wall jump should also be enabled
            Assert.IsTrue(abilities.HasWallStick, "Wall stick should be unlocked");
            Assert.IsTrue(abilities.HasWallSlide, "Wall slide should be enabled via wall stick");
            Assert.IsTrue(abilities.HasWallJump, "Wall jump should be enabled via wall stick");
        }

        [Test]
        public void GetAbility_ReturnsFalse_ForUnknownAbility()
        {
            // Act: Query unknown ability
            bool result = abilities.GetAbility("unknownability");

            // Assert: Should return false
            Assert.IsFalse(result, "Unknown ability should return false");
        }

        [Test]
        public void GetAbility_IsCaseInsensitive()
        {
            // Arrange: Unlock double jump
            abilities.SetAbility("doublejump", true);

            // Act: Query with different casing
            bool lowerCase = abilities.GetAbility("doublejump");
            bool upperCase = abilities.GetAbility("DOUBLEJUMP");
            bool mixedCase = abilities.GetAbility("DoubleJump");

            // Assert: All should return true
            Assert.IsTrue(lowerCase, "Lowercase query should work");
            Assert.IsTrue(upperCase, "Uppercase query should work");
            Assert.IsTrue(mixedCase, "Mixed case query should work");
        }

        #endregion

        #region Event Tests

        [Test]
        public void SetAbility_FiresOnAbilityChangedEvent_WhenStateChanges()
        {
            // Arrange: Subscribe to event
            string changedAbility = null;
            bool changedState = false;
            PlayerAbilities.OnAbilityChanged += (ability, state) =>
            {
                changedAbility = ability;
                changedState = state;
            };

            // Act: Change ability state
            abilities.SetAbility("dash", false);

            // Assert: Event should have fired
            Assert.AreEqual("dash", changedAbility, "Event should pass correct ability name");
            Assert.IsFalse(changedState, "Event should pass correct state (false)");

            // Note: Cannot cleanup static event from tests - event will be cleared when domain reloads
        }

        [Test]
        public void SetAbility_DoesNotFireEvent_WhenStateUnchanged()
        {
            // Arrange: Set initial state and subscribe to event
            abilities.SetAbility("doublejump", true);

            int eventFireCount = 0;
            PlayerAbilities.OnAbilityChanged += (ability, state) => eventFireCount++;

            // Act: Set same state again
            abilities.SetAbility("doublejump", true);

            // Assert: Event should not fire
            Assert.AreEqual(0, eventFireCount, "Event should not fire when state is unchanged");

            // Note: Cannot cleanup static event from tests - event will be cleared when domain reloads
        }

        [Test]
        public void ToggleAbility_FiresEvent_TwiceWhenToggledTwice()
        {
            // Arrange: Subscribe to event
            int eventFireCount = 0;
            PlayerAbilities.OnAbilityChanged += (ability, state) => eventFireCount++;

            // Act: Toggle twice
            abilities.ToggleAbility("airattack");
            abilities.ToggleAbility("airattack");

            // Assert: Event should fire twice
            Assert.AreEqual(2, eventFireCount, "Event should fire twice when toggled twice");

            // Note: Cannot cleanup static event from tests - event will be cleared when domain reloads
        }

        [Test]
        public void ToggleAbility_FlipsState_Correctly()
        {
            // Arrange: Get initial state
            bool initialState = abilities.HasComboAttack;

            // Act: Toggle once
            abilities.ToggleAbility("comboattack");
            bool afterFirstToggle = abilities.HasComboAttack;

            // Toggle again
            abilities.ToggleAbility("comboattack");
            bool afterSecondToggle = abilities.HasComboAttack;

            // Assert: State should flip each time
            Assert.AreNotEqual(initialState, afterFirstToggle, "First toggle should flip state");
            Assert.AreEqual(initialState, afterSecondToggle, "Second toggle should restore original state");
        }

        #endregion

        #region Dictionary Operations Tests

        [Test]
        public void GetAllAbilities_ReturnsAllAbilityStates()
        {
            // Arrange: Set specific ability states
            abilities.SetAbility("doublejump", true);
            abilities.SetAbility("dash", false);
            abilities.SetAbility("wallstick", true);

            // Act: Get all abilities
            Dictionary<string, bool> allAbilities = abilities.GetAllAbilities();

            // Assert: Dictionary should contain all abilities with correct states
            Assert.IsNotNull(allAbilities, "GetAllAbilities should not return null");
            Assert.IsTrue(allAbilities.ContainsKey("doublejump"), "Should contain doublejump");
            Assert.IsTrue(allAbilities.ContainsKey("dash"), "Should contain dash");
            Assert.IsTrue(allAbilities.ContainsKey("wallstick"), "Should contain wallstick");

            Assert.IsTrue(allAbilities["doublejump"], "Double jump should be true");
            Assert.IsFalse(allAbilities["dash"], "Dash should be false");
            Assert.IsTrue(allAbilities["wallstick"], "Wall stick should be true");
        }

        [Test]
        public void LoadAbilities_RestoresState_FromDictionary()
        {
            // Arrange: Create ability state dictionary
            var savedAbilities = new Dictionary<string, bool>
            {
                {"doublejump", false},
                {"dash", true},
                {"wallstick", false},
                {"airattack", true}
            };

            // Act: Load abilities
            abilities.LoadAbilities(savedAbilities);

            // Assert: States should match loaded dictionary
            Assert.IsFalse(abilities.HasDoubleJump, "Double jump should be false");
            Assert.IsTrue(abilities.HasDash, "Dash should be true");
            Assert.IsFalse(abilities.HasWallStick, "Wall stick should be false");
            Assert.IsTrue(abilities.HasAirAttack, "Air attack should be true");
        }

        [Test]
        public void LoadAbilities_FiresEvents_ForEachAbilityChange()
        {
            // Arrange: Create state dictionary and track events
            var savedAbilities = new Dictionary<string, bool>
            {
                {"doublejump", false},
                {"dash", false}
            };

            int eventFireCount = 0;
            PlayerAbilities.OnAbilityChanged += (ability, state) => eventFireCount++;

            // Act: Load abilities
            abilities.LoadAbilities(savedAbilities);

            // Assert: Should fire event for each changed ability
            Assert.AreEqual(2, eventFireCount, "Should fire event for each ability in dictionary");

            // Note: Cannot cleanup static event from tests - event will be cleared when domain reloads
        }

        [Test]
        public void GetAllAbilities_ReturnsNewDictionary_NotReference()
        {
            // Act: Get abilities twice
            Dictionary<string, bool> dict1 = abilities.GetAllAbilities();
            Dictionary<string, bool> dict2 = abilities.GetAllAbilities();

            // Assert: Should be different dictionary instances
            Assert.AreNotSame(dict1, dict2, "Should return new dictionary each time, not reference");

            // Modify first dictionary
            dict1["doublejump"] = !dict1["doublejump"];

            // Second dictionary should be unchanged
            Assert.AreNotEqual(dict1["doublejump"], dict2["doublejump"],
                "Modifying one dictionary should not affect the other");
        }

        #endregion

        #region Edge Cases and All Abilities Tests

        [Test]
        public void AllMovementAbilities_CanBeUnlocked()
        {
            // Act: Unlock all movement abilities
            abilities.SetAbility("doublejump", true);
            abilities.SetAbility("dash", true);
            abilities.SetAbility("wallstick", true);
            abilities.SetAbility("ledgegrab", true);
            abilities.SetAbility("dashjump", true);

            // Assert: All should be unlocked
            Assert.IsTrue(abilities.HasDoubleJump, "Double jump should be unlocked");
            Assert.IsTrue(abilities.HasDash, "Dash should be unlocked");
            Assert.IsTrue(abilities.HasWallStick, "Wall stick should be unlocked");
            Assert.IsTrue(abilities.HasLedgeGrab, "Ledge grab should be unlocked");
            Assert.IsTrue(abilities.HasDashJump, "Dash jump should be unlocked");
        }

        [Test]
        public void AllCombatAbilities_CanBeUnlocked()
        {
            // Act: Unlock all combat abilities
            abilities.SetAbility("airattack", true);
            abilities.SetAbility("dashattack", true);
            abilities.SetAbility("comboattack", true);

            // Assert: All should be unlocked
            Assert.IsTrue(abilities.HasAirAttack, "Air attack should be unlocked");
            Assert.IsTrue(abilities.HasDashAttack, "Dash attack should be unlocked");
            Assert.IsTrue(abilities.HasComboAttack, "Combo attack should be unlocked");
        }

        [Test]
        public void SetAbility_HandlesUnknownAbility_Gracefully()
        {
            // Act: Try to set unknown ability (should log warning but not crash)
            abilities.SetAbility("superpower", true);

            // Assert: Known abilities should be unaffected
            Assert.IsTrue(abilities.HasDoubleJump, "Known abilities should remain unchanged");
        }

        [Test]
        public void LegacyAbilityNames_MapCorrectly()
        {
            // Arrange: Use legacy names that should map to wall stick
            abilities.SetAbility("wallstick", true);

            // Act: Query using legacy names
            bool wallSlideQuery = abilities.GetAbility("wallslide");
            bool wallJumpQuery = abilities.GetAbility("walljump");

            // Assert: Legacy names should map to wall stick ability
            Assert.IsTrue(wallSlideQuery, "wallslide should map to wallstick");
            Assert.IsTrue(wallJumpQuery, "walljump should map to wallstick");
        }

        #endregion
    }
}
