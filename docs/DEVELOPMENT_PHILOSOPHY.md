# Development Philosophy - Jumper 2D Platformer

**Goal:** Ship a playable, fun, stable game as fast as possible.

**Core Principle:** Good enough code that ships beats perfect code that doesn't.

**Date:** 2025-10-21
**Status:** Active Development Guidelines

---

## Table of Contents

1. [Core Principles](#core-principles)
2. [Testing Strategy](#testing-strategy)
3. [Architecture Approach](#architecture-approach)
4. [Refactoring Policy](#refactoring-policy)
5. [Decision Trees](#decision-trees)
6. [Common Traps to Avoid](#common-traps-to-avoid)
7. [Shipping Checklist](#shipping-checklist)

---

## Core Principles

### 1. **Ship First, Perfect Later**

> "A shipped game with bugs beats perfect code that never releases."

**Priority order:**
1. Does it work in PlayMode? ✅
2. Can I add features? ✅
3. Can I fix bugs in <30 min? ✅
4. Is the code "perfect"? ← **Don't care until v2.0**

### 2. **Players Don't See Code**

**Players experience:**
- Movement feel (jump height, dash distance)
- Visual polish (animations, VFX)
- Level design (fun platforming challenges)
- Combat satisfaction (impactful hits)

**Players DON'T experience:**
- Code architecture
- Test coverage
- Design patterns
- File organization

**Focus 80% effort on what players experience.**

### 3. **Time is Limited**

As a solo dev, every hour spent on code quality is an hour NOT spent on:
- Level design
- Enemy variety
- Boss fights
- Playtesting
- Polish
- Marketing
- Shipping

**Choose ruthlessly.**

### 4. **Technical Debt is OK (For v1.0)**

**Acceptable technical debt:**
- ✅ Large files (600-800 lines)
- ✅ Some code duplication
- ✅ TODOs and commented code
- ✅ Untested systems that work
- ✅ Non-optimal architecture
- ✅ Debug logs left in code

**Unacceptable technical debt:**
- ❌ Game-breaking bugs
- ❌ Cannot add new features
- ❌ Cannot find/fix bugs
- ❌ Performance issues affecting gameplay

**Ship with acceptable debt. Refactor in v2.0 if game succeeds.**

---

## Testing Strategy

### Philosophy: **Hybrid Testing**

**NOT pure TDD.** **NOT zero tests.** **Strategic testing.**

### The 30-70 Rule

- **30% of code** has unit tests (logic, state, rules)
- **70% of code** tested via PlayMode (feel, visuals, UX)

### When to Write Tests

#### ✅ **DO Write Tests:**

**1. When stuck debugging >15 minutes**
```
Bug appears → Can't find cause in 15 min → STOP
→ Write failing test reproducing bug
→ Fix code until test passes
→ Bug caught forever (regression test)
```

**2. When adding complex logic**
```
Examples:
- New combo system (3-hit sequences)
- Enemy AI state machine (patrol/chase/attack)
- Ability unlock system (progression rules)
- Save/load system (data persistence)

Action: Write 3-10 tests BEFORE or AFTER coding
```

**3. When refactoring working code**
```
Need to refactor PlayerMovement
→ Write tests for current behavior first
→ Refactor with confidence
→ Tests catch regressions
```

**4. Critical game rules**
```
Examples:
- Air attack limit (max 2 before landing)
- Dash cooldown (prevent spam)
- Coyote time (0.15s jump grace period)
- Double jump consumption

Action: 1 test per rule
```

#### ❌ **DON'T Write Tests:**

**1. Game feel / tuning**
```
❌ Jump height feels good
❌ Dash distance is satisfying
❌ Wall slide speed is controllable
❌ Camera follow is smooth

Action: Test these in PlayMode ONLY
```

**2. Visual systems**
```
❌ Animations transition smoothly
❌ Sprites face correct direction
❌ VFX look good
❌ UI layout is clean

Action: PlayMode testing, no unit tests
```

**3. Systems that work**
```
✅ Ground detection works in PlayMode
✅ Enemy AI patrols correctly
✅ Camera follows player

Action: Don't test until they break
```

**4. Simple getters/setters**
```
❌ public bool IsGrounded { get; set; }
❌ public float Health { get; private set; }

Action: Skip these, waste of time
```

### Test Coverage Target

**For this project: 20-30% code coverage**

**Current status:** ~98 tests covering:
- PlayerCombat ✅
- PlayerMovement ✅
- PlayerHealth ✅
- PlayerJumpSystem ✅
- PlayerAbilities ✅
- PlayerStateTracker ✅

**Untested (and that's OK):**
- PlayerGroundDetection
- PlayerWallDetection
- SimpleEnemy AI
- Camera/Input/GameManager
- Animation controllers
- UI systems

**Add tests ONLY when hitting bugs or adding complex features.**

### The Testing Decision Tree

```
Need to verify something works?
│
├─ Is it LOGIC/RULES? (attack limits, state machines, combos)
│  └─ Write unit test (2-5 seconds per run)
│
├─ Is it GAME FEEL? (jump height, dash speed, camera)
│  └─ PlayMode test only (30-60 seconds per run)
│
├─ Is it VISUAL? (animations, VFX, sprites)
│  └─ PlayMode test only
│
└─ Does it WORK RIGHT NOW?
   ├─ YES → Don't test, keep building
   └─ NO → Debug, then decide if test needed
```

---

## Architecture Approach

### Current Architecture: **11-Component Player System**

```
PlayerController (orchestrator)
PlayerMovement (movement logic)
PlayerGroundDetection (ground/slope detection)
PlayerWallDetection (wall detection)
PlayerJumpSystem (jump logic)
PlayerStateTracker (state management)
PlayerAnimationController (animations)
PlayerCombat (combat system)
PlayerInputHandler (input routing)
PlayerRespawnSystem (death/respawn)
PlayerAbilities (ability unlocks)
```

**Status:** ✅ **KEEP IT. IT WORKS.**

### Architecture Philosophy

#### The Iron Rule: **If It Works, Don't Touch It**

**Bad reasons to refactor:**
- "This file is 600 lines, should split it"
- "This could be more modular"
- "Best practices say X"
- "This architecture document recommends Y"
- "I read that professionals do Z"

**Good reasons to refactor:**
- "I literally cannot add wall run without rewriting this"
- "I spend 50% of my time fighting state synchronization"
- "I tried to add feature X and broke 5 other things"
- "Every bug fix takes >2 hours to find the issue"

**Rule:** Don't refactor until actively blocked.

### When Architecture Matters

**Architecture DOES matter for:**
- ✅ New games (v2.0, next project)
- ✅ Team projects (5+ developers)
- ✅ Live service games (years of updates)
- ✅ Shipped games with DLC plans

**Architecture DOESN'T matter for:**
- ❌ First game demo/prototype
- ❌ Solo dev shipping v1.0
- ❌ Unproven game concept
- ❌ Pre-launch development

**You're in the "doesn't matter" category. Ship first.**

### Component Count Guidelines

**Current:** 11 components (works fine)

**Alternative considered:** 3-4 components (from architecture document)
- PlayerController (orchestrator)
- PlayerMovement (all movement)
- PlayerCombat (all combat)
- PlayerInput (optional)

**Decision:** Keep 11 components until actively blocked. Don't refactor preemptively.

**Guideline for NEW features:**
- Adding grapple hook? → New component (independent system)
- Adding wall run? → Add to PlayerMovement (variation of movement)
- Adding inventory? → New component (independent system)
- Adding roll dodge? → Add to PlayerMovement (variation of dash)

**Rule:** New component only if truly independent system.

---

## Refactoring Policy

### The Three-Strike Rule

**Only refactor when ALL THREE are true:**

1. ✅ **Clear pain point** - "I cannot add feature X" or "Every bug takes hours"
2. ✅ **Specific solution** - "Merging A and B will fix it" (not "maybe refactor will help")
3. ✅ **Time-boxed** - "12 hours max" (not "refactor until perfect")

**If even ONE is false → Don't refactor.**

### Refactoring Decision Tree

```
Want to refactor something?
│
├─ Is it actively BLOCKING you?
│  ├─ NO → Don't refactor, keep building
│  └─ YES → Continue...
│
├─ Do you have SPECIFIC solution? (not vague "make it better")
│  ├─ NO → Don't refactor, ship first, learn more
│  └─ YES → Continue...
│
├─ Can you do it in <12 hours?
│  ├─ NO → Solution too big, ship first
│  └─ YES → OK to refactor
│
└─ Write tests for current behavior FIRST
   → Refactor
   → Tests catch regressions
   → Ship
```

### Refactoring Time Budget

**Total allowed refactoring time before v1.0 ships: 0-24 hours**

**Why so low?**
- Solo dev shipping v1.0
- Refactoring doesn't add features
- Players don't see code improvements
- Risk: Refactor forever, never ship

**After v1.0 ships:**
- IF game succeeds → Refactor for v2.0
- IF game fails → Move to next project (refactoring was waste)

### The "Maybe Later" List

**Instead of refactoring now, keep a list:**

**File: `docs/technical_debt.md`**
```markdown
# Technical Debt Backlog

## Not Blocking (Ship First)
- [ ] PlayerMovement.cs is 600 lines (could split into Basic/Advanced)
- [ ] State synchronization passes 15 params (could use events)
- [ ] 108 commented Debug.Log statements (could clean up)
- [ ] PlayerController has 11 component references (could reduce)

## Review After v1.0 Ships
- If game succeeds → Tackle top 3 items for v2.0
- If game fails → Archive this project, start next
```

**Review this list AFTER shipping, not before.**

---

## Decision Trees

### Should I Add This Feature?

```
New feature idea?
│
├─ Does it make the game MORE FUN?
│  ├─ NO → Skip it
│  └─ YES → Continue...
│
├─ Can players EXPERIENCE it? (not just code improvement)
│  ├─ NO → Skip it
│  └─ YES → Continue...
│
├─ Can I implement in <8 hours?
│  ├─ NO → Save for v2.0
│  └─ YES → Continue...
│
└─ Will it help ship a demo FASTER?
   ├─ NO → Save for post-launch
   └─ YES → Build it!
```

### Should I Fix This Bug?

```
Bug discovered?
│
├─ Is it GAME-BREAKING? (crashes, softlocks, progression blockers)
│  ├─ YES → Fix immediately
│  └─ NO → Continue...
│
├─ Does it affect CORE GAMEPLAY? (movement, combat, death)
│  ├─ YES → Fix before shipping
│  └─ NO → Continue...
│
├─ Will players NOTICE it? (visual glitch, rare edge case)
│  ├─ NO → Document in known_issues.md, ship anyway
│  └─ YES → Continue...
│
└─ Can I fix it in <1 hour?
   ├─ YES → Fix it
   └─ NO → Add to bug backlog, evaluate later
```

### Should I Optimize This Code?

```
Want to optimize something?
│
├─ Is it causing PERFORMANCE issues? (<60 FPS, stutters, lag)
│  ├─ NO → Don't optimize
│  └─ YES → Continue...
│
├─ Can you MEASURE the improvement? (profiler data)
│  ├─ NO → Don't optimize (premature optimization)
│  └─ YES → Continue...
│
├─ Is optimization <4 hours work?
│  ├─ NO → Ship first, optimize later
│  └─ YES → Profile, optimize, measure
```

**Rule:** Don't optimize until you measure a problem.

### Should I Add Documentation?

```
Want to document something?
│
├─ Is it PUBLIC API? (used by other developers/systems)
│  ├─ YES → Add XML comments
│  └─ NO → Continue...
│
├─ Is it COMPLEX algorithm? (will you forget how it works in 2 weeks)
│  ├─ YES → Add code comments
│  └─ NO → Continue...
│
├─ Is it CRITICAL system? (save/load, progression, core mechanics)
│  ├─ YES → Add brief comment
│  └─ NO → Skip documentation
```

**Rule:** Code should be self-documenting. Comments for complex parts only.

---

## Common Traps to Avoid

### Trap 1: **"I should refactor before adding features"**

**Trap:**
- "My code is messy, I'll refactor it clean first"
- "Once I have perfect architecture, adding features will be fast"
- **Result:** Refactor for weeks, never ship

**Reality:**
- You learn what good architecture IS by building features
- Refactoring before features = solving wrong problems
- Perfect architecture doesn't make features faster

**Solution:** Add features with current code. Refactor ONLY if blocked.

### Trap 2: **"I need 80% test coverage"**

**Trap:**
- "Professional games have good test coverage"
- "I should test everything before it breaks"
- **Result:** Spend months writing tests, never ship

**Reality:**
- Most indie games have <10% test coverage
- Celeste, Hollow Knight, Stardew Valley had minimal tests at launch
- Tests don't make game fun

**Solution:** Test critical logic (20-30% coverage). PlayMode test the rest.

### Trap 3: **"This code isn't clean enough to ship"**

**Trap:**
- "Players deserve perfect code"
- "I'm embarrassed to ship this code quality"
- **Result:** Perfectionism paralysis, never ship

**Reality:**
- Players never see code
- Shipped "ugly" code beats unshipped "beautiful" code
- You can refactor v2.0 if game succeeds

**Solution:** Ship if gameplay works. Refactor after v1.0 proves concept.

### Trap 4: **"I should learn this new pattern/framework/tool"**

**Trap:**
- "ECS would make this faster"
- "I should learn State Machine pattern properly"
- "Unity DOTS looks cool, I should migrate"
- **Result:** Learning rabbit hole, rewrite project, never ship

**Reality:**
- New tools are exciting (procrastination)
- Your current tools work fine
- Switching tools = starting over

**Solution:** Ship with current tools. Learn new tools for NEXT project.

### Trap 5: **"I need to plan everything before coding"**

**Trap:**
- "I should design the entire system first"
- "I need to know all features before starting"
- **Result:** Analysis paralysis, never start

**Reality:**
- You don't know what you need until you build
- Game design emerges through iteration
- Planning feels productive but isn't

**Solution:** Build minimum feature. Playtest. Learn. Iterate.

---

## Shipping Checklist

### Minimum Viable Demo (3-5 hours of gameplay)

**Core Mechanics:**
- ✅ Player movement (run, jump, dash, wall slide)
- ✅ Player combat (basic attacks, combos)
- ✅ Enemy AI (patrol, chase, attack)
- ✅ Death/respawn system
- ✅ Health system

**Content:**
- ✅ 3-5 playable levels
- ✅ 2-3 enemy types
- ✅ 1 boss fight (optional but recommended)
- ✅ Basic progression (unlock abilities)
- ✅ Checkpoints/save points

**Polish:**
- ✅ Movement feels good (tuned jump/dash)
- ✅ Attacks feel impactful (screen shake, VFX)
- ✅ UI is readable (health bars, menus)
- ✅ No game-breaking bugs
- ✅ Audio (basic SFX, background music)

**NOT Required for v1.0:**
- ❌ Perfect code architecture
- ❌ 80% test coverage
- ❌ Full documentation
- ❌ Optimized performance (unless <60 FPS)
- ❌ All bugs fixed (only game-breakers)
- ❌ Achievements/stats tracking
- ❌ Multiple difficulty modes
- ❌ Localization

### Pre-Ship Quality Gate

**Only ship if ALL are true:**

1. ✅ **Can complete game start-to-finish** without softlocks
2. ✅ **Core mechanics work** (movement, combat, death)
3. ✅ **Runs at 60 FPS** on target hardware
4. ✅ **No game-breaking bugs** (crashes, progression blockers)
5. ✅ **Friends playtested** and had fun
6. ✅ **5+ hours of content** (levels, enemies, challenges)

**If ANY are false:** Fix before shipping. If all true: SHIP NOW.

### Post-Ship Roadmap

**After v1.0 launches:**

1. **Week 1-2:** Monitor player feedback, fix critical bugs
2. **Week 3-4:** Analyze what worked, what didn't
3. **Month 2:** Plan v2.0 features based on player feedback
4. **Month 3+:** Build v2.0 OR start next game (if v1.0 failed)

**Refactoring decision:**
- IF game succeeds (1000+ players, positive reviews) → Refactor for v2.0
- IF game fails (<100 players, negative reviews) → Move to next project

**Don't refactor until you know if game is worth improving.**

---

## Quick Reference

### The Three Questions

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

**Recommended time split:**

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

---

## Summary

**Development Philosophy in One Sentence:**

> "Ship a fun, stable game fast by being ruthless about priorities: build features players experience, test critical logic, ignore perfectionism, and refactor only when blocked."

**Core Values:**
1. **Shipping > Perfection**
2. **Gameplay > Code Quality**
3. **Iteration > Planning**
4. **Strategic Testing > Comprehensive Testing**
5. **Good Enough > Best Practices**

**Success Metric:**
- ✅ Game shipped in 3-6 months
- ❌ Perfect code that never releases

---

**This document is the north star for all development decisions. When in doubt, re-read this and choose the option that gets you closer to shipping.**

**Last Updated:** 2025-10-21
**Next Review:** After v1.0 ships
