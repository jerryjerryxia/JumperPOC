using UnityEngine;
using Enemies;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Simple head stomp system that creates a separate trigger collider
/// on a layer that interacts with the player. When player falls into
/// the trigger, they get an automatic velocity boost (like auto-jump).
/// 
/// Supports IEnemyBase interface for enemy integration.
/// </summary>
public class SimpleHeadStomp : MonoBehaviour
{
    [Header("Stomp Settings")]
    [SerializeField] private float bounceForce = 18f; // Jump force applied to player
    [SerializeField] private float minimumFallSpeed = -2f; // Player must be falling
    // Head stomp never damages enemy - just bounces player
    
    [Header("Detection")]
    [SerializeField] private Vector2 triggerPosition = new Vector2(0f, 0.6f); // Local position offset from enemy center
    [SerializeField] private Vector2 triggerSize = new Vector2(0.2f, 0.01f); // Size of trigger box
    [SerializeField] private LayerMask playerLayer = -1; // What layer is the player on?
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private Color hitColor = Color.red;
    
    private IEnemyBase enemyInterface;
    private Collider2D enemyCollider;
    private bool lastFrameHit = false;
    
    void Awake()
    {
        // Get IEnemyBase interface for enemy integration
        enemyInterface = GetComponent<IEnemyBase>();
        
        enemyCollider = GetComponent<Collider2D>();
        
        // Check if we have the interface
        if (enemyInterface == null)
        {
            // Debug.LogWarning($"[SimpleHeadStomp] {name} requires IEnemyBase component!");
        }
        
        if (enemyCollider == null)
        {
            // Debug.LogError($"[SimpleHeadStomp] {name} requires Collider2D component!");
        }
        
        // Auto-configure player layer if not set
        if (playerLayer == -1)
        {
            // Try to get player layer from IEnemyBase interface
            if (enemyInterface != null)
            {
                playerLayer = enemyInterface.PlayerLayer;
                if (enableDebugLogging)
                {
                    // Debug.Log($"[SimpleHeadStomp] Auto-configured player layer from IEnemyBase: {playerLayer}");
                }
            }
            
            // Final fallback: try common player layers
            if (playerLayer == -1)
            {
                int playerLayerNum = LayerMask.NameToLayer("Player");
                if (playerLayerNum == -1)
                    playerLayerNum = LayerMask.NameToLayer("PlayerHitbox");
                if (playerLayerNum == -1)
                    playerLayerNum = 0; // Default layer
                    
                playerLayer = 1 << playerLayerNum;
                if (enableDebugLogging)
                {
                    // Debug.Log($"[SimpleHeadStomp] Auto-configured player layer fallback: {playerLayerNum} (mask: {playerLayer})");
                }
            }
        }
        
        // Create head stomp trigger automatically
        CreateHeadStompTrigger();
        
        if (enableDebugLogging)
        {
            string systemType = enemyInterface != null ? "IEnemyBase" : "None";
            // Debug.Log($"[SimpleHeadStomp] Initialized on {name} using {systemType}:");
            // Debug.Log($"  • Bounce Force: {bounceForce}");
            // Debug.Log($"  • Min Fall Speed: {minimumFallSpeed}");
            // Debug.Log($"  • Trigger Position: {triggerPosition}");
            // Debug.Log($"  • Trigger Size: {triggerSize}");
            // Debug.Log($"  • Player Layer: {playerLayer}");
        }
    }
    
    private void CreateHeadStompTrigger()
    {
        // Check if trigger already exists as child
        Transform existingTrigger = transform.Find("HeadStompTrigger");
        if (existingTrigger != null)
        {
            if (Application.isPlaying)
                Destroy(existingTrigger.gameObject);
            else
                DestroyImmediate(existingTrigger.gameObject);
        }
        
        // Create new trigger GameObject as child of this enemy
        GameObject triggerObj = new GameObject("HeadStompTrigger");
        triggerObj.transform.SetParent(transform);
        triggerObj.layer = LayerMask.NameToLayer("HeadStompTrigger"); // Dedicated head stomp layer
        
        // Add trigger collider
        BoxCollider2D triggerCollider = triggerObj.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        
        // Position and size the trigger box using local coordinates
        SetupTriggerBox(triggerObj, triggerCollider);
        
        // Add trigger handler component
        HeadStompTriggerHandler handler = triggerObj.AddComponent<HeadStompTriggerHandler>();
        handler.Initialize(this);
        
        if (enableDebugLogging)
        {
            // Debug.Log($"[SimpleHeadStomp] Created head stomp trigger as child of {name} on HeadStompTrigger layer");
        }
    }
    
    private void SetupTriggerBox(GameObject triggerObj, BoxCollider2D triggerCollider)
    {
        // Position trigger using inspector-configurable offset
        Vector3 localPosition = new Vector3(
            triggerPosition.x, // X offset from enemy center
            triggerPosition.y, // Y offset from enemy center
            0f
        );
        
        // Apply to trigger (local position automatically follows parent)
        triggerObj.transform.localPosition = localPosition;
        triggerCollider.size = triggerSize;
        triggerCollider.offset = Vector2.zero;
    }
    
    // Note: Head stomp detection now happens through the separate trigger child object
    // These methods are kept for backwards compatibility but shouldn't trigger in normal use
    
    // Removed - using trigger-based system now
    
    private void ProcessPotentialStompFromTrigger(Collider2D other)
    {
        // Get player components
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;
        
        Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;
        
        // Check if head stomp is available
        if (!player.CanHeadStomp)
        {
            if (enableDebugLogging)
            {
                // Debug.Log($"[SimpleHeadStomp] Head stomp not available - must land or wall stick first");
            }
            return;
        }
        
        // Check if player is falling
        if (playerRb.linearVelocity.y > minimumFallSpeed) 
        {
            if (enableDebugLogging)
            {
                // Debug.Log($"[SimpleHeadStomp] Player not falling fast enough: {playerRb.linearVelocity.y} > {minimumFallSpeed}");
            }
            return;
        }
        
        // No position check needed - if player is in the trigger, they're above the enemy by design
        
        // Consume the head stomp
        player.ConsumeHeadStomp();
        
        // Execute stomp!
        if (enableDebugLogging)
        {
            // Debug.Log($"[SimpleHeadStomp] ✓ STOMP EXECUTED (Trigger)! Applying bounce force: {bounceForce}");
        }
        ExecuteStomp(playerRb);
        lastFrameHit = true;
    }
    
    // Called by HeadStompTriggerHandler when player enters trigger
    public void OnPlayerEnterTrigger(Collider2D player)
    {
        if (enableDebugLogging)
            // Debug.Log($"[SimpleHeadStomp] Player entered head stomp trigger on {name}");
            
        ProcessPotentialStompFromTrigger(player);
    }
    
    private void ExecuteStomp(Rigidbody2D playerRb)
    {
        // Apply automatic velocity boost (like auto-jump but higher)
        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, bounceForce);
        
        // Never damage the enemy - just boost the player
        // This acts like an automatic high jump when player lands on enemy
        
        // Visual feedback
        lastFrameHit = true;
        
        // Optional: Add effects here (particles, sound, etc.)
        if (enableDebugLogging)
        {
            // Debug.Log($"[SimpleHeadStomp] ✅ AUTO-JUMP TRIGGERED! Velocity set to: {bounceForce}");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || enemyCollider == null) return;
        
        // Draw head stomp detection box visualization
        Vector3 enemyCenter = enemyCollider.bounds.center;
        
        // Calculate trigger position in world space
        Vector3 triggerWorldPos = transform.position + new Vector3(triggerPosition.x, triggerPosition.y, 0f);
        
        // Use hit color if we just had a successful stomp, otherwise use gizmo color
        Color currentColor = lastFrameHit ? hitColor : gizmoColor;
        
        // Draw filled box for head stomp area
        Gizmos.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0.3f);
        Gizmos.DrawCube(triggerWorldPos, triggerSize);
        
        // Draw wire outline for clarity
        Gizmos.color = currentColor;
        Gizmos.DrawWireCube(triggerWorldPos, triggerSize);
        
        // Draw connection line between enemy center and head stomp trigger
        Gizmos.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0.7f);
        Gizmos.DrawLine(enemyCenter, triggerWorldPos);
        
#if UNITY_EDITOR
        // Add label for head stomp area
        var labelStyle = new GUIStyle();
        labelStyle.normal.textColor = currentColor;
        labelStyle.fontSize = 9;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        UnityEditor.Handles.Label(triggerWorldPos + Vector3.up * 0.2f, "HEAD STOMP", labelStyle);
#endif
        
        // Reset hit indicator after one frame
        if (Application.isPlaying && lastFrameHit)
        {
            lastFrameHit = false;
        }
    }
}