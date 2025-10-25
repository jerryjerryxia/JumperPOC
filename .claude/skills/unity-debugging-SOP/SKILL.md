---
name: unity-debug-sop
description: Systematic debugging for Unity. Use for Unity errors, bugs, unexpected behavior, crashes, NullReferenceException, input issues, physics problems, or when asked to debug/fix/troubleshoot Unity code.
---

# Unity Debug SOP

Enforce this debugging protocol for all Unity bugs. **Critical: ONE CHANGE AT A TIME.**

## Protocol

### 1. Capture State
```bash
# Required before ANY changes:
- Exact error message + stack trace
- Unity version, target platform
- Reproduction steps (exact sequence)
- Run project, confirm bug exists
```

### 2. Diagnose
Form ONE testable hypothesis. Add logging BEFORE changing code.

**CRITICAL: All debug logs go in `DebugLogger.cs`**
```csharp
// Create/update DebugLogger.cs with all debugging logs
public static class DebugLogger
{
    public static void LogPlayerState(string method, object value, bool active)
    {
        Debug.Log($"[PlayerController.{method}] Value={value}, Active={active}");
    }
    
    // Add all debugging methods here
}

// Use from other scripts:
DebugLogger.LogPlayerState("Update", transform.position, gameObject.activeSelf);
```

This keeps debug code centralized for easy removal after debugging.

### 3. Test
- Make EXACTLY ONE change
- Test immediately  
- Fixed? → Verify (step 4)
- Not fixed? → Revert, new hypothesis

### 4. Verify
```bash
# All must pass:
✓ Original repro steps no longer trigger bug
✓ Test edge cases
✓ Check Console for new errors
✓ Test in Build (not just Editor)
✓ No regressions
```

### 5. Document
Record: what broke, root cause, fix, prevention.

**After successful fix:**
- Remove or comment out `DebugLogger.cs` methods used for this bug
- Remove DebugLogger calls from production code
- Keep file structure for future debugging sessions

## Unity-Specific Patterns

### NullReferenceException

**Priority checks:**
1. Inspector assignment missing
2. Timing (before Awake/Start)
3. GameObject inactive/destroyed

```csharp
// Always null check before use
if (component == null) {
    Debug.LogError($"Missing on {gameObject.name}", this);
    return;
}
```

### Input System Not Working

**Checklist:**
1. PlayerInput component exists + enabled?
2. Input Action Asset assigned?
3. Correct Action Map enabled?
4. Actions enabled: `playerInput.actions.Enable()`?

```csharp
// Wrong - reading outside callback
void Update() { 
    var v = moveAction.ReadValue<Vector2>(); // ERROR
}

// Right - cache in callback
Vector2 _input;
void OnMove(InputAction.CallbackContext c) { 
    _input = c.ReadValue<Vector2>(); 
}
```

### Physics Issues

**Collisions not detecting:**
- Both have Colliders? One has Rigidbody?
- Layer collision matrix allows? (Edit > Project Settings > Physics)
- OnTrigger: Is Trigger checked?
- Fast objects: Set Rigidbody Collision Detection = Continuous

**Objects pass through:**
```csharp
rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
```

### Common Gotchas

**Coroutines stop when:**
- GameObject disabled
- Component disabled  
- Scene unloaded

**Serialization:**
- Properties don't serialize (use fields)
- Private needs `[SerializeField]`

**Execution Order:**
- Awake → OnEnable → Start (per-object, not global)
- Physics in FixedUpdate, input in Update
- Use Script Execution Order for dependencies

**GetComponent returns null:**
```csharp
// Cache in Start, not Update
private Rigidbody _rb;
void Start() { _rb = GetComponent<Rigidbody>(); }
```

**GameObject.Find issues:**
- Case-sensitive
- Won't find inactive objects
- Use SerializeField references instead

## Tools

**Console:** Clear before test, enable "Error Pause"

**Profiler (Window > Analysis > Profiler):** Performance issues

**Input Debugger (Window > Analysis > Input Debugger):** Input System state

**Frame Debugger (Window > Analysis > Frame Debugger):** Rendering issues

## Decision Tree

```
Bug encountered
  ↓
Capture error message + repro steps
  ↓
Can reproduce? → No: Try multiple times with variation
               → Yes: ↓
  ↓
Recognize pattern? → Yes: Use category checklist
                   → No: Add logging
  ↓
Form hypothesis
  ↓
Make ONE change → Test
  ↓
Fixed? → No: Revert, next hypothesis
       → Yes: ↓
  ↓
Verify (all checks) → Document → Done
```

## Critical Rules

1. **ONE CHANGE AT A TIME** - Never bundle fixes
2. **Log before changing** - Understand before modifying
3. **Test in Build** - Editor != Build
4. **Verify thoroughly** - Symptoms can hide deeper issues
5. **Commit after fix** - Clean version control

## Example

**Bug:** Player movement broken

**Step 1 - Capture:**
```
Error: None
Symptom: WASD does nothing
Unity: 2022.3.10f1
Input System: 1.7.0
Repro: Always, in GameplayScene
```

**Step 2 - Diagnose:**
Hypothesis: "Input callback not firing"
```csharp
// In DebugLogger.cs:
public static void LogInputCallback(Vector2 value)
{
    Debug.Log($"[PlayerController.OnMove] Called: {value}");
}

// In PlayerController.cs:
void OnMove(InputAction.CallbackContext c) {
    DebugLogger.LogInputCallback(c.ReadValue<Vector2>());
    // existing code
}
```
Result: No log → callback never fires

**Step 3 - Test:**
New hypothesis: "PlayerInput component missing"
Check Inspector → component missing
Add PlayerInput component, assign action asset

**Step 4 - Verify:**
- ✓ WASD moves player
- ✓ Works in build
- ✓ No console errors

**Step 5 - Document:**
```
Bug: No input response
Cause: PlayerInput component not attached
Fix: Added PlayerInput, assigned actions
Prevention: Prefab checklist
```
Clean up: Remove `LogInputCallback` from DebugLogger.cs and all calls to it.
