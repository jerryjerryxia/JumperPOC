using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionDetector : MonoBehaviour
{
    [Header("Ledge Detection")]
    public float ledgeDetectionRange = 0.8f;
    public float ledgeGrabHeight = 0.5f;
    public LayerMask groundLayer = 1 << 6; // Layer 6 for ground
    public LayerMask ledgeLayer = 1 << 7;  // Layer 7 for ledges (you can set this up)
    
    [Header("Wall Detection")]
    public float wallDetectionRange = 0.6f;
    public float wallSlideThreshold = -2f;
    
    [Header("Visual Debug")]
    public bool showDebugGizmos = true;
    public Color ledgeGizmoColor = Color.green;
    public Color wallGizmoColor = Color.red;
    public Color groundGizmoColor = Color.blue;

    private PlayerController playerController;
    private Rigidbody2D rb;
    private bool facingRight;
    
    // Detection states
    private bool ledgeDetected;
    private bool wallDetected;
    private bool canClimbLedge;
    private Vector2 ledgePosition;
    private Vector2 ledgeNormal;
    
    // Input handling
    private Vector2 moveInput;
    private bool jumpInput;
    private bool climbInput;
    private Controls input;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        input = new Controls();
        
        // PlayerController may be disabled in new component system, that's OK
        if (playerController == null)
        {
            // Debug.LogWarning("PlayerInteractionDetector: PlayerController not found - some features may be limited");
        }
    }
    
    void OnEnable()
    {
        input?.Enable();
    }
    
    void OnDisable()
    {
        input?.Disable();
    }

    void Update()
    {
        // Get input from the player controller
        GetInputFromController();
        
        // Update facing direction
        facingRight = transform.localScale.x > 0;
        
        // Perform detections
        DetectLedge();
        DetectWall();
        
        // Handle ledge grab input
        HandleLedgeGrab();
        
        // Handle wall interactions
        HandleWallInteractions();
    }

    void GetInputFromController()
    {
        // Get input from PlayerController
        if (playerController != null)
        {
            moveInput = playerController.MoveInput;
            // Check for jump input using the same input instance
            jumpInput = input.Gameplay.Jump.WasPressedThisFrame();
            climbInput = moveInput.y > 0.5f; // Up input for climbing
        }
    }

    void DetectLedge()
    {
        Vector2 playerPos = transform.position;
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        
        // Check for ledge in front of player
        RaycastHit2D ledgeCheck = Physics2D.Raycast(
            playerPos + Vector2.up * 0.5f, 
            direction, 
            ledgeDetectionRange, 
            groundLayer
        );
        
        // Check if there's no ground above the ledge
        bool noGroundAbove = false;
        if (ledgeCheck.collider != null)
        {
            Vector2 ledgeTop = ledgeCheck.point + Vector2.up * ledgeGrabHeight;
            noGroundAbove = !Physics2D.OverlapCircle(ledgeTop, 0.1f, groundLayer);
        }
        
        // Check if player is at the right height to grab
        bool atLedgeHeight = Physics2D.OverlapCircle(playerPos + Vector2.up * 0.8f, 0.1f, groundLayer);
        
        ledgeDetected = ledgeCheck.collider != null && noGroundAbove && !atLedgeHeight;
        
        if (ledgeDetected)
        {
            ledgePosition = ledgeCheck.point;
            ledgeNormal = ledgeCheck.normal;
            canClimbLedge = true;
        }
        else
        {
            canClimbLedge = false;
        }
    }

    void DetectWall()
    {
        Vector2 playerPos = transform.position;
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        
        // Check for wall in front of player
        RaycastHit2D wallCheck = Physics2D.Raycast(
            playerPos, 
            direction, 
            wallDetectionRange, 
            groundLayer
        );
        
        wallDetected = wallCheck.collider != null;
        
        // Update player controller wall state
        if (playerController != null)
        {
            // The wall sliding logic is already in PlayerController
            // We just need to ensure the detection is working
        }
    }

    void HandleLedgeGrab()
    {
        if (ledgeDetected && canClimbLedge && jumpInput)
        {
            // Trigger ledge grab
            StartLedgeGrab();
        }
    }

    void HandleWallInteractions()
    {
        if (wallDetected && !playerController.IsGrounded)
        {
            // Check if player is sliding down the wall
            bool isWallSliding = rb.linearVelocity.y < wallSlideThreshold;
            
            if (isWallSliding)
            {
                // Player is wall sliding - this is handled in PlayerController
                // We just need to ensure the detection is working
            }
        }
    }

    void StartLedgeGrab()
    {
        if (playerController == null) return;
        
        // Set player state to ledge grabbing
        playerController.SetLedgeGrabbing(true);
        
        // Position player at ledge
        Vector2 targetPosition = ledgePosition + Vector2.up * 0.5f;
        if (!facingRight)
        {
            targetPosition.x -= 0.5f; // Adjust for left-facing
        }
        
        // Move player to ledge position
        transform.position = targetPosition;
        
        // Stop player movement
        rb.linearVelocity = Vector2.zero;
        
        // Start ledge grab sequence
        StartCoroutine(LedgeGrabSequence());
    }

    System.Collections.IEnumerator LedgeGrabSequence()
    {
        // Wait for player input to climb or drop
        float timer = 0f;
        float maxHoldTime = 3f; // Max time to hold ledge
        
        while (timer < maxHoldTime)
        {
            timer += Time.deltaTime;
            
            // Check for climb input
            if (climbInput)
            {
                // Start climbing
                StartClimbing();
                yield break;
            }
            
            // Check for drop input (down + jump)
            if (moveInput.y < -0.5f && jumpInput)
            {
                // Drop from ledge
                DropFromLedge();
                yield break;
            }
            
            yield return null;
        }
        
        // Auto-drop after max time
        DropFromLedge();
    }

    void StartClimbing()
    {
        if (playerController == null) return;
        
        // Set climbing state
        playerController.SetClimbing(true);
        playerController.SetLedgeGrabbing(false);
        
        // Determine climb direction based on input
        bool climbUp = moveInput.y > 0.5f;
        bool climbSide = Mathf.Abs(moveInput.x) > 0.5f;
        
        if (climbUp)
        {
            // Climb up over the ledge
            Vector2 climbPosition = ledgePosition + Vector2.up * 1.5f;
            if (!facingRight)
            {
                climbPosition.x -= 0.5f;
            }
            
            // Move player to climb position
            transform.position = climbPosition;
        }
        else if (climbSide)
        {
            // Side climb (if you have side climb animations)
            // This would trigger the side climb animation
        }
        
        // End climbing after a short delay
        StartCoroutine(EndClimbing());
    }

    System.Collections.IEnumerator EndClimbing()
    {
        yield return new WaitForSeconds(0.5f); // Adjust based on your climb animation length
        
        if (playerController != null)
        {
            playerController.SetClimbing(false);
        }
    }

    void DropFromLedge()
    {
        if (playerController == null) return;
        
        // End ledge grab
        playerController.SetLedgeGrabbing(false);
        playerController.SetClimbing(false); // Ensure climbing is false if dropping
        
        // Add a small downward force
        rb.AddForce(Vector2.down * 2f, ForceMode2D.Impulse);
    }

    // Public methods for external access
    public bool IsLedgeDetected() => ledgeDetected;
    public bool CanClimbLedge() => canClimbLedge;
    public bool IsWallDetected() => wallDetected;
    public Vector2 GetLedgePosition() => ledgePosition;

#if UNITY_EDITOR
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Vector2 playerPos = transform.position;
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        
        // Ledge detection gizmos
        Gizmos.color = ledgeGizmoColor;
        Gizmos.DrawRay(playerPos + Vector2.up * 0.5f, direction * ledgeDetectionRange);
        Gizmos.DrawWireSphere(playerPos + Vector2.up * 0.8f, 0.1f);
        
        // Wall detection gizmos
        Gizmos.color = wallGizmoColor;
        Gizmos.DrawRay(playerPos, direction * wallDetectionRange);
        
        // Ground detection gizmos
        Gizmos.color = groundGizmoColor;
        Gizmos.DrawWireSphere(playerPos + Vector2.down * 0.6f, 0.1f);
        
        // Ledge position indicator
        if (ledgeDetected)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ledgePosition, 0.2f);
            Gizmos.DrawWireSphere(ledgePosition + Vector2.up * ledgeGrabHeight, 0.1f);
        }
    }

    // Debug info display - DISABLED for clean play mode
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        // Hide debug panel for clean play mode - only show abilities and health
        return;
        
        GUILayout.BeginArea(new Rect(10, 220, 300, 150));
        GUILayout.Label("Interaction Debug:", GUI.skin.label);
        GUILayout.Label($"Ledge Detected: {ledgeDetected}");
        GUILayout.Label($"Can Climb Ledge: {canClimbLedge}");
        GUILayout.Label($"Wall Detected: {wallDetected}");
        GUILayout.Label($"Ledge Position: {ledgePosition}");
        GUILayout.Label($"Facing Right: {facingRight}");
        GUILayout.EndArea();
    }
#endif
} 