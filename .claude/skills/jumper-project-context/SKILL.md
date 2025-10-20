---
name: jumper-project-context
description: Provides comprehensive context about the Jumper 2D platformer Unity project. Use when working on the Jumper game, making changes to gameplay systems, or when the user references their Unity platformer project. Loads automatically to understand project architecture, conventions, and systems before making changes.
---

# Jumper Project Context

This skill provides essential context about the Jumper 2D platformer project to ensure Claude understands the codebase architecture, conventions, and systems before making any changes.

## Project Overview

**Project**: Jumper (JumperPOC)
**Engine**: Unity 6000.2.0b9
**Type**: 2D Platformer
**Location**: X:/GameDev/Jumper/JumperPOC

## Core Architecture

### Key Systems

1. **Player System** (`Assets/Scripts/Player/`)
   - `PlayerController.cs` - Main movement, jumping, dashing (1,848 LOC - large, complex)
   - `PlayerCombat.cs` - Attack system, combos, hitboxes
   - `PlayerAbilities.cs` - Ability unlocking system (dash, double jump, wall stick)
   
2. **Enemy System** (`Assets/Scripts/Enemies/`)
   - `SimpleEnemy.cs` - Base enemy behavior
   - `HeadStompTriggerHandler.cs` - Head stomp mechanics
   - `Enemy1Hitbox.cs` - Enemy collision handling
   - Uses interface `IEnemyBase` for enemy contracts

3. **Level Management**
   - `LevelTransitionManager.cs` - Scene transitions
   - `SimpleRespawnManager.cs` - Death and respawn handling
   - `SavePoint.cs` - Checkpoint system

4. **Environment**
   - `BreakableTerrain.cs` - Destructible terrain blocks
   - `BreakableTerrainManager.cs` - Terrain management
   - `CustomCompositeColliderGenerator.cs` - Tilemap collision optimization

### Architecture Patterns Used

**Singleton Pattern**: 
- `PlayerController`, `SimpleRespawnManager`, `PlayerAbilities`
- Uses `DontDestroyOnLoad()` for persistence

**Event System**:
- C# events and delegates for component communication
- Example: Health changes, combat events, landing events

**Component-Based**:
- Separates concerns (PlayerController, PlayerCombat, PlayerAbilities)
- Components require each other via `[RequireComponent]`

**Ability System**:
- `PlayerAbilities` manages unlockable features
- Systems check abilities before execution (e.g., wall stick, double jump)

## Important Conventions

### Code Style

**Naming**:
- Private fields: `_camelCase` with underscore prefix
- Public properties: `PascalCase`
- Serialized fields: `[SerializeField] private`
- Constants: Often as serialized fields for designer control

**Component References**:
- Cached in `Awake()` method
- Never use `GetComponent<>()` in Update/FixedUpdate
- Pattern: `private Rigidbody2D _rb; void Awake() { _rb = GetComponent<Rigidbody2D>(); }`

**Unity Lifecycle**:
- `Awake()` - Initialize component references
- `Start()` - Initialize with other objects
- `FixedUpdate()` - Physics and Rigidbody movement
- `Update()` - Input and non-physics logic

### Project-Specific Patterns

**Input System**:
- Uses custom `InputManager` singleton
- Event-based input handling
- Subscribe in `OnEnable()`, unsubscribe in `OnDisable()`

**Animation**:
- Uses `SafeSetBool()`, `SafeSetFloat()`, etc. to prevent missing parameter errors
- Tracks missing parameters to avoid log spam

**Debug Visualization**:
- Heavy use of `OnDrawGizmos()` and `OnDrawGizmosSelected()`
- Debug panels with `OnGUI()` (often commented out for production)
- Toggle flags like `showJumpDebug`, `enableSlopeVisualization`

## Key Gameplay Systems

### Movement System (PlayerController)

**Features**:
- Variable jump height (Hollow Knight style)
- Coyote time (grace period after leaving ledge)
- Jump buffering (early jump input)
- Slope movement and detection
- Wall slide and wall stick (ability-gated)
- Dash and dash jump mechanics

**Important Details**:
- Movement in `FixedUpdate()` for physics
- Uses Rigidbody2D, not transform manipulation
- Multi-directional raycasts for slope detection
- Platform edge climbing assistance ("buffer climbing")

### Combat System (PlayerCombat)

**Features**:
- Ground attack combos
- Air attacks
- Dash attacks
- Attack hitbox detection with layers

**Patterns**:
- State tracking (IsAttacking, IsDashAttacking, IsAirAttacking)
- Attack buffering for combo fluidity
- Movement during attacks (reduced speed multiplier)

### Ability System

**Unlockable Abilities**:
- `HasDash` - Dash movement
- `HasDoubleJump` - Mid-air jump
- `HasWallStick` - Wall slide and stick
- `GetAbility("dashjump")` - Dash jump combo

**Integration**:
- All systems check `PlayerAbilities.Instance` before executing
- Abilities gate features, not remove code
- Example: Wall detection always runs, but stick/slide only with ability

## Common Gotchas

### 1. Wall Stick Behavior
- Complex logic with multiple raycasts (top, middle, bottom)
- Must have 2+ raycast hits for wall stick
- Wall stick disabled → prevents all wall friction
- Recent fixes for corner sticking issues

### 2. Variable Jump Implementation
- Two methods: velocity clamping vs gravity reduction
- Compensation system for wall friction
- Separate min/max velocities for first jump vs double jump

### 3. Large PlayerController File
- 1,848 lines - consider refactoring when making major changes
- Many commented debug sections
- Historical fixes documented in comments

### 4. Layer System
- `Ground` layer - platforms and terrain
- `LandingBuffer` layer - platform edges for climbing
- Proper layer setup is critical for movement

### 5. Persistent Player
- Player uses `DontDestroyOnLoad()`
- Singleton pattern prevents duplicates across scenes
- Respawn system resets position, not GameObject

## Working with This Project

### Before Making Changes

1. **Check existing documentation** in `docs/` folder
2. **Review related systems** - components are tightly coupled
3. **Test with abilities enabled/disabled** - behavior changes significantly
4. **Consider debug visualization** - many systems have Gizmo helpers

### When Adding Features

1. **Follow existing patterns** - use serialized fields for config
2. **Add ability gates** if it's an unlockable feature
3. **Cache components** in Awake() if accessing other systems
4. **Add debug visualization** for spatial calculations
5. **Update relevant manager** (PlayerAbilities, InputManager, etc.)

### When Refactoring

1. **PlayerController is a candidate** - too large (1,848 LOC)
2. **Consider extracting**: Jump system, Wall interaction, Dash system
3. **Maintain serialized field structure** - designers rely on Inspector values
4. **Keep event subscription pattern** - OnEnable/OnDisable pairs

## Reference Files

For detailed implementation patterns and best practices:
- `references/project-architecture.md` - Detailed system breakdown
- `references/component-interactions.md` - How systems communicate
- `references/gameplay-features.md` - Feature implementation details
- `references/conventions.md` - Code style and patterns

## Quick Reference Commands

When working on this project, common tasks:

**Understanding a system:**
"Explain how the [jump/dash/combat] system works in this project"

**Making changes:**
"Add [feature] following the existing patterns in this codebase"

**Debugging:**
"Why might wall stick behavior not work correctly?"

**Refactoring:**
"Help me extract the jump logic from PlayerController into a separate component"

---

**Remember**: This is an active development project. Systems are complex and interconnected. Always test changes with different ability combinations enabled/disabled.
