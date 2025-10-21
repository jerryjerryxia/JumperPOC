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
            testGameObject = new GameObject("TestPlayerHealth");
            health = testGameObject.AddComponent<PlayerHealth>();

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

        #region Damage Tests

        [Test]
        public void TakeDamage_ReducesHealth_ByExactAmount()
        {
            // INVARIANT: Damage must reduce health by exact amount
            // BUG THIS CATCHES: Incorrect damage calculation or scaling
            float initialHealth = health.CurrentHealth;

            health.TakeDamage(20f);

            Assert.AreEqual(initialHealth - 20f, health.CurrentHealth, 0.001f,
                "BUG: Health not reduced by exact damage amount");
        }

        [Test]
        public void TakeDamage_DoesNotReduceBelowZero()
        {
            // INVARIANT: Health must clamp to 0, never go negative
            // BUG THIS CATCHES: Missing clamp allowing negative health
            health.TakeDamage(health.MaxHealth + 50f);

            Assert.AreEqual(0f, health.CurrentHealth,
                "BUG: Health went below zero (missing clamp)");
        }

        [Test]
        public void TakeDamage_InvokesOnDamageTakenEvent()
        {
            // INVARIANT: Damage event must fire with correct amount
            // BUG THIS CATCHES: Event not firing, breaking UI/feedback systems
            float receivedDamage = 0f;
            health.OnDamageTaken += (damage) => receivedDamage = damage;

            health.TakeDamage(30f);

            Assert.AreEqual(30f, receivedDamage,
                "BUG: OnDamageTaken event didn't fire or passed wrong value");
        }

        [Test]
        public void TakeDamage_InvokesOnHealthChangedEvent()
        {
            // INVARIANT: Health changed event must fire with current/max values
            // BUG THIS CATCHES: Event not firing, breaking health bars/UI
            float newHealth = -1f;
            float maxHealth = -1f;
            health.OnHealthChanged += (current, max) =>
            {
                newHealth = current;
                maxHealth = max;
            };

            health.TakeDamage(25f);

            Assert.AreEqual(health.CurrentHealth, newHealth,
                "BUG: OnHealthChanged didn't fire or passed wrong current health");
            Assert.AreEqual(health.MaxHealth, maxHealth,
                "BUG: OnHealthChanged passed wrong max health");
        }

        [Test]
        public void TakeDamage_UpdatesHealthPercentage()
        {
            // INVARIANT: Health percentage must reflect current/max ratio
            // BUG THIS CATCHES: Percentage calculation broken
            float maxHealth = health.MaxHealth;

            health.TakeDamage(maxHealth / 2); // Take 50% damage

            Assert.AreEqual(0.5f, health.HealthPercentage, 0.001f,
                "BUG: Health percentage calculation wrong (should be 0.5)");
        }

        [Test]
        public void TakeDamage_KillsPlayer_WhenHealthReachesZero()
        {
            // INVARIANT: Player must die when health reaches 0
            // BUG THIS CATCHES: Death not detected, player survives at 0 HP
            health.TakeDamage(health.MaxHealth);

            Assert.IsTrue(health.IsDead,
                "BUG: Player not marked dead when health reached zero");
            Assert.AreEqual(0f, health.CurrentHealth, "Health should be zero");
        }

        [Test]
        public void TakeDamage_InvokesOnDeathEvent_WhenHealthReachesZero()
        {
            // INVARIANT: Death event must fire when health reaches 0
            // BUG THIS CATCHES: Death event not firing, breaking game state
            bool deathEventFired = false;
            health.OnDeath += () => deathEventFired = true;

            health.TakeDamage(health.MaxHealth);

            Assert.IsTrue(deathEventFired,
                "BUG: OnDeath event didn't fire when health reached zero");
        }

        [Test]
        public void TakeDamage_WithNegativeDamage_ActsAsHealing()
        {
            // BUG DETECTED: Negative damage acts as healing (should it?)
            // This test documents current behavior - may want to fix
            health.TakeDamage(30f);
            float damagedHealth = health.CurrentHealth;

            health.TakeDamage(-20f); // Negative damage

            Assert.Greater(health.CurrentHealth, damagedHealth,
                "DOCUMENTED BUG: Negative damage currently acts as healing");
        }

        #endregion

        #region Healing Tests

        [Test]
        public void Heal_IncreasesHealth_ByExactAmount()
        {
            // INVARIANT: Heal must increase health by exact amount
            // BUG THIS CATCHES: Incorrect heal calculation
            health.TakeDamage(30f);
            float damagedHealth = health.CurrentHealth;

            health.Heal(15f);

            Assert.AreEqual(damagedHealth + 15f, health.CurrentHealth, 0.001f,
                "BUG: Health not increased by exact heal amount");
        }

        [Test]
        public void Heal_DoesNotExceedMaxHealth()
        {
            // INVARIANT: Heal must clamp to MaxHealth
            // BUG THIS CATCHES: Missing clamp allowing health > max
            health.TakeDamage(20f);

            health.Heal(50f); // Heal more than damage taken

            Assert.AreEqual(health.MaxHealth, health.CurrentHealth,
                "BUG: Health exceeded max (missing clamp)");
        }

        [Test]
        public void Heal_InvokesOnHealthChangedEvent_WhenHealthChanges()
        {
            // INVARIANT: Health changed event must fire when healing
            // BUG THIS CATCHES: Event not firing on heal, breaking UI updates
            health.TakeDamage(30f);

            bool eventInvoked = false;
            health.OnHealthChanged += (current, max) => eventInvoked = true;

            health.Heal(10f);

            Assert.IsTrue(eventInvoked,
                "BUG: OnHealthChanged didn't fire when healing");
        }

        [Test]
        public void Heal_DoesNotInvokeEvent_WhenAtMaxHealth()
        {
            // INVARIANT: Event should not fire if no actual change
            // BUG THIS CATCHES: Unnecessary event spam when already at max
            int eventCount = 0;
            health.OnHealthChanged += (current, max) => eventCount++;

            health.Heal(10f); // Already at max

            Assert.AreEqual(0, eventCount,
                "BUG: OnHealthChanged fired when already at max health");
        }

        [Test]
        public void Heal_DoesNothing_WhenDead()
        {
            // INVARIANT: Dead players cannot be healed
            // BUG THIS CATCHES: Healing allowing resurrection
            health.TakeDamage(health.MaxHealth); // Kill player
            float deadHealth = health.CurrentHealth; // Should be 0

            health.Heal(50f); // Try to heal

            Assert.AreEqual(deadHealth, health.CurrentHealth,
                "BUG: Dead player was healed (resurrection exploit)");
            Assert.IsTrue(health.IsDead, "Player should still be dead");
        }

        #endregion

        #region Max Health Tests

        [Test]
        public void SetMaxHealth_UpdatesMaxHealth()
        {
            // INVARIANT: SetMaxHealth must actually change MaxHealth
            // BUG THIS CATCHES: SetMaxHealth not working
            float initialMax = health.MaxHealth;

            health.SetMaxHealth(150f);

            Assert.AreEqual(150f, health.MaxHealth,
                "BUG: SetMaxHealth didn't update MaxHealth");
            Assert.AreNotEqual(initialMax, health.MaxHealth,
                "BUG: MaxHealth unchanged after SetMaxHealth");
        }

        [Test]
        public void SetMaxHealth_ClampsCurrentHealth_WhenLowerThanCurrent()
        {
            // INVARIANT: Current health must clamp to new max if exceeded
            // BUG THIS CATCHES: CurrentHealth > MaxHealth breaking health logic
            float initialHealth = health.CurrentHealth; // Should be MaxHealth

            health.SetMaxHealth(50f); // Set max lower than current

            Assert.AreEqual(50f, health.CurrentHealth,
                "BUG: Current health not clamped to new max");
            Assert.LessOrEqual(health.CurrentHealth, health.MaxHealth,
                "BUG: CurrentHealth > MaxHealth");
        }

        [Test]
        public void SetMaxHealth_WithHealToFull_RestoresFullHealth()
        {
            // INVARIANT: healToFull parameter must restore to new max
            // BUG THIS CATCHES: healToFull parameter not working
            health.TakeDamage(40f);

            health.SetMaxHealth(200f, healToFull: true);

            Assert.AreEqual(200f, health.CurrentHealth,
                "BUG: healToFull didn't restore to new max");
            Assert.AreEqual(health.MaxHealth, health.CurrentHealth,
                "Should be at full health after healToFull");
        }

        [Test]
        public void SetMaxHealth_WithoutHealToFull_PreservesCurrentHealth()
        {
            // INVARIANT: Without healToFull, current health stays same
            // BUG THIS CATCHES: healToFull default value wrong
            health.TakeDamage(30f);
            float damagedHealth = health.CurrentHealth;

            health.SetMaxHealth(150f, healToFull: false);

            Assert.AreEqual(damagedHealth, health.CurrentHealth,
                "BUG: Current health changed when healToFull=false");
        }

        [Test]
        public void SetMaxHealth_InvokesOnHealthChangedEvent()
        {
            // INVARIANT: Changing max health should fire event
            // BUG THIS CATCHES: UI not updated when max health changes
            bool eventInvoked = false;
            health.OnHealthChanged += (current, max) => eventInvoked = true;

            health.SetMaxHealth(120f);

            Assert.IsTrue(eventInvoked,
                "BUG: OnHealthChanged didn't fire when max health changed");
        }

        #endregion
    }
}
