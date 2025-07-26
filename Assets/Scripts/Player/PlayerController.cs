using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerCombat))] // Combat is now required for full functionality
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float runSpeed = 6f;
    public float jumpForce = 8f;
    public int extraJumps = 1;
    public float wallSlideSpeed = 2f;
    public Vector2 wallJump = new(7f, 10f);

    [Header("Dash")]
    public float dashSpeed = 10f;
    public float dashTime = 0.25f;
    public float dashCooldown = 0.4f;
    public int maxDashes = 2;
    public int maxAirDashes = 2;

    [Header("Buffer Climbing")]
    [SerializeField] private float climbingAssistanceOffset = 0.2f; // How far below platform edge to trigger assistance
    [SerializeField] private float climbForce = 3.0f;
    [SerializeField] private float forwardBoost = 1.2f;
    [SerializeField] private bool showClimbingGizmos = true;
    
    [Header("Animation")]
    public float animationTransitionSpeed = 0.1f;
    private HashSet<string> missingAnimatorParams = new HashSet<string>();
    private bool hasLoggedAnimatorWarnings = false;
    
    
    [Header("Ground Check")]
    public float groundCheckOffsetY = 0f;
    public float groundCheckRadius = 0.03f;
    
    [Header("Wall Detection")]
    public float wallCheckDistance = 0.15f;
    
    // Component references
    private Rigidbody2D rb;
    private Animator animator;
    private PlayerCombat combat;
    private InputManager inputManager;
    
    // Input
    private Vector2 moveInput;
    private bool jumpQueued, dashQueued;
    
    // Movement state
    private bool facingRight = true;
    private bool isDashing;
    private bool onWall;
    private int jumpsRemaining;
    private int airDashesRemaining;
    private int airDashesUsed;
    private int dashesRemaining;
    private float dashTimer, dashCDTimer;
    private float lastDashInputTime = 0f;
    private float lastDashEndTime = 0f;
    private float lastLandTime = 0f;
    private float lastJumpTime = 0f;
    private bool wasGroundedBeforeDash = false;
    
    // Animation state tracking
    private bool isGrounded;
    private bool isRunning;
    private bool wallContact; // Simple wall contact detection
    
    // Ground detection state (shared between CheckGrounding and CheckWallDetection)
    private bool groundedByBuffer;
    private bool pressingHorizontally;
    private bool isBufferClimbing;
    private bool isJumping;
    private bool isDashingAnim;
    private bool isClimbing;
    private bool isWallSliding;
    private bool isWallSticking;
    private bool isLedgeGrabbing;
    private bool wallStickAllowed; // Track if wall stick animation should play
    private bool isFalling;
    private bool isLedgeClimbing;
    private float facingDirection;
    private float horizontalInput;
    private float verticalInput;
    private bool prevOnWall;
    private bool isGroundedByPlatform;
    private bool isGroundedByBuffer;
    
    // Basic attack state for compatibility (when no PlayerCombat)
    private bool isAttacking = false;
    
    // Public properties for external access
    public bool IsGrounded => isGrounded;
    public bool IsRunning => isRunning;
    public bool IsJumping => isJumping;
    public bool IsAttacking => combat?.IsAttacking ?? isAttacking;
    public bool IsAirAttacking => combat?.IsAirAttacking ?? false;
    public bool IsDashing => isDashing;
    public bool IsDashAttacking => combat?.IsDashAttacking ?? false;
    public bool IsClimbing => isClimbing;
    public bool IsWallSliding => isWallSliding;
    public bool IsWallSticking => isWallSticking;
    public bool IsLedgeGrabbing => isLedgeGrabbing;
    public bool IsFalling => isFalling;
    public bool IsLedgeClimbing => isLedgeClimbing;
    public Vector2 MoveInput => moveInput;
    public bool FacingRight => facingRight;
    public bool OnWall => onWall;
    public float RunSpeed => runSpeed;
    public float DashTimer => dashTimer;
    public float DashTime => dashTime;
    public int AirDashesRemaining => airDashesRemaining;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        combat = GetComponent<PlayerCombat>();
        Physics2D.queriesStartInColliders = false;
        
        // Verify required components
        VerifyComponentSetup();
        
        // Initialize dash counts
        airDashesRemaining = maxAirDashes;
        dashesRemaining = maxDashes;
    }
    
    void Start()
    {
        // Validate animator parameters on start
        ValidateAnimatorSetup();
    }
    
    private void VerifyComponentSetup()
    {
        // Check required components
        if (rb == null)
        {
            Debug.LogError($"[PlayerController] Rigidbody2D component missing on {gameObject.name}!");
        }
        
        if (animator == null)
        {
            Debug.LogError($"[PlayerController] Animator component missing on {gameObject.name}!");
        }
        
        if (combat == null)
        {
            Debug.LogError($"[PlayerController] PlayerCombat component missing on {gameObject.name}! Combat functionality will be disabled.");
            #if UNITY_EDITOR
            // In editor, offer to add the component
            if (UnityEditor.EditorUtility.DisplayDialog("Missing PlayerCombat Component",
                $"The GameObject '{gameObject.name}' is missing the PlayerCombat component. Would you like to add it?",
                "Yes", "No"))
            {
                combat = gameObject.AddComponent<PlayerCombat>();
                Debug.Log($"[PlayerController] Added PlayerCombat component to {gameObject.name}");
            }
            #endif
        }
        
        // Validate layer setup
        int groundLayer = LayerMask.NameToLayer("Ground");
        int bufferLayer = LayerMask.NameToLayer("LandingBuffer");
        
        if (groundLayer == -1)
        {
            Debug.LogError("[PlayerController] 'Ground' layer is not defined in project settings!");
        }
        
        if (bufferLayer == -1)
        {
            Debug.LogWarning("[PlayerController] 'LandingBuffer' layer is not defined. Edge detection may not work properly.");
        }
    }
    
    private void ValidateAnimatorSetup()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        
        // List of required animator parameters
        string[] requiredBools = { "IsGrounded", "IsRunning", "IsJumping", "IsAttacking", 
                                  "IsDashAttacking", "IsAirAttacking", "IsClimbing", 
                                  "IsWallSliding", "IsFalling", "onWall" };
        string[] requiredFloats = { "FacingDirection", "HorizontalInput", "VerticalInput" };
        string[] requiredInts = { "AttackCombo" };
        string[] requiredTriggers = { "Dash", "DoubleJump" };
        
        List<string> missingParams = new List<string>();
        
        // Check bools
        foreach (var param in requiredBools)
        {
            if (!HasAnimatorParameter(param))
                missingParams.Add($"{param} (Bool)");
        }
        
        // Check floats
        foreach (var param in requiredFloats)
        {
            if (!HasAnimatorParameter(param))
                missingParams.Add($"{param} (Float)");
        }
        
        // Check ints
        foreach (var param in requiredInts)
        {
            if (!HasAnimatorParameter(param))
                missingParams.Add($"{param} (Int)");
        }
        
        // Check triggers
        foreach (var param in requiredTriggers)
        {
            if (!HasAnimatorParameter(param))
                missingParams.Add($"{param} (Trigger)");
        }
        
        if (missingParams.Count > 0)
        {
            Debug.LogWarning($"[PlayerController] Animator Controller is missing parameters:\n" +
                string.Join("\n", missingParams.Select(p => $"  • {p}")) +
                "\n\nPlease add these parameters to your Animator Controller.");
        }
    }
    
    void OnEnable() 
    { 
        // Try to get InputManager instance, with retry for timing issues
        if (InputManager.Instance != null)
        {
            SetupInputManager();
        }
        else
        {
            // Retry after a frame in case InputManager hasn't initialized yet
            StartCoroutine(RetryInputManagerSetup());
        }
    }
    
    private void SetupInputManager()
    {
        inputManager = InputManager.Instance;
        if (inputManager != null)
        {
            // Subscribe to input events
            inputManager.OnMoveInput += OnMoveInput;
            inputManager.OnJumpPressed += OnJumpInput;
            inputManager.OnDashPressed += OnDashInput;
            inputManager.OnAttackPressed += OnAttackInput;
        }
    }
    
    private System.Collections.IEnumerator RetryInputManagerSetup()
    {
        yield return null; // Wait one frame
        
        if (InputManager.Instance != null)
        {
            SetupInputManager();
        }
        else
        {
            Debug.LogError("InputManager instance not found! Make sure InputManager is in the scene.");
        }
    }
    
    void OnDisable() 
    { 
        if (inputManager != null)
        {
            // Unsubscribe from input events
            inputManager.OnMoveInput -= OnMoveInput;
            inputManager.OnJumpPressed -= OnJumpInput;
            inputManager.OnDashPressed -= OnDashInput;
            inputManager.OnAttackPressed -= OnAttackInput;
        }
    }

    void FixedUpdate()
    {
        // Update moveInput from InputManager if available
        if (inputManager != null)
        {
            moveInput = inputManager.MoveInput;
        }
        
        // Removed complex horizontal movement tracking
        
        // Check for buffered combat actions
        combat?.CheckBufferedDashAttack();
        
        // Ground and wall detection
        CheckGrounding();
        CheckWallDetection();
        
        // Update movement states
        UpdateMovementStates();
        
        // Handle movement
        HandleMovement();
        
        // Handle jumping
        HandleJumping();
        
        // Handle dashing
        HandleDashInput();
        
        // Apply dash velocity after other movement (to override it)
        if (isDashing)
        {
            // Clear attack states during dash (like original)
            if (combat != null && combat.IsAttacking && !combat.IsDashAttacking)
            {
                combat.ResetAttackSystem();
            }
            
            rb.linearVelocity = new Vector2(facingRight ? dashSpeed : -dashSpeed, 0);
            if ((dashTimer += Time.fixedDeltaTime) >= dashTime)
            {
                isDashing = false;
                lastDashEndTime = Time.time;
                combat?.OnDashEnd();
            }
        }
        
        // Update dash cooldown
        UpdateDashCooldown();
        
        // Sprite flip and animation updates
        UpdateSpriteFacing();
        UpdateAnimatorParameters();
        
    }
    
    
    private void CheckGrounding()
    {
        Collider2D col = GetComponent<Collider2D>();
        float feetY = col.bounds.min.y;
        Vector2 feetPos = new Vector2(transform.position.x, feetY + groundCheckOffsetY);
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


        bool wasGrounded = isGrounded;
        
        // Special case: If player is pressing horizontally and buffer is detected,
        // allow "climbing" onto platform even if wall is detected (platform edge climbing)
        pressingHorizontally = Mathf.Abs(moveInput.x) > 0.1f;
        
        // Check if player is within climbing assistance range of a platform edge
        bool nearPlatformEdge = CheckClimbingAssistanceZone();
        
        isBufferClimbing = groundedByBuffer && pressingHorizontally && nearPlatformEdge &&
                           Mathf.Abs(rb.linearVelocity.y) < 1.0f; // Not falling too fast
        
        
        // Standard grounding logic with platform edge climbing exception
        if (isBufferClimbing)
        {
            // Buffer climbing takes priority - disable wall state to prevent animation conflict
            isGrounded = true;
            isGroundedByPlatform = false;
            isGroundedByBuffer = true;
            // Force wall state off during buffer climbing
            onWall = false;
        }
        else
        {
            // Normal grounding logic - player is grounded if touching ground, regardless of wall state
            isGrounded = groundedByPlatform || groundedByBuffer;
            isGroundedByPlatform = groundedByPlatform;
            isGroundedByBuffer = groundedByBuffer && !groundedByPlatform;
        }
        
        
        // Handle landing
        if (!wasGrounded && isGrounded)
        {
            combat?.OnLanding();
            airDashesRemaining = maxAirDashes;
            airDashesUsed = 0;
            dashesRemaining = maxDashes;
            lastLandTime = Time.time; // Track landing time for wall detection
        }

        // Buffer logic for edge platforms
        if (isGroundedByBuffer)
        {
            bool hasHorizontalInput = Mathf.Abs(moveInput.x) > 0.05f;
            if (!hasHorizontalInput)
            {
                isGrounded = false;
            }
        }
    }
    
    private void CheckWallDetection()
    {
        // Simple wall detection - no complex timing or state persistence
        Collider2D playerCollider = GetComponent<Collider2D>();
        int groundLayer = LayerMask.NameToLayer("Ground");
        int groundMask = 1 << groundLayer;
        
        // Basic wall contact detection using 3 raycasts
        Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
        Vector2[] checkPoints = {
            transform.position + Vector3.up * 0.3f,   // Upper
            transform.position,                       // Center
            transform.position + Vector3.down * 0.3f  // Lower
        };
        
        bool hasWallContact = false;
        
        foreach (Vector2 point in checkPoints)
        {
            RaycastHit2D hit = Physics2D.Raycast(point, wallDirection, wallCheckDistance, groundMask);
            
            if (hit.collider != null && hit.collider != playerCollider)
            {
                // Check if it's a valid vertical wall (normal pointing away from wall)
                bool isVerticalWall = Mathf.Abs(hit.normal.x) > 0.9f;
                if (isVerticalWall)
                {
                    hasWallContact = true;
                    break;
                }
            }
        }
        
        // Input checks
        bool pressingTowardWall = (facingRight && moveInput.x > 0.1f) || (!facingRight && moveInput.x < -0.1f);
        bool notMovingAwayFromWall = !((facingRight && moveInput.x < -0.1f) || (!facingRight && moveInput.x > 0.1f));
        
        // SIMPLE WALL LOGIC:
        // Wall stick: Player actively pressing toward wall
        bool canStickToWall = !isGrounded && hasWallContact && pressingTowardWall && !isBufferClimbing;
        
        // Wall slide: Player touching wall but not pressing toward it (released input)
        bool canSlideOnWall = !isGrounded && hasWallContact && notMovingAwayFromWall && !pressingTowardWall && !isBufferClimbing;
        
        // Set physics state - either sticking or sliding
        onWall = canStickToWall || canSlideOnWall;
        
        // Store wall stick state for animation
        wallStickAllowed = canStickToWall;
    }
    
    private void UpdateMovementStates()
    {
        // Store previous states for debugging
        bool prevIsFalling = isFalling;
        bool prevIsGrounded = isGrounded;
        
        // Calculate wall states
        isWallSliding = onWall && rb.linearVelocity.y < -wallSlideSpeed && !isDashing && !IsDashAttacking && !IsAirAttacking;
        isWallSticking = wallStickAllowed && !isWallSliding && !isDashing && !IsDashAttacking && !IsAirAttacking;
        
        // Then calculate running state (which depends on wall states)
        isRunning = Mathf.Abs(moveInput.x) > 0.1f && !isDashing && !IsDashAttacking && !IsAirAttacking && !onWall && !isWallSticking;
        
        isJumping = !isGrounded && !isWallSliding && !isWallSticking && !isClimbing && !isLedgeGrabbing && !isDashing && !IsDashAttacking && !IsAirAttacking && rb.linearVelocity.y > 0;
        
        isFalling = !isGrounded && !isWallSliding && !isWallSticking && !isClimbing && !isLedgeGrabbing && !IsDashAttacking && !IsAirAttacking && rb.linearVelocity.y < 0;
        isDashingAnim = isDashing;

        horizontalInput = moveInput.x;
        verticalInput = moveInput.y;
        facingDirection = facingRight ? 1f : -1f;
    }
    
    private void HandleMovement()
    {
        // Buffer climbing assistance - provide upward and forward momentum
        if (isBufferClimbing)
        {
            // Apply upward assist to help climb over platform edge
            if (rb.linearVelocity.y <= 0.5f) // Only if not already moving up significantly
            {
                rb.linearVelocity = new Vector2(
                    moveInput.x * runSpeed * forwardBoost, 
                    Mathf.Max(rb.linearVelocity.y, climbForce)
                );
            }
            else
            {
                // Just apply forward momentum if already moving up
                rb.linearVelocity = new Vector2(moveInput.x * runSpeed * forwardBoost, rb.linearVelocity.y);
            }
            
            // Debug.Log($"[BUFFER CLIMBING ASSIST] Applied climbing force: upward={climbForce}, forward boost={forwardBoost}");
            return; // Skip normal movement processing
        }
        
        // Horizontal run (skip during air attack and dashing as they have their own movement)
        if (!IsAirAttacking && !isDashing)
        {
            rb.linearVelocity = new Vector2(moveInput.x * runSpeed, rb.linearVelocity.y);
        }

        // Wall slide slow-down
        if (onWall && !isGrounded && rb.linearVelocity.y < -wallSlideSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
        
        // Handle attack movement overrides (allow reduced movement during attacks) - EXACTLY like original
        if (IsAttacking && isGrounded && !IsDashAttacking && !IsAirAttacking)
        {
            float attackSpeedMultiplier = combat?.attackMovementSpeed ?? 0.3f;
            rb.linearVelocity = new Vector2(moveInput.x * runSpeed * attackSpeedMultiplier, rb.linearVelocity.y);
        }
        
        // Let combat system handle air attack and dash attack movement
        Vector2 combatMovement = combat?.GetAttackMovement() ?? Vector2.zero;
        if (combatMovement != Vector2.zero && (IsDashAttacking || IsAirAttacking))
        {
            rb.linearVelocity = new Vector2(combatMovement.x, combatMovement.y != 0 ? combatMovement.y : rb.linearVelocity.y);
        }
    }
    
    private void HandleJumping()
    {
        if (!jumpQueued) return;
        
        // CRITICAL: Block jumping during active air attacks to prevent infinite elevation exploit
        if (combat != null && (combat.IsAirAttacking || combat.IsDashAttacking))
        {
            jumpQueued = false;
            return;
        }
        
        if (isGrounded)
        {
            Jump(jumpForce);
            jumpsRemaining = extraJumps;
            airDashesRemaining = maxAirDashes;
            airDashesUsed = 0;
            dashesRemaining = maxDashes;
        }
        else if (onWall)
        {
            facingRight = !facingRight;
            Jump(wallJump.y, wallJump.x * (facingRight ? 1 : -1));
            jumpsRemaining = extraJumps;
            airDashesRemaining = maxAirDashes;
            airDashesUsed = 0;
            dashesRemaining = maxDashes;
        }
        else if (jumpsRemaining > 0)
        {
            Jump(jumpForce * 0.9f);
            jumpsRemaining--;
            airDashesUsed = 0;
            if (animator != null) SafeSetTrigger("DoubleJump");
            
            // Notify combat system about double jump
            if (combat != null)
            {
                combat.OnDoubleJump();
            }
        }
        
        jumpQueued = false;
    }
    
    private void HandleDashInput()
    {
        bool canDash = false;
        if (isGrounded)
        {
            canDash = (dashesRemaining > 0) || (dashesRemaining <= 0 && dashCDTimer <= 0);
        }
        else
        {
            canDash = (airDashesUsed == 0);
        }
        
        if (dashQueued && !isDashing && canDash && !onWall)
        {
            if (isGrounded)
            {
                if (dashesRemaining > 0)
                {
                    dashesRemaining--;
                    if (dashesRemaining <= 0)
                    {
                        dashCDTimer = dashCooldown;
                    }
                }
                else
                {
                    dashesRemaining = maxDashes - 1;
                    dashCDTimer = 0;
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
            
            if (animator != null) SafeSetTrigger("Dash");
            dashQueued = false;
            lastDashInputTime = Time.time;
        }
        else
        {
            dashQueued = false;
        }
    }
    
    private void UpdateDashCooldown()
    {
        if (dashesRemaining <= 0)
        {
            dashCDTimer -= Time.fixedDeltaTime;
        }
    }
    
    private void UpdateSpriteFacing()
    {
        bool oldFacing = facingRight;
        if (moveInput.x != 0) facingRight = moveInput.x > 0;
        transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
        
        // Debug facing changes
        if (oldFacing != facingRight)
        {
            Debug.Log($"[FACING CHANGE] From {(oldFacing ? "Right" : "Left")} to {(facingRight ? "Right" : "Left")} | moveInput.x: {moveInput.x:F3}");
        }
    }
    
    private void UpdateAnimatorParameters()
    {
        if (animator == null) return;

        SafeSetBool("IsGrounded", isGrounded);
        SafeSetBool("IsRunning", isRunning);
        SafeSetBool("IsJumping", isJumping);
        SafeSetBool("IsAttacking", IsAttacking);
        SafeSetBool("IsDashAttacking", IsDashAttacking);
        SafeSetBool("IsAirAttacking", IsAirAttacking);
        SafeSetBool("IsClimbing", isClimbing);
        SafeSetBool("IsWallSliding", isWallSliding);
        SafeSetBool("IsWallSticking", isWallSticking);
        SafeSetBool("IsFalling", isFalling);
        SafeSetBool("onWall", onWall); // Use onWall physics state for animator (both stick and slide)
        SafeSetFloat("FacingDirection", facingDirection);
        SafeSetFloat("HorizontalInput", horizontalInput);
        
        // Combined parameter for wall land animation: use same threshold as onWall logic for consistency
        bool pressingTowardWallStrong = (facingRight && horizontalInput > 0.1f) || (!facingRight && horizontalInput < -0.1f);
        SafeSetBool("PressingTowardWall", pressingTowardWallStrong);
        
        SafeSetFloat("VerticalInput", verticalInput);
        SafeSetInteger("AttackCombo", combat?.AttackCombo ?? 0);
        
        // Debug animator parameter updates when falling (commented out for performance)
        // if (isFalling)
        // {
        //     Debug.Log($"Animator Update - IsFalling: {isFalling}, IsGrounded: {isGrounded}, isDashing: {IsDashAttacking}, velocity.y: {rb.linearVelocity.y:F2}");
        // }
        
        // Log missing parameters once
        if (!hasLoggedAnimatorWarnings && missingAnimatorParams.Count > 0)
        {
            hasLoggedAnimatorWarnings = true;
            Debug.LogWarning($"[PlayerController] Animator is missing the following parameters: {string.Join(", ", missingAnimatorParams)}\n" +
                "Please add these parameters to your Animator Controller or the animations may not work correctly.");
        }
    }
    
    private void SafeSetBool(string paramName, bool value)
    {
        if (HasAnimatorParameter(paramName))
        {
            // Debug critical animator parameters (commented out for performance)
            // if (paramName == "IsGrounded" || paramName == "IsFalling")
            // {
            //     Debug.Log($"Setting Animator: {paramName} = {value}");
            // }
            animator.SetBool(paramName, value);
        }
        else
        {
            missingAnimatorParams.Add(paramName);
        }
    }
    
    private void SafeSetFloat(string paramName, float value)
    {
        if (HasAnimatorParameter(paramName))
        {
            animator.SetFloat(paramName, value);
        }
        else
        {
            missingAnimatorParams.Add(paramName);
        }
    }
    
    private void SafeSetInteger(string paramName, int value)
    {
        if (HasAnimatorParameter(paramName))
        {
            animator.SetInteger(paramName, value);
        }
        else
        {
            missingAnimatorParams.Add(paramName);
        }
    }
    
    private void SafeSetTrigger(string paramName)
    {
        if (HasAnimatorParameter(paramName))
        {
            animator.SetTrigger(paramName);
        }
        else
        {
            missingAnimatorParams.Add(paramName);
            Debug.LogWarning($"[PlayerController] Animator trigger '{paramName}' not found in Animator Controller!");
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

    private void Jump(float yForce, float xForce = 0)
    {
        rb.linearVelocity = new Vector2(xForce == 0 ? rb.linearVelocity.x : xForce, 0);
        rb.AddForce(new Vector2(0, yForce), ForceMode2D.Impulse);
        lastJumpTime = Time.time; // Track jump time for wall stick timing
    }
    
    public void ConsumeAirDash()
    {
        airDashesRemaining--;
    }
    
    public void ConsumeJump()
    {
        if (jumpsRemaining > 0)
        {
            jumpsRemaining--;
        }
    }
    
    private IEnumerator BasicAttack()
    {
        isAttacking = true;
        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }

    // Public methods for external state changes
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
        // Delegate to combat component - this is for backward compatibility
    }

    public void SetAttackCombo(int combo)
    {
        // Delegate to combat component - this is for backward compatibility
    }

    /* — Input Event Handlers — */
    private void OnMoveInput(Vector2 input)
    {
        moveInput = input;
    }
    
    private void OnJumpInput()
    {
        jumpQueued = true;
    }
    
    private void OnDashInput()
    {
        if (Time.time - lastDashInputTime < 0.05f)
        {
            return;
        }
        
        // Prevent dash input when on wall
        if (onWall)
        {
            return;
        }
        
        lastDashInputTime = Time.time;
        dashQueued = true;
    }
    
    private void OnAttackInput()
    {
        if (combat != null)
        {
            combat.HandleAttackInput();
        }
        else
        {
            // Basic attack when PlayerCombat is not attached
            Debug.Log("Attack input received - using basic attack (add PlayerCombat for full combat system)");
            StartCoroutine(BasicAttack());
        }
    }

    // Debug info
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 350, 600));
        GUILayout.Label("=== WALL LAND DEBUG ===", GUI.skin.label);
        
        // Key states only
        GUILayout.Label($"Grounded: {isGrounded}");
        GUILayout.Label($"Moving: {isRunning}");
        GUI.contentColor = isWallSticking ? Color.green : Color.red;
        GUILayout.Label($"Wall Sticking: {isWallSticking}");
        GUI.contentColor = onWall ? Color.green : Color.red;
        GUILayout.Label($"On Wall: {onWall}");
        GUI.contentColor = Color.white;
        
        // Simple wall detection breakdown
        GUILayout.Label("\n--- SIMPLE WALL DETECTION ---");
        GUI.contentColor = wallContact ? Color.green : Color.red;
        GUILayout.Label($"Wall Contact: {wallContact}");
        GUI.contentColor = Color.white;
        
        GUILayout.Label($"wallCheckDistance: {wallCheckDistance:F2}");
        GUILayout.Label($"Player Position: {transform.position:F2}");
        GUILayout.Label($"Facing Right: {facingRight}");
        GUILayout.Label($"moveInput.x: {moveInput.x:F3}");
        
        // Simple wall stick conditions
        GUILayout.Label("\n--- SIMPLE WALL STICK CONDITIONS ---");
        bool pressingTowardWall = (facingRight && moveInput.x > 0.1f) || (!facingRight && moveInput.x < -0.1f);
        
        GUI.contentColor = !isGrounded ? Color.green : Color.red;
        GUILayout.Label($"1. !isGrounded: {!isGrounded}");
        GUI.contentColor = wallContact ? Color.green : Color.red;
        GUILayout.Label($"2. wallContact: {wallContact}");
        GUI.contentColor = pressingTowardWall ? Color.green : Color.red;
        GUILayout.Label($"3. pressingTowardWall: {pressingTowardWall}");
        GUI.contentColor = !isBufferClimbing ? Color.green : Color.red;
        GUILayout.Label($"4. !isBufferClimbing: {!isBufferClimbing}");
        GUI.contentColor = Color.white;
        
        // Animation triggers
        GUILayout.Label("\n--- ANIMATION TRIGGERS ---");
        bool pressingTowardWallAnim = (facingRight && horizontalInput > 0.1f) || (!facingRight && horizontalInput < -0.1f);
        GUILayout.Label($"IsWallSticking: {isWallSticking}");
        GUILayout.Label($"PressingTowardWall: {pressingTowardWallAnim}");
        
        GUILayout.EndArea();
    }

    private bool CheckClimbingAssistanceZone()
    {
        // Get player position
        Vector3 playerPos = transform.position;
        float checkDirection = facingRight ? 1f : -1f;
        
        int groundLayer = LayerMask.NameToLayer("Ground");
        int groundMask = 1 << groundLayer;
        
        // Check if there's a platform edge slightly above and in front of the player
        // This detects when player is positioned below a platform edge (where wall-stick happens)
        Vector2 platformCheckOrigin = playerPos + Vector3.up * climbingAssistanceOffset + Vector3.right * checkDirection * 0.1f;
        Vector2 checkDown = Vector2.down;
        float checkDistance = climbingAssistanceOffset + 0.3f; // Check down from above player
        
        RaycastHit2D platformHit = Physics2D.Raycast(platformCheckOrigin, checkDown, checkDistance, groundMask);
        
        // Also check if player is near a wall (platform edge) horizontally
        Vector2 wallCheckOrigin = playerPos;
        Vector2 checkHorizontal = checkDirection > 0 ? Vector2.right : Vector2.left;
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheckOrigin, checkHorizontal, 0.3f, groundMask);
        
        // Return true if we're below a platform edge AND near a wall
        bool belowPlatformEdge = platformHit.collider != null;
        bool nearWallEdge = wallHit.collider != null;
        
        return belowPlatformEdge && nearWallEdge;
    }
    
    // Debug visualization for wall detection
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        Vector3 playerPos = transform.position;
        Vector3 direction = facingRight ? Vector3.right : Vector3.left;
        
        // Simple wall detection rays - 3 rays only
        if (!isGrounded)
        {
            Gizmos.color = onWall ? Color.red : Color.yellow;
            // Draw the 3 simple wall check rays
            Gizmos.DrawRay(playerPos + Vector3.up * 0.3f, direction * wallCheckDistance);    // Upper
            Gizmos.DrawRay(playerPos, direction * wallCheckDistance);                        // Center
            Gizmos.DrawRay(playerPos + Vector3.down * 0.3f, direction * wallCheckDistance);  // Lower
        }
        else
        {
            Gizmos.color = Color.gray;
            // Show disabled wall detection when grounded
            Gizmos.DrawRay(playerPos + Vector3.up * 0.3f, direction * wallCheckDistance);
            Gizmos.DrawRay(playerPos, direction * wallCheckDistance);
            Gizmos.DrawRay(playerPos + Vector3.down * 0.3f, direction * wallCheckDistance);
        }
        
        // Ground detection
        Gizmos.color = isGrounded ? Color.green : Color.blue;
        Gizmos.DrawWireSphere(playerPos + Vector3.down * 0.6f, 0.1f);
        
        // Precise ground check visualization
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            float feetY = col.bounds.min.y;
            Vector2 feetPos = new Vector2(transform.position.x, feetY + groundCheckOffsetY);
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(feetPos, groundCheckRadius);
            
            if (Application.isPlaying)
            {
                int groundLayer = LayerMask.NameToLayer("Ground");
                int bufferLayer = LayerMask.NameToLayer("LandingBuffer");
                int platformMask = (1 << groundLayer);
                int bufferMask = (1 << bufferLayer);
                
                bool groundedByPlatform = Physics2D.OverlapCircle(feetPos, groundCheckRadius, platformMask);
                bool groundedByBuffer = Physics2D.OverlapCircle(feetPos, groundCheckRadius, bufferMask);
                
                // Apply velocity restriction to buffer detection (same as main logic)
                if (groundedByBuffer && rb.linearVelocity.y > 0.1f)
                {
                    groundedByBuffer = false;
                }
                
                Gizmos.color = groundedByPlatform ? Color.green : Color.red;
                Gizmos.DrawWireSphere(feetPos, groundCheckRadius + 0.02f);
                
                Gizmos.color = groundedByBuffer ? Color.yellow : Color.cyan;
                Gizmos.DrawWireSphere(feetPos, groundCheckRadius + 0.04f);
                
                
                // Climbing assistance zone visualization
                if (showClimbingGizmos)
                {
                    bool nearPlatformEdge = CheckClimbingAssistanceZone();
                    float checkDirection = facingRight ? 1f : -1f;
                    
                    // Show vertical detection zone above player
                    Vector3 platformCheckOrigin = playerPos + Vector3.up * climbingAssistanceOffset + Vector3.right * checkDirection * 0.1f;
                    float checkDistance = climbingAssistanceOffset + 0.3f;
                    
                    // Draw platform detection raycast
                    Gizmos.color = nearPlatformEdge ? Color.orange : Color.gray;
                    Gizmos.DrawRay(platformCheckOrigin, Vector3.down * checkDistance);
                    Gizmos.DrawWireSphere(platformCheckOrigin, 0.05f);
                    
                    // Draw horizontal wall detection
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawRay(playerPos, Vector3.right * checkDirection * 0.3f);
                    
                    // Draw offset zone indicator
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(playerPos, playerPos + Vector3.up * climbingAssistanceOffset);
                    
                    // Show activation status
                    Gizmos.color = isBufferClimbing ? Color.green : Color.red;
                    Gizmos.DrawWireSphere(playerPos + Vector3.up * 0.5f, 0.1f);
                    
                    // Draw climbing force visualization when active
                    if (isBufferClimbing)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawRay(playerPos, Vector3.up * climbForce * 0.2f); // Scale for visibility
                        
                        Gizmos.color = Color.blue;
                        Gizmos.DrawRay(playerPos, direction * forwardBoost);
                    }
                }
            }
        }
    }
}