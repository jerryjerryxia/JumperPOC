# Unity Test Patterns

## Table of Contents
- Testing Game Logic
- Testing MonoBehaviours
- Testing Coroutines
- Testing Unity Events
- Testing Physics
- Testing UI
- Testing ScriptableObjects
- Testing Static Classes
- Testing Singletons

## Testing Game Logic

### Health System
```csharp
[Test]
public void TakeDamage_WithPositiveAmount_ReducesHealth()
{
    // Arrange
    var health = new HealthSystem(100);
    
    // Act
    health.TakeDamage(30);
    
    // Assert
    Assert.AreEqual(70, health.CurrentHealth);
}

[Test]
public void TakeDamage_WhenHealthReachesZero_MarksDead()
{
    // Arrange
    var health = new HealthSystem(50);
    
    // Act
    health.TakeDamage(60);
    
    // Assert
    Assert.AreEqual(0, health.CurrentHealth);
    Assert.IsTrue(health.IsDead);
}

[Test]
public void Heal_WhenBelowMax_IncreasesHealth()
{
    // Arrange
    var health = new HealthSystem(100);
    health.TakeDamage(30);
    
    // Act
    health.Heal(20);
    
    // Assert
    Assert.AreEqual(90, health.CurrentHealth);
}

[Test]
public void Heal_WhenAtMax_DoesNotExceedMax()
{
    // Arrange
    var health = new HealthSystem(100);
    health.TakeDamage(10);
    
    // Act
    health.Heal(50);
    
    // Assert
    Assert.AreEqual(100, health.CurrentHealth);
}
```

### Inventory System
```csharp
[Test]
public void AddItem_WhenSpaceAvailable_ReturnsTrue()
{
    // Arrange
    var inventory = new Inventory(capacity: 10);
    var item = new Item("Sword");
    
    // Act
    var added = inventory.AddItem(item);
    
    // Assert
    Assert.IsTrue(added);
    Assert.AreEqual(1, inventory.ItemCount);
    Assert.IsTrue(inventory.Contains(item));
}

[Test]
public void AddItem_WhenInventoryFull_ReturnsFalse()
{
    // Arrange
    var inventory = new Inventory(capacity: 1);
    inventory.AddItem(new Item("Sword"));
    
    // Act
    var added = inventory.AddItem(new Item("Shield"));
    
    // Assert
    Assert.IsFalse(added);
    Assert.AreEqual(1, inventory.ItemCount);
}

[Test]
public void RemoveItem_WhenItemExists_RemovesAndReturnsTrue()
{
    // Arrange
    var inventory = new Inventory(capacity: 10);
    var item = new Item("Sword");
    inventory.AddItem(item);
    
    // Act
    var removed = inventory.RemoveItem(item);
    
    // Assert
    Assert.IsTrue(removed);
    Assert.AreEqual(0, inventory.ItemCount);
    Assert.IsFalse(inventory.Contains(item));
}
```

### Score System
```csharp
[Test]
public void AddScore_IncreasesScore()
{
    // Arrange
    var scoreSystem = new ScoreSystem();
    
    // Act
    scoreSystem.AddScore(100);
    
    // Assert
    Assert.AreEqual(100, scoreSystem.CurrentScore);
}

[Test]
public void AddScore_WithMultiplier_MultipliesScore()
{
    // Arrange
    var scoreSystem = new ScoreSystem();
    scoreSystem.SetMultiplier(2.0f);
    
    // Act
    scoreSystem.AddScore(100);
    
    // Assert
    Assert.AreEqual(200, scoreSystem.CurrentScore);
}

[TestCase(100, 1000, true)]
[TestCase(500, 1000, false)]
[TestCase(1000, 1000, true)]
public void HasReachedScore_ReturnsCorrectResult(int current, int target, bool expected)
{
    // Arrange
    var scoreSystem = new ScoreSystem();
    scoreSystem.AddScore(current);
    
    // Act
    var result = scoreSystem.HasReachedScore(target);
    
    // Assert
    Assert.AreEqual(expected, result);
}
```

## Testing MonoBehaviours

### Component Initialization
```csharp
[Test]
public void Awake_InitializesComponentCorrectly()
{
    // Arrange
    var gameObject = new GameObject();
    
    // Act
    var component = gameObject.AddComponent<MyComponent>();
    
    // Assert
    Assert.IsNotNull(component);
    Assert.AreEqual(0, component.InitialValue);
    
    // Cleanup
    Object.DestroyImmediate(gameObject);
}
```

### Component Dependencies
```csharp
[Test]
public void GetComponent_FindsRequiredComponent()
{
    // Arrange
    var gameObject = new GameObject();
    var rigidbody = gameObject.AddComponent<Rigidbody2D>();
    
    // Act
    var component = gameObject.AddComponent<PlayerController>();
    
    // Assert
    Assert.IsNotNull(component.GetRigidbody());
    Assert.AreEqual(rigidbody, component.GetRigidbody());
    
    // Cleanup
    Object.DestroyImmediate(gameObject);
}
```

### Public Methods
```csharp
[Test]
public void Move_UpdatesPosition()
{
    // Arrange
    var gameObject = new GameObject();
    var component = gameObject.AddComponent<PlayerController>();
    var initialPosition = component.transform.position;
    
    // Act
    component.Move(Vector2.right);
    
    // Assert
    Assert.AreNotEqual(initialPosition, component.transform.position);
    
    // Cleanup
    Object.DestroyImmediate(gameObject);
}
```

## Testing Coroutines

### Basic Coroutine Test
```csharp
[UnityTest]
public IEnumerator Coroutine_CompletesSuccessfully()
{
    // Arrange
    var gameObject = new GameObject();
    var component = gameObject.AddComponent<MyComponent>();
    
    // Act
    yield return component.MyCoroutine();
    
    // Assert
    Assert.IsTrue(component.CoroutineCompleted);
    
    // Cleanup
    Object.DestroyImmediate(gameObject);
}
```

### Delayed Coroutine
```csharp
[UnityTest]
public IEnumerator DelayedAction_WaitsCorrectDuration()
{
    // Arrange
    var gameObject = new GameObject();
    var component = gameObject.AddComponent<MyComponent>();
    var startTime = Time.time;
    
    // Act
    yield return component.DelayedAction(1.0f);
    
    var elapsed = Time.time - startTime;
    
    // Assert
    Assert.GreaterOrEqual(elapsed, 1.0f);
    Assert.IsTrue(component.ActionExecuted);
    
    // Cleanup
    Object.DestroyImmediate(gameObject);
}
```

### Frame-by-Frame Updates
```csharp
[UnityTest]
public IEnumerator UpdateValue_IncreasesOverFrames()
{
    // Arrange
    var gameObject = new GameObject();
    var component = gameObject.AddComponent<MyComponent>();
    var initialValue = component.Value;
    
    // Act - Wait multiple frames
    yield return null;
    yield return null;
    yield return null;
    
    // Assert
    Assert.Greater(component.Value, initialValue);
    
    // Cleanup
    Object.DestroyImmediate(gameObject);
}
```

## Testing Unity Events

### Event Invocation
```csharp
[Test]
public void OnDamaged_InvokesEvent()
{
    // Arrange
    var gameObject = new GameObject();
    var component = gameObject.AddComponent<Health>();
    var eventInvoked = false;
    component.OnDamaged.AddListener(() => eventInvoked = true);
    
    // Act
    component.TakeDamage(10);
    
    // Assert
    Assert.IsTrue(eventInvoked);
    
    // Cleanup
    Object.DestroyImmediate(gameObject);
}

[Test]
public void OnDeath_InvokesEventWhenHealthReachesZero()
{
    // Arrange
    var gameObject = new GameObject();
    var component = gameObject.AddComponent<Health>();
    var deathEventInvoked = false;
    component.OnDeath.AddListener(() => deathEventInvoked = true);
    
    // Act
    component.TakeDamage(component.MaxHealth);
    
    // Assert
    Assert.IsTrue(deathEventInvoked);
    
    // Cleanup
    Object.DestroyImmediate(gameObject);
}
```

### Event Parameters
```csharp
[Test]
public void OnScoreChanged_PassesCorrectValue()
{
    // Arrange
    var gameObject = new GameObject();
    var component = gameObject.AddComponent<ScoreManager>();
    var receivedScore = 0;
    component.OnScoreChanged.AddListener(score => receivedScore = score);
    
    // Act
    component.AddScore(100);
    
    // Assert
    Assert.AreEqual(100, receivedScore);
    
    // Cleanup
    Object.DestroyImmediate(gameObject);
}
```

## Testing Physics

### Collision Detection
```csharp
[UnityTest]
public IEnumerator OnCollisionEnter_DetectsCollision()
{
    // Arrange
    var obj1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
    obj1.AddComponent<Rigidbody>();
    var component = obj1.AddComponent<CollisionDetector>();
    
    var obj2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
    obj2.transform.position = Vector3.up * 5;
    obj2.AddComponent<Rigidbody>();
    
    // Act - Wait for collision
    yield return new WaitForSeconds(1.0f);
    
    // Assert
    Assert.IsTrue(component.HasCollided);
    
    // Cleanup
    Object.Destroy(obj1);
    Object.Destroy(obj2);
}
```

### Trigger Detection
```csharp
[UnityTest]
public IEnumerator OnTriggerEnter_DetectsTrigger()
{
    // Arrange
    var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
    trigger.GetComponent<Collider>().isTrigger = true;
    var component = trigger.AddComponent<TriggerDetector>();
    
    var player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    player.transform.position = Vector3.up * 5;
    player.AddComponent<Rigidbody>();
    
    // Act - Wait for trigger
    yield return new WaitForSeconds(1.0f);
    
    // Assert
    Assert.IsTrue(component.WasTriggered);
    
    // Cleanup
    Object.Destroy(trigger);
    Object.Destroy(player);
}
```

## Testing UI

### Button Click
```csharp
[Test]
public void Button_WhenClicked_InvokesAction()
{
    // Arrange
    var gameObject = new GameObject();
    var button = gameObject.AddComponent<UnityEngine.UI.Button>();
    var clicked = false;
    button.onClick.AddListener(() => clicked = true);
    
    // Act
    button.onClick.Invoke();
    
    // Assert
    Assert.IsTrue(clicked);
    
    // Cleanup
    Object.DestroyImmediate(gameObject);
}
```

### Text Update
```csharp
[Test]
public void UpdateScoreText_UpdatesTextComponent()
{
    // Arrange
    var gameObject = new GameObject();
    var textComponent = gameObject.AddComponent<TMPro.TextMeshProUGUI>();
    var uiManager = gameObject.AddComponent<UIManager>();
    
    // Act
    uiManager.UpdateScoreText(100);
    
    // Assert
    Assert.AreEqual("Score: 100", textComponent.text);
    
    // Cleanup
    Object.DestroyImmediate(gameObject);
}
```

## Testing ScriptableObjects

### ScriptableObject Values
```csharp
[Test]
public void ScriptableObject_HasExpectedValues()
{
    // Arrange & Act
    var config = ScriptableObject.CreateInstance<GameConfig>();
    config.MaxHealth = 100;
    config.Speed = 5.0f;
    
    // Assert
    Assert.AreEqual(100, config.MaxHealth);
    Assert.AreEqual(5.0f, config.Speed);
    
    // Cleanup
    Object.DestroyImmediate(config);
}
```

## Testing Static Classes

### Utility Methods
```csharp
[Test]
public void CalculateDamage_WithArmor_ReducesDamage()
{
    // Arrange
    var baseDamage = 100;
    var armor = 50;
    
    // Act
    var result = DamageCalculator.CalculateDamage(baseDamage, armor);
    
    // Assert
    Assert.Less(result, baseDamage);
}

[TestCase(100, 0, 100)]
[TestCase(100, 50, 50)]
[TestCase(100, 100, 0)]
public void CalculateDamage_WithVariousArmor_ReturnsExpectedDamage(
    int damage, int armor, int expected)
{
    // Act
    var result = DamageCalculator.CalculateDamage(damage, armor);
    
    // Assert
    Assert.AreEqual(expected, result);
}
```

## Testing Singletons

### Singleton Access
```csharp
[Test]
public void Instance_ReturnsValidInstance()
{
    // Arrange
    var gameObject = new GameObject();
    var instance = gameObject.AddComponent<GameManager>();
    GameManager.SetInstanceForTesting(instance);
    
    // Act
    var retrieved = GameManager.Instance;
    
    // Assert
    Assert.IsNotNull(retrieved);
    Assert.AreEqual(instance, retrieved);
    
    // Cleanup
    GameManager.ClearInstanceForTesting();
    Object.DestroyImmediate(gameObject);
}
```

### Singleton State
```csharp
[Test]
public void Singleton_MaintainsState()
{
    // Arrange
    var gameObject = new GameObject();
    var instance = gameObject.AddComponent<GameManager>();
    GameManager.SetInstanceForTesting(instance);
    
    // Act
    GameManager.Instance.SetScore(100);
    var score = GameManager.Instance.GetScore();
    
    // Assert
    Assert.AreEqual(100, score);
    
    // Cleanup
    GameManager.ClearInstanceForTesting();
    Object.DestroyImmediate(gameObject);
}
```
