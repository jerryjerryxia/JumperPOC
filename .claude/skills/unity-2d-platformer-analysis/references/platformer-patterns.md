# 2D Platformer Patterns

Common design patterns and implementations for 2D platformer games in Unity.

## Movement Mechanics

### Basic Ground Movement
```csharp
public class PlayerMovement : MonoBehaviour {
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _acceleration = 50f;
    [SerializeField] private float _deceleration = 50f;
    
    private Rigidbody2D _rb;
    private float _horizontalInput;
    
    void FixedUpdate() {
        float targetSpeed = _horizontalInput * _moveSpeed;
        float speedDiff = targetSpeed - _rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? _acceleration : _deceleration;
        float movement = speedDiff * accelRate * Time.fixedDeltaTime;
        
        _rb.AddForce(movement * Vector2.right);
    }
}
```

### Ground Detection (Multiple Methods)

**Method 1: Raycast**
```csharp
[SerializeField] private LayerMask _groundLayer;
[SerializeField] private float _groundCheckDistance = 0.1f;
[SerializeField] private Vector2 _groundCheckSize = new Vector2(0.5f, 0.1f);

private bool IsGrounded() {
    return Physics2D.BoxCast(
        transform.position, 
        _groundCheckSize, 
        0f, 
        Vector2.down, 
        _groundCheckDistance, 
        _groundLayer
    );
}
```

**Method 2: Collision Contacts**
```csharp
private bool _isGrounded;
private int _groundContactCount;

void OnCollisionEnter2D(Collision2D collision) {
    if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) {
        _groundContactCount++;
        _isGrounded = true;
    }
}

void OnCollisionExit2D(Collision2D collision) {
    if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) {
        _groundContactCount--;
        if (_groundContactCount <= 0) {
            _groundContactCount = 0;
            _isGrounded = false;
        }
    }
}
```

### Jump Mechanics with Feel

**Coyote Time** (grace period after leaving ledge)
```csharp
[SerializeField] private float _coyoteTime = 0.15f;
private float _coyoteTimeCounter;

void Update() {
    if (IsGrounded()) {
        _coyoteTimeCounter = _coyoteTime;
    } else {
        _coyoteTimeCounter -= Time.deltaTime;
    }
}

private bool CanJump() {
    return _coyoteTimeCounter > 0f;
}
```

**Jump Buffering** (early jump input)
```csharp
[SerializeField] private float _jumpBufferTime = 0.2f;
private float _jumpBufferCounter;

void Update() {
    if (Input.GetButtonDown("Jump")) {
        _jumpBufferCounter = _jumpBufferTime;
    } else {
        _jumpBufferCounter -= Time.deltaTime;
    }
    
    if (_jumpBufferCounter > 0f && CanJump()) {
        Jump();
        _jumpBufferCounter = 0f;
    }
}
```

**Variable Jump Height** (hold for higher jump)
```csharp
[SerializeField] private float _jumpForce = 10f;
[SerializeField] private float _jumpCutMultiplier = 0.5f;

void Update() {
    if (Input.GetButtonDown("Jump") && CanJump()) {
        _rb.velocity = new Vector2(_rb.velocity.x, _jumpForce);
    }
    
    // Cut jump short if button released
    if (Input.GetButtonUp("Jump") && _rb.velocity.y > 0) {
        _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y * _jumpCutMultiplier);
    }
}
```

**Better Jump Feel**
```csharp
[SerializeField] private float _fallMultiplier = 2.5f;
[SerializeField] private float _lowJumpMultiplier = 2f;

void FixedUpdate() {
    // Enhanced falling
    if (_rb.velocity.y < 0) {
        _rb.velocity += Vector2.up * Physics2D.gravity.y * (_fallMultiplier - 1) * Time.fixedDeltaTime;
    }
    // Shortened jump if not holding button
    else if (_rb.velocity.y > 0 && !Input.GetButton("Jump")) {
        _rb.velocity += Vector2.up * Physics2D.gravity.y * (_lowJumpMultiplier - 1) * Time.fixedDeltaTime;
    }
}
```

## Combat System

### Basic Attack System
```csharp
public class PlayerCombat : MonoBehaviour {
    [SerializeField] private Transform _attackPoint;
    [SerializeField] private float _attackRange = 0.5f;
    [SerializeField] private LayerMask _enemyLayers;
    [SerializeField] private int _attackDamage = 10;
    [SerializeField] private float _attackCooldown = 0.5f;
    
    private float _nextAttackTime;
    
    void Update() {
        if (Time.time >= _nextAttackTime && Input.GetButtonDown("Attack")) {
            Attack();
            _nextAttackTime = Time.time + _attackCooldown;
        }
    }
    
    void Attack() {
        // Detect enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            _attackPoint.position, 
            _attackRange, 
            _enemyLayers
        );
        
        // Apply damage
        foreach (Collider2D enemy in hitEnemies) {
            if (enemy.TryGetComponent<IEnemy>(out var enemyComponent)) {
                enemyComponent.TakeDamage(_attackDamage);
            }
        }
    }
    
    void OnDrawGizmosSelected() {
        if (_attackPoint != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_attackPoint.position, _attackRange);
        }
    }
}
```

### Health System
```csharp
public class Health : MonoBehaviour {
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _invincibilityDuration = 1f;
    
    private int _currentHealth;
    private bool _isInvincible;
    private float _invincibilityTimer;
    
    public event Action<int> OnHealthChanged;
    public event Action OnDeath;
    
    void Awake() {
        _currentHealth = _maxHealth;
    }
    
    void Update() {
        if (_isInvincible) {
            _invincibilityTimer -= Time.deltaTime;
            if (_invincibilityTimer <= 0) {
                _isInvincible = false;
            }
        }
    }
    
    public void TakeDamage(int damage) {
        if (_isInvincible || _currentHealth <= 0) return;
        
        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        OnHealthChanged?.Invoke(_currentHealth);
        
        if (_currentHealth <= 0) {
            Die();
        } else {
            StartInvincibility();
        }
    }
    
    public void Heal(int amount) {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        OnHealthChanged?.Invoke(_currentHealth);
    }
    
    private void StartInvincibility() {
        _isInvincible = true;
        _invincibilityTimer = _invincibilityDuration;
    }
    
    private void Die() {
        OnDeath?.Invoke();
        // Handle death (animation, respawn, etc.)
    }
}
```

### Knockback System
```csharp
public class Knockback : MonoBehaviour {
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private float _knockbackForce = 10f;
    [SerializeField] private float _knockbackDuration = 0.2f;
    
    private bool _isKnockedBack;
    
    public void ApplyKnockback(Vector2 direction) {
        if (_isKnockedBack) return;
        
        StartCoroutine(KnockbackRoutine(direction));
    }
    
    private IEnumerator KnockbackRoutine(Vector2 direction) {
        _isKnockedBack = true;
        _rb.velocity = Vector2.zero;
        _rb.AddForce(direction.normalized * _knockbackForce, ForceMode2D.Impulse);
        
        yield return new WaitForSeconds(_knockbackDuration);
        
        _isKnockedBack = false;
    }
    
    public bool IsKnockedBack => _isKnockedBack;
}
```

## Enemy AI Patterns

### Patrol Behavior
```csharp
public class PatrolAI : MonoBehaviour {
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _waypointReachedDistance = 0.1f;
    
    private int _currentWaypointIndex;
    private Rigidbody2D _rb;
    
    void FixedUpdate() {
        if (_waypoints.Length == 0) return;
        
        Transform targetWaypoint = _waypoints[_currentWaypointIndex];
        Vector2 direction = (targetWaypoint.position - transform.position).normalized;
        _rb.velocity = direction * _moveSpeed;
        
        if (Vector2.Distance(transform.position, targetWaypoint.position) < _waypointReachedDistance) {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
        }
    }
}
```

### Player Detection
```csharp
public class PlayerDetection : MonoBehaviour {
    [SerializeField] private float _detectionRadius = 5f;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private float _losePlayerDistance = 7f;
    
    private Transform _player;
    private bool _playerDetected;
    
    public bool PlayerDetected => _playerDetected;
    public Transform Player => _player;
    
    void Update() {
        Collider2D playerCollider = Physics2D.OverlapCircle(
            transform.position, 
            _detectionRadius, 
            _playerLayer
        );
        
        if (playerCollider != null) {
            _player = playerCollider.transform;
            _playerDetected = true;
        } else if (_playerDetected) {
            // Check if player moved too far
            if (_player != null && Vector2.Distance(transform.position, _player.position) > _losePlayerDistance) {
                _playerDetected = false;
                _player = null;
            }
        }
    }
    
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _losePlayerDistance);
    }
}
```

### Head Stomp Mechanic
```csharp
public class HeadStompReceiver : MonoBehaviour {
    [SerializeField] private float _bounceForce = 15f;
    [SerializeField] private int _damage = 10;
    
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            // Check if player is above enemy
            if (collision.transform.position.y > transform.position.y) {
                // Bounce player
                Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
                if (playerRb != null) {
                    playerRb.velocity = new Vector2(playerRb.velocity.x, _bounceForce);
                }
                
                // Damage enemy
                GetComponent<Health>()?.TakeDamage(_damage);
            }
        }
    }
}
```

## Level Design Systems

### Checkpoint System
```csharp
public class Checkpoint : MonoBehaviour {
    [SerializeField] private int _checkpointID;
    
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            CheckpointManager.Instance.SetCheckpoint(_checkpointID, transform.position);
        }
    }
}

public class CheckpointManager : MonoBehaviour {
    public static CheckpointManager Instance { get; private set; }
    
    private int _currentCheckpointID = -1;
    private Vector3 _currentCheckpointPosition;
    
    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void SetCheckpoint(int id, Vector3 position) {
        if (id > _currentCheckpointID) {
            _currentCheckpointID = id;
            _currentCheckpointPosition = position;
        }
    }
    
    public Vector3 GetCheckpointPosition() {
        return _currentCheckpointPosition;
    }
}
```

### Respawn System
```csharp
public class RespawnManager : MonoBehaviour {
    [SerializeField] private Transform _player;
    [SerializeField] private float _respawnDelay = 1f;
    
    private Health _playerHealth;
    
    void Start() {
        _playerHealth = _player.GetComponent<Health>();
        _playerHealth.OnDeath += HandlePlayerDeath;
    }
    
    void OnDestroy() {
        if (_playerHealth != null) {
            _playerHealth.OnDeath -= HandlePlayerDeath;
        }
    }
    
    private void HandlePlayerDeath() {
        StartCoroutine(RespawnRoutine());
    }
    
    private IEnumerator RespawnRoutine() {
        yield return new WaitForSeconds(_respawnDelay);
        
        Vector3 respawnPosition = CheckpointManager.Instance.GetCheckpointPosition();
        _player.position = respawnPosition;
        _playerHealth.Heal(_playerHealth.MaxHealth);
    }
}
```

### Level Transition
```csharp
public class LevelTransition : MonoBehaviour {
    [SerializeField] private string _targetSceneName;
    [SerializeField] private int _spawnPointID;
    
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            TransitionToLevel();
        }
    }
    
    private void TransitionToLevel() {
        LevelManager.Instance.LoadLevel(_targetSceneName, _spawnPointID);
    }
}
```

## State Machine Pattern

### Player State Machine
```csharp
public enum PlayerState { Idle, Running, Jumping, Falling, Attacking }

public class PlayerStateMachine : MonoBehaviour {
    private PlayerState _currentState;
    private Dictionary<PlayerState, IState> _states;
    
    void Awake() {
        _states = new Dictionary<PlayerState, IState> {
            { PlayerState.Idle, new IdleState(this) },
            { PlayerState.Running, new RunningState(this) },
            { PlayerState.Jumping, new JumpingState(this) },
            { PlayerState.Falling, new FallingState(this) },
            { PlayerState.Attacking, new AttackingState(this) }
        };
        
        TransitionTo(PlayerState.Idle);
    }
    
    void Update() {
        _states[_currentState].Update();
    }
    
    public void TransitionTo(PlayerState newState) {
        if (_currentState == newState) return;
        
        _states[_currentState]?.Exit();
        _currentState = newState;
        _states[_currentState]?.Enter();
    }
}

public interface IState {
    void Enter();
    void Update();
    void Exit();
}
```

## Camera Systems

### Camera Follow (Smooth)
```csharp
public class CameraFollow : MonoBehaviour {
    [SerializeField] private Transform _target;
    [SerializeField] private float _smoothSpeed = 0.125f;
    [SerializeField] private Vector3 _offset;
    [SerializeField] private Vector2 _minBounds;
    [SerializeField] private Vector2 _maxBounds;
    
    void LateUpdate() {
        if (_target == null) return;
        
        Vector3 desiredPosition = _target.position + _offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed);
        
        // Clamp to bounds
        smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, _minBounds.x, _maxBounds.x);
        smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, _minBounds.y, _maxBounds.y);
        
        transform.position = smoothedPosition;
    }
}
```

## Collectibles

### Collectible Base Class
```csharp
public abstract class Collectible : MonoBehaviour {
    [SerializeField] protected int _value = 1;
    [SerializeField] protected AudioClip _collectSound;
    
    protected virtual void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            Collect(collision.gameObject);
        }
    }
    
    protected virtual void Collect(GameObject player) {
        // Play sound
        if (_collectSound != null) {
            AudioSource.PlayClipAtPoint(_collectSound, transform.position);
        }
        
        // Override in derived classes for specific behavior
        OnCollected(player);
        
        Destroy(gameObject);
    }
    
    protected abstract void OnCollected(GameObject player);
}

public class Coin : Collectible {
    protected override void OnCollected(GameObject player) {
        GameManager.Instance.AddCoins(_value);
    }
}

public class HealthPickup : Collectible {
    protected override void OnCollected(GameObject player) {
        player.GetComponent<Health>()?.Heal(_value);
    }
}
```
