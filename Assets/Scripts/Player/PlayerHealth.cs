using System;
using UnityEngine;

namespace Player
{
    public class PlayerHealth : MonoBehaviour
    {
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f; // Default to max health for EditMode tests
    
    [Header("Damage Settings")]
    [SerializeField] private float invincibilityDuration = 1f;
    [SerializeField] private int invincibilityFlashCount = 5;
    
    // Events
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action<float> OnDamageTaken; // damage amount
    public event Action OnDeath;
    
    // Components
    private PlayerController playerController;
    private PlayerCombat playerCombat;
    private SpriteRenderer spriteRenderer;
    
    // State
    private bool isInvincible = false;
    private bool isDead = false;
    
    // Properties
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsDead => isDead;
    public bool IsInvincible => isInvincible;
    
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        currentHealth = maxHealth;
    }
    
    private void Start()
    {
        // Notify UI of initial health
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead || isInvincible) return;
        
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityCoroutine());
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        if (currentHealth != previousHealth)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
    
    public void SetMaxHealth(float newMaxHealth, bool healToFull = false)
    {
        maxHealth = newMaxHealth;
        if (healToFull)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Disable player controls
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        if (playerCombat != null)
        {
            playerCombat.enabled = false;
        }
        
        // Trigger death event
        OnDeath?.Invoke();
        
        // Play death animation if available
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            // Check if Die parameter exists before setting it
            foreach (var param in animator.parameters)
            {
                if (param.name == "Die" && param.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger("Die");
                    break;
                }
            }
        }
        
        // Handle death (respawn, game over, etc.)
        // This can be expanded based on game requirements
        Invoke(nameof(HandleDeathComplete), 2f);
    }
    
    private void HandleDeathComplete()
    {
        // Reset player using save point system or initial position
        if (playerController != null)
        {
            playerController.ResetToRespawnPoint();
        }
        
        // Reset health state
        isDead = false;
        currentHealth = maxHealth;
        
        // Re-enable player controls
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        if (playerCombat != null)
        {
            playerCombat.enabled = true;
        }
        
        // Notify UI of health reset
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    private System.Collections.IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        
        // Flash the sprite
        float flashDuration = invincibilityDuration / (invincibilityFlashCount * 2);
        
        for (int i = 0; i < invincibilityFlashCount; i++)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
                yield return new WaitForSeconds(flashDuration);
                spriteRenderer.enabled = true;
                yield return new WaitForSeconds(flashDuration);
            }
        }
        
        isInvincible = false;
    }
    
    // Debug method for testing
    [ContextMenu("Take 10 Damage")]
    private void Debug_Take10Damage()
    {
        TakeDamage(10f);
    }
    
    [ContextMenu("Heal 20 HP")]
    private void Debug_Heal20HP()
    {
        Heal(20f);
    }
    
    // Public getters for UI
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    }
}