# Test Suite Expansion Plan

## Current Status (61 Tests)

### ✅ Already Tested Components
1. **PlayerAbilitiesTests** (19 tests) - Ability unlock system, events, state queries
2. **PlayerStateTrackerTests** (26 tests) - Movement state calculations, wall mechanics
3. **PlayerInputHandlerTests** (16 tests) - Input event routing, subscriptions

**Total:** 61 tests, all passing ✅

---

## Expansion Plan (Target: 150+ Tests)

### Priority 1: Core Movement Components (40+ tests)

#### PlayerMovementTests (20 tests)
**Complexity:** Medium - Has external dependencies but good separation of concerns
**Focus Areas:**
- Direction flipping (facingRight logic)
- Dash state management (cooldowns, air dashes)
- Horizontal movement calculations
- Dash jump window tracking
- Air dash consumption
- Ground dash vs air dash differences

**Test Categories:**
1. **Direction Management** (4 tests)
   - Flip when moving opposite direction
   - Maintain direction when not moving
   - Handle zero input
   - Edge case: rapid direction changes

2. **Dash Mechanics** (8 tests)
   - Start dash when available
   - Cooldown enforcement
   - Air dash vs ground dash tracking
   - Dash counter reset on landing
   - Air dash limit enforcement
   - Dash jump window timing
   - Consume air dash correctly
   - Cannot dash during cooldown

3. **Movement State** (4 tests)
   - Running state when moving
   - Idle state when stopped
   - Movement during dash
   - Movement after dash ends

4. **Configuration** (4 tests)
   - RunSpeed property access
   - DashSpeed property access
   - Configuration injection
   - Default values

#### PlayerJumpSystemTests (20 tests)
**Complexity:** High - Complex state machine with variable jump, double jump, wall jump
**Focus Areas:**
- Jump counter management (jumps remaining)
- Variable jump height mechanics
- Double jump delay and forced fall
- Wall jump detection
- Coyote time integration
- Jump compensation for slopes/walls

**Test Categories:**
1. **Basic Jump Counter** (6 tests)
   - Initialize with correct jump count
   - Consume jump correctly
   - Reset jumps on landing
   - Cannot jump when no jumps remain
   - Double jump ability check
   - Jump cooldown/delay

2. **Variable Jump System** (6 tests)
   - Min velocity on tap
   - Max velocity on hold
   - Jump hold timer increments
   - Jump hold duration limit
   - Release stops variable jump
   - Disabled when feature off

3. **Double Jump Mechanics** (4 tests)
   - Forced fall before double jump
   - Minimum delay enforcement
   - Double jump velocity range
   - Cannot double jump while grounded

4. **Wall Jump** (4 tests)
   - Wall jump direction calculation
   - Wall jump velocity application
   - Wall jump compensation multiplier
   - Wall jump resets air jumps

### Priority 2: Detection Components (30+ tests)

#### PlayerGroundDetectionTests (15 tests)
**Complexity:** Medium-High - Physics-based but can mock Unity components
**Focus Areas:**
- Ground check logic
- Slope detection and angle calculation
- Buffer climbing detection
- Coyote time counter management
- Landing detection

**Test Categories:**
1. **Ground State** (5 tests)
   - IsGrounded when touching ground
   - Not grounded in air
   - Grounded by platform detection
   - Grounded by buffer detection
   - Buffer climbing state

2. **Slope Detection** (5 tests)
   - Detect slope presence
   - Calculate slope angle correctly
   - Slope normal vector calculation
   - Max slope angle enforcement
   - Slope vs flat ground detection

3. **Coyote Time** (5 tests)
   - Counter decrements when airborne
   - Counter resets on landing
   - Enabled/disabled state
   - LeftGroundByJumping flag
   - Coyote time allows jump

#### PlayerWallDetectionTests (15 tests)
**Complexity:** Medium - Raycast-based detection with clear boundaries
**Focus Areas:**
- Wall detection with multiple raycasts
- Wall stick logic (movement toward wall)
- Wall contact point tracking
- Wall normal calculation

**Test Categories:**
1. **Wall Detection** (6 tests)
   - OnWall when touching wall
   - Not on wall when separated
   - Multiple raycast validation
   - Wall detection distance
   - Wall layer filtering
   - Wall contact point tracking

2. **Wall Stick Logic** (5 tests)
   - Wall stick allowed when pressing toward wall
   - Not allowed when pulling away
   - Not allowed when not on wall
   - Direction threshold enforcement
   - Wall stick during various states

3. **Wall Properties** (4 tests)
   - Wall normal vector calculation
   - Wall side detection (left/right)
   - Wall contact position
   - Wall raycast configuration

### Priority 3: Combat & Health (25+ tests)

#### PlayerCombatTests (15 tests)
**Complexity:** Medium - Animation-driven with state management
**Focus Areas:**
- Basic attack system
- Combo attack tracking
- Air attack mechanics
- Dash attack mechanics
- Attack cooldowns

**Test Categories:**
1. **Basic Attack** (5 tests)
   - Start attack when not attacking
   - Attack cooldown enforcement
   - Attack state flag management
   - Attack animation trigger
   - Cannot attack during dash

2. **Combo System** (5 tests)
   - Combo counter increments
   - Combo resets after timeout
   - Max combo count enforcement
   - Combo window timing
   - Combo requires ability

3. **Special Attacks** (5 tests)
   - Air attack detection
   - Dash attack during dash
   - Attack type differentiation
   - Special attack cooldowns
   - Ability requirements

#### PlayerHealthTests (10 tests)
**Complexity:** Low-Medium - Straightforward state management
**Focus Areas:**
- Damage handling
- Death detection
- Health restoration
- Invincibility frames

**Test Categories:**
1. **Damage System** (4 tests)
   - Take damage reduces health
   - Cannot go below zero
   - Death triggered at zero
   - Damage during invincibility

2. **Death & Respawn** (3 tests)
   - Death flag set correctly
   - Health reset on respawn
   - Death event triggered

3. **Invincibility** (3 tests)
   - Invincibility timer
   - No damage during invincibility
   - Timer expiration

### Priority 4: Support Systems (20+ tests)

#### PlayerRespawnSystemTests (10 tests)
**Complexity:** Low-Medium - Clear state management
**Focus Areas:**
- Respawn point setting
- Death zone detection
- Position reset
- State reset on respawn

**Test Categories:**
1. **Respawn Points** (4 tests)
   - Set respawn point
   - Default respawn point
   - Return to respawn on death
   - Multiple respawn points

2. **Death Zones** (3 tests)
   - Trigger respawn in death zone
   - Fall below threshold
   - Death zone collision

3. **Reset Logic** (3 tests)
   - Position reset
   - Velocity reset
   - State reset (health, jumps, etc.)

#### PlayerAnimationControllerTests (10 tests)
**Complexity:** Low - Parameter setting validation
**Focus Areas:**
- Animator parameter updates
- State-to-animation mapping
- Parameter value correctness

**Test Categories:**
1. **Parameter Updates** (5 tests)
   - Set IsGrounded parameter
   - Set IsRunning parameter
   - Set IsJumping parameter
   - Set IsDashing parameter
   - Set Speed parameter

2. **Animation Triggers** (3 tests)
   - Jump trigger
   - Attack trigger
   - Dash trigger

3. **Parameter Validation** (2 tests)
   - Null animator handling
   - Parameter existence check

---

## Test Implementation Strategy

### Phase 1: Movement Core (Week 1)
- ✅ PlayerMovementTests (20 tests)
- ✅ PlayerJumpSystemTests (20 tests)
**Target:** 101 total tests (61 + 40)

### Phase 2: Detection (Week 2)
- ✅ PlayerGroundDetectionTests (15 tests)
- ✅ PlayerWallDetectionTests (15 tests)
**Target:** 131 total tests (101 + 30)

### Phase 3: Combat & Health (Week 3)
- ✅ PlayerCombatTests (15 tests)
- ✅ PlayerHealthTests (10 tests)
**Target:** 156 total tests (131 + 25)

### Phase 4: Support Systems (Week 4)
- ✅ PlayerRespawnSystemTests (10 tests)
- ✅ PlayerAnimationControllerTests (10 tests)
**Target:** 176 total tests (156 + 20)

---

## Testing Principles

### Test Isolation
- Each test creates fresh GameObject instances
- Tests clean up in TearDown
- No shared state between tests
- Mock external dependencies

### Test Structure (AAA Pattern)
```csharp
[Test]
public void Component_Behavior_Condition()
{
    // Arrange: Set up test conditions

    // Act: Execute the behavior

    // Assert: Verify the outcome
}
```

### Naming Convention
`{Component}_{ExpectedBehavior}_{Condition}`

Examples:
- `Movement_FlipsDirection_WhenMovingOpposite()`
- `JumpSystem_ConsumesJump_WhenJumping()`
- `GroundDetection_ResetsCoyoteTime_OnLanding()`

### Test Categories
```csharp
#region Category Name
// Related tests grouped together
#endregion
```

### Coverage Goals
- **Unit Tests (EditMode):** Business logic, state management, calculations
- **Integration Tests (PlayMode):** Physics interactions, collision detection
- **Edge Cases:** Null checks, boundary values, state transitions

---

## Current Implementation Status

### Completed ✅
- [x] PlayerAbilitiesTests (19 tests)
- [x] PlayerStateTrackerTests (26 tests)
- [x] PlayerInputHandlerTests (16 tests)

### In Progress 🔄
- [ ] PlayerMovementTests (20 tests) - **NEXT**
- [ ] PlayerJumpSystemTests (20 tests)

### Planned 📋
- [ ] PlayerGroundDetectionTests (15 tests)
- [ ] PlayerWallDetectionTests (15 tests)
- [ ] PlayerCombatTests (15 tests)
- [ ] PlayerHealthTests (10 tests)
- [ ] PlayerRespawnSystemTests (10 tests)
- [ ] PlayerAnimationControllerTests (10 tests)

---

## Success Metrics

### Code Coverage
- **Target:** 80%+ for player systems
- **Current:** ~30% (3/10 components)
- **After Expansion:** ~90% (10/10 components)

### Test Count
- **Current:** 61 tests
- **Target:** 176+ tests
- **Growth:** 188% increase

### Test Execution Time
- **Target:** <5 seconds for full EditMode suite
- **Current:** ~2 seconds (61 tests)
- **Projected:** ~8-10 seconds (176 tests)

### Regression Prevention
- Catch state machine bugs before runtime
- Validate configuration changes
- Ensure consistent behavior across components
- Document expected behavior through tests

---

## Notes

- Focus on **testable business logic** in EditMode tests
- Defer **physics interactions** to PlayMode tests (future)
- Prioritize **state machine validation** over Unity-specific features
- Keep tests **fast and isolated** for rapid feedback
- Use **descriptive test names** that document behavior

**Remember:** These tests are *living documentation* of how the system should behave!
