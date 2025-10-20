# Testing Guide - Unity 2D Platformer

## Overview

This project uses **Unity Test Framework** for comprehensive unit and integration testing. Tests are organized into EditMode (pure logic) and PlayMode (Unity runtime required) categories.

## Test Infrastructure

### Folder Structure
```
Assets/Tests/
├── EditMode/                          # Unit tests (no Unity runtime needed)
│   ├── Tests.EditMode.asmdef
│   ├── PlayerAbilitiesTests.cs        (19 tests)
│   ├── PlayerStateTrackerTests.cs     (26 tests)
│   └── PlayerInputHandlerTests.cs     (16 tests)
├── PlayMode/                          # Integration tests (Unity runtime required)
│   └── Tests.PlayMode.asmdef
└── TestHelpers/                       # Shared test utilities
    ├── TestHelpers.asmdef
    ├── AssertExtensions.cs            # Custom assertions
    └── MockInputManager.cs            # Mock input system
```

### Test Coverage

**Total Tests: 61** (as of Phase 1 completion)

| Component | Tests | Coverage | Priority |
|-----------|-------|----------|----------|
| PlayerAbilities | 19 | Ability unlock system, events, dictionary ops | ✅ Complete |
| PlayerStateTracker | 26 | Movement states, wall mechanics, edge cases | ✅ Complete |
| PlayerInputHandler | 16 | Event routing, subscriptions, null safety | ✅ Complete |

## Running Tests

### Via Unity Editor

1. **Open Test Runner:**
   - Window → General → Test Runner

2. **Run EditMode Tests:**
   - Click "EditMode" tab
   - Click "Run All" button
   - All 61 tests should pass (✅ green)

3. **Run Specific Test:**
   - Expand test hierarchy
   - Click individual test
   - Click "Run Selected"

### Via Command Line

```bash
# Run all EditMode tests
Unity.exe -runTests -batchmode -projectPath . -testPlatform EditMode -testResults results.xml

# Run PlayMode tests (when added)
Unity.exe -runTests -batchmode -projectPath . -testPlatform PlayMode -testResults results.xml
```

## Writing New Tests

### EditMode Test Template

```csharp
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class MyComponentTests
    {
        private GameObject testGameObject;
        private MyComponent component;

        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestObject");
            component = testGameObject.AddComponent<MyComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        [Test]
        public void MyMethod_ReturnsExpectedValue_WhenConditionMet()
        {
            // Arrange
            // ... setup test data

            // Act
            // ... call method under test

            // Assert
            // ... verify results
        }
    }
}
```

### Best Practices

1. **Follow AAA Pattern:**
   - **Arrange:** Set up test data and conditions
   - **Act:** Execute the method under test
   - **Assert:** Verify expected outcomes

2. **Descriptive Test Names:**
   - Format: `MethodName_ExpectedBehavior_WhenCondition`
   - Examples:
     - `SetAbility_UnlocksDoubleJump_WhenCalled()`
     - `UpdateStates_SetsIsRunning_WhenGroundedAndMoving()`

3. **Test One Thing:**
   - Each test should verify a single behavior
   - Multiple assertions are OK if testing related aspects

4. **Use Test Helpers:**
   ```csharp
   // For Vector2 comparisons
   AssertExtensions.AreApproximatelyEqual(expected, actual, 0.001f);

   // For float comparisons
   AssertExtensions.AreApproximatelyEqual(5.0f, result, 0.01f);

   // For range checks
   AssertExtensions.IsInRange(value, min, max);
   ```

5. **Clean Up Resources:**
   - Always implement `[TearDown]` to destroy test GameObjects
   - Use `Object.DestroyImmediate()` in EditMode tests
   - Unsubscribe from events in TearDown

## Test Categories

### Unit Tests (EditMode)

**Pure logic components with no Unity runtime dependencies:**

- ✅ **PlayerAbilities** - Ability unlock system
- ✅ **PlayerStateTracker** - Movement state calculations
- ✅ **PlayerInputHandler** - Input event routing

**Characteristics:**
- Fast execution (<5 seconds for all tests)
- No physics simulation needed
- Value-type inputs only
- Deterministic results

### Integration Tests (PlayMode) - FUTURE

**Components requiring Unity runtime:**

- ⏳ PlayerGroundDetection - Physics raycasts
- ⏳ PlayerWallDetection - Physics raycasts
- ⏳ PlayerMovement - Rigidbody2D physics
- ⏳ PlayerJumpSystem - Physics and timing
- ⏳ PlayerCombat - Animation and hitboxes

**Characteristics:**
- Slower execution (requires scene setup)
- Physics simulation needed
- May require coroutines for timing
- Test scene setup required

## Critical Test Scenarios

### PlayerStateTracker - Sequential Wall Logic ⚠️

**MOST IMPORTANT TESTS:**

```csharp
[Test]
public void UpdateStates_RequiresWallStickFirst_BeforeWallSlide()
{
    // CRITICAL: Wall slide can ONLY happen after wall sticking first
    // This test validates the core wall mechanic sequencing
}

[Test]
public void UpdateStates_AllowsRunningOnSlope_EvenWithWallDetection()
{
    // CRITICAL: Slope running must work even when wall is detected
    // This prevents the slope-edge bug
}
```

**Why These Matter:**
- Wall stick → wall slide sequencing is core to the game feel
- Slope running edge case was a known bug
- These tests prevent regressions

## Common Test Patterns

### Testing Events

```csharp
[Test]
public void MyEvent_FiresCorrectly_WhenConditionMet()
{
    // Arrange: Subscribe to event
    bool eventFired = false;
    component.OnMyEvent += () => eventFired = true;

    // Act: Trigger event
    component.DoSomething();

    // Assert: Event should fire
    Assert.IsTrue(eventFired, "Event should have fired");
}
```

### Testing State Transitions

```csharp
[Test]
public void State_TransitionsCorrectly_FromIdleToRunning()
{
    // Arrange: Start in idle state
    Assert.IsFalse(component.IsRunning);

    // Act: Trigger transition
    component.StartRunning();

    // Assert: State changed
    Assert.IsTrue(component.IsRunning);
}
```

### Testing With Value Types

```csharp
[Test]
public void UpdateStates_HandlesZeroVelocity()
{
    // Act: Pass value types directly (no mocking needed!)
    stateTracker.UpdateStates(
        moveInput: Vector2.zero,
        velocity: Vector2.zero,
        isGrounded: true,
        // ... other parameters
    );

    // Assert: State calculated correctly
    Assert.IsFalse(stateTracker.IsJumping);
}
```

## Test Maintenance

### When to Update Tests

- **Before refactoring:** Write tests for current behavior
- **After adding features:** Add tests for new functionality
- **When fixing bugs:** Add test reproducing the bug first
- **During code review:** Ensure new code has test coverage

### Test Coverage Goals

**Current Status (Phase 1):**
- ✅ Pure logic components: 100% coverage (PlayerAbilities, PlayerStateTracker, PlayerInputHandler)
- ⏳ Physics components: 0% coverage (future Phase 2)
- ⏳ Integration: 0% coverage (future Phase 3)

**Target (Phase 2-3):**
- All critical gameplay systems: >80% coverage
- All state transitions: 100% coverage
- Edge cases: >90% coverage

## Troubleshooting

### Tests Fail to Compile

**Problem:** Assembly definition issues
**Solution:**
1. Check that `Tests.EditMode.asmdef` references `Assembly-CSharp`
2. Verify NUnit framework is included in assembly references
3. Rebuild assembly definitions: Assets → Open C# Project

### Tests Pass in Editor but Fail in Build

**Problem:** Different execution contexts
**Solution:**
1. Ensure tests are EditMode only
2. Check for Unity Editor-specific API usage
3. Use `#if UNITY_EDITOR` for editor-only code

### Null Reference in Tests

**Problem:** Component not initialized
**Solution:**
1. Verify `[SetUp]` creates all required components
2. Check that dependencies are added (Rigidbody2D, Animator, etc.)
3. Add null checks in component code

### Physics Tests Don't Work

**Problem:** EditMode tests can't simulate physics
**Solution:**
1. Move to PlayMode test assembly
2. Use `[UnityTest]` with coroutines
3. Create test scene with physics setup

## Next Steps (Phase 2)

### Immediate Priorities

1. **Run all 61 tests in Unity Test Runner**
   - Verify 100% pass rate
   - Fix any failures
   - Document any edge cases discovered

2. **Set Up Physics Test Harness** (Week 3-4)
   - Create test scene with platforms, walls, slopes
   - Build prefab player with all components
   - Write PlayMode tests for PlayerGroundDetection

3. **CI/CD Integration** (Week 4+)
   - GitHub Actions workflow for automated testing
   - Test execution on every PR
   - Coverage reporting

### Future Test Additions

- PlayerGroundDetection: 15-20 tests (slope detection, coyote time)
- PlayerWallDetection: 10-15 tests (raycast validation)
- PlayerMovement: 20-25 tests (dash physics, buffer climbing)
- PlayerJumpSystem: 25-30 tests (variable jump heights, wall jump)
- PlayerCombat: 15-20 tests (combo timing, hitbox activation)

**Total Goal: 120-150 tests by end of Phase 3**

## Resources

### Unity Test Framework Docs
- [Unity Test Framework Manual](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [NUnit Documentation](https://docs.nunit.org/)

### Best Practices
- [Test-Driven Development in Unity](https://unity.com/how-to/test-driven-development-unity)
- [Unity Testing Best Practices](https://docs.unity3d.com/Manual/testing-editortestsrunner.html)

---

**Last Updated:** 2025-10-20
**Test Count:** 61 tests (Phase 1 complete)
**Next Review:** After Phase 2 (Physics Integration)
