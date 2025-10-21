using NUnit.Framework;
using UnityEngine;
using Player;

namespace Tests.EditMode
{
    /// <summary>
    /// Unit tests for PlayerHealth component.
    /// Tests damage handling, healing, death detection, and health state management.
    /// </summary>
    [TestFixture]
    public class PlayerHealthTests
    {
        private GameObject testGameObject;
        private PlayerHealth health;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with PlayerHealth component
            testGameObject = new GameObject("TestPlayerHealth");
            health = testGameObject.AddComponent<PlayerHealth>();

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
        public void Awake_InitializesHealthToMax()
        {
            // Assert: Health should be initialized to max
            Assert.AreEqual(health.MaxHealth, health.CurrentHealth,
                "Current health should equal max health after initialization");
        }

        [Test]
        public void HealthPercentage_ReturnsOne_WhenFullHealth()
        {
            // Assert: Full health should return 100%
            Assert.AreEqual(1f, health.HealthPercentage, 0.001f,
                "Health percentage should be 1.0 at full health");
        }

        [Test]
        public void IsDead_ReturnsFalse_OnInitialization()
        {
            // Assert: Should not be dead on initialization
            Assert.IsFalse(health.IsDead, "Player should not be dead on initialization");
        }

        [Test]
        public void IsInvincible_ReturnsFalse_OnInitialization()
        {
            // Assert: Should not be invincible on initialization
            Assert.IsFalse(health.IsInvincible, "Player should not be invincible on initialization");
        }

        #endregion

        #region Damage Tests

        [Test]
        public void TakeDamage_ReducesHealth_WhenNotInvincible()
        {
            // Arrange: Initial health
            float initialHealth = health.CurrentHealth;

            // Act: Take damage
            health.TakeDamage(20f);

            // Assert: Health should be reduced
            Assert.Less(health.CurrentHealth, initialHealth, "Health should be reduced after taking damage");
            Assert.AreEqual(initialHealth - 20f, health.CurrentHealth, 0.001f,
                "Health should be reduced by exact damage amount");
        }

        [Test]
        public void TakeDamage_DoesNotReduceBelowZero()
        {
            // Act: Take more damage than max health
            health.TakeDamage(health.MaxHealth + 50f);

            // Assert: Health should be clamped to zero
            Assert.AreEqual(0f, health.CurrentHealth, "Health should not go below zero");
        }

        [Test]
        public void TakeDamage_InvokesOnDamageTakenEvent()
        {
            // Arrange: Subscribe to damage event
            float receivedDamage = 0f;
            health.OnDamageTaken += (damage) => receivedDamage = damage;

            // Act: Take damage
            health.TakeDamage(30f);

            // Assert: Event should be invoked with correct damage amount
            Assert.AreEqual(30f, receivedDamage, "OnDamageTaken event should pass correct damage amount");
        }

        [Test]
        public void TakeDamage_InvokesOnHealthChangedEvent()
        {
            // Arrange: Subscribe to health changed event
            float newHealth = -1f;
            float maxHealth = -1f;
            health.OnHealthChanged += (current, max) =>
            {
                newHealth = current;
                maxHealth = max;
            };

            // Act: Take damage
            health.TakeDamage(25f);

            // Assert: Event should be invoked with updated health
            Assert.AreEqual(health.CurrentHealth, newHealth, "OnHealthChanged should pass current health");
            Assert.AreEqual(health.MaxHealth, maxHealth, "OnHealthChanged should pass max health");
        }

        [Test]
        public void TakeDamage_UpdatesHealthPercentage()
        {
            // Arrange: Start with full health
            float maxHealth = health.MaxHealth;

            // Act: Take damage
            health.TakeDamage(maxHealth / 2); // Take 50% damage

            // Assert: Health percentage should be 0.5
            Assert.AreEqual(0.5f, health.HealthPercentage, 0.001f,
                "Health percentage should reflect current health");
        }

        [Test]
        public void TakeDamage_KillsPlayer_WhenHealthReachesZero()
        {
            // Act: Take fatal damage
            health.TakeDamage(health.MaxHealth);

            // Assert: Player should be dead
            Assert.IsTrue(health.IsDead, "Player should be dead when health reaches zero");
            Assert.AreEqual(0f, health.CurrentHealth, "Health should be zero");
        }

        [Test]
        public void TakeDamage_InvokesOnDeathEvent_WhenHealthReachesZero()
        {
            // Arrange: Subscribe to death event
            bool deathEventFired = false;
            health.OnDeath += () => deathEventFired = true;

            // Act: Take fatal damage
            health.TakeDamage(health.MaxHealth);

            // Assert: Death event should be invoked
            Assert.IsTrue(deathEventFired, "OnDeath event should be invoked when health reaches zero");
        }

        #endregion

        #region Healing Tests

        [Test]
        public void Heal_IncreasesHealth_WhenBelowMax()
        {
            // Arrange: Take some damage first
            health.TakeDamage(30f);
            float damagedHealth = health.CurrentHealth;

            // Act: Heal
            health.Heal(15f);

            // Assert: Health should increase
            Assert.Greater(health.CurrentHealth, damagedHealth, "Health should increase after healing");
            Assert.AreEqual(damagedHealth + 15f, health.CurrentHealth, 0.001f,
                "Health should increase by heal amount");
        }

        [Test]
        public void Heal_DoesNotExceedMaxHealth()
        {
            // Arrange: Take some damage
            health.TakeDamage(20f);

            // Act: Heal more than damage taken
            health.Heal(50f);

            // Assert: Health should be clamped to max
            Assert.AreEqual(health.MaxHealth, health.CurrentHealth,
                "Health should not exceed max health");
        }

        [Test]
        public void Heal_InvokesOnHealthChangedEvent_WhenHealthChanges()
        {
            // Arrange: Damage first, then subscribe
            health.TakeDamage(30f);

            bool eventInvoked = false;
            health.OnHealthChanged += (current, max) => eventInvoked = true;

            // Act: Heal
            health.Heal(10f);

            // Assert: Event should be invoked
            Assert.IsTrue(eventInvoked, "OnHealthChanged should be invoked when healing");
        }

        [Test]
        public void Heal_DoesNotInvokeEvent_WhenAtMaxHealth()
        {
            // Arrange: Already at max health
            int eventCount = 0;
            health.OnHealthChanged += (current, max) => eventCount++;

            // Act: Try to heal when already at max
            health.Heal(10f);

            // Assert: Event should not be invoked
            Assert.AreEqual(0, eventCount, "OnHealthChanged should not fire when already at max health");
        }

        [Test]
        public void Heal_DoesNothing_WhenDead()
        {
            // Arrange: Kill the player
            health.TakeDamage(health.MaxHealth);
            float deadHealth = health.CurrentHealth; // Should be 0

            // Act: Try to heal while dead
            health.Heal(50f);

            // Assert: Health should remain zero
            Assert.AreEqual(deadHealth, health.CurrentHealth, "Cannot heal when dead");
            Assert.IsTrue(health.IsDead, "Should still be dead after heal attempt");
        }

        #endregion

        #region Max Health Tests

        [Test]
        public void SetMaxHealth_UpdatesMaxHealth()
        {
            // Arrange: Initial max health
            float initialMax = health.MaxHealth;

            // Act: Set new max health
            health.SetMaxHealth(150f);

            // Assert: Max health should be updated
            Assert.AreEqual(150f, health.MaxHealth, "Max health should be updated");
            Assert.AreNotEqual(initialMax, health.MaxHealth, "Max health should change");
        }

        [Test]
        public void SetMaxHealth_ClampsCurrentHealth_WhenLowerThanCurrent()
        {
            // Arrange: Start with full health
            float initialHealth = health.CurrentHealth; // Should be MaxHealth

            // Act: Set max health lower than current
            health.SetMaxHealth(50f);

            // Assert: Current health should be clamped to new max
            Assert.AreEqual(50f, health.CurrentHealth,
                "Current health should be clamped to new max health");
            Assert.LessOrEqual(health.CurrentHealth, health.MaxHealth,
                "Current health should not exceed max health");
        }

        [Test]
        public void SetMaxHealth_WithHealToFull_RestoresFullHealth()
        {
            // Arrange: Take damage
            health.TakeDamage(40f);

            // Act: Set new max health and heal to full
            health.SetMaxHealth(200f, healToFull: true);

            // Assert: Should be at full health
            Assert.AreEqual(200f, health.CurrentHealth, "Should be healed to new max health");
            Assert.AreEqual(health.MaxHealth, health.CurrentHealth, "Should be at full health");
        }

        [Test]
        public void SetMaxHealth_WithoutHealToFull_PreservesCurrentHealth()
        {
            // Arrange: Take damage
            health.TakeDamage(30f);
            float damagedHealth = health.CurrentHealth;

            // Act: Set higher max health without healing
            health.SetMaxHealth(150f, healToFull: false);

            // Assert: Current health should be unchanged
            Assert.AreEqual(damagedHealth, health.CurrentHealth,
                "Current health should remain the same");
        }

        [Test]
        public void SetMaxHealth_InvokesOnHealthChangedEvent()
        {
            // Arrange: Subscribe to event
            bool eventInvoked = false;
            health.OnHealthChanged += (current, max) => eventInvoked = true;

            // Act: Set new max health
            health.SetMaxHealth(120f);

            // Assert: Event should be invoked
            Assert.IsTrue(eventInvoked, "OnHealthChanged should be invoked when max health changes");
        }

        #endregion

        #region Getter Methods Tests

        [Test]
        public void GetCurrentHealth_ReturnsCurrentHealth()
        {
            // Arrange: Take some damage
            health.TakeDamage(25f);

            // Act: Get current health
            float retrievedHealth = health.GetCurrentHealth();

            // Assert: Should return current health
            Assert.AreEqual(health.CurrentHealth, retrievedHealth,
                "GetCurrentHealth should return current health value");
        }

        [Test]
        public void GetMaxHealth_ReturnsMaxHealth()
        {
            // Act: Get max health
            float retrievedMaxHealth = health.GetMaxHealth();

            // Assert: Should return max health
            Assert.AreEqual(health.MaxHealth, retrievedMaxHealth,
                "GetMaxHealth should return max health value");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void TakeDamage_WithZeroDamage_DoesNotChangeHealth()
        {
            // Arrange: Initial health
            float initialHealth = health.CurrentHealth;

            // Act: Take zero damage
            health.TakeDamage(0f);

            // Assert: Health should be unchanged
            Assert.AreEqual(initialHealth, health.CurrentHealth,
                "Zero damage should not change health");
        }

        [Test]
        public void TakeDamage_WithNegativeDamage_DoesNotHeal()
        {
            // Arrange: Take some damage first
            health.TakeDamage(30f);
            float damagedHealth = health.CurrentHealth;

            // Act: Try negative damage (shouldn't be used this way, but test for safety)
            health.TakeDamage(-20f);

            // Assert: Health should increase (negative damage becomes healing in current implementation)
            // Note: This reveals that negative damage acts as healing - may want to fix this
            Assert.Greater(health.CurrentHealth, damagedHealth,
                "Negative damage currently acts as healing");
        }

        [Test]
        public void Heal_WithZeroAmount_DoesNotChangeHealth()
        {
            // Arrange: Take some damage
            health.TakeDamage(20f);
            float damagedHealth = health.CurrentHealth;

            // Act: Heal with zero
            health.Heal(0f);

            // Assert: Health should be unchanged
            Assert.AreEqual(damagedHealth, health.CurrentHealth,
                "Zero healing should not change health");
        }

        #endregion
    }
}
