using UnityEngine;

/// <summary>
/// Simple trigger zone that initiates level transitions when player enters.
/// Integrates seamlessly with existing GameManager and save system.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelTransition : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private string targetSceneName = "Level2_CommercialArea";
    [SerializeField] private string spawnPointId = "LevelEntry";
    
    [Header("Trigger Settings")]
    [SerializeField] private bool requireMovementDirection = true;
    [SerializeField] private Vector2 requiredDirection = Vector2.right;
    [SerializeField] private float directionThreshold = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;
    
    private bool isTransitioning = false;
    private float transitionCooldown = 1f;
    private float lastTransitionTime = -999f;
    
    private void Awake()
    {
        // Ensure we have a trigger collider
        var collider = GetComponent<Collider2D>();
        if (collider != null && !collider.isTrigger)
        {
            collider.isTrigger = true;
            Debug.LogWarning($"LevelTransition on {name}: Collider was not set as trigger. Fixed automatically.");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || isTransitioning) return;
        
        // Cooldown to prevent rapid transitions
        if (Time.time - lastTransitionTime < transitionCooldown) return;
        
        var playerController = other.GetComponent<PlayerController>();
        if (playerController == null) return;
        
        // Check movement direction if required
        if (requireMovementDirection && !IsMovingInRequiredDirection(playerController))
        {
            return;
        }
        
        // Initiate transition
        InitiateTransition();
    }
    
    private bool IsMovingInRequiredDirection(PlayerController player)
    {
        // Get player's current velocity or movement input
        var velocity = player.GetComponent<Rigidbody2D>()?.linearVelocity ?? Vector2.zero;
        
        // Check if moving in the required direction
        float dot = Vector2.Dot(velocity.normalized, requiredDirection.normalized);
        return dot > directionThreshold;
    }
    
    private void InitiateTransition()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"LevelTransition on {name}: Target scene name not set!");
            return;
        }
        
        isTransitioning = true;
        lastTransitionTime = Time.time;
        
        Debug.Log($"Initiating transition to {targetSceneName} with spawn point {spawnPointId}");
        
        // Store transition data for the target scene
        LevelTransitionManager.SetPendingTransition(spawnPointId);
        
        // Use existing GameManager to load scene, or fallback to direct SceneManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("GameManager not found! Using direct SceneManager.LoadScene() as fallback.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
        }
    }
    
    // Editor tool support methods
    public void SetTransitionData(string sceneName, string spawnId)
    {
        targetSceneName = sceneName;
        spawnPointId = spawnId;
    }
    
    public string GetTargetScene() => targetSceneName;
    public string GetSpawnPointId() => spawnPointId;
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        var collider = GetComponent<BoxCollider2D>();
        if (collider == null) return;
        
        Vector3 center = transform.position + (Vector3)collider.offset;
        Vector3 size = collider.size;
        
        // Simple transition zone
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f); // Blue
        Gizmos.DrawCube(center, size);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(center, size);
        
        // Direction arrow if enabled
        if (requireMovementDirection)
        {
            Gizmos.color = Color.white;
            Vector3 arrowEnd = center + (Vector3)requiredDirection * 0.5f;
            Gizmos.DrawLine(center, arrowEnd);
            
            // Simple arrow head
            Vector3 arrowHead1 = arrowEnd + (Vector3)(Quaternion.Euler(0, 0, 135) * requiredDirection) * 0.2f;
            Vector3 arrowHead2 = arrowEnd + (Vector3)(Quaternion.Euler(0, 0, -135) * requiredDirection) * 0.2f;
            Gizmos.DrawLine(arrowEnd, arrowHead1);
            Gizmos.DrawLine(arrowEnd, arrowHead2);
        }
        
        #if UNITY_EDITOR
        // Simple label
        Vector3 labelPos = center + Vector3.up * (size.y * 0.5f + 0.5f);
        var style = new GUIStyle();
        style.normal.textColor = Color.cyan;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        UnityEditor.Handles.Label(labelPos, $"TRANSITION → {targetSceneName}", style);
        #endif
    }
}