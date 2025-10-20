using UnityEngine;
using System.Collections;

/// <summary>
/// Handles death zone detection, save points, and respawn logic.
/// Manages player position resets and camera synchronization.
/// EXTRACTED FROM PlayerController lines 1374-1507
/// </summary>
public class PlayerRespawnSystem : MonoBehaviour
{
    // Component references
    private Transform playerTransform;
    private Rigidbody2D rb;
    private PlayerCombat combat;

    // Respawn state
    public Vector3 RespawnPosition { get; private set; }
    public Vector3 InitialPosition { get; private set; }
    public bool HasRespawnPoint { get; private set; }

    // External state references (passed from PlayerController)
    private System.Action<float, float, float> onResetDashState;
    private System.Action<float, float, float, bool> onResetJumpState;
    private System.Action onResetCombat;

    /// <summary>
    /// Initialize component references and initial position
    /// </summary>
    public void Initialize(Transform transform, Rigidbody2D rigidbody, PlayerCombat playerCombat)
    {
        playerTransform = transform;
        rb = rigidbody;
        combat = playerCombat;
        InitialPosition = transform.position;
        RespawnPosition = InitialPosition;
        HasRespawnPoint = false;
    }

    /// <summary>
    /// Set callbacks for state reset (called by PlayerController)
    /// </summary>
    public void SetResetCallbacks(
        System.Action<float, float, float> dashStateReset,
        System.Action<float, float, float, bool> jumpStateReset,
        System.Action combatReset)
    {
        onResetDashState = dashStateReset;
        onResetJumpState = jumpStateReset;
        onResetCombat = combatReset;
    }

    /// <summary>
    /// Set a new respawn point (called by save points)
    /// </summary>
    public void SetRespawnPoint(Vector3 newRespawnPosition)
    {
        RespawnPosition = newRespawnPosition;
        HasRespawnPoint = true;
        Debug.Log($"[SavePoint] Respawn point set to: {RespawnPosition}");
    }

    /// <summary>
    /// Check if player has fallen below death zone
    /// </summary>
    public bool IsInDeathZone(float deathZoneY)
    {
        return playerTransform.position.y < deathZoneY;
    }

    /// <summary>
    /// Manual reset for testing
    /// </summary>
    [ContextMenu("Test Reset")]
    public void TestReset()
    {
        // Debug.Log("[Death/Reset] Manual reset triggered");
        ResetToRespawnPoint();
    }

    /// <summary>
    /// Reset to the current respawn point (save point or initial position)
    /// </summary>
    public void ResetToRespawnPoint()
    {
        Vector3 targetPosition;

        // Use SimpleRespawnManager if available and properly initialized, otherwise fall back to existing system
        if (SimpleRespawnManager.Instance != null && SimpleRespawnManager.Instance.IsInitialized())
        {
            targetPosition = SimpleRespawnManager.Instance.GetRespawnPosition();
        }
        else
        {
            targetPosition = HasRespawnPoint ? RespawnPosition : InitialPosition;
        }

        ResetToPosition(targetPosition);
    }

    /// <summary>
    /// Reset to save point position
    /// </summary>
    public void ResetToSavePoint(Vector3 savePosition)
    {
        ResetToPosition(savePosition);
    }

    /// <summary>
    /// Reset to initial position (legacy method)
    /// </summary>
    public void ResetToInitialPosition()
    {
        ResetToPosition(InitialPosition);
    }

    /// <summary>
    /// Core reset method that handles position reset and state cleanup
    /// </summary>
    private void ResetToPosition(Vector3 targetPosition)
    {
        // Debug.Log($"[Death/Reset] BEFORE RESET - Current: {playerTransform.position}, Target: {targetPosition}");

        // Reset physics FIRST to prevent interference
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Force position through both transform and rigidbody
        playerTransform.position = targetPosition;
        rb.position = targetPosition;

        // Force physics update
        Physics2D.SyncTransforms();

        // Double-check position was set
        // Debug.Log($"[Death/Reset] AFTER RESET - Position is now: {playerTransform.position}, RB position: {rb.position}");

        // Reset dash state via callback
        onResetDashState?.Invoke(0f, 0f, 0f); // dashTimer, dashJumpTime, lastDashEndTime all 0

        // Reset jump state via callback
        onResetJumpState?.Invoke(0f, 0f, 0f, false); // unused, unused, unused, no leftGroundByJumping

        // Reset combat system
        onResetCombat?.Invoke();

        // Reset camera position by forcing Cinemachine to snap
        StartCoroutine(ResetCameraPosition());

        // Debug.Log($"[Death/Reset] Player reset to position: {targetPosition}");
    }

    /// <summary>
    /// Reset camera position to follow player
    /// </summary>
    private IEnumerator ResetCameraPosition()
    {
        // Wait one frame for position to be applied
        yield return null;

        // Try to find and reset Cinemachine camera using reflection to avoid compile errors
        var cinemachineType = System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");
        if (cinemachineType != null)
        {
            var vcam = FindFirstObjectByType(cinemachineType);
            if (vcam != null)
            {
                // Use reflection to call OnTargetObjectWarped
                var method = cinemachineType.GetMethod("OnTargetObjectWarped");
                if (method != null)
                {
                    method.Invoke(vcam, new object[] { playerTransform, playerTransform.position - InitialPosition });
                    // Debug.Log("[Death/Reset] Camera position reset via Cinemachine");
                    yield break;
                }
            }
        }

        // Fallback: try to find regular camera and snap it directly
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Simple camera snap to player position
            mainCamera.transform.position = new Vector3(InitialPosition.x, InitialPosition.y, mainCamera.transform.position.z);
            // Debug.Log("[Death/Reset] Camera position reset directly");
        }
    }
}
