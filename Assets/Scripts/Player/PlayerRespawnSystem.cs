using UnityEngine;

/// <summary>
/// Handles death zone detection, save points, and respawn logic.
/// Manages player position resets.
/// </summary>
public class PlayerRespawnSystem : MonoBehaviour
{
    // Component references
    private Transform playerTransform;
    private Rigidbody2D rb;

    // Respawn state
    public Vector3 RespawnPosition { get; private set; }
    public Vector3 InitialPosition { get; private set; }
    public bool HasRespawnPoint { get; private set; }

    /// <summary>
    /// Initialize component references and initial position
    /// </summary>
    public void Initialize(Transform transform, Rigidbody2D rigidbody)
    {
        playerTransform = transform;
        rb = rigidbody;
        InitialPosition = transform.position;
        RespawnPosition = InitialPosition;
    }

    /// <summary>
    /// Set a new respawn point (save point)
    /// </summary>
    public void SetRespawnPoint(Vector3 position)
    {
        // To be implemented in Phase 6
    }

    /// <summary>
    /// Respawn player at current respawn point
    /// </summary>
    public void Respawn()
    {
        // To be implemented in Phase 6
    }

    /// <summary>
    /// Check if player has fallen below death zone
    /// </summary>
    public bool IsInDeathZone(float deathZoneY)
    {
        return playerTransform.position.y < deathZoneY;
    }
}
