using System.Collections;
using UnityEngine;
using Player;

namespace Enemies
{
    /// <summary>
    /// Simple projectile for enemy ranged attacks.
    /// Moves in a direction, deals damage on player collision, self-destructs.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float speed = 8f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float lifetime = 5f; // Auto-destroy after this many seconds
        [SerializeField] private LayerMask playerLayer = 1 << 0; // Player layer
        [SerializeField] private LayerMask obstacleLayer = 1 << 6; // Ground/obstacles

        [Header("Visual")]
        [SerializeField] private bool rotateTowardsDirection = true;

        private Rigidbody2D rb;
        private Vector2 direction;
        private bool hasHit = false;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.gravityScale = 0f; // Projectiles don't fall
                rb.freezeRotation = !rotateTowardsDirection;
            }
        }

        void Start()
        {
            // Auto-destroy after lifetime
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// Initialize the projectile with a direction and optional custom damage.
        /// Call this immediately after instantiating the projectile.
        /// </summary>
        public void Initialize(Vector2 fireDirection, float customDamage = -1f)
        {
            direction = fireDirection.normalized;

            if (customDamage > 0)
            {
                damage = customDamage;
            }

            // Set velocity
            if (rb != null)
            {
                rb.linearVelocity = direction * speed;
            }

            // Rotate sprite to face direction
            if (rotateTowardsDirection)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        void OnTriggerEnter2D(Collider2D collision)
        {
            if (hasHit) return; // Prevent double-hits

            // Check if hit player
            if (((1 << collision.gameObject.layer) & playerLayer) != 0)
            {
                PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
                if (playerHealth != null && !playerHealth.IsInvincible)
                {
                    playerHealth.TakeDamage(damage);
                    hasHit = true;
                    DestroyProjectile();
                }
            }
            // Check if hit obstacle
            else if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
            {
                hasHit = true;
                DestroyProjectile();
            }
        }

        private void DestroyProjectile()
        {
            // Optional: Spawn impact VFX here
            Destroy(gameObject);
        }

        #region Debug Visualization

        void OnDrawGizmos()
        {
            // Draw projectile direction
            if (Application.isPlaying && rb != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 0.5f);
            }
        }

        #endregion
    }
}
