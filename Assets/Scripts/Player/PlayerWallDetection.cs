using UnityEngine;

/// <summary>
/// Handles all wall detection for the player including wall contact, sliding, and sticking.
/// Manages wall state and wall normal calculations.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerWallDetection : MonoBehaviour
{
    // Component references (injected by PlayerController)
    private Rigidbody2D rb;
    private Collider2D col;
    private Transform playerTransform;
    private PlayerAbilities abilities;

    // Public wall state
    public bool OnWall { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsWallSticking { get; private set; }
    public bool WallContact { get; private set; }
    public bool WallStickAllowed { get; private set; }
    public Vector2 WallNormal { get; private set; }

    // State tracking
    public bool WasWallSticking { get; private set; }
    public bool HasEverWallStuck { get; private set; }

    /// <summary>
    /// Initialize component references from PlayerController
    /// </summary>
    public void Initialize(Rigidbody2D rigidbody, Collider2D collider, Transform transform, PlayerAbilities playerAbilities)
    {
        rb = rigidbody;
        col = collider;
        playerTransform = transform;
        abilities = playerAbilities;
    }

    /// <summary>
    /// Main detection method - called from PlayerController.FixedUpdate()
    /// </summary>
    public void CheckWallDetection()
    {
        // To be implemented in Phase 3
    }
}
