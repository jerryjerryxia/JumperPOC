using System;
using System.Collections;
using UnityEngine;
using Enemies;
using Player;

/// <summary>
/// Simplified, elegant enemy system with clear state machine behavior.
/// Handles patrol, chase, and attack with platform constraints.
/// Compatible with existing head stomp and hitbox systems.
/// </summary>
public class SimpleEnemy : MonoBehaviour, IEnemyBase
    {
        [Header("Movement")]
        [SerializeField] private float patrolSpeed = 1f;
        [SerializeField] private float chaseSpeed = 2f;
        [SerializeField] private float patrolWaitTime = 2f;
        
        [Header("Detection")]
        [SerializeField] private float detectionRange = 5f;
        [SerializeField] private float chaseExitDelay = 5f; // Time before returning to patrol after losing player
        [SerializeField] public LayerMask playerLayer = 1 << 0; // Public for head stomp compatibility
        
        [Header("Combat")]
        [SerializeField] private float attackRange = 0.5f;
        [SerializeField] private float attackCooldownMin = 3f;
        [SerializeField] private float attackCooldownMax = 5f;
        [SerializeField] private float attackDamage = 10f;
        
        [Header("Platform Constraints")]
        [SerializeField] private float edgeCheckDistance = 1f;
        [SerializeField] private float minEdgeOffset = 0.1f;
        [SerializeField] private float maxEdgeOffset = 0.8f;
        [SerializeField] private LayerMask groundLayer = 1 << 6;
        
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        
        // Events for UI integration
        public event Action<float, float> OnHealthChanged;
        public event Action OnDamageTaken;
        
        // Enemy states
        private enum EnemyState { Patrol, Chase, Attack, Dead }
        private EnemyState currentState = EnemyState.Patrol;
        
        // Core components
        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private Collider2D col;
        private Enemy1Hitbox hitbox;
        
        // Compatibility with existing systems
        public bool IsFacingRight => facingRight;
        public LayerMask PlayerLayer => playerLayer;
        
        // Movement state
        private int moveDirection = 1; // 1 = right, -1 = left
        private bool facingRight = true;
        private float currentEdgeOffset;
        private float waitTimer;
        private bool isWaiting;
        
        // Combat state
        private float currentHealth;
        private float lastAttackTime;
        private float currentAttackCooldown;
        
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
            hitbox = GetComponentInChildren<Enemy1Hitbox>();
            
            // Initialize health
            currentHealth = maxHealth;
            
            // Initialize movement
            GenerateRandomEdgeOffset();
            
            
            // Initialize combat
            currentAttackCooldown = UnityEngine.Random.Range(attackCooldownMin, attackCooldownMax);
            
            // Set physics properties
            if (rb != null)
            {
                rb.freezeRotation = true;
                rb.mass = 10f; // Heavy enough to not be pushed by player
            }
        }
        
        void Start()
        {
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
            // Simple circular detection  
            Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
            
            if (playerCollider != null)
            {
                GameObject detectedPlayer = playerCollider.gameObject;
                
                // Basic platform check - only chase if player is roughly at same height
                float heightDifference = Mathf.Abs(detectedPlayer.transform.position.y - transform.position.y);
                if (heightDifference < 3f) // Reasonable height difference
                {
                    // Player found - reset lost state
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
            }
            else
            {
                // Player not detected
                if ((currentState == EnemyState.Chase || currentState == EnemyState.Attack) && !playerLost)
                {
                    // Just lost player - start the 5 second timer
                    playerLost = true;
                    playerLostTime = Time.time;
                }
                
                // Check if 5 seconds have passed since losing player
                if (playerLost && Time.time >= playerLostTime + chaseExitDelay)
                {
                    // 5 seconds passed - return to patrol
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
            
            // Check if we should stop before edge
            if (ShouldStopAtEdge())
            {
                StartWaiting();
            }
        }
        
        private bool ShouldStopAtEdge()
        {
            // Check for ground at offset distance in movement direction
            Vector2 checkPosition = transform.position + Vector3.right * moveDirection * currentEdgeOffset;
            RaycastHit2D groundCheck = Physics2D.Raycast(checkPosition, Vector2.down, edgeCheckDistance, groundLayer);
            
            return groundCheck.collider == null; // No ground = edge detected
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
            GenerateRandomEdgeOffset();
        }
        
        private void GenerateRandomEdgeOffset()
        {
            currentEdgeOffset = UnityEngine.Random.Range(minEdgeOffset, maxEdgeOffset);
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
            
            // If we've lost the player but are in delay period, stop moving but don't exit chase yet
            if (playerLost)
            {
                moveDirection = 0; // Stop moving during the 5s delay
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // FORCE stop
                return;
            }
            
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
            
            // Check if in attack range - USE EXACT SAME LOGIC AS WORKING SYSTEM
            if (distanceToPlayer <= attackRange)
            {
                // Within attack range - stop moving and face player
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                
                // Only update facing if horizontal direction is significant (prevents floating-point flickering)
                if (Mathf.Abs(directionToPlayer.x) > MINIMUM_DIRECTION_THRESHOLD)
                {
                    facingRight = directionToPlayer.x > 0;
                    UpdateSpriteFlip();
                }
                moveDirection = 0;
                
                if (CanAttack())
                {
                    PerformAttack(); // Use different method name to avoid confusion
                }
                return;
            }
            
            // Check if chasing would cause us to leave the platform - EXACT SAME LOGIC
            int chaseDirection = directionToPlayer.x > 0 ? 1 : -1;
            if (!CanMoveInDirection(chaseDirection))
            {
                // Can't chase in that direction without falling off platform
                // Stop movement but DON'T update facing to prevent flickering
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                moveDirection = 0;
                // Note: Removed facing update here to prevent flickering when stuck at edges
                return;
            }
            
            // Safe to move toward player - use dead zone for facing to prevent flickering
            moveDirection = chaseDirection;
            // Only update facing if horizontal direction is significant (prevents floating-point flickering)
            if (Mathf.Abs(directionToPlayer.x) > MINIMUM_DIRECTION_THRESHOLD)
            {
                facingRight = directionToPlayer.x > 0;
                UpdateSpriteFlip();
            }
        }
        
        private bool CanMoveInDirection(int direction)
        {
            // EXACT SAME LOGIC as working Enemy1Controller
            float chaseEdgeOffset = 0.3f; // Smaller offset for more aggressive chasing
            float chaseEdgeDistance = 1f; // Smaller distance - only check 1 unit down instead of 2
            Vector2 edgeCheckPosition = (Vector2)transform.position + new Vector2(chaseEdgeOffset * direction, 0f);
            
            // Only check Ground layer (layer 6) - exclude player and other layers
            int groundLayerOnly = 1 << 6;
            RaycastHit2D groundCheck = Physics2D.Raycast(edgeCheckPosition, Vector2.down, chaseEdgeDistance, groundLayerOnly);
            bool wouldReachEdge = !groundCheck.collider;
            
            // Check for wall in the direction we want to move - only check Ground layer
            Vector2 wallCheckPosition = (Vector2)transform.position;
            RaycastHit2D wallCheck = Physics2D.Raycast(wallCheckPosition, Vector2.right * direction, 0.5f, groundLayerOnly);
            bool wouldHitWall = wallCheck.collider != null;
            
            return !wouldReachEdge && !wouldHitWall;
        }
        
        #endregion
        
        #region Attack Behavior
        
        private void HandleAttack()
        {
            // Attack state is handled entirely by coroutines after PerformAttack() is called
            // This method should do NOTHING - the attack plays out via coroutines
            // The attack ends automatically in EndHitboxAttackAfterDelay()
            
            // Just ensure we're not moving during attack
            moveDirection = 0;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        
        private bool CanAttack()
        {
            return Time.time >= lastAttackTime + currentAttackCooldown;
        }
        
        private void PerformAttack()
        {
            if (currentState == EnemyState.Attack || isDead) return;
            
            // EXACT SAME LOGIC as working Enemy1Controller Attack() method
            SwitchState(EnemyState.Attack);
            lastAttackTime = Time.time;
            currentAttackCooldown = UnityEngine.Random.Range(attackCooldownMin, attackCooldownMax);
            
            // Stop movement during attack - CRITICAL
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
            
            if (!isDead && hitbox != null)
            {
                hitbox.StartLanceChopAttack();
                
                // End the hitbox attack after a duration (lance chop duration)
                StartCoroutine(EndHitboxAttackAfterDelay(0.4f)); // Hitbox active for 0.4 seconds
            }
            else if (!isDead)
            {
                // Fallback damage if no hitbox
                StartCoroutine(DealDirectDamage());
            }
        }
        
        private IEnumerator EndHitboxAttackAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Check if enemy died during the attack - if so, don't continue
            if (isDead) yield break;
            
            if (hitbox != null)
            {
                hitbox.EndLanceChopAttack();
            }
            
            // Attack finished - CRITICAL: This is what ends the attack loop!
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
        
        
        private IEnumerator DealDirectDamage()
        {
            yield return new WaitForSeconds(0.5f); // Attack delay
            
            if (!isDead && player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
                if (distanceToPlayer <= attackRange)
                {
                    PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                    if (playerHealth != null && !playerHealth.IsInvincible)
                    {
                        playerHealth.TakeDamage(attackDamage);
                    }
                }
            }
        }
        
        #endregion
        
        #region Movement
        
        private void HandleMovement()
        {
            // CRITICAL: Stop ALL movement during attacks like working system
            if (currentState == EnemyState.Attack)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                return;
            }
            
            if (isWaiting || moveDirection == 0)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                return;
            }
            
            float speed = currentState == EnemyState.Chase ? chaseSpeed : patrolSpeed;
            rb.linearVelocity = new Vector2(moveDirection * speed, rb.linearVelocity.y);
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
            
            // Disable main collider but keep hitboxes for head stomp
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
            
            // CRITICAL: Use EXACT same logic as working Enemy1Controller
            bool shouldBeMoving;
            bool isAttackingState = currentState == EnemyState.Attack;
            bool isChasingState = currentState == EnemyState.Chase;
            
            if (isAttackingState)
            {
                shouldBeMoving = false; // Force no movement animation during attack
            }
            else if (isChasingState && !isAttackingState)
            {
                // During chase, use velocity for movement detection
                shouldBeMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            }
            else
            {
                // Normal patrol movement
                shouldBeMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
            }
            
            // Set animation parameters using EXACT same names as working system
            animator.SetBool("IsMoving", shouldBeMoving);
            animator.SetBool("IsGrounded", IsGrounded());
            animator.SetFloat("VelocityY", rb.linearVelocity.y);
            
            // Enemy specific animations - same logic as working system
            animator.SetBool("IsChasing", isChasingState && !isAttackingState);
            animator.SetBool("IsAttacking", isAttackingState);
        }
        
        private bool IsGrounded()
        {
            Vector2 position = transform.position;
            RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, 0.2f, groundLayer);
            return hit.collider != null;
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
            
            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(position, attackRange);
            
            // Edge detection during play
            if (Application.isPlaying)
            {
                // Current edge detection for patrol
                Gizmos.color = Color.blue;
                Vector2 edgeCheckPos = position + Vector3.right * moveDirection * currentEdgeOffset;
                Gizmos.DrawWireSphere(edgeCheckPos, 0.1f);
                Gizmos.DrawRay(edgeCheckPos, Vector2.down * edgeCheckDistance);
                
                // Chase edge detection
                if (currentState == EnemyState.Chase && moveDirection != 0)
                {
                    Gizmos.color = Color.cyan;
                    Vector2 chaseEdgePos = position + Vector3.right * moveDirection * 0.3f;
                    Gizmos.DrawWireSphere(chaseEdgePos, 0.05f);
                    Gizmos.DrawRay(chaseEdgePos, Vector2.down * 1f);
                }
                
                // Attack safety check
                if (currentState == EnemyState.Attack && player != null)
                {
                    Vector2 playerDirection = (player.transform.position - transform.position).normalized;
                    int attackDirection = playerDirection.x > 0 ? 1 : -1;
                    Gizmos.color = Color.magenta;
                    Vector2 attackEdgePos = position + Vector3.right * attackDirection * 0.2f;
                    Gizmos.DrawWireSphere(attackEdgePos, 0.05f);
                    Gizmos.DrawRay(attackEdgePos, Vector2.down * 1f);
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
        
        #endregion
    }