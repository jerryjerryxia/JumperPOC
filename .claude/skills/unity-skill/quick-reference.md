# Unity Quick Reference

## THE GOLDEN RULE
**NEVER READ CONSOLE WITHOUT COMPILING FIRST**

## Standard Fix Workflow
```
1. Read console error
2. Edit .cs file  
3. Switch to Unity (triggers compile)
4. Wait 3-5 seconds
5. Read console again (NEW state)
6. Repeat if needed
```

## Common Errors

**CS0103**: Name doesn't exist → Check spelling/using statements  
**CS0246**: Type not found → Add using statement  
**CS1002**: ; expected → Add semicolon  
**NullReferenceException**: Object not assigned → Null check + Inspector assignment  

## Code Best Practices

```csharp
// ✅ Tunable values
[SerializeField] private float speed = 5f;

// ✅ Cache components
private Rigidbody2D rb;
void Awake() { rb = GetComponent<Rigidbody2D>(); }

// ✅ Null checks
if (player == null) return;

// ✅ Physics in FixedUpdate
void FixedUpdate() { rb.MovePosition(...); }

// ✅ Input in Update  
void Update() { Input.GetKey(...); }
```

## Anti-Patterns
❌ Edit → Read console (no compile!)  
❌ Magic numbers (use [SerializeField])  
❌ GetComponent in Update  
❌ GameObject.Find every frame  
❌ No null checks  

## Mental Model
```
Edit File → Unity Compiles → Changes Live → Test
            ↑ THIS STEP IS REQUIRED! ↑
```

Changes don't exist until Unity compiles!
