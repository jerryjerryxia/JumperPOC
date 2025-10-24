using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

/// <summary>
/// Camera controller that handles vertical camera adjustment based on W/S input.
/// Works with Cinemachine Virtual Camera to provide smooth camera panning for exploration.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class CameraController : MonoBehaviour
{
    [Header("Camera Adjustment Settings")]
    [SerializeField] private float verticalRange = 2f; // Maximum vertical offset in both directions
    [SerializeField] private float adjustmentSpeed = 3f; // Speed of camera adjustment
    [SerializeField] private float returnSpeed = 2f; // Speed when returning to center
    [SerializeField] private float inputThreshold = 0.1f; // Minimum input to trigger adjustment
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showDebugGizmos = true;
    
    // Component references
    private CinemachineCamera cinemachineCamera;
    private CinemachineFollow followComponent;
    private InputManager inputManager;
    
    // Camera state
    private Vector3 baseFollowOffset;
    private float targetVerticalOffset;
    private float currentVerticalOffset;
    private float verticalInput;
    
    // Properties for external access
    public float CurrentVerticalOffset => currentVerticalOffset;
    public float TargetVerticalOffset => targetVerticalOffset;
    public bool IsAdjusting => Mathf.Abs(verticalInput) > inputThreshold;
    
    void Awake()
    {
        // Get required components
        cinemachineCamera = GetComponent<CinemachineCamera>();
        followComponent = GetComponent<CinemachineFollow>();

        if (cinemachineCamera == null)
        {
            Debug.LogError("[CameraController] CinemachineCamera component not found!");
        }

        if (followComponent == null)
        {
            Debug.LogError("[CameraController] CinemachineFollow component not found!");
        }

        // Subscribe to scene loaded events in Awake() to catch the current scene load
        // This ensures we don't miss the sceneLoaded event that fires before Start()
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Store the base follow offset for reference
        if (followComponent != null)
        {
            baseFollowOffset = followComponent.FollowOffset;
            currentVerticalOffset = baseFollowOffset.y;
            targetVerticalOffset = baseFollowOffset.y;
        }

        // Setup input manager connection
        if (InputManager.Instance != null)
        {
            inputManager = InputManager.Instance;
            inputManager.OnMoveInput += OnMoveInput;
        }
        else
        {
            Debug.LogWarning("[CameraController] InputManager not found! Camera adjustment will not work.");
        }

        // Validate setup
        ValidateSetup();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[CameraController] Scene loaded: {scene.name}, re-validating player target");

        // Small delay to ensure LevelSpawnPoint has positioned the player
        StartCoroutine(RevalidatePlayerTarget());
    }

    private System.Collections.IEnumerator RevalidatePlayerTarget()
    {
        // Wait for LevelSpawnPoint.Start() to execute and position player
        // Use WaitForFixedUpdate to ensure LevelSpawnPoint's VerifySpawnPosition() completes first
        // Then wait one more frame to be absolutely sure the position is stable
        yield return new WaitForFixedUpdate();
        yield return null; // Wait one additional frame for safety

        // Re-find and re-target the player
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && cinemachineCamera != null)
        {
            cinemachineCamera.Target.TrackingTarget = player.transform;
            Debug.Log($"[CameraController] Re-targeted player at position: {player.transform.position}");

            // Force camera to snap to player (no smooth transition)
            // This prevents the camera from being stuck at the old scene position
            if (followComponent != null)
            {
                // Reset camera offset to prevent weird initial state
                ResetCameraOffset();

                // Force Cinemachine to immediately snap to the player's position
                // This bypasses the smooth damping and teleports the camera
                cinemachineCamera.ForceCameraPosition(player.transform.position + followComponent.FollowOffset, cinemachineCamera.transform.rotation);
                Debug.Log($"[CameraController] Forced camera snap to player position");
            }
        }
        else
        {
            Debug.LogWarning("[CameraController] Could not re-find player after scene load!");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from input events
        if (inputManager != null)
        {
            inputManager.OnMoveInput -= OnMoveInput;
        }

        // Unsubscribe from scene events
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void Update()
    {
        UpdateCameraOffset();
        
        if (showDebugInfo)
        {
            LogDebugInfo();
        }
    }
    
    private void OnMoveInput(Vector2 moveInput)
    {
        verticalInput = moveInput.y;
        
        // Calculate target offset based on input and range
        if (Mathf.Abs(verticalInput) > inputThreshold)
        {
            targetVerticalOffset = baseFollowOffset.y + (verticalInput * verticalRange);
        }
        else
        {
            // Return to center when no input
            targetVerticalOffset = baseFollowOffset.y;
        }
    }
    
    private void UpdateCameraOffset()
    {
        if (followComponent == null) return;
        
        // Choose appropriate speed based on whether we're adjusting or returning
        float speed = Mathf.Abs(verticalInput) > inputThreshold ? adjustmentSpeed : returnSpeed;
        
        // Smoothly interpolate to target offset
        currentVerticalOffset = Mathf.Lerp(currentVerticalOffset, targetVerticalOffset, 
            speed * Time.deltaTime);
        
        // Apply the new offset to the camera
        Vector3 newOffset = baseFollowOffset;
        newOffset.y = currentVerticalOffset;
        followComponent.FollowOffset = newOffset;
    }
    
    private void ValidateSetup()
    {
        if (cinemachineCamera == null || followComponent == null)
        {
            Debug.LogError("[CameraController] Missing required Cinemachine components!");
            enabled = false;
            return;
        }
        
        // Check if we have a follow target, and try to find player if missing
        if (cinemachineCamera.Target.TrackingTarget == null)
        {
            // Try to find the persistent player
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                cinemachineCamera.Target.TrackingTarget = player.transform;
                Debug.Log("[CameraController] Automatically found and set persistent player as tracking target.");
            }
            else
            {
                Debug.LogWarning("[CameraController] No tracking target set on Cinemachine camera and no player found!");
            }
        }
        
        // Validate reasonable values
        if (verticalRange <= 0)
        {
            Debug.LogWarning("[CameraController] Vertical range should be greater than 0!");
        }
        
        if (adjustmentSpeed <= 0 || returnSpeed <= 0)
        {
            Debug.LogWarning("[CameraController] Speed values should be greater than 0!");
        }
    }
    
    private void LogDebugInfo()
    {
        if (Time.frameCount % 30 == 0) // Log every 30 frames to avoid spam
        {
            Debug.Log($"[CameraController] Input: {verticalInput:F2}, Target: {targetVerticalOffset:F2}, " +
                     $"Current: {currentVerticalOffset:F2}, IsAdjusting: {IsAdjusting}");
        }
    }
    
    // Public utility methods
    public void SetVerticalRange(float range)
    {
        verticalRange = Mathf.Max(0, range);
    }
    
    public void SetAdjustmentSpeed(float speed)
    {
        adjustmentSpeed = Mathf.Max(0.1f, speed);
    }
    
    public void SetReturnSpeed(float speed)
    {
        returnSpeed = Mathf.Max(0.1f, speed);
    }
    
    public void ResetCameraOffset()
    {
        targetVerticalOffset = baseFollowOffset.y;
        currentVerticalOffset = baseFollowOffset.y;
        
        if (followComponent != null)
        {
            followComponent.FollowOffset = baseFollowOffset;
        }
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || followComponent == null) return;
        
        // Draw camera adjustment range
        Transform followTarget = cinemachineCamera != null ? cinemachineCamera.Target.TrackingTarget : null;
        if (followTarget == null) return;
        
        Vector3 targetPos = followTarget.position;
        
        // Draw vertical adjustment range
        Gizmos.color = Color.cyan;
        Vector3 maxOffset = targetPos + Vector3.up * verticalRange;
        Vector3 minOffset = targetPos + Vector3.down * verticalRange;
        Gizmos.DrawLine(maxOffset, minOffset);
        
        // Draw range indicators
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(maxOffset, 0.1f);
        Gizmos.DrawWireSphere(minOffset, 0.1f);
        Gizmos.DrawWireSphere(targetPos, 0.05f);
        
        // Draw current camera target position
        if (Application.isPlaying)
        {
            Gizmos.color = IsAdjusting ? Color.green : Color.red;
            Vector3 currentTarget = targetPos + Vector3.up * (currentVerticalOffset - baseFollowOffset.y);
            Gizmos.DrawWireSphere(currentTarget, 0.15f);
            
            // Draw arrow showing adjustment direction
            if (IsAdjusting)
            {
                Gizmos.color = Color.white;
                Vector3 direction = Vector3.up * Mathf.Sign(verticalInput) * 0.3f;
                Gizmos.DrawRay(currentTarget, direction);
            }
        }
    }
    
    #if UNITY_EDITOR
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying) return;
        
        // Debug panel for camera adjustment
        GUILayout.BeginArea(new Rect(10, Screen.height - 150, 300, 140));
        GUILayout.Label("=== CAMERA CONTROLLER DEBUG ===", GUI.skin.label);
        
        GUILayout.Label($"Vertical Input: {verticalInput:F2}");
        GUILayout.Label($"Target Offset: {targetVerticalOffset:F2}");
        GUILayout.Label($"Current Offset: {currentVerticalOffset:F2}");
        GUILayout.Label($"Base Offset: {baseFollowOffset.y:F2}");
        
        GUI.contentColor = IsAdjusting ? Color.green : Color.red;
        GUILayout.Label($"Is Adjusting: {IsAdjusting}");
        GUI.contentColor = Color.white;
        
        GUILayout.Label($"Vertical Range: ±{verticalRange:F1}");
        
        GUILayout.EndArea();
    }
    #endif
}