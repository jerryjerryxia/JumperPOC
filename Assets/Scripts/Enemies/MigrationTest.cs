using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Test script to verify SimpleEnemy implementation is working correctly.
    /// Attach to any GameObject to run basic validation in play mode.
    /// This script validates that the enemy system is using the IEnemyBase interface correctly.
    /// </summary>
    public class MigrationTest : MonoBehaviour
    {
        [Header("Enemy System Verification")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private bool logDetailedResults = true;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                TestEnemySystem();
            }
        }
        
        [ContextMenu("Test Enemy System")]
        public void TestEnemySystem()
        {
            Debug.Log("=== Enemy System Validation Test ===");
            
            // Find all SimpleEnemy instances
            SimpleEnemy[] simpleEnemies = FindObjectsByType<SimpleEnemy>(FindObjectsSortMode.None);
            Debug.Log($"Found {simpleEnemies.Length} SimpleEnemy instances");
            
            // Verify all enemies implement IEnemyBase interface
            int interfaceImplementations = 0;
            foreach (var enemy in simpleEnemies)
            {
                if (enemy is IEnemyBase)
                {
                    interfaceImplementations++;
                }
            }
            
            Debug.Log($"✅ {interfaceImplementations}/{simpleEnemies.Length} enemies implement IEnemyBase interface");
            
            // Test each SimpleEnemy instance
            int workingEnemies = 0;
            foreach (var enemy in simpleEnemies)
            {
                if (TestEnemyInstance(enemy))
                {
                    workingEnemies++;
                }
            }
            
            Debug.Log($"=== Enemy System Test Results ===");
            Debug.Log($"SimpleEnemy instances: {simpleEnemies.Length}");
            Debug.Log($"Working correctly: {workingEnemies}");
            Debug.Log($"IEnemyBase implementations: {interfaceImplementations}");
            
            if (interfaceImplementations == simpleEnemies.Length && workingEnemies == simpleEnemies.Length)
            {
                Debug.Log("🎉 ENEMY SYSTEM VALIDATION SUCCESSFUL! All enemies implement IEnemyBase and are working correctly.");
            }
            else
            {
                Debug.LogWarning("⚠️ Enemy system may have issues. Check logs above for details.");
            }
        }
        
        private bool TestEnemyInstance(SimpleEnemy enemy)
        {
            if (enemy == null) return false;
            
            bool isWorking = true;
            string enemyName = enemy.gameObject.name;
            
            try
            {
                // Test basic properties
                float health = enemy.GetCurrentHealth();
                float maxHealth = enemy.GetMaxHealth();
                bool isDead = enemy.IsDead;
                bool isChasing = enemy.IsChasing;
                bool isAttacking = enemy.IsAttacking;
                
                if (logDetailedResults)
                {
                    Debug.Log($"✅ {enemyName}: Health={health}/{maxHealth}, Dead={isDead}, Chasing={isChasing}, Attacking={isAttacking}");
                }
                
                // Test component integration
                var hitbox = enemy.GetComponentInChildren<Enemy1Hitbox>();
                if (hitbox != null)
                {
                    if (logDetailedResults)
                    {
                        Debug.Log($"✅ {enemyName}: Enemy1Hitbox found and integrated");
                    }
                }
                else if (logDetailedResults)
                {
                    Debug.LogWarning($"⚠️ {enemyName}: No Enemy1Hitbox found - attacks may not work properly");
                }
                
                // Test required components
                var rb = enemy.GetComponent<Rigidbody2D>();
                var animator = enemy.GetComponent<Animator>();
                var spriteRenderer = enemy.GetComponent<SpriteRenderer>();
                
                if (rb == null || animator == null || spriteRenderer == null)
                {
                    Debug.LogError($"❌ {enemyName}: Missing required components! RB={rb != null}, Animator={animator != null}, SpriteRenderer={spriteRenderer != null}");
                    isWorking = false;
                }
                else if (logDetailedResults)
                {
                    Debug.Log($"✅ {enemyName}: All required components present");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ {enemyName}: Exception during testing - {ex.Message}");
                isWorking = false;
            }
            
            return isWorking;
        }
    }
}