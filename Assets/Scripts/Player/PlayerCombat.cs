using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack")]
    public float airAttackDuration = 0.25f;
    public float attackDuration = 0.3f;
    public float attackMovementSpeed = 0.05f;
    public float comboWindowTime = 0.5f;
    public float inputBufferTime = 0.2f;

    [Header("Dash Attack")]
    public float dashAttackDuration = 0.25f;
    public float dashAttackSpeed = 2f;
    public float dashAttackInputWindow = 0.2f;
    public float dashAttackPreWindow = 0.1f;
    
    [Header("References")]
    public AttackHitbox AttackHitbox;
    
    // Component references
    private PlayerController playerController;
    private Rigidbody2D rb;
    private Animator animator;
    
    // Combat state
    private bool isAirAttacking = false;
    private float originalGravityScale;
    private int airAttacksUsed = 0;
    private int maxAirAttacks = 2;
    private bool canUseSecondAirAttack = false;
    private int attackCombo = 0;
    private bool isAttacking;
    private bool isDashAttacking = false;
    private bool isAirDashAttacking = false;
    private float attackTimer = 0f;
    private float comboWindowTimer = 0f;
    private float inputBufferTimer = 0f;
    private bool attackInputBuffered = false;
    private bool canCombo = false;
    private bool waitingForNextAttack = false;
    private Vector2 dashAttackDirection;
    private bool dashAttackQueued = false;
    private float dashEndTime = 0f;
    private float dashStartTime = 0f;
    private bool allowDashAttack = true;
    private bool dashAttackConsumed = false;
    private int attackInputCount = 0;
    private int attackBufferCount = 0;
    private float lastAttackTime = 0f;
    private bool isDuringDash = false; // Track actual dash state
    
    // Public properties
    public bool IsAttacking => isAttacking;
    public bool IsAirAttacking => isAirAttacking;
    public bool IsDashAttacking => isDashAttacking;
    public int AttackCombo => attackCombo;
    public bool HasUsedAirAttack => 
        (airAttacksUsed >= 1 && !canUseSecondAirAttack) || // Used first attack, no double jump yet
        (airAttacksUsed >= 2); // Used both attacks
    public int AirAttacksUsed => airAttacksUsed;
    
    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        // PlayerController may be disabled in new component system, that's OK
        if (playerController == null)
        {
            // Debug.LogWarning("PlayerCombat: PlayerController not found - some features may be limited");
        }
        
        originalGravityScale = rb.gravityScale;
    }
    
    void OnEnable()
    {
        // Input is handled by PlayerController, which calls our HandleAttackInput() method
        // No direct subscription needed to prevent double input handling
    }
    
    void OnDisable()
    {
        // No input subscription to clean up
    }
    
    void Update()
    {
        UpdateCombatTimers();
    }
    
    void UpdateCombatTimers()
    {
        // DESIGN: Attacks end when their timer expires - quick, snappy feel
        // Timer is set by airAttackDuration or dashAttackDuration parameters

        // Update attack timers
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;

            // End attacks when timer expires
            if (attackTimer <= 0)
            {
                if (isAirAttacking || isDashAttacking)
                {
                    ResetAttackSystem();
                    return;
                }
            }
        }
        
        // Update combo window
        if (comboWindowTimer > 0)
        {
            comboWindowTimer -= Time.deltaTime;
            if (comboWindowTimer <= 0)
            {
                canCombo = false;
            }
        }
        
        // Update input buffer
        if (inputBufferTimer > 0)
        {
            inputBufferTimer -= Time.deltaTime;
            if (inputBufferTimer <= 0)
            {
                attackInputBuffered = false;
            }
        }
    }
    
    public void HandleAttackInput()
    {
        attackInputCount++;
        // Debug.Log($"ATTACK INPUT #{attackInputCount} - States: isDashing={playerController.IsDashing}, isAttacking={isAttacking}, dashEndTime={dashEndTime}, timeSinceDash={Time.time - dashEndTime:F2}");
        
        // Check if we're dashing - queue dash attack
        if (isDuringDash && allowDashAttack && !dashAttackConsumed)
        {
            float timeSinceDashStart = Time.time - dashStartTime;
            // Debug.Log($"Dash timing check - timeSinceDashStart={timeSinceDashStart:F3}s, preWindow={dashAttackPreWindow}");
            if (timeSinceDashStart >= dashAttackPreWindow)
            {
                dashAttackQueued = true;
                // Debug.Log("Dash attack queued during dash");
                return;
            }
            else
            {
                // Debug.Log("Dash attack NOT queued - too early in dash");
            }
        }
        
        // Check for dash attack in grace period after dash - prevent when actively wall sticking/sliding
        // Allow dash attack when jumping from wall (OnWall may still be true but not in wall state)
        bool actuallyOnWall = playerController.OnWall && (playerController.IsWallSticking || playerController.IsWallSliding);
        if (!actuallyOnWall && !isDuringDash && !isAttacking && dashEndTime > 0 && Time.time - dashEndTime <= dashAttackInputWindow && !dashAttackConsumed &&
            PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasDashAttack)
        {
            if (allowDashAttack)
            {
                // Debug.Log("Dash attack triggered in grace period");
                StartDashAttack();
                dashAttackConsumed = true;
                return;
            }
            else
            {
                // Debug.Log("Dash attack blocked - allowDashAttack is false");
                dashAttackConsumed = true;
            }
        }
        
        // Check for air attack - prevent attack when actively wall sticking/sliding
        // Allow air attack when jumping from wall (OnWall may still be true but not in wall state)
        actuallyOnWall = playerController.OnWall && (playerController.IsWallSticking || playerController.IsWallSliding);
        if (!actuallyOnWall && !playerController.IsGrounded && !isAttacking && !isDashAttacking && !playerController.IsDashing && !HasUsedAirAttack &&
            PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasAirAttack)
        {
            StartAirAttack();
            return;
        }
        
        // Check if we can execute the attack immediately
        if (!isAttacking && !isDashAttacking && !playerController.IsDashing)
        {
            if (dashEndTime > 0 && Time.time - dashEndTime > dashAttackInputWindow)
            {
                dashEndTime = 0f;
            }
            
            if (playerController.IsGrounded && dashEndTime == 0)
            {
                // Debug.Log("Attack executing immediately - not buffered");
                StartAttackCombo();
                return;
            }
        }
        
        // Check if we can do a combo attack
        if (isAttacking && !isDashAttacking && !isAirAttacking && 
            PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasComboAttack)
        {
            if (canCombo && !waitingForNextAttack)
            {
                // Debug.Log("Combo attack executing immediately - not buffered");
                PerformNextCombo();
                return;
            }
        }
        
        // Buffer the input if we couldn't execute immediately
        attackInputBuffered = true;
        inputBufferTimer = inputBufferTime;
        attackBufferCount++;
        // Debug.Log($"Attack input buffered #{attackBufferCount} - inputBufferTimer set to {inputBufferTime}");
    }
    
    public void OnDashStart()
    {
        isDuringDash = true;
        allowDashAttack = true;
        dashStartTime = Time.time;
        dashAttackConsumed = false;
        dashAttackQueued = false;
        
        // If we're already attacking, cancel it and don't allow dash attack
        if (isAttacking && !isDashAttacking)
        {
            allowDashAttack = false;
            ResetAttackSystem();
        }
        
        // Debug.Log($"OnDashStart - isDuringDash={isDuringDash}, allowDashAttack={allowDashAttack}");
    }
    
    public void OnDashEnd()
    {
        isDuringDash = false;
        dashEndTime = Time.time;
        // Debug.Log($"OnDashEnd - isDuringDash={isDuringDash}, dashAttackQueued={dashAttackQueued}, allowDashAttack={allowDashAttack}, isDashAttacking={isDashAttacking}");
        
        // Execute queued dash attack if we have one - prevent when on wall
        if (!playerController.OnWall && dashAttackQueued && allowDashAttack && !dashAttackConsumed)
        {
            // Debug.Log("Executing queued dash attack on dash end");
            dashAttackConsumed = true;
            StartDashAttack();
        }
        else if (playerController.OnWall && dashAttackQueued)
        {
            // Debug.Log("Dash attack blocked - player is on wall");
            dashAttackQueued = false;
            dashAttackConsumed = true;
        }
        else if (isDashAttacking && attackTimer <= 0)
        {
            // If we were dash attacking but timer is zero, reset
            // Debug.Log("Dash ended, but dash attack state still true with no timer - resetting");
            ResetAttackSystem();
        }
    }
    
    public void CheckBufferedDashAttack()
    {
        // Check if we have a buffered attack input that should become a dash attack - prevent when on wall
        if (!playerController.OnWall && !isDuringDash && !isAttacking && dashEndTime > 0 && Time.time - dashEndTime <= dashAttackInputWindow && allowDashAttack && !dashAttackConsumed &&
            PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasDashAttack)
        {
            if (attackInputBuffered && inputBufferTimer > 0)
            {
                // Debug.Log("Converting buffered attack to dash attack");
                dashAttackConsumed = true;
                StartDashAttack();
                attackInputBuffered = false;
                inputBufferTimer = 0f;
                dashEndTime = 0f;
            }
        }
        
        // Clear dash end time after window expires
        if (dashEndTime > 0 && Time.time - dashEndTime > dashAttackInputWindow)
        {
            dashEndTime = 0f;
            dashAttackQueued = false;
            allowDashAttack = true;
            dashAttackConsumed = false;
            // Debug.Log("Dash attack window expired - resetting dash attack state");
        }
    }
    
    public void OnLanding()
    {
        // Reset all attacks on landing unless timer is still active
        bool attackStillActive = (isAirAttacking || isDashAttacking) && attackTimer > 0;

        if (!attackStillActive)
        {
            ResetAttackSystem();
        }

        // Always reset air attack counters on actual landing
        airAttacksUsed = 0;
        canUseSecondAirAttack = false;
    }
    
    public void OnDoubleJump()
    {
        // Enable second air attack after double jump
        canUseSecondAirAttack = true;
        // Debug.Log($"Double jump performed - second air attack now enabled. AirAttacksUsed: {airAttacksUsed}");
    }
    
    public Vector2 GetAttackMovement()
    {
        // DESIGN CHANGE: No movement interruption for any attacks
        // Let normal movement system handle everything - attacks don't override player control
        return Vector2.zero;
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
            animator.Update(0);
        }
        
        if (AttackHitbox != null)
        {
            AttackHitbox.SetAttackType(AttackHitbox.AttackType.GroundStab);
            AttackHitbox.SetActive(true);
        }
        
        Invoke(nameof(EnableComboWindow), attackDuration * 0.5f);
    }
    
    private void StartDashAttack()
    {
        isAirDashAttacking = !playerController.IsGrounded;
        
        // Debug.Log($"StartDashAttack called - isAirDashAttacking: {isAirDashAttacking}");
        
        if (isAirDashAttacking)
        {
            // Check if air attack limit is reached
            if (airAttacksUsed >= maxAirAttacks)
            {
                // Debug.Log("Air dash attack BLOCKED - air attack limit reached");
                return;
            }
            
            // Air dash attacks should consume air attack resources to limit total air attacks to 2
            airAttacksUsed++;
        }
        
        isDashAttacking = true;
        isAttacking = true;
        dashAttackQueued = false;
        attackTimer = dashAttackDuration;
        canCombo = false;
        attackInputBuffered = false;
        waitingForNextAttack = false;
        inputBufferTimer = 0f;
        comboWindowTimer = 0f;
        lastAttackTime = Time.time;

        dashAttackDirection = playerController.FacingRight ? Vector2.right : Vector2.left;
        dashEndTime = 0f;

        // DESIGN CHANGE: No movement interruption for dash attacks
        // Preserve all velocity - player continues their current movement
        // No gravity modification, no velocity changes

        if (animator != null)
        {
            animator.SetBool("IsDashAttacking", true);
            animator.SetBool("IsAttacking", true);
            animator.SetTrigger("Attack");
            animator.Update(0);
            
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (!currentState.IsName("DashAttack"))
            {
                animator.CrossFade("DashAttack", 0.1f, 0);
            }
        }
        
        if (AttackHitbox != null)
        {
            AttackHitbox.SetAttackType(AttackHitbox.AttackType.DashAttack);
            AttackHitbox.SetActive(true);
        }
        
        InvokeRepeating(nameof(CheckDashAttackStuck), 0.5f, 0.1f);
    }
    
    private void StartAirAttack()
    {
        isAirAttacking = true;
        isAttacking = true;
        airAttacksUsed++;
        attackTimer = airAttackDuration;
        canCombo = false;
        attackInputBuffered = false;
        waitingForNextAttack = false;
        lastAttackTime = Time.time;

        // DESIGN: Quick, snappy air attack with fixed minimum duration
        // NO movement interruption - preserve existing horizontal velocity
        // Gravity stays enabled - player falls naturally
        // Don't modify velocity at all - player continues their current movement

        if (animator != null)
        {
            animator.SetBool("IsAirAttacking", true);
            animator.SetBool("IsAttacking", true);
            animator.CrossFade("PlayerAirSwordSwing", 0.05f, 0);
        }

        if (AttackHitbox != null)
        {
            AttackHitbox.SetAttackType(AttackHitbox.AttackType.AirAttack);
            AttackHitbox.SetActive(true);
        }
    }
    
    private void PerformNextCombo()
    {
        waitingForNextAttack = true;
        canCombo = false;
        attackInputBuffered = false;
        
        if (attackCombo >= 3)
        {
            attackCombo = 1;
            if (animator != null)
            {
                animator.SetBool("IsAttacking", false);
                animator.SetInteger("AttackCombo", 0);
                Invoke(nameof(RestartComboLoop), 0.05f);
            }
        }
        else
        {
            attackCombo++;
            if (animator != null)
            {
                animator.SetInteger("AttackCombo", attackCombo);
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
    
    private void EnableComboWindow()
    {
        if (isAttacking && PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasComboAttack)
        {
            canCombo = true;
            comboWindowTimer = comboWindowTime;
            
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
            AttackHitbox.SetActive(false);
        }
        
        if (attackInputBuffered && !waitingForNextAttack && 
            PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasComboAttack)
        {
            PerformNextCombo();
        }
        else if (!waitingForNextAttack)
        {
            ResetAttackSystem();
        }
    }
    
    public void ResetAttackSystem()
    {
        isAttacking = false;
        isDashAttacking = false;
        isAirAttacking = false;
        isAirDashAttacking = false;
        dashAttackQueued = false;
        dashEndTime = 0f;
        attackCombo = 0;
        canCombo = false;
        attackInputBuffered = false;
        waitingForNextAttack = false;
        attackTimer = 0f;
        comboWindowTimer = 0f;
        inputBufferTimer = 0f;
        isDuringDash = false;

        CancelInvoke(nameof(EnableComboWindow));
        CancelInvoke(nameof(RestartComboLoop));
        CancelInvoke(nameof(CheckDashAttackStuck));

        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsDashAttacking", false);
            animator.SetBool("IsAirAttacking", false);
            animator.SetInteger("AttackCombo", 0);
            animator.Update(0);

            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

            // Force-transition combo attacks to idle immediately (always grounded)
            if (currentState.IsName("PlayerSwordStab") ||
                currentState.IsName("PlayerSwordChop") ||
                currentState.IsName("PlayerSwordSwing3"))
            {
                animator.CrossFade("DaggerIdle", 0f, 0);
            }
            // Force-transition air/dash attacks to idle ONLY if grounded for snappy feel
            // If airborne, let animator naturally transition to jump/fall based on velocity
            else if (currentState.IsName("PlayerAirSwordSwing") || currentState.IsName("DashAttack"))
            {
                bool isGrounded = playerController != null && playerController.IsGrounded;
                if (isGrounded)
                {
                    animator.CrossFade("DaggerIdle", 0f, 0);
                }
                // If airborne, animator will transition naturally based on IsJumping/IsFalling parameters
            }
        }

        if (AttackHitbox != null)
        {
            AttackHitbox.SetActive(false);
        }
    }
    
    private void CheckDashAttackStuck()
    {
        if (isDashAttacking && Time.time - lastAttackTime > dashAttackDuration * 2f)
        {
            ResetAttackSystem();
        }
    }
    
    // Animation event callbacks
    public void OnAttackAnimationStart()
    {
        if (isAttacking)
        {
            waitingForNextAttack = false;
            attackTimer = attackDuration;
            lastAttackTime = Time.time;
            
            if (AttackHitbox != null)
            {
                // Set attack type based on combo count
                AttackHitbox.AttackType comboType = attackCombo switch
                {
                    1 => AttackHitbox.AttackType.GroundStab,
                    2 => AttackHitbox.AttackType.GroundChop,
                    3 => AttackHitbox.AttackType.GroundSlash,
                    _ => AttackHitbox.AttackType.GroundStab
                };
                AttackHitbox.SetAttackType(comboType);
                AttackHitbox.SetActive(true);
            }
            
            CancelInvoke(nameof(EnableComboWindow));
            Invoke(nameof(EnableComboWindow), attackDuration * 0.5f);
        }
    }
    
    public void OnAttackAnimationEnd()
    {
        if (isAttacking && !isDashAttacking)
        {
            attackTimer = 0;
            EndCurrentAttack();
        }
    }
    
    public void OnDashAttackAnimationStart()
    {
        if (isDashAttacking && AttackHitbox != null)
        {
            AttackHitbox.SetAttackType(AttackHitbox.AttackType.DashAttack);
            AttackHitbox.SetActive(true);
        }
    }
    
    public void OnDashAttackAnimationEnd()
    {
        if (isDashAttacking)
        {
            attackTimer = 0;
            ResetAttackSystem();
        }
    }
    
    public void OnAirAttackAnimationStart()
    {
        if (isAirAttacking && AttackHitbox != null)
        {
            AttackHitbox.SetAttackType(AttackHitbox.AttackType.AirAttack);
            AttackHitbox.SetActive(true);
        }
    }
    
    public void OnAirAttackAnimationEnd()
    {
        if (isAirAttacking)
        {
            attackTimer = 0;
            ResetAttackSystem();
        }
    }
}