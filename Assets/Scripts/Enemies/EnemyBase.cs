using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies
{
    public abstract class EnemyBase : MonoBehaviour
    {
        // Events
        public event Action<float, float> OnHealthChanged;
        public event Action OnDamageTaken;
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 1f; // Match inspector value
    [SerializeField] protected float patrolWaitTime = 2f; // Match inspector value
    
    
    [Header("Detection")]
    [SerializeField] protected float groundCheckDistance = 0.1f; // Basic ground detection
    [SerializeField] protected float wallCheckDistance = 0.5f; // Match inspector value
    [SerializeField] protected LayerMask groundLayer = 1 << 6; // Default to Ground layer (layer 6)
    [SerializeField] protected LayerMask wallLayer = 1 << 6; // Ground layer
    
    [Header("Edge Detection (Fine Tuning)")]
    [SerializeField] [Tooltip("How far down to raycast for edge detection. Reduce if platforms below interfere.")]
    protected float edgeDetectionDistance = 0.5f;
    [SerializeField] [Tooltip("Depth for scanning if player is on same platform. Affects chase behavior.")]
    protected float platformScanDepth = 5f;
    [SerializeField] [Tooltip("Depth for basic IsGrounded check. Keep small for responsive ground detection.")]
    protected float basicGroundDepth = 0.2f;
    [SerializeField] [Tooltip("Show ground detection visualization in scene view when enemy is selected.")]
    protected bool showGroundDetection = true;
    
    [Header("Combat")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected float attackRange = 0.5f; // Match inspector value
    [SerializeField] protected float attackCooldownMin = 3f;
    [SerializeField] protected float attackCooldownMax = 5f;
    
    [Header("Platform Detection")]
    [SerializeField] protected float levelTolerance = 0.1f; // How much height difference is allowed for same platform
    [SerializeField] protected Vector2 edgeDetectionOffset = new Vector2(0f, -0.1f); // How far ahead to check (x + random offset)
    [SerializeField] protected float platformScanDistance = 10f;
    [SerializeField] protected float platformScanHeight = 0.5f;
    
    
    [Header("Player Interaction")]
    [SerializeField] protected float playerSlowdownFactor = 0.3f;
    [SerializeField] protected LayerMask playerLayer = 1 << 0; // Default layer
    
    // Components
    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected Collider2D col;
    
    // State variables
    protected float currentHealth;
    protected bool isPatrolling = true;
    protected bool isAttacking = false;
    protected bool isDead = false;
    protected bool isFacingRight = true;
    protected float lastAttackTime;
    protected float currentAttackCooldown;
    
    // Performance optimization - cache physics checks
    private float lastBoundaryCheckTime;
    private const float BOUNDARY_CHECK_INTERVAL = 0.1f; // Check every 0.1 seconds instead of every frame
    
    // Patrol variables
    protected int movementDirection = 1; // 1 for right, -1 for left
    protected float waitTimer = 0f;
    protected bool isWaiting = false;
    
    // Platform boundaries
    protected bool hasReachedEdge = false;
    protected bool hasHitWall = false;

    // Randomized edge detection
    protected float currentEdgeOffset;
    
    
    
    protected virtual void Awake()
    {
        // Get components
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        
        // Initialize
        currentHealth = maxHealth;
        
        // Initialize random edge offset
        GenerateRandomEdgeOffset();
        
        // Set physics properties to prevent being pushed by player
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Make enemy immovable by external forces while allowing our movement
        rb.mass = 10f; // Reasonable mass that still allows movement
    }
    
    protected virtual void Start()
    {
        // Set initial facing direction based on sprite flip
        isFacingRight = !spriteRenderer.flipX;
        movementDirection = isFacingRight ? 1 : -1;
        
        // Initialize attack cooldown
        currentAttackCooldown = GetRandomAttackCooldown();
        
        // No need for complex platform detection with simple edge offset approach
        
        // Notify UI of initial health
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    
    protected virtual void Update()
    {
        if (isDead) return;
        
        // Check for platform edges and walls
        CheckPlatformBoundaries();
        
        // Handle different states
        if (isPatrolling && !isAttacking)
        {
            HandlePatrol();
        }
        
        // Update animations
        UpdateAnimations();
    }
    
    protected virtual void FixedUpdate()
    {
        if (isDead || isAttacking) return;
        
        // Apply movement during patrol when conditions are met
        if (isPatrolling && !isWaiting)
        {
            // Only move if we have ground ahead and no wall blocking
            if (!hasReachedEdge && !hasHitWall)
            {
                Move();
            }
            else
            {
                // Stop movement when at boundary
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                
                // Debug why we're not moving
                if (Time.fixedTime % 1.0f < Time.fixedDeltaTime) // Log once per second
                {
                    // Debug.Log($"{gameObject.name}: Stopped at boundary - hasReachedEdge: {hasReachedEdge}, hasHitWall: {hasHitWall}");
                }
            }
        }
        else if (isPatrolling && isWaiting)
        {
            // Stop movement while waiting
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }
    
    protected virtual void CheckPlatformBoundaries()
    {
        // Performance optimization - only check boundaries at intervals, not every frame
        if (Time.time - lastBoundaryCheckTime < BOUNDARY_CHECK_INTERVAL)
            return;
        
        lastBoundaryCheckTime = Time.time;
        
        // Use consistent layer detection
        LayerMask detectionLayer = groundLayer != 0 ? groundLayer : (1 << 6);
        
        // CORRECT EDGE DETECTION: Check if there's ground at the same level ahead
        // Step 1: Find the current ground level
        Vector2 currentGroundCheck = (Vector2)transform.position;
        RaycastHit2D currentGround = Physics2D.Raycast(currentGroundCheck, Vector2.down, edgeDetectionDistance, detectionLayer);
        
        if (currentGround.collider != null)
        {
            // Step 2: Check ahead at the same level as current ground
            float currentGroundY = currentGround.point.y;
            Vector2 aheadCheckPos = (Vector2)transform.position + new Vector2((edgeDetectionOffset.x + currentEdgeOffset) * movementDirection, 0f);
            
            // Cast down from ahead position to see if ground continues at same level
            RaycastHit2D aheadGroundCheck = Physics2D.Raycast(aheadCheckPos, Vector2.down, edgeDetectionDistance, detectionLayer);
            
            if (aheadGroundCheck.collider != null)
            {
                // Check if the ground ahead is at roughly the same level (within tolerance)
                float groundLevelDifference = Mathf.Abs(aheadGroundCheck.point.y - currentGroundY);
                
                // If ground ahead is at same level, path is clear
                hasReachedEdge = groundLevelDifference > levelTolerance;
                
                // Debug visualization
                // Debug.DrawRay(aheadCheckPos, Vector2.down * 2f, hasReachedEdge ? Color.red : Color.green, 0.1f);
                // Debug.DrawLine(new Vector3(currentGround.point.x, currentGroundY, 0), new Vector3(aheadGroundCheck.point.x, aheadGroundCheck.point.y, 0), hasReachedEdge ? Color.red : Color.green, 0.1f);
                
                if (hasReachedEdge)
                {
                    // Debug.Log($"{gameObject.name}: EDGE DETECTED - Current ground Y: {currentGroundY:F2}, Ahead ground Y: {aheadGroundCheck.point.y:F2}, Difference: {groundLevelDifference:F2}");
                }
            }
            else
            {
                // No ground found ahead at all - definitely an edge
                hasReachedEdge = true;
                // Debug.DrawRay(aheadCheckPos, Vector2.down * 2f, Color.red, 0.1f);
                // Debug.Log($"{gameObject.name}: EDGE DETECTED - No ground found ahead at position {aheadCheckPos:F2}");
            }
        }
        else
        {
            // Enemy is not grounded - shouldn't happen during normal patrol
            hasReachedEdge = true;
            // Debug.LogWarning($"{gameObject.name}: Not grounded! Position: {transform.position:F2}");
        }
        
        // Check for wall - use same layer detection
        Vector2 wallCheckPosition = (Vector2)transform.position;
        RaycastHit2D wallCheck = Physics2D.Raycast(wallCheckPosition, Vector2.right * movementDirection, wallCheckDistance, detectionLayer);
        hasHitWall = wallCheck.collider != null;
        
        // Debug visualization for wall check
        // Debug.DrawRay(wallCheckPosition, Vector2.right * movementDirection * wallCheckDistance, hasHitWall ? Color.red : Color.blue, 0.1f);
        
        // Debug logging for wall detection
        if (hasHitWall)
        {
            // Debug.Log($"{gameObject.name}: WALL DETECTED - Position: {wallCheckPosition:F2}, Hit: {wallCheck.collider.name}, Distance: {wallCheck.distance:F2}");
        }
        
        // Debug current state when path is clear
        if (!hasReachedEdge && !hasHitWall && !isWaiting)
        {
            // Debug.Log($"{gameObject.name}: Clear path ahead - Should be moving in direction {movementDirection}");
        }
    }
    
    
    protected virtual void HandlePatrol()
    {
        // Handle wait timer first
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                TurnAround(); // This will generate new random offset and reset boundary flags
                isWaiting = false;
                // Debug.Log($"{gameObject.name}: Wait complete, turned around. New direction: {movementDirection}, flags reset: hasReachedEdge={hasReachedEdge}, hasHitWall={hasHitWall}");
            }
            return; // Exit early if waiting
        }
        
        // Check for boundary conditions (edge or wall)
        if (hasReachedEdge || hasHitWall)
        {
            // Debug.Log($"{gameObject.name}: Boundary hit with random offset {currentEdgeOffset:F2}! Edge={hasReachedEdge}, Wall={hasHitWall}");
            StartWaiting();
        }
        
        // If not waiting and no boundaries hit, the enemy should be moving
        // This will be handled in FixedUpdate() -> Move()
    }
    
    protected virtual void Move()
    {
        Vector2 newVelocity = new Vector2(moveSpeed * movementDirection, rb.linearVelocity.y);
        rb.linearVelocity = newVelocity;
        
        // Debug logging to verify movement is being applied (commented out to reduce console spam)
        // if (Time.fixedTime % 1.0f < Time.fixedDeltaTime) // Log once per second
        // {
        //     // Debug.Log($"{gameObject.name}: Moving - Speed: {moveSpeed}, Direction: {movementDirection}, Velocity: {newVelocity}, Position: {transform.position:F2}");
        // }
    }
    
    protected virtual void StartWaiting()
    {
        isWaiting = true;
        waitTimer = patrolWaitTime;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        // Debug.Log($"{gameObject.name}: Starting wait for {patrolWaitTime} seconds");
    }
    
    protected virtual void GenerateRandomEdgeOffset()
    {
        // Generate random offset for more realistic edge detection variation
        // This will be overridden by derived classes with their own min/max ranges
        currentEdgeOffset = UnityEngine.Random.Range(0f, 0.8f); // Base range - derived classes should override this
        // Debug.Log($"{gameObject.name}: New random edge offset: {currentEdgeOffset:F2} (base range: 0.0 - 0.8)");
    }
    
    protected virtual void TurnAround()
    {
        movementDirection *= -1;
        isFacingRight = !isFacingRight;
        Flip();
        
        // Generate new random edge offset for this direction
        GenerateRandomEdgeOffset();
        
        // Reset boundary flags when turning around so enemy can move again
        hasReachedEdge = false;
        hasHitWall = false;
    }
    
    protected virtual void Flip()
    {
        spriteRenderer.flipX = !isFacingRight;
    }
    
    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        // Trigger events
        OnDamageTaken?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            OnDamageReceived();
        }
    }
    
    protected virtual void OnDamageReceived()
    {
        // Override in derived classes for specific damage reactions
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }
    
    protected virtual void Die()
    {
        isDead = true;
        isPatrolling = false;
        isAttacking = false; // Immediately stop any ongoing attacks
        
        // Stop all coroutines to prevent attack animations from continuing
        StopAllCoroutines();
        
        // Stop movement
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        
        // Disable collider
        if (col != null)
        {
            col.enabled = false;
        }
        
        // Reset animator to idle state before death animation
        if (animator != null)
        {
            // Clear any attack states and reset to idle
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsChasing", false);
            
            // Play death animation
            animator.SetTrigger("Death");
        }
        
        // Destroy after delay (can be overridden)
        StartCoroutine(DestroyAfterDelay(2f));
    }
    
    protected IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    protected virtual void UpdateAnimations()
    {
        if (animator == null || isDead) return; // Don't update animations if dead
        
        // Update animator parameters
        animator.SetBool("IsMoving", Mathf.Abs(rb.linearVelocity.x) > 0.1f && !isWaiting);
        animator.SetBool("IsGrounded", IsGrounded());
        animator.SetFloat("VelocityY", rb.linearVelocity.y);
    }
    
    protected virtual bool IsGrounded()
    {
        // Check if the enemy is on the ground (use configurable depth)
        Vector2 position = transform.position;
        LayerMask detectionLayer = groundLayer != 0 ? groundLayer : (1 << 6); // Consistent with boundary detection
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, basicGroundDepth, detectionLayer);
        
        // Debug visualization for ground check
        // Debug.DrawRay(position, Vector2.down * basicGroundDepth, hit.collider ? Color.green : Color.yellow, 0.1f);
        
        return hit.collider != null;
    }
    
    
    public virtual bool IsPlayerOnSamePlatform(GameObject player)
    {
        if (player == null) return false;
        
        // For larger detection ranges, we want to be more permissive
        // Allow detection of players on different platforms/walls within detection range
        
        // First check if player is grounded (not in air) - but allow wall detection
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && !playerController.IsGrounded && !playerController.OnWall)
        {
            // Only exclude if player is freely falling (not on ground or wall)
            if (playerController.IsFalling)
            {
                return false; // Player is falling through air
            }
        }
        
        // Much more lenient height check for larger detection ranges
        float heightDifference = Mathf.Abs(player.transform.position.y - transform.position.y);
        if (heightDifference > 6f) // Allow significant height differences
        {
            return false; // Only exclude if extremely far vertically
        }
        
        // For large detection ranges, we don't require same collider
        // Just check that both player and enemy are near some ground
        Vector2 playerPos = player.transform.position;
        Vector2 enemyPos = transform.position;
        
        RaycastHit2D playerGroundHit = Physics2D.Raycast(playerPos, Vector2.down, platformScanDepth, groundLayer);
        RaycastHit2D enemyGroundHit = Physics2D.Raycast(enemyPos, Vector2.down, platformScanDepth, groundLayer);
        
        // As long as both have some ground/platform nearby, consider them valid for detection
        bool playerHasGround = playerGroundHit.collider != null;
        bool enemyHasGround = enemyGroundHit.collider != null;
        
        // Also check for walls for the player (wall sliding scenario)
        bool playerOnWall = false;
        if (playerController != null && playerController.OnWall)
        {
            playerOnWall = true;
        }
        
        return enemyHasGround && (playerHasGround || playerOnWall);
    }
    
    protected virtual float GetRandomAttackCooldown()
    {
        return UnityEngine.Random.Range(attackCooldownMin, attackCooldownMax);
    }
    
    // Abstract methods for derived classes to implement
    public abstract void Attack();
    
    // Optional virtual methods for customization
    protected virtual void OnPlayerDetected(GameObject player) { }
    protected virtual void OnPlayerLost() { }
    
    // Collision detection for player interaction
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                ApplyPlayerSlowdown(playerController);
            }
        }
    }
    
    protected virtual void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                RemovePlayerSlowdown(playerController);
            }
        }
    }
    
    protected virtual void ApplyPlayerSlowdown(PlayerController playerController)
    {
        // This would need to be implemented in PlayerController
        // For now, we'll add a method call that can be implemented later
        // Debug.Log("Player entered enemy contact zone - applying slowdown");
    }
    
    protected virtual void RemovePlayerSlowdown(PlayerController playerController)
    {
        // This would need to be implemented in PlayerController
        // For now, we'll add a method call that can be implemented later
        // Debug.Log("Player exited enemy contact zone - removing slowdown");
    }
    
    // Public methods for health
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    
    // Gizmos for debugging
    protected virtual void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;
        
        if (showGroundDetection)
        {
            DrawGroundDetectionGizmos(position);
        }
        
        // Draw wall detection
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(position, position + Vector3.right * wallCheckDistance * (isFacingRight ? 1 : -1));
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position, attackRange);
        
        // Draw current edge detection offset
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(1, 1, 0, 0.8f); // Yellow for current edge offset
            Vector3 offsetPos = position + Vector3.right * (currentEdgeOffset * (isFacingRight ? 1 : -1));
            Gizmos.DrawWireSphere(offsetPos, 0.15f);
            
            // Draw line showing edge detection distance
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawLine(position, offsetPos);
        }
    }
    
    protected virtual void DrawGroundDetectionGizmos(Vector3 position)
    {
        // Get the detection layer
        LayerMask detectionLayer = groundLayer != 0 ? groundLayer : (1 << 6);
        
        // 1. Draw current ground check (directly below enemy)
        Gizmos.color = Color.green;
        Vector3 currentGroundStart = position;
        Vector3 currentGroundEnd = currentGroundStart + Vector3.down * edgeDetectionDistance;
        Gizmos.DrawLine(currentGroundStart, currentGroundEnd);
        
        // Show current ground hit if available
        RaycastHit2D currentGroundHit = Physics2D.Raycast(currentGroundStart, Vector2.down, edgeDetectionDistance, detectionLayer);
        if (currentGroundHit.collider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentGroundHit.point, 0.1f);
        }
        
        // 2. Draw edge detection check (ahead position)
        Vector3 aheadCheckPos = position + new Vector3((edgeDetectionOffset.x + (Application.isPlaying ? currentEdgeOffset : 0.4f)) * (isFacingRight ? 1 : -1), 0f, 0f);
        
        // Draw the ahead ground check
        Gizmos.color = Color.yellow;
        Vector3 aheadGroundStart = aheadCheckPos;
        Vector3 aheadGroundEnd = aheadGroundStart + Vector3.down * edgeDetectionDistance;
        Gizmos.DrawLine(aheadGroundStart, aheadGroundEnd);
        
        // Show ahead ground hit if available
        RaycastHit2D aheadGroundHit = Physics2D.Raycast(aheadCheckPos, Vector2.down, edgeDetectionDistance, detectionLayer);
        if (aheadGroundHit.collider != null)
        {
            // Check if it would be considered an edge (different level)
            bool wouldBeEdge = currentGroundHit.collider != null && 
                              Mathf.Abs(aheadGroundHit.point.y - currentGroundHit.point.y) > levelTolerance;
            
            Gizmos.color = wouldBeEdge ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(aheadGroundHit.point, 0.1f);
            
            // Draw line connecting current and ahead ground levels
            if (currentGroundHit.collider != null)
            {
                Gizmos.color = wouldBeEdge ? Color.red : Color.green;
                Gizmos.DrawLine((Vector3)currentGroundHit.point, (Vector3)aheadGroundHit.point);
            }
        }
        else
        {
            // No ground found ahead - definitely an edge
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(aheadGroundEnd, 0.1f);
        }
        
        // 3. Draw level tolerance indicator
        if (currentGroundHit.collider != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan, semi-transparent
            Vector3 toleranceCenter = (Vector3)currentGroundHit.point;
            Vector3 toleranceSize = new Vector3(2f, levelTolerance * 2f, 0.1f);
            Gizmos.DrawCube(toleranceCenter, toleranceSize);
        }
        
        // 4. Draw basic ground check (for IsGrounded)
        Gizmos.color = Color.cyan;
        Vector3 basicGroundStart = position;
        Vector3 basicGroundEnd = basicGroundStart + Vector3.down * basicGroundDepth;
        Gizmos.DrawLine(basicGroundStart, basicGroundEnd);
        
        // 5. Add labels in scene view
        #if UNITY_EDITOR
        if (currentGroundHit.collider != null)
        {
            UnityEditor.Handles.Label((Vector3)currentGroundHit.point + Vector3.up * 0.2f, $"Current Ground: {currentGroundHit.point.y:F2}");
        }
        if (aheadGroundHit.collider != null)
        {
            UnityEditor.Handles.Label((Vector3)aheadGroundHit.point + Vector3.up * 0.2f, $"Ahead Ground: {aheadGroundHit.point.y:F2}");
            if (currentGroundHit.collider != null)
            {
                float diff = Mathf.Abs(aheadGroundHit.point.y - currentGroundHit.point.y);
                UnityEditor.Handles.Label((Vector3)aheadGroundHit.point + Vector3.up * 0.4f, $"Diff: {diff:F2} (Tolerance: {levelTolerance:F2})");
            }
        }
        
        // Parameter labels
        UnityEditor.Handles.Label(position + Vector3.up * 1f, $"Edge Detection Distance: {edgeDetectionDistance:F2}");
        UnityEditor.Handles.Label(position + Vector3.up * 1.2f, $"Platform Scan Depth: {platformScanDepth:F2}");
        UnityEditor.Handles.Label(position + Vector3.up * 1.4f, $"Basic Ground Depth: {basicGroundDepth:F2}");
        UnityEditor.Handles.Label(position + Vector3.up * 1.6f, $"Level Tolerance: {levelTolerance:F2}");
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(position + Vector3.up * 1.8f, $"Current Edge Offset: {currentEdgeOffset:F2}");
        }
        #endif
    }
    }
}