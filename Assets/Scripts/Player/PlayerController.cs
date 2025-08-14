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
    public float runSpeed = 5f;
    public float jumpForce = 9f;
    public int extraJumps = 1;
    public float wallSlideSpeed = 2f;
    public Vector2 wallJump = new(7f, 10f);

    [Header("Dash")]
    public float dashSpeed = 8f;
    public float dashTime = 0.25f;
    public float dashCooldown = 0.4f;
    public int maxDashes = 2;
    public int maxAirDashes = 2;
    
    [Header("Dash Jump")]
    [SerializeField] public Vector2 dashJump = new(5f, 9f); // (horizontal, vertical) force
    [SerializeField] public float dashJumpWindow = 0.2f; // Grace period after dash ends
    
    [Header("Death Zone")]
    [SerializeField] private float deathZoneY = -20f; // Y position that triggers reset
    [SerializeField] private float deathZoneWidth = 100f; // Width of death zone visualization
    [SerializeField] private bool showDeathZone = true; // Show death zone in scene view

    [Header("Buffer Climbing")]
    [SerializeField] private float climbingAssistanceOffset = 0.25f; // How far below platform edge to trigger assistance
    [SerializeField] private float climbForce = 3f;
    [SerializeField] private float forwardBoost = 0f;
    
    [Header("Coyote Time")]
    [SerializeField] private bool enableCoyoteTime = true; // Enable coyote time feature
    [SerializeField] private float coyoteTimeDuration = 0.12f; // Grace period after leaving ground
    [SerializeField] private bool coyoteTimeDuringDashWindow = false; // Allow coyote time during dash jump window
    [SerializeField] private bool showCoyoteTimeDebug = false; // Show debug info for coyote time
    [SerializeField] private bool showClimbingGizmos = true;
    
    [Header("Jump Compensation")]
    [SerializeField] private float wallJumpCompensation = 1.2f; // Multiplier to counteract friction
    [SerializeField] private bool enableJumpCompensation = true;
    
    [Header("Animation")]
    public float animationTransitionSpeed = 0.1f;
    private HashSet<string> missingAnimatorParams = new HashSet<string>();
    private bool hasLoggedAnimatorWarnings = false;
    
    
    [Header("Ground Check")]
    public float groundCheckOffsetY = -0.02f;
    public float groundCheckRadius = 0.03f;
    
    [Header("Wall Detection")]
    public float wallCheckDistance = 0.15f;
    
    [Header("Wall Detection Raycasts")]
    [SerializeField] private float wallRaycastTop = 0.32f;    // Top raycast position
    [SerializeField] private float wallRaycastMiddle = 0.28f; // Middle raycast position  
    [SerializeField] private float wallRaycastBottom = 0.02f; // Bottom raycast position
    
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
    
    // Dash jump momentum preservation
    private float dashJumpTime = 0f;
    private float dashJumpMomentumDuration = 0.3f; // Preserve momentum for 0.3 seconds
    
    // Death/Reset system
    private Vector3 initialPosition;
    
    // Wall state sequence tracking
    private bool wasWallSticking = false;
    private bool hasEverWallStuck = false;
    
    // Coyote time tracking
    private float coyoteTimeCounter = 0f;
    private bool leftGroundByJumping = false;
    
    // Head stomp permission system
    private bool canHeadStomp = true;
    
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
    
    // Jump compensation state
    private bool isCompensatingJump = false;
    
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
    public bool CanHeadStomp => canHeadStomp;
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
        
        // Ensure input is set up if InputManager is available
        if (InputManager.Instance != null && inputManager == null)
        {
            SetupInputManager();
        }
        
        // Debug: Check for physics materials that might cause unwanted friction
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider != null && playerCollider.sharedMaterial != null)
        {
            PhysicsMaterial2D mat = playerCollider.sharedMaterial;
            // Debug.LogWarning($"[WallStick] Player has physics material: friction={mat.friction}, bounciness={mat.bounciness}. This might cause unwanted wall sticking!");
        }
        
        // Auto-calculate upper-mid raycast position to match top of player collider
        CalculateOptimalRaycastPositions();
        
        // Store initial position for death/reset system
        initialPosition = transform.position;
        // Debug.Log($"[Death/Reset] Initial position stored: {initialPosition}");
    }
    
    private void CalculateOptimalRaycastPositions()
    {
        // Get the player's BoxCollider2D to verify raycast positions
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            // Calculate the top edge of the collider relative to transform position
            float colliderHalfHeight = boxCollider.size.y * 0.5f;
            float colliderTopOffset = boxCollider.offset.y + colliderHalfHeight;
            
            // Debug.Log($"[WallStick] Simplified wall detection initialized - Using 3 raycasts at: Top={wallRaycastTop:F3}, Middle={wallRaycastMiddle:F3}, Bottom={wallRaycastBottom:F3}");
            // Debug.Log($"[WallStick] Collider top offset: {colliderTopOffset:F3} for reference");
        }
        else
        {
            // Debug.LogWarning("[WallStick] BoxCollider2D not found - using default raycast positions");
        }
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
        else
        {
            Debug.LogError("[PlayerController] InputManager is NULL!");
        }
        
        // Removed complex horizontal movement tracking
        
        // Check for buffered combat actions
        combat?.CheckBufferedDashAttack();
        
        // Simple death zone check - if player falls below deathZoneY, reset
        if (transform.position.y < deathZoneY)
        {
            // Debug.Log($"[Death/Reset] Player fell below death zone (Y < -20). Current Y: {transform.position.y}");
            ResetToInitialPosition();
            return; // Skip rest of frame after reset
        }
        
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
            
            // Check if dash should end early due to wall collision
            bool dashEndedByWall = CheckDashWallCollision();
            
            rb.linearVelocity = new Vector2(facingRight ? dashSpeed : -dashSpeed, 0);
            
            // End dash if timer expires OR wall collision detected
            if ((dashTimer += Time.fixedDeltaTime) >= dashTime || dashEndedByWall)
            {
                isDashing = false;
                lastDashEndTime = Time.time;
                combat?.OnDashEnd();
                
                if (dashEndedByWall)
                {
                    // Allow falling immediately after wall collision
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                }
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
        
        // DEBUG: Log when ground detection might be causing corner sticking (disabled - root cause identified)
        // if (PlayerAbilities.Instance != null && !PlayerAbilities.Instance.HasWallStick && 
        //     groundedByPlatform && Mathf.Abs(rb.linearVelocity.x) < 0.1f && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
        // {
        //     Debug.Log($"[CORNER STICK DEBUG] Grounded detected at corner - feetPos: {feetPos}, velocity: {rb.linearVelocity}");
        // }
        
        

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
            // OLD FIX: False grounding override (likely not needed with wall friction fix)
            // bool potentiallyStuckAtCorner = false;
            // if (PlayerAbilities.Instance != null && !PlayerAbilities.Instance.HasWallStick)
            // {
            //     // This fix is disabled as the root cause is likely fixed in wall friction prevention
            // }
            
            // Normal grounding logic
            isGrounded = groundedByPlatform || groundedByBuffer;
            isGroundedByPlatform = groundedByPlatform;
            isGroundedByBuffer = groundedByBuffer && !groundedByPlatform;
        }
        
        
        // Handle landing
        if (!wasGrounded && isGrounded)
        {
            combat?.OnLanding();
            airDashesRemaining = maxAirDashes;
            canHeadStomp = true; // Reset head stomp on landing
            airDashesUsed = 0;
            dashesRemaining = maxDashes;
            lastLandTime = Time.time; // Track landing time for wall detection
            
            // Clear dash jump momentum preservation on landing
            dashJumpTime = 0f;
        }

        // Coyote time tracking
        if (enableCoyoteTime)
        {
            if (isGrounded)
            {
                // Reset coyote time when grounded
                coyoteTimeCounter = coyoteTimeDuration;
                leftGroundByJumping = false;
            }
            else if (wasGrounded && !isGrounded)
            {
                // Just left ground - check if it was from jumping
                if (rb.linearVelocity.y > 0.5f) // Positive Y velocity suggests jumping
                {
                    // Left ground by jumping - disable coyote time
                    leftGroundByJumping = true;
                    coyoteTimeCounter = 0f;
                }
                else
                {
                    // Left ground by walking/falling off - preserve coyote time
                    leftGroundByJumping = false;
                    // Keep existing counter value
                }
            }
            else if (!isGrounded && coyoteTimeCounter > 0f)
            {
                // Decrement coyote time while airborne
                coyoteTimeCounter -= Time.fixedDeltaTime;
                if (coyoteTimeCounter < 0f)
                {
                    coyoteTimeCounter = 0f;
                }
            }
        }
        else
        {
            // Coyote time disabled - always reset
            coyoteTimeCounter = 0f;
            leftGroundByJumping = false;
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
        // Simplified wall detection using only 3 raycasts
        Collider2D playerCollider = GetComponent<Collider2D>();
        int groundLayer = LayerMask.NameToLayer("Ground");
        int groundMask = 1 << groundLayer;
        
        // Wall detection using 3 raycasts at specified heights
        Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
        Vector2[] checkPoints = {
            transform.position + Vector3.up * wallRaycastTop,    // Top (0.32)
            transform.position + Vector3.up * wallRaycastMiddle, // Middle (0.28)
            transform.position + Vector3.up * wallRaycastBottom  // Bottom (0.02)
        };
        
        // Count how many raycasts hit a wall
        int wallHitCount = 0;
        
        foreach (Vector2 point in checkPoints)
        {
            RaycastHit2D hit = Physics2D.Raycast(point, wallDirection, wallCheckDistance, groundMask);
            
            if (hit.collider != null && hit.collider != playerCollider)
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
        bool hasWallStickAbility = PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick;
        
        if (hasWallStickAbility)
        {
            // When wall stick enabled: Need at least 2 raycasts hitting to allow wall stick
            bool hasEnoughContactForWallStick = wallHitCount >= 2;
            
            // Wall stick: Player actively pressing toward wall with enough contact
            bool canStickToWall = !isGrounded && hasEnoughContactForWallStick && pressingTowardWall && !isBufferClimbing;
            
            // Wall slide: Player touching wall with enough contact but not pressing toward it
            bool canSlideOnWall = !isGrounded && hasEnoughContactForWallStick && notMovingAwayFromWall && !pressingTowardWall && !isBufferClimbing;
            
            // Set physics state - either sticking or sliding
            onWall = canStickToWall || canSlideOnWall;
            
            // Store wall stick state for animation
            wallStickAllowed = canStickToWall;
        }
        else
        {
            // Wall stick ability disabled - no wall interaction at all
            onWall = false;
            wallStickAllowed = false;
            
            // Wall stick disabled - no wall interactions allowed
        }
    }
    
    private void UpdateMovementStates()
    {
        // Store previous states for debugging
        bool prevIsFalling = isFalling;
        bool prevIsGrounded = isGrounded;
        
        // CRITICAL: Sequential wall state logic - stick must come before slide
        // First, calculate if wall sticking conditions are met (requires wall stick ability)
        bool canWallStick = wallStickAllowed && !isDashing && !IsDashAttacking && !IsAirAttacking &&
                           PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick;
        isWallSticking = canWallStick;
        
        // Debug: Check if wall stick ability is disabled but onWall is somehow true
        if (PlayerAbilities.Instance != null && !PlayerAbilities.Instance.HasWallStick && onWall)
        {
            // Debug.LogError($"[WallStick] BUG DETECTED! Wall stick disabled but onWall={onWall}. wallStickAllowed={wallStickAllowed}");
        }
        
        // Comprehensive debug when ability is disabled
        if (PlayerAbilities.Instance != null && !PlayerAbilities.Instance.HasWallStick && !isGrounded)
        {
            // Debug.Log($"[WallStick] DISABLED STATE: onWall={onWall}, isWallSticking={isWallSticking}, isWallSliding={isWallSliding}, velocity.y={rb.linearVelocity.y:F2}, wallStickAllowed={wallStickAllowed}");
        }
        
        // Track wall stick history for sequential logic
        if (!wasWallSticking && isWallSticking)
        {
            canHeadStomp = true; // Reset head stomp when starting to wall stick
            
            // Clear coyote time when starting to wall stick
            if (enableCoyoteTime)
            {
                coyoteTimeCounter = 0f;
                leftGroundByJumping = false;
            }
        }
        
        if (isWallSticking)
        {
            hasEverWallStuck = true;
        }
        
        // Wall slide can ONLY trigger if player has wall stuck during this wall contact session
        bool canWallSlide = onWall && rb.linearVelocity.y < -wallSlideSpeed && !isDashing && !IsDashAttacking && !IsAirAttacking &&
                           PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick;
        bool allowWallSlide = canWallSlide && hasEverWallStuck;
        
        // Only allow wall slide if player has been wall sticking first
        isWallSliding = allowWallSlide;
        
        // Reset wall stick history when no longer on wall
        if (!onWall)
        {
            hasEverWallStuck = false;
        }
        
        // Update wall sticking state - can't be sticking while sliding
        if (isWallSliding)
        {
            isWallSticking = false;
        }
        
        // Then calculate running state (which depends on wall states)
        isRunning = Mathf.Abs(moveInput.x) > 0.1f && !isDashing && !IsDashAttacking && !IsAirAttacking && !onWall && !isWallSticking;
        
        isJumping = !isGrounded && !isWallSliding && !isWallSticking && !isClimbing && !isLedgeGrabbing && !isDashing && !IsDashAttacking && !IsAirAttacking && rb.linearVelocity.y > 0;
        
        isFalling = !isGrounded && !isWallSliding && !isWallSticking && !isClimbing && !isLedgeGrabbing && !IsDashAttacking && !IsAirAttacking && rb.linearVelocity.y < 0;
        isDashingAnim = isDashing;

        horizontalInput = moveInput.x;
        verticalInput = moveInput.y;
        facingDirection = facingRight ? 1f : -1f;
        
        // Store previous wall sticking state for next frame
        wasWallSticking = isWallSticking;
    }
    
    private void HandleMovement()
    {
        // Buffer climbing assistance - provide upward and forward momentum
        if (isBufferClimbing)
        {
            float horizontalVelocity = moveInput.x * runSpeed * forwardBoost;
            
            // CRITICAL FIX: Prevent horizontal movement when wall stick is disabled (even during ledge buffer)
            if (PlayerAbilities.Instance != null && !PlayerAbilities.Instance.HasWallStick)
            {
                // Check if we're pushing against a wall when ability is disabled
                Collider2D playerCollider = GetComponent<Collider2D>();
                int groundLayer = LayerMask.NameToLayer("Ground");
                int groundMask = 1 << groundLayer;
                
                Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
                Vector2 centerPoint = transform.position;
                
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
        
        // Horizontal run (skip during air attack, dashing, and dash jump momentum preservation)
        bool isDashJumpMomentumActive = dashJumpTime > 0 && Time.time - dashJumpTime <= dashJumpMomentumDuration;
        if (isDashJumpMomentumActive && moveInput.x == 0)
        {
            // Debug log when momentum preservation is preventing movement override
            // Debug.Log($"[DashJump] Momentum preservation active - keeping horizontal velocity: {rb.linearVelocity.x:F2}");
        }
        
        if (!IsAirAttacking && !isDashing && !isDashJumpMomentumActive)
        {
            float horizontalVelocity = moveInput.x * runSpeed;
            
            // Wall movement prevention logic
            bool hasWallStickAbility = PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick;
            bool shouldCheckWallMovement = !isGrounded || isBufferClimbing;
            
            if (shouldCheckWallMovement)
            {
                // Perform wall detection using our 3 raycasts
                Collider2D playerCollider = GetComponent<Collider2D>();
                int groundLayer = LayerMask.NameToLayer("Ground");
                int groundMask = 1 << groundLayer;
                
                Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
                Vector2[] checkPoints = {
                    transform.position + Vector3.up * wallRaycastTop,    // Top (0.32)
                    transform.position + Vector3.up * wallRaycastMiddle, // Middle (0.28)
                    transform.position + Vector3.up * wallRaycastBottom  // Bottom (0.02)
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
            
            rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
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
        if (IsAttacking && isGrounded && !IsDashAttacking && !IsAirAttacking)
        {
            float attackSpeedMultiplier = combat?.attackMovementSpeed ?? 0.3f;
            float attackHorizontalVelocity = moveInput.x * runSpeed * attackSpeedMultiplier;
            
            // Combat system respects wall movement rules
            bool hasWallStickAbility = PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick;
            if (!hasWallStickAbility)
            {
                // Use simplified wall detection
                Collider2D playerCollider = GetComponent<Collider2D>();
                int groundLayer = LayerMask.NameToLayer("Ground");
                int groundMask = 1 << groundLayer;
                
                Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
                Vector2[] checkPoints = {
                    transform.position + Vector3.up * wallRaycastTop,
                    transform.position + Vector3.up * wallRaycastMiddle,
                    transform.position + Vector3.up * wallRaycastBottom
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
        if (combatMovement != Vector2.zero && (IsDashAttacking || IsAirAttacking))
        {
            float combatHorizontalVelocity = combatMovement.x;
            
            // CRITICAL FIX: Combat movement must also respect wall stick prevention  
            if (PlayerAbilities.Instance != null && !PlayerAbilities.Instance.HasWallStick)
            {
                // Check if combat movement would push against wall when ability is disabled
                bool movingIntoWall = (facingRight && combatHorizontalVelocity > 0.1f) || (!facingRight && combatHorizontalVelocity < -0.1f);
                
                if (movingIntoWall)
                {
                    Collider2D playerCollider = GetComponent<Collider2D>();
                    int groundLayer = LayerMask.NameToLayer("Ground");
                    int groundMask = 1 << groundLayer;
                    
                    Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
                    Vector2 centerPoint = transform.position;
                    
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
    }
    
    private void HandleJumping()
    {
        if (!jumpQueued) return;
        
        // CRITICAL: Block jumping during active air attacks to prevent infinite elevation exploit
        // Allow normal jumps when airborne after dashing off platform
        if (combat != null && (combat.IsAirAttacking || (combat.IsDashAttacking && isGrounded)))
        {
            jumpQueued = false;
            return;
        }
        
        // Dash Jump: Jump during active dash or shortly after dash ends
        if (CanPerformDashJump())
        {
            PerformDashJump();
            jumpQueued = false;
            return;
        }
        
        // Ground jump (including coyote time with dash window separation)
        bool inDashJumpWindow = lastDashEndTime > 0 && Time.time - lastDashEndTime <= dashJumpWindow;
        bool allowCoyoteTime = enableCoyoteTime && (!inDashJumpWindow || coyoteTimeDuringDashWindow);
        bool canGroundJump = isGrounded || (allowCoyoteTime && coyoteTimeCounter > 0f && !leftGroundByJumping);
        if (canGroundJump)
        {
            Jump(jumpForce);
            jumpsRemaining = extraJumps;
            airDashesRemaining = maxAirDashes;
            airDashesUsed = 0;
            dashesRemaining = maxDashes;
            if (animator != null) SafeSetTrigger("Jump");
            
            // Consume coyote time if it was used
            if (!isGrounded && enableCoyoteTime && coyoteTimeCounter > 0f)
            {
                coyoteTimeCounter = 0f;
                leftGroundByJumping = true; // Prevent further coyote jumps
            }
        }
        else if (onWall && PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick)
        {
            facingRight = !facingRight;
            Jump(wallJump.y, wallJump.x * (facingRight ? 1 : -1));
            jumpsRemaining = extraJumps;
            airDashesRemaining = maxAirDashes;
            airDashesUsed = 0;
            dashesRemaining = maxDashes;
            if (animator != null) SafeSetTrigger("Jump");
        }
        else if (onWall && PlayerAbilities.Instance != null && !PlayerAbilities.Instance.HasWallStick)
        {
            // DEBUG: Player tried to wall jump but ability is disabled
            // Debug.LogError($"[WallStick] WALL JUMP BLOCKED! Ability disabled but onWall={onWall}. This suggests onWall is incorrectly true!");
        }
        else if (jumpsRemaining > 0 && PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasDoubleJump)
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
        // Check if dash ability is unlocked
        if (PlayerAbilities.Instance == null || !PlayerAbilities.Instance.HasDash)
        {
            dashQueued = false;
            return;
        }
        
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
        if (moveInput.x != 0) facingRight = moveInput.x > 0;
        transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
    }
    
    private void UpdateAnimatorParameters()
    {
        if (animator == null) return;

        SafeSetBool("IsGrounded", isGrounded);
        SafeSetBool("IsRunning", isRunning);
        SafeSetBool("IsJumping", isJumping);
        SafeSetBool("IsDashing", isDashing);
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
        // Use compensation method if the separate file exists, otherwise fallback to original
        if (enableJumpCompensation)
        {
            JumpWithCompensation(yForce, xForce);
        }
        else
        {
            // For horizontal force (like dash jump), apply as impulse for proper momentum
            if (xForce != 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Keep horizontal velocity, clear vertical only
                rb.AddForce(new Vector2(xForce, yForce), ForceMode2D.Impulse);
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(new Vector2(0, yForce), ForceMode2D.Impulse);
            }
            lastJumpTime = Time.time; // Track jump time for wall stick timing
        }
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
    
    public void ConsumeHeadStomp()
    {
        canHeadStomp = false;
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
            // Debug.Log("Attack input received - using basic attack (add PlayerCombat for full combat system)");
            StartCoroutine(BasicAttack());
        }
    }

    // Debug info - DISABLED for clean play mode
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        // Hide debug panel for clean play mode - only show abilities and health
        return;
        
        GUILayout.BeginArea(new Rect(10, 10, 350, 600));
        GUILayout.Label("=== WALL LAND DEBUG ===", GUI.skin.label);
        
        // Ability status
        bool hasWallStickAbility = PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick;
        GUI.contentColor = hasWallStickAbility ? Color.green : Color.red;
        GUILayout.Label($"Wall Stick Ability: {(hasWallStickAbility ? "ENABLED" : "DISABLED")}");
        GUI.contentColor = Color.white;
        
        // Key states only
        GUILayout.Label($"Grounded: {isGrounded}");
        GUILayout.Label($"Moving: {isRunning}");
        GUI.contentColor = isWallSticking ? Color.green : Color.red;
        GUILayout.Label($"Wall Sticking: {isWallSticking}");
        GUI.contentColor = isWallSliding ? Color.green : Color.red;
        GUILayout.Label($"Wall Sliding: {isWallSliding}");
        GUI.contentColor = onWall ? Color.green : Color.red;
        GUILayout.Label($"On Wall: {onWall}");
        GUI.contentColor = hasEverWallStuck ? Color.green : Color.red;
        GUILayout.Label($"Has Ever Wall Stuck: {hasEverWallStuck}");
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
        
        // Sequential wall logic status
        GUILayout.Label("\n--- SEQUENTIAL WALL LOGIC ---");
        bool canWallSlide = onWall && rb.linearVelocity.y < -wallSlideSpeed;
        GUI.contentColor = canWallSlide ? Color.green : Color.red;
        GUILayout.Label($"Can Wall Slide (Physics): {canWallSlide}");
        GUI.contentColor = hasEverWallStuck ? Color.green : Color.red;
        GUILayout.Label($"Allows Wall Slide (Logic): {hasEverWallStuck}");
        GUI.contentColor = Color.white;
        GUILayout.Label($"Velocity Y: {rb.linearVelocity.y:F2}");
        GUILayout.Label($"Wall Slide Speed: -{wallSlideSpeed}");
        
        // Animation triggers
        GUILayout.Label("\n--- ANIMATION TRIGGERS ---");
        bool pressingTowardWallAnim = (facingRight && horizontalInput > 0.1f) || (!facingRight && horizontalInput < -0.1f);
        GUILayout.Label($"IsWallSticking: {isWallSticking}");
        GUILayout.Label($"PressingTowardWall: {pressingTowardWallAnim}");
        
        // Coyote time debug (only show when enabled)
        if (showCoyoteTimeDebug && enableCoyoteTime)
        {
            GUILayout.Label("\n--- COYOTE TIME ---");
            GUI.contentColor = coyoteTimeCounter > 0f ? Color.yellow : Color.gray;
            GUILayout.Label($"Coyote Counter: {coyoteTimeCounter:F3}s");
            GUI.contentColor = leftGroundByJumping ? Color.red : Color.green;
            GUILayout.Label($"Left By Jumping: {leftGroundByJumping}");
            
            // Show dash window separation info
            bool inDashWindow = lastDashEndTime > 0 && Time.time - lastDashEndTime <= dashJumpWindow;
            GUI.contentColor = inDashWindow ? Color.red : Color.gray;
            GUILayout.Label($"In Dash Window: {inDashWindow}");
            GUI.contentColor = coyoteTimeDuringDashWindow ? Color.green : Color.red;
            GUILayout.Label($"Coyote During Dash: {coyoteTimeDuringDashWindow}");
            
            GUI.contentColor = Color.white;
            bool allowCoyoteTime = enableCoyoteTime && (!inDashWindow || coyoteTimeDuringDashWindow);
            bool coyoteActive = allowCoyoteTime && coyoteTimeCounter > 0f && !leftGroundByJumping && !isGrounded;
            GUI.contentColor = coyoteActive ? Color.green : Color.gray;
            GUILayout.Label($"Coyote Jump Available: {coyoteActive}");
            GUI.contentColor = Color.white;
        }
        
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
    
    /// <summary>
    /// Enhanced jump method with wall friction compensation
    /// </summary>
    private void JumpWithCompensation(float yForce, float xForce = 0)
    {
        // Check if we need compensation
        bool needsCompensation = CheckIfNeedsJumpCompensation();
        
        if (needsCompensation)
        {
            // Debug.Log($"[JumpFix] Wall friction detected - applying {wallJumpCompensation}x compensation");
            
            // Apply force compensation
            float compensatedForce = yForce * wallJumpCompensation;
            // For horizontal force (like dash jump), apply as impulse for proper momentum
            if (xForce != 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Keep horizontal velocity, clear vertical only
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
            // Normal jump without compensation
            // For horizontal force (like dash jump), apply as impulse for proper momentum
            if (xForce != 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Keep horizontal velocity, clear vertical only
                rb.AddForce(new Vector2(xForce, yForce), ForceMode2D.Impulse);
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(new Vector2(0, yForce), ForceMode2D.Impulse);
            }
        }
        
        lastJumpTime = Time.time;
    }
    
    /// <summary>
    /// Check if jump needs friction compensation
    /// </summary>
    private bool CheckIfNeedsJumpCompensation()
    {
        // Only compensate if wall stick is disabled
        if (PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick)
            return false;
        
        // Check if we're against a wall
        if (!CheckIfAgainstWall())
            return false;
        
        // Check if player is pressing into the wall
        bool pressingIntoWall = (facingRight && moveInput.x > 0.1f) || 
                                (!facingRight && moveInput.x < -0.1f);
        
        return pressingIntoWall;
    }
    
    /// <summary>
    /// Check if player is physically against a wall
    /// </summary>
    private bool CheckIfAgainstWall()
    {
        Collider2D playerCollider = GetComponent<Collider2D>();
        int groundLayer = LayerMask.NameToLayer("Ground");
        int groundMask = 1 << groundLayer;
        
        Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
        
        // Use the same 3 raycasts as wall detection
        Vector2[] checkPoints = {
            transform.position + Vector3.up * wallRaycastTop,
            transform.position + Vector3.up * wallRaycastMiddle,
            transform.position + Vector3.up * wallRaycastBottom
        };
        
        foreach (Vector2 point in checkPoints)
        {
            // Use shorter distance to check for actual contact
            RaycastHit2D hit = Physics2D.Raycast(point, wallDirection, wallCheckDistance * 0.7f, groundMask);
            
            if (hit.collider != null && hit.collider != playerCollider)
            {
                // Check if it's a vertical wall
                if (Mathf.Abs(hit.normal.x) > 0.9f)
                {
                    return true; // We're against a wall
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if dash should end early due to wall collision
    /// </summary>
    private bool CheckDashWallCollision()
    {
        if (!isDashing) return false;
        
        // Only check for wall collision when wall stick is disabled
        // When wall stick is enabled, dashing into walls is expected behavior
        if (PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasWallStick)
            return false;
            
        // Use same wall detection as existing system
        Vector2 wallDirection = facingRight ? Vector2.right : Vector2.left;
        int groundLayer = LayerMask.NameToLayer("Ground");
        int groundMask = 1 << groundLayer;
        
        // Check if we're hitting a wall during dash
        Vector2[] checkPoints = {
            transform.position + Vector3.up * wallRaycastTop,
            transform.position + Vector3.up * wallRaycastMiddle,
            transform.position + Vector3.up * wallRaycastBottom
        };
        
        foreach (Vector2 point in checkPoints)
        {
            RaycastHit2D hit = Physics2D.Raycast(point, wallDirection, wallCheckDistance * 0.8f, groundMask);
            
            if (hit.collider != null)
            {
                // Check if it's a vertical wall (not a slope)
                if (Mathf.Abs(hit.normal.x) > 0.9f)
                {
                    // Debug.Log($"[DashFix] Dash collision detected - ending dash early");
                    return true; // End dash early
                }
            }
        }
        
        return false; // No wall collision, continue dash
    }
    
    /// <summary>
    /// Check if player can perform dash jump
    /// </summary>
    private bool CanPerformDashJump()
    {
        // Must have dash jump ability
        if (PlayerAbilities.Instance == null || !PlayerAbilities.Instance.GetAbility("dashjump"))
            return false;
        
        // GROUND ONLY: Disable dash jump completely in air
        if (!isGrounded)
            return false;
        
        // Can dash jump during active dash
        if (isDashing)
            return true;
        
        // Can dash jump shortly after dash ends (grace period)
        if (lastDashEndTime > 0 && Time.time - lastDashEndTime <= dashJumpWindow)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Execute dash jump with horizontal momentum
    /// </summary>
    private void PerformDashJump()
    {
        // Use dashJump.x for proper horizontal impulse force
        float horizontalForce = facingRight ? dashJump.x : -dashJump.x;
        
        // Store current velocity before ending dash
        Vector2 currentVelocity = rb.linearVelocity;
        // Debug.Log($"[DashJump] Current velocity before dash jump: {currentVelocity}");
        
        // End dash if currently dashing (prevents physics conflicts)
        if (isDashing)
        {
            isDashing = false;
            dashTimer = dashTime; // Force dash timer to expire
            lastDashEndTime = Time.time;
            combat?.OnDashEnd();
            // Debug.Log("[DashJump] Ended dash to perform dash jump");
        }
        
        // Apply dash jump manually without using Jump() method to avoid velocity clearing
        rb.linearVelocity = new Vector2(currentVelocity.x, 0); // Keep dash momentum, clear vertical
        rb.AddForce(new Vector2(horizontalForce, dashJump.y), ForceMode2D.Impulse);
        
        // Start momentum preservation period
        dashJumpTime = Time.time;
        
        // Debug.Log($"[DashJump] Applied force: H={horizontalForce}, V={dashJump.y}, Final velocity: {rb.linearVelocity}");
        
        // Ground dash jump: reset air abilities like normal jump
        // (Air dash jump is disabled, so this only applies to ground)
        jumpsRemaining = extraJumps;
        airDashesRemaining = maxAirDashes;
        airDashesUsed = 0;
        
        // Clear dash end time to prevent repeated dash jumps
        lastDashEndTime = 0f;
        
        // Trigger jump animation (dash jumps use regular jump animation)
        if (animator != null) 
        {
            SafeSetTrigger("Jump");
        }
        
        // Debug.Log($"[DashJump] Performed GROUND dash jump - Horizontal: {horizontalForce:F1}, Vertical: {dashJump.y:F1}, Result velocity: {rb.linearVelocity}");
    }
    
    // Debug visualization for wall detection
    void OnDrawGizmos()
    {
        // Remove the isPlaying check so gizmos show in edit mode too
        // if (!Application.isPlaying) return;
        
        Vector3 playerPos = transform.position;
        Vector3 direction = facingRight ? Vector3.right : Vector3.left;
        
        // Main wall detection rays - 3 rays (using inspector parameters)
        if (!isGrounded)
        {
            Gizmos.color = onWall ? Color.red : Color.yellow;
            // Draw the 3 wall check rays
            Gizmos.DrawRay(playerPos + Vector3.up * wallRaycastTop, direction * wallCheckDistance);    // Top (0.32)
            Gizmos.DrawRay(playerPos + Vector3.up * wallRaycastMiddle, direction * wallCheckDistance); // Middle (0.28)
            Gizmos.DrawRay(playerPos + Vector3.up * wallRaycastBottom, direction * wallCheckDistance); // Bottom (0.02)
        }
        else
        {
            Gizmos.color = Color.gray;
            // Show disabled wall detection when grounded
            Gizmos.DrawRay(playerPos + Vector3.up * wallRaycastTop, direction * wallCheckDistance);
            Gizmos.DrawRay(playerPos + Vector3.up * wallRaycastMiddle, direction * wallCheckDistance);
            Gizmos.DrawRay(playerPos + Vector3.up * wallRaycastBottom, direction * wallCheckDistance);
        }
        
        // Draw small spheres at raycast origins for clarity
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerPos + Vector3.up * wallRaycastTop, 0.02f);    // Top
        Gizmos.DrawWireSphere(playerPos + Vector3.up * wallRaycastMiddle, 0.02f); // Middle
        Gizmos.DrawWireSphere(playerPos + Vector3.up * wallRaycastBottom, 0.02f); // Bottom
        
        // Ground detection
        Gizmos.color = isGrounded ? Color.green : Color.blue;
        Gizmos.DrawWireSphere(playerPos + Vector3.down * 0.6f, 0.1f);
        
        // Precise ground check visualization
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            float feetY = col.bounds.min.y;
            Vector2 feetPos = new Vector2(transform.position.x, feetY + groundCheckOffsetY);
            
            // Smaller, less prominent ground check visualization  
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // Transparent magenta
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
                
                // More subtle ground detection visualization
                Gizmos.color = groundedByPlatform ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
                Gizmos.DrawWireSphere(feetPos, groundCheckRadius + 0.02f);
                
                Gizmos.color = groundedByBuffer ? new Color(1f, 1f, 0f, 0.4f) : new Color(0f, 1f, 1f, 0.4f);
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
        
        // Draw death zone in scene view
        if (showDeathZone)
        {
            Gizmos.color = Color.red;
            
            // Use adjustable width for death zone visualization
            float halfWidth = deathZoneWidth / 2f;
            float leftBound = -halfWidth;
            float rightBound = halfWidth;
            
            // Center on player's initial X position for better visibility
            if (initialPosition != Vector3.zero)
            {
                leftBound = initialPosition.x - halfWidth;
                rightBound = initialPosition.x + halfWidth;
            }
            
            // Draw death zone line
            Vector3 leftPoint = new Vector3(leftBound, deathZoneY, 0);
            Vector3 rightPoint = new Vector3(rightBound, deathZoneY, 0);
            Gizmos.DrawLine(leftPoint, rightPoint);
            
            // Draw danger zone (area below death line)
            Gizmos.color = new Color(1f, 0f, 0f, 0.1f); // Semi-transparent red
            Vector3[] dangerZone = new Vector3[4];
            dangerZone[0] = leftPoint;
            dangerZone[1] = rightPoint;
            dangerZone[2] = new Vector3(rightBound, deathZoneY - 10f, 0);
            dangerZone[3] = new Vector3(leftBound, deathZoneY - 10f, 0);
            
            // Draw filled danger zone
            Gizmos.DrawLine(dangerZone[0], dangerZone[1]);
            Gizmos.DrawLine(dangerZone[1], dangerZone[2]);
            Gizmos.DrawLine(dangerZone[2], dangerZone[3]);
            Gizmos.DrawLine(dangerZone[3], dangerZone[0]);
            
            // Add warning text indicators
            Gizmos.color = Color.red;
            for (float x = leftBound; x <= rightBound; x += 10f)
            {
                // Draw small downward arrows to indicate danger
                Vector3 arrowTop = new Vector3(x, deathZoneY, 0);
                Vector3 arrowBottom = new Vector3(x, deathZoneY - 0.5f, 0);
                Vector3 arrowLeft = new Vector3(x - 0.2f, deathZoneY - 0.3f, 0);
                Vector3 arrowRight = new Vector3(x + 0.2f, deathZoneY - 0.3f, 0);
                
                Gizmos.DrawLine(arrowTop, arrowBottom);
                Gizmos.DrawLine(arrowBottom, arrowLeft);
                Gizmos.DrawLine(arrowBottom, arrowRight);
            }
        }
    }
    
    // Death/Reset system for testing
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Camera Bounder"))
        {
            // Debug.Log("[Death/Reset] Camera Bounder detected! Resetting...");
            ResetToInitialPosition();
        }
    }
    
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Camera Bounder"))
        {
            // Debug.Log("[Death/Reset] Camera Bounder detected! Resetting...");
            ResetToInitialPosition();
        }
    }
    
    // Manual reset for testing - add this as a public method you can call from inspector
    [ContextMenu("Test Reset")]
    public void TestReset()
    {
        // Debug.Log("[Death/Reset] Manual reset triggered");
        ResetToInitialPosition();
    }
    
    public void ResetToInitialPosition()
    {
        // Debug.Log($"[Death/Reset] BEFORE RESET - Current: {transform.position}, Initial: {initialPosition}");
        
        // Reset physics FIRST to prevent interference
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        // Force position through both transform and rigidbody
        transform.position = initialPosition;
        rb.position = initialPosition;
        
        // Force physics update
        Physics2D.SyncTransforms();
        
        // Double-check position was set
        // Debug.Log($"[Death/Reset] AFTER RESET - Position is now: {transform.position}, RB position: {rb.position}");
        
        // Reset movement states
        isDashing = false;
        dashTimer = 0f;
        dashJumpTime = 0f;
        lastDashEndTime = 0f;
        
        // Reset coyote time
        coyoteTimeCounter = 0f;
        leftGroundByJumping = false;
        
        // Reset abilities
        jumpsRemaining = extraJumps;
        dashesRemaining = maxDashes;
        airDashesRemaining = maxAirDashes;
        airDashesUsed = 0;
        
        // Reset combat system
        if (combat != null)
        {
            combat.ResetAttackSystem();
        }
        
        // Reset camera position by forcing Cinemachine to snap
        StartCoroutine(ResetCameraPosition());
        
        // Debug.Log("[Death/Reset] Player reset to initial position");
    }
    
    private System.Collections.IEnumerator ResetCameraPosition()
    {
        // Wait one frame for position to be applied
        yield return null;
        
        // Try to find and reset Cinemachine camera using reflection to avoid compile errors
        var cinemachineType = System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine");
        if (cinemachineType != null)
        {
            var vcam = FindFirstObjectByType(cinemachineType);
            if (vcam != null)
            {
                // Use reflection to call OnTargetObjectWarped
                var method = cinemachineType.GetMethod("OnTargetObjectWarped");
                if (method != null)
                {
                    method.Invoke(vcam, new object[] { transform, transform.position - initialPosition });
                    // Debug.Log("[Death/Reset] Camera position reset via Cinemachine");
                    yield break;
                }
            }
        }
        
        // Fallback: try to find regular camera and snap it directly
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Simple camera snap to player position
            mainCamera.transform.position = new Vector3(initialPosition.x, initialPosition.y, mainCamera.transform.position.z);
            // Debug.Log("[Death/Reset] Camera position reset directly");
        }
    }
}