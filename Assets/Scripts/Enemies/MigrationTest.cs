using UnityEngine;

namespace Enemies
{
    /// <summary>
    /// Test script to verify SimpleEnemy migration is working correctly.
    /// Attach to any GameObject to run basic validation in play mode.
    /// </summary>
    public class MigrationTest : MonoBehaviour
    {
        [Header("Migration Verification")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private bool logDetailedResults = true;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                TestSimpleEnemyMigration();
            }
        }
        
        [ContextMenu("Test SimpleEnemy Migration")]
        public void TestSimpleEnemyMigration()
        {
            Debug.Log("=== SimpleEnemy Migration Test ===");
            
            // Find all SimpleEnemy instances
            SimpleEnemy[] simpleEnemies = FindObjectsByType<SimpleEnemy>(FindObjectsSortMode.None);
            Debug.Log($"Found {simpleEnemies.Length} SimpleEnemy instances");
            
            // Find any remaining Enemy1Controller instances
            Enemy1Controller[] oldEnemies = FindObjectsByType<Enemy1Controller>(FindObjectsSortMode.None);
            
            if (oldEnemies.Length > 0)
            {
                Debug.LogError($"MIGRATION INCOMPLETE: {oldEnemies.Length} Enemy1Controller instances still exist!");
                foreach (var enemy in oldEnemies)
                {
                    Debug.LogError($"  - {enemy.gameObject.name} still uses Enemy1Controller", enemy.gameObject);
                }
            }
            else
            {
                Debug.Log("✅ No Enemy1Controller instances found - migration successful!");
            }
            
            // Test each SimpleEnemy instance
            int workingEnemies = 0;
            foreach (var enemy in simpleEnemies)
            {
                if (TestEnemyInstance(enemy))
                {
                    workingEnemies++;
                }
            }
            
            Debug.Log($"=== Migration Test Results ===");
            Debug.Log($"SimpleEnemy instances: {simpleEnemies.Length}");
            Debug.Log($"Working correctly: {workingEnemies}");
            Debug.Log($"Old Enemy1Controller instances: {oldEnemies.Length}");
            
            if (oldEnemies.Length == 0 && workingEnemies == simpleEnemies.Length)
            {
                Debug.Log("🎉 MIGRATION SUCCESSFUL! All enemies are using SimpleEnemy and working correctly.");
            }
            else
            {
                Debug.LogWarning("⚠️ Migration may have issues. Check logs above for details.");
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