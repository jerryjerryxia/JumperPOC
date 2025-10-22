using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages player progression and abilities in Metroidvania style.
/// Controls which abilities are unlocked and available to the player.
/// </summary>
public class PlayerAbilities : MonoBehaviour
{
    [Header("Movement Abilities")]
    [SerializeField] private bool hasDoubleJump = true; // Start with double jump for testing
    [SerializeField] private bool hasDash = true;
    [SerializeField] private bool hasWallStick = true; // Wall stick enables both wall slide and wall jump
    [SerializeField] private bool hasLedgeGrab = true;
    
    [Header("Combat Abilities")]
    [SerializeField] private bool hasAirAttack = true;
    [SerializeField] private bool hasDashAttack = true;
    [SerializeField] private bool hasComboAttack = true;
    
    [Header("Advanced Abilities - Future")]
    [SerializeField] private bool hasDashJump = true; // Start enabled for testing
    [SerializeField] private bool hasDoubleAirDash = false;
    [SerializeField] private bool hasTripleJump = false;
    [SerializeField] private bool hasGlide = false;
    
    // Events for ability changes
    public static event Action<string, bool> OnAbilityChanged;
    
    // Component references
    private PlayerController playerController;
    private PlayerCombat playerCombat;
    
    // Singleton for easy access
    public static PlayerAbilities Instance { get; private set; }
    
    // Public properties for ability checks
    public bool HasDoubleJump => hasDoubleJump;
    public bool HasDash => hasDash;
    public bool HasWallStick => hasWallStick;
    public bool HasWallSlide => hasWallStick; // Wall slide is enabled when wall stick is unlocked
    public bool HasWallJump => hasWallStick; // Wall jump is enabled when wall stick is unlocked
    public bool HasLedgeGrab => hasLedgeGrab;
    public bool HasAirAttack => hasAirAttack;
    public bool HasDashAttack => hasDashAttack;
    public bool HasComboAttack => hasComboAttack;
    public bool HasDashJump => hasDashJump;
    public bool HasDoubleAirDash => hasDoubleAirDash;
    public bool HasTripleJump => hasTripleJump;
    public bool HasGlide => hasGlide;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Get references
        playerController = GetComponent<PlayerController>();
        playerCombat = GetComponent<PlayerCombat>();
        
        if (playerController == null)
        {
            // Debug.LogError("[PlayerAbilities] PlayerController component not found!");
        }
    }

    #if UNITY_EDITOR || UNITY_INCLUDE_TESTS
    /// <summary>
    /// Initialize singleton for testing - EditMode tests don't call Awake() automatically
    /// </summary>
    public void InitializeForTesting()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    #endif
    
    void Start()
    {
        // Apply initial ability states
        RefreshAllAbilities();
    }
    
    /// <summary>
    /// Unlocks or locks a specific ability
    /// </summary>
    public void SetAbility(string abilityName, bool unlocked)
    {
        bool changed = false;
        
        switch (abilityName.ToLower())
        {
            case "doublejump":
                if (hasDoubleJump != unlocked)
                {
                    hasDoubleJump = unlocked;
                    changed = true;
                    RefreshJumpAbilities();
                }
                break;
                
            case "dash":
                if (hasDash != unlocked)
                {
                    hasDash = unlocked;
                    changed = true;
                    RefreshMovementAbilities();
                }
                break;
                
            case "wallstick":
                if (hasWallStick != unlocked)
                {
                    hasWallStick = unlocked;
                    changed = true;
                    RefreshWallAbilities();
                }
                break;
                
            case "ledgegrab":
                if (hasLedgeGrab != unlocked)
                {
                    hasLedgeGrab = unlocked;
                    changed = true;
                    RefreshInteractionAbilities();
                }
                break;
                
            case "airattack":
                if (hasAirAttack != unlocked)
                {
                    hasAirAttack = unlocked;
                    changed = true;
                    RefreshCombatAbilities();
                }
                break;
                
            case "dashattack":
                if (hasDashAttack != unlocked)
                {
                    hasDashAttack = unlocked;
                    changed = true;
                    RefreshCombatAbilities();
                }
                break;
                
            case "comboattack":
                if (hasComboAttack != unlocked)
                {
                    hasComboAttack = unlocked;
                    changed = true;
                    RefreshCombatAbilities();
                }
                break;
                
            case "dashjump":
                if (hasDashJump != unlocked)
                {
                    hasDashJump = unlocked;
                    changed = true;
                    RefreshMovementAbilities();
                }
                break;
                
            default:
                // Debug.LogWarning($"[PlayerAbilities] Unknown ability: {abilityName}");
                return;
        }
        
        if (changed)
        {
            // Debug.Log($"[PlayerAbilities] {abilityName} {(unlocked ? "unlocked" : "locked")}");
            OnAbilityChanged?.Invoke(abilityName, unlocked);
        }
    }
    
    /// <summary>
    /// Gets the status of a specific ability
    /// </summary>
    public bool GetAbility(string abilityName)
    {
        return abilityName.ToLower() switch
        {
            "doublejump" => hasDoubleJump,
            "dash" => hasDash,
            "wallstick" => hasWallStick,
            "wallslide" => hasWallStick, // Legacy support - both map to wall stick
            "walljump" => hasWallStick,  // Legacy support - both map to wall stick
            "ledgegrab" => hasLedgeGrab,
            "airattack" => hasAirAttack,
            "dashattack" => hasDashAttack,
            "comboattack" => hasComboAttack,
            "dashjump" => hasDashJump,
            "doubleairdash" => hasDoubleAirDash,
            "triplejump" => hasTripleJump,
            "glide" => hasGlide,
            _ => false,
        };
    }
    
    /// <summary>
    /// Toggle an ability on/off (useful for testing)
    /// </summary>
    public void ToggleAbility(string abilityName)
    {
        bool currentState = GetAbility(abilityName);
        SetAbility(abilityName, !currentState);
    }
    
    /// <summary>
    /// Refresh all ability states
    /// </summary>
    public void RefreshAllAbilities()
    {
        RefreshJumpAbilities();
        RefreshMovementAbilities();
        RefreshWallAbilities();
        RefreshInteractionAbilities();
        RefreshCombatAbilities();
    }
    
    private void RefreshJumpAbilities()
    {
        if (playerController == null) return;
        
        // Update extra jumps based on abilities
        int extraJumps = 0;
        if (hasDoubleJump) extraJumps += 1;
        if (hasTripleJump) extraJumps += 1; // Future expansion

        // Set extra jumps directly on PlayerController
        playerController.extraJumps = extraJumps;

        // Debug.Log($"[PlayerAbilities] Jump abilities refreshed - Extra jumps: {extraJumps}");
    }
    
    private void RefreshMovementAbilities()
    {
        if (playerController == null) return;
        
        // Dash abilities are checked at runtime via HasDash property
        // No direct modification needed as PlayerController will query this
        
        // Debug.Log($"[PlayerAbilities] Movement abilities refreshed - Dash: {hasDash}");
    }
    
    private void RefreshWallAbilities()
    {
        if (playerController == null) return;
        
        // Wall abilities are checked at runtime via HasWallSlide/HasWallJump properties
        // No direct modification needed
        
        // Debug.Log($"[PlayerAbilities] Wall abilities refreshed - WallStick: {hasWallStick} (enables slide & jump)");
    }
    
    private void RefreshInteractionAbilities()
    {
        // Ledge grab abilities would be handled by PlayerInteractionDetector
        // Debug.Log($"[PlayerAbilities] Interaction abilities refreshed - LedgeGrab: {hasLedgeGrab}");
    }
    
    private void RefreshCombatAbilities()
    {
        if (playerCombat == null) return;
        
        // Combat abilities are checked at runtime via properties
        // Debug.Log($"[PlayerAbilities] Combat abilities refreshed - AirAttack: {hasAirAttack}, DashAttack: {hasDashAttack}, Combo: {hasComboAttack}");
    }
    
    /// <summary>
    /// Get all current abilities as a dictionary (useful for saving/loading)
    /// </summary>
    public Dictionary<string, bool> GetAllAbilities()
    {
        return new Dictionary<string, bool>
        {
            {"doublejump", hasDoubleJump},
            {"dash", hasDash},
            {"wallstick", hasWallStick},
            {"ledgegrab", hasLedgeGrab},
            {"airattack", hasAirAttack},
            {"dashattack", hasDashAttack},
            {"comboattack", hasComboAttack},
            {"dashjump", hasDashJump},
            {"doubleairdash", hasDoubleAirDash},
            {"triplejump", hasTripleJump},
            {"glide", hasGlide}
        };
    }
    
    /// <summary>
    /// Load abilities from a dictionary (useful for save/load system)
    /// </summary>
    public void LoadAbilities(Dictionary<string, bool> abilities)
    {
        foreach (var ability in abilities)
        {
            SetAbility(ability.Key, ability.Value);
        }
    }
    
    // Debug methods for testing
    #if UNITY_EDITOR
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        // Show ability debug panel in top-right corner
        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 400));
        GUILayout.Label("=== PLAYER ABILITIES ===");
        
        GUILayout.Space(10);
        
        // Movement Abilities
        GUILayout.Label("Movement:");
        if (GUILayout.Button($"Double Jump: {(hasDoubleJump ? "ON" : "OFF")}"))
            ToggleAbility("doublejump");
        if (GUILayout.Button($"Dash: {(hasDash ? "ON" : "OFF")}"))
            ToggleAbility("dash");
        if (GUILayout.Button($"Wall Stick: {(hasWallStick ? "ON" : "OFF")}"))
            ToggleAbility("wallstick");
        if (GUILayout.Button($"Ledge Grab: {(hasLedgeGrab ? "ON" : "OFF")}"))
            ToggleAbility("ledgegrab");
        if (GUILayout.Button($"Dash Jump: {(hasDashJump ? "ON" : "OFF")}"))
            ToggleAbility("dashjump");
            
        GUILayout.Space(10);
        
        // Combat Abilities
        GUILayout.Label("Combat:");
        if (GUILayout.Button($"Air Attack: {(hasAirAttack ? "ON" : "OFF")}"))
            ToggleAbility("airattack");
        if (GUILayout.Button($"Dash Attack: {(hasDashAttack ? "ON" : "OFF")}"))
            ToggleAbility("dashattack");
        if (GUILayout.Button($"Combo Attack: {(hasComboAttack ? "ON" : "OFF")}"))
            ToggleAbility("comboattack");
        
        GUILayout.EndArea();
    }
    #endif
}