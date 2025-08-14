using System.Collections;
using UnityEngine;
using Enemies;

public class AttackHitbox : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private LayerMask enemyLayers = -1;
    
    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private Vector2 knockbackDirection = Vector2.right;
    
    [Header("Hit Detection")]
    [SerializeField] private bool canHitMultipleEnemies = true;
    [SerializeField] private bool oneHitPerAttack = true;
    
    // Components
    private Collider2D hitboxCollider;
    private PlayerCombat playerCombat;
    
    // State
    private bool isActive = false;
    private System.Collections.Generic.HashSet<GameObject> hitEnemies = new System.Collections.Generic.HashSet<GameObject>();
    
    // Attack type for different hitbox configurations
    public enum AttackType
    {
        GroundStab,     // Attack 1 - forward thrust
        GroundChop,     // Attack 2 - overhead swing  
        GroundSlash,    // Attack 3 - horizontal slice
        DashAttack,     // Dash/Air attack - double slash
        AirAttack       // Air attack (same as dash)
    }
    
    [Header("Attack Type")]
    [SerializeField] private AttackType attackType = AttackType.GroundStab;
    
    [Header("Attack Configurations")]
    [Space(10)]
    
    [Header("Ground Stab (Attack 1)")]
    [SerializeField] private Vector2 stabSize = new Vector2(0.58f, 0.3f);
    [SerializeField] private Vector2 stabOffset = new Vector2(0.15f, 0.15f);
    [SerializeField] private float stabDamage = 25f;
    [SerializeField] private float stabKnockback = 8f;
    
    [Header("Ground Chop (Attack 2)")]
    [SerializeField] private Vector2 chopSize = new Vector2(0.45f, 0.43f);
    [SerializeField] private Vector2 chopOffset = new Vector2(0.1f, 0.21f);
    [SerializeField] private float chopDamage = 30f;
    [SerializeField] private float chopKnockback = 10f;
    
    [Header("Ground Slash (Attack 3)")]
    [SerializeField] private Vector2 slashSize = new Vector2(0.5f, 0.3f);
    [SerializeField] private Vector2 slashOffset = new Vector2(0.15f, 0.15f);
    [SerializeField] private float slashDamage = 35f;
    [SerializeField] private float slashKnockback = 12f;
    
    [Header("Dash/Air Attack")]
    [SerializeField] private Vector2 dashSize = new Vector2(0.6f, 0.3f);
    [SerializeField] private Vector2 dashOffset = new Vector2(0.1f, 0.15f);
    [SerializeField] private float dashDamage = 40f;
    [SerializeField] private float dashKnockback = 15f;
    
    // Note: BoxCollider2D size/offset are auto-configured by this script - don't modify manually!
    
    private void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
        playerCombat = GetComponentInParent<PlayerCombat>();
        
        if (hitboxCollider == null)
        {
            Debug.LogError($"AttackHitbox on {gameObject.name} requires a Collider2D component!");
        }
        
        // Set as trigger for hit detection
        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
        }
        
        // Set to PlayerHitbox layer for proper collision detection with enemies
        // Assumes PlayerHitbox layer exists - if not, this will be ignored
        int playerHitboxLayer = LayerMask.NameToLayer("PlayerHitbox");
        if (playerHitboxLayer != -1)
        {
            gameObject.layer = playerHitboxLayer;
        }
        else
        {
            Debug.LogWarning($"PlayerHitbox layer not found! AttackHitbox on {gameObject.name} may not collide with enemies properly. Please create a 'PlayerHitbox' layer and set it to collide with the Enemy layer.");
        }
        
        // Configure hitbox size and position based on attack type
        ConfigureHitboxForAttackType();
        
        // Start disabled
        SetActive(false);
    }
    
    private void ConfigureHitboxForAttackType()
    {
        if (hitboxCollider == null) return;
        
        // Configure size and offset based on attack type
        Vector2 size;
        Vector2 offset;
        
        switch (attackType)
        {
            case AttackType.GroundStab:
                size = stabSize;
                offset = stabOffset;
                damage = stabDamage;
                knockbackForce = stabKnockback;
                break;
                
            case AttackType.GroundChop:
                size = chopSize;
                offset = chopOffset;
                damage = chopDamage;
                knockbackForce = chopKnockback;
                break;
                
            case AttackType.GroundSlash:
                size = slashSize;
                offset = slashOffset;
                damage = slashDamage;
                knockbackForce = slashKnockback;
                break;
                
            case AttackType.DashAttack:
            case AttackType.AirAttack:
                size = dashSize;
                offset = dashOffset;
                damage = dashDamage;
                knockbackForce = dashKnockback;
                break;
                
            default:
                size = stabSize;
                offset = stabOffset;
                damage = stabDamage;
                knockbackForce = stabKnockback;
                break;
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
            // Clear hit enemies for new attack
            hitEnemies.Clear();
            
            // Update knockback direction based on player facing
            if (playerCombat != null)
            {
                PlayerController playerController = playerCombat.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    knockbackDirection = playerController.FacingRight ? Vector2.right : Vector2.left;
                }
            }
        }
    }
    
    public void SetAttackType(AttackType type)
    {
        attackType = type;
        ConfigureHitboxForAttackType();
    }
    
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        
        // Check if it's an enemy
        if (((1 << other.gameObject.layer) & enemyLayers) == 0) return;
        
        // Prevent multiple hits on same enemy per attack
        if (oneHitPerAttack && hitEnemies.Contains(other.gameObject)) return;
        
        // Try to deal damage
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            // Deal damage
            enemy.TakeDamage(damage);
            
            // Apply knockback
            ApplyKnockback(other.gameObject);
            
            // Track hit enemy
            if (oneHitPerAttack)
            {
                hitEnemies.Add(other.gameObject);
            }
            
            // Visual/audio feedback could go here
            // Debug.Log($"Player hit {enemy.name} for {damage} damage with {attackType}!");
            
            // Disable hitbox if we only want one hit per attack and we don't allow multiple enemies
            if (!canHitMultipleEnemies)
            {
                SetActive(false);
            }
        }
    }
    
    private void ApplyKnockback(GameObject target)
    {
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null && knockbackForce > 0f)
        {
            Vector2 knockback = knockbackDirection.normalized * knockbackForce;
            targetRb.AddForce(knockback, ForceMode2D.Impulse);
        }
    }
    
    // Debug visualization
    private void OnDrawGizmos()
    {
        if (hitboxCollider == null) return;
        
        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
        
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
        
        // Draw attack type label
        if (hitboxCollider != null)
        {
            Vector3 labelPos = transform.TransformPoint(hitboxCollider.offset) + Vector3.up * 0.5f;
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPos, attackType.ToString());
            #endif
        }
    }
    
    // Editor utility method to apply changes during development
    [ContextMenu("Apply Current Attack Type Configuration")]
    private void ApplyCurrentConfiguration()
    {
        ConfigureHitboxForAttackType();
        // Debug.Log($"Applied {attackType} configuration - Size: {damage}, Damage: {damage}");
    }
}