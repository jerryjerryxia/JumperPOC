---
name: unity-unit-testing
description: Comprehensive guide for unit testing Unity 2D projects using Unity Test Framework. Use when asked to write unit tests, test Unity scripts, create test suites, test MonoBehaviours, test game logic, or help with testing Unity code. Includes test templates, best practices, helper scripts, and patterns for testing gameplay systems, coroutines, physics, UI, and more.
---

# Unity Unit Testing Skill

Write effective unit tests for Unity 2D projects using Unity Test Framework.

## Quick Start

### 1. Analyze What to Test

Before writing tests, use the analysis script to understand what should be tested:

```bash
python scripts/suggest_tests.py path/to/YourScript.cs
```

This provides specific testing recommendations for the script.

### 2. Generate Test Boilerplate

Generate a test class skeleton:

```bash
python scripts/generate_test_boilerplate.py path/to/YourScript.cs --output Tests/YourScriptTests.cs
```

This creates a test file with:
- Proper SetUp/TearDown for MonoBehaviours
- Test method stubs for each public method
- Correct import statements

### 3. Implement Tests

Fill in the generated test stubs following the AAA pattern:
- **Arrange**: Set up test conditions
- **Act**: Execute the method under test
- **Assert**: Verify expected behavior

### 4. Use Templates for Common Patterns

Reference templates in `assets/templates/` for common scenarios:
- `monobehaviour_test.cs` - Testing Unity components
- `static_class_test.cs` - Testing pure logic/utility classes
- `coroutine_test.cs` - Testing coroutines with [UnityTest]
- `playmode_test.cs` - Testing physics, collisions, scene-based behavior

## Test Types

### EditMode Tests (Preferred)
- Fast execution (milliseconds)
- No Unity runtime required
- Use for: Pure logic, calculations, data structures, simple MonoBehaviour methods

### PlayMode Tests (When Necessary)
- Slower execution (seconds)
- Full Unity runtime available
- Use for: Coroutines, physics, collisions, animations, frame-based behavior

**Rule**: Always use EditMode unless you need PlayMode features.

## Writing Tests

### Test Naming Convention

Use descriptive names that explain the test:

```
{MethodName}_When{Condition}_Should{ExpectedBehavior}
```

Examples:
- `TakeDamage_WhenHealthAboveZero_ReducesHealth`
- `AddItem_WhenInventoryFull_ReturnsFalse`
- `Jump_WhenGrounded_AppliesUpwardForce`

### MonoBehaviour Testing Pattern

```csharp
public class PlayerControllerTests
{
    private PlayerController _component;
    private GameObject _gameObject;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject();
        _component = _gameObject.AddComponent<PlayerController>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_gameObject);
    }

    [Test]
    public void Move_WithValidInput_UpdatesPosition()
    {
        // Arrange
        var initialPosition = _component.transform.position;
        
        // Act
        _component.Move(Vector2.right);
        
        // Assert
        Assert.AreNotEqual(initialPosition, _component.transform.position);
    }
}
```

### Coroutine Testing Pattern

```csharp
[UnityTest]
public IEnumerator DelayedAction_WaitsCorrectDuration()
{
    // Arrange
    var startTime = Time.time;
    
    // Act
    yield return _component.DelayedAction(1.0f);
    
    // Assert
    var elapsed = Time.time - startTime;
    Assert.GreaterOrEqual(elapsed, 1.0f);
    Assert.IsTrue(_component.ActionCompleted);
}
```

### Static Class Testing Pattern

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
    var result = DamageCalculator.CalculateDamage(damage, armor);
    Assert.AreEqual(expected, result);
}
```

## Key Testing Principles

1. **Test Behavior, Not Implementation**
   - Focus on what the code does, not how it does it
   - Tests should survive refactoring

2. **Keep Tests Fast**
   - Prefer EditMode over PlayMode
   - Minimize WaitForSeconds usage
   - Don't test Unity's built-in functionality

3. **Test Isolation**
   - Each test should be independent
   - Reset state in SetUp
   - Clean up in TearDown
   - Don't share state between tests

4. **Focus on Critical Logic**
   - Gameplay mechanics (damage, scoring, movement)
   - Complex algorithms
   - Edge cases and error conditions
   - Bug fixes (write test first, then fix)

5. **Use Mocking for Dependencies**
   - Abstract external dependencies with interfaces
   - Use dependency injection for testability
   - See `references/mocking_guide.md` for patterns

## Reference Documentation

### For Best Practices
Read `references/best_practices.md` for:
- Test organization and structure
- EditMode vs PlayMode decisions
- Common Unity testing patterns
- Assertion guidelines
- Performance considerations
- Common pitfalls and debugging

### For Specific Patterns
Read `references/test_patterns.md` for examples of testing:
- Health systems
- Inventory systems
- Score systems
- MonoBehaviour components
- Coroutines
- Unity Events
- Physics and collisions
- UI components
- ScriptableObjects
- Singletons

### For Mocking and Test Doubles
Read `references/mocking_guide.md` for:
- Types of test doubles (Dummy, Stub, Fake, Mock, Spy)
- Creating testable dependencies
- Mocking Unity components
- Mocking Input and Time
- Advanced mocking patterns

## Common Test Scenarios

### Testing Game Logic

```csharp
[Test]
public void TakeDamage_WhenHealthReachesZero_MarksDead()
{
    var health = new HealthSystem(50);
    health.TakeDamage(60);
    
    Assert.AreEqual(0, health.CurrentHealth);
    Assert.IsTrue(health.IsDead);
}
```

### Testing Unity Events

```csharp
[Test]
public void OnDamaged_InvokesEvent()
{
    var eventInvoked = false;
    _component.OnDamaged.AddListener(() => eventInvoked = true);
    
    _component.TakeDamage(10);
    
    Assert.IsTrue(eventInvoked);
}
```

### Testing With Multiple Inputs

```csharp
[TestCase(100, 0, 100)]
[TestCase(100, 50, 50)]
[TestCase(100, 100, 0)]
public void CalculateDamage_ReturnsExpectedDamage(int damage, int armor, int expected)
{
    var result = DamageCalculator.CalculateDamage(damage, armor);
    Assert.AreEqual(expected, result);
}
```

## Helper Scripts

### Generate Test Boilerplate
Automatically creates test class structure:
```bash
python scripts/generate_test_boilerplate.py YourScript.cs --output YourScriptTests.cs
```

Detects:
- MonoBehaviour vs regular class
- Public methods to test
- Generates appropriate SetUp/TearDown

### Suggest Tests
Analyzes code and recommends what to test:
```bash
python scripts/suggest_tests.py YourScript.cs
```

Identifies:
- Methods, properties, coroutines
- Unity-specific features (collisions, events)
- Recommended test mode (EditMode vs PlayMode)
- Edge cases to consider

## Workflow

1. **Analyze**: Run `suggest_tests.py` to understand what needs testing
2. **Generate**: Use `generate_test_boilerplate.py` to create test skeleton
3. **Reference**: Check templates and patterns for similar scenarios
4. **Implement**: Write test logic following AAA pattern
5. **Verify**: Run tests in Unity Test Runner
6. **Iterate**: Refine tests based on failures and edge cases

## Tips for Success

- Start with simple EditMode tests before PlayMode tests
- Test one thing per test method
- Use descriptive test names
- Keep tests under 20 lines when possible
- Don't test Unity's built-in functionality
- Focus on gameplay-critical logic first
- Use [TestCase] for multiple similar scenarios
- Always clean up GameObjects in TearDown
- Read the reference docs for specific patterns

## When to Use This Skill

Trigger this skill when:
- "Write unit tests for this script"
- "Help me test my player controller"
- "Create a test suite for my inventory system"
- "How do I test this MonoBehaviour?"
- "Test this coroutine"
- "Write tests for this game logic"
- "Help me set up Unity tests"
- Any request involving testing Unity code
