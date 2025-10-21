# Mocking and Test Doubles in Unity

## Overview

Test doubles are objects that replace real dependencies in tests. They help isolate the code being tested and make tests faster, more reliable, and easier to set up.

## Types of Test Doubles

### 1. Dummy
An object passed around but never used. Only fills parameter lists.

```csharp
public void SomeMethod(ILogger logger, int value)
{
    // logger is never used
}

[Test]
public void Test_WithDummyLogger()
{
    var dummy = new DummyLogger(); // Does nothing
    SomeMethod(dummy, 42);
}
```

### 2. Stub
Provides predefined answers to method calls.

```csharp
public class StubHealthSystem : IHealthSystem
{
    public int CurrentHealth => 100; // Always returns 100
    public void TakeDamage(int amount) { } // Does nothing
}

[Test]
public void Test_WithStubHealth()
{
    var stub = new StubHealthSystem();
    var player = new Player(stub);
    
    Assert.AreEqual(100, player.GetHealth());
}
```

### 3. Fake
A working implementation, but simplified (e.g., in-memory database instead of real one).

```csharp
public class FakeDataStore : IDataStore
{
    private Dictionary<string, object> _data = new Dictionary<string, object>();
    
    public void Save(string key, object value)
    {
        _data[key] = value;
    }
    
    public object Load(string key)
    {
        return _data.ContainsKey(key) ? _data[key] : null;
    }
}

[Test]
public void Test_WithFakeDataStore()
{
    var fake = new FakeDataStore();
    var saveSystem = new SaveSystem(fake);
    
    saveSystem.SaveScore(100);
    Assert.AreEqual(100, saveSystem.LoadScore());
}
```

### 4. Mock
Records interactions and verifies they happened correctly.

```csharp
public class MockHealthSystem : IHealthSystem
{
    public int CurrentHealth { get; private set; }
    public int TakeDamageCallCount { get; private set; }
    public int LastDamageAmount { get; private set; }
    
    public void TakeDamage(int amount)
    {
        TakeDamageCallCount++;
        LastDamageAmount = amount;
        CurrentHealth -= amount;
    }
}

[Test]
public void Attack_CallsTakeDamage()
{
    // Arrange
    var mock = new MockHealthSystem();
    var player = new Player(mock);
    
    // Act
    player.Attack(30);
    
    // Assert
    Assert.AreEqual(1, mock.TakeDamageCallCount);
    Assert.AreEqual(30, mock.LastDamageAmount);
}
```

### 5. Spy
Records information about how it was used (partial mock).

```csharp
public class SpyAudioManager : AudioManager
{
    public List<string> PlayedSounds { get; } = new List<string>();
    
    public override void PlaySound(string soundName)
    {
        PlayedSounds.Add(soundName);
        base.PlaySound(soundName); // Also does real work
    }
}

[Test]
public void Jump_PlaysJumpSound()
{
    // Arrange
    var spy = new SpyAudioManager();
    var player = new Player(spy);
    
    // Act
    player.Jump();
    
    // Assert
    Assert.Contains("Jump", spy.PlayedSounds);
}
```

## Creating Testable Dependencies

### Use Interfaces
Define interfaces for dependencies to allow substitution:

```csharp
public interface IHealthSystem
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    void TakeDamage(int amount);
    void Heal(int amount);
    bool IsDead { get; }
}

public class HealthSystem : IHealthSystem
{
    // Real implementation
}

public class MockHealthSystem : IHealthSystem
{
    // Test implementation
}
```

### Constructor Injection
Allow dependencies to be injected through constructor:

```csharp
public class Player
{
    private readonly IHealthSystem _health;
    private readonly IInventory _inventory;
    
    // Production constructor
    public Player() : this(new HealthSystem(), new Inventory()) { }
    
    // Test constructor
    public Player(IHealthSystem health, IInventory inventory)
    {
        _health = health;
        _inventory = inventory;
    }
}

[Test]
public void Test_WithMockDependencies()
{
    var mockHealth = new MockHealthSystem();
    var mockInventory = new MockInventory();
    var player = new Player(mockHealth, mockInventory);
    
    // Test player with controlled dependencies
}
```

### Property Injection
Allow dependencies to be set after construction:

```csharp
public class Enemy
{
    public IPathfinding Pathfinding { get; set; }
    
    public Enemy()
    {
        Pathfinding = new AStarPathfinding(); // Default
    }
}

[Test]
public void Test_WithMockPathfinding()
{
    var enemy = new Enemy();
    enemy.Pathfinding = new MockPathfinding();
    
    // Test enemy with mock pathfinding
}
```

## Mocking Unity Components

### Mocking MonoBehaviours
Unity MonoBehaviours are harder to mock. Use interfaces to abstract behavior:

```csharp
// Instead of depending directly on MonoBehaviour
public interface IPlayerInput
{
    Vector2 GetMovementInput();
    bool GetJumpInput();
}

public class PlayerInputController : MonoBehaviour, IPlayerInput
{
    public Vector2 GetMovementInput()
    {
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }
    
    public bool GetJumpInput()
    {
        return Input.GetButtonDown("Jump");
    }
}

public class MockPlayerInput : IPlayerInput
{
    public Vector2 MovementInput { get; set; }
    public bool JumpInput { get; set; }
    
    public Vector2 GetMovementInput() => MovementInput;
    public bool GetJumpInput() => JumpInput;
}

[Test]
public void Movement_WithInput_MovesPlayer()
{
    // Arrange
    var mockInput = new MockPlayerInput { MovementInput = Vector2.right };
    var player = new PlayerController(mockInput);
    
    // Act
    player.ProcessMovement();
    
    // Assert
    Assert.Greater(player.transform.position.x, 0);
}
```

### Mocking Unity Input
Abstract input system for testing:

```csharp
public interface IInputService
{
    bool GetButton(string buttonName);
    float GetAxis(string axisName);
}

public class UnityInputService : IInputService
{
    public bool GetButton(string buttonName) => Input.GetButton(buttonName);
    public float GetAxis(string axisName) => Input.GetAxis(axisName);
}

public class MockInputService : IInputService
{
    private Dictionary<string, bool> _buttons = new Dictionary<string, bool>();
    private Dictionary<string, float> _axes = new Dictionary<string, float>();
    
    public void SetButton(string buttonName, bool value) => _buttons[buttonName] = value;
    public void SetAxis(string axisName, float value) => _axes[axisName] = value;
    
    public bool GetButton(string buttonName) => _buttons.GetValueOrDefault(buttonName);
    public float GetAxis(string axisName) => _axes.GetValueOrDefault(axisName);
}
```

### Mocking Time
Create a time provider for deterministic time-based tests:

```csharp
public interface ITimeProvider
{
    float DeltaTime { get; }
    float Time { get; }
}

public class UnityTimeProvider : ITimeProvider
{
    public float DeltaTime => Time.deltaTime;
    public float Time => UnityEngine.Time.time;
}

public class MockTimeProvider : ITimeProvider
{
    public float DeltaTime { get; set; }
    public float Time { get; set; }
}

[Test]
public void Timer_AfterOneSecond_Completes()
{
    // Arrange
    var mockTime = new MockTimeProvider { DeltaTime = 0.1f };
    var timer = new Timer(mockTime, duration: 1.0f);
    
    // Act - Simulate 10 frames of 0.1s each
    for (int i = 0; i < 10; i++)
    {
        timer.Update();
    }
    
    // Assert
    Assert.IsTrue(timer.IsComplete);
}
```

## Advanced Mocking Patterns

### Spy Pattern for Verification
Track method calls while maintaining real behavior:

```csharp
public class SpyDamageCalculator : IDamageCalculator
{
    private readonly IDamageCalculator _real;
    
    public List<(int damage, int armor)> CalculationHistory { get; } = new();
    
    public SpyDamageCalculator(IDamageCalculator real)
    {
        _real = real;
    }
    
    public int CalculateDamage(int damage, int armor)
    {
        CalculationHistory.Add((damage, armor));
        return _real.CalculateDamage(damage, armor);
    }
}

[Test]
public void Battle_CalculatesDamageForEachAttack()
{
    // Arrange
    var real = new DamageCalculator();
    var spy = new SpyDamageCalculator(real);
    var battle = new Battle(spy);
    
    // Act
    battle.ExecuteRound();
    
    // Assert
    Assert.AreEqual(3, spy.CalculationHistory.Count, "Should calculate damage 3 times");
}
```

### State-Based Mock
Mock that changes behavior based on state:

```csharp
public class MockEnemy : IEnemy
{
    public int Health { get; set; } = 100;
    public bool IsAggressive { get; set; }
    
    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health < 50)
        {
            IsAggressive = true; // Becomes aggressive when damaged
        }
    }
}

[Test]
public void Enemy_WhenDamagedBelow50Health_BecomesAggressive()
{
    // Arrange
    var enemy = new MockEnemy();
    
    // Act
    enemy.TakeDamage(60);
    
    // Assert
    Assert.IsTrue(enemy.IsAggressive);
}
```

### Behavior Verification Mock
```csharp
public class MockAudioSystem : IAudioSystem
{
    public int PlayCallCount { get; private set; }
    public int StopCallCount { get; private set; }
    public string LastPlayedSound { get; private set; }
    
    public void Play(string soundName)
    {
        PlayCallCount++;
        LastPlayedSound = soundName;
    }
    
    public void Stop()
    {
        StopCallCount++;
    }
    
    public void VerifyPlayCalled(int times, string soundName)
    {
        Assert.AreEqual(times, PlayCallCount, $"Expected Play to be called {times} times");
        Assert.AreEqual(soundName, LastPlayedSound, $"Expected sound '{soundName}' to be played");
    }
}

[Test]
public void GameOver_PlaysGameOverSound()
{
    // Arrange
    var mockAudio = new MockAudioSystem();
    var gameManager = new GameManager(mockAudio);
    
    // Act
    gameManager.EndGame();
    
    // Assert
    mockAudio.VerifyPlayCalled(1, "GameOver");
}
```

## Best Practices

### 1. Keep Mocks Simple
Don't make mocks that are as complex as the real implementation.

### 2. Use Real Objects When Possible
Only mock when:
- The real object is slow (database, network, file I/O)
- The real object is hard to set up
- You need to control specific behavior
- The real object has side effects

### 3. Don't Mock Value Objects
Simple data classes don't need mocking:

```csharp
// DON'T mock this
public class Vector3Data
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

// Just use the real thing
var position = new Vector3Data { X = 1, Y = 2, Z = 3 };
```

### 4. Mock at Architecture Boundaries
Mock external systems, not internal classes:
- Database access
- Network calls
- File I/O
- Unity Input system
- Audio system
- Analytics

### 5. Verify Only What Matters
Don't over-verify. Focus on behavior that affects the test outcome:

```csharp
// BAD: Over-verification
Assert.AreEqual(1, mock.MethodACallCount);
Assert.AreEqual(1, mock.MethodBCallCount);
Assert.AreEqual(1, mock.MethodCCallCount);
Assert.AreEqual("value", mock.LastParameter);

// GOOD: Verify key behavior
Assert.IsTrue(mock.ImportantMethodWasCalled);
```

### 6. One Mock Per Test Focus
Keep tests focused on one interaction:

```csharp
[Test]
public void Attack_DamagesEnemy()
{
    var mockEnemy = new MockEnemy();
    player.Attack(mockEnemy);
    Assert.AreEqual(1, mockEnemy.TakeDamageCallCount);
}

[Test]
public void Attack_PlaysAttackSound()
{
    var mockAudio = new MockAudioSystem();
    player.Attack(mockAudio);
    Assert.AreEqual("Attack", mockAudio.LastPlayedSound);
}
```

## Common Pitfalls

### Over-Mocking
Don't mock everything. Use real objects when they're simple and fast.

### Brittle Tests
Mocks tied to implementation details break when refactoring:

```csharp
// FRAGILE: Tied to implementation
Assert.AreEqual(3, mock.InternalMethodCallCount);

// BETTER: Test observable behavior
Assert.AreEqual(expectedResult, actualResult);
```

### Testing the Mock
Make sure you're testing real code, not the mock:

```csharp
// BAD: Only testing mock behavior
[Test]
public void MockReturns100()
{
    var mock = new MockHealthSystem { Health = 100 };
    Assert.AreEqual(100, mock.Health); // Useless test
}

// GOOD: Testing real code with mock
[Test]
public void Player_WithFullHealth_CanTakeDamage()
{
    var mock = new MockHealthSystem { Health = 100 };
    var player = new Player(mock);
    player.TakeDamage(50);
    Assert.AreEqual(50, mock.Health);
}
```
