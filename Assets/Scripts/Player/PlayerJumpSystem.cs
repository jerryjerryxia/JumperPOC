using UnityEngine;

/// <summary>
/// Handles all jump mechanics including variable jump, double jump, wall jump, dash jump,
/// forced fall, and jump compensation for wall friction and slopes.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerJumpSystem : MonoBehaviour
{
    // Component references (injected by PlayerController)
    private Rigidbody2D rb;
    private Transform playerTransform;
    private Collider2D col;
    private PlayerGroundDetection groundDetection;
    private PlayerWallDetection wallDetection;
    private PlayerAbilities abilities;
    private Animator animator;
    private PlayerCombat combat;
    private InputManager inputManager;

    // Configuration (injected from PlayerController)
    private int extraJumps;
    private Vector2 wallJump;
    private bool enableVariableJump;
    private float minJumpVelocity;
    private float maxJumpVelocity;
    private float jumpHoldDuration;
    private float jumpGravityReduction;
    private float minDoubleJumpVelocity;
    private float maxDoubleJumpVelocity;
    private float doubleJumpMinDelay;
    private float forcedFallDuration;
    private float forcedFallVelocity;
    private bool useVelocityClamping;
    private bool showJumpDebug;
    private Vector2 dashJump;
    private float dashJumpWindow;
    private float wallJumpCompensation;
    private bool enableJumpCompensation;
    private float wallCheckDistance;
    private float wallRaycastTop;
    private float wallRaycastMiddle;
    private float wallRaycastBottom;
    private bool enableCoyoteTime;
    private bool coyoteTimeDuringDashWindow;
    private int maxAirDashes;
    private int maxDashes;

    // External state dependencies
    private bool facingRight;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool onWall;
    private bool isOnSlope;
    private float currentSlopeAngle;
    private Vector2 slopeNormal;
    private float coyoteTimeCounter;
    private bool leftGroundByJumping;
    private bool isBufferClimbing;
    private bool isDashing;
    private float lastDashEndTime;

    // Public jump state
    public bool IsVariableJumpActive { get; private set; }
    public bool IsForcedFalling { get; private set; }
    public int JumpsRemaining { get; set; }
    public float LastJumpTime { get; private set; }
    public float DashJumpTime { get; set; }
    public bool IsJumpHeld { get; set; }
    public bool FacingRight { get; private set; }

    // Internal jump state
    private float jumpHoldTimer;
    private float forcedFallTimer;
    private bool pendingDoubleJump;
    private float originalGravityScaleForJump;
    private float compensatedMinVelocity;
    private float compensatedMaxVelocity;

    /// <summary>
    /// Initialize component references from PlayerController
    /// </summary>
    public void Initialize(Rigidbody2D rigidbody, Transform transform, PlayerGroundDetection ground,
                          PlayerWallDetection wall, PlayerAbilities playerAbilities, Animator playerAnimator)
    {
        rb = rigidbody;
        playerTransform = transform;
        col = GetComponent<Collider2D>();
        groundDetection = ground;
        wallDetection = wall;
        abilities = playerAbilities;
        animator = playerAnimator;
    }

    /// <summary>
    /// Set configuration values from PlayerController
    /// </summary>
    public void SetConfiguration(int _extraJumps, Vector2 _wallJump, bool _enableVariableJump,
                                 float _minJumpVelocity, float _maxJumpVelocity, float _jumpHoldDuration,
                                 float _jumpGravityReduction, float _minDoubleJumpVelocity, float _maxDoubleJumpVelocity,
                                 float _doubleJumpMinDelay, float _forcedFallDuration, float _forcedFallVelocity,
                                 bool _useVelocityClamping, bool _showJumpDebug, Vector2 _dashJump, float _dashJumpWindow,
                                 float _wallJumpCompensation, bool _enableJumpCompensation, float _wallCheckDistance,
                                 float _wallRaycastTop, float _wallRaycastMiddle, float _wallRaycastBottom,
                                 bool _enableCoyoteTime, bool _coyoteTimeDuringDashWindow, int _maxAirDashes, int _maxDashes,
                                 PlayerCombat _combat, InputManager _inputManager)
    {
        extraJumps = _extraJumps;
        wallJump = _wallJump;
        enableVariableJump = _enableVariableJump;
        minJumpVelocity = _minJumpVelocity;
        maxJumpVelocity = _maxJumpVelocity;
        jumpHoldDuration = _jumpHoldDuration;
        jumpGravityReduction = _jumpGravityReduction;
        minDoubleJumpVelocity = _minDoubleJumpVelocity;
        maxDoubleJumpVelocity = _maxDoubleJumpVelocity;
        doubleJumpMinDelay = _doubleJumpMinDelay;
        forcedFallDuration = _forcedFallDuration;
        forcedFallVelocity = _forcedFallVelocity;
        useVelocityClamping = _useVelocityClamping;
        showJumpDebug = _showJumpDebug;
        dashJump = _dashJump;
        dashJumpWindow = _dashJumpWindow;
        wallJumpCompensation = _wallJumpCompensation;
        enableJumpCompensation = _enableJumpCompensation;
        wallCheckDistance = _wallCheckDistance;
        wallRaycastTop = _wallRaycastTop;
        wallRaycastMiddle = _wallRaycastMiddle;
        wallRaycastBottom = _wallRaycastBottom;
        enableCoyoteTime = _enableCoyoteTime;
        coyoteTimeDuringDashWindow = _coyoteTimeDuringDashWindow;
        maxAirDashes = _maxAirDashes;
        maxDashes = _maxDashes;
        combat = _combat;
        inputManager = _inputManager;
    }

    /// <summary>
    /// Update external state (called before jump processing)
    /// </summary>
    public void UpdateExternalState(bool _facingRight, Vector2 _moveInput, bool _isGrounded, bool _onWall,
                                    bool _isOnSlope, float _currentSlopeAngle, Vector2 _slopeNormal,
                                    float _coyoteTimeCounter, bool _leftGroundByJumping, bool _isBufferClimbing,
                                    bool _isDashing, float _lastDashEndTime)
    {
        facingRight = _facingRight;
        FacingRight = _facingRight; // Sync public property
        moveInput = _moveInput;
        isGrounded = _isGrounded;
        onWall = _onWall;
        isOnSlope = _isOnSlope;
        currentSlopeAngle = _currentSlopeAngle;
        slopeNormal = _slopeNormal;
        coyoteTimeCounter = _coyoteTimeCounter;
        leftGroundByJumping = _leftGroundByJumping;
        isBufferClimbing = _isBufferClimbing;
        isDashing = _isDashing;
        lastDashEndTime = _lastDashEndTime;
    }

    /// <summary>
    /// Update variable jump state - called from PlayerController.FixedUpdate()
    /// </summary>
    public void UpdateVariableJump()
    {
        if (!enableVariableJump) return;

        // NOTE: We don't check inputManager.JumpHeld here because of Update/FixedUpdate timing issues
        // The InputManager's JumpHeld reflects the current frame's button state in Update(),
        // but FixedUpdate runs at a different rate and may miss the hold state.
        // Instead, we rely on IsJumpHeld which is set when the button is first pressed
        // and only gets cleared when we explicitly want to end the variable jump.

        // Handle active variable jump
        if (IsVariableJumpActive)
        {
            // Check if still moving upward
            bool movingUpward = rb.linearVelocity.y > 0.1f;

            // Update timer
            jumpHoldTimer += Time.fixedDeltaTime;

            // Check end conditions - use IsJumpHeld instead of jumpCurrentlyHeld
            // IsJumpHeld represents the intended hold state from button press
            if (!IsJumpHeld || !movingUpward || jumpHoldTimer >= jumpHoldDuration)
            {
                EndVariableJump();
            }
            else
            {
                // Use compensated velocities if they were set (for jump compensation)
                float effectiveMinVelocity = compensatedMinVelocity > 0 ? compensatedMinVelocity : minJumpVelocity;
                float effectiveMaxVelocity = compensatedMaxVelocity > 0 ? compensatedMaxVelocity : maxJumpVelocity;

                // If min and max velocities are the same, no variable jump behavior needed
                if (Mathf.Approximately(effectiveMinVelocity, effectiveMaxVelocity))
                {
                    // NO velocity manipulation and NO gravity reduction when Min=Max
                    if (showJumpDebug)
                    {
                        Debug.Log($"[Variable Jump] Min=Max ({effectiveMinVelocity:F1}), no height variance - Timer: {jumpHoldTimer:F2}/{jumpHoldDuration:F2}");
                    }
                }
                else if (useVelocityClamping)
                {
                    // CONSTANT VELOCITY METHOD: Maintain perfectly steady upward velocity
                    float constantVelocity = effectiveMaxVelocity;

                    // Force constant velocity - completely override physics
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, constantVelocity);

                    // Use jumpGravityReduction to maintain compensation compatibility
                    rb.gravityScale = originalGravityScaleForJump * jumpGravityReduction;

                    if (showJumpDebug)
                    {
                        Debug.Log($"[Constant Velocity Jump] Constant Vel: {constantVelocity:F1}, Gravity: {rb.gravityScale:F2}, Timer: {jumpHoldTimer:F2}/{jumpHoldDuration:F2}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handle forced fall state - called from PlayerController.FixedUpdate()
    /// </summary>
    public void UpdateForcedFall()
    {
        if (!IsForcedFalling) return;

        forcedFallTimer += Time.fixedDeltaTime;

        // Maintain forced fall velocity
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcedFallVelocity);

        // Check if forced fall duration is complete
        if (forcedFallTimer >= forcedFallDuration)
        {
            // End forced fall and trigger double jump
            IsForcedFalling = false;

            if (pendingDoubleJump && JumpsRemaining > 0)
            {
                pendingDoubleJump = false;
                PerformDoubleJump();

                if (showJumpDebug)
                {
                    Debug.Log($"[Double Jump] Forced fall complete - executing double jump! Duration: {forcedFallTimer:F2}s");
                }
            }
        }
    }

    /// <summary>
    /// Handle jump input and execution
    /// </summary>
    public bool HandleJumping(bool jumpQueued, out int airDashesRemaining, out int airDashesUsed, out int dashesRemaining,
                             out float coyoteTimeOut, out bool leftGroundByJumpingOut)
    {
        airDashesRemaining = 0;
        airDashesUsed = 0;
        dashesRemaining = 0;
        coyoteTimeOut = coyoteTimeCounter;
        leftGroundByJumpingOut = leftGroundByJumping;

        if (!jumpQueued)
        {
            return false;
        }

        // CRITICAL: Block jumping during active air attacks
        if (combat != null && (combat.IsAirAttacking || (combat.IsDashAttacking && isGrounded)))
        {
            return false;
        }

        // Dash Jump: Jump during active dash or shortly after dash ends
        if (CanPerformDashJump())
        {
            PerformDashJump(out airDashesRemaining, out airDashesUsed);
            return true;
        }

        // PRIORITY 1: Ground jump ALWAYS takes priority over wall jumps
        bool inDashJumpWindow = lastDashEndTime > 0 && Time.time - lastDashEndTime <= dashJumpWindow;
        bool allowCoyoteTime = enableCoyoteTime && (!inDashJumpWindow || coyoteTimeDuringDashWindow);
        bool canGroundJump = isGrounded || (allowCoyoteTime && coyoteTimeCounter > 0f && !leftGroundByJumping);

        if (canGroundJump)
        {
            // Ground jump
            Jump(maxJumpVelocity);
            JumpsRemaining = extraJumps;
            airDashesRemaining = maxAirDashes;
            airDashesUsed = 0;
            dashesRemaining = maxDashes;
            if (animator != null) SafeSetTrigger("Jump");

            // Consume coyote time if it was used
            if (!isGrounded && enableCoyoteTime && coyoteTimeCounter > 0f)
            {
                coyoteTimeOut = 0f;
                leftGroundByJumpingOut = true;
            }

            if (showJumpDebug)
            {
                Debug.Log($"[Jump Priority] Ground jump executed");
            }
            return true;
        }
        // PRIORITY 2: Wall jump (only when airborne and wall stick enabled)
        else if (!isGrounded && onWall && abilities != null && abilities.HasWallStick)
        {
            // Wall jump - flip direction
            facingRight = !facingRight;
            FacingRight = facingRight; // Sync public property
            Jump(wallJump.y, wallJump.x * (facingRight ? 1 : -1));
            JumpsRemaining = extraJumps;
            airDashesRemaining = maxAirDashes;
            airDashesUsed = 0;
            dashesRemaining = maxDashes;
            if (animator != null) SafeSetTrigger("Jump");

            if (showJumpDebug)
            {
                Debug.Log($"[Jump Priority] Wall jump executed");
            }
            return true;
        }
        // PRIORITY 3: Double jump
        else if (JumpsRemaining > 0 && abilities != null && abilities.HasDoubleJump && CanPerformDoubleJump())
        {
            PerformDoubleJump();
            if (combat != null)
            {
                combat.OnDoubleJump();
            }
            airDashesUsed = 0;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Core jump execution with variable jump and compensation support
    /// </summary>
    private void Jump(float yForce, float xForce = 0)
    {
        // For variable jump, set initial velocity directly
        if (enableVariableJump && xForce == 0)
        {
            float actualMinVelocity = minJumpVelocity;
            float actualMaxVelocity = maxJumpVelocity;

            // Apply wall jump compensation
            if (enableJumpCompensation && CheckIfNeedsJumpCompensation())
            {
                float compensationMultiplier = wallJumpCompensation;
                actualMinVelocity *= compensationMultiplier;
                actualMaxVelocity *= compensationMultiplier;
            }

            // SLOPE JUMP COMPENSATION
            float slopeCompensation = 0f;
            if (isOnSlope && Mathf.Abs(moveInput.x) > 0.1f)
            {
                Vector2 slopeUpDirection = new Vector2(slopeNormal.y, -slopeNormal.x).normalized;
                bool runningUpSlope = Vector2.Dot(new Vector2(moveInput.x, 0), slopeUpDirection) > 0;

                if (runningUpSlope)
                {
                    float slopeAngleRatio = currentSlopeAngle / 45f;
                    slopeCompensation = actualMinVelocity * 0.5f * slopeAngleRatio;
                }
            }

            // Set velocity
            float finalVelocity = actualMinVelocity + slopeCompensation;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, finalVelocity);

            // Start variable jump tracking
            IsVariableJumpActive = true;
            jumpHoldTimer = 0f;
            originalGravityScaleForJump = rb.gravityScale;
            compensatedMinVelocity = actualMinVelocity;
            compensatedMaxVelocity = actualMaxVelocity;
            LastJumpTime = Time.time;

            return;
        }

        // Use compensation method for non-variable jumps
        if (enableJumpCompensation)
        {
            JumpWithCompensation(yForce, xForce);
        }
        else
        {
            if (xForce != 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(new Vector2(xForce, yForce), ForceMode2D.Impulse);
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(new Vector2(0, yForce), ForceMode2D.Impulse);
            }
            LastJumpTime = Time.time;
        }
    }

    /// <summary>
    /// Jump with wall friction compensation
    /// </summary>
    private void JumpWithCompensation(float yForce, float xForce = 0)
    {
        // For variable jump, use regular Jump method
        if (enableVariableJump && xForce == 0)
        {
            Jump(yForce, xForce);
            return;
        }

        bool needsCompensation = CheckIfNeedsJumpCompensation();

        if (needsCompensation)
        {
            float compensatedForce = yForce * wallJumpCompensation;
            if (xForce != 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(new Vector2(xForce, compensatedForce), ForceMode2D.Impulse);
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(new Vector2(0, compensatedForce), ForceMode2D.Impulse);
            }
        }
        else
        {
            if (xForce != 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(new Vector2(xForce, yForce), ForceMode2D.Impulse);
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(new Vector2(0, yForce), ForceMode2D.Impulse);
            }
        }

        LastJumpTime = Time.time;
    }

    /// <summary>
    /// Check if jump needs wall friction compensation
    /// </summary>
    private bool CheckIfNeedsJumpCompensation()
    {
        if (!CheckIfAgainstWall())
            return false;

        bool pressingIntoWall = (facingRight && moveInput.x > 0.1f) ||
                                (!facingRight && moveInput.x < -0.1f);

        if (pressingIntoWall)
        {
            if (showJumpDebug)
            {
                Debug.Log($"[Jump Compensation] Wall friction detected");
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if player is against a wall
    /// </summary>
    private bool CheckIfAgainstWall()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        int groundMask = 1 << groundLayer;

        Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;

        Vector2[] checkPoints = {
            playerTransform.position + Vector3.up * wallRaycastTop,
            playerTransform.position + Vector3.up * wallRaycastMiddle,
            playerTransform.position + Vector3.up * wallRaycastBottom
        };

        foreach (Vector2 point in checkPoints)
        {
            RaycastHit2D hit = Physics2D.Raycast(point, wallDirection, wallCheckDistance * 0.7f, groundMask);

            if (hit.collider != null && hit.collider != col)
            {
                if (Mathf.Abs(hit.normal.x) > 0.9f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Check if double jump can be performed
    /// </summary>
    private bool CanPerformDoubleJump()
    {
        float timeSinceLastJump = Time.time - LastJumpTime;
        if (timeSinceLastJump < doubleJumpMinDelay)
        {
            if (showJumpDebug)
            {
                Debug.Log($"[Double Jump] Too soon! Time: {timeSinceLastJump:F2}s");
            }
            return false;
        }

        // If falling, allow immediate double jump
        if (rb.linearVelocity.y < 0f)
        {
            return true;
        }

        // If ascending, initiate forced fall
        if (rb.linearVelocity.y >= 0f)
        {
            if (!IsForcedFalling && !pendingDoubleJump)
            {
                StartForcedFall();
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// Start forced fall before double jump
    /// </summary>
    private void StartForcedFall()
    {
        IsForcedFalling = true;
        forcedFallTimer = 0f;
        pendingDoubleJump = true;

        if (IsVariableJumpActive)
        {
            EndVariableJump();
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcedFallVelocity);
    }

    /// <summary>
    /// Execute double jump
    /// </summary>
    private void PerformDoubleJump()
    {
        if (enableVariableJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, minDoubleJumpVelocity);

            IsVariableJumpActive = true;
            jumpHoldTimer = 0f;
            // FIX: Don't poll inputManager.JumpHeld - use event-based IsJumpHeld that's already set
            // IsJumpHeld should already be true from OnJumpInput event
            originalGravityScaleForJump = rb.gravityScale;
            compensatedMinVelocity = minDoubleJumpVelocity;
            compensatedMaxVelocity = maxDoubleJumpVelocity;
            LastJumpTime = Time.time;
        }
        else
        {
            Jump(maxJumpVelocity * 0.9f);
        }

        JumpsRemaining--;
        if (animator != null) SafeSetTrigger("DoubleJump");

        if (combat != null)
        {
            combat.OnDoubleJump();
        }
    }

    /// <summary>
    /// Check if dash jump can be performed
    /// </summary>
    public bool CanPerformDashJump()
    {
        if (abilities == null || !abilities.GetAbility("dashjump"))
            return false;

        if (!isGrounded)
            return false;

        if (isDashing)
            return true;

        if (lastDashEndTime > 0 && Time.time - lastDashEndTime <= dashJumpWindow)
            return true;

        return false;
    }

    /// <summary>
    /// Perform dash jump
    /// </summary>
    public void PerformDashJump(out int airDashesRemaining, out int airDashesUsed)
    {
        float horizontalForce = facingRight ? dashJump.x : -dashJump.x;

        Vector2 currentVelocity = rb.linearVelocity;

        // Apply dash jump
        rb.linearVelocity = new Vector2(currentVelocity.x, 0);
        rb.AddForce(new Vector2(horizontalForce, dashJump.y), ForceMode2D.Impulse);

        DashJumpTime = Time.time;
        LastJumpTime = Time.time;

        JumpsRemaining = extraJumps;
        airDashesRemaining = maxAirDashes;
        airDashesUsed = 0;

        if (animator != null) SafeSetTrigger("Jump");
    }

    /// <summary>
    /// End variable jump and restore gravity
    /// </summary>
    private void EndVariableJump()
    {
        IsVariableJumpActive = false;
        rb.gravityScale = originalGravityScaleForJump;
        compensatedMinVelocity = 0f;
        compensatedMaxVelocity = 0f;

        if (showJumpDebug)
        {
            Debug.Log($"[Variable Jump] Ended");
        }
    }

    /// <summary>
    /// Safely set animator trigger
    /// </summary>
    private void SafeSetTrigger(string triggerName)
    {
        if (animator == null) return;
        if (!HasAnimatorParameter(triggerName)) return;
        animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Check if animator has parameter
    /// </summary>
    private bool HasAnimatorParameter(string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}
