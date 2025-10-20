# Unity Development Skill

## Overview
This skill enables Claude to work effectively with Unity projects through Windows MCP, understanding Unity's compilation model, debugging workflows, and best practices for game development.

## Core Principles

### 1. Unity Compilation Workflow
**CRITICAL**: Unity must recompile C# scripts before changes take effect.

```
Workflow:
1. Modify .cs file → Save
2. Switch to Unity Editor (triggers compilation)
3. Wait for compilation to complete
4. THEN check Console/test
```

**NEVER:**
- Read console immediately after code changes
- Assume changes are live without compilation
- Make multiple edits before first compilation
- Test in Play Mode with uncompiled changes

**ALWAYS:**
- Switch to Unity after saving files
- Wait for spinning "compiling" icon to stop
- Check for compilation errors before testing
- Verify "All compiler errors have to be fixed" message

### 2. Error Investigation Pattern

```
CORRECT WORKFLOW:
1. Read Console errors
2. Analyze error messages
3. Make ONE targeted fix
4. Save file
5. Switch to Unity → Wait for compilation
6. If errors remain → Read NEW console state
7. Repeat

INCORRECT WORKFLOW:
❌ Read console → Make fix → Immediately read console again (old errors!)
❌ Make multiple changes → Check console once
❌ Read console without compilation step
```

### 3. Console Reading Best Practices

**When to Read Console:**
- ✅ After Unity compilation completes
- ✅ When Unity Play Mode errors occur
- ✅ Before starting work (initial state)
- ✅ After Play Mode ends (runtime errors)

**When NOT to Read Console:**
- ❌ Immediately after saving .cs files
- ❌ Multiple times without code changes
- ❌ Before compilation finishes
- ❌ "Just to check" without cause

### 4. Unity-Specific File Operations

**Script Creation:**
```csharp
// Unity expects specific naming conventions
// File name MUST match class name

// ✅ CORRECT
// File: PlayerController.cs
public class PlayerController : MonoBehaviour { }

// ❌ WRONG
// File: PlayerController.cs
public class Player_Controller : MonoBehaviour { }
```

**Common Directories:**
```
Assets/
├── Scripts/         (C# scripts)
├── Scenes/          (.unity scene files)
├── Prefabs/         (.prefab files)
├── Materials/       (.mat files)
├── Textures/        (images)
└── Resources/       (runtime-loadable assets)
```

**File Extensions to Know:**
- `.cs` - C# scripts (require compilation)
- `.unity` - Scene files (text-based YAML)
- `.prefab` - Prefab files (text-based YAML)
- `.meta` - Unity metadata (auto-generated, usually ignore)
- `.asset` - ScriptableObject data files

### 5. Code Modification Patterns

**Incremental Changes:**
```
PREFERRED:
1. Fix one issue
2. Compile & verify
3. Fix next issue
4. Compile & verify

AVOID:
1. Fix five issues at once
2. Compile
3. Debug which fix broke something
```

**Testing Changes:**
```
WORKFLOW:
1. Make change to .cs file
2. Save
3. Switch-Tool to Unity
4. Wait for compilation
5. State-Tool (check for compiler errors in Console)
6. If clean → Enter Play Mode for testing
7. If errors → Fix and repeat from step 1
```

### 6. Common Unity Pitfalls

**Magic Numbers:**
```csharp
// ❌ BAD
if (distance < 5.0f) { }
speed = 10.0f;

// ✅ GOOD
[SerializeField] private float triggerDistance = 5.0f;
[SerializeField] private float moveSpeed = 10.0f;
```

**Null References:**
```csharp
// ❌ BAD - Will throw at runtime
private GameObject player;
void Start() {
    player.transform.position = Vector3.zero; // NullReferenceException!
}

// ✅ GOOD - Defensive
private GameObject player;
void Start() {
    player = GameObject.FindGameObjectWithTag("Player");
    if (player == null) {
        Debug.LogError("Player not found!");
        return;
    }
    player.transform.position = Vector3.zero;
}

// ✅ BETTER - Inspector assignment
[SerializeField] private GameObject player;
void Start() {
    if (player == null) {
        Debug.LogError("Player reference not assigned in Inspector!");
        return;
    }
    player.transform.position = Vector3.zero;
}
```

**Performance - Update vs FixedUpdate:**
```csharp
// Movement code → FixedUpdate (physics)
void FixedUpdate() {
    rb.MovePosition(transform.position + direction * speed * Time.fixedDeltaTime);
}

// Input reading → Update (every frame)
void Update() {
    direction = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
}
```

**Memory Leaks:**
```csharp
// ❌ BAD - Creates garbage every frame
void Update() {
    Vector3 pos = new Vector3(x, y, z); // Allocates!
}

// ✅ GOOD - Reuse or use static
private Vector3 cachedPosition;
void Update() {
    cachedPosition.Set(x, y, z); // No allocation
}
```

### 7. Unity Console Error Patterns

**Common Error Types:**

**Compilation Errors (Fix FIRST):**
```
error CS0103: The name 'varName' does not exist
error CS0246: The type or namespace 'TypeName' could not be found
error CS1002: ; expected
error CS0029: Cannot implicitly convert type 'int' to 'float'
```
→ Script won't compile. Fix before testing.

**Runtime Errors (Appear in Play Mode):**
```
NullReferenceException: Object reference not set to an instance
IndexOutOfRangeException: Index was outside the bounds of the array
MissingReferenceException: The object of type 'X' has been destroyed
```
→ Logic errors. Require debugging.

**Warnings (Non-blocking but should fix):**
```
warning CS0649: Field 'X' is never assigned to
warning CS0618: 'X' is obsolete
```
→ Code works but has issues.

### 8. Windows MCP Tool Usage for Unity

**Reading Unity Console:**
```
1. Switch-Tool: "Unity" (bring Unity to front)
2. State-Tool: use_vision=True (capture Console window)
3. Look for Console tab in hierarchy
4. Parse error messages
```

**Editing Unity Scripts:**
```
1. Identify correct .cs file path
2. Use Filesystem:read_file to view current code
3. Use Filesystem:edit_file for changes
4. Switch-Tool: "Unity" to trigger compilation
5. Wait-Tool: 3-5 seconds (compilation time)
6. State-Tool to verify compilation succeeded
```

**Testing in Play Mode:**
```
1. Ensure compilation complete
2. Click-Tool on Play button (top center of Unity)
3. Wait-Tool: 1-2 seconds
4. State-Tool to observe game state
5. Click Play again to exit (errors persist after stopping)
```

### 9. Debugging Workflow

**Step-by-Step Process:**
```
1. OBSERVE: Read console, identify error type
2. LOCATE: Find relevant .cs file and line number
3. UNDERSTAND: Read surrounding code context
4. PLAN: Determine fix approach
5. FIX: Make ONE targeted change
6. COMPILE: Switch to Unity, wait
7. VERIFY: Read new console state
8. TEST: Enter Play Mode if compilation clean
9. ITERATE: Repeat if issues remain
```

**When Stuck:**
- Read more context around error location
- Check Unity API documentation (common pattern issues)
- Verify variable types match expected types
- Check for null references
- Look for uninitialized variables
- Verify Inspector references are assigned

### 10. Best Practices Summary

**DO:**
- ✅ Make small, incremental changes
- ✅ Wait for compilation after each change
- ✅ Read console only after compilation
- ✅ Use [SerializeField] for tunable values
- ✅ Add null checks for object references
- ✅ Use descriptive variable names
- ✅ Extract magic numbers to named constants
- ✅ Test in Play Mode after compilation succeeds

**DON'T:**
- ❌ Read console without compiling
- ❌ Make multiple changes before testing
- ❌ Ignore compilation warnings
- ❌ Use magic numbers
- ❌ Assume references are assigned
- ❌ Test with compilation errors present
- ❌ Edit files while Unity is compiling
- ❌ Skip null checks on GameObjects

### 11. Unity-Specific Code Patterns

**Component Access:**
```csharp
// ✅ Cache in Start/Awake
private Rigidbody2D rb;
void Awake() {
    rb = GetComponent<Rigidbody2D>();
}

// ❌ Don't call repeatedly
void Update() {
    GetComponent<Rigidbody2D>().velocity = Vector2.zero; // Expensive!
}
```

**Object Finding:**
```csharp
// ✅ Find once, cache result
private GameObject player;
void Start() {
    player = GameObject.FindGameObjectWithTag("Player");
}

// ❌ Don't find every frame
void Update() {
    GameObject.FindGameObjectWithTag("Player"); // Very expensive!
}
```

**Coroutines vs Update:**
```csharp
// Use coroutines for timed actions
IEnumerator DelayedAction() {
    yield return new WaitForSeconds(2f);
    DoSomething();
}

// Use Update for continuous checks
void Update() {
    if (Input.GetKeyDown(KeyCode.Space)) {
        Jump();
    }
}
```

### 12. Project Structure Awareness

**When Reading Code:**
- Check for namespace usage
- Understand inheritance (MonoBehaviour, ScriptableObject)
- Look for [RequireComponent] attributes
- Note [SerializeField] vs public fields

**When Modifying Code:**
- Maintain existing code style
- Preserve [Header] and [Tooltip] attributes
- Keep serialized fields in logical groups
- Don't break existing Inspector layouts

### 13. Common Compilation Issues

**Missing Using Statements:**
```csharp
// Error: The type or namespace name 'List<>' could not be found
// Fix: Add using System.Collections.Generic;
```

**Incorrect Type Usage:**
```csharp
// Error: Cannot implicitly convert type 'float' to 'int'
// Fix: Use explicit cast (int)floatValue or change type
```

**Access Modifiers:**
```csharp
// Error: 'ClassName.PrivateMethod()' is inaccessible due to its protection level
// Fix: Change private to public, or use proper accessor
```

**Script Name Mismatch:**
```csharp
// File: PlayerMovement.cs
// Error: The type 'PlayerMove' does not match the file name
// Fix: Rename class to PlayerMovement
```

---

## Integration with Claude Workflow

When working with Unity projects:

1. **Initial Assessment**: Use State-Tool to understand Unity's current state
2. **File Operations**: Use Filesystem tools to read/modify .cs files
3. **Compilation Trigger**: Use Switch-Tool to bring Unity to foreground
4. **Verification**: Use State-Tool (with vision) to check Console
5. **Testing**: Use Click-Tool to enter/exit Play Mode
6. **Iteration**: Repeat with small, verified changes

**Remember**: Unity is a visual editor. Many issues (missing references, incorrect settings) can only be seen in Unity Inspector, not in code files. When debugging runtime issues, always consider what might be misconfigured in the editor.

---

## Quick Reference

**Compilation Check:**
```
Switch-Tool: "Unity" → Wait-Tool: 3 → State-Tool: use_vision=True
```

**Play Mode Test:**
```
Click-Tool: Play Button → Wait-Tool: 2 → State-Tool → Click-Tool: Play Button (stop)
```

**Error Fix Loop:**
```
Read Console → Edit File → Switch to Unity → Wait → Verify → Test
```

**Never Do:**
```
Edit → Read Console (NO COMPILATION STEP)
```

This skill ensures efficient Unity development through proper understanding of compilation requirements and debugging workflows.
