using UnityEngine;
using System;

/// <summary>
/// Calculates and tracks all movement states (running, jumping, falling, etc.).
/// Provides unified state access for other systems.
/// EXTRACTED FROM: PlayerController.UpdateMovementStates() (commit edcd431)
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
    public bool IsWallSticking { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsDashingAnim { get; private set; }

    // Input states
    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }
    public float FacingDirection { get; private set; }

    // Events
    public event Action OnEnterWallStick;

    // State tracking for wall stick/slide logic
    private bool wasWallSticking;
    private bool hasEverWallStuck;

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
    /// EXACT CODE EXTRACTED FROM: PlayerController.UpdateMovementStates() in commit edcd431
    /// </summary>
    public void UpdateStates(
        Vector2 moveInput,
        bool isGrounded,
        bool onWall,
        bool wallStickAllowed,
        bool isDashing,
        bool isDashAttacking,
        bool isAirAttacking,
        Vector2 velocity,
        bool isOnSlope,
        bool facingRight,
        float wallSlideSpeed,
        bool hasWallStickAbility,
        bool showJumpDebug)
    {
        // CRITICAL: Sequential wall state logic - stick must come before slide
        // First, calculate if wall sticking conditions are met (requires wall stick ability)
        bool canWallStick = wallStickAllowed && !isDashing && !isDashAttacking && !isAirAttacking && hasWallStickAbility;
        IsWallSticking = canWallStick;

        // Track wall stick history for sequential logic and trigger event
        if (!wasWallSticking && IsWallSticking)
        {
            // Trigger event for physics side effects (handled in PlayerController)
            OnEnterWallStick?.Invoke();
        }

        if (IsWallSticking)
        {
            hasEverWallStuck = true;
        }

        // Wall slide can ONLY trigger if player has wall stuck during this wall contact session
        bool canWallSlide = onWall && velocity.y < -wallSlideSpeed && !isDashing && !isDashAttacking && !isAirAttacking && hasWallStickAbility;
        bool allowWallSlide = canWallSlide && hasEverWallStuck;

        // Only allow wall slide if player has been wall sticking first
        IsWallSliding = allowWallSlide;

        // Reset wall stick history when no longer on wall
        if (!onWall)
        {
            hasEverWallStuck = false;
        }

        // Update wall sticking state - can't be sticking while sliding
        if (IsWallSliding)
        {
            IsWallSticking = false;
        }

        // Then calculate running state (which depends on wall states)
        // SLOPE FIX: Allow running on slopes even if wall detection is confused
        bool allowRunningOnSlope = isOnSlope && isGrounded && Mathf.Abs(moveInput.x) > 0.1f;
        IsRunning = Mathf.Abs(moveInput.x) > 0.1f && !isDashing && !isDashAttacking && !isAirAttacking &&
                   (!onWall || allowRunningOnSlope) && !IsWallSticking;

        IsJumping = !isGrounded && !IsWallSliding && !IsWallSticking && !IsClimbing && !IsLedgeGrabbing && !isDashing && !isDashAttacking && !isAirAttacking && velocity.y > 0;

        IsFalling = !isGrounded && !IsWallSliding && !IsWallSticking && !IsClimbing && !IsLedgeGrabbing && !isDashAttacking && !isAirAttacking && velocity.y < 0;

        IsDashingAnim = isDashing;

        HorizontalInput = moveInput.x;
        VerticalInput = moveInput.y;
        FacingDirection = facingRight ? 1f : -1f;

        // Store previous wall sticking state for next frame
        wasWallSticking = IsWallSticking;
    }
}
