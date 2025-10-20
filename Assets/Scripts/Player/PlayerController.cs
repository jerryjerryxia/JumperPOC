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
    public float runSpeed = 4f;
    public int extraJumps = 1;
    public float wallSlideSpeed = 2f;
    public Vector2 wallJump = new(7f, 10f);
    
    [Header("Variable Jump (Hollow Knight Style)")]
    [SerializeField] private bool enableVariableJump = true;
    [SerializeField] private float minJumpVelocity = 4f; // Velocity for tap jump (lower = shorter)
    [SerializeField] private float maxJumpVelocity = 4f; // Velocity for full hold jump (higher = taller)
    [SerializeField] private float jumpHoldDuration = 0.3f; // Max time to hold for variable height
    [SerializeField] private float jumpGravityReduction = 0f; // Gravity multiplier while holding (lower = floatier)
    
    [Header("Double Jump Settings")]
    [SerializeField] private float minDoubleJumpVelocity = 4f; // Velocity for tap double jump
    [SerializeField] private float maxDoubleJumpVelocity = 4f; // Velocity for full hold double jump
    [SerializeField] private float doubleJumpMinDelay = 0.2f; // Minimum time after first jump before double jump is available
    [SerializeField] private float forcedFallDuration = 0.1f; // How long to force fall before double jump when ascending
    [SerializeField] private float forcedFallVelocity = -2f; // Velocity during forced fall phase
    [SerializeField] private bool useVelocityClamping = true; // Use velocity clamping method
    [SerializeField] private bool showJumpDebug = false; // Debug visualization

    [Header("Dash")]
    public float dashSpeed = 8f;
    public float dashTime = 0.25f;
    public float dashCooldown = 0.4f;
    public int maxDashes = 2;
    public int maxAirDashes = 2;
    
    [Header("Dash Jump")]
    [SerializeField] public Vector2 dashJump = new(5f, 11f); // (horizontal, vertical) force
    [SerializeField] public float dashJumpWindow = 0.1f; // Grace period after dash ends
    
    [Header("Death Zone")]
    public float deathZoneY = -20f; // Y position that triggers reset
    public float deathZoneWidth = 100f; // Width of death zone visualization
    public bool showDeathZone = true; // Show death zone in scene view

    [Header("Buffer Climbing")]
    [SerializeField] private float climbingAssistanceOffset = 0.06f; // How far below platform edge to trigger assistance
    [SerializeField] private float climbForce = 3f;
    [SerializeField] private float forwardBoost = 0f;
    
    [Header("Coyote Time")]
    [SerializeField] private bool enableCoyoteTime = true; // Enable coyote time feature
    [SerializeField] private float coyoteTimeDuration = 0.12f; // Grace period after leaving ground
    [SerializeField] private bool coyoteTimeDuringDashWindow = false; // Allow coyote time during dash jump window
    public bool showClimbingGizmos = true;
    
    [Header("Jump Compensation")]
    [SerializeField] private float wallJumpCompensation = 1.2f; // Multiplier to counteract friction
    [SerializeField] private bool enableJumpCompensation = true;
    
    [Header("Slope Movement")]
    [SerializeField] private float maxSlopeAngle = 60f; // Maximum walkable slope angle
    
    [Header("Slope Raycast Parameters")]
    [SerializeField] private bool enableSlopeVisualization = true; // Show raycast debug lines
    [SerializeField] private float slopeRaycastDistance = 0.2f; // Distance for slope detection raycasts
    [SerializeField] private Vector2 raycastDirection1 = Vector2.down; // Direction 1: Straight down
    [SerializeField] private Vector2 raycastDirection2 = new Vector2(0.707f, -0.707f); // Direction 2: Down-right 45°
    [SerializeField] private Vector2 raycastDirection3 = new Vector2(-0.707f, -0.707f); // Direction 3: Down-left 45°
    [SerializeField] private float debugLineDuration = 0.1f; // How long debug lines stay visible
    
    [Header("Animation")]
    public float animationTransitionSpeed = 0.1f;
    
    
    [Header("Ground Check")]
    public float groundCheckOffsetY = -0.02f;
    public float groundCheckRadius = 0.03f;
    
    [Header("Wall Detection")]
    public float wallCheckDistance = 0.15f;
    
    [Header("Wall Detection Raycasts")]
    public float wallRaycastTop = 0.32f;    // Top raycast position
    public float wallRaycastMiddle = 0.28f; // Middle raycast position
    public float wallRaycastBottom = 0.02f; // Bottom raycast position
    
    // Component references
    private Rigidbody2D rb;
    private Animator animator;
    private PlayerCombat combat;
    private InputManager inputManager;

    // Refactored component system
    private PlayerGroundDetection groundDetection;
    private PlayerWallDetection wallDetection;
    private PlayerMovement movement;
    private PlayerJumpSystem jumpSystem;
    private PlayerAnimationController animationController;
    private PlayerRespawnSystem respawnSystem;
    private PlayerStateTracker stateTracker;
    private PlayerDebugVisualizer debugVisualizer;

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
    
    // Variable jump state (synced from jumpSystem)
    private bool isJumpHeld = false;
    private bool isVariableJumpActive = false;
    private float lastLandTime = 0f;
    private float lastJumpTime = 0f;

    // Dash jump momentum preservation
    private float dashJumpTime = 0f;

    // Wall state sequence tracking
    private bool wasWallSticking = false;
    private bool hasEverWallStuck = false;
    
    // Coyote time tracking
    private float coyoteTimeCounter = 0f;
    private bool leftGroundByJumping = false;
    
    // Head stomp permission system (always enabled - see CanHeadStomp property)
    
    // Animation state tracking
    private bool isGrounded;
    private bool isRunning;
    
    // Ground detection state (synced from PlayerGroundDetection)
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
    private bool isGroundedByPlatform;
    private bool isGroundedByBuffer;
    
    // Basic attack state for compatibility (when no PlayerCombat)
    private bool isAttacking = false;
    
    // Jump compensation state (removed - was unused)
    // private bool isCompensatingJump = false;
    
    // Slope detection state
    private bool isOnSlope = false;
    private Vector2 slopeNormal = Vector2.up;
    private float currentSlopeAngle = 0f;
    
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
    public bool CanHeadStomp => true; // DISABLED: canHeadStomp; // Disabled requirement to land before head stomping again
    public Vector2 MoveInput => moveInput;
    public bool FacingRight => facingRight;
    public bool OnWall => onWall;
    public float RunSpeed => runSpeed;
    public float DashTimer => dashTimer;
    public float DashTime => dashTime;
    public int AirDashesRemaining => airDashesRemaining;
    public bool IsOnSlope => isOnSlope;
    public float CurrentSlopeAngle => currentSlopeAngle;
    
    // Variable jump properties
    public float JumpForce => maxJumpVelocity; // For compatibility with external systems
    public float MinJumpVelocity => minJumpVelocity;
    public float MaxJumpVelocity => maxJumpVelocity;
    public bool IsVariableJumpActive => isVariableJumpActive;
    public bool EnableVariableJump => enableVariableJump;

    private static PlayerController instance;
    
    void Awake()
    {
        // Singleton pattern for persistent player
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Destroy duplicate player if one already exists
            Destroy(gameObject);
            return;
        }
        
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        combat = GetComponent<PlayerCombat>();
        Physics2D.queriesStartInColliders = false;

        // Initialize refactored component system
        InitializeComponents();

        // Verify required components
        VerifyComponentSetup();
        
        // Initialize dash counts
        airDashesRemaining = maxAirDashes;
        dashesRemaining = maxDashes;
    }
    
    void Start()
    {
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

        // Initialize SimpleRespawnManager with player's actual starting position
        if (SimpleRespawnManager.Instance != null)
        {
            SimpleRespawnManager.Instance.InitializeWithPlayerStartPosition(transform.position);
        }
        else
        {
            Debug.LogWarning("[SavePoint System] SimpleRespawnManager not found! Save points will not work. Use Tools > Setup Save Point System to fix.");
        }
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
    
    /// <summary>
    /// Initialize all refactored components
    /// </summary>
    private void InitializeComponents()
    {
        // Get component references (components must be manually added to GameObject)
        groundDetection = GetComponent<PlayerGroundDetection>();
        wallDetection = GetComponent<PlayerWallDetection>();
        movement = GetComponent<PlayerMovement>();
        jumpSystem = GetComponent<PlayerJumpSystem>();
        animationController = GetComponent<PlayerAnimationController>();
        respawnSystem = GetComponent<PlayerRespawnSystem>();
        stateTracker = GetComponent<PlayerStateTracker>();
        debugVisualizer = GetComponent<PlayerDebugVisualizer>();

        // Initialize each component with required references
        Collider2D col = GetComponent<Collider2D>();
        PlayerAbilities abilities = GetComponent<PlayerAbilities>();

        groundDetection.Initialize(rb, col, transform);
        wallDetection.Initialize(rb, col, transform, abilities);
        movement.Initialize(rb, transform, animator, col, groundDetection, wallDetection, combat, jumpSystem);
        jumpSystem.Initialize(rb, transform, groundDetection, wallDetection, abilities, animator);
        animationController.Initialize(animator);
        respawnSystem.Initialize(transform, rb, combat);
        stateTracker.Initialize(rb, groundDetection, wallDetection, movement, jumpSystem, combat);
        debugVisualizer.Initialize(this, rb, inputManager, respawnSystem);

        // Set up respawn callbacks for state reset
        respawnSystem.SetResetCallbacks(
            // Dash state reset: dashTimer, dashJumpTime, lastDashEndTime
            (timer, jumpTime, endTime) =>
            {
                isDashing = false;
                dashTimer = timer;
                dashJumpTime = jumpTime;
                lastDashEndTime = endTime;
            },
            // Jump state reset: 3 unused float params, 1 bool for leftGroundByJumping
            (unused1, unused2, unused3, leftGround) =>
            {
                coyoteTimeCounter = 0f;
                leftGroundByJumping = leftGround;
                jumpsRemaining = extraJumps;
                dashesRemaining = maxDashes;
                airDashesRemaining = maxAirDashes;
                airDashesUsed = 0;
            },
            // Combat reset
            () =>
            {
                combat?.ResetAttackSystem();
            }
        );

        // Configure ground detection with all necessary values
        groundDetection.SetConfiguration(groundCheckOffsetY, groundCheckRadius, maxSlopeAngle,
                                        enableSlopeVisualization, slopeRaycastDistance,
                                        raycastDirection1, raycastDirection2, raycastDirection3,
                                        debugLineDuration, enableCoyoteTime, coyoteTimeDuration,
                                        climbingAssistanceOffset, maxAirDashes, maxDashes, combat);

        // Configure wall detection
        wallDetection.SetConfiguration(wallCheckDistance, wallRaycastTop, wallRaycastMiddle, wallRaycastBottom);

        // Configure movement system
        movement.SetConfiguration(runSpeed, wallSlideSpeed, climbingAssistanceOffset, climbForce, forwardBoost,
                                 dashSpeed, dashTime, dashCooldown, maxDashes, maxAirDashes, dashJumpWindow,
                                 wallCheckDistance, wallRaycastTop, wallRaycastMiddle, wallRaycastBottom);

        // Configure jump system
        jumpSystem.SetConfiguration(extraJumps, wallJump, enableVariableJump,
                                   minJumpVelocity, maxJumpVelocity, jumpHoldDuration,
                                   jumpGravityReduction, minDoubleJumpVelocity, maxDoubleJumpVelocity,
                                   doubleJumpMinDelay, forcedFallDuration, forcedFallVelocity,
                                   useVelocityClamping, showJumpDebug, dashJump, dashJumpWindow,
                                   wallJumpCompensation, enableJumpCompensation, wallCheckDistance,
                                   wallRaycastTop, wallRaycastMiddle, wallRaycastBottom,
                                   enableCoyoteTime, coyoteTimeDuringDashWindow, maxAirDashes, maxDashes,
                                   combat, inputManager);
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
            inputManager.OnJumpReleased += OnJumpReleased;  // Subscribe to release event
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
            inputManager.OnJumpReleased -= OnJumpReleased;  // Unsubscribe from release event
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
        
        // Update jump system state
        jumpSystem.UpdateExternalState(facingRight, moveInput, isGrounded, onWall,
                                       isOnSlope, currentSlopeAngle, slopeNormal,
                                       coyoteTimeCounter, leftGroundByJumping, isBufferClimbing,
                                       isDashing, lastDashEndTime);

        // Handle variable jump mechanics
        jumpSystem.UpdateVariableJump();

        // Handle forced fall for double jump
        jumpSystem.UpdateForcedFall();

        // Sync jump state from jumpSystem
        isVariableJumpActive = jumpSystem.IsVariableJumpActive;
        jumpsRemaining = jumpSystem.JumpsRemaining;
        lastJumpTime = jumpSystem.LastJumpTime;
        dashJumpTime = jumpSystem.DashJumpTime;
        isJumpHeld = jumpSystem.IsJumpHeld;
        
        // Removed complex horizontal movement tracking
        
        // Check for buffered combat actions
        combat?.CheckBufferedDashAttack();
        
        // Simple death zone check - if player falls below deathZoneY, reset
        if (respawnSystem.IsInDeathZone(deathZoneY))
        {
            // Debug.Log($"[Death/Reset] Player fell below death zone (Y < -20). Current Y: {transform.position.y}");
            respawnSystem.ResetToRespawnPoint();
            return; // Skip rest of frame after reset
        }
        
        // Ground and wall detection
        groundDetection.UpdateExternalState(moveInput, facingRight, lastJumpTime, dashJumpTime);

        bool wasGroundedBeforeCheck = isGrounded;
        groundDetection.CheckGrounding();

        // Sync ground state from detection component
        isGrounded = groundDetection.IsGrounded;
        isOnSlope = groundDetection.IsOnSlope;
        currentSlopeAngle = groundDetection.CurrentSlopeAngle;
        slopeNormal = groundDetection.SlopeNormal;
        isGroundedByPlatform = groundDetection.IsGroundedByPlatform;
        isGroundedByBuffer = groundDetection.IsGroundedByBuffer;
        isBufferClimbing = groundDetection.IsBufferClimbing;
        coyoteTimeCounter = groundDetection.CoyoteTimeCounter;
        leftGroundByJumping = groundDetection.LeftGroundByJumping;

        // Sync dash state from detection component (updated on landing)
        airDashesRemaining = groundDetection.AirDashesRemaining;
        airDashesUsed = groundDetection.AirDashesUsed;
        dashesRemaining = groundDetection.DashesRemaining;
        lastLandTime = groundDetection.LastLandTime;

        // Handle landing-specific logic that's not in groundDetection
        if (!wasGroundedBeforeCheck && isGrounded)
        {
            // Clear dash jump momentum preservation on landing
            dashJumpTime = 0f;
        }

        // Wall detection
        wallDetection.UpdateExternalState(facingRight, moveInput, isGrounded, isBufferClimbing);
        wallDetection.CheckWallDetection();

        // Sync wall state from detection component
        onWall = wallDetection.OnWall;
        wallStickAllowed = wallDetection.WallStickAllowed;

        // Update movement states
        UpdateMovementStates();

        // CRITICAL: Update sprite facing BEFORE passing state to movement component
        // This ensures dash uses the CURRENT facing direction, not the old one
        if (moveInput.x != 0) facingRight = moveInput.x > 0;
        transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);

        // Update movement component state (pass isJumping for slope physics)
        movement.UpdateExternalState(facingRight, moveInput, isGrounded, onWall, wallStickAllowed,
                                     isOnSlope, currentSlopeAngle, slopeNormal, isBufferClimbing,
                                     isWallSliding, isWallSticking, IsAttacking, IsDashAttacking,
                                     IsAirAttacking, jumpQueued, isJumping, dashJumpTime);

        // Handle movement through movement component
        movement.HandleMovement();
        
        // Sync current dash state TO movement component before dashing
        // This ensures movement has the latest air dash count
        movement.SetDashState(dashesRemaining, airDashesUsed);

        // Handle dashing through movement component
        movement.HandleDash(dashQueued);
        dashQueued = false;

        // Apply dash velocity through movement component
        movement.ApplyDashVelocity();

        // Update dash cooldown
        movement.UpdateDashCooldown();

        // Sync movement state from movement component BEFORE jumping
        // This ensures jump system sees if we just started dashing
        isDashing = movement.IsDashing;
        dashTimer = movement.DashTimer;
        dashJumpTime = movement.DashJumpTime;
        dashesRemaining = movement.DashesRemaining;
        airDashesUsed = movement.AirDashesUsed;
        dashCDTimer = movement.DashCDTimer;
        lastDashEndTime = movement.LastDashEndTime;

        // Handle jumping
        int airDashOut, airDashUsedOut, dashesOut;
        float coyoteOut;
        bool leftGroundOut;
        bool jumpExecuted = jumpSystem.HandleJumping(jumpQueued, out airDashOut, out airDashUsedOut, out dashesOut, out coyoteOut, out leftGroundOut);

        if (jumpExecuted)
        {
            // Sync state updated by jump system
            airDashesRemaining = airDashOut;
            airDashesUsed = airDashUsedOut;
            dashesRemaining = dashesOut;
            coyoteTimeCounter = coyoteOut;
            leftGroundByJumping = leftGroundOut;
            jumpsRemaining = jumpSystem.JumpsRemaining;
            facingRight = jumpSystem.FacingRight; // Sync facing direction (changes during wall jump)

            // CRITICAL: If this was a dash jump, end the dash to prevent velocity override
            if (jumpSystem.DashJumpTime == Time.time)
            {
                isDashing = false;
                lastDashEndTime = Time.time;
                combat?.OnDashEnd();

                // Update movement component that dash ended
                movement.EndDash();
            }
        }

        jumpQueued = false;

        // Animation updates (sprite facing already done above)
        animationController.UpdateAnimations(isGrounded, isRunning, isJumping, isDashing,
                                             IsAttacking, IsDashAttacking, IsAirAttacking,
                                             isClimbing, isWallSliding, isWallSticking, isFalling,
                                             onWall, facingDirection, horizontalInput, verticalInput,
                                             facingRight, combat?.AttackCombo ?? 0);
        
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
            // IMMEDIATELY and AGGRESSIVELY cancel ALL upward velocity when entering wall stick
            // This is critical for high-velocity dash jumps that would otherwise cause falling
            if (rb.linearVelocity.y > 0f)
            {
                // Store original velocity for debugging
                float originalVelocity = rb.linearVelocity.y;
                
                // ZERO out vertical velocity immediately - no gradual transition
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

                // Clear any dash jump momentum that might interfere
                if (dashJumpTime > 0f)
                {
                    dashJumpTime = 0f; // Stop dash jump momentum preservation
                }
                
                if (showJumpDebug)
                {
                    Debug.Log($"[Wall Stick] EMERGENCY STOP - cancelled velocity {originalVelocity:F2} �?0, dash momentum cleared");
                }
            }
            
            // Head stomp is always enabled (see CanHeadStomp property)
            
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
        // SLOPE FIX: Allow running on slopes even if wall detection is confused
        bool allowRunningOnSlope = isOnSlope && isGrounded && Mathf.Abs(moveInput.x) > 0.1f;
        bool wasRunning = isRunning;
        isRunning = Mathf.Abs(moveInput.x) > 0.1f && !isDashing && !IsDashAttacking && !IsAirAttacking && 
                   (!onWall || allowRunningOnSlope) && !isWallSticking;
        
        // Debug animation changes on slopes - ALWAYS LOG WHEN ON SLOPES
        if (isOnSlope && isGrounded)
        {
            // Debug.Log($"[SLOPE ANIMATION] On slope: isRunning={isRunning}, onWall={onWall}, moveInput={moveInput.x:F2}, allowRunningOnSlope={allowRunningOnSlope}");
        }
        
        isJumping = !isGrounded && !isWallSliding && !isWallSticking && !isClimbing && !isLedgeGrabbing && !isDashing && !IsDashAttacking && !IsAirAttacking && rb.linearVelocity.y > 0;
        
        isFalling = !isGrounded && !isWallSliding && !isWallSticking && !isClimbing && !isLedgeGrabbing && !IsDashAttacking && !IsAirAttacking && rb.linearVelocity.y < 0;
        isDashingAnim = isDashing;

        horizontalInput = moveInput.x;
        verticalInput = moveInput.y;
        facingDirection = facingRight ? 1f : -1f;
        
        // Store previous wall sticking state for next frame
        wasWallSticking = isWallSticking;
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
        // Head stomp is always enabled (see CanHeadStomp property)
        airDashesUsed = 0; // Reset air dash count on successful head stomp
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

    /* �?Input Event Handlers �?*/
    private void OnMoveInput(Vector2 input)
    {
        moveInput = input;
    }
    
    private void OnJumpInput()
    {
        jumpQueued = true;

        // Track jump hold state for variable jump
        if (enableVariableJump)
        {
            isJumpHeld = true;
            jumpSystem.IsJumpHeld = true; // Set jumpSystem's jump hold state
        }
    }

    private void OnJumpReleased()
    {
        // Clear jump hold state when button is released
        if (enableVariableJump)
        {
            isJumpHeld = false;
            jumpSystem.IsJumpHeld = false;
        }
    }

    private void OnDashInput()
    {
        if (Time.time - lastDashInputTime < 0.05f)
        {
            return;
        }

        // Prevent dash input when actively wall sticking or wall sliding
        // Allow dash when jumping from wall (onWall may still be true but not in wall state)
        if (onWall && (isWallSticking || isWallSliding))
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

    // Debug visualization moved to PlayerDebugVisualizer component
    // Toggle debug panels via PlayerDebugVisualizer inspector settings

    // Gizmos visualization moved to PlayerDebugVisualizer component

    // Death/Reset system - delegated to PlayerRespawnSystem
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Camera Bounder"))
        {
            // Debug.Log("[Death/Reset] Camera Bounder detected! Resetting...");
            respawnSystem.ResetToRespawnPoint();
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Camera Bounder"))
        {
            // Debug.Log("[Death/Reset] Camera Bounder detected! Resetting...");
            respawnSystem.ResetToRespawnPoint();
        }
    }

    // Public API methods - delegate to respawn system
    [ContextMenu("Test Reset")]
    public void TestReset() => respawnSystem.TestReset();

    public void SetRespawnPoint(Vector3 newRespawnPosition) => respawnSystem.SetRespawnPoint(newRespawnPosition);

    public void ResetToRespawnPoint() => respawnSystem.ResetToRespawnPoint();

    public void ResetToSavePoint(Vector3 savePosition) => respawnSystem.ResetToSavePoint(savePosition);

    public void ResetToInitialPosition() => respawnSystem.ResetToInitialPosition();
}
