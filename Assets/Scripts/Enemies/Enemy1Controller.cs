using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;

namespace Enemies
{
    public class Enemy1Controller : EnemyBase
    {
    [Header("Enemy1 Specific")]
    [SerializeField] private float playerDetectionRange = 5f; // Match inspector value
    [SerializeField] private float chaseSpeed = 2f; // Match inspector value
    [SerializeField] private float directionChangeDelay = 0.2f; // Prevent rapid direction changes
    [SerializeField] private float chaseExitDelay = 5f; // Time to wait before leaving chase mode after losing player
    [SerializeField] private bool debugPatrolTargets = false; // Show debug info for patrol targets
    
    [Header("Randomized Patrol")]
    [SerializeField] private float minEdgeOffset = 0.1f; // Match inspector value - minimum distance from edge to stop
    [SerializeField] private float maxEdgeOffset = 0.8f; // Match inspector value - maximum distance from edge to stop
    
    [Header("Detection Settings")]
    [SerializeField] private bool requireFacingForDetection = true; // Only detect player when facing them
    [SerializeField] private float detectionArcAngle = 90f; // Detection arc angle in patrol mode (90 = front quarter)
    [SerializeField] private bool useDirectionalAttack = true; // Use directional attack in patrol mode
    [SerializeField] private float heightTolerance = 0.1f; // How far below enemy level player can be and still be detected
    
    // Detection
    private GameObject player;
    private bool isPlayerDetected = false;
    private bool isChasing = false;
    private float originalMoveSpeed;
    private float chaseStartTime;
    private float lastDirectionChangeTime;
    
    // Public properties
    public bool IsFacingRight => isFacingRight;
    private float chaseExitTimer = 0f; // Timer for delayed chase exit
    private bool waitingToExitChase = false; // Flag to track if we're waiting to exit chase
    
    // Performance optimization - cache player detection
    private float lastPlayerDetectionTime;
    private const float PLAYER_DETECTION_INTERVAL = 0.05f; // Check every 0.05 seconds (20fps)
    
    protected override void Awake()
    {
        base.Awake();
        originalMoveSpeed = moveSpeed;
    }
    
    protected override void Start()
    {
        base.Start();
        
        // Ensure we start in patrol mode
        isPatrolling = true;
        isChasing = false;
        isPlayerDetected = false;
        waitingToExitChase = false;
        chaseExitTimer = 0f;
        
        // // Debug.Log($"{gameObject.name}: Started with patrol={isPatrolling}, chase={isChasing}, waiting={isWaiting}, direction={movementDirection}, edgeOffset={currentEdgeOffset:F2}");
    }
    
    protected override void GenerateRandomEdgeOffset()
    {
        currentEdgeOffset = Random.Range(minEdgeOffset, maxEdgeOffset);
        // // Debug.Log($"{gameObject.name}: New random edge offset: {currentEdgeOffset:F2} (range: {minEdgeOffset:F2} - {maxEdgeOffset:F2})");
    }
    
    private void HandleChaseExitTimer()
    {
        // Handle delayed chase exit when waiting to leave chase mode
        if (waitingToExitChase)
        {
            chaseExitTimer -= Time.deltaTime;
            if (chaseExitTimer <= 0f)
            {
                // Timer expired - actually exit chase mode now
                // // Debug.Log($"{gameObject.name}: Chase exit timer expired. Returning to patrol mode.");
                waitingToExitChase = false;
                StopChasing();
            }
        }
    }
    
    protected override void Update()
    {
        if (isDead) return;
        
        // Always check platform boundaries
        CheckPlatformBoundaries();
        
        // Check for player
        DetectPlayer();
        
        // Handle delayed chase exit timer
        HandleChaseExitTimer();
        
        // Handle patrol logic only when not chasing and no player detected
        if (!isChasing && !isPlayerDetected)
        {
            // Normal patrol behavior
            if (isPatrolling && !isAttacking)
            {
                if (debugPatrolTargets)
                {
                    // // Debug.Log($"{gameObject.name}: Patrolling - X={transform.position.x:F2}, EdgeOffset={currentEdgeOffset:F2}, Dir={movementDirection}");
                }
                HandlePatrol();
            }
        }
        
        // Update animations
        UpdateAnimations();
    }
    
    private void DetectPlayer()
    {
        // Performance optimization - only check player detection at intervals
        if (Time.time - lastPlayerDetectionTime < PLAYER_DETECTION_INTERVAL)
            return;
        
        lastPlayerDetectionTime = Time.time;
        
        // Choose detection method based on current state
        Collider2D playerCollider;
        
        if (isChasing)
        {
            // Chase mode: Full 360° detection (but still northern hemisphere only)
            playerCollider = GetPlayerInNorthernHemisphere(true);
        }
        else
        {
            // Patrol mode: Directional detection in front only (northern hemisphere)
            playerCollider = GetPlayerInNorthernHemisphere(false);
        }
        
        if (playerCollider != null)
        {
            GameObject detectedPlayer = playerCollider.gameObject;
            
            // Check if enemy is facing the player (only in patrol mode - chase mode has 360° detection)
            if (requireFacingForDetection && !isChasing)
            {
                Vector3 directionToPlayer = detectedPlayer.transform.position - transform.position;
                bool facingPlayer = (directionToPlayer.x > 0 && isFacingRight) || (directionToPlayer.x < 0 && !isFacingRight);
                
                if (!facingPlayer)
                {
                    // Enemy not facing player - no detection (patrol mode only)
                    if (isPlayerDetected)
                    {
                        // Debug.Log($"{gameObject.name}: Player behind enemy in patrol mode. Stopping detection. Was chasing: {isChasing}");
                        isPlayerDetected = false;
                        player = null;
                        
                        if (isChasing && !waitingToExitChase)
                        {
                            // Start delayed chase exit timer
                            // Debug.Log($"{gameObject.name}: Starting {chaseExitDelay}s timer before returning to patrol.");
                            waitingToExitChase = true;
                            chaseExitTimer = chaseExitDelay;
                        }
                        else if (!isChasing)
                        {
                            OnPlayerLost();
                        }
                    }
                    return;
                }
            }
            
            // Check if player is on the same platform
            bool playerOnSamePlatform = IsPlayerOnSamePlatform(detectedPlayer);
            if (!playerOnSamePlatform)
            {
                // Player is detected but not on our platform - don't chase
                if (isPlayerDetected)
                {
                    // Debug.Log($"{gameObject.name}: Player no longer on same platform. Was chasing: {isChasing}");
                    isPlayerDetected = false;
                    player = null;
                    
                    if (isChasing && !waitingToExitChase)
                    {
                        // Start delayed chase exit timer
                        // Debug.Log($"{gameObject.name}: Starting {chaseExitDelay}s timer before returning to patrol.");
                        waitingToExitChase = true;
                        chaseExitTimer = chaseExitDelay;
                    }
                    else if (!isChasing)
                    {
                        OnPlayerLost();
                    }
                }
                return;
            }
            
            if (!isPlayerDetected)
            {
                // Player just entered detection range and is on our platform
                player = detectedPlayer;
                isPlayerDetected = true;
                // Debug.Log($"{gameObject.name}: Player detected and on same platform! Starting chase immediately.");
                OnPlayerDetected(player);
                
                // Cancel any pending chase exit
                if (waitingToExitChase)
                {
                    // Debug.Log($"{gameObject.name}: Player re-detected during chase exit timer. Canceling exit.");
                    waitingToExitChase = false;
                    chaseExitTimer = 0f;
                }
                
                // Immediately start chasing when player is detected
                StartChasing();
            }
            else
            {
                // Player was already detected - cancel any pending chase exit since player is still here
                if (waitingToExitChase)
                {
                    // Debug.Log($"{gameObject.name}: Player still detected. Canceling chase exit timer.");
                    waitingToExitChase = false;
                    chaseExitTimer = 0f;
                }
            }
            
            // Continue tracking player
            player = detectedPlayer;
            
            // Always chase the player when detected (unless attacking)
            if (!isAttacking)
            {
                // Check if player is within attack range (directional in patrol, full range in chase)
                float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
                bool inAttackRange = IsPlayerInAttackRange(player.transform.position, distanceToPlayer);
                
                if (inAttackRange)
                {
                    // Within attack range - stop moving and face player
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    FacePlayer();
                    
                    // Attack if cooldown is ready
                    if (Time.time >= lastAttackTime + currentAttackCooldown)
                    {
                        Attack();
                    }
                    // If on cooldown, just wait at current position
                }
                else
                {
                    // Outside attack range - chase player
                    ChasePlayer();
                }
            }
        }
        else
        {
            if (isPlayerDetected)
            {
                // Player just left detection range
                isPlayerDetected = false;
                player = null;
                
                if (isChasing && !waitingToExitChase)
                {
                    // Start delayed chase exit timer
                    // Debug.Log($"{gameObject.name}: Player out of detection range! Starting {chaseExitDelay}s timer before returning to patrol.");
                    waitingToExitChase = true;
                    chaseExitTimer = chaseExitDelay;
                }
                else if (!isChasing)
                {
                    // Not chasing, exit immediately
                    // Debug.Log($"{gameObject.name}: Player out of detection range while in patrol mode.");
                    OnPlayerLost();
                }
            }
        }
    }
    
    private void ChasePlayer()
    {
        if (player == null || isAttacking) return; // Don't chase during attacks
        
        // Determine direction to player
        float directionToPlayer = Mathf.Sign(player.transform.position.x - transform.position.x);
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        
        // Don't try to get too close - maintain minimum distance  
        if (IsPlayerInAttackRange(player.transform.position, distanceToPlayer))
        {
            // Within attack range - stop moving
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            FacePlayer();
            return;
        }
        
        // Check if chasing would cause us to leave the platform
        if (!CanMoveInDirection((int)directionToPlayer))
        {
            // Can't chase in that direction without falling off platform
            // Stop movement and maintain facing direction towards player
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            
            // Still face the player even if we can't move
            if ((directionToPlayer > 0 && !isFacingRight) || (directionToPlayer < 0 && isFacingRight))
            {
                isFacingRight = directionToPlayer > 0;
                Flip();
            }
            
            // Debug.Log($"{gameObject.name}: Can't chase - would leave platform. Facing player and waiting.");
            return;
        }
        
        // Make sure we're in chase state
        if (!isChasing)
        {
            StartChasing();
        }
        
        // Update movement direction and facing
        movementDirection = (int)directionToPlayer;
        if ((directionToPlayer > 0 && !isFacingRight) || (directionToPlayer < 0 && isFacingRight))
        {
            isFacingRight = directionToPlayer > 0;
            Flip();
        }
    }
    
    private void FacePlayer()
    {
        if (player == null) return;
        
        // Prevent rapid direction changes during combat
        if (Time.time - lastDirectionChangeTime < directionChangeDelay) return;
        
        // Determine direction to player
        float directionToPlayer = Mathf.Sign(player.transform.position.x - transform.position.x);
        
        // Only update facing direction if player is significantly on the other side
        // This prevents flickering when player is very close
        if ((directionToPlayer > 0 && !isFacingRight) || (directionToPlayer < 0 && isFacingRight))
        {
            // Only change if the player has moved far enough to the other side
            float distanceFromCenter = Mathf.Abs(player.transform.position.x - transform.position.x);
            if (distanceFromCenter > 0.3f) // Reduced threshold for more responsive facing
            {
                isFacingRight = directionToPlayer > 0;
                lastDirectionChangeTime = Time.time;
                Flip();
            }
        }
    }
    
    private bool CanMoveInDirection(int direction)
    {
        // Check for platform edge in the direction we want to move
        // Use a smaller offset and distance for chase movement to be more aggressive
        float chaseEdgeOffset = 0.3f; // Smaller offset for more aggressive chasing
        float chaseEdgeDistance = 1f; // Smaller distance - only check 1 unit down instead of 2
        Vector2 edgeCheckPosition = (Vector2)transform.position + new Vector2(chaseEdgeOffset * direction, 0f);
        
        // Only check Ground layer (layer 6) - exclude player and other layers
        int groundLayerOnly = 1 << 6;
        RaycastHit2D groundCheck = Physics2D.Raycast(edgeCheckPosition, Vector2.down, chaseEdgeDistance, groundLayerOnly);
        bool wouldReachEdge = !groundCheck.collider;
        
        // Debug the edge check
        // Debug.DrawRay(edgeCheckPosition, Vector2.down * chaseEdgeDistance, wouldReachEdge ? Color.red : Color.green, 0.1f);
        
        // Check for wall in the direction we want to move - only check Ground layer
        Vector2 wallCheckPosition = (Vector2)transform.position;
        RaycastHit2D wallCheck = Physics2D.Raycast(wallCheckPosition, Vector2.right * direction, wallCheckDistance, groundLayerOnly);
        bool wouldHitWall = wallCheck.collider != null;
        
        // We rely on the base class edge detection with randomized offset
        // No additional platform bounds checking needed
        
        bool canMove = !wouldReachEdge && !wouldHitWall;
        if (!canMove)
        {
            Vector2 debugPos = transform.position;
            // Debug.Log($"{gameObject.name}: Can't move in direction {direction}. Edge: {wouldReachEdge}, Wall: {wouldHitWall}, WallHit: {(wallCheck.collider ? wallCheck.collider.name : "none")}");
            // Debug.Log($"  EdgeCheck: pos={debugPos:F2}, checkPos={edgeCheckPosition:F2}, distance={chaseEdgeDistance:F2}, groundHit={groundCheck.collider != null}");
            if (groundCheck.collider != null)
            {
                // Debug.Log($"  GroundHit: {groundCheck.collider.name} at {groundCheck.point:F2}");
            }
        }
        
        return canMove;
    }
    
    private void StartChasing()
    {
        isChasing = true;
        isPatrolling = false;
        isWaiting = false;
        waitTimer = 0f; // Clear any wait timer
        chaseStartTime = Time.time;
        moveSpeed = chaseSpeed;
        
        // Immediately stop any patrol movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }
    
    private void StopChasing()
    {
        // Debug.Log($"{gameObject.name}: StopChasing called! Before - isChasing={isChasing}, cooldown={currentAttackCooldown:F2}");
        
        isChasing = false;
        isPatrolling = true;
        moveSpeed = originalMoveSpeed;
        
        // Reset chase exit timer state
        waitingToExitChase = false;
        chaseExitTimer = 0f;
        
        // Reset attack cooldown when leaving chase mode - allow immediate attack
        currentAttackCooldown = GetRandomAttackCooldown();
        lastAttackTime = Time.time - currentAttackCooldown; // Allow immediate attack by setting time in the past
        
        // Reset wait state to prevent immediate direction changes
        isWaiting = true;
        waitTimer = patrolWaitTime * 0.5f; // Shorter wait when transitioning back from chase
        
        // Debug.Log($"{gameObject.name}: StopChasing complete! After - isChasing={isChasing}, new cooldown={currentAttackCooldown:F2}");
    }
    
    protected override void OnPlayerDetected(GameObject player)
    {
        // React to player detection
        if (animator != null)
        {
            animator.SetTrigger("Alert");
        }
        
        // Stop patrol wait
        isWaiting = false;
        waitTimer = 0f;
    }
    
    protected override void OnPlayerLost()
    {
        // Stop chasing and return to patrol
        StopChasing();
    }
    
    protected override void FixedUpdate()
    {
        if (isDead || isAttacking) return;
        
        // Apply movement for both patrol and chase (but not during attacks)
        if (isChasing && player != null)
        {
            // Check distance to player first
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            
            // Only move if we're outside attack range
            if (!IsPlayerInAttackRange(player.transform.position, distanceToPlayer))
            {
                // Chase movement - only if we can move in that direction
                float directionToPlayer = Mathf.Sign(player.transform.position.x - transform.position.x);
                if (CanMoveInDirection((int)directionToPlayer))
                {
                    Move();
                }
                else
                {
                    // Stop movement if we can't chase without falling off
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                }
            }
            else
            {
                // Within attack range - don't move
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
        else if (isPatrolling && !isWaiting)
        {
            // Normal patrol movement
            Move();
        }
    }
    
    public override void Attack()
    {
        if (isAttacking || isDead) return;
        
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Set a new random cooldown for the next attack
        currentAttackCooldown = GetRandomAttackCooldown();
        // Debug.Log($"Enemy1 attacking! Next attack cooldown: {currentAttackCooldown:F2} seconds");
        
        // Stop movement during attack
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Start hitbox attack (this would be called by animation event in a full implementation)
        StartCoroutine(StartHitboxAttackAfterDelay(0.3f)); // Shorter delay for hitbox activation
    }
    
    private IEnumerator StartHitboxAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (!isDead)
        {
            // Start the hitbox attack
            Enemy1Hitbox enemy1Hitbox = GetComponentInChildren<Enemy1Hitbox>();
            if (enemy1Hitbox != null)
            {
                enemy1Hitbox.StartLanceChopAttack();
                
                // End the hitbox attack after a duration (lance chop duration)
                StartCoroutine(EndHitboxAttackAfterDelay(0.4f)); // Hitbox active for 0.4 seconds
            }
            else
            {
                Debug.LogWarning("Enemy1Hitbox not found! Please add Enemy1Hitbox component to a child GameObject.");
                // Fallback to old damage system if no hitbox
                StartCoroutine(DealDamageAfterDelay(0.2f));
            }
        }
    }
    
    private IEnumerator EndHitboxAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Check if enemy died during the attack - if so, don't continue
        if (isDead) yield break;
        
        Enemy1Hitbox enemy1Hitbox = GetComponentInChildren<Enemy1Hitbox>();
        if (enemy1Hitbox != null)
        {
            enemy1Hitbox.EndLanceChopAttack();
        }
        
        // Attack finished - don't change facing direction here
        // The next attack cycle will handle facing direction if needed
        isAttacking = false;
    }
    
    // Fallback damage method (kept for compatibility)
    private IEnumerator DealDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Check if enemy died during the attack - if so, don't continue
        if (isDead) yield break;
        
        if (player != null && !isDead)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            if (IsPlayerInAttackRange(player.transform.position, distanceToPlayer))
            {
                // Check if we hit the player
                PlayerController playerController = player.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                    if (playerHealth != null && !playerHealth.IsInvincible)
                    {
                        playerHealth.TakeDamage(attackDamage);
                        Debug.Log($"Enemy1 dealt {attackDamage} damage to player! (fallback method)");
                    }
                }
            }
        }
        
        // Attack finished
        isAttacking = false;
    }
    
    protected override void UpdateAnimations()
    {
        if (animator == null || isDead) return; // Don't update animations if dead
        
        // Handle IsMoving with proper logic for chase-stop scenarios
        bool shouldBeMoving = false;
        
        if (isChasing && player != null && !isAttacking)
        {
            // When chasing, only show moving animation if we're actually trying to move
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            shouldBeMoving = !IsPlayerInAttackRange(player.transform.position, distanceToPlayer) && Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        }
        else if (isPatrolling && !isWaiting && !isAttacking)
        {
            // Normal patrol movement
            shouldBeMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        }
        
        // Update base animations but override IsMoving
        animator.SetBool("IsMoving", shouldBeMoving);
        animator.SetBool("IsGrounded", IsGrounded());
        animator.SetFloat("VelocityY", rb.linearVelocity.y);
        
        // Enemy1 specific animations
        animator.SetBool("IsChasing", isChasing && !isAttacking);
        animator.SetBool("IsAttacking", isAttacking);
    }
    
    private Collider2D GetPlayerInDetectionArc()
    {
        // Get all colliders in detection range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, playerDetectionRange, playerLayer);
        
        foreach (Collider2D collider in colliders)
        {
            Vector3 directionToPlayer = collider.transform.position - transform.position;
            
            // NORTHERN HEMISPHERE CHECK: Only detect players at same level or above
            if (directionToPlayer.y < -heightTolerance) // Player is significantly below enemy
            {
                continue; // Skip this player - they're on a lower platform
            }
            
            float angleToPlayer = Vector3.Angle(isFacingRight ? Vector3.right : Vector3.left, directionToPlayer);
            
            // Check if player is within the detection arc
            if (angleToPlayer <= detectionArcAngle * 0.5f)
            {
                return collider;
            }
        }
        
        return null; // No player found in detection arc
    }
    
    private Collider2D GetPlayerInNorthernHemisphere(bool fullCircle = false)
    {
        // Get all colliders in detection range
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, playerDetectionRange, playerLayer);
        
        foreach (Collider2D collider in colliders)
        {
            Vector3 directionToPlayer = collider.transform.position - transform.position;
            
            // NORTHERN HEMISPHERE CHECK: Only detect players at same level or above
            if (directionToPlayer.y < -heightTolerance) // Player is significantly below enemy
            {
                continue; // Skip this player - they're on a lower platform
            }
            
            if (fullCircle)
            {
                // Full 360° detection (chase mode)
                return collider;
            }
            else
            {
                // Directional detection (patrol mode)
                float angleToPlayer = Vector3.Angle(isFacingRight ? Vector3.right : Vector3.left, directionToPlayer);
                if (angleToPlayer <= detectionArcAngle * 0.5f)
                {
                    return collider;
                }
            }
        }
        
        return null; // No player found in northern hemisphere
    }
    
    private void DrawNorthernHemisphereSphere(Vector3 center, float radius)
    {
        // Draw upper semicircle (northern hemisphere)
        for (int i = 0; i <= 180; i += 10)
        {
            float angle1 = i * Mathf.Deg2Rad;
            float angle2 = (i + 10) * Mathf.Deg2Rad;
            
            Vector3 point1 = center + new Vector3(
                Mathf.Cos(angle1) * radius,
                Mathf.Sin(angle1) * radius,
                0
            );
            
            Vector3 point2 = center + new Vector3(
                Mathf.Cos(angle2) * radius,
                Mathf.Sin(angle2) * radius,
                0
            );
            
            // Only draw if both points are in northern hemisphere
            if (point1.y >= center.y - heightTolerance && point2.y >= center.y - heightTolerance)
            {
                Gizmos.DrawLine(point1, point2);
            }
        }
        
        // Draw horizontal line at the height tolerance level
        Gizmos.color = new Color(1, 1, 1, 0.5f); // White line for height boundary
        Vector3 leftBoundary = center + Vector3.left * radius + Vector3.down * heightTolerance;
        Vector3 rightBoundary = center + Vector3.right * radius + Vector3.down * heightTolerance;
        Gizmos.DrawLine(leftBoundary, rightBoundary);
    }
    
    private bool IsPlayerInAttackRange(Vector3 playerPosition, float distance)
    {
        if (distance > attackRange) return false;
        
        Vector3 directionToPlayer = playerPosition - transform.position;
        
        // NORTHERN HEMISPHERE CHECK: Don't attack players below
        if (directionToPlayer.y < -heightTolerance)
        {
            return false; // Player is on a lower platform - can't attack
        }
        
        // In chase mode, attack range is full circle (but still northern hemisphere)
        if (isChasing) return true;
        
        // In patrol mode, check if attack should be directional
        if (!useDirectionalAttack) return true;
        
        // Directional attack: only attack if player is in front
        float angleToPlayer = Vector3.Angle(isFacingRight ? Vector3.right : Vector3.left, directionToPlayer);
        
        // Use a smaller arc for attacks than detection (more focused)
        float attackArcAngle = detectionArcAngle * 0.7f; // 70% of detection arc
        return angleToPlayer <= attackArcAngle * 0.5f;
    }
    
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        Vector3 position = transform.position;
        
        // Draw detection range based on current mode
        if (isChasing)
        {
            // Chase mode: Full northern hemisphere detection (always shows full half-circle)
            Gizmos.color = new Color(1, 0, 0, 0.3f); // Red for chase mode
            DrawNorthernHemisphereSphere(position, playerDetectionRange);
        }
        else
        {
            // Patrol mode: Directional detection
            Gizmos.color = new Color(1, 1, 0, 0.3f); // Yellow for patrol mode
            float detectionDirection = isFacingRight ? 1f : -1f;
            float halfArc = detectionArcAngle * 0.5f;
            
            // Draw detection arc (only northern hemisphere)
            for (float i = -halfArc; i <= halfArc; i += 10f)
            {
                float angle = i * Mathf.Deg2Rad;
                // Rotate the angle based on facing direction
                float actualAngle = angle + (isFacingRight ? 0 : Mathf.PI);
                
                Vector3 point1 = position + new Vector3(
                    Mathf.Cos(actualAngle) * playerDetectionRange,
                    Mathf.Sin(actualAngle) * playerDetectionRange,
                    0
                );
                
                // Only draw if point is in northern hemisphere (y >= -heightTolerance)
                if (point1.y >= position.y - heightTolerance)
                {
                    float nextAngle = (i + 10f) * Mathf.Deg2Rad + (isFacingRight ? 0 : Mathf.PI);
                    Vector3 point2 = position + new Vector3(
                        Mathf.Cos(nextAngle) * playerDetectionRange,
                        Mathf.Sin(nextAngle) * playerDetectionRange,
                        0
                    );
                    
                    if (point2.y >= position.y - heightTolerance)
                    {
                        Gizmos.DrawLine(point1, point2);
                    }
                }
            }
            
            // Draw arc boundary lines
            float leftBoundary = (-halfArc) * Mathf.Deg2Rad + (isFacingRight ? 0 : Mathf.PI);
            float rightBoundary = (halfArc) * Mathf.Deg2Rad + (isFacingRight ? 0 : Mathf.PI);
            
            Gizmos.DrawLine(position, position + new Vector3(
                Mathf.Cos(leftBoundary) * playerDetectionRange,
                Mathf.Sin(leftBoundary) * playerDetectionRange,
                0
            ));
            Gizmos.DrawLine(position, position + new Vector3(
                Mathf.Cos(rightBoundary) * playerDetectionRange,
                Mathf.Sin(rightBoundary) * playerDetectionRange,
                0
            ));
        }
        
        // Draw attack range - FIXED: Only show northern hemisphere for both modes
        if (isChasing)
        {
            // Chase mode: Full northern hemisphere attack range
            Gizmos.color = new Color(1, 0, 0, 0.5f); // Red for chase attack
            DrawNorthernHemisphereSphere(position, attackRange);
        }
        else if (useDirectionalAttack)
        {
            // Patrol mode: Directional attack range (northern hemisphere only)
            Gizmos.color = new Color(1, 0.5f, 0, 0.5f); // Orange for directional attack
            float attackArcAngle = detectionArcAngle * 0.7f; // Smaller than detection arc
            float halfAttackArc = attackArcAngle * 0.5f;
            
            // Draw attack arc (only northern hemisphere)
            for (float i = -halfAttackArc; i <= halfAttackArc; i += 15f)
            {
                float angle = i * Mathf.Deg2Rad;
                float actualAngle = angle + (isFacingRight ? 0 : Mathf.PI);
                
                Vector3 point1 = position + new Vector3(
                    Mathf.Cos(actualAngle) * attackRange,
                    Mathf.Sin(actualAngle) * attackRange,
                    0
                );
                
                // Only draw if point is in northern hemisphere
                if (point1.y >= position.y - heightTolerance)
                {
                    float nextAngle = (i + 15f) * Mathf.Deg2Rad + (isFacingRight ? 0 : Mathf.PI);
                    Vector3 point2 = position + new Vector3(
                        Mathf.Cos(nextAngle) * attackRange,
                        Mathf.Sin(nextAngle) * attackRange,
                        0
                    );
                    
                    if (point2.y >= position.y - heightTolerance)
                    {
                        Gizmos.DrawLine(point1, point2);
                    }
                }
            }
            
            // Draw attack arc boundary lines
            float leftAttackBoundary = (-halfAttackArc) * Mathf.Deg2Rad + (isFacingRight ? 0 : Mathf.PI);
            float rightAttackBoundary = (halfAttackArc) * Mathf.Deg2Rad + (isFacingRight ? 0 : Mathf.PI);
            
            Vector3 leftPoint = position + new Vector3(
                Mathf.Cos(leftAttackBoundary) * attackRange,
                Mathf.Sin(leftAttackBoundary) * attackRange,
                0
            );
            Vector3 rightPoint = position + new Vector3(
                Mathf.Cos(rightAttackBoundary) * attackRange,
                Mathf.Sin(rightAttackBoundary) * attackRange,
                0
            );
            
            // Only draw boundary lines if they're in northern hemisphere
            if (leftPoint.y >= position.y - heightTolerance)
            {
                Gizmos.DrawLine(position, leftPoint);
            }
            if (rightPoint.y >= position.y - heightTolerance)
            {
                Gizmos.DrawLine(position, rightPoint);
            }
        }
        else
        {
            // Non-directional attack in patrol mode (shouldn't happen with current settings)
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            DrawNorthernHemisphereSphere(position, attackRange);
        }
    }
    }
}