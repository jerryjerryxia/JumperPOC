# Phase 2: Parameter Migration Plan

## Overview

Phase 1 successfully synchronized code defaults with inspector-tuned values. Phase 2 will complete the parameter migration by moving parameters from PlayerController to their owning components.

## Current State Analysis

### Architecture Pattern (Current)
```
PlayerController (owns all parameters)
  ├─ [SerializeField] parameters defined here
  ├─ Initialize() → creates components
  └─ SetConfiguration() → passes parameters to components

Component (receives configuration)
  ├─ private fields (no SerializeField)
  └─ SetConfiguration() → receives values from PlayerController
```

### Problems with Current Design
1. **Poor Inspector Organization**: All 37+ parameters appear in PlayerController inspector
2. **No Single Source of Truth**: Parameters defined in PlayerController but used in components
3. **Confusing Ownership**: Not clear which component owns which parameter
4. **Harder to Maintain**: Changes require updating both PlayerController and component
5. **Inspector Clutter**: Related parameters scattered across one massive inspector

## Target State (Phase 2)

### Architecture Pattern (Target)
```
PlayerController (minimal parameters)
  ├─ animationTransitionSpeed (only locally-used parameter)
  ├─ Initialize() → creates components
  └─ No SetConfiguration() calls needed

Component (owns its parameters)
  ├─ [SerializeField] parameters defined here
  └─ Uses parameters directly (no injection needed)
```

### Benefits of New Design
1. ✅ **Clear Ownership**: Each component owns its parameters
2. ✅ **Better Inspector Organization**: Parameters grouped by component
3. ✅ **Single Source of Truth**: Parameter lives where it's used
4. ✅ **Easier Maintenance**: Changes only affect one component
5. ✅ **Follows Unity Best Practices**: Component configuration pattern

---

## Parameter Distribution Plan

### PlayerMovement (12 parameters)
```csharp
[Header("Movement")]
[SerializeField] private float runSpeed = 4f;
[SerializeField] private float wallSlideSpeed = 2f;

[Header("Buffer Climbing")]
[SerializeField] private float climbForce = 3f;
[SerializeField] private float forwardBoost = 0f;

[Header("Dash")]
[SerializeField] private float dashSpeed = 8f;
[SerializeField] private float dashTime = 0.25f;
[SerializeField] private float dashCooldown = 0.4f;
[SerializeField] private int maxDashes = 2;
[SerializeField] private int maxAirDashes = 2;

[Header("Dash Jump")]
[SerializeField] private Vector2 dashJump = new(5f, 11f);
[SerializeField] private float dashJumpWindow = 0.1f;
```

### PlayerJumpSystem (18 parameters)
```csharp
[Header("Jump")]
[SerializeField] private int extraJumps = 1;
[SerializeField] private Vector2 wallJump = new(7f, 10f);

[Header("Variable Jump (Hollow Knight Style)")]
[SerializeField] private bool enableVariableJump = true;
[SerializeField] private float minJumpVelocity = 2f; // ← Phase 1 fixed
[SerializeField] private float maxJumpVelocity = 4f;
[SerializeField] private float jumpHoldDuration = 0.3f;
[SerializeField] private float jumpGravityReduction = 0f;

[Header("Double Jump Settings")]
[SerializeField] private float minDoubleJumpVelocity = 1f; // ← Phase 1 fixed
[SerializeField] private float maxDoubleJumpVelocity = 3f; // ← Phase 1 fixed
[SerializeField] private float doubleJumpMinDelay = 0.2f;
[SerializeField] private float forcedFallDuration = 0.1f;
[SerializeField] private float forcedFallVelocity = -2f;
[SerializeField] private bool useVelocityClamping = true;
[SerializeField] private bool showJumpDebug = false;

[Header("Jump Compensation")]
[SerializeField] private float wallJumpCompensation = 1.2f;
[SerializeField] private bool enableJumpCompensation = true;

[Header("Coyote Time")]
[SerializeField] private bool enableCoyoteTime = false; // ← Phase 1 fixed
[SerializeField] private float coyoteTimeDuration = 0.02f; // ← Phase 1 fixed
[SerializeField] private bool coyoteTimeDuringDashWindow = false;
```

### PlayerGroundDetection (10 parameters)
```csharp
[Header("Ground Check")]
[SerializeField] private float groundCheckOffsetY = -0.02f;
[SerializeField] private float groundCheckRadius = 0.03f;

[Header("Slope Movement")]
[SerializeField] private float maxSlopeAngle = 60f;

[Header("Slope Raycast Parameters")]
[SerializeField] private bool enableSlopeVisualization = true;
[SerializeField] private float slopeRaycastDistance = 0.2f;
[SerializeField] private Vector2 raycastDirection1 = Vector2.down;
[SerializeField] private Vector2 raycastDirection2 = new Vector2(0.707f, -0.707f);
[SerializeField] private Vector2 raycastDirection3 = new Vector2(-0.707f, -0.707f);
[SerializeField] private float debugLineDuration = 0.1f;

[Header("Buffer Climbing")]
[SerializeField] private float climbingAssistanceOffset = 0.06f;
```

### PlayerWallDetection (4 parameters)
```csharp
[Header("Wall Detection")]
[SerializeField] private float wallCheckDistance = 0.15f;

[Header("Wall Detection Raycasts")]
[SerializeField] private float wallRaycastTop = 0.32f;
[SerializeField] private float wallRaycastMiddle = 0.28f;
[SerializeField] private float wallRaycastBottom = 0.02f;
```

### PlayerRespawnSystem (3 parameters)
```csharp
[Header("Death Zone")]
[SerializeField] private float deathZoneY = -20f;
[SerializeField] private float deathZoneWidth = 100f;
[SerializeField] private bool showDeathZone = true;
```

### PlayerController (1 parameter)
```csharp
[Header("Animation")]
[SerializeField] private float animationTransitionSpeed = 0.1f;
```

---

## Handling Shared Parameters

Some parameters are used by multiple components. We need a strategy to avoid duplication.

### Strategy: Primary Owner Pattern

**Rule**: One component owns the parameter, others read from it.

### Shared Parameter Resolution

#### climbingAssistanceOffset
- **Used by**: PlayerGroundDetection (primary), PlayerMovement (secondary)
- **Primary Owner**: PlayerGroundDetection (detection logic)
- **Solution**: PlayerMovement reads from PlayerGroundDetection.ClimbingAssistanceOffset

#### Wall Detection Parameters (wallCheckDistance, wallRaycastTop/Middle/Bottom)
- **Used by**: PlayerWallDetection (primary), PlayerMovement (secondary), PlayerJumpSystem (secondary)
- **Primary Owner**: PlayerWallDetection (detection logic)
- **Solution**: Others read from PlayerWallDetection properties

#### maxAirDashes, maxDashes
- **Used by**: PlayerMovement (dash execution), PlayerGroundDetection (reset on landing), PlayerJumpSystem (reset on jump)
- **Primary Owner**: PlayerMovement (dash system owner)
- **Solution**: Others read from PlayerMovement properties

---

## Migration Steps (Test-Driven)

### Step 1: Add Parameters to Components
For each component:
1. Add `[SerializeField]` parameters with **Phase 1 corrected defaults**
2. Remove corresponding private fields
3. Keep SetConfiguration() method temporarily (for backward compatibility)

### Step 2: Update Component References
1. Add public properties for shared parameters
2. Update components to read from primary owners
3. Remove redundant SetConfiguration() parameters

### Step 3: Update PlayerController
1. Remove migrated `[SerializeField]` parameters
2. Remove SetConfiguration() calls
3. Keep only animationTransitionSpeed

### Step 4: Test
1. Open Unity and verify inspector shows parameters in correct components
2. Test all gameplay mechanics (no changes expected)
3. Verify inspector values match expected values

---

## Risk Mitigation

### Inspector Value Preservation

**Problem**: Unity resets serialized values when field names change.

**Solution**:
1. Keep parameter names identical
2. Use same default values (Phase 1 corrected values)
3. Inspector values should persist automatically

### Testing Checklist

After migration, test:
- [ ] Ground jump (tap vs hold)
- [ ] Double jump (tap vs hold)
- [ ] Wall jump
- [ ] Dash
- [ ] Dash jump
- [ ] Wall slide
- [ ] Wall stick
- [ ] Slope movement
- [ ] Buffer climbing
- [ ] Coyote time disabled (as per Phase 1)
- [ ] Death zone respawn
- [ ] Combat system integration

---

## Implementation Order

**Recommended Order** (least coupled → most coupled):

1. **PlayerRespawnSystem** (3 params, no dependencies)
2. **PlayerWallDetection** (4 params, primary owner of wall detection)
3. **PlayerGroundDetection** (10 params, primary owner of ground detection and climbing offset)
4. **PlayerJumpSystem** (18 params, reads from wall/ground detection)
5. **PlayerMovement** (12 params, reads from wall/ground detection)
6. **PlayerController** (clean up, remove SetConfiguration calls)

---

## Code Examples

### Before (Current)
```csharp
// PlayerController.cs
[SerializeField] private float runSpeed = 4f;
[SerializeField] private float dashSpeed = 8f;

void InitializeComponents() {
    movement.SetConfiguration(runSpeed, dashSpeed, ...);
}

// PlayerMovement.cs
private float runSpeed;
private float dashSpeed;

public void SetConfiguration(float _runSpeed, float _dashSpeed, ...) {
    runSpeed = _runSpeed;
    dashSpeed = _dashSpeed;
}
```

### After (Target)
```csharp
// PlayerController.cs
void InitializeComponents() {
    // No SetConfiguration needed!
}

// PlayerMovement.cs
[SerializeField] private float runSpeed = 4f;
[SerializeField] private float dashSpeed = 8f;
// Parameters used directly, no SetConfiguration needed
```

---

## Expected Outcome

### Inspector Organization (Before)
```
Player GameObject
├─ PlayerController (37+ parameters) ❌
├─ PlayerMovement (0 parameters)
├─ PlayerJumpSystem (0 parameters)
├─ PlayerGroundDetection (0 parameters)
├─ PlayerWallDetection (0 parameters)
└─ PlayerRespawnSystem (0 parameters)
```

### Inspector Organization (After)
```
Player GameObject
├─ PlayerController (1 parameter) ✅
├─ PlayerMovement (12 parameters) ✅
├─ PlayerJumpSystem (18 parameters) ✅
├─ PlayerGroundDetection (10 parameters) ✅
├─ PlayerWallDetection (4 parameters) ✅
└─ PlayerRespawnSystem (3 parameters) ✅
```

**Total**: 48 parameters properly distributed across 6 components

---

## Validation

After migration:
1. ✅ Each component owns its parameters
2. ✅ Inspector organized by responsibility
3. ✅ No SetConfiguration() boilerplate
4. ✅ Gameplay unchanged (Phase 1 defaults preserved)
5. ✅ Follows Unity best practices
6. ✅ Easier to maintain and extend

---

## Ready to Proceed?

Phase 2 migration is ready to execute. The plan:
1. Is low-risk (preserves all values and behavior)
2. Improves code organization significantly
3. Follows Unity best practices
4. Has clear rollback path (git commit)

**Next Step**: Begin implementation starting with PlayerRespawnSystem (simplest component).
