# Jumper Project Architecture

## System Dependency Graph

```
InputManager (Singleton)
    ↓ (events)
PlayerController (Singleton)
    ├─→ PlayerCombat (Required Component)
    ├─→ PlayerAbilities (Singleton, checked)
    ├─→ Rigidbody2D (Required Component)
    └─→ Animator (Required Component)

SimpleRespawnManager (Singleton)
    ↓ (manages)
PlayerController position reset

LevelTransitionManager (Singleton)
    ├─→ Scene loading
    └─→ SavePoint activation

BreakableTerrainManager
    ↓ (spawns/manages)
BreakableTerrain instances
```

## Component Communication Patterns

### 1. Event-Based Communication

**PlayerController → PlayerCombat**
```csharp
// PlayerCombat subscribes to player events
void OnEnable() {
    playerController.OnLanding += HandleLanding;
}

void OnDisable() {
    playerController.OnLanding -= HandleLanding;
}
```

**Health System → UI**
```csharp
public event Action<int> OnHealthChanged;
OnHealthChanged?.Invoke(_currentHealth);
```

### 2. Direct Component Access

**PlayerController accessing PlayerCombat**
```csharp
private PlayerCombat combat;

void Awake() {
    combat = GetComponent<PlayerCombat>();
}

void Update() {
    if (combat?.IsAttacking ?? false) {
        // Handle attack state
    }
}
```

### 3. Singleton Pattern

**Accessing Global Managers**
```csharp
if (PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasDash) {
    // Execute dash logic
}

SimpleRespawnManager.Instance.SetRespawnPoint(position);
```

## PlayerController Deep Dive

### State Management

**Movement States** (mutually exclusive):
```
isGrounded → on platform
isJumping → moving upward
isFalling → moving downward
isWallSticking → attached to wall
isWallSliding → sliding down wall
isDashing → dash active
```

**Combat States** (can overlap with movement):
```
IsAttacking → ground attack
IsAirAttacking → aerial attack
IsDashAttacking → dash + attack
```

### Update Loop Flow

**FixedUpdate() Execution Order**:
```
1. Check death zone (Y < -20)
2. Handle variable jump mechanics
3. Handle forced fall (for double jump)
4. Check buffered double jump
5. CheckGrounding() - determine if on ground
6. CheckWallDetection() - 3 raycasts for walls
7. UpdateMovementStates() - calculate state bools
8. HandleMidJumpWallCompensation() - jump height fix
9. HandleMovement() - apply velocity
10. HandleJumping() - process jump input
11. HandleDashInput() - process dash
12. Apply dash velocity (if dashing)
13. UpdateDashCooldown()
14. UpdateSpriteFacing()
15. UpdateAnimatorParameters()
```

### Critical Subsystems

**Ground Detection**:
- Uses `Physics2D.OverlapCircle()` at feet position
- Checks both `Ground` layer and `LandingBuffer` layer
- Buffer layer enables platform edge climbing
- Coyote time extends ground detection after leaving edge

**Wall Detection**:
- 3 raycasts at different heights (top, middle, bottom)
- Requires 2+ hits for valid wall stick
- Prevents movement into walls when wall stick disabled
- Handles corner cases with single hit detection

**Variable Jump System**:
- Hollow Knight style hold-for-height
- Two modes: velocity clamping vs gravity reduction
- Separate min/max velocities for regular vs double jump
- Jump compensation system for wall friction
- Hold duration determines max height achieved

**Slope Movement**:
- Multi-directional raycasts for detection
- Moves along slope surface, not horizontally
- Anti-sliding force when idle on slopes
- Configurable max walkable angle

## PlayerCombat System

### Attack State Machine

```
Idle
  ↓ [Ground Attack Input]
GroundAttack (Combo 1)
  ↓ [Attack Input within window]
GroundAttack (Combo 2)
  ↓ [Attack Input within window]
GroundAttack (Combo 3)
  → [Timeout] → Idle

Airborne
  ↓ [Attack Input]
AirAttack
  → [Land] → Idle

Dashing
  ↓ [Attack Input]
DashAttack
  → [End] → Idle
```

### Attack Buffering

**Ground Combo Buffer**:
- Stores attack input during attack animation
- Window: Last 0.2s of animation
- Allows fluid combo execution

**Dash Attack Buffer**:
- Can buffer attack during dash
- Executes immediately after dash ends
- Preserves dash momentum

### Hitbox System

**Attack Hitboxes**:
- Uses `Physics2D.OverlapCircle()` at attack point
- Filters by enemy layer mask
- Calls `TakeDamage()` on hit enemies
- Visualized with Gizmos

**Hit Detection**:
```csharp
Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
    _attackPoint.position, 
    _attackRange, 
    _enemyLayers
);
```

## PlayerAbilities System

### Ability Structure

```csharp
private bool _hasDash = false;
private bool _hasDoubleJump = false;
private bool _hasWallStick = false;

private Dictionary<string, bool> _abilities = new Dictionary<string, bool> {
    { "dashjump", false }
};
```

### Ability Integration Pattern

**Before executing feature**:
```csharp
if (PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasDash) {
    // Execute dash logic
} else {
    // Ability not unlocked - skip or provide feedback
}
```

**Dynamic behavior changes**:
```csharp
// Wall detection always runs, but behavior changes
bool hasWallStickAbility = PlayerAbilities.Instance?.HasWallStick ?? false;

if (hasWallStickAbility) {
    // Allow wall stick/slide
} else {
    // Prevent wall friction entirely
}
```

## Scene Management

### Level Transition Flow

```
Player enters LevelTransition trigger
    ↓
LevelTransitionManager.LoadLevel(sceneName, spawnPointID)
    ↓
Fade out effect (optional)
    ↓
SceneManager.LoadScene(sceneName)
    ↓
Find LevelSpawnPoint with matching ID
    ↓
Move player to spawn point
    ↓
Fade in effect (optional)
```

### Respawn System

**Save Point Activation**:
```
Player touches SavePoint collider
    ↓
SimpleRespawnManager.SetRespawnPoint(position)
    ↓
Visual feedback (animation/particle)
```

**Death and Respawn**:
```
Player Y position < deathZoneY (-20)
    ↓
PlayerController.ResetToRespawnPoint()
    ↓
Get position from SimpleRespawnManager
    ↓
Teleport player to respawn point
    ↓
Reset health/state
```

## Enemy System

### Enemy Behavior Pattern

**Base Enemy Structure**:
```csharp
public class SimpleEnemy : MonoBehaviour, IEnemyBase {
    // Movement
    private Rigidbody2D _rb;
    private float _moveSpeed;
    
    // AI State
    private enum State { Patrol, Chase, Attack }
    
    // Detection
    private float _detectionRadius;
    private LayerMask _playerLayer;
}
```

### Head Stomp Mechanic

**Player Side**:
```csharp
// In PlayerController or PlayerCombat
void OnTriggerEnter2D(Collider2D collision) {
    if (collision.CompareTag("EnemyHeadStompTrigger")) {
        // Apply bounce to player
        _rb.velocity = new Vector2(_rb.velocity.x, bounceForce);
        
        // Damage enemy
        enemy.TakeDamage(damage);
    }
}
```

**Enemy Side**:
```csharp
// HeadStompTriggerHandler on enemy head collider
void OnTriggerEnter2D(Collider2D collision) {
    if (collision.CompareTag("Player")) {
        float playerY = collision.transform.position.y;
        float enemyY = transform.position.y;
        
        if (playerY > enemyY) {
            // Valid head stomp
            GetComponentInParent<IEnemyBase>()?.TakeDamage(damage);
        }
    }
}
```

## Breakable Terrain System

### Terrain Lifecycle

```
BreakableTerrainManager spawns terrain prefabs
    ↓
BreakableTerrain.Initialize()
    ↓
Player/Attack collides with terrain
    ↓
BreakableTerrain.Break()
    ↓
    ├─→ Play break animation
    ├─→ Disable collider
    ├─→ Spawn particles
    └─→ Destroy after delay
```

### Composite Collider Integration

**CustomCompositeColliderGenerator**:
- Generates optimized collision shapes from tilemaps
- Merges adjacent tiles into single colliders
- Reduces physics calculation overhead
- Regenerates when terrain breaks

## Input System Flow

### Event-Based Input

```
User presses button
    ↓
Unity Input System (or old Input Manager)
    ↓
InputManager.Update() detects input
    ↓
InputManager fires event (e.g., OnJumpPressed)
    ↓
PlayerController receives event
    ↓
Sets internal flag (e.g., jumpQueued = true)
    ↓
FixedUpdate() processes flag
    ↓
Executes action (e.g., Jump())
```

### Input Buffering

**Jump Buffer**:
```csharp
private float lastJumpInputTime = 0f;
const float JUMP_INPUT_BUFFER_TIME = 0.1f;

void OnJumpInput() {
    lastJumpInputTime = Time.time;
}

void FixedUpdate() {
    bool hasRecentJumpInput = (Time.time - lastJumpInputTime) <= JUMP_INPUT_BUFFER_TIME;
    
    if (hasRecentJumpInput && CanJump()) {
        Jump();
    }
}
```

## Performance Considerations

### Caching Pattern

**Component References**:
```csharp
// ✅ Good - cached in Awake
private Rigidbody2D _rb;
void Awake() { _rb = GetComponent<Rigidbody2D>(); }

// ❌ Bad - called every frame
void Update() {
    GetComponent<Rigidbody2D>().velocity = ...;
}
```

### Physics Optimization

**Raycast Minimization**:
- Only cast when state requires it
- Cache raycast results when possible
- Use appropriate layer masks to filter results

**Collision Detection**:
- Static objects use Static Rigidbody
- Composite colliders merge tilemaps
- Appropriate collider types (Box fastest)

## Common Modification Patterns

### Adding a New Ability

1. Add property to `PlayerAbilities`:
```csharp
[SerializeField] private bool _hasWallJump = false;
public bool HasWallJump => _hasWallJump;
```

2. Gate feature in relevant system:
```csharp
if (PlayerAbilities.Instance?.HasWallJump ?? false) {
    // Execute wall jump logic
}
```

3. Add unlock method:
```csharp
public void UnlockWallJump() {
    _hasWallJump = true;
    OnAbilityUnlocked?.Invoke("walljump");
}
```

### Adding a New Enemy Type

1. Create class implementing `IEnemyBase`
2. Add movement/AI logic
3. Add head stomp trigger collider
4. Attach `HeadStompTriggerHandler` script
5. Configure layer masks and detection

### Extending Combat System

1. Add new attack type to `PlayerCombat`
2. Create hitbox configuration
3. Add animator parameters
4. Implement input handling
5. Add to attack state machine

## Debug and Testing Aids

### Gizmo Visualization

**Common Gizmo Patterns**:
```csharp
void OnDrawGizmosSelected() {
    // Attack range
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    
    // Detection radius
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, detectionRadius);
    
    // Ground check
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere(feetPosition, groundCheckRadius);
}
```

### Debug Visualization Flags

**PlayerController Debug Toggles**:
- `showJumpDebug` - Variable jump mechanics
- `enableSlopeVisualization` - Slope raycasts
- `showDeathZone` - Death boundary
- `showClimbingGizmos` - Platform edge detection

**OnGUI Debug Panels**:
- Wall stick state panel
- Variable jump metrics
- Movement state tracking
- Usually commented out in production
