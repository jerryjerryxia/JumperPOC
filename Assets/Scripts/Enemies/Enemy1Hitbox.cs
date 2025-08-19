using System.Collections;
using UnityEngine;
using Player;

namespace Enemies
{
    public class Enemy1Hitbox : MonoBehaviour
    {
    [Header("Damage Settings")]
    [SerializeField] private LayerMask playerLayers = -1;
    
    [Header("Knockback")]
    [SerializeField] private Vector2 knockbackDirection = Vector2.right;
    
    [Header("Hit Detection")]
    [SerializeField] private bool canHitMultipleTimes = false;
    [SerializeField] private bool oneHitPerAttack = true;
    
    [Header("Lance Chop Attack Configuration")]
    [SerializeField] private Vector2 chopSize = new Vector2(0.4f, 0.6f);
    [SerializeField] private Vector2 chopOffset = new Vector2(0.2f, 0.3f);
    [SerializeField] private float chopDamage = 15f;
    [SerializeField] private float chopKnockback = 10f;
    
    // Note: BoxCollider2D size/offset are auto-configured by this script - don't modify manually!
    
    // Components
    private Collider2D hitboxCollider;
    private Enemy1Controller enemy1Controller;
    private IEnemyBase enemyBase; // For compatibility with new enemy system
    
    // State
    private bool isActive = false;
    private System.Collections.Generic.HashSet<GameObject> hitPlayers = new System.Collections.Generic.HashSet<GameObject>();
    private bool previousFacingRight = true;
    
    // Properties
    public bool IsActive => isActive;
    
    private void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
        enemy1Controller = GetComponentInParent<Enemy1Controller>();
        enemyBase = GetComponentInParent<IEnemyBase>(); // Support new enemy system
        
        if (hitboxCollider == null)
        {
            // Debug.LogError($"Enemy1Hitbox on {gameObject.name} requires a Collider2D component!");
        }
        
        // Set as trigger for hit detection
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false; // Start with collider disabled
        }
        
        // Set to EnemyHitbox layer for collision with player
        // Assumes EnemyHitbox layer exists - if not, this will be ignored
        int enemyHitboxLayer = LayerMask.NameToLayer("EnemyHitbox");
        if (enemyHitboxLayer != -1)
        {
            gameObject.layer = enemyHitboxLayer;
            
            // IMPORTANT: Make sure EnemyHitbox layer does NOT collide with Ground layer (6)
            // This prevents the hitbox from interfering with enemy's edge/ground detection
            if (enemyHitboxLayer == 6)
            {
                // Debug.LogError("EnemyHitbox layer should NOT be the same as Ground layer (6)! This will break enemy patrol behavior.");
            }
        }
        else
        {
            // Debug.LogWarning($"EnemyHitbox layer not found! Enemy1Hitbox on {gameObject.name} may not collide with player properly. Please create an 'EnemyHitbox' layer and set it to collide with the player layer.");
        }
        
        // Configure hitbox size and position
        ConfigureHitbox();
        
        // Start disabled
        SetActive(false);
    }
    
    private void Start()
    {
        // Initialize facing direction after all components are ready
        if (enemy1Controller != null)
        {
            previousFacingRight = enemy1Controller.IsFacingRight;
        }
        else if (enemyBase != null)
        {
            previousFacingRight = enemyBase.IsFacingRight;
        }
    }
    
    private void LateUpdate()
    {
        // Update hitbox position if enemy changes facing direction
        // Use LateUpdate to avoid interfering with enemy movement logic
        // Check both enemy controller types for compatibility
        bool currentFacing = GetEnemyFacingDirection();
        if (currentFacing != previousFacingRight)
        {
            previousFacingRight = currentFacing;
            // Only reconfigure if hitbox collider exists
            if (hitboxCollider != null)
            {
                ConfigureHitbox();
            }
        }
    }
    
    private bool GetEnemyFacingDirection()
    {
        if (enemy1Controller != null)
        {
            return enemy1Controller.IsFacingRight;
        }
        else if (enemyBase != null)
        {
            return enemyBase.IsFacingRight;
        }
        return previousFacingRight; // Fallback to last known direction
    }
    
    private void ConfigureHitbox()
    {
        if (hitboxCollider == null) return;
        
        // Use the lance chop configuration
        Vector2 size = chopSize;
        Vector2 offset = chopOffset;
        
        // Flip the X offset if enemy is facing left
        bool facingRight = GetEnemyFacingDirection();
        if (!facingRight)
        {
            offset.x = -offset.x;
        }
        
        // Apply to BoxCollider2D if present
        if (hitboxCollider is BoxCollider2D boxCollider)
        {
            boxCollider.size = size;
            boxCollider.offset = offset;
        }
        // Apply to CircleCollider2D if present
        else if (hitboxCollider is CircleCollider2D circleCollider)
        {
            circleCollider.radius = Mathf.Max(size.x, size.y) * 0.5f;
            circleCollider.offset = offset;
        }
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = active;
        }
        
        if (active)
        {
            // Clear hit players for new attack
            hitPlayers.Clear();
            
            // Reconfigure hitbox position based on current facing direction
            ConfigureHitbox();
            
            // Update knockback direction based on enemy facing
            bool facingRight = GetEnemyFacingDirection();
            knockbackDirection = facingRight ? Vector2.right : Vector2.left;
        }
    }
    
    public void SetDamage(float newDamage)
    {
        chopDamage = newDamage;
    }
    
    public void SetKnockback(float newKnockback)
    {
        chopKnockback = newKnockback;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        
        // Check if it's a player
        if (((1 << other.gameObject.layer) & playerLayers) == 0) return;
        
        // Prevent multiple hits on same player per attack
        if (oneHitPerAttack && hitPlayers.Contains(other.gameObject)) return;
        
        // Try to deal damage
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // Check if player is invincible
            if (playerHealth.IsInvincible) return;
            
            // Deal damage
            playerHealth.TakeDamage(chopDamage);
            
            // Apply knockback
            ApplyKnockback(other.gameObject);
            
            // Track hit player
            if (oneHitPerAttack)
            {
                hitPlayers.Add(other.gameObject);
            }
            
            // Visual/audio feedback could go here
            // Debug.Log($"Enemy1 hit {other.name} for {chopDamage} damage with lance chop!");
            
            // Disable hitbox if we only want one hit per attack and we don't allow multiple hits
            if (!canHitMultipleTimes)
            {
                SetActive(false);
            }
        }
    }
    
    private void ApplyKnockback(GameObject target)
    {
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null && chopKnockback > 0f)
        {
            Vector2 knockback = knockbackDirection.normalized * chopKnockback;
            targetRb.AddForce(knockback, ForceMode2D.Impulse);
        }
    }
    
    // Public methods for Enemy1Controller to use
    public void StartLanceChopAttack()
    {
        SetActive(true);
        // Could add attack duration timer here if needed
        // Debug.Log("Enemy1 lance chop attack started!");
    }
    
    public void EndLanceChopAttack()
    {
        SetActive(false);
        // Debug.Log("Enemy1 lance chop attack ended!");
    }
    
    // Debug visualization
    private void OnDrawGizmos()
    {
        if (hitboxCollider == null) return;
        
        Gizmos.color = isActive ? Color.red : new Color(1f, 0.5f, 0f, 0.5f); // Red when active, orange when inactive
        
        if (hitboxCollider is BoxCollider2D boxCollider)
        {
            Vector3 worldPos = transform.TransformPoint(boxCollider.offset);
            Vector3 worldSize = Vector3.Scale(boxCollider.size, transform.lossyScale);
            Gizmos.DrawCube(worldPos, worldSize);
        }
        else if (hitboxCollider is CircleCollider2D circleCollider)
        {
            Vector3 worldPos = transform.TransformPoint(circleCollider.offset);
            float worldRadius = circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            Gizmos.DrawSphere(worldPos, worldRadius);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        OnDrawGizmos();
        
        // Draw attack label
        if (hitboxCollider != null)
        {
            Vector3 labelPos = transform.TransformPoint(hitboxCollider.offset) + Vector3.up * 0.8f;
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPos, "Lance Chop");
            #endif
        }
    }
    
    // Editor utility method to apply changes during development
    [ContextMenu("Apply Hitbox Configuration")]
    private void ApplyConfiguration()
    {
        ConfigureHitbox();
        // Debug.Log($"Applied Enemy1 lance chop configuration - Size: {chopSize}, Offset: {chopOffset}, Damage: {chopDamage}");
    }
    
    // Called when values are changed in the Inspector
    private void OnValidate()
    {
        // Only run in editor and when we have a collider
        if (Application.isPlaying) return;
        
        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponent<Collider2D>();
        }
        
        if (hitboxCollider != null)
        {
            ConfigureHitbox();
        }
    }
    }
}