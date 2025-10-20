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
    private PlayerCombat combat;

    // Configuration (injected from PlayerController)
    private float groundCheckOffsetY;
    private float groundCheckRadius;
    private float maxSlopeAngle;
    private bool enableSlopeVisualization;
    private float slopeRaycastDistance;
    private Vector2 raycastDirection1;
    private Vector2 raycastDirection2;
    private Vector2 raycastDirection3;
    private float debugLineDuration;
    private bool enableCoyoteTime;
    private float coyoteTimeDuration;
    private float climbingAssistanceOffset;

    // External state dependencies
    private Vector2 moveInput;
    private bool facingRight;
    private float lastJumpTime;
    private float dashJumpTime;
    private int maxAirDashes;
    private int maxDashes;

    // Public grounding state
    public bool IsGrounded { get; private set; }
    public bool IsOnSlope { get; private set; }
    public float CurrentSlopeAngle { get; private set; }
    public Vector2 SlopeNormal { get; private set; } = Vector2.up;
    public bool IsGroundedByPlatform { get; private set; }
    public bool IsGroundedByBuffer { get; private set; }
    public bool IsBufferClimbing { get; private set; }

    // Internal state
    private bool groundedByBuffer;
    private bool pressingHorizontally;

    // Coyote time state
    public float CoyoteTimeCounter { get; private set; }
    public bool LeftGroundByJumping { get; set; }

    // External state setters
    public int AirDashesRemaining { get; set; }
    public int AirDashesUsed { get; set; }
    public int DashesRemaining { get; set; }
    public float LastLandTime { get; set; }

    /// <summary>
    /// Initialize component references and configuration from PlayerController
    /// </summary>
    public void Initialize(Rigidbody2D rigidbody, Collider2D collider, Transform transform)
    {
        rb = rigidbody;
        col = collider;
        playerTransform = transform;
    }

    /// <summary>
    /// Set configuration values from PlayerController
    /// </summary>
    public void SetConfiguration(float _groundCheckOffsetY, float _groundCheckRadius, float _maxSlopeAngle,
                                bool _enableSlopeVisualization, float _slopeRaycastDistance,
                                Vector2 _raycastDirection1, Vector2 _raycastDirection2, Vector2 _raycastDirection3,
                                float _debugLineDuration, bool _enableCoyoteTime, float _coyoteTimeDuration,
                                float _climbingAssistanceOffset, int _maxAirDashes, int _maxDashes, PlayerCombat _combat)
    {
        groundCheckOffsetY = _groundCheckOffsetY;
        groundCheckRadius = _groundCheckRadius;
        maxSlopeAngle = _maxSlopeAngle;
        enableSlopeVisualization = _enableSlopeVisualization;
        slopeRaycastDistance = _slopeRaycastDistance;
        raycastDirection1 = _raycastDirection1;
        raycastDirection2 = _raycastDirection2;
        raycastDirection3 = _raycastDirection3;
        debugLineDuration = _debugLineDuration;
        enableCoyoteTime = _enableCoyoteTime;
        coyoteTimeDuration = _coyoteTimeDuration;
        climbingAssistanceOffset = _climbingAssistanceOffset;
        maxAirDashes = _maxAirDashes;
        maxDashes = _maxDashes;
        combat = _combat;
    }

    /// <summary>
    /// Update external state (called before CheckGrounding)
    /// </summary>
    public void UpdateExternalState(Vector2 _moveInput, bool _facingRight, float _lastJumpTime, float _dashJumpTime)
    {
        moveInput = _moveInput;
        facingRight = _facingRight;
        lastJumpTime = _lastJumpTime;
        dashJumpTime = _dashJumpTime;
    }

    /// <summary>
    /// Main detection method - called from PlayerController.FixedUpdate()
    /// </summary>
    public void CheckGrounding()
    {
        // DESIGN CHANGE: Dash attacks and air attacks now fall naturally
        // No need to skip ground detection - all attacks use normal physics
        // Ground detection runs normally for all states

        float feetY = col.bounds.min.y;
        Vector2 feetPos = new Vector2(playerTransform.position.x, feetY + groundCheckOffsetY);
        int groundLayer = LayerMask.NameToLayer("Ground");
        int bufferLayer = LayerMask.NameToLayer("LandingBuffer");
        int platformMask = (1 << groundLayer);
        int bufferMask = (1 << bufferLayer);

        bool groundedByPlatform = Physics2D.OverlapCircle(feetPos, groundCheckRadius, platformMask);
        groundedByBuffer = Physics2D.OverlapCircle(feetPos, groundCheckRadius, bufferMask);

        // Only allow buffer grounding when moving downward or horizontally (prevent ghost jumps when jumping upward)
        if (groundedByBuffer && rb.linearVelocity.y > 0.1f)
        {
            groundedByBuffer = false;
        }

        bool wasGrounded = IsGrounded;

        // Special case: If player is pressing horizontally and buffer is detected,
        // allow "climbing" onto platform even if wall is detected (platform edge climbing)
        pressingHorizontally = Mathf.Abs(moveInput.x) > 0.1f;

        // Check if player is within climbing assistance range of a platform edge
        bool nearPlatformEdge = CheckClimbingAssistanceZone();

        IsBufferClimbing = groundedByBuffer && pressingHorizontally && nearPlatformEdge &&
                           Mathf.Abs(rb.linearVelocity.y) < 1.0f; // Not falling too fast

        // Standard grounding logic with platform edge climbing exception
        if (IsBufferClimbing)
        {
            // Buffer climbing takes priority - disable wall state to prevent animation conflict
            IsGrounded = true;
            IsGroundedByPlatform = false;
            IsGroundedByBuffer = true;
        }
        else
        {
            // ENHANCED GROUNDING LOGIC: Include slope detection in ground check
            bool groundedBySlope = CheckSlopeGrounding(feetPos, platformMask);

            IsGrounded = groundedByPlatform || groundedByBuffer || groundedBySlope;
            IsGroundedByPlatform = groundedByPlatform;
            IsGroundedByBuffer = groundedByBuffer && !groundedByPlatform;
        }

        // Complete the grounding logic
        FinishCheckGrounding(wasGrounded);
    }

    /// <summary>
    /// Check if player is grounded on slopes
    /// </summary>
    private bool CheckSlopeGrounding(Vector2 feetPos, int platformMask)
    {
        // Reset slope detection
        IsOnSlope = false;
        SlopeNormal = Vector2.up;
        CurrentSlopeAngle = 0f;

        // JUMP GRACE PERIOD: Prevent slope grounding briefly after jump starts
        // This allows isGrounded to become false, enabling jump animation
        float timeSinceJump = Time.time - lastJumpTime;
        float slopeJumpGracePeriod = 0.15f; // Prevent slope grounding for 0.15s after jump

        if (timeSinceJump < slopeJumpGracePeriod && rb.linearVelocity.y > 0.5f)
        {
            return false; // Don't detect slope grounding during jump grace period
        }

        // MULTI-DIRECTIONAL RAYCAST: Use editor-configurable directions
        Vector2[] raycastDirections = {
            raycastDirection1.normalized,
            raycastDirection2.normalized,
            raycastDirection3.normalized
        };

        RaycastHit2D bestSlopeHit = new RaycastHit2D();
        float bestAngle = 0f;
        Vector2 bestNormal = Vector2.up;

        // Try each raycast direction to find the best slope hit
        foreach (Vector2 direction in raycastDirections)
        {
            RaycastHit2D slopeHit = Physics2D.Raycast(feetPos, direction, slopeRaycastDistance, platformMask);

            // VISUAL DEBUG: Draw each raycast line (only if enabled)
            if (enableSlopeVisualization)
            {
                if (slopeHit.collider != null)
                {
                    Debug.DrawLine(feetPos, slopeHit.point, Color.green, debugLineDuration);
                    Debug.DrawLine(slopeHit.point, feetPos + direction * slopeRaycastDistance, Color.red, debugLineDuration);
                    Debug.DrawLine(slopeHit.point, slopeHit.point + slopeHit.normal * 0.5f, Color.yellow, debugLineDuration);
                }
                else
                {
                    Debug.DrawLine(feetPos, feetPos + direction * slopeRaycastDistance, Color.red, debugLineDuration);
                }
            }

            // Make sure we don't hit the player's own collider
            if (slopeHit.collider != null && slopeHit.collider != col)
            {
                Vector2 hitNormal = slopeHit.normal;
                float hitAngle = Vector2.Angle(hitNormal, Vector2.up);

                // Use the hit with the most significant slope angle
                if (hitAngle > bestAngle)
                {
                    bestSlopeHit = slopeHit;
                    bestAngle = hitAngle;
                    bestNormal = hitNormal;
                }
            }
        }

        // Process the best slope hit found
        if (bestSlopeHit.collider != null)
        {
            // GHOST GROUNDING FIX: Check VERTICAL distance to slope, not raycast distance
            float verticalDistance = Mathf.Abs(feetPos.y - bestSlopeHit.point.y);
            float maxVerticalGroundingDistance = 0.2f;

            if (verticalDistance > maxVerticalGroundingDistance)
            {
                return false; // Too far above slope surface to be grounded
            }

            SlopeNormal = bestNormal;
            CurrentSlopeAngle = bestAngle;

            // Consider it a slope if angle is significant but walkable
            if (CurrentSlopeAngle > 1f && CurrentSlopeAngle <= maxSlopeAngle)
            {
                IsOnSlope = true;
                return true; // Player is grounded on a slope
            }
        }

        return false; // No slope grounding found
    }

    /// <summary>
    /// Complete grounding logic including landing and coyote time
    /// </summary>
    private void FinishCheckGrounding(bool wasGrounded)
    {
        // Handle landing
        if (!wasGrounded && IsGrounded)
        {
            combat?.OnLanding();
            AirDashesRemaining = maxAirDashes;
            AirDashesUsed = 0;
            DashesRemaining = maxDashes;
            LastLandTime = Time.time;
        }

        // Coyote time tracking
        if (enableCoyoteTime)
        {
            if (IsGrounded)
            {
                // Reset coyote time when grounded
                CoyoteTimeCounter = coyoteTimeDuration;
                LeftGroundByJumping = false;
            }
            else if (wasGrounded && !IsGrounded)
            {
                // Just left ground - check if it was from jumping
                if (rb.linearVelocity.y > 0.5f)
                {
                    LeftGroundByJumping = true;
                    CoyoteTimeCounter = 0f;
                }
                else
                {
                    LeftGroundByJumping = false;
                }
            }
            else if (!IsGrounded && CoyoteTimeCounter > 0f)
            {
                // Decrement coyote time while airborne
                CoyoteTimeCounter -= Time.fixedDeltaTime;
                if (CoyoteTimeCounter < 0f)
                {
                    CoyoteTimeCounter = 0f;
                }
            }
        }
        else
        {
            CoyoteTimeCounter = 0f;
            LeftGroundByJumping = false;
        }
    }

    /// <summary>
    /// Check if player is in climbing assistance zone (near platform edge)
    /// </summary>
    private bool CheckClimbingAssistanceZone()
    {
        Vector3 playerPos = playerTransform.position;
        float checkDirection = facingRight ? 1f : -1f;

        int groundLayer = LayerMask.NameToLayer("Ground");
        int groundMask = 1 << groundLayer;

        Vector2 platformCheckOrigin = playerPos + Vector3.up * climbingAssistanceOffset + Vector3.right * checkDirection * 0.1f;
        Vector2 checkDown = Vector2.down;
        float checkDistance = climbingAssistanceOffset + 0.3f;

        RaycastHit2D platformHit = Physics2D.Raycast(platformCheckOrigin, checkDown, checkDistance, groundMask);

        Vector2 wallCheckOrigin = playerPos;
        Vector2 checkHorizontal = checkDirection > 0 ? Vector2.right : Vector2.left;
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheckOrigin, checkHorizontal, 0.3f, groundMask);

        bool belowPlatformEdge = platformHit.collider != null;
        bool nearWallEdge = wallHit.collider != null;

        return belowPlatformEdge && nearWallEdge;
    }
}
