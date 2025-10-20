---
name: unity-2d-platformer-analysis
description: Comprehensive analysis skill for Unity 2D platformer games. Use when asked to analyze, review, audit, or evaluate a Unity game project. Examines architecture, code quality, performance, Unity best practices, gameplay systems, and provides actionable recommendations.
---

# Unity 2D Platformer Analysis

This skill provides a comprehensive framework for analyzing Unity 2D platformer game projects. Use this when tasked with reviewing, auditing, or evaluating a Unity game codebase.

## Analysis Workflow

Follow this structured approach for thorough project analysis:

### 0. ⚠️ CRITICAL: Check Inspector & Animator Configuration FIRST

**BEFORE diving into code changes or complex debugging, ALWAYS verify Unity Inspector and Animator settings:**

This is the **single most important step** to avoid wasting time on unnecessary code fixes.

**Animator Controller Configuration:**
- Use Unity MCP tools: `mcp__UnityMCP__manage_asset` to inspect animator controller files
- Check transition conditions between animation states
- Verify parameter names match exactly (e.g., `IsWallSticking` vs `IsWallSliding`)
- Check transition order/priority - higher transitions are evaluated first
- Verify `Has Exit Time` settings on transitions
- Check blend tree configurations and thresholds

**Inspector Values:**
- Use `mcp__UnityMCP__manage_gameobject` with `get_components` to read all component properties
- Verify serialized field values match code defaults
- Check if Inspector overrides are causing unexpected behavior
- Validate prefab overrides haven't diverged from code

**Common Inspector/Animator Issues That Masquerade as Code Bugs:**
1. **Wrong parameter names** in animator transitions (e.g., `IsWallSliding` instead of `IsWallSticking`)
2. **Incorrect transition conditions** (missing conditions, wrong comparison operators)
3. **Transition priority issues** (important transitions blocked by less important ones)
4. **Inspector values** overriding code defaults (e.g., duration = 0.8 in Inspector vs 0.05 in code)
5. **Missing references** in Inspector (null GameObject/Component references)
6. **Layer/Tag misconfigurations** causing physics or collision issues
7. **Animation events** calling wrong methods or with wrong timing

**Process:**
1. When encountering animation bugs → Check Animator Controller transitions FIRST
2. When parameters seem wrong → Check Inspector values FIRST
3. When nothing makes sense → Verify ALL Unity configurations before touching code

**Example from real debugging session:**
- Symptom: Player stuck in idle animation after dash attack near wall, despite `isWallSticking=True`
- Attempted code fixes: Timing delays, frame synchronization, force-transitions, state checking
- Actual cause: Animator transition had `IsWallSliding` instead of `IsWallSticking`
- Time wasted: ~2 hours of unnecessary code changes
- Fix: 30 seconds to change one animator transition condition

### 1. Project Discovery

**Explore the project structure:**
- Identify Unity version (check `ProjectSettings/ProjectVersion.txt`)
- Map the `Assets/` folder structure
- Locate key directories: Scripts, Scenes, Prefabs, Materials, Audio, etc.
- Check for existing documentation in `docs/` or similar folders
- Identify any third-party packages or plugins

**Generate project tree:**
```bash
# Use appropriate file system tools to map the directory structure
# Focus on Assets/, Scripts/, Scenes/, and ProjectSettings/
```

### 2. Code Architecture Analysis

**Examine core systems:**
- **Player systems**: Movement, combat, abilities, state management
- **Enemy systems**: AI, behavior patterns, spawning
- **Game management**: Scene loading, save/load, game state
- **UI systems**: Menus, HUD, dialogs
- **Physics & collision**: Custom collision handling, triggers
- **Audio management**: Sound effects, music, audio pooling

**Architecture patterns to identify:**
- Singleton usage (managers)
- Observer pattern (events/delegates)
- State machines (player/enemy states)
- Object pooling
- Dependency injection or service locators
- ScriptableObject usage for data

### 3. Code Quality Assessment

**Review for:**
- **Naming conventions**: PascalCase for classes/methods, camelCase for fields
- **Code organization**: Single Responsibility Principle adherence
- **Performance considerations**: Update vs FixedUpdate usage, caching, garbage collection
- **Unity lifecycle awareness**: Proper use of Awake, Start, OnEnable, OnDisable
- **Serialization**: `[SerializeField]` vs public fields
- **Null safety**: Null checks, missing reference handling
- **Magic numbers**: Use of constants or ScriptableObjects for configuration
- **Comments and documentation**: XML comments, inline explanations

Refer to `references/unity-best-practices.md` for detailed guidelines.

### 4. Gameplay Systems Analysis

**For 2D platformers, examine:**
- **Movement mechanics**: Ground detection, jump physics, coyote time, jump buffering
- **Combat system**: Attack patterns, hitboxes, damage calculation, invincibility frames
- **Enemy behavior**: Patrol patterns, player detection, attack logic
- **Level design systems**: Checkpoints, respawn, level transitions
- **Collectibles & progression**: Item pickup, inventory, upgrades
- **Environmental interactions**: Platforms, hazards, breakable objects

Refer to `references/platformer-patterns.md` for common implementations.

### 5. Performance & Optimization Review

**Check for:**
- Excessive `GetComponent<>()` calls (should be cached in Awake/Start)
- Camera.main usage (should be cached)
- FindObjectOfType in Update loops
- Excessive instantiation/destruction (use object pooling)
- Unoptimized physics (excessive colliders, raycasts)
- Large Update/FixedUpdate loops
- Memory leaks (event subscriptions not cleaned up)
- Texture sizes and sprite atlasing
- Audio clip loading (streaming vs decompressed)

### 6. Unity-Specific Best Practices

**Verify:**
- Proper layer and tag usage
- Physics layers and collision matrix configuration
- Input system (old Input Manager vs new Input System)
- Animation controller organization
- Prefab organization and variants
- Scene structure and hierarchy
- Asset organization and naming

### 7. Testing & Debugging Infrastructure

**Assess:**
- Debug visualization (Gizmos, Debug.DrawRay)
- Editor tools and custom inspectors
- Cheat codes or debug menus
- Error handling and logging
- Unit tests (if any)

## Analysis Output Format

Structure the analysis report as follows:

### Executive Summary
- Project overview (Unity version, scope, key features)
- Overall assessment (1-3 paragraphs)
- Critical issues (if any)
- Top 3-5 recommendations

### Detailed Findings

#### 1. Architecture & Design
- Current architecture overview
- Strengths
- Weaknesses
- Recommendations

#### 2. Code Quality
- Overall code quality rating
- Specific issues by category (with file/line references)
- Positive patterns observed
- Refactoring suggestions

#### 3. Gameplay Systems
- Per-system evaluation (player, enemies, etc.)
- Implementation quality
- Polish opportunities

#### 4. Performance & Optimization
- Performance bottlenecks identified
- Memory usage concerns
- Specific optimization recommendations

#### 5. Unity Best Practices Compliance
- Areas of compliance
- Violations or anti-patterns
- Unity-specific improvements

### Actionable Recommendations

Prioritize recommendations by impact and effort:
- **Quick wins** (high impact, low effort)
- **Important improvements** (high impact, medium effort)
- **Future considerations** (lower priority or larger scope)

## Using Helper Scripts

### Project Statistics

Use `scripts/analyze_unity_project.py` to gather metrics:

```bash
python scripts/analyze_unity_project.py /path/to/unity/project
```

This generates:
- Line counts by file type
- Script complexity metrics
- Dependency graphs
- Unused assets detection

## Analysis Checklist

Use this checklist to ensure thorough coverage:

- [ ] Project structure mapped
- [ ] Unity version and settings reviewed
- [ ] All core systems identified and evaluated
- [ ] Code quality assessment completed
- [ ] Performance review conducted
- [ ] Unity best practices verified
- [ ] Existing documentation reviewed
- [ ] Helper script metrics gathered
- [ ] Report structured with clear recommendations
- [ ] Prioritized action items provided

## Tips for Effective Analysis

1. **Start broad, then deep**: Get the big picture first, then dive into specific systems
2. **Reference existing docs**: Check for READMEs, comments, or documentation folders
3. **Look for patterns**: Identify repeated code structures or anti-patterns
4. **Consider context**: Some "violations" may be intentional for valid reasons
5. **Be constructive**: Frame issues as opportunities for improvement
6. **Prioritize impact**: Focus on issues that affect gameplay, performance, or maintainability
7. **Provide examples**: Reference specific files and line numbers when noting issues
8. **Balance criticism with praise**: Acknowledge good practices and clever solutions
