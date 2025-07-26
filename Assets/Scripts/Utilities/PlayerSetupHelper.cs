using UnityEngine;
using Player;

/// <summary>
/// Helper script to automatically set up Player with PlayerController + PlayerCombat
/// Attach this to your Player GameObject and it will configure everything automatically
/// </summary>
public class PlayerSetupHelper : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool setupOnAwake = true;
    [SerializeField] private bool createAttackHitbox = true;
    [SerializeField] private Vector2 hitboxSize = new Vector2(0.3f, 0.3f);
    [SerializeField] private Vector2 hitboxOffset = new Vector2(0.2f, 0.15f);
    
    private void Awake()
    {
        if (setupOnAwake)
        {
            SetupPlayer();
        }
    }
    
    [ContextMenu("Setup Player")]
    public void SetupPlayer()
    {
        GameObject player = gameObject;
        
        // Ensure required components
        EnsureComponent<PlayerController>();
        EnsureComponent<PlayerCombat>();
        EnsureComponent<PlayerHealth>();
        EnsureComponent<Rigidbody2D>();
        EnsureComponent<Animator>();
        EnsureComponent<Collider2D>();
        
        // Configure Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3f; // Adjust as needed
        
        // Set up attack hitbox
        if (createAttackHitbox)
        {
            SetupAttackHitbox();
        }
        
        // Configure PlayerCombat reference
        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat.AttackHitbox == null)
        {
            GameObject hitbox = transform.Find("AttackHitbox")?.gameObject;
            if (hitbox != null)
            {
                combat.AttackHitbox = hitbox.GetComponent<AttackHitbox>();
            }
        }
        
        Debug.Log("Player setup complete! PlayerController + PlayerCombat configured.");
    }
    
    private T EnsureComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
            Debug.Log($"Added {typeof(T).Name} component");
        }
        return component;
    }
    
    private void SetupAttackHitbox()
    {
        // Check if hitbox already exists
        Transform existing = transform.Find("AttackHitbox");
        if (existing != null)
        {
            Debug.Log("AttackHitbox already exists");
            return;
        }
        
        // Create attack hitbox
        GameObject hitbox = new GameObject("AttackHitbox");
        hitbox.transform.SetParent(transform);
        hitbox.transform.localPosition = new Vector3(hitboxOffset.x, hitboxOffset.y, 0);
        
        // Add and configure collider
        BoxCollider2D hitboxCollider = hitbox.AddComponent<BoxCollider2D>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.size = hitboxSize;
        
        // Set to PlayerHitbox layer for collision with enemies
        int playerHitboxLayer = LayerMask.NameToLayer("PlayerHitbox");
        if (playerHitboxLayer != -1)
        {
            hitbox.layer = playerHitboxLayer;
        }
        else
        {
            Debug.LogWarning("PlayerHitbox layer not found! Please create a 'PlayerHitbox' layer in Tags & Layers and set it to collide with Enemy layer.");
        }
        
        // Add the AttackHitbox component (this will also auto-set the layer)
        AttackHitbox attackHitboxComponent = hitbox.AddComponent<AttackHitbox>();
        
        // Keep GameObject active, AttackHitbox will manage collider
        hitbox.SetActive(true);
        
        Debug.Log("Attack hitbox created and configured");
    }
    
    [ContextMenu("Remove Setup Helper")]
    public void RemoveSetupHelper()
    {
        // Remove this component after setup is complete
        if (Application.isPlaying)
        {
            Destroy(this);
        }
        else
        {
            DestroyImmediate(this);
        }
    }
}