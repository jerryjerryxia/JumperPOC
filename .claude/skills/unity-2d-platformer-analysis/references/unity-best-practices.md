# Unity Best Practices

## Code Organization

### Naming Conventions
- **Classes**: PascalCase (`PlayerController`, `EnemyManager`)
- **Methods**: PascalCase (`MovePlayer`, `TakeDamage`)
- **Private fields**: camelCase with underscore prefix (`_currentHealth`, `_isGrounded`)
- **Public properties**: PascalCase (`CurrentHealth`, `IsGrounded`)
- **Constants**: PascalCase or UPPER_SNAKE_CASE (`MaxHealth`, `JUMP_FORCE`)
- **Prefabs**: Descriptive names (`Player`, `Enemy_Goblin`, `Platform_Moving`)

### File Structure
- One MonoBehaviour per file
- File name must match class name
- Organize by feature/system, not by type
- Keep related scripts together

## Unity Lifecycle

### Initialization Order
```csharp
// Initialization
Awake()      // Initialize references, set up object
OnEnable()   // Subscribe to events
Start()      // Initialize after all Awake calls

// During Play
FixedUpdate()  // Physics calculations (fixed timestep)
Update()       // Per-frame logic
LateUpdate()   // Camera following, post-processing

// Cleanup
OnDisable()  // Unsubscribe from events
OnDestroy()  // Final cleanup
```

### Best Practices
- Use `Awake()` for internal initialization
- Use `Start()` when referencing other objects
- Use `FixedUpdate()` for physics (Rigidbody movement)
- Use `Update()` for input and non-physics logic
- Use `LateUpdate()` for camera following
- Always unsubscribe from events in `OnDisable()`

## Performance Optimization

### Caching References
❌ **Bad:**
```csharp
void Update() {
    GetComponent<Rigidbody2D>().velocity = new Vector2(speed, 0);
    Camera.main.transform.position = transform.position;
}
```

✅ **Good:**
```csharp
private Rigidbody2D _rb;
private Transform _cameraTransform;

void Awake() {
    _rb = GetComponent<Rigidbody2D>();
    _cameraTransform = Camera.main.transform;
}

void Update() {
    _rb.velocity = new Vector2(speed, 0);
    _cameraTransform.position = transform.position;
}
```

### Avoid Expensive Operations in Update
- Cache `GetComponent<>()` calls
- Cache `Camera.main`
- Don't use `Find()` or `FindObjectOfType()` in Update
- Use object pooling for frequently instantiated objects
- Minimize string operations and allocations

### Physics Optimization
- Use appropriate collider types (BoxCollider2D is fastest)
- Disable unused colliders
- Use layers for collision filtering
- Set static objects to Static rigidbody type
- Use CompositeCollider2D for tilemap performance

## Serialization

### Field Serialization
❌ **Bad:**
```csharp
public float health;  // Exposed unnecessarily
private float speed;  // Not serialized when needed
```

✅ **Good:**
```csharp
[SerializeField] private float _health;
[SerializeField] private float _speed;

public float Health => _health;  // Property for controlled access
```

### ScriptableObjects for Data
Use ScriptableObjects for:
- Game configuration (movement speeds, jump heights)
- Enemy stats and behaviors
- Item definitions
- Audio clip collections
- Shared data between scenes

## Input Handling

### Old Input Manager
```csharp
void Update() {
    float horizontal = Input.GetAxisRaw("Horizontal");
    if (Input.GetButtonDown("Jump")) {
        Jump();
    }
}
```

### New Input System (Preferred)
```csharp
private PlayerInputActions _inputActions;

void Awake() {
    _inputActions = new PlayerInputActions();
}

void OnEnable() {
    _inputActions.Enable();
    _inputActions.Player.Jump.performed += OnJump;
}

void OnDisable() {
    _inputActions.Player.Jump.performed -= OnJump;
    _inputActions.Disable();
}

private void OnJump(InputAction.CallbackContext context) {
    Jump();
}
```

## Event Management

### C# Events Pattern
```csharp
// Define delegate and event
public delegate void HealthChangedDelegate(int newHealth);
public event HealthChangedDelegate OnHealthChanged;

// Invoke event safely
OnHealthChanged?.Invoke(_currentHealth);

// Subscribe/Unsubscribe
void OnEnable() {
    player.OnHealthChanged += HandleHealthChanged;
}

void OnDisable() {
    player.OnHealthChanged -= HandleHealthChanged;
}
```

### UnityEvent Pattern
```csharp
using UnityEngine.Events;

[Serializable]
public class HealthEvent : UnityEvent<int> { }

[SerializeField] private HealthEvent _onHealthChanged;

// Invoke
_onHealthChanged?.Invoke(_currentHealth);
```

## Common Anti-Patterns to Avoid

### 1. Public Fields Everywhere
Use `[SerializeField] private` instead of public for inspector values

### 2. Singleton Overuse
Only use singletons for truly global managers (GameManager, AudioManager)

### 3. Update() for Everything
Use coroutines, events, or Invoke for delayed/periodic actions

### 4. String Comparisons for Tags
❌ `if (other.gameObject.tag == "Player")`
✅ `if (other.CompareTag("Player"))`

### 5. Instantiate in Loops
Use object pooling for frequently created/destroyed objects

### 6. Empty Unity Callbacks
Remove empty Update(), FixedUpdate(), etc. to save overhead

### 7. Finding Objects by Name
Use references, tags, or layers instead of GameObject.Find()

## Memory Management

### Avoiding Garbage Collection Spikes
- Cache collections (Lists, Arrays) instead of creating new ones
- Use object pooling for projectiles, particles, enemies
- Avoid string concatenation in Update
- Use StringBuilder for complex string operations
- Minimize boxing/unboxing operations

### Object Pooling Pattern
```csharp
public class ObjectPool : MonoBehaviour {
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _initialSize = 10;
    private Queue<GameObject> _pool;

    void Awake() {
        _pool = new Queue<GameObject>();
        for (int i = 0; i < _initialSize; i++) {
            CreateNewObject();
        }
    }

    private GameObject CreateNewObject() {
        GameObject obj = Instantiate(_prefab);
        obj.SetActive(false);
        _pool.Enqueue(obj);
        return obj;
    }

    public GameObject Get() {
        if (_pool.Count == 0) {
            CreateNewObject();
        }
        GameObject obj = _pool.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj) {
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }
}
```

## Editor Tools

### Custom Inspector
```csharp
#if UNITY_EDITOR
[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        
        PlayerController controller = (PlayerController)target;
        if (GUILayout.Button("Reset to Default")) {
            controller.ResetToDefault();
        }
    }
}
#endif
```

### Gizmos for Debugging
```csharp
void OnDrawGizmos() {
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, detectionRadius);
}

void OnDrawGizmosSelected() {
    Gizmos.color = Color.red;
    Gizmos.DrawLine(transform.position, targetPosition);
}
```

## Error Handling

### Null Checks
```csharp
// Defensive programming
void Start() {
    if (_rigidbody == null) {
        Debug.LogError($"{name}: Rigidbody2D not found!", this);
        enabled = false;
        return;
    }
}

// Null-conditional operator
_animator?.SetBool("IsGrounded", _isGrounded);

// Null-coalescing assignment (C# 8.0+)
_rigidbody ??= GetComponent<Rigidbody2D>();
```

### Logging
```csharp
Debug.Log("Informational message");
Debug.LogWarning("Warning message");
Debug.LogError("Error message");
Debug.LogException(exception);

// Context-aware logging (click to highlight in hierarchy)
Debug.LogError("Missing reference!", this);
```
