using UnityEngine;

/// <summary>
/// Handles all jump mechanics including variable jump, double jump, forced fall,
/// jump compensation, and dash jump.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerJumpSystem : MonoBehaviour
{
    // Component references (injected by PlayerController)
    private Rigidbody2D rb;
    private Transform playerTransform;
    private PlayerGroundDetection groundDetection;
    private PlayerWallDetection wallDetection;
    private PlayerAbilities abilities;
    private Animator animator;

    // Public jump state
    public bool IsVariableJumpActive { get; private set; }
    public bool IsForcedFalling { get; private set; }
    public int JumpsRemaining { get; private set; }
    public float MinJumpVelocity { get; private set; }
    public float MaxJumpVelocity { get; private set; }

    // Jump timing state
    public float LastJumpTime { get; private set; }
    public float DashJumpTime { get; private set; }

    /// <summary>
    /// Initialize component references from PlayerController
    /// </summary>
    public void Initialize(Rigidbody2D rigidbody, Transform transform, PlayerGroundDetection ground,
                          PlayerWallDetection wall, PlayerAbilities playerAbilities, Animator playerAnimator)
    {
        rb = rigidbody;
        playerTransform = transform;
        groundDetection = ground;
        wallDetection = wall;
        abilities = playerAbilities;
        animator = playerAnimator;
    }

    /// <summary>
    /// Update variable jump state - called from PlayerController.FixedUpdate()
    /// </summary>
    public void UpdateVariableJump()
    {
        // To be implemented in Phase 4
    }

    /// <summary>
    /// Handle jump input and execution
    /// </summary>
    public void HandleJumping(bool jumpQueued, Vector2 moveInput)
    {
        // To be implemented in Phase 4
    }

    /// <summary>
    /// Check if player can perform dash jump
    /// </summary>
    public bool CanPerformDashJump()
    {
        // To be implemented in Phase 4
        return false;
    }
}
