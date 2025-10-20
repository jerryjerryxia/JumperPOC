using UnityEngine;

/// <summary>
/// Calculates and tracks all movement states (running, jumping, falling, etc.).
/// Provides unified state access for other systems.
/// </summary>
public class PlayerStateTracker : MonoBehaviour
{
    // Component references
    private Rigidbody2D rb;
    private PlayerGroundDetection groundDetection;
    private PlayerWallDetection wallDetection;
    private PlayerMovement movement;
    private PlayerJumpSystem jumpSystem;
    private PlayerCombat combat;

    // Public movement states
    public bool IsRunning { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsFalling { get; private set; }
    public bool IsClimbing { get; set; }
    public bool IsLedgeGrabbing { get; set; }
    public bool IsLedgeClimbing { get; set; }

    /// <summary>
    /// Initialize component references
    /// </summary>
    public void Initialize(Rigidbody2D rigidbody, PlayerGroundDetection ground, PlayerWallDetection wall,
                          PlayerMovement playerMovement, PlayerJumpSystem jump, PlayerCombat playerCombat)
    {
        rb = rigidbody;
        groundDetection = ground;
        wallDetection = wall;
        movement = playerMovement;
        jumpSystem = jump;
        combat = playerCombat;
    }

    /// <summary>
    /// Update all movement states - called from PlayerController.FixedUpdate()
    /// </summary>
    public void UpdateStates(Vector2 moveInput)
    {
        // To be implemented in Phase 6
    }
}
