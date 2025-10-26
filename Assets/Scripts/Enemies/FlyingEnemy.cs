using System;
using System.Collections;
using UnityEngine;
using Enemies;
using Player;

/// <summary>
/// Flying enemy system with aerial movement patterns.
/// Handles hover patrol, chase, and attack with aerial movement.
/// Compatible with existing head stomp and hitbox systems.
/// </summary>
public class FlyingEnemy : MonoBehaviour, IEnemyBase
{
    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float retreatSpeed = 1.5f; // Slower than chase - player can catch up
    [SerializeField] private float hoverAmplitude = 0.5f; // How much to bob up and down
    [SerializeField] private float hoverFrequency = 1f; // How fast to bob
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float chaseExitDelay = 5f; // Time before returning to patrol after losing player
    [SerializeField] public LayerMask playerLayer = 1 << 0; // Public for head stomp compatibility

    [Header("Combat - Ranged Attack")]
    [SerializeField] private float attackRange = 8f; // Max range to detect and attack player
    [SerializeField] private float preferredDistance = 5f; // Preferred distance to maintain from player
    [SerializeField] private float tooCloseDistance = 3f; // Back away if player gets this close
    [SerializeField] private float attackCooldownMin = 2f;
    [SerializeField] private float attackCooldownMax = 4f;
    [SerializeField] private float projectileDamage = 15f;
    [SerializeField] private GameObject projectilePrefab; // Assign in Inspector
    [SerializeField] private Vector2 projectileSpawnOffset = new Vector2(0.5f, 0f); // Offset from enemy position
    [SerializeField] private float aimHeightOffset = 0.8f; // How much higher to aim (targets upper body/head)

    [Header("Patrol Pattern")]
    [SerializeField] private PatrolType patrolType = PatrolType.HorizontalHover;
    [SerializeField] private float patrolDistance = 5f; // Distance to travel in one direction
    [SerializeField] private float circleRadius = 3f; // Radius for circular patrol
    [SerializeField] private float circleSpeed = 1f; // Speed for circular patrol

    [Header("Boundaries")]
    [SerializeField] private float minHeightAboveGround = 2f; // Minimum height to maintain above ground
    [SerializeField] private float groundCheckDistance = 5f; // How far down to check for ground
    [SerializeField] private float maxHeight = 10f; // Maximum height
    [SerializeField] private float boundaryCheckDistance = 2f; // Distance to check for walls
    [SerializeField] private LayerMask obstacleLayer = 1 << 6; // Ground and obstacles

    [Header("Obstacle Avoidance - Steering Behaviors (Reactive)")]
    [SerializeField] private bool useSteeringBehaviors = true; // Enable/disable steering obstacle avoidance
    [SerializeField] private float steeringWeight = 1.0f; // How much steering affects movement (0-1 = blend, >1 = stronger)
    [SerializeField] private float obstacleAvoidDistance = 2.5f; // How far ahead to detect obstacles
    [SerializeField] private float avoidanceForce = 8f; // Strength of avoidance steering
    [SerializeField] private int rayCount = 5; // Number of detection rays (more = smoother but more expensive)
    [SerializeField] private float raySpreadAngle = 45f; // Angular spread of detection rays
    [SerializeField] private float maxSteeringForce = 15f; // Maximum steering force that can be applied
    [SerializeField] private bool showSteeringDebug = true; // Show debug rays and forces in scene view

    [Header("Platform Prediction (Proactive)")]
    [SerializeField] private bool usePlatformPrediction = true; // Enable/disable predictive platform avoidance
    [SerializeField] private float predictionWeight = 0.8f; // How much prediction affects movement (0-1)

    [Header("Health")]
    [SerializeField] private float maxHealth = 80f;

    // Events for UI integration
    public event Action<float, float> OnHealthChanged;
    public event Action OnDamageTaken;

    // Enemy states
    private enum EnemyState { Patrol, Chase, Attack, Dead }
    private EnemyState currentState = EnemyState.Patrol;

    // Patrol types
    public enum PatrolType { HorizontalHover, VerticalHover, Circle, Stationary }

    // Core components
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private SteeringBehaviors steering;
    private PlatformPredictor predictor;

    // Compatibility with existing systems
    public bool IsFacingRight => facingRight;
    public LayerMask PlayerLayer => playerLayer;

    // Movement state
    private int moveDirection = 1; // 1 = right, -1 = left
    private bool facingRight = true;
    private float waitTimer;
    private bool isWaiting;
    private Vector3 startPosition; // Starting position for patrol
    private float patrolTimer; // Timer for patrol movement
    private float hoverOffset; // Current hover offset

    // Combat state
    private float currentHealth;
    private float lastAttackTime;
    private float currentAttackCooldown;
    private float lastAttackEndTime; // When the last attack finished
    private const float RETREAT_COOLDOWN = 1.5f; // Can't retreat for this long after attacking

    // Detection state
    private GameObject player;
    private float playerLostTime; // When we last saw the player
    private bool playerLost = false; // Are we in the 5s delay period?

    // Anti-flickering: Dead zone for floating-point precision when vertically aligned
    private const float MINIMUM_DIRECTION_THRESHOLD = 0.1f;

    void Awake()
    {
        // Get components
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // Get or add steering behaviors component
        steering = GetComponent<SteeringBehaviors>();
        if (steering == null && useSteeringBehaviors)
        {
            steering = gameObject.AddComponent<SteeringBehaviors>();
        }

        // Configure steering behaviors with inspector parameters
        if (steering != null && useSteeringBehaviors)
        {
            steering.Configure(
                obstacleAvoidDistance,
                avoidanceForce,
                rayCount,
                raySpreadAngle,
                maxSteeringForce,
                showSteeringDebug,
                obstacleLayer
            );
        }

        // Get or add platform predictor component
        predictor = GetComponent<PlatformPredictor>();
        if (predictor == null && usePlatformPrediction)
        {
            predictor = gameObject.AddComponent<PlatformPredictor>();
        }

        // Initialize health
        currentHealth = maxHealth;

        // Initialize combat
        currentAttackCooldown = UnityEngine.Random.Range(attackCooldownMin, attackCooldownMax);

        // Set physics properties for flying enemy
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.gravityScale = 0f; // No gravity for flying enemies
            rb.mass = 5f; // Lighter than ground enemies
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth movement, prevent flickering
        }
    }

    void Start()
    {
        // CRITICAL: Set startPosition in Start() after Unity has positioned the object
        startPosition = transform.position;

        // Set initial facing direction
        facingRight = !spriteRenderer.flipX;
        moveDirection = facingRight ? 1 : -1;

        // Notify UI of initial health
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        if (currentState == EnemyState.Dead) return;

        DetectPlayer();
        UpdateState();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (currentState == EnemyState.Dead) return;

        HandleMovement();
    }

    #region Detection

    private void DetectPlayer()
    {
        // Circular detection (3D distance for flying enemies)
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);

        if (playerCollider != null)
        {
            GameObject detectedPlayer = playerCollider.gameObject;

            // Flying enemies can chase at any height
            player = detectedPlayer;
            playerLost = false;
            playerLostTime = 0f;

            if (currentState == EnemyState.Patrol)
            {
                // Trigger alert animation when first detecting player
                if (animator != null)
                {
                    animator.SetTrigger("Alert");
                }
                SwitchState(EnemyState.Chase);
            }
        }
        else
        {
            // Player not detected
            if ((currentState == EnemyState.Chase || currentState == EnemyState.Attack) && !playerLost)
            {
                // Just lost player - start the delay timer
                playerLost = true;
                playerLostTime = Time.time;
            }

            // Check if delay has passed since losing player
            if (playerLost && Time.time >= playerLostTime + chaseExitDelay)
            {
                // Return to patrol
                player = null;
                playerLost = false;
                playerLostTime = 0f;
                SwitchState(EnemyState.Patrol);
            }
        }
    }

    #endregion

    #region State Management

    private void SwitchState(EnemyState newState)
    {
        currentState = newState;

        // Reset waiting when switching states
        if (newState != EnemyState.Patrol)
        {
            isWaiting = false;
            waitTimer = 0f;
        }
        else if (newState == EnemyState.Patrol)
        {
            // When returning to patrol, reset patrol position
            startPosition = transform.position;
            patrolTimer = 0f;

            // Restore movement direction based on current facing
            if (moveDirection == 0)
            {
                moveDirection = facingRight ? 1 : -1;
            }

            isWaiting = false;
            waitTimer = 0f;
        }
    }

    private void UpdateState()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                HandlePatrol();
                break;

            case EnemyState.Chase:
                HandleChase();
                break;

            case EnemyState.Attack:
                HandleAttack();
                break;
        }
    }

    #endregion

    #region Patrol Behavior

    private void HandlePatrol()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                TurnAround();
                isWaiting = false;
            }
            return;
        }

        // Check boundaries based on patrol type
        switch (patrolType)
        {
            case PatrolType.HorizontalHover:
            case PatrolType.VerticalHover:
                if (ShouldStopAtBoundary())
                {
                    StartWaiting();
                }
                break;

            case PatrolType.Circle:
                // Circular patrol doesn't need boundary checks
                break;

            case PatrolType.Stationary:
                // Stationary just hovers in place
                moveDirection = 0;
                break;
        }
    }

    private bool ShouldStopAtBoundary()
    {
        bool shouldStop = false;

        if (patrolType == PatrolType.HorizontalHover)
        {
            // Check horizontal distance traveled
            float distanceTraveled = Mathf.Abs(transform.position.x - startPosition.x);
            if (distanceTraveled >= patrolDistance)
            {
                shouldStop = true;
            }

            // Check for walls ahead
            Vector2 checkDirection = Vector2.right * moveDirection;
            RaycastHit2D wallCheck = Physics2D.Raycast(transform.position, checkDirection, boundaryCheckDistance, obstacleLayer);
            if (wallCheck.collider != null)
            {
                shouldStop = true;
            }
        }
        else if (patrolType == PatrolType.VerticalHover)
        {
            // Check vertical distance traveled
            float distanceTraveled = Mathf.Abs(transform.position.y - startPosition.y);
            if (distanceTraveled >= patrolDistance)
            {
                shouldStop = true;
            }

            // Check max height boundary (minHeight is handled by ground avoidance now)
            if (transform.position.y >= maxHeight)
            {
                shouldStop = true;
            }
        }

        return shouldStop;
    }

    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = patrolWaitTime;
    }

    private void TurnAround()
    {
        moveDirection *= -1;
        facingRight = !facingRight;
        UpdateSpriteFlip();

        // Reset start position for new patrol direction
        startPosition = transform.position;
    }

    #endregion

    #region Chase Behavior

    private void HandleChase()
    {
        if (player == null)
        {
            SwitchState(EnemyState.Patrol);
            return;
        }

        // If we've lost the player but are in delay period, hover in place
        if (playerLost)
        {
            moveDirection = 0;
            rb.linearVelocity = Vector2.zero; // Stop moving during delay
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;

        // Always face the player
        if (Mathf.Abs(directionToPlayer.x) > MINIMUM_DIRECTION_THRESHOLD)
        {
            facingRight = directionToPlayer.x > 0;
            UpdateSpriteFlip();
        }

        // RANGED ENEMY DISTANCE-KEEPING BEHAVIOR
        // Logic: Approach until preferred distance, then hold ground unless player gets TOO close

        bool canRetreat = Time.time > lastAttackEndTime + RETREAT_COOLDOWN;

        // Player TOO close - retreat (only after cooldown)
        if (distanceToPlayer < tooCloseDistance && canRetreat)
        {
            // Retreat at slower speed than chase speed
            // This makes it possible for player to close distance
            moveDirection = -1; // Special marker for retreat in CalculateChaseVelocity

            // Still attack while retreating
            if (CanAttack())
            {
                PerformAttack();
            }
        }
        // Player within safe zone (between tooClose and preferred) - hold ground and attack
        else if (distanceToPlayer < preferredDistance)
        {
            // Don't retreat, don't advance - just gentle hover (not fleeing)
            moveDirection = 2; // Special marker for "hovering in place" mode

            if (CanAttack())
            {
                PerformAttack();
            }
        }
        // Player at/beyond preferred distance but within attack range - hold and attack
        else if (distanceToPlayer <= attackRange)
        {
            // At ideal range - gentle hover and attack
            moveDirection = 2; // Hovering mode

            if (CanAttack())
            {
                PerformAttack();
            }
        }
        // Player outside attack range - approach to preferred distance
        else
        {
            // Too far - approach until we reach preferred distance
            moveDirection = 1; // Approach player
        }
    }

    #endregion

    #region Attack Behavior

    private void HandleAttack()
    {
        // Attack state is handled by coroutines after PerformAttack()
        // Stop moving during attack
        moveDirection = 0;
        rb.linearVelocity = Vector2.zero;
    }

    private bool CanAttack()
    {
        return Time.time >= lastAttackTime + currentAttackCooldown;
    }

    private void PerformAttack()
    {
        if (currentState == EnemyState.Attack || isDead) return;

        SwitchState(EnemyState.Attack);
        lastAttackTime = Time.time;
        currentAttackCooldown = UnityEngine.Random.Range(attackCooldownMin, attackCooldownMax);

        // Stop movement during attack
        rb.linearVelocity = Vector2.zero;

        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Shoot projectile after a short delay
        StartCoroutine(ShootProjectile());
    }

    private IEnumerator ShootProjectile()
    {
        yield return new WaitForSeconds(0.3f); // Wind-up delay before shooting

        if (!isDead && player != null && projectilePrefab != null)
        {
            // Calculate spawn position (offset from enemy based on facing direction)
            Vector2 spawnOffset = new Vector2(
                projectileSpawnOffset.x * (facingRight ? 1 : -1),
                projectileSpawnOffset.y
            );
            Vector3 spawnPosition = transform.position + (Vector3)spawnOffset;

            // Calculate target position (aim at player's upper body/head, not feet)
            Vector3 targetPosition = player.transform.position + new Vector3(0, aimHeightOffset, 0);

            // Calculate direction to target
            Vector2 directionToPlayer = (targetPosition - spawnPosition).normalized;

            // Instantiate projectile
            GameObject projectileObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

            // Initialize projectile
            EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(directionToPlayer, projectileDamage);
            }
        }

        // Attack finished - wait for attack animation to complete
        yield return new WaitForSeconds(0.5f);

        // Check if enemy died during attack
        if (isDead) yield break;

        // Mark when attack ended (for retreat cooldown)
        lastAttackEndTime = Time.time;

        // Return to appropriate state
        if (currentState == EnemyState.Attack)
        {
            if (player != null && Vector2.Distance(transform.position, player.transform.position) <= detectionRange)
            {
                SwitchState(EnemyState.Chase);
            }
            else
            {
                SwitchState(EnemyState.Patrol);
            }
        }
    }

    #endregion

    #region Movement

    private void HandleMovement()
    {
        // Stop all movement during attacks
        if (currentState == EnemyState.Attack)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isWaiting || moveDirection == 0)
        {
            // Apply hovering motion even when waiting
            ApplyHoverMotion();
            return;
        }

        Vector2 velocity = Vector2.zero;

        switch (currentState)
        {
            case EnemyState.Patrol:
                velocity = CalculatePatrolVelocity();
                break;

            case EnemyState.Chase:
                velocity = CalculateChaseVelocity();
                break;
        }

        // PLATFORM PREDICTION (PROACTIVE): Avoid predicted platform positions
        if (usePlatformPrediction && predictor != null && predictor.ShouldAvoidPlatforms)
        {
            Vector2 predictiveOffset = predictor.SuggestedAvoidanceOffset * predictionWeight;
            velocity += predictiveOffset;
        }

        // STEERING OBSTACLE AVOIDANCE (REACTIVE): Avoid platforms and walls dynamically
        if (useSteeringBehaviors && steering != null)
        {
            Vector2 avoidanceForce = steering.AvoidObstacles(velocity);
            velocity += avoidanceForce * steeringWeight;
        }

        // GROUND AVOIDANCE: Check if too close to ground and add upward force
        velocity = ApplyGroundAvoidance(velocity);

        rb.linearVelocity = velocity;
    }

    private Vector2 CalculatePatrolVelocity()
    {
        Vector2 velocity = Vector2.zero;

        switch (patrolType)
        {
            case PatrolType.HorizontalHover:
                velocity.x = moveDirection * patrolSpeed;
                // Add hovering motion - calculate target Y position and move towards it
                float targetHoverY = startPosition.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
                float yDelta = targetHoverY - transform.position.y;
                velocity.y = Mathf.Clamp(yDelta * 3f, -patrolSpeed, patrolSpeed); // Smooth, clamped hover
                break;

            case PatrolType.VerticalHover:
                velocity.y = moveDirection * patrolSpeed;
                // Gentle horizontal sway - calculate target X and move towards it
                float targetHoverX = startPosition.x + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude * 0.5f;
                float xDelta = targetHoverX - transform.position.x;
                velocity.x = Mathf.Clamp(xDelta * 2f, -patrolSpeed * 0.5f, patrolSpeed * 0.5f);
                break;

            case PatrolType.Circle:
                // Circular movement around start position
                patrolTimer += Time.fixedDeltaTime * circleSpeed;
                float targetX = startPosition.x + Mathf.Cos(patrolTimer) * circleRadius;
                float targetY = startPosition.y + Mathf.Sin(patrolTimer) * circleRadius;

                // Calculate velocity to move towards target point
                Vector2 targetPos = new Vector2(targetX, targetY);
                Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
                velocity = direction * patrolSpeed * circleSpeed;

                // Update facing based on velocity direction with dead zone
                if (velocity.x > MINIMUM_DIRECTION_THRESHOLD)
                {
                    facingRight = true;
                    UpdateSpriteFlip();
                }
                else if (velocity.x < -MINIMUM_DIRECTION_THRESHOLD)
                {
                    facingRight = false;
                    UpdateSpriteFlip();
                }
                break;

            case PatrolType.Stationary:
                // Just hover in place
                float stationaryTargetY = startPosition.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
                float stationaryYDelta = stationaryTargetY - transform.position.y;
                velocity.y = Mathf.Clamp(stationaryYDelta * 3f, -1f, 1f); // Very gentle hover
                velocity.x = 0f;
                break;
        }

        return velocity;
    }

    private Vector2 CalculateChaseVelocity()
    {
        if (player == null) return Vector2.zero;

        Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;

        // moveDirection values:
        // -1 = retreat (move away from player)
        // 0 = stop completely
        // 1 = approach (move toward player)
        // 2 = gentle hover (drift around without fleeing)

        if (moveDirection == -1)
        {
            // Retreating - move AWAY from player at slower speed
            return -directionToPlayer * retreatSpeed;
        }
        else if (moveDirection == 0)
        {
            // Stop completely
            return Vector2.zero;
        }
        else if (moveDirection == 2)
        {
            // Gentle hovering - drift around without specific direction
            // Add subtle movement so enemy isn't a static target
            float hoverX = Mathf.Sin(Time.time * hoverFrequency * 0.7f) * patrolSpeed * 0.3f;
            float hoverY = Mathf.Cos(Time.time * hoverFrequency * 0.5f) * patrolSpeed * 0.3f;
            return new Vector2(hoverX, hoverY);
        }
        else
        {
            // Approaching - move toward player at chase speed
            return directionToPlayer * chaseSpeed;
        }
    }

    private void ApplyHoverMotion()
    {
        // Gentle hovering motion when stationary (used when waiting)
        float targetY = startPosition.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        float yDelta = targetY - transform.position.y;
        float yVelocity = Mathf.Clamp(yDelta * 2f, -1f, 1f); // Clamped for stability

        Vector2 velocity = new Vector2(0, yVelocity);

        // Apply platform prediction even while hovering
        if (usePlatformPrediction && predictor != null && predictor.ShouldAvoidPlatforms)
        {
            Vector2 predictiveOffset = predictor.SuggestedAvoidanceOffset * predictionWeight * 0.7f; // Reduced strength while hovering
            velocity += predictiveOffset;
        }

        // Apply steering obstacle avoidance even while hovering
        if (useSteeringBehaviors && steering != null)
        {
            Vector2 avoidanceForce = steering.AvoidObstacles(velocity);
            velocity += avoidanceForce * steeringWeight * 0.5f; // Reduced strength while hovering
        }

        // Apply ground avoidance to hover motion too
        velocity = ApplyGroundAvoidance(velocity);

        rb.linearVelocity = velocity;
    }

    private Vector2 ApplyGroundAvoidance(Vector2 currentVelocity)
    {
        // Check for ground below enemy
        RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, obstacleLayer);

        if (groundHit.collider != null)
        {
            float distanceToGround = groundHit.distance;

            // Too close to ground - add upward force
            if (distanceToGround < minHeightAboveGround)
            {
                // Calculate how much we need to rise
                float heightDeficit = minHeightAboveGround - distanceToGround;
                float upwardForce = Mathf.Clamp(heightDeficit * 2f, 0f, chaseSpeed);

                // Override downward velocity with upward correction
                if (currentVelocity.y < upwardForce)
                {
                    currentVelocity.y = upwardForce;
                }
            }
        }

        return currentVelocity;
    }

    private void UpdateSpriteFlip()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }
    }

    #endregion

    #region Health & Combat

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        OnDamageTaken?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Hit reaction
            if (animator != null)
            {
                animator.SetTrigger("Hit");
            }
        }
    }

    private bool isDead = false;

    private void Die()
    {
        isDead = true;
        currentState = EnemyState.Dead;

        // Stop movement
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        // Disable collider
        if (col != null)
        {
            col.enabled = false;
        }

        // Play death animation
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        // Destroy after delay
        StartCoroutine(DestroyAfterDelay(2f));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    #endregion

    #region Animation

    private void UpdateAnimations()
    {
        if (animator == null || isDead) return;

        bool shouldBeMoving;
        bool isAttackingState = currentState == EnemyState.Attack;
        bool isChasingState = currentState == EnemyState.Chase;

        if (isAttackingState)
        {
            shouldBeMoving = false; // No movement animation during attack
        }
        else
        {
            // Use velocity for movement detection
            shouldBeMoving = rb.linearVelocity.magnitude > 0.1f;
        }

        // Set animation parameters (same names as ground enemy for consistency)
        animator.SetBool("IsMoving", shouldBeMoving);
        animator.SetFloat("VelocityY", rb.linearVelocity.y);

        // Flying enemy specific animations
        animator.SetBool("IsChasing", isChasingState && !isAttackingState);
        animator.SetBool("IsAttacking", isAttackingState);
    }

    #endregion

    #region Public Interface

    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead => isDead;
    public bool IsChasing => currentState == EnemyState.Chase;
    public bool IsAttacking => currentState == EnemyState.Attack;

    #endregion

    #region Debug Visualization

    void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, detectionRange);

        // Attack range (outer)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position, attackRange);

        // Preferred distance (middle)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(position, preferredDistance);

        // Too close distance (inner)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(position, tooCloseDistance);

        // Patrol visualization
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;

            switch (patrolType)
            {
                case PatrolType.HorizontalHover:
                    Gizmos.DrawLine(startPosition - Vector3.right * patrolDistance, startPosition + Vector3.right * patrolDistance);
                    break;

                case PatrolType.VerticalHover:
                    Gizmos.DrawLine(startPosition - Vector3.up * patrolDistance, startPosition + Vector3.up * patrolDistance);
                    break;

                case PatrolType.Circle:
                    DrawCircle(startPosition, circleRadius, Color.cyan);
                    break;

                case PatrolType.Stationary:
                    Gizmos.DrawWireSphere(startPosition, 0.5f);
                    break;
            }

            // Boundary check visualization
            if (patrolType == PatrolType.HorizontalHover)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(position, Vector2.right * moveDirection * boundaryCheckDistance);
            }

            // Projectile spawn point visualization
            if (projectilePrefab != null)
            {
                Gizmos.color = Color.cyan;
                Vector2 spawnOffset = new Vector2(
                    projectileSpawnOffset.x * (facingRight ? 1 : -1),
                    projectileSpawnOffset.y
                );
                Vector3 spawnPos = position + (Vector3)spawnOffset;
                Gizmos.DrawWireSphere(spawnPos, 0.15f);

                // Show aiming line in Chase/Attack state
                if (Application.isPlaying && player != null &&
                    (currentState == EnemyState.Chase || currentState == EnemyState.Attack))
                {
                    Vector3 targetPos = player.transform.position + new Vector3(0, aimHeightOffset, 0);
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(spawnPos, targetPos);
                    Gizmos.DrawWireSphere(targetPos, 0.2f); // Show aim point
                }
            }

            // Ground avoidance visualization
            Gizmos.color = Color.blue;
            RaycastHit2D groundHit = Physics2D.Raycast(position, Vector2.down, groundCheckDistance, obstacleLayer);
            if (groundHit.collider != null)
            {
                // Draw line to ground
                Gizmos.DrawLine(position, (Vector3)groundHit.point);

                // Draw minimum height threshold
                Gizmos.color = Color.yellow;
                Vector3 minHeightLine = (Vector3)groundHit.point + Vector3.up * minHeightAboveGround;
                Gizmos.DrawLine(minHeightLine - Vector3.right * 0.5f, minHeightLine + Vector3.right * 0.5f);

                // Highlight if too close to ground
                if (groundHit.distance < minHeightAboveGround)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(position, 0.3f);
                }
            }
        }

        // Current state indicator
        Gizmos.color = currentState switch
        {
            EnemyState.Patrol => Color.green,
            EnemyState.Chase => Color.orange,
            EnemyState.Attack => Color.red,
            EnemyState.Dead => Color.gray,
            _ => Color.white
        };
        Gizmos.DrawWireCube(position + Vector3.up * 1f, Vector3.one * 0.2f);

        // Player lost indicator
        if (Application.isPlaying && playerLost)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(position + Vector3.up * 1.5f, Vector3.one * 0.1f);
        }
    }

    private void DrawCircle(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        int segments = 32;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);

            Gizmos.DrawLine(point1, point2);
        }
    }

    #endregion

    #region Editor Utilities

    // Called when values are changed in the Inspector (Edit Mode or Play Mode)
    private void OnValidate()
    {
        // Only reconfigure steering if we're in play mode and steering is active
        if (Application.isPlaying && steering != null && useSteeringBehaviors)
        {
            steering.Configure(
                obstacleAvoidDistance,
                avoidanceForce,
                rayCount,
                raySpreadAngle,
                maxSteeringForce,
                showSteeringDebug,
                obstacleLayer
            );
        }
    }

    #endregion
}
