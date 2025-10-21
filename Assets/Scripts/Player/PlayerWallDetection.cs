using UnityEngine;

/// <summary>
/// Handles all wall detection for the player including wall stick and wall slide.
/// Manages wall contact state and determines when player can stick or slide on walls.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerWallDetection : MonoBehaviour
{
    [Header("Wall Detection")]
    [SerializeField] private float wallCheckDistance = 0.15f;

    [Header("Wall Detection Raycasts")]
    [SerializeField] private float wallRaycastTop = 0.32f;    // Top raycast position
    [SerializeField] private float wallRaycastMiddle = 0.28f; // Middle raycast position
    [SerializeField] private float wallRaycastBottom = 0.02f; // Bottom raycast position

    // Component references (injected by PlayerController)
    private Rigidbody2D rb;
    private Collider2D col;
    private Transform playerTransform;
    private PlayerAbilities abilities;

    // External state dependencies
    private bool facingRight;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isBufferClimbing;

    // Public wall state
    public bool OnWall { get; private set; }
    public bool WallStickAllowed { get; private set; }

    // Public properties for shared parameters (used by PlayerMovement and PlayerJumpSystem)
    public float WallCheckDistance => wallCheckDistance;
    public float WallRaycastTop => wallRaycastTop;
    public float WallRaycastMiddle => wallRaycastMiddle;
    public float WallRaycastBottom => wallRaycastBottom;

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
    /// Set configuration values from PlayerController
    /// DEPRECATED: Parameters are now owned by this component
    /// </summary>
    public void SetConfiguration(float _wallCheckDistance, float _wallRaycastTop, float _wallRaycastMiddle, float _wallRaycastBottom)
    {
        // No-op: Parameters are now [SerializeField] in this component
        // This method kept temporarily for backward compatibility during migration
    }

    /// <summary>
    /// Update external state (called before CheckWallDetection)
    /// </summary>
    public void UpdateExternalState(bool _facingRight, Vector2 _moveInput, bool _isGrounded, bool _isBufferClimbing)
    {
        facingRight = _facingRight;
        moveInput = _moveInput;
        isGrounded = _isGrounded;
        isBufferClimbing = _isBufferClimbing;
    }

    /// <summary>
    /// Main wall detection method - called from PlayerController.FixedUpdate()
    /// </summary>
    public void CheckWallDetection()
    {
        // Simplified wall detection using only 3 raycasts
        int groundLayer = LayerMask.NameToLayer("Ground");
        int groundMask = 1 << groundLayer;

        // Wall detection using 3 raycasts at specified heights
        Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
        Vector2[] checkPoints = {
            playerTransform.position + Vector3.up * wallRaycastTop,    // Top (0.32)
            playerTransform.position + Vector3.up * wallRaycastMiddle, // Middle (0.28)
            playerTransform.position + Vector3.up * wallRaycastBottom  // Bottom (0.02)
        };

        // Count how many raycasts hit a wall
        int wallHitCount = 0;

        foreach (Vector2 point in checkPoints)
        {
            RaycastHit2D hit = Physics2D.Raycast(point, wallDirection, wallCheckDistance, groundMask);

            if (hit.collider != null && hit.collider != col)
            {
                // Check if it's a valid vertical wall
                bool isVerticalWall = Mathf.Abs(hit.normal.x) > 0.9f;
                if (isVerticalWall)
                {
                    wallHitCount++;
                }
            }
        }

        // Input checks
        bool pressingTowardWall = (facingRight && moveInput.x > 0.1f) || (!facingRight && moveInput.x < -0.1f);
        bool notMovingAwayFromWall = !((facingRight && moveInput.x < -0.1f) || (!facingRight && moveInput.x > 0.1f));

        // Wall stick ability check
        bool hasWallStickAbility = abilities != null && abilities.HasWallStick;

        if (hasWallStickAbility)
        {
            // When wall stick enabled: Need at least 2 raycasts hitting to allow wall stick
            bool hasEnoughContactForWallStick = wallHitCount >= 2;

            // Wall stick: Player actively pressing toward wall with enough contact
            bool canStickToWall = !isGrounded && hasEnoughContactForWallStick && pressingTowardWall && !isBufferClimbing;

            // Wall slide: Player touching wall with enough contact but not pressing toward it
            bool canSlideOnWall = !isGrounded && hasEnoughContactForWallStick && notMovingAwayFromWall && !pressingTowardWall && !isBufferClimbing;

            // Set physics state - either sticking or sliding
            OnWall = canStickToWall || canSlideOnWall;

            // Store wall stick state for animation
            WallStickAllowed = canStickToWall;
        }
        else
        {
            // Wall stick ability disabled - no wall interaction at all
            OnWall = false;
            WallStickAllowed = false;
        }
    }
}
