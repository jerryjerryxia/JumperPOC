# Claude Code Configuration - SPARC Development Environment

## 🚨 CRITICAL: CONCURRENT EXECUTION & FILE MANAGEMENT

**ABSOLUTE RULES**:
1. ALL operations MUST be concurrent/parallel in a single message
2. **NEVER save working files, text/mds and tests to the root folder**
3. ALWAYS organize files in appropriate subdirectories

### ⚡ GOLDEN RULE: "1 MESSAGE = ALL RELATED OPERATIONS"

**MANDATORY PATTERNS:**
- **TodoWrite**: ALWAYS batch ALL todos in ONE call (5-10+ todos minimum)
- **Task tool**: ALWAYS spawn ALL agents in ONE message with full instructions
- **File operations**: ALWAYS batch ALL reads/writes/edits in ONE message
- **Bash commands**: ALWAYS batch ALL terminal operations in ONE message
- **Memory operations**: ALWAYS batch ALL memory store/retrieve in ONE message

### 📁 File Organization Rules

**NEVER save to root folder. Use these directories:**
- `/src` - Source code files
- `/tests` - Test files
- `/docs` - Documentation and markdown files
- `/config` - Configuration files
- `/scripts` - Utility scripts
- `/examples` - Example code

## Project Overview

# important-instruction-reminders
Do what has been asked; nothing more, nothing less.
NEVER create files unless they're absolutely necessary for achieving your goal.
ALWAYS prefer editing an existing file to creating a new one.
NEVER proactively create documentation files (*.md) or README files. Only create documentation files if explicitly requested by the User.
Never save working files, text/mds and tests to the root folder.

## Project Overview

This is a Unity 2D platformer Metroidvania proof-of-concept (POC) game built with Unity 6000.1.4f1. The project features advanced movement mechanics including wall sliding, dashing, ledge grabbing, and a 3-hit combo system. The ultimate goal of this project is to build a vertical Metroidvania that features rich movement ability progression and extensive map exploration and backtracking. The vertical map design is inspired by Dark Souls 1 and the smooth and extremely polished movement feel is inspired by ENDER LILIES: Quietus of the Knights. The combat is inspired by Ori and the Blind Forest, where defeating an enemy is not the player's only goal: they can also use enemies as leverage to reach otherwise unreachable platforms. More of the game design elements will be updated here once they become clear, but use the aforementioned games as guidelines for development for now.

## 🎯 Core Development Philosophy

**MISSION:** Ship a playable, fun, stable game as fast as possible.

**PRINCIPLE:** Good enough code that ships beats perfect code that doesn't.

### The Iron Rules

1. **Ship First, Perfect Later**
   - Does it work in PlayMode? ✅ → Good enough
   - Can I add features? ✅ → Good enough
   - Can I fix bugs in <30 min? ✅ → Good enough
   - Is the code "perfect"? ← **Don't care until v2.0**

2. **Players Don't See Code**
   - Focus 80% effort on what players experience (movement feel, visuals, level design)
   - Focus 20% effort on code quality (tests, architecture, refactoring)
   - Players experience: jump height, dash distance, animations, combat satisfaction
   - Players DON'T experience: code architecture, test coverage, design patterns

3. **If It Works, Don't Touch It**
   - Current monolithic PlayerController (2,820 lines)? ✅ **KEEP IT. IT WORKS.**
   - Bad reasons to refactor: "This file is too big", "Could be more modular", "Best practices say X"
   - Good reasons to refactor: "Cannot add feature X without rewriting", "Every bug takes >2 hours to find"
   - **Only refactor when actively blocked by current architecture**

### The Three-Strike Refactoring Rule

**Only refactor when ALL THREE are true:**
1. ✅ Clear pain point - "I cannot add feature X" or "Every bug takes hours"
2. ✅ Specific solution - "Merging A and B will fix it" (not "maybe refactor will help")
3. ✅ Time-boxed - "12 hours max" (not "refactor until perfect")

**If even ONE is false → Don't refactor.**

### The 30-70 Testing Strategy

**NOT pure TDD. NOT zero tests. Strategic testing.**

- **30% of code** has unit tests (logic, state, rules, critical game mechanics)
- **70% of code** tested via PlayMode (feel, visuals, UX, game balance)

**Write tests when:**
- ✅ Stuck debugging >15 minutes (write failing test, then fix)
- ✅ Adding complex logic (combo systems, AI state machines, ability unlocks)
- ✅ Refactoring working code (tests catch regressions)
- ✅ Critical game rules (air attack limits, dash cooldown, coyote time)

**DON'T write tests for:**
- ❌ Game feel / tuning (jump height, dash distance, wall slide speed)
- ❌ Visual systems (animations, sprites, VFX, UI layout)
- ❌ Systems that work (don't test until they break)
- ❌ Simple getters/setters

**Target: 20-30% code coverage** (not 80%)

### Acceptable vs. Unacceptable Technical Debt

**Acceptable for v1.0:**
- ✅ Large files (even 2000+ lines if they work)
- ✅ Some code duplication
- ✅ TODOs and commented code
- ✅ Untested systems that work
- ✅ Non-optimal architecture
- ✅ Debug logs left in code

**Unacceptable (Fix Before Shipping):**
- ❌ Game-breaking bugs
- ❌ Cannot add new features
- ❌ Cannot find/fix bugs
- ❌ Performance issues affecting gameplay

### The Three Questions (For ANY Decision)

When facing ANY development decision, ask:

1. **"Does this get me closer to shipping?"**
   - YES → Do it
   - NO → Skip it

2. **"Will players experience this?"**
   - YES → High priority
   - NO → Low priority

3. **"Am I actually blocked, or just uncomfortable with imperfect code?"**
   - Blocked → Fix the blocker
   - Uncomfortable → Ship anyway

### Time Allocation (Solo Dev)

```
40% - Building features (movement, combat, abilities)
30% - Creating content (levels, enemies, bosses)
15% - Polish (feel, VFX, sound, juice)
10% - Playtesting and iteration
5%  - Code quality (refactoring, tests, cleanup)

NOT:
50% - Code quality ← TRAP
20% - Architecture planning ← TRAP
10% - Learning new tools ← TRAP
```

### Red Flags (You're Off Track)

**Stop and re-evaluate if:**
- ⚠️ Haven't added player-facing feature in >1 week
- ⚠️ Spending >20% time on refactoring
- ⚠️ Writing more tests than game code
- ⚠️ Researching architecture patterns instead of building
- ⚠️ Rewriting working code to be "better"
- ⚠️ Adding features players won't see (dev tools, debug systems)

**Get back on track:** Focus on shippable features.

### Success Metric

- ✅ **Game shipped in 3-6 months**
- ❌ **Perfect code that never releases**

**Full details:** See `docs/DEVELOPMENT_PHILOSOPHY.md`

---

## Interaction with the user

Always look critically at the project when asked to perform analysis on the project. Do the same for the prompt that the user inputs. You must point out potential issues in my prompts or things that I might have not considered. I am new to Unity 2D games development, so you must guide me through this development. When I ask for things that are clearly not making sense, don't ever try to make me feel good. You must directly point out what I have missed and let me know why I am wrong. 

When the user reports a bug and the code fixes repeatedly fail, it is highly likely that the issue lies in the editor. So when trying to fix a bug, always take a close look at the editor and inspector setup along with the code base - doing so have helped us 100% of the times, so you must take editor setup into consideration when attempting any bug fix. 

Version control is essential. After a version of the code base is fully tested and confirmed functional, commit it onto github. Never stage or commit anything when the change is not tested by the user - only commit when the user confirms that the change is ready to commit. 

## Developer Information

**Git Configuration:**
- Email: jerryjerryxia@gmail.com
- Name: jerryjerryxia

---

## Unity Quick Reference

**Unity Version:** 6000.1.4f1

**Run Tests:**
- Unity Editor: Window → General → Test Runner → EditMode → Run All
- Command Line: `Unity.exe -runTests -batchmode -projectPath . -testPlatform EditMode`
- **Current Suite:** ~98 tests across 6 files (PlayerAbilities, PlayerStateTracker, PlayerMovement, PlayerHealth, PlayerJumpSystem, PlayerCombat)
- **Docs:** `docs/How_To_Run_Tests.md`, `docs/Testing_Guide.md`

**Build Project:**
- Prefer Unity Editor GUI: File → Build Settings
- Command Line: `"C:\Program Files\Unity\Hub\Editor\6000.1.4f1\Editor\Unity.exe" -batchmode -quit -projectPath . -buildTarget StandaloneWindows64`

## Architecture Overview

**Current Approach:** Monolithic Player System (4 Components, ~4,110 lines) + Simplified Enemy System

**Philosophy:** Pragmatic monolith. Works great for solo dev shipping v1.0. Refactor in v2.0 if game succeeds.

### Player System (4 Components)

**Core Player Components:**

1. **`PlayerController.cs` (2,820 lines)** - Monolithic controller
   - **Movement:** Running, dashing, wall sliding, ledge climbing, slope handling
   - **Jump System:** Variable jump (Hollow Knight style), double jump, wall jump, coyote time
   - **Ground Detection:** Slope detection, landing buffer, moving platform tracking
   - **Wall Detection:** Triple raycast system for wall sliding/sticking
   - **Input Handling:** Input buffering, queue management
   - **Animation State:** 22 animator parameters, state synchronization
   - **Death/Respawn:** Death zones, checkpoint system

   **Why monolithic?** Tried splitting into 11+ components, caused state synchronization hell. Consolidated back into one file. Works perfectly for adding features fast.

2. **`PlayerCombat.cs` (733 lines)** - Combat system
   - 3-hit combo system with timing windows
   - Air attack limiting (max 2 before landing)
   - Dash attack with momentum
   - Head stomp integration
   - Attack hitbox management
   - Input buffering for attacks

3. **`PlayerHealth.cs` (203 lines)** - Health system
   - Damage handling with invincibility frames
   - Death detection and events
   - Health UI integration
   - Respawn coordination

4. **`PlayerAbilities.cs` (354 lines)** - Ability unlock system
   - Progression tracking (double jump, dash, wall jump, etc.)
   - Singleton pattern for global access
   - Ability state management
   - Event system for UI updates

**Supporting Components:**
- `AttackHitbox.cs` (291 lines) - Attack collision and damage dealing
- `PlayerInteractionDetector.cs` (346 lines) - Ledge detection and grabbing (optional system)

**Total Player Code:** ~4,110 lines across 4 main components

### Other Systems

**Enemy System:**
- `SimpleEnemy.cs` (720 lines) - Clean state machine (Patrol/Chase/Attack/Dead)
- Platform-aware AI (edge detection, wall avoidance)
- Event-driven health system for UI integration
- Head stomp compatibility via `SimpleHeadStomp.cs`

**Environment:**
- `MovingPlatform.cs` (285 lines) - Horizontal/vertical/diagonal platforms with velocity inheritance
- `BreakableTerrain.cs` (780 lines) - Destructible terrain system
- Level transitions and spawn point management

**UI Systems:**
- Player health overlay
- Enemy floating health bars
- Health bar component (`HealthBarUI.cs`)

**Input:**
- New Unity Input System (`Controls.inputactions`)
- WASD movement, Space (jump), Shift (dash), Left Click (attack)

**Levels:**
- 3 scenes: Level1_ThePit, Level2_CommercialArea, Level3_HomeArea
- Checkpoint/save point system
- Scene transition management

**Tilemap:**
- Custom composite collider generation
- Tile offset handling for pixel-perfect collisions

### Key Dependencies

**Unity Packages:**
- Test Framework 1.5.1
- Input System 1.14.0
- Cinemachine 3.1.3
- URP 17.2.0
- 2D Animation 12.0.2

**Third-Party:**
- DOTween Pro (Demigiant)
- Unity MCP (UnityMCP server)

### Architecture Philosophy

✅ **MONOLITHIC BY CHOICE** - Not by accident

**Why this works for solo dev:**
- ✅ All movement logic in one place (easy to debug)
- ✅ No state synchronization across 15+ components
- ✅ Adding new movement features is straightforward
- ✅ 68 serialized fields in one Inspector (not scattered across 11 components)
- ✅ Comprehensive test coverage (98 tests) catches regressions

**Known tradeoffs:**
- ⚠️ Large file (2,820 lines) - but Find in File works great
- ⚠️ Not "clean code" by textbook standards - but it ships
- ⚠️ Some duplicate logic - acceptable for v1.0

**Refactoring policy:**
- 📅 **Review after v1.0 ships** - IF game succeeds, consider splitting PlayerController into:
  - `PlayerMovement.cs` (500-700 lines) - Pure movement logic
  - `PlayerController.cs` (200-300 lines) - Orchestration only
- 📅 **If game fails** - Move to next project, don't refactor

**See also:** `docs/outdated_docs/ARCHITECTURE_GUIDELINES.md` for alternative 3-4 component design (future v2.0 option)