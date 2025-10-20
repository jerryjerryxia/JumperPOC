# Unity Development Skill

## What This Skill Does

This skill teaches Claude how to work effectively with Unity projects through Windows MCP, with special emphasis on:

- **Understanding Unity's compilation model** (changes require recompilation)
- **Proper console reading workflow** (never read without compiling first)
- **Debugging patterns** (incremental fixes with verification)
- **Unity best practices** (SerializeFields, null checks, performance)
- **Common pitfalls** (magic numbers, memory leaks, Update vs FixedUpdate)

## Installation

1. Copy this folder to your Claude skills directory
2. Claude will automatically reference it when working with Unity projects

## Key Insight

**The #1 mistake**: Reading Unity Console after making code changes WITHOUT compiling first.

```
❌ WRONG: Edit code → Read console (shows OLD errors)
✅ RIGHT: Edit code → Switch to Unity → Wait → Read console (shows NEW state)
```

## What's Included

- **SKILL.md**: Complete Unity development guide
- **README.md**: This file
- **quick-reference.md**: Cheat sheet for common workflows

## When Claude Should Use This Skill

- Working with `.cs` files in a Unity project
- Debugging Unity console errors
- Modifying Unity scripts
- Testing Unity gameplay in Play Mode
- Any Unity development task through Windows MCP

## Quick Start

When you ask Claude to work with Unity, it should:
1. Read this SKILL.md first
2. Follow the compilation workflow
3. Never read console without compiling
4. Make incremental, tested changes

## Example Usage

**You**: "Fix the NullReferenceException in PlayerController.cs"

**Claude should**:
1. Read the SKILL.md
2. Read PlayerController.cs
3. Read Unity console (current state)
4. Make ONE fix
5. Switch to Unity (trigger compilation)
6. Wait for compilation
7. Read console (NEW state)
8. Verify fix worked

**Claude should NOT**:
1. Make fix
2. Immediately read console (old errors!)
3. Get confused why error still shows

---

This skill was created to prevent the common mistake of checking results before Unity has compiled the changes.
