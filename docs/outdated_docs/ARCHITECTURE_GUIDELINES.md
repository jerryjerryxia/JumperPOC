# Unity 2D Platformer Architecture Guidelines

**Project:** Jumper 2D Platformer
**Date:** 2025-10-21
**Status:** Definitive Recommendation

---

## Executive Summary

**DEFINITIVE RECOMMENDATION: 3-4 Components for Player Controller**

```
PlayerController (200-300 lines)
PlayerMovement (500-700 lines)
PlayerCombat (500-700 lines)
PlayerInput (100-200 lines) [OPTIONAL]
```

**Total: ~1200-1800 lines across 3-4 files**

This is the **optimal balance** between separation of concerns and practical Unity development. More components = over-engineered. Fewer = monolithic.

---

## The Core Principle

> **A component should be an INDEPENDENTLY TESTABLE SYSTEM, not just "a part of something."**

### Good Separation ✅

Components that can function and be tested independently:

- **PlayerMovement** - Contains ALL movement logic (run, jump, dash, wall slide)
  - Testable: "When grounded and jump pressed, velocity.y = jumpForce"
  - Self-contained: All ground/wall detection is PRIVATE helper methods
  - Simple API: Other components query `IsGrounded`, `IsDashing`, etc.

- **PlayerCombat** - Contains ALL combat logic (attacks, combos, hitboxes)
  - Testable: "When air attack used twice, third attack blocked"
  - Self-contained: All timing, state management is internal
  - Simple API: Other components query `IsAttacking`, `AttackCombo`, etc.

- **PlayerInput** - Routes input events
  - Testable: "When spacebar pressed, OnJump event fires"
  - Self-contained: No game logic, pure input routing
  - Simple API: Exposes events/actions for other components

### Bad Separation ❌

Components that cannot function independently:

- **PlayerGroundDetection** - Requires slope context, coyote time, buffer climbing state
- **PlayerJumpSystem** - Requires ground state, wall state, dash state
- **PlayerWallDetection** - Requires abilities, movement state
- **PlayerStateTracker** - Requires 10+ external states

**Problem:** These are interdependent pieces of ONE system (movement). Making them separate creates:
- State synchronization hell (passing 15+ parameters every frame)
- Testing requires mocking 6+ components
- Can't understand one without understanding all

**Solution:** These should be **private methods inside PlayerMovement**, not separate components.

---

## Why 11 Components Was Wrong

### What Happened

**Starting point:** Monolithic PlayerController (~2000 lines)

**Refactoring attempt:** Extreme decomposition
- PlayerMovement
- PlayerJumpSystem
- PlayerGroundDetection
- PlayerWallDetection
- PlayerStateTracker
- PlayerAnimationController
- PlayerCombat
- PlayerInputHandler
- PlayerRespawnSystem
- PlayerDebugVisualizer
- PlayerAbilities

**Result:** 11 components, 6800+ total lines

### The Problems Created

1. **State Synchronization Hell**
   ```csharp
   // PlayerController passes 15+ parameters every frame
   jumpSystem.UpdateExternalState(facingRight, moveInput, isGrounded, onWall,
                                  isOnSlope, currentSlopeAngle, slopeNormal,
                                  coyoteTimeCounter, leftGroundByJumping, isBufferClimbing,
                                  isDashing, lastDashEndTime);

   // Then reads state back
   jumpsRemaining = jumpSystem.JumpsRemaining;
   lastJumpTime = jumpSystem.LastJumpTime;
   dashJumpTime = jumpSystem.DashJumpTime;
   ```

2. **Circular Dependencies**
   ```csharp
   // Each component needs 3-7 other components
   public void Initialize(Rigidbody2D rb, Transform transform, Animator animator,
                         Collider2D col, PlayerGroundDetection ground,
                         PlayerWallDetection wall, PlayerCombat combat,
                         PlayerJumpSystem jump)
   ```

3. **Complex Testing**
   ```csharp
   // Tests require reflection to clean up singletons
   var field = typeof(PlayerAbilities).GetField("Instance",
       System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
   field.SetValue(null, null);
   ```

4. **Feature Addition Nightmare**
   - Adding wall run: Edit PlayerController, PlayerMovement, PlayerWallDetection, PlayerStateTracker, PlayerAnimationController
   - Every new feature touches 5+ files
   - Update 3+ Initialize() methods

### The Root Mistake

Treating Unity components like **microservices or pure functions**.

Unity components are **coarse-grained by design** because:
- Update loop overhead per component
- Serialization cost per component
- Inspector clutter with many components
- Component interdependencies are expensive

---

## The 3-4 Component Architecture

### Component Breakdown

#### 1. PlayerController (~250 lines)

**Responsibility:** Lightweight orchestrator

```csharp
public class PlayerController : MonoBehaviour
{
    // Component references
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerInput input;

    // Core components
    private Rigidbody2D rb;
    private Animator animator;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Initialize other components
        movement.Initialize(rb, transform);
        combat.Initialize(rb, animator);
        input.Initialize();
    }

    void Update()
    {
        // Sprite flipping
        if (movement.Velocity.x != 0)
        {
            transform.localScale = new Vector3(
                movement.Velocity.x > 0 ? 1 : -1, 1, 1
            );
        }

        // Animation updates
        animator.SetBool("IsGrounded", movement.IsGrounded);
        animator.SetBool("IsAttacking", combat.IsAttacking);
    }

    // Public API for external systems
    public void Respawn(Vector3 position)
    {
        transform.position = position;
        movement.ResetState();
        combat.ResetState();
    }
}
```

**Key Points:**
- No game logic (just orchestration)
- Simple initialization
- Minimal state (mostly in child components)
- Public API for death/respawn/scene transitions

---

#### 2. PlayerMovement (~600 lines)

**Responsibility:** ALL movement mechanics

```csharp
public class PlayerMovement : MonoBehaviour
{
    // ===== CONFIGURATION =====
    [Header("Run")]
    [SerializeField] private float runSpeed = 4f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private int maxAirJumps = 1;
    [SerializeField] private float coyoteTime = 0.15f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private int maxAirDashes = 1;

    [Header("Wall")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(6, 8);
    [SerializeField] private float wallStickTime = 0.2f;

    [Header("Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float wallCheckDistance = 0.5f;

    // ===== STATE (all private) =====
    private Rigidbody2D rb;
    private Transform playerTransform;

    private bool isGrounded;
    private bool onWall;
    private bool isWallSliding;
    private int jumpsRemaining;
    private int dashesRemaining;
    private bool isDashing;
    private float coyoteTimeCounter;
    private float wallStickCounter;

    // ===== PUBLIC API =====
    public bool IsGrounded => isGrounded;
    public bool IsDashing => isDashing;
    public bool IsWallSliding => isWallSliding;
    public Vector2 Velocity => rb.velocity;

    // ===== INITIALIZATION =====
    public void Initialize(Rigidbody2D rigidbody, Transform transform)
    {
        rb = rigidbody;
        playerTransform = transform;
        jumpsRemaining = maxAirJumps;
        dashesRemaining = 1;
    }

    // ===== CORE LOOP =====
    void FixedUpdate()
    {
        CheckGroundState();
        CheckWallState();
        HandleHorizontalMovement();
        HandleJump();
        HandleDash();
        HandleWallSlide();
    }

    // ===== PRIVATE IMPLEMENTATION =====

    private void CheckGroundState()
    {
        // Ground detection via raycasts
        // Slope detection
        // Coyote time tracking
        // Landing callbacks

        // This was PlayerGroundDetection - now a private method
    }

    private void CheckWallState()
    {
        // Wall detection via raycasts
        // Wall stick determination
        // Wall normal calculation

        // This was PlayerWallDetection - now a private method
    }

    private void HandleJump()
    {
        // Ground jump
        // Air jump (double/triple)
        // Wall jump
        // Variable jump height
        // Jump buffering

        // This was PlayerJumpSystem - now a private method
    }

    private void HandleHorizontalMovement()
    {
        // Run speed
        // Slope movement
        // Air control
        // Movement smoothing
    }

    private void HandleDash()
    {
        // Dash input check
        // Dash velocity application
        // Dash cooldown
        // Air dash tracking
    }

    private void HandleWallSlide()
    {
        // Wall slide velocity
        // Wall stick duration
        // Transition to wall jump
    }

    // ===== PUBLIC METHODS =====

    public void Jump()
    {
        // Called by input system
        // Handles all jump types internally
    }

    public void Dash(Vector2 direction)
    {
        // Called by input system
        // Handles dash execution
    }

    public void ResetState()
    {
        // Called on respawn/death
        jumpsRemaining = maxAirJumps;
        dashesRemaining = 1;
        isDashing = false;
    }
}
```

**Key Points:**
- Self-contained: All movement logic in ONE place
- Ground/wall detection are PRIVATE helpers (not components)
- Jump/dash logic are PRIVATE helpers (not components)
- Simple public API for queries and commands
- No complex state synchronization

---

#### 3. PlayerCombat (~700 lines)

**Responsibility:** ALL combat mechanics

```csharp
public class PlayerCombat : MonoBehaviour
{
    // ===== CONFIGURATION =====
    [Header("Ground Attacks")]
    [SerializeField] private float attackDuration = 0.3f;
    [SerializeField] private int maxCombo = 3;

    [Header("Air Attacks")]
    [SerializeField] private float airAttackDuration = 0.25f;
    [SerializeField] private int maxAirAttacks = 2;

    [Header("Dash Attacks")]
    [SerializeField] private float dashAttackDuration = 0.25f;
    [SerializeField] private float dashAttackWindow = 0.2f;

    [Header("References")]
    [SerializeField] private AttackHitbox hitbox;

    // ===== STATE (all private) =====
    private Rigidbody2D rb;
    private Animator animator;

    private bool isAttacking;
    private bool isAirAttacking;
    private bool isDashAttacking;
    private int attackCombo;
    private int airAttacksUsed;
    private float attackTimer;

    // ===== PUBLIC API =====
    public bool IsAttacking => isAttacking;
    public bool IsAirAttacking => isAirAttacking;
    public bool IsDashAttacking => isDashAttacking;
    public int AttackCombo => attackCombo;

    // ===== INITIALIZATION =====
    public void Initialize(Rigidbody2D rigidbody, Animator anim)
    {
        rb = rigidbody;
        animator = anim;
    }

    // ===== CORE LOOP =====
    void Update()
    {
        UpdateAttackTimers();
    }

    // ===== PUBLIC METHODS =====

    public void Attack(bool isGrounded, bool isDashing)
    {
        // Determine attack type based on state
        if (isDashing)
            StartDashAttack();
        else if (!isGrounded)
            StartAirAttack();
        else
            StartGroundAttack();
    }

    public void OnLanding()
    {
        // Reset air attack counter
        airAttacksUsed = 0;
    }

    public void ResetState()
    {
        isAttacking = false;
        isDashAttacking = false;
        isAirAttacking = false;
        attackCombo = 0;
        airAttacksUsed = 0;
    }

    // ===== PRIVATE IMPLEMENTATION =====

    private void StartGroundAttack()
    {
        // Ground combo logic
    }

    private void StartAirAttack()
    {
        // Air attack with 2-attack limit
    }

    private void StartDashAttack()
    {
        // Dash attack timing window
    }

    private void UpdateAttackTimers()
    {
        // Attack duration tracking
    }
}
```

**Key Points:**
- Self-contained: All combat state internal
- Queries movement state via simple parameters (isGrounded, isDashing)
- NO bidirectional state sync
- Well-tested (already has 12 tests)

---

#### 4. PlayerInput (~150 lines) [OPTIONAL]

**Responsibility:** Input routing

```csharp
public class PlayerInput : MonoBehaviour
{
    // Events
    public event Action<Vector2> OnMove;
    public event Action OnJumpPressed;
    public event Action OnJumpReleased;
    public event Action OnDashPressed;
    public event Action OnAttackPressed;

    private PlayerInputActions inputActions;

    public void Initialize()
    {
        inputActions = new PlayerInputActions();
        inputActions.Enable();

        // Subscribe to input system
        inputActions.Player.Move.performed += ctx => OnMove?.Invoke(ctx.ReadValue<Vector2>());
        inputActions.Player.Jump.performed += _ => OnJumpPressed?.Invoke();
        inputActions.Player.Jump.canceled += _ => OnJumpReleased?.Invoke();
        inputActions.Player.Dash.performed += _ => OnDashPressed?.Invoke();
        inputActions.Player.Attack.performed += _ => OnAttackPressed?.Invoke();
    }

    void OnDestroy()
    {
        inputActions?.Disable();
    }
}
```

**Key Points:**
- Pure input routing (no game logic)
- Could be merged into PlayerController if you prefer
- Easy to test (just event firing)

**Alternative:** Skip this component and handle input directly in PlayerController if you want even simpler architecture (3 components instead of 4).

---

## Comparison Table

| Aspect | 11 Components (Current) | 3-4 Components (Recommended) | 1 Component (Monolith) |
|--------|------------------------|------------------------------|------------------------|
| **Total lines** | 6800+ | 1200-1800 | 2000+ |
| **Lines per file** | 150-750 | 250-700 | 2000+ |
| **State sync complexity** | High (15+ params/frame) | None (internal state) | None (all local) |
| **Dependencies** | Each needs 3-7 others | Each needs 1-2 others | None |
| **Test complexity** | High (6+ mocks needed) | Low (1-2 mocks) | Low (no mocks) |
| **Test count needed** | 50+ for same coverage | 10-15 per component | 30-40 total |
| **Feature addition** | Touch 5+ files | Touch 1-2 files | Touch 1 file |
| **Inspector clutter** | 11+ components | 3-4 components | 1 component |
| **Unity performance** | Acceptable | Good | Good |
| **Beginner friendly** | No | **Yes** ✓ | Maybe |
| **Industry standard** | No (outlier) | **Yes** ✓ | Common |
| **Maintenance burden** | Very High | Medium | High (large file) |

**Verdict:** 3-4 components is optimal for Unity 2D platformer development.

---

## Real Game Examples

### Celeste (Critically Acclaimed 2D Platformer)

```
Player.cs (~1500 lines)
├─ Most movement logic
├─ State management
└─ Core gameplay

PlayerHair.cs
└─ Visual effects only

PlayerSprite.cs
└─ Rendering/animation
```

**Total:** ~3 main components for player

### Hollow Knight

```
HeroController
├─ Main movement
├─ State machine
└─ Core mechanics

InputHandler
└─ Input routing

HeroAnimator
└─ Animation system
```

**Total:** ~4-5 main components for player

### Most Unity Tutorials

```
PlayerController
└─ Movement

PlayerCombat
└─ Combat

(Optional) PlayerInput
└─ Input
```

**Total:** 2-3 components typical

**Conclusion:** Your current 11 components is an outlier. Industry standard is 3-5 for player controller.

---

## Migration Path

### Phase 1: Merge Movement (4-8 hours)

**Step 1:** Create new consolidated PlayerMovement.cs

**Step 2:** Copy code from:
- Old PlayerMovement → new PlayerMovement
- PlayerGroundDetection → new private methods
- PlayerWallDetection → new private methods
- PlayerJumpSystem → new private methods
- PlayerStateTracker → new private methods

**Step 3:** Remove state synchronization:
```csharp
// DELETE all of this from PlayerController
jumpSystem.UpdateExternalState(facingRight, moveInput, isGrounded, ...);
jumpsRemaining = jumpSystem.JumpsRemaining;
lastJumpTime = jumpSystem.LastJumpTime;
// etc.
```

**Step 4:** Simplify public API:
```csharp
// PlayerMovement now has simple getters
public bool IsGrounded { get; private set; }
public bool IsDashing { get; private set; }
public Vector2 Velocity => rb.velocity;

// PlayerController just queries these
animator.SetBool("IsGrounded", movement.IsGrounded);
```

**Step 5:** Remove old component files:
- Delete PlayerGroundDetection.cs
- Delete PlayerWallDetection.cs
- Delete PlayerJumpSystem.cs
- Delete PlayerStateTracker.cs

**Step 6:** Update tests:
- Tests for ground detection → tests for PlayerMovement.IsGrounded
- Tests for wall detection → tests for PlayerMovement.OnWall
- Tests for jump system → tests for PlayerMovement.Jump()

### Phase 2: Simplify PlayerController (2 hours)

**Step 1:** Remove complex initialization:
```csharp
// OLD (delete this complexity)
public void Initialize(Rigidbody2D rb, Transform t, Animator a, Collider2D c,
                      PlayerGroundDetection g, PlayerWallDetection w, ...);

// NEW (simple)
void Awake()
{
    rb = GetComponent<Rigidbody2D>();
    movement = GetComponent<PlayerMovement>();
    combat = GetComponent<PlayerCombat>();

    movement.Initialize(rb, transform);
    combat.Initialize(rb, animator);
}
```

**Step 2:** Remove state tracking variables:
```csharp
// DELETE these from PlayerController (now in components)
private bool isGrounded;
private bool onWall;
private int jumpsRemaining;
private float dashTimer;
// etc.
```

**Step 3:** Simplify FixedUpdate:
```csharp
// OLD (689 lines)
void FixedUpdate()
{
    // Update 11 components
    // Sync state 40+ times
    // Complex orchestration
}

// NEW (~50 lines)
void FixedUpdate()
{
    // Components update themselves
    movement.UpdateMovement(input.MoveInput);
    combat.UpdateCombat();

    // Simple sprite flipping
    if (movement.Velocity.x != 0)
        transform.localScale = new Vector3(movement.Velocity.x > 0 ? 1 : -1, 1, 1);

    // Simple animation
    animator.SetBool("IsGrounded", movement.IsGrounded);
    animator.SetBool("IsAttacking", combat.IsAttacking);
}
```

### Phase 3: Cleanup (2 hours)

**Step 1:** Remove singleton patterns where possible
- Convert PlayerAbilities to ScriptableObject
- Use dependency injection instead of .Instance calls

**Step 2:** Remove backup files
```bash
git rm Assets/Scripts/Player/*Backup*.cs
```

**Step 3:** Clean up debug logs
- Remove 108 commented Debug.Log statements
- Or implement proper conditional logging

**Step 4:** Update documentation
- Update CLAUDE.md with new architecture
- Document the 3-4 component pattern

---

## Testing Strategy

### PlayerMovement Tests (~30 tests)

**Ground Detection (10 tests):**
- Grounded on flat surface
- Grounded on slope (up to max angle)
- Not grounded on steep slope
- Coyote time works after leaving ground
- Landing callback fires
- Landing resets dash/jump counters
- Buffer climbing assists over ledges
- Slope movement follows surface
- Slope idle prevents sliding
- Ground layer detection correct

**Jump System (10 tests):**
- Ground jump works when grounded
- Air jump works when airborne (with double jump ability)
- Wall jump works on wall
- Variable jump height (hold vs tap)
- Jump buffering works
- Coyote time allows late jump
- Jump consumes from counter correctly
- Max jump count enforced
- Jump blocked when at max count
- Jump resets on landing

**Wall Mechanics (5 tests):**
- Wall stick when pushing into wall
- Wall slide when falling on wall
- Wall jump pushes away from wall
- Wall stick disabled when ability locked
- Wall detection uses correct raycasts

**Dash Mechanics (5 tests):**
- Ground dash works and consumes charge
- Air dash works once per air time
- Dash cooldown prevents spam
- Dash velocity correct direction
- Dash resets on landing

### PlayerCombat Tests (~12 tests - already exist!)

Keep existing tests:
- Air attack limit (2 max)
- Rapid click protection
- Slot forfeiture on double jump
- Dash attack timing window
- Attack state reset
- Combo sequencing
- Input buffering

### PlayerController Tests (~5 tests)

**Integration Tests:**
- Component initialization
- Sprite flipping follows movement
- Animation parameters updated correctly
- Respawn resets all components
- Death triggers respawn

**Total: ~47 tests** (down from 159 with complex mocking)

---

## When to Add More Components

Only create new components when you add **truly independent systems**:

### Good Reasons to Add Component ✅

1. **PlayerGrapple** - If you add grappling hook mechanic
   - Independent from movement (new input, new physics)
   - Own state (grapple point, rope length, swing physics)
   - Testable: "When grapple fired at valid point, rope attaches"

2. **PlayerInventory** - If you add item system
   - Independent from gameplay (UI, data storage)
   - Own state (items, quantities, equipment)
   - Testable: "When item collected, inventory count increases"

3. **PlayerDialogue** - If you add NPC conversations
   - Independent from gameplay (triggered by external systems)
   - Own state (current dialogue, choices made)
   - Testable: "When dialogue starts, player movement disabled"

### Bad Reasons to Add Component ❌

1. **PlayerClimbLadder** - This is just movement variation
   - Merge into PlayerMovement as HandleLadderClimb()
   - Not independent enough to warrant component

2. **PlayerRollDodge** - This is just dash variation
   - Merge into PlayerMovement as HandleRoll()
   - Shares state with dash (cooldown, charges)

3. **PlayerJetpack** - This is just jump variation
   - Merge into PlayerMovement as HandleJetpack()
   - Uses same air control as double jump

**Rule of thumb:** If it shares significant state or logic with existing components, it's a private method, not a new component.

---

## Architecture Decision Record

### Decision: Use 3-4 Component Architecture

**Date:** 2025-10-21

**Status:** Accepted

**Context:**
- Previous architecture: 11 components with tight coupling
- Problem: Complex state synchronization, difficult testing, high maintenance burden
- Alternatives considered:
  1. Keep 11 components (rejected: too complex)
  2. Merge to monolith (rejected: loses separation benefits)
  3. Use 3-4 components (accepted: optimal balance)

**Decision:**
- Merge movement-related components into single PlayerMovement
- Keep PlayerCombat separate (independent system)
- Keep PlayerController as lightweight orchestrator
- Optional PlayerInput for input routing

**Consequences:**

**Positive:**
- Simpler testing (10-15 tests per component vs 50+ with mocking)
- Easier feature addition (touch 1-2 files vs 5+)
- Better performance (fewer update calls, better caching)
- Standard Unity pattern (matches industry examples)

**Negative:**
- Larger files (600 lines vs 150 lines)
- Need to refactor existing code (8-12 hour investment)
- Existing tests need updates

**Migration Risk:** Low
- Can migrate incrementally
- Tests catch regressions
- Core gameplay already works

---

## Lessons Learned

### What Went Wrong

1. **Over-applied "separation of concerns"**
   - Treated every logical concept as needing its own component
   - Ignored Unity's coarse-grained component model
   - Created distributed monolith instead of clean separation

2. **Followed microservice patterns in wrong context**
   - Microservices work for network-separated systems
   - Unity components share memory space and frame budget
   - Wrong abstraction for the problem

3. **Ignored industry patterns**
   - Most Unity player controllers: 2-4 components
   - We created 11 components (outlier)
   - Should have researched first

### What to Do Different

1. **Start with fewer components, split only when needed**
   - Default: PlayerController + PlayerMovement + PlayerCombat
   - Add components only for truly independent systems
   - Resist urge to "extract" every piece of logic

2. **Use private methods for implementation details**
   - Ground detection → private method in PlayerMovement
   - Not every helper needs to be a component
   - Components are for SYSTEMS, methods are for LOGIC

3. **Test at component boundaries, not implementation details**
   - Test PlayerMovement.IsGrounded (public API)
   - Don't test internal ground detection raycast logic
   - Focus on behavior, not implementation

---

## Quick Reference

### Decision Tree: Should This Be a Component?

```
Is it truly independent from other systems?
├─ YES
│  └─ Does it have its own state and lifecycle?
│     ├─ YES
│     │  └─ Would it have 10+ tests of its own?
│     │     ├─ YES → Make it a component ✅
│     │     └─ NO → Make it a private method ❌
│     └─ NO → Make it a private method ❌
└─ NO → Make it a private method ❌
```

### Component Count Guidelines

| Project Type | Recommended Components | Max Components |
|--------------|----------------------|----------------|
| Simple 2D platformer | 2-3 | 4 |
| Complex 2D platformer (this project) | 3-4 | 5 |
| 3D action game | 4-6 | 8 |
| RPG with many systems | 6-10 | 12 |

**Your project:** 3-4 components (current: 11, overengineered)

---

## Conclusion

**The optimal Unity 2D platformer player architecture is 3-4 components:**

1. **PlayerController** - Lightweight orchestration
2. **PlayerMovement** - All movement (run, jump, dash, wall mechanics)
3. **PlayerCombat** - All combat (attacks, combos, timing)
4. **PlayerInput** - Input routing (optional, can merge into controller)

This balances:
- ✅ Separation of concerns (independent systems separated)
- ✅ Simplicity (no complex state synchronization)
- ✅ Testability (each component testable with minimal mocking)
- ✅ Maintainability (features touch 1-2 files, not 5+)
- ✅ Unity best practices (follows industry patterns)

**Do not exceed 5 components unless adding truly independent systems (grapple, inventory, dialogue, etc.).**

---

**This document is the definitive architectural guideline. Reference this when making component decisions.**
