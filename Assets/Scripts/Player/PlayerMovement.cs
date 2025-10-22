using UnityEngine;

/// <summary>
/// Handles horizontal movement, slope physics, buffer climbing, and dashing.
/// Manages movement velocity and sprite flipping.
/// This is a complete extraction from PlayerController preserving all functionality.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float wallSlideSpeed = 2f;

    [Header("Buffer Climbing")]
    [SerializeField] private float climbForce = 3f;
    [SerializeField] private float forwardBoost = 0f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashTime = 0.25f;
    [SerializeField] private float dashCooldown = 0.4f;
    [SerializeField] private int maxDashes = 2;
    [SerializeField] private int maxAirDashes = 2;

    [Header("Dash Jump")]
    [SerializeField] private float dashJumpWindow = 0.1f;

    // ===== COMPONENT REFERENCES (injected by PlayerController) =====
    private Rigidbody2D rb;
    private Transform playerTransform;
    private Animator animator;
    private Collider2D col;
    private PlayerGroundDetection groundDetection;
    private PlayerWallDetection wallDetection;
    private PlayerCombat combat;
    private PlayerJumpSystem jumpSystem;

    // ===== EXTERNAL STATE (updated via UpdateExternalState) =====
    private Vector2 moveInput;
    private bool jumpQueued;
    private bool isGrounded;
    private bool isBufferClimbing;
    private bool isOnSlope;
    private Vector2 slopeNormal;
    private float currentSlopeAngle;
    private bool isWallSliding;
    private bool isWallSticking;
    private bool isVariableJumpActive;
    private bool onWall;
    private bool wallStickAllowed;
    private bool isAttacking;
    private bool isDashAttacking;
    private bool isAirAttacking;
    private bool isJumping;

    // ===== MOVEMENT STATE =====
    private bool facingRight = true;
    private bool isDashing;
    private float dashTimer;
    private float dashCDTimer;
    private int dashesRemaining;
    private int airDashesRemaining;
    private int airDashesUsed;
    private bool wasGroundedBeforeDash;
    private float dashJumpTime;
    private float lastDashEndTime;
    private float dashJumpMomentumDuration = 0.3f;
    private bool isRunning;
    private float facingDirection;
    private float horizontalInput;
    private float originalGravityScale = 1f; // Store original gravity for wall stick

    // ===== MOVING PLATFORM STATE =====
    private MovingPlatform currentPlatformTracked;
    private Vector3 lastPlatformPosition;

    // ===== PUBLIC PROPERTIES =====
    public bool IsDashing => isDashing;
    public bool FacingRight => facingRight;
    public bool IsRunning => isRunning;
    public float DashTimer => dashTimer;
    public float DashTime => dashTime;
    public float DashCDTimer => dashCDTimer;
    public int DashesRemaining => dashesRemaining;
    public int AirDashesRemaining => airDashesRemaining;
    public int AirDashesUsed => airDashesUsed;
    public bool WasGroundedBeforeDash => wasGroundedBeforeDash;
    public float DashJumpTime => dashJumpTime;
    public float LastDashEndTime => lastDashEndTime;
    public float FacingDirection => facingDirection;
    public float HorizontalInput => horizontalInput;
    public float RunSpeed => runSpeed;

    // Public properties for shared parameters (used by PlayerJumpSystem and PlayerGroundDetection)
    public int MaxAirDashes => maxAirDashes;
    public int MaxDashes => maxDashes;

    /// <summary>
    /// Initialize component references from PlayerController
    /// </summary>
    public void Initialize(Rigidbody2D rigidbody, Transform transform, Animator playerAnimator, Collider2D collider,
                          PlayerGroundDetection ground, PlayerWallDetection wall, PlayerCombat playerCombat, PlayerJumpSystem jump)
    {
        rb = rigidbody;
        playerTransform = transform;
        animator = playerAnimator;
        col = collider;
        groundDetection = ground;
        wallDetection = wall;
        combat = playerCombat;
        jumpSystem = jump;
    }

    /// <summary>
    /// Set configuration values from PlayerController
    /// DEPRECATED: Parameters are now owned by this component
    /// </summary>
    public void SetConfiguration(float _runSpeed, float _wallSlideSpeed, float _climbingAssistanceOffset,
                                float _climbForce, float _forwardBoost, float _dashSpeed, float _dashTime,
                                float _dashCooldown, int _maxDashes, int _maxAirDashes, float _dashJumpWindow,
                                float _wallCheckDistance, float _wallRaycastTop, float _wallRaycastMiddle, float _wallRaycastBottom)
    {
        // No-op: Parameters are now [SerializeField] in this component
        // Initialize dash state with our own parameters
        dashesRemaining = maxDashes;
        airDashesRemaining = maxAirDashes;
    }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
    /// <summary>
    /// Initialize for EditMode testing - sets component references and dash state
    /// Accepts configurable parameters to test behavior with different values
    ///
    /// NOTE: Animator is set to NULL to avoid animator.parameters exceptions in EditMode.
    /// This means tests verify COUNTER LOGIC only, not animator integration.
    /// </summary>
    public void InitializeForTesting(Rigidbody2D rigidbody, Transform transform, Animator playerAnimator, Collider2D collider,
                                     int testMaxDashes = 2, int testMaxAirDashes = 2, float testDashCooldown = 0.4f)
    {
        // Set component references
        rb = rigidbody;
        playerTransform = transform;
        animator = null;  // CRITICAL: Set to null to avoid animator.parameters exceptions in EditMode
        col = collider;
        groundDetection = null;
        wallDetection = null;
        combat = null;
        jumpSystem = null;

        // Set configurable test parameters (instead of hardcoding)
        maxDashes = testMaxDashes;
        maxAirDashes = testMaxAirDashes;
        dashCooldown = testDashCooldown;

        // Initialize dash state based on configured values
        facingRight = true;
        dashesRemaining = maxDashes;
        airDashesRemaining = maxAirDashes;
        airDashesUsed = 0;
    }
#endif

    /// <summary>
    /// Update external state - called before HandleMovement
    /// </summary>
    public void UpdateExternalState(bool _facingRight, Vector2 _moveInput, bool _isGrounded, bool _onWall, bool _wallStickAllowed,
                                    bool _isOnSlope, float _currentSlopeAngle, Vector2 _slopeNormal, bool _isBufferClimbing,
                                    bool _isWallSliding, bool _isWallSticking, bool _isAttacking, bool _isDashAttacking,
                                    bool _isAirAttacking, bool _jumpQueued, bool _isJumping, float _dashJumpTime)
    {
        facingRight = _facingRight;
        moveInput = _moveInput;
        isGrounded = _isGrounded;
        onWall = _onWall;
        wallStickAllowed = _wallStickAllowed;
        isOnSlope = _isOnSlope;
        currentSlopeAngle = _currentSlopeAngle;
        slopeNormal = _slopeNormal;
        isBufferClimbing = _isBufferClimbing;
        isWallSliding = _isWallSliding;
        isWallSticking = _isWallSticking;
        isAttacking = _isAttacking;
        isDashAttacking = _isDashAttacking;
        isAirAttacking = _isAirAttacking;
        jumpQueued = _jumpQueued;
        isJumping = _isJumping;
        dashJumpTime = _dashJumpTime;
    }

    /// <summary>
    /// Set dash state from PlayerController (for synchronization with jump system)
    /// </summary>
    public void SetDashState(int _dashesRemaining, int _airDashesUsed)
    {
        dashesRemaining = _dashesRemaining;
        airDashesUsed = _airDashesUsed;
    }

    /// <summary>
    /// End dash (called when jump system triggers dash jump)
    /// </summary>
    public void EndDash()
    {
        isDashing = false;
        lastDashEndTime = Time.time;
    }

    /// <summary>
    /// Handle horizontal movement - called from PlayerController.FixedUpdate()
    /// EXTRACTED FROM PlayerController lines 803-1091
    /// </summary>
    public void HandleMovement()
    {
        // CRITICAL: Wall stick takes ABSOLUTE priority - stop ALL movement AND gravity
        if (isWallSticking)
        {
            // Store original gravity before disabling (if not already stored)
            if (rb.gravityScale != 0f)
            {
                originalGravityScale = rb.gravityScale;
            }

            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f; // Disable gravity to prevent sliding
            return; // Exit immediately - no other movement logic
        }

        // Restore gravity when not wall sticking
        if (rb.gravityScale == 0f && !isWallSticking)
        {
            rb.gravityScale = originalGravityScale;
        }

        // MOVING PLATFORM MOVEMENT INHERITANCE
        // Apply platform movement FIRST, before any player movement code
        // This ensures platform movement doesn't interfere with player input
        MovingPlatform currentPlatform = groundDetection?.CurrentPlatform;

        if (isGrounded && currentPlatform != null)
        {
            // Check if we just landed on a platform (new platform or first frame on platform)
            if (currentPlatformTracked != currentPlatform)
            {
                // Store the platform's position when we first land
                lastPlatformPosition = currentPlatform.transform.position;
                currentPlatformTracked = currentPlatform;
            }
            else
            {
                // Calculate how much the platform moved this frame
                Vector3 platformDelta = currentPlatform.transform.position - lastPlatformPosition;

                // Move player by the same amount the platform moved
                // Using transform.position for immediate, non-physics movement
                // This happens BEFORE player input is processed
                if (platformDelta.magnitude > 0.0001f) // Only if platform actually moved
                {
                    playerTransform.position += platformDelta;
                }

                // Update platform position for next frame
                lastPlatformPosition = currentPlatform.transform.position;
            }
        }
        else
        {
            // Not on platform anymore, clear tracking
            currentPlatformTracked = null;
        }

        // Buffer climbing assistance - provide upward and forward momentum
        if (isBufferClimbing)
        {
            // Read climbingAssistanceOffset from PlayerGroundDetection
            float climbingAssistanceOffset = groundDetection?.ClimbingAssistanceOffset ?? 0.06f;
            float horizontalVelocity = moveInput.x * runSpeed * forwardBoost;

            // CRITICAL FIX: Prevent horizontal movement when wall stick is disabled (even during ledge buffer)
            if (PlayerAbilities.Instance != null && !PlayerAbilities.Instance.HasWallStick)
            {
                // Check if we're pushing against a wall when ability is disabled
                Collider2D playerCollider = col;
                int groundLayer = LayerMask.NameToLayer("Ground");
                int groundMask = 1 << groundLayer;

                // Read wall detection parameters from PlayerWallDetection
                float wallCheckDistance = wallDetection?.WallCheckDistance ?? 0.15f;

                Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
                Vector2 centerPoint = playerTransform.position;

                RaycastHit2D wallHit = Physics2D.Raycast(centerPoint, wallDirection, wallCheckDistance, groundMask);

                if (wallHit.collider != null && wallHit.collider != playerCollider)
                {
                    bool isVerticalWall = Mathf.Abs(wallHit.normal.x) > 0.9f;
                    bool pressingIntoWall = (facingRight && moveInput.x > 0.1f) || (!facingRight && moveInput.x < -0.1f);

                    if (isVerticalWall && pressingIntoWall)
                    {
                        horizontalVelocity = 0f;
                        // Debug.Log($"[WallStick] DISABLED - Preventing ledge buffer wall friction at platform bottom.");
                    }
                }
            }

            // Apply upward assist to help climb over platform edge
            if (rb.linearVelocity.y <= 0.5f) // Only if not already moving up significantly
            {
                Vector2 newVelocity = new Vector2(
                    horizontalVelocity,
                    Mathf.Max(rb.linearVelocity.y, climbForce)
                );
                rb.linearVelocity = newVelocity;
            }
            else
            {
                // Just apply forward momentum if already moving up
                Vector2 newVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
                rb.linearVelocity = newVelocity;
            }

            // Debug.Log($"[BUFFER CLIMBING ASSIST] Applied climbing force: upward={climbForce}, forward boost={forwardBoost}");
            return; // Skip normal movement processing
        }

        // Horizontal run (skip during dashing and dash jump momentum preservation)
        // DESIGN CHANGE: Allow horizontal movement during air attacks and dash attacks
        bool isDashJumpMomentumActive = dashJumpTime > 0 && Time.time - dashJumpTime <= dashJumpMomentumDuration;
        if (isDashJumpMomentumActive && moveInput.x == 0)
        {
            // Debug log when momentum preservation is preventing movement override
            // Debug.Log($"[DashJump] Momentum preservation active - keeping horizontal velocity: {rb.linearVelocity.x:F2}");
        }

        if (!isDashing && !isDashJumpMomentumActive)
        {
            float horizontalVelocity = moveInput.x * runSpeed;

            // Wall movement prevention logic
            bool hasWallStickAbility = PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick;
            bool shouldCheckWallMovement = !isGrounded || isBufferClimbing;

            if (shouldCheckWallMovement)
            {
                // Perform wall detection using our 3 raycasts
                Collider2D playerCollider = col;
                int groundLayer = LayerMask.NameToLayer("Ground");
                int groundMask = 1 << groundLayer;

                // Read wall detection parameters from PlayerWallDetection
                float wallCheckDistance = wallDetection?.WallCheckDistance ?? 0.15f;
                float wallRaycastTop = wallDetection?.WallRaycastTop ?? 0.32f;
                float wallRaycastMiddle = wallDetection?.WallRaycastMiddle ?? 0.28f;
                float wallRaycastBottom = wallDetection?.WallRaycastBottom ?? 0.02f;

                Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
                Vector2[] checkPoints = {
                    playerTransform.position + Vector3.up * wallRaycastTop,    // Top (0.32)
                    playerTransform.position + Vector3.up * wallRaycastMiddle, // Middle (0.28)
                    playerTransform.position + Vector3.up * wallRaycastBottom  // Bottom (0.02)
                };

                int wallHitCount = 0;
                foreach (Vector2 point in checkPoints)
                {
                    RaycastHit2D wallHit = Physics2D.Raycast(point, wallDirection, wallCheckDistance, groundMask);

                    if (wallHit.collider != null && wallHit.collider != playerCollider)
                    {
                        bool isVerticalWall = Mathf.Abs(wallHit.normal.x) > 0.9f;
                        if (isVerticalWall)
                        {
                            wallHitCount++;
                        }
                    }
                }

                bool pressingIntoWall = (facingRight && moveInput.x > 0.1f) || (!facingRight && moveInput.x < -0.1f);

                if (hasWallStickAbility)
                {
                    // Wall stick enabled: Need 2+ hits for wall stick, but prevent movement for 1 hit
                    if (wallHitCount >= 1 && wallHitCount < 2 && pressingIntoWall)
                    {
                        // Not enough contact for wall stick - prevent movement to avoid getting stuck
                        horizontalVelocity = 0f;
                        // Debug.Log($"[WallStick] ENABLED - Preventing movement, insufficient contact for wall stick (hits: {wallHitCount}/3, need 2+)");
                    }
                    // If 2+ hits, allow normal movement for wall stick behavior
                }
                else
                {
                    // Wall stick disabled: Any raycast hit prevents horizontal movement
                    if (wallHitCount >= 1 && pressingIntoWall)
                    {
                        horizontalVelocity = 0f;
                        // Debug.Log($"[WallStick] DISABLED - Preventing wall movement (hits: {wallHitCount}/3)");
                    }
                }
            }

            // SLOPE-AWARE MOVEMENT: When on slopes, move along the slope surface
            if (isOnSlope && isGrounded && Mathf.Abs(moveInput.x) > 0.1f)
            {
                // Calculate movement along slope surface
                Vector2 slopeDirection = new Vector2(slopeNormal.y, -slopeNormal.x).normalized;

                // Ensure we're moving in the right direction (down or up slope based on input)
                if ((moveInput.x > 0 && slopeDirection.x < 0) || (moveInput.x < 0 && slopeDirection.x > 0))
                {
                    slopeDirection *= -1; // Flip direction if needed
                }

                Vector2 slopeMovement = slopeDirection * Mathf.Abs(moveInput.x) * runSpeed;
                rb.linearVelocity = new Vector2(slopeMovement.x, slopeMovement.y);

                // Debug.Log($"[SLOPE MOVEMENT] Moving along slope: angle={currentSlopeAngle:F1}°, velocity=({slopeMovement.x:F2}, {slopeMovement.y:F2})");
            }
            else
            {
                // Normal flat movement
                rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
            }
        }

        // Wall slide slow-down - only if actually wall sliding (not just near wall)
        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }

        // Debug: Check if player is moving slowly near walls when ability is disabled
        if (PlayerAbilities.Instance != null && !PlayerAbilities.Instance.HasWallStick && !isGrounded)
        {
            // Check if velocity is being affected when it shouldn't be
            if (Mathf.Abs(rb.linearVelocity.y) < 3f && Mathf.Abs(rb.linearVelocity.y) > 0.1f)
            {
                // Debug.Log($"[WallStick] PHYSICS DEBUG: Velocity unexpectedly slow when ability disabled: velocity={rb.linearVelocity}, onWall={onWall}");
            }
        }

        // OLD FIX: Velocity override (no longer needed with improved wall detection)
        // if (PlayerAbilities.Instance != null && !PlayerAbilities.Instance.HasWallStick)
        // {
        //     // This fix is disabled as the root cause is fixed in wall friction prevention
        // }

        // Handle attack movement overrides (allow reduced movement during attacks) - EXACTLY like original
        // IMPORTANT: Skip attack movement when jump is queued to prevent movement freeze
        bool isAttacking = combat != null && combat.IsAttacking;
        bool isDashAttacking = combat != null && combat.IsDashAttacking;
        bool isAirAttacking = combat != null && combat.IsAirAttacking;

        if (isAttacking && isGrounded && !isDashAttacking && !isAirAttacking && !jumpQueued)
        {
            float attackSpeedMultiplier = combat?.attackMovementSpeed ?? 0.3f;
            float attackHorizontalVelocity = moveInput.x * runSpeed * attackSpeedMultiplier;

            // Combat system respects wall movement rules
            bool hasWallStickAbility = PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick;
            if (!hasWallStickAbility)
            {
                // Use simplified wall detection
                Collider2D playerCollider = col;
                int groundLayer = LayerMask.NameToLayer("Ground");
                int groundMask = 1 << groundLayer;

                // Read wall detection parameters from PlayerWallDetection
                float wallCheckDistance = wallDetection?.WallCheckDistance ?? 0.15f;
                float wallRaycastTop = wallDetection?.WallRaycastTop ?? 0.32f;
                float wallRaycastMiddle = wallDetection?.WallRaycastMiddle ?? 0.28f;
                float wallRaycastBottom = wallDetection?.WallRaycastBottom ?? 0.02f;

                Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
                Vector2[] checkPoints = {
                    playerTransform.position + Vector3.up * wallRaycastTop,
                    playerTransform.position + Vector3.up * wallRaycastMiddle,
                    playerTransform.position + Vector3.up * wallRaycastBottom
                };

                bool hasAnyWallHit = false;
                foreach (Vector2 point in checkPoints)
                {
                    RaycastHit2D wallHit = Physics2D.Raycast(point, wallDirection, wallCheckDistance, groundMask);
                    if (wallHit.collider != null && wallHit.collider != playerCollider)
                    {
                        bool isVerticalWall = Mathf.Abs(wallHit.normal.x) > 0.9f;
                        if (isVerticalWall)
                        {
                            hasAnyWallHit = true;
                            break;
                        }
                    }
                }

                bool pressingIntoWall = (facingRight && moveInput.x > 0.1f) || (!facingRight && moveInput.x < -0.1f);

                // When wall stick disabled, any hit prevents movement during combat
                if (hasAnyWallHit && pressingIntoWall)
                {
                    attackHorizontalVelocity = 0f;
                    // Debug.Log($"[WallStick] DISABLED - Combat system preventing wall movement.");
                }
            }

            rb.linearVelocity = new Vector2(attackHorizontalVelocity, rb.linearVelocity.y);
        }

        // Let combat system handle air attack and dash attack movement
        Vector2 combatMovement = combat?.GetAttackMovement() ?? Vector2.zero;
        if (combatMovement != Vector2.zero && (isDashAttacking || isAirAttacking))
        {
            float combatHorizontalVelocity = combatMovement.x;

            // CRITICAL FIX: Combat movement must also respect wall stick prevention
            if (PlayerAbilities.Instance != null && !PlayerAbilities.Instance.HasWallStick)
            {
                // Check if combat movement would push against wall when ability is disabled
                bool movingIntoWall = (facingRight && combatHorizontalVelocity > 0.1f) || (!facingRight && combatHorizontalVelocity < -0.1f);

                if (movingIntoWall)
                {
                    Collider2D playerCollider = col;
                    int groundLayer = LayerMask.NameToLayer("Ground");
                    int groundMask = 1 << groundLayer;

                    // Read wall detection parameters from PlayerWallDetection
                    float wallCheckDistance = wallDetection?.WallCheckDistance ?? 0.15f;

                    Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
                    Vector2 centerPoint = playerTransform.position;

                    RaycastHit2D wallHit = Physics2D.Raycast(centerPoint, wallDirection, wallCheckDistance, groundMask);

                    if (wallHit.collider != null && wallHit.collider != playerCollider)
                    {
                        bool isVerticalWall = Mathf.Abs(wallHit.normal.x) > 0.9f;

                        if (isVerticalWall)
                        {
                            combatHorizontalVelocity = 0f;
                            // Debug.Log($"[WallStick] DISABLED - Combat movement respecting wall stick prevention.");
                        }
                    }
                }
            }

            rb.linearVelocity = new Vector2(combatHorizontalVelocity, combatMovement.y != 0 ? combatMovement.y : rb.linearVelocity.y);
        }

        // SLOPE MOVEMENT: Prevent sliding down slopes + prevent upward drift
        if (isOnSlope && isGrounded && !isJumping && !isDashing && !isDashAttacking && !isAirAttacking)
        {
            if (Mathf.Abs(moveInput.x) < 0.1f)
            {
                // Calculate anti-sliding force (original math was correct, application was wrong)
                Vector2 gravity = Physics2D.gravity * rb.gravityScale;
                Vector2 slopeDirection = new Vector2(slopeNormal.y, -slopeNormal.x).normalized;
                float gravityAlongSlope = Vector2.Dot(gravity, slopeDirection);

                // Apply counterforce as actual force (not velocity!) to prevent sliding
                Vector2 counterForce = -slopeDirection * gravityAlongSlope * rb.mass;
                rb.AddForce(counterForce, ForceMode2D.Force);

                // Safety clamp to prevent upward drift (keeps our fix)
                // Don't clamp during variable jump - it would kill the jump on slopes
                if (rb.linearVelocity.y > 0.1f && !isVariableJumpActive)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0.1f);
                }

                // Debug.Log($"[SLOPE PHYSICS] IDLE - Anti-slide force: {counterForce.magnitude:F3}, Y velocity: {rb.linearVelocity.y:F3}");
            }
            else
            {
                // Debug.Log($"[SLOPE PHYSICS] MOVING on {currentSlopeAngle:F1}° slope - Normal movement");
                // When moving, let normal movement physics handle everything
            }
        }

        // Update state for external reading
        UpdateMovementState();
    }

    /// <summary>
    /// Handle dash input and execution
    /// EXTRACTED FROM PlayerController lines 1094-1151
    /// </summary>
    public void HandleDash(bool dashQueued)
    {
        // Check if dash ability is unlocked
        if (PlayerAbilities.Instance == null || !PlayerAbilities.Instance.HasDash)
        {
            return;
        }

        bool canDash = false;
        if (isGrounded)
        {
            // Allow dash if we have charges OR cooldown expired
            canDash = (dashesRemaining > 0) || (dashesRemaining <= 0 && dashCDTimer <= 0);
        }
        else
        {
            canDash = (airDashesUsed == 0);
        }

        // Allow dash when jumping from wall (onWall may still be true but not in wall state)
        bool onWall = wallDetection != null && wallDetection.OnWall;
        bool isWallSticking = onWall && wallDetection.WallStickAllowed;
        bool actuallyOnWall = onWall && (isWallSticking || isWallSliding);

        if (dashQueued && !isDashing && canDash && !actuallyOnWall)
        {
            if (isGrounded)
            {
                if (dashesRemaining > 0)
                {
                    // Normal consumption
                    dashesRemaining--;
                    Debug.Log($"[DASH] Consumed charge. Remaining: {dashesRemaining}, CD: {dashCDTimer:F2}");
                    if (dashesRemaining <= 0)
                    {
                        dashCDTimer = dashCooldown;
                        Debug.Log($"[DASH] Out of charges, starting cooldown: {dashCDTimer:F2}");
                    }
                }
                else // dashesRemaining <= 0 && dashCDTimer <= 0 (cooldown expired)
                {
                    // FIX: Restore to FULL charges, then consume one
                    dashesRemaining = maxDashes - 1;
                    Debug.Log($"[DASH] Cooldown expired, restored and consumed. Remaining: {dashesRemaining}");
                }
            }
            else
            {
                airDashesUsed = 1;
            }

            combat?.OnDashStart();
            isDashing = true;
            dashTimer = 0;
            wasGroundedBeforeDash = isGrounded;

            if (animator != null)
            {
                // Safe trigger set
                if (animator.parameters != null)
                {
                    foreach (var param in animator.parameters)
                    {
                        if (param.name == "Dash" && param.type == AnimatorControllerParameterType.Trigger)
                        {
                            animator.SetTrigger("Dash");
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Update dash cooldown timer
    /// EXTRACTED FROM PlayerController lines 1153-1159
    /// </summary>
    public void UpdateDashCooldown()
    {
        if (dashesRemaining <= 0)
        {
            float oldTimer = dashCDTimer;
            dashCDTimer -= Time.fixedDeltaTime;
            if (Mathf.Abs(oldTimer - dashCDTimer) > 0.001f && dashCDTimer > 0 && dashCDTimer < 0.1f)
            {
                Debug.Log($"[DASH COOLDOWN] Decrementing: {oldTimer:F3} → {dashCDTimer:F3}");
            }
        }
    }

    /// <summary>
    /// Apply dash velocity and check for wall collisions
    /// Called from PlayerController after HandleMovement
    /// </summary>
    public void ApplyDashVelocity()
    {
        if (!isDashing) return;

        // CRITICAL: Don't apply dash velocity if wall sticking
        // Wall stick overrides dash - end the dash instead
        if (isWallSticking)
        {
            isDashing = false;
            lastDashEndTime = Time.time;
            combat?.OnDashEnd();
            return;
        }

        // Check if dash should end early due to wall collision
        bool dashEndedByWall = CheckDashWallCollision();

        rb.linearVelocity = new Vector2(facingRight ? dashSpeed : -dashSpeed, 0);

        // End dash if timer expires OR wall collision detected
        if ((dashTimer += Time.fixedDeltaTime) >= dashTime || dashEndedByWall)
        {
            isDashing = false;
            dashJumpTime = Time.time; // Track when dash ended for dash-jump window
            lastDashEndTime = Time.time; // Track for coyote time logic
            combat?.OnDashEnd();

            if (dashEndedByWall)
            {
                // Allow falling immediately after wall collision
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
    }

    /// <summary>
    /// Check if dash should end due to wall collision
    /// EXTRACTED FROM PlayerController lines 1564-1613
    /// </summary>
    private bool CheckDashWallCollision()
    {
        if (!isDashing) return false;

        // Only check for wall collision when wall stick is disabled
        // When wall stick is enabled, dashing into walls is expected behavior
        if (PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick)
            return false;

        // CRITICAL FIX: Only end dash if ACTIVELY COLLIDING with wall (very close distance)
        // Don't end dash just because a wall is nearby - check if we're actually hitting it
        Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
        int groundLayer = LayerMask.NameToLayer("Ground");
        int groundMask = 1 << groundLayer;

        // Use very short raycast distance to only detect actual collisions
        float collisionCheckDistance = 0.15f; // Much shorter than wallCheckDistance

        // Read wall detection parameters from PlayerWallDetection
        float wallRaycastTop = wallDetection?.WallRaycastTop ?? 0.32f;
        float wallRaycastMiddle = wallDetection?.WallRaycastMiddle ?? 0.28f;
        float wallRaycastBottom = wallDetection?.WallRaycastBottom ?? 0.02f;

        Vector2[] checkPoints = {
            playerTransform.position + Vector3.up * wallRaycastTop,
            playerTransform.position + Vector3.up * wallRaycastMiddle,
            playerTransform.position + Vector3.up * wallRaycastBottom
        };

        // Count how many raycasts hit - require at least 2 for solid collision
        int hitCount = 0;

        foreach (Vector2 point in checkPoints)
        {
            RaycastHit2D hit = Physics2D.Raycast(point, wallDirection, collisionCheckDistance, groundMask);

            if (hit.collider != null)
            {
                // Check if it's a vertical wall (not a slope)
                if (Mathf.Abs(hit.normal.x) > 0.9f)
                {
                    hitCount++;
                }
            }
        }

        // Only end dash if we hit wall with at least 2 raycasts (solid collision)
        if (hitCount >= 2)
        {
            // Debug.Log($"[DashFix] Dash collision detected - hitCount: {hitCount}");
            return true;
        }

        return false; // No solid wall collision, continue dash
    }

    /// <summary>
    /// Update sprite facing direction
    /// EXTRACTED FROM PlayerController lines 1161-1165
    /// </summary>
    public void UpdateSpriteFacing(Vector2 _moveInput)
    {
        if (_moveInput.x != 0) facingRight = _moveInput.x > 0;
        playerTransform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
    }

    /// <summary>
    /// Update internal movement state for external reading
    /// </summary>
    private void UpdateMovementState()
    {
        // Update isRunning state
        bool isAttacking = combat != null && combat.IsAttacking;
        bool isDashAttacking = combat != null && combat.IsDashAttacking;
        bool isAirAttacking = combat != null && combat.IsAirAttacking;

        isRunning = Mathf.Abs(moveInput.x) > 0.1f && !isDashing && !isDashAttacking && !isAirAttacking &&
                    !isWallSliding && isGrounded;

        // Update facing direction and input for animator
        facingDirection = facingRight ? 1 : -1;
        horizontalInput = moveInput.x;
    }

    /// <summary>
    /// Reset dash state on landing (called from PlayerController when landing)
    /// </summary>
    public void OnLanding()
    {
        dashesRemaining = maxDashes;
        airDashesRemaining = maxAirDashes;
        airDashesUsed = 0;
    }

    /// <summary>
    /// Reset all movement state (called from respawn system)
    /// </summary>
    public void ResetMovementState()
    {
        isDashing = false;
        dashTimer = 0f;
        dashJumpTime = 0f;
        dashesRemaining = maxDashes;
        airDashesRemaining = maxAirDashes;
        airDashesUsed = 0;
        facingRight = true;
    }
}
