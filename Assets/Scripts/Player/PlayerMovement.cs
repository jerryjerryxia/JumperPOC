using UnityEngine;

/// <summary>
/// Handles horizontal movement, slope physics, buffer climbing, and dashing.
/// Manages movement velocity and sprite flipping.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    // Component references (injected by PlayerController)
    private Rigidbody2D rb;
    private Transform playerTransform;
    private PlayerGroundDetection groundDetection;
    private PlayerWallDetection wallDetection;
    private PlayerCombat combat;

    // Public movement state
    public bool IsDashing { get; private set; }
    public bool FacingRight { get; private set; } = true;
    public float RunSpeed { get; private set; }
    public int DashesRemaining { get; private set; }
    public int AirDashesRemaining { get; private set; }
    public float DashTimer { get; private set; }

    /// <summary>
    /// Initialize component references from PlayerController
    /// </summary>
    public void Initialize(Rigidbody2D rigidbody, Transform transform, PlayerGroundDetection ground,
                          PlayerWallDetection wall, PlayerCombat playerCombat)
    {
        rb = rigidbody;
        playerTransform = transform;
        groundDetection = ground;
        wallDetection = wall;
        combat = playerCombat;
    }

    /// <summary>
    /// Handle horizontal movement - called from PlayerController.FixedUpdate()
    /// </summary>
    public void HandleMovement(Vector2 moveInput)
    {
        // To be implemented in Phase 5
    }

    /// <summary>
    /// Handle dash input and execution
    /// </summary>
    public void HandleDash(bool dashQueued)
    {
        // To be implemented in Phase 5
    }

    /// <summary>
    /// Update sprite facing direction
    /// </summary>
    public void UpdateSpriteFacing(Vector2 moveInput)
    {
        // To be implemented in Phase 5
    }
}
