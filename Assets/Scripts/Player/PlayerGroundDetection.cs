using UnityEngine;

/// <summary>
/// Handles all ground detection for the player including platforms, buffers, and slopes.
/// Manages grounding state, slope detection, and coyote time.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerGroundDetection : MonoBehaviour
{
    // Component references (injected by PlayerController)
    private Rigidbody2D rb;
    private Collider2D col;
    private Transform playerTransform;

    // Public grounding state
    public bool IsGrounded { get; private set; }
    public bool IsOnSlope { get; private set; }
    public float CurrentSlopeAngle { get; private set; }
    public Vector2 SlopeNormal { get; private set; }
    public bool IsGroundedByPlatform { get; private set; }
    public bool IsGroundedByBuffer { get; private set; }
    public bool IsBufferClimbing { get; private set; }

    // Coyote time state
    public float CoyoteTimeCounter { get; private set; }
    public bool LeftGroundByJumping { get; set; }

    /// <summary>
    /// Initialize component references from PlayerController
    /// </summary>
    public void Initialize(Rigidbody2D rigidbody, Collider2D collider, Transform transform)
    {
        rb = rigidbody;
        col = collider;
        playerTransform = transform;
    }

    /// <summary>
    /// Main detection method - called from PlayerController.FixedUpdate()
    /// </summary>
    public void CheckGrounding()
    {
        // To be implemented in Phase 2
    }
}
