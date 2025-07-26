using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController_Original : MonoBehaviour, Controls.IGameplayActions
{
    // Tweaks in Inspector
    [Header("Movement")]
    public float runSpeed = 12f;
    public float jumpForce = 16f;
    public int   extraJumps = 1;          // double-jump = 1
    public float wallSlideSpeed = 2f;
    public Vector2 wallJump = new(14f, 20f);

    [Header("Dash")]
    public float dashSpeed = 24f;
    public float dashTime = 0.15f;
    public float dashCooldown = 0.4f;
    public int maxDashes = 2; // Maximum dashes before cooldown
    public int maxAirDashes = 2; // Maximum air dashes allowed per jump

    [Header("Animation")]
    public float animationTransitionSpeed = 0.1f;
    
    [Header("Attack")]
    public float airAttackDuration = 0.8f; // Duration of air attack animation (increased for full animation)
    public float attackDuration = 0.3f; // Duration of ground attack animation
    public float attackMovementSpeed = 0.3f; // Movement speed multiplier during attacks
    public float comboWindowTime = 0.5f; // Time window to input next attack
    public float inputBufferTime = 0.2f; // How long to buffer attack inputs
    
    [Header("Dash Attack")]
    public float dashAttackDuration = 0.4f; // Duration of dash attack
    public float dashAttackSpeed = 4f; // Slight forward speed during dash attack
    public float dashAttackInputWindow = 1.0f; // Time after dash to still accept attack input
    public float dashAttackPreWindow = 0.1f; // Time before dash ends to start accepting input
    
    
    private bool isAirAttacking = false;
    private float originalGravityScale;
    private bool hasUsedAirAttack = false; // Track if air attack was used in current jump
    private int attackCombo = 0;
    private bool isAttacking;
    private bool isDashAttacking = false;
    private bool isAirDashAttacking = false; // Track if dash attack is in air
    private float attackTimer = 0f;
    private float comboWindowTimer = 0f;
    private float inputBufferTimer = 0f;
    private bool attackInputBuffered = false;
    private bool canCombo = false;
    private bool waitingForNextAttack = false; // Waiting for animation to transition
    private Vector2 dashAttackDirection;
    private bool dashAttackQueued = false; // Attack was pressed during dash
    private float dashEndTime = 0f; // When the dash ended
    private float dashStartTime = 0f; // When the dash started
    private bool allowDashAttack = true; // Whether dash attack is allowed after current dash
    private bool dashAttackConsumed = false; // Whether the current dash's attack input has been consumed
    private int attackInputCount = 0; // Debug counter for attack inputs
    private int attackBufferCount = 0; // Debug counter for attack buffers
    private float lastDashInputTime = 0f; // Time of last dash input to prevent spam
    public Collider2D AttackHitbox; // Reference to the attack hitbox
    
    [Header("Ground Check")]
    public float groundCheckOffsetY = 0.01f;
    public float groundCheckRadius = 0.04f;
    
    // Refs
    Rigidbody2D rb;
    Animator animator;
    Controls input;
    Vector2 moveInput;
    bool jumpQueued, dashQueued;
    bool facingRight = true, isDashing, onWall;
    int jumpsRemaining;
    int airDashesRemaining; // Track remaining air dash attacks
    int airDashesUsed; // Track regular air dashes used (separate from attacks)
    int dashesRemaining; // Track remaining dashes before cooldown
    float dashTimer, dashCDTimer;
    

    // Animation state tracking
    private bool isGrounded;
    private bool isRunning;
    private bool isJumping;
    private bool isDashingAnim;
    private bool isClimbing;
    private bool isWallSliding;
    private bool isLedgeGrabbing;
    private bool isFalling;
    private bool isLedgeClimbing;
    public bool IsLedgeClimbing => isLedgeClimbing;
    private float lastLedgeClimbTime;
    private float facingDirection;
    private float horizontalInput;
    private float verticalInput;
    private bool prevOnWall;
    private bool isGroundedByPlatform;
    private bool isGroundedByBuffer;
    private float lastAttackTime = 0f;
    

    // Public properties for external access
    public bool IsGrounded => isGrounded;
    public bool IsRunning => isRunning;
    public bool IsJumping => isJumping;
    public bool IsAttacking => isAttacking;
    public bool IsAirAttacking => isAirAttacking;
    public bool IsDashing => isDashingAnim;
    public bool IsDashAttacking => isDashAttacking;
    public bool IsClimbing => isClimbing;
    public bool IsWallSliding => isWallSliding;
    public bool IsLedgeGrabbing => isLedgeGrabbing;
    public bool IsFalling => isFalling;
    public Vector2 MoveInput => moveInput;
    public bool FacingRight => facingRight;

    // Animation parameter names
    private readonly string[] animParams = {
        "IsGrounded", "IsMoving", "IsJumping", "IsAttacking", "IsDashing", 
        "IsClimbing", "IsWallSliding", "IsLedgeGrabbing", "AttackCombo", 
        "FacingDirection", "HorizontalInput", "VerticalInput"
    };

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        input = new Controls();
        input.Gameplay.SetCallbacks(this);
        Physics2D.queriesStartInColliders = false;
        
        // Store the original gravity scale for restoration
        originalGravityScale = rb.gravityScale;
        
        // Initialize air dash count and dash count
        airDashesRemaining = maxAirDashes;
        dashesRemaining = maxDashes;
        
    }
    void OnEnable()  { if (input != null) input.Enable(); }
    void OnDisable() { if (input != null) input.Disable(); }

    void FixedUpdate()
    {
        if (isDashing)
        {
            // Ensure attack states are cleared during dash
            if (isAttacking && !isDashAttacking)
            {
                ResetAttackSystem();
            }
            
            rb.linearVelocity = new Vector2(facingRight ? dashSpeed : -dashSpeed, 0);
            if ((dashTimer += Time.fixedDeltaTime) >= dashTime) 
            {
                isDashing = false;
                // Always set dash end time - let the attack logic decide if it's valid
                dashEndTime = Time.time;
                dashAttackConsumed = false; // Reset for new dash cycle
                Debug.Log($"Dash ended - dashEndTime={dashEndTime}, dashAttackQueued={dashAttackQueued}, allowDashAttack={allowDashAttack}");
                // Check if dash attack was queued during the dash
                if (dashAttackQueued && allowDashAttack)
                {
                    Debug.Log("Dash attack queued - starting immediately");
                    dashAttackConsumed = true; // Mark as consumed since we're using the queued input
                    StartDashAttack();
                    return; // Don't continue to other movement logic
                }
            }
            return;
        }
        
        // Check for buffered dash attack input (grace period after dash ends)
        // This should ONLY trigger from attack inputs, not dash inputs
        if (!isDashing && !isAttacking && dashEndTime > 0 && Time.time - dashEndTime <= dashAttackInputWindow && allowDashAttack)
        {
            if (attackInputBuffered && inputBufferTimer > 0)  // Only use buffered ATTACK input if it's recent
            {
                Debug.Log("Buffered dash attack triggered in FixedUpdate - from ATTACK input");
                StartDashAttack();
                attackInputBuffered = false;
                inputBufferTimer = 0f;  // Clear the buffer timer
                dashEndTime = 0f;  // Clear dash end time to prevent reuse
                return;
            }
        }
        
        // Clear old dashEndTime if it's past the window
        if (dashEndTime > 0 && Time.time - dashEndTime > dashAttackInputWindow)
        {
            dashEndTime = 0f;
        }
        
        // Emergency fallback: Force reset isDashAttacking if it's been stuck too long
        if (isDashAttacking && Time.time - lastAttackTime > dashAttackDuration * 2f)
        {
            Debug.Log($"EMERGENCY FALLBACK: isDashAttacking stuck for {Time.time - lastAttackTime:F2}s - forcing reset");
            ResetAttackSystem();
        }

        // Ground check (precisely at feet using collider bounds)
        Collider2D col = GetComponent<Collider2D>();
        float feetY = col.bounds.min.y;
        Vector2 feetPos = new Vector2(transform.position.x, feetY + groundCheckOffsetY);
        int groundLayer = LayerMask.NameToLayer("Ground");
        int bufferLayer = LayerMask.NameToLayer("LandingBuffer");
        int groundMask = (1 << groundLayer) | (1 << bufferLayer);
        int platformMask = (1 << groundLayer);
        int bufferMask = (1 << bufferLayer);


        bool groundedByPlatform = Physics2D.OverlapCircle(feetPos, groundCheckRadius, platformMask);
        bool groundedByBuffer = Physics2D.OverlapCircle(feetPos, groundCheckRadius, bufferMask);


        // Expose for debug/logic
        bool wasGrounded = isGrounded;
        isGrounded = (groundedByPlatform || groundedByBuffer) && !onWall;
        isGroundedByPlatform = groundedByPlatform && !onWall;
        isGroundedByBuffer = groundedByBuffer && !groundedByPlatform && !onWall;
        
        // Reset attack system when landing (but not during dash attack)
        if (!wasGrounded && isGrounded && !isDashAttacking)
        {
            ResetAttackSystem();
            hasUsedAirAttack = false; // Reset air attack allowance when landing
            airDashesRemaining = maxAirDashes; // Reset air dash attacks when landing
            airDashesUsed = 0; // Reset air dash usage when landing
            dashesRemaining = maxDashes; // Reset dash count when landing
        }


        // If grounded only by buffer, check input direction
        if (isGroundedByBuffer)
        {
            // Simplified buffer logic: allow movement if there's any horizontal input
            // This prevents "stuck running in air" while still allowing movement back onto platform
            bool hasHorizontalInput = Mathf.Abs(moveInput.x) > 0.05f;
            
            if (!hasHorizontalInput)
            {
                isGrounded = false; // Let the player fall if not pressing any direction
            }
        }

        // Wall check with more comprehensive detection points
        Collider2D playerCollider = GetComponent<Collider2D>();
        bool wallHit = false;
        
        // Use more raycast points to ensure reliable wall detection
        Vector2[] wallCheckOrigins = {
            transform.position + Vector3.up * 0.4f,   // Top
            transform.position + Vector3.up * 0.3f,   // Upper
            transform.position + Vector3.up * 0.15f,  // Middle-upper
            transform.position,                       // Center
            transform.position + Vector3.up * -0.15f, // Middle-lower
            transform.position + Vector3.up * -0.3f   // Lower
        };
        
        foreach (var origin in wallCheckOrigins)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, facingRight ? Vector2.right : Vector2.left, 0.3f, 1<<6);
            Debug.DrawRay(origin, (facingRight ? Vector2.right : Vector2.left) * 0.3f, Color.red, 0.1f);
            if (hit.collider != null && hit.collider != playerCollider)
            {
                if (Mathf.Abs(hit.normal.x) > 0.95f && Mathf.Abs(hit.normal.y) < 0.2f)
                {
                    wallHit = true;
                    break;
                }
            }
        }

        // onWall detection: only when not grounded and either pressing toward wall or falling
        bool pressingTowardWall = (facingRight && moveInput.x > 0.1f) || (!facingRight && moveInput.x < -0.1f);
        onWall = wallHit && !isGrounded && (pressingTowardWall || rb.linearVelocity.y < 0);
        if (onWall != prevOnWall)
        {
            prevOnWall = onWall;
        }
        

        // Update movement state
        isRunning = Mathf.Abs(moveInput.x) > 0.1f && !isDashing && !isDashAttacking && !isAirAttacking;
        isWallSliding = onWall && !isGrounded && rb.linearVelocity.y < -wallSlideSpeed && !isDashing && !isDashAttacking && !isAirAttacking;
        isJumping = !isGrounded && !isWallSliding && !isClimbing && !isLedgeGrabbing && !isDashing && !isDashAttacking && !isAirAttacking && rb.linearVelocity.y > 0;
        isFalling = !isGrounded && !isWallSliding && !isClimbing && !isLedgeGrabbing && !isDashing && !isDashAttacking && !isAirAttacking && rb.linearVelocity.y < 0;
        isDashingAnim = isDashing;

        // Update input values
        horizontalInput = moveInput.x;
        verticalInput = moveInput.y;
        facingDirection = facingRight ? 1f : -1f;
        
        // Horizontal run (skip during air attack as it has its own movement)
        if (!isAirAttacking)
        {
            rb.linearVelocity = new Vector2(moveInput.x * runSpeed, rb.linearVelocity.y);
        }

        // Wall slide slow-down
        if (onWall && !isGrounded && rb.linearVelocity.y < -wallSlideSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);

        // Jump
        if (jumpQueued)
        {
            if (isGrounded)
            {
                Jump(jumpForce);
                jumpsRemaining = extraJumps;          // refresh extras
                airDashesRemaining = maxAirDashes;    // refresh air dash attacks
                airDashesUsed = 0;                   // reset air dash usage
                dashesRemaining = maxDashes;          // refresh dash count
                hasUsedAirAttack = false;            // Reset air attack for new jump
            }
            else if (onWall)
            {
                facingRight = !facingRight;           // flip away from wall
                Jump(wallJump.y, wallJump.x * (facingRight?1:-1));
                jumpsRemaining = extraJumps;          // refresh extras
                airDashesRemaining = maxAirDashes;    // refresh air dash attacks
                airDashesUsed = 0;                   // reset air dash usage
                dashesRemaining = maxDashes;          // refresh dash count
                hasUsedAirAttack = false;            // Reset air attack for wall jump
            }
            else if (jumpsRemaining > 0)
            {
                Jump(jumpForce * 0.9f);
                jumpsRemaining--;
                airDashesUsed = 0;                   // reset air dash usage for double jump
                hasUsedAirAttack = false;            // Reset air attack for double jump
                if (animator != null) animator.SetTrigger("DoubleJump"); // Set trigger
            }
            jumpQueued = false;
        }

        // Regular dash
        bool canDash = false;
        if (isGrounded)
        {
            // Ground dash: can dash if we have dashes remaining OR if cooldown is over
            canDash = (dashesRemaining > 0) || (dashesRemaining <= 0 && dashCDTimer <= 0);
        }
        else
        {
            // Air dash: can only dash if we haven't used our air dash yet
            canDash = (airDashesUsed == 0);
        }
        
        if (dashQueued && !isDashing && canDash)
        {
            if (isGrounded)
            {
                // Ground dash logic
                if (dashesRemaining > 0)
                {
                    // We have dashes remaining - use one
                    dashesRemaining--;
                    
                    // If this was our last dash, start cooldown
                    if (dashesRemaining <= 0)
                    {
                        dashCDTimer = dashCooldown;
                    }
                }
                else
                {
                    // We're out of dashes but cooldown is over - reset and use one
                    dashesRemaining = maxDashes - 1; // Reset to max, then consume one
                    dashCDTimer = 0; // Clear cooldown
                }
            }
            else
            {
                // Air dash logic
                airDashesUsed = 1;
            }
            
            // Cancel any ongoing attack when starting dash
            allowDashAttack = true; // Reset to allow dash attacks by default
            if (isAttacking && !isDashAttacking)
            {
                allowDashAttack = false; // Don't allow dash attack after attack cancel
                ResetAttackSystem();
                // Force immediate animator update to ensure parameters are synced
                UpdateAnimatorParameters();
            }
            
            isDashing = true;
            dashTimer = 0;
            dashStartTime = Time.time;
            dashAttackConsumed = false; // Reset for new dash
            Debug.Log($"NEW DASH STARTED - dashQueued was {dashQueued}, allowDashAttack={allowDashAttack}");
            if (animator != null) animator.SetTrigger("Dash"); // Set dash trigger
            dashQueued = false; // Clear it here after successful dash start
            
            // Also clear any pending dash inputs to prevent immediate re-queuing
            lastDashInputTime = Time.time;
        }
        else
        {
            dashQueued = false; // Clear it if dash didn't start
        }
        
        // Only decrement cooldown if we have no dashes remaining
        if (dashesRemaining <= 0)
        {
            dashCDTimer -= Time.fixedDeltaTime;
        }

        // Handle attack movement (allow reduced movement during attacks)
        if (isAttacking && isGrounded && !isDashAttacking && !isAirAttacking)
        {
            rb.linearVelocity = new Vector2(moveInput.x * runSpeed * attackMovementSpeed, rb.linearVelocity.y);
        }
        
        // Handle air attack - minimal movement control
        if (isAirAttacking)
        {
            // Keep mostly stationary with very slight drift control
            float airDriftFactor = 0.1f;
            rb.linearVelocity = new Vector2(
                moveInput.x * runSpeed * airDriftFactor,
                0f // Keep vertical velocity at 0
            );
        }
        
        // Handle dash attack - slight forward movement during attack
        if (isDashAttacking)
        {
            // Move slightly forward during the attack
            float forwardSpeed = facingRight ? dashAttackSpeed : -dashAttackSpeed;
            // Always maintain zero vertical velocity during dash attacks
            // This prevents falling when ground dash attacks go off platform
            rb.linearVelocity = new Vector2(forwardSpeed, 0f);
        }
        
        // Update attack timers - use reasonable timeout for dash attack and air attack
        if (attackTimer > 0)
        {
            attackTimer -= Time.fixedDeltaTime;
            if (isDashAttacking && attackTimer % 0.1f < 0.02f) // Log every ~0.1 seconds
            {
                Debug.Log($"Dash attack timer: {attackTimer:F2}s remaining");
            }
            if (attackTimer <= 0)
            {
                if (isDashAttacking)
                {
                    // Dash attack should have ended by now
                    Debug.Log("TIMER FALLBACK: Dash attack timer expired - forcing reset");
                    ResetAttackSystem();
                }
                else if (isAirAttacking)
                {
                    // Air attack should have ended by now
                    ResetAttackSystem();
                }
            }
        }
        
        // Update combo window
        if (comboWindowTimer > 0)
        {
            comboWindowTimer -= Time.fixedDeltaTime;
            if (comboWindowTimer <= 0)
            {
                canCombo = false;
            }
        }
        
        // Update input buffer
        if (inputBufferTimer > 0)
        {
            inputBufferTimer -= Time.fixedDeltaTime;
            if (inputBufferTimer <= 0)
            {
                attackInputBuffered = false;
            }
        }
        
        // Process buffered attack - removed since we handle this in EnableComboWindow now

        // Sprite flip
        if (moveInput.x != 0) facingRight = moveInput.x > 0;
        transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);

        // Update animator parameters
        UpdateAnimatorParameters();
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null) return;

        // Update all animator parameters with safety checks
        SafeSetBool("IsGrounded", isGrounded);
        SafeSetBool("IsRunning", isRunning);
        SafeSetBool("IsJumping", isJumping);
        SafeSetBool("IsAttacking", isAttacking);
        SafeSetBool("IsDashAttacking", isDashAttacking);
        SafeSetBool("IsAirAttacking", isAirAttacking);
        SafeSetBool("IsClimbing", isClimbing);
        SafeSetBool("IsWallSliding", isWallSliding);
        SafeSetBool("IsFalling", isFalling);
        SafeSetBool("onWall", onWall);
        SafeSetFloat("FacingDirection", facingDirection);
        SafeSetFloat("HorizontalInput", horizontalInput);
        SafeSetFloat("VerticalInput", verticalInput);
        SafeSetInteger("AttackCombo", attackCombo);
    }
    
    private void SafeSetBool(string paramName, bool value)
    {
        if (HasAnimatorParameter(paramName))
        {
            animator.SetBool(paramName, value);
        }
    }
    
    private void SafeSetFloat(string paramName, float value)
    {
        if (HasAnimatorParameter(paramName))
        {
            animator.SetFloat(paramName, value);
        }
    }
    
    private void SafeSetInteger(string paramName, int value)
    {
        if (HasAnimatorParameter(paramName))
        {
            animator.SetInteger(paramName, value);
        }
    }
    
    private bool HasAnimatorParameter(string paramName)
    {
        if (animator == null) return false;
        
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName)
            {
                return true;
            }
        }
        return false;
    }

    // Public methods for external state changes (e.g., from ledge grab detection)
    public void SetClimbing(bool climbing)
    {
        isClimbing = climbing;
    }

    public void SetLedgeGrabbing(bool ledgeGrabbing)
    {
        isLedgeGrabbing = ledgeGrabbing;
    }

    public void SetAttacking(bool attacking)
    {
        isAttacking = attacking;
    }

    public void SetAttackCombo(int combo)
    {
        attackCombo = combo;
    }
    
    private void ResetAttackSystem()
    {
        Debug.Log($"ResetAttackSystem called - was isDashAttacking: {isDashAttacking}");
        isAttacking = false;
        isDashAttacking = false;
        isAirAttacking = false;
        isAirDashAttacking = false;
        dashAttackQueued = false;
        dashEndTime = 0f; // Clear dash end time
        attackCombo = 0;
        canCombo = false;
        attackInputBuffered = false;
        waitingForNextAttack = false;
        attackTimer = 0f;
        comboWindowTimer = 0f;
        inputBufferTimer = 0f;
        
        // Always restore gravity to original value
        if (rb != null)
        {
            rb.gravityScale = originalGravityScale;
        }
        
        // Cancel any pending invokes
        CancelInvoke(nameof(EnableComboWindow));
        CancelInvoke(nameof(RestartComboLoop));
        CancelInvoke(nameof(CheckDashAttackStuck));
        
        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsDashAttacking", false);
            animator.SetBool("IsAirAttacking", false);
            animator.SetInteger("AttackCombo", 0);
            
            // Force immediate state machine evaluation
            animator.Update(0);
            
            // If still in attack state, force exit with crossfade (but not for air attacks)
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.IsName("PlayerSwordStab") ||
                currentState.IsName("PlayerSwordChop") ||
                currentState.IsName("PlayerSwordSwing3") ||
                currentState.IsName("DashAttack"))
            {
                // Force transition to base layer idle state for ground attacks
                animator.CrossFade("DaggerIdle", 0f, 0);
            }
            else if (currentState.IsName("PlayerAirSwordSwing"))
            {
                // For air attacks, let the state machine handle transitions naturally
            }
        }
        if (AttackHitbox != null)
        {
            AttackHitbox.enabled = false;
        }
    }

    /* — IGameplayActions — */
    public void OnMove  (InputAction.CallbackContext c) => moveInput   = c.ReadValue<Vector2>();
    public void OnJump  (InputAction.CallbackContext c){ if (c.started) jumpQueued = true; }
    public void OnDash  (InputAction.CallbackContext c)
    { 
        if (c.started) 
        { 
            // Add detailed logging to track phantom inputs
            Debug.Log($"DASH INPUT RECEIVED - Control: {c.control.name}, Time:{Time.time:F3}, Frame:{Time.frameCount}, isDashing:{isDashing}, isDashAttacking:{isDashAttacking}, Context:{c.phase}");
            
            // Prevent spam dash inputs (debounce) - allow legitimate chains but prevent rapid spam
            if (Time.time - lastDashInputTime < 0.05f) 
            {
                Debug.Log($"DASH INPUT IGNORED - too soon after last input ({Time.time - lastDashInputTime:F3}s ago)");
                return;
            }
            
            lastDashInputTime = Time.time;
            dashQueued = true; 
        } 
    }
    public void OnAttack(InputAction.CallbackContext c)
    {
        if (c.started)
        {
            attackInputCount++;
            Debug.Log($"ATTACK INPUT #{attackInputCount} - Control: {c.control.name}, States: isDashing={isDashing}, isAttacking={isAttacking}, dashEndTime={dashEndTime}, timeSinceDash={Time.time - dashEndTime:F2}");
            
            // Check if we're dashing - queue dash attack (allow even if already attacking for double dash)
            if (isDashing && allowDashAttack)
            {
                // Only queue if we're actually dashing or near the end of dash
                Debug.Log($"Dash timing check - dashTimer={dashTimer:F2}, dashTime={dashTime}, preWindow={dashAttackPreWindow}, threshold={dashTime - dashAttackPreWindow:F2}");
                if (dashTimer >= (dashTime - dashAttackPreWindow))
                {
                    dashAttackQueued = true;
                    Debug.Log("Dash attack queued during dash - setting attack trigger");
                    // Set attack trigger for animator transition
                    if (animator != null)
                    {
                        animator.SetTrigger("Attack");
                    }
                    // Don't cut dash short - let it complete naturally and then trigger attack
                    return;
                }
                else
                {
                    Debug.Log("Dash attack NOT queued - too early in dash");
                }
            }
            
            // Check for dash attack in grace period after dash
            if (!isDashing && !isAttacking && dashEndTime > 0 && Time.time - dashEndTime <= dashAttackInputWindow && !dashAttackConsumed)
            {
                if (allowDashAttack)
                {
                    Debug.Log("Dash attack triggered in grace period - setting attack trigger");
                    // Set attack trigger for animator transition
                    if (animator != null)
                    {
                        animator.SetTrigger("Attack");
                    }
                    StartDashAttack();
                    // Mark this dash's attack input as consumed
                    dashAttackConsumed = true;
                    return;
                }
                else
                {
                    Debug.Log("Dash attack blocked - allowDashAttack is false");
                    // Mark as consumed even if blocked
                    dashAttackConsumed = true;
                }
            }
            
            // Check for air attack - only if not used yet
            if (!isGrounded && !isAttacking && !isDashAttacking && !isDashing && !onWall && !hasUsedAirAttack)
            {
                StartAirAttack();
                return;
            }
            
            // Check if we can execute the attack immediately
            if (!isAttacking && !isDashAttacking && !isDashing)
            {
                // If we're past the dash attack window, clear it and allow regular attacks
                if (dashEndTime > 0 && Time.time - dashEndTime > dashAttackInputWindow)
                {
                    dashEndTime = 0f;
                }
                
                // Start regular attack if grounded and not in dash attack window
                if (isGrounded && dashEndTime == 0)
                {
                    Debug.Log("Attack executing immediately - not buffered");
                    StartAttackCombo();
                    return; // Don't buffer if we executed immediately
                }
            }
            
            // Check if we can do a combo attack
            if (isAttacking && !isDashAttacking && !isAirAttacking)
            {
                // If in combo window, process immediately
                if (canCombo && !waitingForNextAttack)
                {
                    Debug.Log("Combo attack executing immediately - not buffered");
                    PerformNextCombo();
                    return; // Don't buffer if we executed immediately
                }
            }
            
            // Only buffer if we couldn't execute immediately
            attackInputBuffered = true;
            inputBufferTimer = inputBufferTime;
            attackBufferCount++;
            Debug.Log($"Attack input buffered #{attackBufferCount} - inputBufferTimer set to {inputBufferTime} (couldn't execute immediately)");
        }
    }
    
    private void StartAttackCombo()
    {
        
        attackCombo = 1;
        isAttacking = true;
        attackTimer = attackDuration;
        canCombo = false;
        attackInputBuffered = false;
        waitingForNextAttack = false;
        lastAttackTime = Time.time;
        
        if (animator != null)
        {
            animator.SetInteger("AttackCombo", attackCombo);
            animator.SetBool("IsAttacking", true);
            // Don't use trigger, let the state machine handle transitions
            
            // Force immediate update
            animator.Update(0);
        }
        
        if (AttackHitbox != null)
        {
            AttackHitbox.enabled = true;
        }
        
        // Enable combo window halfway through the attack
        Invoke(nameof(EnableComboWindow), attackDuration * 0.5f);
    }
    
    private void StartDashAttack()
    {
        // Check if we're in air for air dash attack
        isAirDashAttacking = !isGrounded;
        
        Debug.Log($"StartDashAttack called - isAirDashAttacking: {isAirDashAttacking}, airDashesRemaining: {airDashesRemaining}");
        
        // Consume air dash if this is an air dash attack
        if (isAirDashAttacking)
        {
            if (airDashesRemaining <= 0)
            {
                Debug.Log("Dash attack BLOCKED - no air dashes remaining");
                return; // Don't start if no air dashes left
            }
            airDashesRemaining--;
            Debug.Log($"Air dash consumed - remaining: {airDashesRemaining}");
        }
        
        // Start dash attack
        isDashAttacking = true;
        isAttacking = true;
        dashAttackQueued = false; // Clear the queue
        attackTimer = dashAttackDuration;
        Debug.Log($"Dash attack timer set to {attackTimer}");
        canCombo = false;
        attackInputBuffered = false;
        waitingForNextAttack = false;
        inputBufferTimer = 0f;
        comboWindowTimer = 0f;
        lastAttackTime = Time.time;
        
        // Set dash attack direction
        dashAttackDirection = facingRight ? Vector2.right : Vector2.left;
        
        // Clear dash end time since we're now attacking
        dashEndTime = 0f;
        Debug.Log("Cleared dashEndTime in StartDashAttack");
        
        // Also clear any queued dash inputs to prevent confusion
        dashQueued = false;
        
        // Always disable gravity during dash attack to prevent falling
        // This ensures ground dash attacks that go off platform behave like air dash attacks
        // Only store original gravity if not already stored (don't overwrite)
        if (rb.gravityScale != 0f)
        {
            originalGravityScale = rb.gravityScale;
        }
        rb.gravityScale = 0f;
        // Stop vertical movement
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        
        if (animator != null)
        {
            animator.SetBool("IsDashAttacking", true);
            animator.SetBool("IsAttacking", true);
            // Ensure attack trigger is set for transition
            animator.SetTrigger("Attack");
            
            // Force immediate animator update
            animator.Update(0);
            
            // Check what state we're in after setting parameters
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            
            // If we're not transitioning to dash attack, force it
            if (!currentState.IsName("DashAttack"))
            {
                animator.CrossFade("DashAttack", 0.1f, 0);
            }
        }
        
        if (AttackHitbox != null)
        {
            AttackHitbox.enabled = true;
        }
        
        // Start safety check for stuck state - check more frequently
        InvokeRepeating(nameof(CheckDashAttackStuck), 0.5f, 0.1f);
    }
    
    // Called by animation event at the start of dash attack animation
    public void OnDashAttackAnimationStart()
    {
        if (isDashAttacking)
        {
            attackTimer = dashAttackDuration;
            lastAttackTime = Time.time;
            
            if (AttackHitbox != null)
            {
                AttackHitbox.enabled = true;
            }
            
        }
    }
    
    // Called by animation event at the end of dash attack animation
    public void OnDashAttackAnimationEnd()
    {
        if (isDashAttacking)
        {
            Debug.Log($"Dash attack animation ended - isAirDashAttacking={isAirDashAttacking}, airDashesRemaining={airDashesRemaining}");
            
            // Restore gravity for all dash attacks (both air and ground)
            rb.gravityScale = originalGravityScale;
            
            // Apply slight downward velocity to start falling naturally
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f);
            
            attackTimer = 0;
            ResetAttackSystem();
        }
    }
    
    // Fallback safety check in case animation events don't fire
    private void CheckDashAttackStuck()
    {
        if (isDashAttacking && Time.time - lastAttackTime > dashAttackDuration * 2f)
        {
            ResetAttackSystem();
        }
    }
    
    private void StartAirAttack()
    {
        isAirAttacking = true;
        isAttacking = true;
        hasUsedAirAttack = true; // Mark air attack as used
        attackTimer = airAttackDuration;
        canCombo = false;
        attackInputBuffered = false;
        waitingForNextAttack = false;
        lastAttackTime = Time.time;
        
        // Store original gravity scale and set to 0 during air attack
        // Only store original gravity if not already stored (don't overwrite)
        if (rb.gravityScale != 0f)
        {
            originalGravityScale = rb.gravityScale;
        }
        rb.gravityScale = 0f; // No gravity during air attack
        
        // Stop vertical movement and add slight forward movement
        float airAttackForwardSpeed = 4f; // Reduced forward speed for more control
        rb.linearVelocity = new Vector2(
            facingRight ? airAttackForwardSpeed : -airAttackForwardSpeed,
            0f // No vertical movement - stay in place
        );
        
        if (animator != null)
        {
            animator.SetBool("IsAirAttacking", true);
            animator.SetBool("IsAttacking", true);
            
            // Force immediate state transition
            animator.CrossFade("PlayerAirSwordSwing", 0.05f, 0);
        }
        
        if (AttackHitbox != null)
        {
            AttackHitbox.enabled = true;
        }
        
    }
    
    // Called by animation event at the start of air attack animation
    public void OnAirAttackAnimationStart()
    {
        if (isAirAttacking)
        {
            attackTimer = airAttackDuration;
            lastAttackTime = Time.time;
            
            if (AttackHitbox != null)
            {
                AttackHitbox.enabled = true;
            }
            
        }
    }
    
    // Called by animation event at the end of air attack animation
    public void OnAirAttackAnimationEnd()
    {
        if (isAirAttacking)
        {
            // Restore original gravity
            rb.gravityScale = originalGravityScale;
            
            // Apply slight downward velocity to start falling
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f);
            
            attackTimer = 0;
            ResetAttackSystem();
        }
    }
    
    
    private void PerformNextCombo()
    {
        // Don't increment combo immediately - wait for animation
        waitingForNextAttack = true;
        canCombo = false;
        attackInputBuffered = false;
        
        // Increment combo or loop back to 1
        if (attackCombo >= 3)
        {
            // Loop back to first attack
            attackCombo = 1;
            if (animator != null)
            {
                // Force a clean transition by briefly disabling and re-enabling attack
                animator.SetBool("IsAttacking", false);
                animator.SetInteger("AttackCombo", 0);
                // Re-enable on next frame
                Invoke(nameof(RestartComboLoop), 0.05f);
            }
        }
        else
        {
            // Continue to next attack in combo
            attackCombo++;
            if (animator != null)
            {
                animator.SetInteger("AttackCombo", attackCombo);
                // Keep IsAttacking true for smooth transitions
            }
        }
    }
    
    private void RestartComboLoop()
    {
        if (animator != null && waitingForNextAttack)
        {
            animator.SetBool("IsAttacking", true);
            animator.SetInteger("AttackCombo", 1);
        }
    }
    
    // Called by animation event at the start of each attack animation
    public void OnAttackAnimationStart()
    {
        // Always process this if we're in an attack state
        if (isAttacking)
        {
            waitingForNextAttack = false;
            attackTimer = attackDuration;
            lastAttackTime = Time.time;
            
            if (AttackHitbox != null)
            {
                AttackHitbox.enabled = true;
            }
            
            // Cancel any pending invokes to prevent overlap
            CancelInvoke(nameof(EnableComboWindow));
            
            // Enable combo window halfway through the attack
            Invoke(nameof(EnableComboWindow), attackDuration * 0.5f);
        }
    }
    
    private void EnableComboWindow()
    {
        if (isAttacking)
        {
            canCombo = true;
            comboWindowTimer = comboWindowTime;
            
            // Check if there's a buffered input waiting
            if (attackInputBuffered && !waitingForNextAttack)
            {
                PerformNextCombo();
            }
        }
    }
    
    private void EndCurrentAttack()
    {
        if (AttackHitbox != null)
        {
            AttackHitbox.enabled = false;
        }
        
        // Check if we should continue combo
        if (attackInputBuffered && !waitingForNextAttack)
        {
            PerformNextCombo();
        }
        else if (!waitingForNextAttack)
        {
            // End the combo only if we're truly done
            ResetAttackSystem();
        }
    }

    // Called by Animation Event at the end of each attack animation
    public void OnAttackAnimationEnd()
    {
        // Always end the current attack when animation ends
        if (isAttacking && !isDashAttacking) // Don't interfere with dash attack
        {
            // Clear the timer to prevent double-ending
            attackTimer = 0;
            
            // Always process the end, even if waiting for next attack
            EndCurrentAttack();
        }
    }
    


    /* — Helpers — */
    void Jump(float yForce, float xForce = 0)
    {
        // If jumping during air attack, we need to cancel the air attack and restore gravity
        if (isAirAttacking)
        {
            rb.gravityScale = originalGravityScale;
            ResetAttackSystem();
        }
        // If jumping during air dash attack, we need to cancel it and restore gravity
        else if (isAirDashAttacking)
        {
            rb.gravityScale = originalGravityScale;
            ResetAttackSystem();
        }
        
        rb.linearVelocity = new Vector2(xForce == 0 ? rb.linearVelocity.x : xForce, 0);
        rb.AddForce(new Vector2(0, yForce), ForceMode2D.Impulse);
    }

    // Remove the DoAttack coroutine as we're using timer-based system now

    // Debug info
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 280));
        GUILayout.Label("Player State Debug:", GUI.skin.label);
        GUILayout.Label($"Grounded: {isGrounded}");
        GUILayout.Label($"Grounded by Platform: {isGroundedByPlatform}");
        GUILayout.Label($"Grounded by Buffer: {isGroundedByBuffer}");
        GUILayout.Label($"Moving: {isRunning}");
        GUILayout.Label($"Jumping: {isJumping}");
        GUILayout.Label($"Falling: {isFalling}");
        GUILayout.Label($"Attacking: {isAttacking}");
        GUILayout.Label($"Dashing: {isDashingAnim} (isDashing: {isDashing})");
        GUILayout.Label($"Wall Sliding: {isWallSliding}");
        GUILayout.Label($"On Wall: {onWall}");
        GUI.contentColor = isClimbing ? Color.green : Color.white;
        GUILayout.Label($"Climbing: {isClimbing}");
        GUI.contentColor = Color.white;
        GUILayout.Label($"Ledge Grabbing: {isLedgeGrabbing}");
        GUILayout.Label($"Attack Combo: {attackCombo}");
        GUILayout.Label($"Air Attacking: {isAirAttacking}");
        GUILayout.Label($"Has Used Air Attack: {hasUsedAirAttack}");
        GUILayout.Label($"Dash Attacking: {isDashAttacking}");
        GUILayout.Label($"Air Dash Attacking: {isAirDashAttacking}");
        GUILayout.Label($"Dash Attack Queued: {dashAttackQueued}");
        GUILayout.Label($"Can Combo: {canCombo}");
        GUILayout.Label($"Jumps Remaining: {jumpsRemaining}");
        GUILayout.Label($"Dashes Remaining: {dashesRemaining}");
        GUILayout.Label($"Air Dash Used: {airDashesUsed > 0}");
        GUILayout.Label($"Air Dash Attacks Remaining: {airDashesRemaining}");
        GUILayout.Label($"Attack Timer: {attackTimer:F2}");
        GUILayout.Label($"Dash Timer: {dashTimer:F2}/{dashTime:F2}");
        GUILayout.Label($"Dash Cooldown: {dashCDTimer:F2}");
        GUILayout.Label($"Combo Window: {comboWindowTimer:F2}");
        
        // Show animator state info
        if (animator != null)
        {
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            GUILayout.Label($"Current Anim State: {currentState.shortNameHash}");
            GUILayout.Label($"Is DashAttack: {currentState.IsName("DashAttack")}");
        }
        GUILayout.Label($"Input Buffered: {attackInputBuffered} ({inputBufferTimer:F2})");
        GUILayout.Label($"Waiting for Next: {waitingForNextAttack}");
        GUILayout.Label($"Time Since Last Attack: {Time.time - lastAttackTime:F2}s");
        GUILayout.Label($"Facing: {(facingRight ? "Right" : "Left")}");
        
        // Emergency reset button for testing
        if (isDashAttacking && GUILayout.Button("FORCE RESET DASH ATTACK"))
        {
            ResetAttackSystem();
        }
        GUILayout.EndArea();
    }

    // Debug visualization for wall detection
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        Vector3 playerPos = transform.position;
        Vector3 direction = facingRight ? Vector3.right : Vector3.left;
        
        // Wall detection rays - only show when not grounded
        if (!isGrounded)
        {
            Gizmos.color = onWall ? Color.red : Color.yellow;
            Gizmos.DrawRay(playerPos + Vector3.up * 0.5f, direction * 0.6f);
            Gizmos.DrawRay(playerPos, direction * 0.6f);
            Gizmos.DrawRay(playerPos + Vector3.down * 0.5f, direction * 0.6f);
        }
        else
        {
            // Show disabled rays when grounded
            Gizmos.color = Color.gray;
            Gizmos.DrawRay(playerPos + Vector3.up * 0.5f, direction * 0.6f);
            Gizmos.DrawRay(playerPos, direction * 0.6f);
            Gizmos.DrawRay(playerPos + Vector3.down * 0.5f, direction * 0.6f);
        }
        
        // Ground detection
        Gizmos.color = isGrounded ? Color.green : Color.blue;
        Gizmos.DrawWireSphere(playerPos + Vector3.down * 0.6f, 0.1f);
        
        // Draw precise ground check using current parameters
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            float feetY = col.bounds.min.y;
            Vector2 feetPos = new Vector2(transform.position.x, feetY + groundCheckOffsetY);
            
            // Main ground check
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(feetPos, groundCheckRadius);
            
            // Buffer detection visualization
            if (Application.isPlaying)
            {
                int groundLayer = LayerMask.NameToLayer("Ground");
                int bufferLayer = LayerMask.NameToLayer("LandingBuffer");
                int platformMask = (1 << groundLayer);
                int bufferMask = (1 << bufferLayer);
                
                bool groundedByPlatform = Physics2D.OverlapCircle(feetPos, groundCheckRadius, platformMask);
                bool groundedByBuffer = Physics2D.OverlapCircle(feetPos, groundCheckRadius, bufferMask);
                
                // Platform check - draw as a ring
                Gizmos.color = groundedByPlatform ? Color.green : Color.red;
                Gizmos.DrawWireSphere(feetPos, groundCheckRadius + 0.02f);
                
                // Buffer check - draw as a ring
                Gizmos.color = groundedByBuffer ? Color.yellow : Color.cyan;
                Gizmos.DrawWireSphere(feetPos, groundCheckRadius + 0.04f);
                
                // Platform direction checks (for buffer logic)
                float checkDist = 0.15f;
                Vector2 leftCheck = feetPos + Vector2.left * checkDist;
                Vector2 rightCheck = feetPos + Vector2.right * checkDist;
                bool platformLeft = Physics2D.OverlapCircle(leftCheck, groundCheckRadius * 0.7f, platformMask);
                bool platformRight = Physics2D.OverlapCircle(rightCheck, groundCheckRadius * 0.7f, platformMask);
                
                Gizmos.color = platformLeft ? Color.green : Color.red;
                Gizmos.DrawWireSphere(leftCheck, groundCheckRadius * 0.7f);
                
                Gizmos.color = platformRight ? Color.green : Color.red;
                Gizmos.DrawWireSphere(rightCheck, groundCheckRadius * 0.7f);
                
            }
        }
    }
}
