# Unity Testing Best Practices

## Test Organization

### File Structure
- Place tests in a `Tests` folder at the same level as the scripts they test
- Use `Editor` folder for EditMode tests, `Runtime` folder for PlayMode tests
- Name test files: `{ClassName}Tests.cs`

### Test Naming Convention
Use descriptive names that explain what's being tested:
```
{MethodName}_When{Condition}_Should{ExpectedBehavior}
```

Examples:
- `TakeDamage_WhenHealthAboveZero_ReducesHealth`
- `AddItem_WhenInventoryFull_ReturnsFalse`
- `Jump_WhenGrounded_AppliesUpwardForce`

## EditMode vs PlayMode Tests

### EditMode Tests (Faster, Preferred)
- Run outside Play Mode
- No access to Unity lifecycle (Start, Update, etc.)
- Use for: Pure logic, data structures, calculations, utility functions
- Runs in milliseconds

### PlayMode Tests (Slower, When Necessary)
- Run in Play Mode simulation
- Full Unity lifecycle available
- Use for: Physics, animations, coroutines, scene-based behavior
- Runs in seconds

**Rule of thumb:** Use EditMode unless you absolutely need PlayMode features.

## Common Unity Testing Patterns

### Testing MonoBehaviours

**Create and destroy in SetUp/TearDown:**
```csharp
[SetUp]
public void SetUp()
{
    _gameObject = new GameObject();
    _component = _gameObject.AddComponent<MyComponent>();
}

[TearDown]
public void TearDown()
{
    Object.DestroyImmediate(_gameObject);
}
```

### Testing Private Methods
Don't test private methods directly. Test public behavior that uses them.
If you must access private state for assertions, use:
- Reflection (slow, fragile)
- Internal visibility + `[assembly: InternalsVisibleTo("Tests")]` (better)
- Refactor to make testable (best)

### Testing Static State
Reset static state in SetUp to ensure test isolation:
```csharp
[SetUp]
public void SetUp()
{
    GameManager.ResetState();
}
```

### Testing Singletons
Use dependency injection or a testable singleton pattern:
```csharp
public class GameManager
{
    public static GameManager Instance { get; private set; }
    
    public static void SetInstance(GameManager instance)
    {
        Instance = instance; // Allow tests to inject
    }
}
```

### Testing Coroutines
Use `[UnityTest]` and return `IEnumerator`:
```csharp
[UnityTest]
public IEnumerator MyCoroutine_CompletesSuccessfully()
{
    yield return _component.MyCoroutine();
    Assert.IsTrue(_component.IsComplete);
}
```

### Testing Time-Based Logic
Use `Time.deltaTime` and `yield return null` to advance frames:
```csharp
[UnityTest]
public IEnumerator Timer_AfterOneSecond_CompletesTimer()
{
    _component.StartTimer(1.0f);
    
    yield return new WaitForSeconds(1.1f);
    
    Assert.IsTrue(_component.IsTimerComplete);
}
```

## Mocking and Test Doubles

### Dependency Injection
Make dependencies injectable for testing:
```csharp
public class Player
{
    private IHealthSystem _health;
    
    public Player(IHealthSystem health = null)
    {
        _health = health ?? new HealthSystem();
    }
}

// In test:
var mockHealth = new MockHealthSystem();
var player = new Player(mockHealth);
```

### Interfaces for Testability
Define interfaces for dependencies:
```csharp
public interface IHealthSystem
{
    int CurrentHealth { get; }
    void TakeDamage(int amount);
}

// Real implementation
public class HealthSystem : IHealthSystem { }

// Test double
public class MockHealthSystem : IHealthSystem { }
```

## Assertions

### Common Assertions
```csharp
Assert.AreEqual(expected, actual);
Assert.AreNotEqual(expected, actual);
Assert.IsTrue(condition);
Assert.IsFalse(condition);
Assert.IsNull(obj);
Assert.IsNotNull(obj);
Assert.Greater(a, b);
Assert.GreaterOrEqual(a, b);
Assert.Less(a, b);
Assert.LessOrEqual(a, b);
```

### Unity-Specific Assertions
```csharp
Assert.AreApproximatelyEqual(expected, actual, tolerance);
UnityEngine.Assertions.Assert.IsTrue(condition, message);
LogAssert.Expect(LogType.Error, "Expected error message");
```

### Multiple Assertions
Use multiple assertions when testing related state:
```csharp
Assert.AreEqual(100, player.MaxHealth, "Max health incorrect");
Assert.AreEqual(100, player.CurrentHealth, "Current health incorrect");
Assert.IsFalse(player.IsDead, "Player should be alive");
```

## Test Isolation

### Each Test is Independent
- Tests should not depend on execution order
- Reset all state in SetUp
- Clean up in TearDown
- Don't share state between tests

### Avoid Test Dependencies
```csharp
// BAD: Tests depend on each other
[Test]
public void Test1_CreatesPlayer() { /* creates player */ }

[Test]
public void Test2_PlayerCanMove() { /* assumes player exists */ }

// GOOD: Each test is self-contained
[Test]
public void CreatePlayer_CreatesValidPlayer() { /* creates and tests */ }

[Test]
public void PlayerMove_WhenCreated_CanMove() { /* creates player, then tests movement */ }
```

## Performance Considerations

### Keep Tests Fast
- Prefer EditMode over PlayMode
- Minimize `WaitForSeconds` usage
- Avoid loading real scenes unless necessary
- Use minimal GameObjects

### Test Coverage
Focus on:
1. Critical gameplay logic (damage, scoring, game state)
2. Complex algorithms (pathfinding, AI decisions)
3. Edge cases (null inputs, boundary values)
4. Bug fixes (write test that reproduces bug, then fix)

Don't test:
- Unity's built-in functionality (Transform, Rigidbody, etc.)
- Simple getters/setters without logic
- Third-party library internals

## Common Pitfalls

### Pitfall: Testing Too Much Unity Infrastructure
Don't write tests for Unity's built-in behavior:
```csharp
// BAD: Testing Unity's Transform component
[Test]
public void Transform_WhenPositionSet_ReturnsPosition()
{
    _transform.position = Vector3.one;
    Assert.AreEqual(Vector3.one, _transform.position);
}
```

### Pitfall: Overly Coupled Tests
Make tests resilient to implementation changes:
```csharp
// FRAGILE: Tests exact private field name
var health = GetPrivateField<int>(_player, "_currentHealth");

// BETTER: Test public behavior
Assert.AreEqual(100, _player.GetHealth());
```

### Pitfall: Not Cleaning Up
Always destroy GameObjects to prevent test pollution:
```csharp
[TearDown]
public void TearDown()
{
    Object.DestroyImmediate(_gameObject);
}
```

## Debugging Tests

### Running Specific Tests
- Use Test Runner window in Unity
- Run individual tests or test suites
- Filter by name or category

### Debugging Failed Tests
1. Check test name clearly describes what failed
2. Add `Debug.Log` statements
3. Use Unity Debugger with tests
4. Verify SetUp/TearDown executed properly
5. Check for state pollution from other tests

### Log Assertion Testing
```csharp
[Test]
public void Method_WhenError_LogsError()
{
    LogAssert.Expect(LogType.Error, "Expected error message");
    _component.MethodThatLogsError();
}
```

## Continuous Integration

### Test Execution in CI
- Run tests in headless mode: `Unity -runTests -batchmode`
- Fail builds on test failures
- Track test execution time
- Monitor test coverage trends

### Test Categories
Use categories to organize test runs:
```csharp
[Test, Category("Fast")]
public void QuickTest() { }

[Test, Category("Slow")]
public void SlowIntegrationTest() { }
```

Run specific categories in CI: `-testCategory "Fast"`
