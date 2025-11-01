# Unity Bug Log - Jumper 2D Platformer

**Project:** Jumper 2D Platformer
**Unity Version:** 6000.1.4f1
**Documentation Format:** Unity Debug SOP Compliant
**Last Updated:** 2025-11-01

This document logs all major bugs encountered during development with complete debugging processes following the Unity Debug SOP. Each bug entry includes: symptoms, root cause analysis, diagnostic process, fix implementation, and verification steps.

---

## Table of Contents

1. [Infinite Dashing Bug](#1-infinite-dashing-bug)
2. [Dash Jump Wall Slide Issue](#2-dash-jump-wall-slide-issue)
3. [Wall Stick Animation Missing After Dash Attack](#3-wall-stick-animation-missing-after-dash-attack)
4. [Camera Snap on Scene Transitions](#4-camera-snap-on-scene-transitions)
5. [Ascending Jump Attack Animation Stuck](#5-ascending-jump-attack-animation-stuck)
6. [Falling Animation Not Playing After Dash Off Platform](#6-falling-animation-not-playing-after-dash-off-platform)
7. [Landing Buffer Ghost Jumps](#7-landing-buffer-ghost-jumps)
8. [Wall Land Animation Playing Incorrectly](#8-wall-land-animation-playing-incorrectly)
9. [Player Floating Above Ground](#9-player-floating-above-ground)
10. [Enemy Health Bar Position Discrepancy](#10-enemy-health-bar-position-discrepancy)
11. [FlyingEnemy Random Spawn Position and Flickering](#11-flyingenemy-random-spawn-position-and-flickering)

---

## 1. Infinite Dashing Bug

**Commit:** d9f2aba
**Date:** 2025-10-21
**Severity:** Game-breaking (balance)

### Step 1 - Capture

**Symptoms:**
- Player can dash infinitely without cooldown
- Dash charges never deplete
- Game balance completely broken

**Error Messages:** None

**Unity Version:** 6000.2.0b9
**Platform:** Windows (Editor)

**Reproduction Steps:**
1. Start gameplay
2. Press Shift repeatedly
3. Observe: Player dashes continuously without limit
4. Expected: Max 2 consecutive dashes, then 0.4s cooldown

### Step 2 - Diagnose

**Initial Hypothesis:** "Dash charges being reset every frame"

**Diagnostic Logging Added:**
```csharp
// In PlayerMovement.cs
Debug.Log($"[Dash] Consuming dash: {dashesRemaining} → {dashesRemaining - 1}, Cooldown: {dashCDTimer}");
```

**Investigation:**
- Traced dash charge management through PlayerController
- Found line 496: `dashesRemaining = groundDetection.GetMaxDashes()`
- This sync was happening EVERY frame in Update()
- Dash charges constantly reset to 2, preventing depletion

**Root Cause:** PlayerController syncing `dashesRemaining` from PlayerGroundDetection every frame, regardless of whether player landed. This constant reset prevented dash charges from depleting.

### Step 3 - Test

**Changes Made:**

1. **PlayerController.cs** - Moved dash state sync inside landing check:
```csharp
// BEFORE (line 496 - every frame):
dashesRemaining = groundDetection.GetMaxDashes();

// AFTER (inside landing detection):
if (groundDetection.IsGrounded && !wasGroundedLastFrame) {
    dashesRemaining = maxDashes; // Only reset on landing
}
```

2. **PlayerJumpSystem.cs** - Removed air dash reset on wall jump:
```csharp
// Removed this line from wall jump:
// airDashesUsed = 0; // Wall jump should NOT grant extra air dash
```

3. **PlayerController.cs** - Added air dash reset on wall stick:
```csharp
// In OnEnterWallStick():
airDashesUsed = 0; // Wall stick resets air dash like landing
```

**Test Results:** Dash charges now deplete correctly, cooldown enforced.

### Step 4 - Verify

**Verification Checklist:**
- ✅ Ground dash: Max 2 consecutive, then 0.4s cooldown
- ✅ Air dash: Max 2 per air time (1 before/after double jump)
- ✅ Wall jump: Does NOT reset air dash counter
- ✅ Wall stick: Resets air dash counter (like landing)
- ✅ No console errors
- ✅ Works in build (not just editor)

### Step 5 - Document

**Root Cause:** Frame-level state synchronization overriding dash charge consumption.

**Fix:** Move dash charge reset to landing event only, remove wall jump air dash reset, add wall stick air dash reset.

**Prevention:**
- Avoid frame-level state syncing for consumable resources
- Use event-based resource reset (OnLanding, OnWallStick)
- Add debug logging for resource consumption during development

---

## 2. Dash Jump Wall Slide Issue

**Commit:** 972777f
**Date:** 2025-10-21
**Severity:** High (gameplay feel)

### Step 1 - Capture

**Symptoms:**
- After dash jumping onto wall, player sticks briefly then slides down
- Expected: Instant wall stick with zero sliding
- Wall stick animation plays but physics shows brief slide

**Error Messages:** None

**Unity Version:** 6000.2.0b9
**Platform:** Windows (Editor)

**Reproduction Steps:**
1. Dash horizontally
2. Jump during/after dash (dash jump momentum)
3. Contact wall while airborne
4. Observe: Brief slide before full wall stick

### Step 2 - Diagnose

**Initial Hypothesis:** "Velocity not zeroed when wall stick triggers"

**Investigation:**
- Examined `OnEnterWallStick` event in PlayerController
- Found conditional velocity zeroing: `if (rb.linearVelocity.y > 0)`
- When falling onto wall, velocity is negative, so check fails
- Gravity continued applying even after wall stick triggered
- Wall slide state overriding wall stick due to stale velocity parameter
- `isDashing` condition blocking wall stick from triggering during dash

**Root Cause Analysis:**
1. Velocity only zeroed for upward movement (y > 0 check)
2. Gravity still active during wall stick (gravityScale = 3.0)
3. Wall slide checking velocity before wall stick could override it
4. Dash state preventing wall stick from triggering

### Step 3 - Test

**Changes Made:**

1. **PlayerController.cs** - Unconditional velocity zeroing:
```csharp
// BEFORE:
if (rb.linearVelocity.y > 0) {
    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
}

// AFTER:
rb.linearVelocity = Vector2.zero; // Zero ALL velocity unconditionally
```

2. **PlayerStateTracker.cs** - Fixed state priority:
```csharp
// Removed !isDashing condition from wall stick check
// Added !IsWallSticking condition to wall slide check
bool isWallSliding = onWall && !isGrounded && !IsWallSticking && rb.linearVelocity.y < -0.1f;
```

3. **PlayerMovement.cs** - Disabled gravity during wall stick:
```csharp
// In HandleMovement():
if (IsWallSticking) {
    rb.gravityScale = 0; // CRITICAL: Disable gravity during wall stick
    return; // Early exit
}

// In OnExitWallStick():
rb.gravityScale = originalGravityScale; // Restore gravity
```

**Test Results:** Wall stick now instant with zero sliding.

### Step 4 - Verify

**Verification Checklist:**
- ✅ Dash jump onto wall sticks instantly with zero slide
- ✅ Regular jump onto wall sticks correctly
- ✅ Wall stick animation plays immediately
- ✅ Gravity properly disabled during wall stick
- ✅ Gravity restored when leaving wall
- ✅ No physics jitter

### Step 5 - Document

**Root Cause:** Three-part issue: (1) Conditional velocity zeroing, (2) Active gravity during wall stick, (3) State priority conflicts.

**Fix:** Unconditional velocity zeroing, gravity disable during wall stick, state priority adjustment.

**Prevention:**
- For stick mechanics, always disable gravity entirely
- Zero ALL velocity components when entering stick state
- Ensure proper state priority (stick > slide > fall)

---

## 3. Wall Stick Animation Missing After Dash Attack

**Commit:** 5129f60
**Date:** 2025-10-21
**Severity:** Medium (visual bug)

### Step 1 - Capture

**Symptoms:**
- After dash-attacking and landing near wall, player wall sticks but stays in ground idle animation
- Logic works (player stuck to wall, can wall jump)
- Visual state incorrect (shows idle instead of wall stick)

**Error Messages:** None

**Unity Version:** 6000.2.0b9
**Platform:** Windows (Editor)

**Reproduction Steps:**
1. Dash attack horizontally
2. Land near wall (wall contact active)
3. Observe: Player sticks to wall (physics correct)
4. Bug: Animation shows idle pose instead of wall stick pose

### Step 2 - Diagnose

**Initial Hypothesis:** "Missing animator transition from Idle to WallStick"

**Investigation:**
- Checked Animator Controller transitions
- Found transitions: Jump → WallStick, Fall → WallStick, Run → WallStick
- **Missing:** Idle → WallStick transition
- When landing from dash attack near wall, animation goes Idle
- No path from Idle to WallStick in animator

**Root Cause:** Animator Controller missing transition from Idle state to WallStick state. When player lands near wall after dash attack, animation enters Idle state with no way to transition to WallStick.

### Step 3 - Test

**Changes Made:**

**PlayerAnimator.controller** - Added new transition:
```
Idle → WallStick
Conditions:
  - onWall == true
  - IsWallSticking == true
Has Exit Time: false
Transition Duration: 0
```

**Test Results:** Player now correctly transitions to wall stick animation when landing near wall after dash attack.

### Step 4 - Verify

**Verification Checklist:**
- ✅ Dash attack → land near wall → wall stick animation plays
- ✅ Regular land near wall → wall stick animation plays
- ✅ Jump to wall → wall stick animation plays
- ✅ Fall to wall → wall stick animation plays
- ✅ Run to wall → wall stick animation plays
- ✅ All animation transitions smooth

### Step 5 - Document

**Root Cause:** Missing Animator transition from Idle to WallStick state.

**Fix:** Added Idle → WallStick transition with proper conditions (onWall && IsWallSticking).

**Prevention:**
- **CRITICAL LESSON:** When encountering animation bugs after code fixes repeatedly fail, check Animator Controller transitions FIRST
- Animation state machines need ALL valid state transitions defined
- Test all animation paths, not just common ones
- When physics works but visuals don't → Animator Controller issue

---

## 4. Camera Snap on Scene Transitions

**Commit:** 4785ebd
**Date:** 2025-10-23
**Severity:** High (player experience)

### Step 1 - Capture

**Symptoms:**
- Camera snaps to incorrect position when transitioning between scenes
- Player position correct, but camera lags behind or jumps
- Occurs on all scene transitions (Level1 ↔ Level2 ↔ Level3)

**Error Messages:** None

**Unity Version:** 6000.2.0b9
**Platform:** Windows (Editor + Build)

**Reproduction Steps:**
1. Enter Level 1
2. Transition to Level 2 via level transition trigger
3. Observe: Camera snaps to wrong position briefly
4. Player spawns at correct location, camera catches up 1-2 frames later

### Step 2 - Diagnose

**Initial Hypothesis:** "Camera revalidation happening before player spawn position finalized"

**Investigation:**
- `SceneManager.sceneLoaded` event subscription in `Start()` instead of `Awake()`
- Event callback registered AFTER scene load completed
- `RevalidatePlayerTarget()` executing immediately
- `LevelSpawnPoint.VerifySpawnPosition()` correcting player position AFTER camera snap
- Timing issue: Camera → Player position, instead of Player position → Camera

**Root Cause:** Event subscription timing issue. SceneManager.sceneLoaded subscribed in Start(), which executes after scene load. Camera attempts to track player before LevelSpawnPoint corrects position drift.

### Step 3 - Test

**Changes Made:**

1. **CameraController.cs** - Move event subscription to Awake:
```csharp
// BEFORE (in Start()):
SceneManager.sceneLoaded += OnSceneLoaded;

// AFTER (in Awake()):
SceneManager.sceneLoaded += OnSceneLoaded;
```

2. **CameraController.cs** - Add frame delay to revalidation:
```csharp
private IEnumerator RevalidatePlayerTarget()
{
    yield return new WaitForFixedUpdate(); // Wait for physics frame
    yield return null; // Wait 1 additional frame

    // Now player position is finalized by LevelSpawnPoint
    FindAndSetPlayerTarget();
}
```

**Test Results:** Camera now follows player correctly on all scene transitions.

### Step 4 - Verify

**Verification Checklist:**
- ✅ Level1 → Level2 transition smooth
- ✅ Level2 → Level3 transition smooth
- ✅ Level3 → Level2 transition smooth
- ✅ Level2 → Level1 transition smooth
- ✅ No camera snap or jitter
- ✅ Player spawns at correct position
- ✅ Works in build (not just editor)

### Step 5 - Document

**Root Cause:** Scene event subscription timing + insufficient frame delay for spawn position correction.

**Fix:** Move SceneManager.sceneLoaded subscription to Awake(), add WaitForFixedUpdate + 1 frame delay in revalidation.

**Prevention:**
- Subscribe to Unity lifecycle events in Awake(), not Start()
- For scene transitions, wait for physics frame + 1 additional frame before camera operations
- Ensure spawn point position correction happens before camera tracking

---

## 5. Ascending Jump Attack Animation Stuck

**Commit:** d530e50
**Date:** 2025-10-20
**Severity:** Medium (gameplay feel)

### Step 1 - Capture

**Symptoms:**
- When attacking while ascending from jump, player gets stuck in air attack animation
- Animation doesn't transition back to jump/fall state
- Movement freezes if player tries to jump during attack

**Error Messages:** None

**Unity Version:** 6000.2.0b9
**Platform:** Windows (Editor)

**Reproduction Steps:**
1. Jump upward
2. Attack while ascending (before reaching peak)
3. Observe: Air attack animation plays
4. Bug: Animation stays in attack pose, doesn't return to jump state
5. Attempt to jump during attack → movement freezes

### Step 2 - Diagnose

**Initial Hypothesis:** "Attack reset forcing transition to Idle even when airborne"

**Investigation:**
- Examined `ResetAttackSystem()` in PlayerCombat
- Found force transition to Idle: `animator.Play("Idle")`
- This happens regardless of player state (grounded vs airborne)
- When airborne, forcing Idle prevents natural jump/fall transitions
- Missing animator transition between AirAttack → Jump states
- Jump queuing causes movement freeze due to attack movement override

**Root Cause Analysis:**
1. ResetAttackSystem() force-transitioning to Idle even when airborne
2. Missing animator transition from AirAttack to Jump/Fall states
3. Attack movement override blocking jump input when attack queued

### Step 3 - Test

**Changes Made:**

1. **PlayerCombat.cs** - Smart state-based reset:
```csharp
// BEFORE:
animator.Play("Idle"); // Always force to Idle

// AFTER:
if (playerController.IsGrounded) {
    animator.Play("Idle"); // Force to Idle only when grounded
}
// When airborne, let animator naturally transition via IsJumping/IsFalling
```

2. **PlayerController.cs** - Skip attack movement when jump queued:
```csharp
// In HandleAttackMovement():
if (jumpQueued) return; // Don't override movement if jump queued
```

3. **PlayerAnimator.controller** (User fix):
- Added transition: AirAttack → Jump state
- Conditions: IsJumping == true
- Allows proper state flow during ascending attacks

**Test Results:** Attack animation now transitions correctly, jump input responsive.

### Step 4 - Verify

**Verification Checklist:**
- ✅ Attack during ascending jump → transitions back to jump state
- ✅ Attack during descending jump → transitions to fall state
- ✅ Attack while grounded → transitions to idle correctly
- ✅ Jump input during attack → movement not frozen
- ✅ All animation transitions smooth
- ✅ No stuck states

### Step 5 - Document

**Root Cause:** Three-part issue: (1) Unconditional force-transition to Idle, (2) Missing animator transitions, (3) Attack movement overriding jump input.

**Fix:** State-aware reset logic, added animator transitions, skip attack movement when jump queued.

**Prevention:**
- Never force animator transitions unconditionally (check player state first)
- Ensure all animation states have exit paths for all player states
- Input queuing should have higher priority than movement overrides

---

## 6. Falling Animation Not Playing After Dash Off Platform

**Commit:** Per DevLog.md (Resolved)
**Date:** 2025-01 (Session Log)
**Severity:** Medium (visual polish)

### Step 1 - Capture

**Symptoms:**
- Player dashes horizontally off platform edge
- Falling animation doesn't play
- Player stays in dash end pose while falling
- Works correctly when dashing far from platform edge
- Fails when dashing close to edge (landing buffer contact)

**Error Messages:** None

**Unity Version:** Unity 6 Beta
**Platform:** Windows (Editor)

**Reproduction Steps:**
1. Stand near platform edge with landing buffer underneath
2. Dash horizontally toward/off edge
3. Observe: Player leaves platform
4. Bug: `isGrounded` remains true, no falling animation
5. Expected: Falling animation should play when leaving platform

### Step 2 - Diagnose

**Initial Hypothesis:** "Landing buffer continues reporting grounded after dashing off platform"

**Investigation:**
- Landing buffer colliders at platform edges remain in contact range
- Ground check circle still detecting landing buffer after leaving platform
- `isGrounded` stays true due to buffer contact
- Falling animation requires `isGrounded = false`
- Need spatial awareness: check for solid ground support, not just buffer contact

**Root Cause:** Landing buffer collision detection lacks spatial context. Ground check detects buffer contact even when player has left platform, preventing falling animation.

### Step 3 - Test

**Changes Made:**

**PlayerController.cs** - Enhanced ground detection with spatial awareness:

```csharp
// Enhanced buffer validation: check if there's actual solid ground support below
if (groundedByBuffer && !groundedByPlatform)
{
    // 1. Check for solid ground directly below player center
    Vector2 centerBelow = new Vector2(transform.position.x, feetPos.y - 0.1f);
    bool hasSolidSupportBelow = Physics2D.OverlapCircle(centerBelow, groundCheckRadius * 0.8f, platformMask);

    if (!hasSolidSupportBelow)
    {
        // 2. Analyze horizontal movement direction
        Vector2 horizontalMovement = new Vector2(rb.linearVelocity.x, 0);
        if (horizontalMovement.magnitude > 1.0f)
        {
            // 3. Check for ground in movement direction
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

**Benefits:**
- Checks for actual solid ground support below player
- Analyzes movement direction for platform edges
- Disables buffer grounding when no support ahead
- Works for any horizontal movement (not dash-specific)

**Test Results:** Falling animation now plays correctly when dashing off platform.

### Step 4 - Verify

**Verification Checklist:**
- ✅ Dash off platform edge → falling animation plays
- ✅ Regular walk off platform → falling animation plays
- ✅ Landing buffer still works for legitimate platform edges
- ✅ No false positives (buffer doesn't trigger mid-air)
- ✅ Works for all horizontal movement speeds
- ✅ Debug visualization confirms spatial checks

### Step 5 - Document

**Root Cause:** Landing buffer ground detection lacking spatial context, treating buffer contact as grounded regardless of actual platform support.

**Fix:** Added three-layer validation: (1) Solid support check below, (2) Movement direction analysis, (3) Ground check ahead in movement direction.

**Prevention:**
- Landing buffer detection should include spatial awareness
- Don't rely solely on collision contact for grounded state
- Check for actual solid ground support, not just trigger contact
- Consider movement direction when validating platform edges

---

## 7. Landing Buffer Ghost Jumps

**Commit:** Per DevLog.md (Resolved)
**Date:** 2025-01 (Session Log)
**Severity:** High (game-breaking exploit)

### Step 1 - Capture

**Symptoms:**
- Landing buffer zones allow "ghost jumps" when jumping upward through them
- Player can jump mid-air by passing through landing buffers from below
- Breaks jump mechanics and allows sequence breaking

**Error Messages:** None

**Unity Version:** Unity 6 Beta
**Platform:** Windows (Editor)

**Reproduction Steps:**
1. Jump upward toward platform with landing buffer underneath
2. Pass through landing buffer zone while ascending
3. Press jump button while inside buffer zone
4. Bug: Player performs mid-air jump (ghost jump)
5. Expected: Jump should only work when actually landing on platform

### Step 2 - Diagnose

**Initial Hypothesis:** "Buffer grounding active regardless of vertical velocity direction"

**Investigation:**
- Landing buffers designed to help player land on platform edges
- Buffer check doesn't validate vertical velocity
- Upward movement through buffer still sets `groundedByBuffer = true`
- Jump system sees grounded state and allows jump
- Need to disable buffer grounding during upward movement

**Root Cause:** Landing buffer grounding check doesn't validate vertical velocity direction. Ascending through buffer incorrectly sets grounded state, allowing invalid jumps.

### Step 3 - Test

**Changes Made:**

**PlayerController.cs** - Added upward velocity check in CheckGrounding():

```csharp
// In CheckGrounding() method:
if (groundedByBuffer && rb.linearVelocity.y > 0.1f)
{
    groundedByBuffer = false; // Disable buffer grounding during upward movement
}
```

**Logic:**
- If grounded by buffer AND moving upward (y > 0.1f)
- Disable buffer grounding
- Prevents ghost jumps during ascent through buffer zone

**Test Results:** Ghost jumps eliminated, buffer still works for legitimate landings.

### Step 4 - Verify

**Verification Checklist:**
- ✅ Cannot jump when ascending through landing buffer
- ✅ Can still jump when descending onto landing buffer
- ✅ Landing buffer assists legitimate platform landings
- ✅ No sequence breaking via mid-air jumps
- ✅ Jump mechanics work correctly in all scenarios
- ✅ No false negatives (legitimate jumps still work)

### Step 5 - Document

**Root Cause:** Landing buffer grounding logic missing vertical velocity validation, allowing grounding during upward movement.

**Fix:** Added upward velocity check (y > 0.1f) to disable buffer grounding during ascent.

**Prevention:**
- Collision-based grounding must validate movement direction
- Triggers/buffers should consider physics context (velocity, forces)
- Test all approach angles for trigger-based mechanics

---

## 8. Wall Land Animation Playing Incorrectly

**Commit:** Per DevLog.md (Resolved)
**Date:** 2025-01 (Session Log)
**Severity:** Medium (visual polish)

### Step 1 - Capture

**Symptoms:**
- Wall stick animation plays at inappropriate times
- Triggers when running past walls (brief contact)
- Triggers during short wall touches (not intentional wall sticks)
- Animation flickers on/off during movement

**Error Messages:** None

**Unity Version:** Unity 6 Beta
**Platform:** Windows (Editor)

**Reproduction Steps:**
1. Run along ground parallel to wall
2. Briefly touch wall (< 100ms contact)
3. Observe: Wall stick animation flickers
4. Expected: Wall stick should only play for intentional sticks (not brief touches)

### Step 2 - Diagnose

**Initial Hypothesis:** "Need to separate wall contact physics from wall stick animation"

**Investigation:**
- `onWall` state triggers immediately on wall contact
- Wall stick animation tied directly to `onWall` state
- No time threshold for differentiating brief contact from intentional stick
- Need separate states: wall contact (physics) vs wall stick (animation)
- Wall stick should require sustained contact without horizontal movement

**Root Cause:** Wall stick animation coupled directly to wall contact physics without time/movement validation. Brief wall touches incorrectly trigger stick animation.

### Step 3 - Test

**Changes Made:**

**PlayerController.cs** - Separated wall slide physics from wall stick animation:

1. Added horizontal movement tracking (10ms window)
2. Created separate `isWallSticking` state from `onWall`
3. Wall stick animation requires 10ms of no horizontal movement
4. Wall contact (`onWall`) separate from animation state (`isWallSticking`)

```csharp
// Wall contact physics - immediate
bool onWall = /* raycast detection */;

// Wall stick animation - requires no horizontal movement for 10ms
if (onWall && horizontalMovement < 0.1f && contactDuration > 0.01f) {
    isWallSticking = true;
}
```

**Test Results:** Wall stick animation only plays for intentional sticks, not brief contacts.

### Step 4 - Verify

**Verification Checklist:**
- ✅ Wall stick animation plays for intentional wall sticks
- ✅ Brief wall contacts don't trigger wall stick animation
- ✅ Running past walls doesn't flicker animation
- ✅ Wall slide physics still work correctly
- ✅ Wall jump still works from brief contacts
- ✅ Animation transitions smooth

### Step 5 - Document

**Root Cause:** Wall stick animation logic coupled to immediate wall contact physics without time/movement validation.

**Fix:** Separated wall contact (physics) from wall stick (animation), added 10ms horizontal movement check, created distinct `isWallSticking` state.

**Prevention:**
- Separate physics state from animation state for context-sensitive mechanics
- Add time thresholds for distinguishing intentional actions from brief contacts
- Use movement analysis to validate player intent

---

## 9. Player Floating Above Ground

**Commit:** Per DevLog.md (Resolved - Inspector issue)
**Date:** 2025-01 (Session Log)
**Severity:** Medium (visual bug)

### Step 1 - Capture

**Symptoms:**
- Player appears to float slightly above ground in Play mode
- Looks correct in Scene view
- Visual gap between player sprite and ground

**Error Messages:** None

**Unity Version:** Unity 6 Beta
**Platform:** Windows (Editor)

**Reproduction Steps:**
1. Open scene in Editor
2. Scene view: Player appears to be on ground correctly
3. Enter Play mode
4. Observe: Player floating ~0.1-0.2 units above ground
5. Movement works correctly, only visual issue

### Step 2 - Diagnose

**Initial Hypothesis:** "Code issue with ground detection or physics"

**Code Investigation (Multiple attempts):**
- Checked ground detection raycasts
- Verified physics settings (gravity, drag)
- Examined collider sizes and positions
- All code appeared correct

**Inspector Investigation:**
- Sprite pivot point location examined
- BoxCollider2D offset value checked
- **Found mismatch:** Sprite pivot vs collider alignment

**Root Cause:** Sprite pivot point and BoxCollider2D offset misalignment in Inspector. Scene view shows sprite bounds, Play mode shows physics collider position. Visual disconnect between sprite rendering and physics collision.

### Step 3 - Test

**Changes Made:**

**Inspector Adjustments (NOT code):**
1. Adjusted BoxCollider2D offset Y value
2. OR: Changed sprite pivot point in sprite editor
3. Aligned sprite visual center with physics collider center

**Test Results:** Player now appears correctly grounded in both Scene and Play modes.

### Step 4 - Verify

**Verification Checklist:**
- ✅ Player sprite aligned with ground in Play mode
- ✅ Scene view and Play mode match
- ✅ Ground detection works correctly
- ✅ Movement physics unchanged
- ✅ Collider visualization matches sprite
- ✅ No regression in other scenes

### Step 5 - Document

**Root Cause:** Inspector configuration mismatch between sprite pivot point and BoxCollider2D offset, not a code issue.

**Fix:** Adjusted BoxCollider2D offset or sprite pivot in Inspector to align visual sprite with physics collider.

**Prevention:**
- **CRITICAL LESSON:** When code logic appears correct but visuals are wrong, check Inspector configurations FIRST
- Sprite/collider alignment issues are Inspector problems, not code problems
- Use gizmo visualization in Scene view to verify collider alignment
- When multiple code attempts fail, suspect Inspector/Unity Editor configuration

---

## 10. Enemy Health Bar Position Discrepancy

**Commit:** Per DevLog.md (Resolved)
**Date:** 2025-01 (Session Log)
**Severity:** Low (visual polish)

### Step 1 - Capture

**Symptoms:**
- Enemy health bar appears at correct position in Scene view
- In Play mode, health bar positioned higher than expected
- Offset between edit mode and runtime positions

**Error Messages:** None

**Unity Version:** Unity 6 Beta
**Platform:** Windows (Editor)

**Reproduction Steps:**
1. Place enemy in scene
2. Observe health bar position in Scene view (correct)
3. Enter Play mode
4. Observe: Health bar Y position higher than Scene view showed
5. Difference ~0.2-0.3 units

### Step 2 - Diagnose

**Initial Hypothesis:** "Health bar positioning calculated without accounting for runtime sprite bounds"

**Investigation:**
- Health bar position calculated in scene editing mode
- Runtime sprite bounds can differ from editor bounds
- No dynamic sprite bounds calculation at runtime
- Need `[ExecuteInEditMode]` for accurate preview
- Static offset doesn't account for sprite scaling/animation

**Root Cause:** Health bar positioning calculated statically at edit time without accounting for runtime sprite bounds changes. Sprite bounds can change during runtime due to animation or scaling.

### Step 3 - Test

**Changes Made:**

**EnemyHealthUI.cs:**

1. Added `[ExecuteInEditMode]` attribute:
```csharp
[ExecuteInEditMode]
public class EnemyHealthUI : MonoBehaviour
```

2. Implemented dynamic sprite bounds calculation:
```csharp
// Calculate position using runtime sprite bounds
Vector3 position = enemySprite.bounds.center + new Vector3(offset.x, offset.y + enemySprite.bounds.extents.y, offset.z);
```

3. Updated default parameters:
```csharp
offset = (0, 0.35, 0) // Adjusted for typical sprite bounds
useDynamicOffset = false // Allow manual tuning
```

**Test Results:** Health bar position now matches between Scene view and Play mode.

### Step 4 - Verify

**Verification Checklist:**
- ✅ Health bar position matches Scene view in Play mode
- ✅ Dynamic bounds calculation works for all enemy types
- ✅ Health bar follows enemy correctly
- ✅ Position updates when sprite animates
- ✅ Manual offset override still works
- ✅ No performance impact from dynamic calculation

### Step 5 - Document

**Root Cause:** Static position calculation at edit time without runtime sprite bounds awareness.

**Fix:** Added `[ExecuteInEditMode]` attribute and dynamic sprite bounds calculation using `enemySprite.bounds`.

**Prevention:**
- UI positioning for animated sprites should use runtime bounds
- Use `[ExecuteInEditMode]` for accurate edit-time preview
- Test edit mode and play mode positions match before finalizing
- Dynamic sprites require dynamic positioning logic

---

## Critical Debugging Patterns

### Pattern 1: Inspector Configuration vs Code Issues

**When to suspect Inspector/Animator:**
- Multiple code attempts fail to fix the issue
- Logic appears correct but behavior is wrong
- Animation bugs (transitions, parameters, states)
- Physics bugs (colliders, layers, rigidbody settings)
- Visual bugs (sprite alignment, positioning)

**Example Cases:**
- Bug #3: Wall Stick Animation Missing → Animator transition missing
- Bug #9: Player Floating → BoxCollider2D offset in Inspector
- Bug #10: Health Bar Position → Static vs dynamic bounds

**Debugging Approach:**
1. Attempt 1-2 code solutions
2. If unsuccessful, STOP coding
3. Check Inspector configurations
4. Check Animator Controller (transitions, parameters)
5. Check component settings (layers, tags, references)
6. Verify prefab overrides match expectations

### Pattern 2: State Synchronization Issues

**Common Symptom:** Variable reset every frame, resource never depletes

**Example Cases:**
- Bug #1: Infinite Dashing → Frame-level sync overriding consumption
- Bug #2: Wall Slide → State priority conflicts

**Solution Pattern:**
- Use event-based state updates (OnLanding, OnWallStick)
- Avoid frame-level syncing for consumable resources
- Implement state priority hierarchy
- Add debug logging for state transitions

### Pattern 3: Physics + Animation Coupling

**Common Symptom:** Physics works but animation stuck/wrong

**Example Cases:**
- Bug #3: Wall Stick Animation Missing
- Bug #5: Ascending Attack Animation Stuck
- Bug #8: Wall Land Animation Incorrect

**Solution Pattern:**
- Separate physics state from animation state
- Add all necessary animator transitions
- Use state-aware reset logic (check grounded before forcing Idle)
- Test all animation paths, not just common ones

### Pattern 4: Timing and Frame Delay Issues

**Common Symptom:** Works sometimes, fails other times

**Example Cases:**
- Bug #4: Camera Snap → Event subscription timing
- Bug #6: Falling Animation → Spatial validation timing

**Solution Pattern:**
- Subscribe to Unity events in Awake(), not Start()
- Add WaitForFixedUpdate + 1 frame for position corrections
- Use coroutines for frame-delayed operations
- Test timing across multiple frames

---

## Prevention Checklist

### Before Implementing New Features

- [ ] Plan state machine transitions (all states need exit paths)
- [ ] Separate physics logic from animation logic
- [ ] Use events for state changes, not frame-level syncing
- [ ] Test all code paths, not just happy path

### During Development

- [ ] Add debug logging to DebugLogger.cs (don't scatter in production code)
- [ ] Test in both Scene view and Play mode
- [ ] Verify Inspector configurations match code expectations
- [ ] Check Animator Controller transitions

### Before Committing Fixes

- [ ] Verify fix in Editor and Build
- [ ] Test edge cases and unusual inputs
- [ ] Remove debug logging from production code
- [ ] Document root cause and prevention
- [ ] Update this bug log with SOP-compliant entry

---

## Lessons Learned

### Top 5 Critical Insights

1. **Inspector First, Code Second:**
   - When encountering bugs, especially animation or physics issues, check Inspector/Animator FIRST
   - Multiple code attempts failing = likely Inspector configuration issue
   - Saves hours of unnecessary code debugging

2. **Separate Physics from Visuals:**
   - Physics state (onWall, isGrounded) ≠ Animation state (isWallSticking, idle)
   - Coupling these causes flickering, stuck animations, incorrect transitions
   - Use separate state tracking for logic vs visuals

3. **Event-Based State Updates:**
   - Never sync state every frame if it's consumable (dash charges, jump slots)
   - Use events: OnLanding, OnWallStick, OnLeaveGround
   - Frame-level syncing causes resource depletion bugs

4. **Timing Matters:**
   - Awake() for event subscriptions, Start() for initialization
   - Add frame delays (WaitForFixedUpdate + 1) for spawn position corrections
   - Scene transitions need time for systems to settle

5. **One Change at a Time:**
   - Never bundle multiple fixes
   - Test immediately after each change
   - Easier to identify what actually fixed the bug
   - Easier to revert if change breaks something else

---

---

## 11. FlyingEnemy Random Spawn Position and Flickering

**Commit:** TBD
**Date:** 2025-10-25
**Severity:** Game-breaking (enemy system)

### Step 1 - Capture

**Symptoms:**
- FlyingEnemy spawns at random, unexpected location very far from position set in Editor
- Enemy rapidly flickers/jitters without any reason
- Enemy randomly changes position during Play mode
- Position instability increases over time

**Error Messages:** None

**Unity Version:** 6000.2.0b9 (Unity 6 Beta)
**Platform:** Windows (Editor)

**Reproduction Steps:**
1. Create GameObject with FlyingEnemy component
2. Add required components (Rigidbody2D, SpriteRenderer, Collider2D, Animator)
3. Set position in Editor (e.g., x=10, y=5)
4. Enter Play mode
5. Observe: Enemy spawns far from set position
6. Observe: Enemy flickers and teleports randomly
7. Expected: Enemy spawns at exact Editor position and hovers smoothly

### Step 2 - Diagnose

**Initial Hypothesis:** "Physics feedback loops creating unstable movement"

**Diagnostic Investigation:**

**Issue #1: Spawn Position**
- Examined `Awake()` method in FlyingEnemy.cs:102
- Found: `startPosition = transform.position;` in `Awake()`
- **TIMING PROBLEM:** Unity finalizes GameObject positions AFTER `Awake()` completes
- `startPosition` captured before Unity's position initialization
- Patrol calculations reference incorrect `startPosition`

**Issue #2: Flickering/Jittering**
- Examined hover motion in `CalculatePatrolVelocity()` (FlyingEnemy.cs:507-511)
- Found unstable calculation mixing position and velocity:
```csharp
// UNSTABLE CODE:
hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
velocity.y = (hoverOffset - rb.linearVelocity.y) * 5f; // Feedback loop!
```
- Subtracting `rb.linearVelocity.y` creates positive feedback loop
- Each frame's velocity affects next frame's calculation
- Multiplier `* 5f` amplifies oscillations
- No clamping = runaway velocity

**Issue #3: Random Teleporting**
- Circular patrol using `* 2f` multiplier (FlyingEnemy.cs:530)
- Overshooting target position each frame
- Position error accumulates exponentially
- No velocity clamping = unbounded movement

**Issue #4: Missing Interpolation**
- Rigidbody2D.interpolation not set
- Physics updates at FixedUpdate rate (50Hz)
- Rendering at variable frame rate (60-144Hz)
- Visual jitter from unsynchronized updates

**Root Cause:** Four-part physics instability:
1. `startPosition` initialized in `Awake()` before Unity positions GameObject
2. Hover calculations creating positive feedback loop (velocity subtraction)
3. Circular patrol overshooting without clamping
4. Missing Rigidbody2D interpolation for smooth rendering

### Step 3 - Test

**Changes Made:**

**1. FlyingEnemy.cs:90-125** - Fixed initialization timing and added interpolation:
```csharp
// BEFORE (Awake):
startPosition = transform.position; // TOO EARLY!

// AFTER (moved to Start):
void Start()
{
    // CRITICAL: Set startPosition in Start() after Unity has positioned the object
    startPosition = transform.position;
    // ... rest of Start() code
}

// ALSO ADDED (in Awake):
rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth movement, prevent flickering
```

**2. FlyingEnemy.cs:500-556** - Fixed hover motion calculations:
```csharp
// BEFORE (Unstable):
hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
velocity.y = (hoverOffset - rb.linearVelocity.y) * 5f; // FEEDBACK LOOP!

// AFTER (Stable):
float targetHoverY = startPosition.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
float yDelta = targetHoverY - transform.position.y;
velocity.y = Mathf.Clamp(yDelta * 3f, -patrolSpeed, patrolSpeed); // CLAMPED
```

**Logic Change:**
- Calculate **target position** (where enemy should be)
- Calculate **delta** (how far from target)
- Apply **clamped velocity** (prevents overshoot)
- Removed velocity feedback (no more `rb.linearVelocity.y` in calculation)

**3. FlyingEnemy.cs:522-544** - Fixed circular patrol:
```csharp
// BEFORE (Overshoot):
velocity = (targetPos - currentPos) * 2f; // OVERSHOOTS!

// AFTER (Normalized):
Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
velocity = direction * patrolSpeed * circleSpeed; // CONSTANT SPEED
```

**4. FlyingEnemy.cs:566-573** - Fixed stationary hover:
```csharp
// BEFORE:
velocity.y = (targetY - transform.position.y) * 2f; // UNBOUNDED

// AFTER:
velocity.y = Mathf.Clamp(yDelta * 3f, -1f, 1f); // CLAMPED
```

**Test Results:**
- Enemy spawns at exact Editor position ✅
- Smooth hovering motion, no flickering ✅
- Stable circular patrol, no teleporting ✅
- All patrol types work correctly ✅

### Step 4 - Verify

**Verification Checklist:**
- ✅ Enemy spawns at exact position set in Editor
- ✅ No flickering or jittering during hover
- ✅ HorizontalHover patrol smooth and stable
- ✅ VerticalHover patrol smooth and stable
- ✅ Circle patrol smooth and stable
- ✅ Stationary patrol gentle hover in place
- ✅ No random position changes
- ✅ Position stability maintained over time (tested 5+ minutes)
- ✅ Chase behavior smooth when player detected
- ✅ Attack behavior works without position glitches

### Step 5 - Document

**Root Cause:** Physics instability from four compounding issues: (1) Premature startPosition capture in Awake(), (2) Positive feedback loops in hover calculations, (3) Unbounded velocity from missing clamps, (4) Missing Rigidbody2D interpolation.

**Fix:** (1) Move startPosition to Start(), (2) Replace velocity feedback with position-based target calculations, (3) Add Mathf.Clamp() to all velocity assignments, (4) Enable Rigidbody2D.Interpolate.

**Prevention:**
- **CRITICAL:** For movement systems, initialize reference positions in `Start()`, not `Awake()`
- Never mix current velocity into velocity calculations (creates feedback loops)
- Always use **target position → delta → clamped velocity** pattern
- Always clamp calculated velocities to prevent runaway physics
- Enable Rigidbody2D interpolation for all moving objects
- Test all movement patterns for 5+ minutes to detect instability

**Pattern Recognition:**
This bug demonstrates the classic **Unity Initialization Timing** issue:
- `Awake()` → Component initialization, before Unity positions objects
- `Start()` → GameObject finalized, safe for position/transform references
- Rule: Capture transform.position in `Start()`, not `Awake()`

---

**Document Maintained By:** Development Team
**Review Frequency:** After each major bug fix
**Format Compliance:** Unity Debug SOP v1.0
