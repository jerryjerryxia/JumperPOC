
# 7/28/2025 Metroidvania Refactor

## Overview
Major refactoring effort to transform the existing Unity 2D platformer from a monolithic 1,041-line PlayerController into a component-based metroidvania architecture. The goal is to support 20+ abilities with prerequisites and runtime toggling over a 2-3 year development timeline.

## Architecture Goals
- **Component-based system**: Separate abilities into individual components
- **State machine foundation**: 4 concurrent layers (Movement, Wall, Dash, Combat)
- **Data-driven configuration**: ScriptableObject for easy tweaking
- **Metroidvania progression**: Ability prerequisites and runtime enabling/disabling
- **Maintainable codebase**: Clear separation of concerns for long-term development

## Progress Summary

### ✅ Phase 1: Foundation (COMPLETED)
- **State Machine System**: Complete IState interface and StateMachineLayer infrastructure
- **PlayerData ScriptableObject**: Centralized configuration system
- **Component Architecture**: PlayerComponent base class for all abilities
- **Foundation Controller**: PlayerControllerNew coordinator running alongside original
- **Editor Tools**: FoundationSetupHelper and InputSystemFixer

### 🔄 Phase 1: Testing (IN PROGRESS - BLOCKED)
**Current Issue**: Player movement not working in foundation system
- InputManager exists at runtime and reports active
- Input debugging shows no input being received
- Original PlayerController works perfectly
- Foundation system cannot be properly tested until input is fixed

### ⏳ Phase 2: Component Separation (PENDING)
- Extract individual movement abilities from PlayerController
- Create modular combat system
- Implement ability toggling system
- Add metroidvania progression hooks

## Technical Implementation

### Files Created
1. **State Machine Core**:
   - `IState.cs` - Base state interface
   - `PlayerState.cs` - Abstract base state
   - `StateMachineLayer.cs` - Layer management
   - `PlayerStateManager.cs` - 4-layer coordinator

2. **Data Systems**:
   - `PlayerData.cs` - ScriptableObject configuration
   - `PlayerComponent.cs` - Base component class

3. **Foundation Controller**:
   - `PlayerControllerNew.cs` - New architecture coordinator
   - Runs in observation mode alongside original

4. **Input System**:
   - `InputManager.cs` - Singleton input handling
   - `InputManagerBootstrap.cs` - Runtime initialization
   - `InputSystemFixer.cs` - Editor debugging tool

5. **Editor Tools**:
   - `FoundationSetupHelper.cs` - One-click setup

### Key Design Decisions
- **Non-breaking approach**: Foundation runs alongside original PlayerController
- **Observation mode**: New system observes original states without interfering
- **Gradual migration**: Abilities moved one at a time after foundation proven
- **Event-driven input**: Centralized InputManager with action events

## Current Status: BLOCKED

### Problem: Input System Not Working
- **Symptoms**: Player doesn't move when using foundation system
- **Verified**: InputManager singleton exists and is active
- **Issue**: Input events not being received by foundation system
- **Impact**: Cannot proceed to Phase 2 without working foundation

### Debugging Attempts
1. Created InputSystemFixer tool
2. Verified InputManager bootstrap process
3. Confirmed Controls.inputactions asset exists
4. Added comprehensive input debugging
5. Modified PlayerController Start() method for proper initialization

### Next Steps (Tomorrow)
1. Deep dive into input system connectivity
2. Verify Unity Input System package configuration
3. Check action map bindings and event subscriptions
4. Consider alternative input handling approaches
5. Document solution once input issue resolved

## Architecture Benefits (Once Working)
- **Modular abilities**: Each ability as separate component
- **Easy testing**: Individual components can be tested in isolation
- **Metroidvania support**: Runtime ability enabling/disabling
- **Performance**: Only active abilities consume resources
- **Maintainability**: Clear code organization for large team
- **Data-driven**: Non-programmers can adjust values via ScriptableObjects

## Technical Debt Addressed
- **Monolithic controller**: 1,041 lines split into focused components
- **Hard-coded values**: Centralized in PlayerData ScriptableObject
- **State management**: Proper state machine vs boolean flags
- **Input handling**: Centralized vs scattered input checks
- **Ability conflicts**: State machine prevents impossible combinations

---

### Previously Resolved Issues

The following issues have been identified and resolved during development:

#### Player Health Bar Shows 0/0 (RESOLVED)
**Root Causes:**
1. Player GameObject missing "Player" tag
2. Broken PlayerHealth component reference due to namespace changes
3. Component not properly serialized in scene

**Solutions Applied:**
1. Set Player GameObject tag to "Player"
2. Remove and re-add PlayerHealth component
3. Use editor tool to recreate health UI

#### Health Bar Not Visible (RESOLVED)
**Root Causes:**
1. Health bar created under wrong canvas (world-space vs screen-space)
2. UI components on wrong layer
3. Missing component references

**Solutions Applied:**
1. Ensure player health bar created under ScreenSpaceOverlay canvas
2. Verify all components assigned to "UI" layer
3. Check component references in inspector

### Editor Helper Tool
**Location**: `Assets/Editor/HealthUISetupHelper.cs`

**Features:**
- Creates properly configured health bar hierarchies
- Handles layer assignment automatically
- Sets up correct canvas types (screen-space for player, world-space for enemies)
- Connects component references via SerializedObject

### Health Bar Positioning
- **Player**: Fixed position (10, -10) from top-left corner
- **Enemy**: Floating 1.0f units above enemy sprite center
- **Enemy bars**: Auto-hide after 2 seconds, show on damage

### Animation Settings
- **Fill Speed**: 2f (configurable per health bar)
- **Animation**: Enabled by default for smooth visual feedback
- **Color Transitions**: Smooth lerp between health state colors

### Integration with Combat System
- Health bars automatically update via event subscriptions
- Player: `PlayerHealth.OnHealthChanged`
- Enemy: `EnemyBase.OnHealthChanged` and `EnemyBase.OnDamageTaken`
- No manual update calls required in combat code

### Performance Considerations
- Enemy health bars use world-space canvas (one per enemy)
- Player health bar uses single screen-space overlay canvas
- Animations only active when health is changing
- UI layer properly separated from gameplay layers

## Important Implementation Details

- **Combo System**: Responsive timer-based combo system with input buffering and combo windows. The key thing to remember is that we must give player a great sense of control and fluidity. 
- **Wall Detection**: Uses Physics2D.OverlapCircle for reliable wall checks
- **Ground Detection**: Custom ground check with offset and radius parameters
- **State Management**: Careful use of boolean flags to prevent conflicting states
- **Physics**: All movement uses Rigidbody2D velocity manipulation, not transform movement



## Development Session Log - January 2025

### Issues Fixed Today

#### 1. Enemy Health Bar Position Discrepancy (RESOLVED)
**Issue**: Enemy health bar appeared at correct position in Scene view but was higher in Play mode.
**Root Cause**: Health bar positioning was calculated in scene editing mode without accounting for runtime sprite bounds changes.
**Solution**: 
- Added `[ExecuteInEditMode]` to EnemyHealthUI component
- Implemented dynamic sprite bounds calculation using `enemySprite.bounds`
- Changed default parameters to offset (0, 0.35, 0) with `useDynamicOffset = false`

#### 2. Enemy Health Bar Visibility on Spawn (RESOLVED)
**Issue**: Enemy health bars were visible when enemies spawned, before taking damage.
**Solution**: Changed default EnemyHealthUI settings:
- `showOnStart = false`
- `alwaysVisible = false`
- Health bars now only appear after enemies take damage

#### 3. Air Attack Restrictions (RESOLVED)
**Issue**: Player could perform unlimited air attacks, breaking game balance.
**Requirement**: Allow 2 air attacks total, but 2nd attack only available after double jump.
**Solution**: 
- Added `canUseSecondAirAttack` flag in PlayerCombat
- Implemented `OnDoubleJump()` method that enables second air attack
- Modified `HasUsedAirAttack` property to check both attack count and double jump status

#### 4. Dash Animation on Wall (RESOLVED)
**Issue**: Player could trigger dash animation while wall sticking.
**Solution**: Added `onWall` check in `OnDashInput()` to prevent dash queuing when on wall.

#### 5. Player Floating Above Ground (RESOLVED)
**Issue**: Player appeared to float slightly above ground in play mode.
**Root Cause**: Sprite pivot point vs collider alignment mismatch.
**Solution**: Identified as sprite/collider configuration issue - requires adjusting BoxCollider2D offset or sprite pivot.

#### 6. Landing Buffer Ghost Jumps (RESOLVED)
**Issue**: Landing buffer zones allowed "ghost jumps" when jumping upward through them.
**Solution**: Added upward velocity check in CheckGrounding():
```csharp
if (groundedByBuffer && rb.linearVelocity.y > 0.1f)
{
    groundedByBuffer = false;
}
```

#### 7. Wall Land Animation Playing Incorrectly (RESOLVED)
**Issue**: Wall stick animation played at inappropriate times (running past walls, brief contact).
**Solution**: 
- Separated wall slide (physics) from wall stick (animation) logic
- Added 10ms horizontal movement tracking
- Wall stick animation only plays after 10ms of no horizontal movement
- Added `isWallSticking` state separate from `onWall`

#### 8. Platform Edge Buffer Climbing (RESOLVED)
**Issue**: Player would get stuck in wall stick state when jumping near platform edges with landing buffers.
**Solution**: Implemented physical movement assistance:
- Added upward boost (`climbForce = 3.0f`) and forward momentum (`forwardBoost = 1.2f`)
- Triggers when player is below platform edge by `climbingAssistanceOffset` (0.2f)
- Added comprehensive gizmo visualization for debugging

#### 9. Combat Debug Log Spam (RESOLVED)
**Issue**: Console flooded with combat-related debug logs.
**Solution**: Commented out 25 Debug.Log statements in PlayerCombat.cs

#### 10. Wall Detection Responsiveness (RESOLVED)
**Issue**: Wall stick required minimum time check, making controls feel unresponsive.
**Solution**: Removed minimum time requirement for wall detection while keeping 10ms check for wall stick animation.

#### 11. Falling Animation After Dashing Off Platform (RESOLVED)
**Issue**: When player dashes horizontally off a platform edge, the falling animation doesn't play. Player gets stuck in dash animation end pose.

**Root Cause**: Landing buffer colliders at platform edges continued to report `isGrounded = true` even after player dashed off the platform, preventing falling animation from triggering.

**Symptoms**:
- Works correctly when dashing far from platform edge
- Fails when dashing close to edge (landing buffer contact)
- `isGrounded` remains true even after dashing off platform
- Ground check circle still detects landing buffer after leaving platform

**Solution**: Enhanced ground detection logic with spatial awareness:
1. **Solid Support Check**: When landing buffer is detected without platform below, check for actual solid ground support directly below player center
2. **Movement Direction Analysis**: When moving horizontally with speed >1.0f, verify there's solid ground in movement direction
3. **Platform Edge Detection**: If no solid support below AND no ground in movement direction, disable buffer grounding

**Key Implementation**:
```csharp
// Enhanced buffer validation: check if there's actual solid ground support below
if (groundedByBuffer && !groundedByPlatform)
{
    Vector2 centerBelow = new Vector2(transform.position.x, feetPos.y - 0.1f);
    bool hasSolidSupportBelow = Physics2D.OverlapCircle(centerBelow, groundCheckRadius * 0.8f, platformMask);
    
    if (!hasSolidSupportBelow)
    {
        Vector2 horizontalMovement = new Vector2(rb.linearVelocity.x, 0);
        if (horizontalMovement.magnitude > 1.0f)
        {
            Vector2 moveDirection = horizontalMovement.normalized;
            Vector2 checkPos = feetPos + moveDirection * groundCheckRadius * 2f;
            bool groundInMovementDirection = Physics2D.OverlapCircle(checkPos, groundCheckRadius, platformMask);
            
            if (!groundInMovementDirection)
            {
                groundedByBuffer = false; // Player has left the platform
            }
        }
    }
}
```

**Benefits**:
- Fixes dash-off-platform animation issue without affecting normal gameplay
- Removes need for workaround special cases
- Works for any horizontal movement (not just dash-specific)
- Maintains landing buffer functionality for legitimate platform edges
- Includes debug visualization for troubleshooting

### Unresolved Issues

#### 1. Wall Slide Physics Triggering When Far From Walls (ONGOING)
**Issue**: Wall slide physics (slower fall speed) triggers when player jumps near walls but is actually quite far from them (e.g., at coordinates X=4.76, Y=8.99).

**Symptoms**:
- Player falls slower than normal free fall when jumping near walls
- Wall slide physics activate without actual wall contact
- Occurs when player is visibly distant from wall surfaces

**Root Cause Analysis** (January 25, 2025):
- Wall detection raycast distance of 0.3f is too generous 
- Hardcoded layer mask `1 << 6` instead of proper Ground layer lookup
- Wall normal validation thresholds may be too permissive (0.95f x-normal, 0.2f y-normal)
- No distance validation to ensure player is actually close to wall surface

**Attempted Solutions**:
1. **Sequential Wall Slide Logic**: Added requirement that wall stick must be active before wall slide can trigger (`wasWallStickingLastFrame` tracking)
2. **Identified Code Fixes** (not yet applied): Reduce raycast distance to 0.15f, add distance validation, use proper layer system, tighten wall normal thresholds

**Current Status**: Wall slide physics issue persists. Sequential logic partially implemented but problem not fully resolved.

## Critical Debugging Methodology

### **Inspector Configuration vs Code Issues**

**IMPORTANT**: When encountering Unity issues, especially animation or component behavior problems, always consider that the root cause may be in Inspector configurations rather than code:

**Common Inspector-Related Issues:**
- **Animation Transitions**: Transition conditions, timing, or parameter names in Animator Controller
- **Component References**: Missing or incorrect component assignments in Inspector
- **Layer Assignments**: Objects on wrong layers affecting physics interactions
- **Collider Configuration**: Size, offset, or trigger settings in Inspector
- **Physics Settings**: Rigidbody constraints, gravity scale, drag values
- **Input System**: Action map assignments or input binding configurations
- **Audio/Visual Effects**: Component settings, trigger conditions, or reference assignments

**Debugging Approach:**
1. **Attempt Code Solution**: Try code-based fixes first if the logic seems clearly flawed
2. **When Code Fixes Fail**: If multiple code attempts don't resolve the issue, ALWAYS suggest checking Inspector configurations
3. **Ask Before Deep Debugging**: Before spending significant time on complex code solutions, remind the user to verify Inspector settings
4. **Document Inspector Dependencies**: When documenting fixes, note any Inspector configurations that are critical

**Example Phrases to Use:**
- "Before we dive deeper into code changes, could you check the Animator Controller transition conditions?"
- "This might be an Inspector configuration issue - let's verify the component references are set correctly"
- "If the code logic looks correct, the issue might be in layer assignments or collider settings"
- "Sometimes animation issues are resolved in the Animator Controller rather than code"

This approach prevents unnecessary code complexity and focuses debugging efforts more effectively.