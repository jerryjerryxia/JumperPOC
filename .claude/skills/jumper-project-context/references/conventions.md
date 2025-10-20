# Jumper Project Conventions

## Code Style Guide

### Naming Conventions

**Fields**:
```csharp
// Private fields - underscore prefix, camelCase
private float _moveSpeed = 5f;
private Rigidbody2D _rb;
private bool _isGrounded;

// Serialized private fields - still use underscore
[SerializeField] private float _jumpForce = 10f;
[SerializeField] private LayerMask _groundLayer;

// Public fields - PascalCase (rare, prefer properties)
public float MaxHealth = 100f;
```

**Properties**:
```csharp
// Public properties - PascalCase
public bool IsGrounded => _isGrounded;
public float CurrentHealth { get; private set; }

// Read-only properties from private fields
public Vector2 MoveInput => _moveInput;
```

**Methods**:
```csharp
// All methods - PascalCase
private void HandleMovement() { }
public void TakeDamage(int amount) { }
```

**Constants**:
```csharp
// Usually as serialized fields for designer control
[SerializeField] private float JUMP_INPUT_BUFFER_TIME = 0.1f;

// True constants - UPPER_SNAKE_CASE or PascalCase
private const float MAX_FALL_SPEED = 20f;
```

### Serialization Patterns

**Always Prefer Serialized Private Fields**:
```csharp
// ✅ Good - serialized private with property
[SerializeField] private float _health;
public float Health => _health;

// ❌ Bad - public field
public float health;

// ❌ Bad - private without serialization
private float health; // Can't set in Inspector
```

**Headers for Organization**:
```csharp
[Header("Movement")]
[SerializeField] private float _runSpeed = 5f;
[SerializeField] private float _jumpForce = 10f;

[Header("Combat")]
[SerializeField] private int _attackDamage = 10;
[SerializeField] private float _attackRange = 0.5f;
```

**Tooltips for Complex Settings**:
```csharp
[Tooltip("Grace period after leaving ground where jump is still allowed")]
[SerializeField] private float _coyoteTimeDuration = 0.12f;
```

## Component Reference Patterns

### Caching in Awake()

**Standard Pattern**:
```csharp
private Rigidbody2D _rb;
private Animator _animator;
private PlayerCombat _combat;

void Awake() {
    _rb = GetComponent<Rigidbody2D>();
    _animator = GetComponent<Animator>();
    _combat = GetComponent<PlayerCombat>();
}
```

**With Null Checks**:
```csharp
void Awake() {
    _rb = GetComponent<Rigidbody2D>();
    
    if (_rb == null) {
        Debug.LogError($"Rigidbody2D not found on {gameObject.name}!");
    }
}
```

### Singleton Pattern

**Standard Singleton Implementation**:
```csharp
public class GameManager : MonoBehaviour {
    private static GameManager instance;
    public static GameManager Instance => instance;
    
    void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return;
        }
        
        // Continue with normal Awake logic
    }
}
```

**Null-Safe Singleton Access**:
```csharp
// ✅ Good - null-conditional with fallback
if (PlayerAbilities.Instance?.HasDash ?? false) {
    // Execute dash
}

// ✅ Good - explicit null check
if (PlayerAbilities.Instance != null && PlayerAbilities.Instance.HasDash) {
    // Execute dash
}
```

## Unity Lifecycle Patterns

### Initialization Order

```csharp
void Awake() {
    // 1. Cache component references
    _rb = GetComponent<Rigidbody2D>();
    
    // 2. Initialize internal state
    _currentHealth = _maxHealth;
    
    // 3. Singleton setup (if applicable)
    if (instance == null) instance = this;
}

void Start() {
    // 4. Reference other objects (they've all run Awake)
    _player = FindObjectOfType<PlayerController>();
    
    // 5. Subscribe to events
    _player.OnDeath += HandlePlayerDeath;
    
    // 6. Validate setup
    ValidateComponentSetup();
}

void OnEnable() {
    // Subscribe to input events
    InputManager.OnJumpPressed += OnJumpInput;
}

void OnDisable() {
    // Unsubscribe from input events
    InputManager.OnJumpPressed -= OnJumpInput;
}
```

### Update vs FixedUpdate

**FixedUpdate** - Physics and Rigidbody:
```csharp
void FixedUpdate() {
    // Movement with Rigidbody
    _rb.velocity = new Vector2(horizontalInput * speed, _rb.velocity.y);
    
    // Ground checks
    CheckGrounding();
    
    // Apply forces
    if (shouldJump) {
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
}
```

**Update** - Input and Logic:
```csharp
void Update() {
    // Read input
    horizontalInput = Input.GetAxisRaw("Horizontal");
    
    // State logic
    if (Input.GetButtonDown("Jump")) {
        jumpQueued = true;
    }
    
    // Timers
    attackCooldownTimer -= Time.deltaTime;
}
```

**LateUpdate** - Camera and Post-Processing:
```csharp
void LateUpdate() {
    // Camera following (after player has moved)
    transform.position = Vector3.Lerp(
        transform.position, 
        target.position, 
        smoothSpeed
    );
}
```

## Event System Patterns

### C# Events

**Declaration**:
```csharp
public event Action OnDeath;
public event Action<int> OnHealthChanged;
public event Action<Vector2> OnPositionUpdated;
```

**Invocation (Null-Safe)**:
```csharp
// ✅ Null-conditional operator
OnDeath?.Invoke();
OnHealthChanged?.Invoke(_currentHealth);

// ❌ Not null-safe
OnDeath.Invoke(); // Crashes if no subscribers
```

**Subscription Pattern**:
```csharp
void OnEnable() {
    player.OnDeath += HandlePlayerDeath;
    player.OnHealthChanged += HandleHealthChanged;
}

void OnDisable() {
    // CRITICAL: Always unsubscribe
    player.OnDeath -= HandlePlayerDeath;
    player.OnHealthChanged -= HandleHealthChanged;
}

// Event handlers
private void HandlePlayerDeath() {
    Debug.Log("Player died!");
}

private void HandleHealthChanged(int newHealth) {
    healthBar.SetHealth(newHealth);
}
```

### UnityEvents (Inspector-Assignable)

```csharp
using UnityEngine.Events;

[System.Serializable]
public class HealthEvent : UnityEvent<int> { }

public class Health : MonoBehaviour {
    [SerializeField] private HealthEvent onHealthChanged;
    
    public void TakeDamage(int damage) {
        _currentHealth -= damage;
        onHealthChanged?.Invoke(_currentHealth);
    }
}
```

## Input Handling Patterns

### Event-Based Input (Current System)

```csharp
// InputManager fires events
public class InputManager : MonoBehaviour {
    public event Action OnJumpPressed;
    public event Action<Vector2> OnMoveInput;
    
    void Update() {
        // Detect input
        if (Input.GetButtonDown("Jump")) {
            OnJumpPressed?.Invoke();
        }
        
        Vector2 moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
        OnMoveInput?.Invoke(moveInput);
    }
}

// PlayerController subscribes
public class PlayerController : MonoBehaviour {
    private Vector2 _moveInput;
    
    void OnEnable() {
        InputManager.OnJumpPressed += HandleJumpInput;
        InputManager.OnMoveInput += HandleMoveInput;
    }
    
    void OnDisable() {
        InputManager.OnJumpPressed -= HandleJumpInput;
        InputManager.OnMoveInput -= HandleMoveInput;
    }
    
    private void HandleJumpInput() {
        _jumpQueued = true;
    }
    
    private void HandleMoveInput(Vector2 input) {
        _moveInput = input;
    }
}
```

### Input Buffering Pattern

```csharp
private float _lastJumpInputTime = 0f;
private const float JUMP_INPUT_BUFFER_TIME = 0.1f;

void OnJumpInput() {
    _lastJumpInputTime = Time.time;
}

void FixedUpdate() {
    // Check if there's a recent jump input
    bool hasRecentJumpInput = (Time.time - _lastJumpInputTime) <= JUMP_INPUT_BUFFER_TIME;
    
    if (hasRecentJumpInput && CanJump()) {
        Jump();
        _lastJumpInputTime = 0f; // Consume the input
    }
}
```

## Animator Integration

### Safe Parameter Setting

**Problem**: Missing animator parameters cause errors

**Solution**: Safe setter methods
```csharp
private HashSet<string> _missingAnimatorParams = new HashSet<string>();
private bool _hasLoggedAnimatorWarnings = false;

private void SafeSetBool(string paramName, bool value) {
    if (HasAnimatorParameter(paramName)) {
        _animator.SetBool(paramName, value);
    } else {
        _missingAnimatorParams.Add(paramName);
    }
}

private bool HasAnimatorParameter(string paramName) {
    if (_animator == null) return false;
    
    foreach (var param in _animator.parameters) {
        if (param.name == paramName) return true;
    }
    return false;
}

void UpdateAnimatorParameters() {
    SafeSetBool("IsGrounded", _isGrounded);
    SafeSetBool("IsRunning", _isRunning);
    SafeSetFloat("HorizontalInput", _horizontalInput);
    
    // Log missing parameters once
    if (!_hasLoggedAnimatorWarnings && _missingAnimatorParams.Count > 0) {
        _hasLoggedAnimatorWarnings = true;
        Debug.LogWarning($"Missing animator parameters: {string.Join(", ", _missingAnimatorParams)}");
    }
}
```

### Animation State Synchronization

```csharp
void FixedUpdate() {
    // Update physics
    HandleMovement();
    
    // Update movement states
    UpdateMovementStates();
    
    // Synchronize animator (after states are updated)
    UpdateAnimatorParameters();
}

private void UpdateMovementStates() {
    _isRunning = Mathf.Abs(_moveInput.x) > 0.1f && _isGrounded;
    _isJumping = !_isGrounded && _rb.velocity.y > 0;
    _isFalling = !_isGrounded && _rb.velocity.y < 0;
}
```

## Debug Visualization

### Gizmos Pattern

```csharp
void OnDrawGizmos() {
    // Always visible gizmos
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, detectionRadius);
}

void OnDrawGizmosSelected() {
    // Only visible when object is selected
    if (_attackPoint != null) {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_attackPoint.position, _attackRange);
    }
    
    // Ground check visualization
    if (Application.isPlaying) {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector2 feetPos = new Vector2(transform.position.x, transform.position.y + _groundCheckOffset);
        Gizmos.DrawWireSphere(feetPos, _groundCheckRadius);
    }
}
```

### Debug Raycast Visualization

```csharp
// In movement/detection code
RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, layerMask);

// Visualize the raycast
if (_enableDebugVisualization) {
    if (hit.collider != null) {
        Debug.DrawLine(origin, hit.point, Color.green, 0.1f);
        Debug.DrawLine(hit.point, origin + direction * distance, Color.red, 0.1f);
    } else {
        Debug.DrawLine(origin, origin + direction * distance, Color.red, 0.1f);
    }
}
```

### OnGUI Debug Panels

```csharp
void OnGUI() {
    if (!Application.isPlaying) return;
    
    // Debug panel (often commented out in production)
    if (_showDebugPanel) {
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label("=== PLAYER DEBUG ===");
        
        GUILayout.Label($"Grounded: {_isGrounded}");
        GUILayout.Label($"Velocity: {_rb.velocity}");
        
        GUI.contentColor = _isJumping ? Color.green : Color.gray;
        GUILayout.Label($"Jumping: {_isJumping}");
        GUI.contentColor = Color.white;
        
        GUILayout.EndArea();
    }
}
```

## Error Handling Patterns

### Component Validation

```csharp
void Awake() {
    // Cache components
    _rb = GetComponent<Rigidbody2D>();
    _animator = GetComponent<Animator>();
    
    // Validate in separate method
    VerifyComponentSetup();
}

private void VerifyComponentSetup() {
    if (_rb == null) {
        Debug.LogError($"[{GetType().Name}] Rigidbody2D not found on {gameObject.name}!");
    }
    
    if (_animator == null) {
        Debug.LogError($"[{GetType().Name}] Animator not found on {gameObject.name}!");
    }
    
    #if UNITY_EDITOR
    // In editor, offer to add missing component
    if (_rb == null) {
        if (UnityEditor.EditorUtility.DisplayDialog(
            "Missing Component",
            $"Add Rigidbody2D to {gameObject.name}?",
            "Yes", "No"))
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
        }
    }
    #endif
}
```

### Null-Safe Property Access

```csharp
// ✅ Null-conditional operator
public bool IsAttacking => _combat?.IsAttacking ?? false;

// ✅ Explicit null check with fallback
public float AttackDamage {
    get {
        if (_combat != null) {
            return _combat.AttackDamage;
        }
        return 0f; // Default fallback
    }
}
```

### Layer Validation

```csharp
void Start() {
    int groundLayer = LayerMask.NameToLayer("Ground");
    int bufferLayer = LayerMask.NameToLayer("LandingBuffer");
    
    if (groundLayer == -1) {
        Debug.LogError("[PlayerController] 'Ground' layer not defined in project settings!");
    }
    
    if (bufferLayer == -1) {
        Debug.LogWarning("[PlayerController] 'LandingBuffer' layer not defined. Edge detection may not work.");
    }
}
```

## Performance Patterns

### Object Pooling (When Needed)

```csharp
public class ProjectilePool : MonoBehaviour {
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private int _initialPoolSize = 20;
    
    private Queue<GameObject> _pool = new Queue<GameObject>();
    
    void Awake() {
        for (int i = 0; i < _initialPoolSize; i++) {
            GameObject obj = Instantiate(_projectilePrefab);
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
    
    public GameObject Get() {
        if (_pool.Count == 0) {
            // Grow pool if needed
            GameObject obj = Instantiate(_projectilePrefab);
            return obj;
        }
        
        GameObject pooled = _pool.Dequeue();
        pooled.SetActive(true);
        return pooled;
    }
    
    public void Return(GameObject obj) {
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }
}
```

### Coroutine Patterns

**For Delayed Actions**:
```csharp
private void StartInvincibility() {
    StartCoroutine(InvincibilityRoutine());
}

private IEnumerator InvincibilityRoutine() {
    _isInvincible = true;
    
    yield return new WaitForSeconds(_invincibilityDuration);
    
    _isInvincible = false;
}
```

**For Sequences**:
```csharp
private IEnumerator AttackSequence() {
    _isAttacking = true;
    _animator.SetTrigger("Attack");
    
    // Wait for animation
    yield return new WaitForSeconds(0.2f);
    
    // Spawn hitbox
    SpawnAttackHitbox();
    
    yield return new WaitForSeconds(0.3f);
    
    _isAttacking = false;
}
```

## Common Gotchas and Solutions

### 1. Event Subscription Memory Leaks

**Problem**: Not unsubscribing from events
```csharp
// ❌ Bad - memory leak
void Start() {
    player.OnDeath += HandleDeath;
}
```

**Solution**: Always unsubscribe
```csharp
// ✅ Good
void OnEnable() {
    player.OnDeath += HandleDeath;
}

void OnDisable() {
    player.OnDeath -= HandleDeath; // CRITICAL
}
```

### 2. Physics in Update Instead of FixedUpdate

**Problem**: Rigidbody manipulation in Update
```csharp
// ❌ Bad - inconsistent physics
void Update() {
    _rb.velocity = new Vector2(speed, _rb.velocity.y);
}
```

**Solution**: Use FixedUpdate for physics
```csharp
// ✅ Good
void FixedUpdate() {
    _rb.velocity = new Vector2(speed, _rb.velocity.y);
}
```

### 3. Tag Comparisons

**Problem**: String comparison for tags
```csharp
// ❌ Slower
if (other.gameObject.tag == "Player") { }
```

**Solution**: Use CompareTag
```csharp
// ✅ Faster
if (other.CompareTag("Player")) { }
```

### 4. Finding Objects Every Frame

**Problem**: Expensive lookups in Update
```csharp
// ❌ Very bad performance
void Update() {
    GameObject player = GameObject.Find("Player");
}
```

**Solution**: Cache in Start/Awake
```csharp
// ✅ Good
private GameObject _player;

void Start() {
    _player = GameObject.FindGameObjectWithTag("Player");
}
```

## Documentation Conventions

### XML Comments for Public API

```csharp
/// <summary>
/// Applies damage to the player and triggers invincibility.
/// </summary>
/// <param name="damage">Amount of damage to apply</param>
/// <returns>True if damage was applied, false if player is invincible</returns>
public bool TakeDamage(int damage) {
    if (_isInvincible) return false;
    
    _currentHealth -= damage;
    StartInvincibility();
    return true;
}
```

### Inline Comments for Complex Logic

```csharp
// CRITICAL: Reset coyote time when landing to prevent ghost jumps
if (!wasGrounded && isGrounded) {
    _coyoteTimeCounter = _coyoteTimeDuration;
    _leftGroundByJumping = false;
}

// Calculate anti-sliding force to prevent slope drift
// Formula: counterForce = -slopeDirection * gravityAlongSlope * mass
Vector2 slopeDirection = new Vector2(_slopeNormal.y, -_slopeNormal.x).normalized;
float gravityAlongSlope = Vector2.Dot(Physics2D.gravity * _rb.gravityScale, slopeDirection);
Vector2 counterForce = -slopeDirection * gravityAlongSlope * _rb.mass;
```

### TODO Comments

```csharp
// TODO: Refactor this method - too complex (100+ lines)
// TODO: Add sound effects for attack impacts
// FIXME: Wall stick sometimes triggers on corners
// HACK: Temporary workaround for animator transition bug - remove after Unity update
```
