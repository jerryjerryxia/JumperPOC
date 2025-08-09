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

This project uses SPARC (Specification, Pseudocode, Architecture, Refinement, Completion) methodology with Claude-Flow orchestration for systematic Test-Driven Development.

## SPARC Commands

### Core Commands
- `npx claude-flow sparc modes` - List available modes
- `npx claude-flow sparc run <mode> "<task>"` - Execute specific mode
- `npx claude-flow sparc tdd "<feature>"` - Run complete TDD workflow
- `npx claude-flow sparc info <mode>` - Get mode details

### Batchtools Commands
- `npx claude-flow sparc batch <modes> "<task>"` - Parallel execution
- `npx claude-flow sparc pipeline "<task>"` - Full pipeline processing
- `npx claude-flow sparc concurrent <mode> "<tasks-file>"` - Multi-task processing

### Build Commands
- `npm run build` - Build project
- `npm run test` - Run tests
- `npm run lint` - Linting
- `npm run typecheck` - Type checking

## SPARC Workflow Phases

1. **Specification** - Requirements analysis (`sparc run spec-pseudocode`)
2. **Pseudocode** - Algorithm design (`sparc run spec-pseudocode`)
3. **Architecture** - System design (`sparc run architect`)
4. **Refinement** - TDD implementation (`sparc tdd`)
5. **Completion** - Integration (`sparc run integration`)

## Code Style & Best Practices

- **Modular Design**: Files under 500 lines
- **Environment Safety**: Never hardcode secrets
- **Test-First**: Write tests before implementation
- **Clean Architecture**: Separate concerns
- **Documentation**: Keep updated

## 🚀 Available Agents (54 Total)

### Core Development
`coder`, `reviewer`, `tester`, `planner`, `researcher`

### Swarm Coordination
`hierarchical-coordinator`, `mesh-coordinator`, `adaptive-coordinator`, `collective-intelligence-coordinator`, `swarm-memory-manager`

### Consensus & Distributed
`byzantine-coordinator`, `raft-manager`, `gossip-coordinator`, `consensus-builder`, `crdt-synchronizer`, `quorum-manager`, `security-manager`

### Performance & Optimization
`perf-analyzer`, `performance-benchmarker`, `task-orchestrator`, `memory-coordinator`, `smart-agent`

### GitHub & Repository
`github-modes`, `pr-manager`, `code-review-swarm`, `issue-tracker`, `release-manager`, `workflow-automation`, `project-board-sync`, `repo-architect`, `multi-repo-swarm`

### SPARC Methodology
`sparc-coord`, `sparc-coder`, `specification`, `pseudocode`, `architecture`, `refinement`

### Specialized Development
`backend-dev`, `mobile-dev`, `ml-developer`, `cicd-engineer`, `api-docs`, `system-architect`, `code-analyzer`, `base-template-generator`

### Testing & Validation
`tdd-london-swarm`, `production-validator`

### Migration & Planning
`migration-planner`, `swarm-init`

## 🎯 Claude Code vs MCP Tools

### Claude Code Handles ALL:
- File operations (Read, Write, Edit, MultiEdit, Glob, Grep)
- Code generation and programming
- Bash commands and system operations
- Implementation work
- Project navigation and analysis
- TodoWrite and task management
- Git operations
- Package management
- Testing and debugging

### MCP Tools ONLY:
- Coordination and planning
- Memory management
- Neural features
- Performance tracking
- Swarm orchestration
- GitHub integration

**KEY**: MCP coordinates, Claude Code executes.

## 🚀 Quick Setup

```bash
# Add Claude Flow MCP server
claude mcp add claude-flow npx claude-flow@alpha mcp start
```

## MCP Tool Categories

### Coordination
`swarm_init`, `agent_spawn`, `task_orchestrate`

### Monitoring
`swarm_status`, `agent_list`, `agent_metrics`, `task_status`, `task_results`

### Memory & Neural
`memory_usage`, `neural_status`, `neural_train`, `neural_patterns`

### GitHub Integration
`github_swarm`, `repo_analyze`, `pr_enhance`, `issue_triage`, `code_review`

### System
`benchmark_run`, `features_detect`, `swarm_monitor`

## 📋 Agent Coordination Protocol

### Every Agent MUST:

**1️⃣ BEFORE Work:**
```bash
npx claude-flow@alpha hooks pre-task --description "[task]"
npx claude-flow@alpha hooks session-restore --session-id "swarm-[id]"
```

**2️⃣ DURING Work:**
```bash
npx claude-flow@alpha hooks post-edit --file "[file]" --memory-key "swarm/[agent]/[step]"
npx claude-flow@alpha hooks notify --message "[what was done]"
```

**3️⃣ AFTER Work:**
```bash
npx claude-flow@alpha hooks post-task --task-id "[task]"
npx claude-flow@alpha hooks session-end --export-metrics true
```

## 🎯 Concurrent Execution Examples

### ✅ CORRECT (Single Message):
```javascript
[BatchTool]:
  // Initialize swarm
  mcp__claude-flow__swarm_init { topology: "mesh", maxAgents: 6 }
  mcp__claude-flow__agent_spawn { type: "researcher" }
  mcp__claude-flow__agent_spawn { type: "coder" }
  mcp__claude-flow__agent_spawn { type: "tester" }
  
  // Spawn agents with Task tool
  Task("Research agent: Analyze requirements...")
  Task("Coder agent: Implement features...")
  Task("Tester agent: Create test suite...")
  
  // Batch todos
  TodoWrite { todos: [
    {id: "1", content: "Research", status: "in_progress", priority: "high"},
    {id: "2", content: "Design", status: "pending", priority: "high"},
    {id: "3", content: "Implement", status: "pending", priority: "high"},
    {id: "4", content: "Test", status: "pending", priority: "medium"},
    {id: "5", content: "Document", status: "pending", priority: "low"}
  ]}
  
  // File operations
  Bash "mkdir -p app/{src,tests,docs}"
  Write "app/src/index.js"
  Write "app/tests/index.test.js"
  Write "app/docs/README.md"
```

### ❌ WRONG (Multiple Messages):
```javascript
Message 1: mcp__claude-flow__swarm_init
Message 2: Task("agent 1")
Message 3: TodoWrite { todos: [single todo] }
Message 4: Write "file.js"
// This breaks parallel coordination!
```

## Performance Benefits

- **84.8% SWE-Bench solve rate**
- **32.3% token reduction**
- **2.8-4.4x speed improvement**
- **27+ neural models**

## Hooks Integration

### Pre-Operation
- Auto-assign agents by file type
- Validate commands for safety
- Prepare resources automatically
- Optimize topology by complexity
- Cache searches

### Post-Operation
- Auto-format code
- Train neural patterns
- Update memory
- Analyze performance
- Track token usage

### Session Management
- Generate summaries
- Persist state
- Track metrics
- Restore context
- Export workflows

## Advanced Features (v2.0.0)

- 🚀 Automatic Topology Selection
- ⚡ Parallel Execution (2.8-4.4x speed)
- 🧠 Neural Training
- 📊 Bottleneck Analysis
- 🤖 Smart Auto-Spawning
- 🛡️ Self-Healing Workflows
- 💾 Cross-Session Memory
- 🔗 GitHub Integration

## Integration Tips

1. Start with basic swarm init
2. Scale agents gradually
3. Use memory for context
4. Monitor progress regularly
5. Train patterns from success
6. Enable hooks automation
7. Use GitHub tools first

## Support

- Documentation: https://github.com/ruvnet/claude-flow
- Issues: https://github.com/ruvnet/claude-flow/issues

---

Remember: **Claude Flow coordinates, Claude Code creates!**

# important-instruction-reminders
Do what has been asked; nothing more, nothing less.
NEVER create files unless they're absolutely necessary for achieving your goal.
ALWAYS prefer editing an existing file to creating a new one.
NEVER proactively create documentation files (*.md) or README files. Only create documentation files if explicitly requested by the User.
Never save working files, text/mds and tests to the root folder.

## Project Overview

This is a Unity 2D platformer Metroidvania proof-of-concept (POC) game built with Unity 6000.1.4f1. The project features advanced movement mechanics including wall sliding, dashing, ledge grabbing, and a 3-hit combo system. The ultimate goal of this project is to build a vertical Metroidvania that features rich movement ability progression and extensive map exploration and backtracking. The vertical map design is inspired by Dark Souls 1 and the smooth and extremely polished movement feel is inspired by ENDER LILIES: Quietus of the Knights. The combat is inspired by Ori and the Blind Forest, where defeating an enemy is not the player's only goal: they can also use enemies as leverage to reach otherwise unreachable platforms. More of the game design elements will be updated here once they become clear, but use the aforementioned games as guidelines for development for now. 

## Interaction with the user

Always look critically at the project when asked to perform analysis on the project. Do the same for the prompt that the user inputs. You must point out potential issues in my prompts or things that I might have not considered. I am new to Unity 2D games development, so you must guide me through this development. When I ask for things that are clearly not making sense, don't ever try to make me feel good. You must directly point out what I have missed and let me know why I am wrong. 

When the user reports a bug and the code fixes repeatedly fail, it is highly likely that the issue lies in the editor. So when trying to fix a bug, always take a close look at the editor and inspector setup along with the code base - doing so have helped us 100% of the times, so you must take editor setup into consideration when attempting any bug fix. 

Version control is essential. After a version of the code base is fully tested and confirmed functional, commit it onto github. Never stage or commit anything when the change is not tested by the user - only commit when the user confirms that the change is ready to commit. 

## Developer Information

**Git Configuration:**
- Email: jerryjerryxia@gmail.com
- Name: jerryjerryxia


## Unity C# Development Guidelines

  You are an expert in C#, Unity, and scalable game development.

  Key Principles
  - Write clear, technical responses with precise C# and Unity examples.
  - Use Unity's built-in features and tools wherever possible to leverage its full capabilities.
  - Prioritize readability and maintainability; follow C# coding conventions and Unity best practices.
  - Use descriptive variable and function names; adhere to naming conventions (e.g., PascalCase for public members, camelCase for private members).
  - Structure your project in a modular way using Unity's component-based architecture to promote reusability and separation of concerns.

  C#/Unity
  - Use MonoBehaviour for script components attached to GameObjects; prefer ScriptableObjects for data containers and shared resources.
  - Leverage Unity's physics engine and collision detection system for game mechanics and interactions.
  - Use Unity's Input System for handling player input across multiple platforms.
  - Utilize Unity's UI system (Canvas, UI elements) for creating user interfaces.
  - Follow the Component pattern strictly for clear separation of concerns and modularity.
  - Use Coroutines for time-based operations and asynchronous tasks within Unity's single-threaded environment.

  Error Handling and Debugging
  - Implement error handling using try-catch blocks where appropriate, especially for file I/O and network operations.
  - Use Unity's Debug class for logging and debugging (e.g., Debug.Log, Debug.LogWarning, Debug.LogError).
  - Utilize Unity's profiler and frame debugger to identify and resolve performance issues.
  - Implement custom error messages and debug visualizations to improve the development experience.
  - Use Unity's assertion system (Debug.Assert) to catch logical errors during development.

  Dependencies
  - Unity Engine
  - .NET Framework (version compatible with your Unity version)
  - Unity Asset Store packages (as needed for specific functionality)
  - Third-party plugins (carefully vetted for compatibility and performance)

  Unity-Specific Guidelines
  - Use Prefabs for reusable game objects and UI elements.
  - Keep game logic in scripts; use the Unity Editor for scene composition and initial setup.
  - Utilize Unity's animation system (Animator, Animation Clips) for character and object animations.
  - Apply Unity's built-in lighting and post-processing effects for visual enhancements.
  - Use Unity's built-in testing framework for unit testing and integration testing.
  - Leverage Unity's asset bundle system for efficient resource management and loading.
  - Use Unity's tag and layer system for object categorization and collision filtering.

  Performance Optimization
  - Use object pooling for frequently instantiated and destroyed objects.
  - Optimize draw calls by batching materials and using atlases for sprites and UI elements.
  - Implement level of detail (LOD) systems for complex 3D models to improve rendering performance.
  - Use Unity's Job System and Burst Compiler for CPU-intensive operations.
  - Optimize physics performance by using simplified collision meshes and adjusting fixed timestep.

  Key Conventions
  1. Follow Unity's component-based architecture for modular and reusable game elements.
  2. Prioritize performance optimization and memory management in every stage of development.
  3. Maintain a clear and logical project structure to enhance readability and asset management.
  
  Refer to Unity documentation and C# programming guides for best practices in scripting, game architecture, and performance optimization.

  


## Unity Development Commands

### Building the Project
Unity projects are typically built through the Unity Editor GUI. For command-line builds:
```bash
# Windows example (adjust path to your Unity installation)
"C:\Program Files\Unity\Hub\Editor\6000.1.4f1\Editor\Unity.exe" -batchmode -quit -projectPath . -buildTarget StandaloneWindows64 -buildWindows64Player builds/JumperPOC.exe
```

### Running Tests
The project includes Unity Test Framework but no tests are currently implemented. To run tests when added:
```bash
# Run EditMode tests
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml

# Run PlayMode tests
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults results.xml
```

### Opening in Unity Editor
```bash
# Open project in Unity Editor
Unity -projectPath .
```

## Architecture Overview

### Core Systems

1. **Player Movement System** (`Assets/Scripts/PlayerController.cs`)
   - Implements physics-based movement with Rigidbody2D
   - Features: running, jumping (with double jump), wall sliding/jumping, dashing
   - Uses Unity's new Input System with action mappings
   - Complex state management for different movement states

2. **Interaction System** (`Assets/Scripts/PlayerInteractionDetector.cs`)
   - Handles ledge detection and grabbing
   - Manages interaction prompts and UI feedback
   - Works in conjunction with PlayerController for movement states

3. **Input System** (`Assets/Settings/Controls.inputactions`)
   - Modern Unity Input System configuration
   - Action maps for gameplay controls (WASD movement, Space jump, Shift dash, etc.)
   - Supports gamepad and keyboard input

4. **Animation System**
   - Complex Animator Controller with multiple blend trees
   - States: Idle, Run, Jump, Fall, WallSlide, Dash, Attack combos, Ledge interactions
   - Smooth transitions using animation parameters

5. **Enemy System** (`Assets/Scripts/Enemies/`)
   - Modular enemy AI with patrol and chase behaviors
   - Platform-aware movement with edge detection
   - Player detection and combat capabilities

### Editor Tools (`Assets/Editor/`)
- `SceneSetupHelper.cs` - Quickly set up test scenes with platforms and player
- `AnimatorSetupHelper.cs` - Helper for setting up animation states
- `BatchSpriteAnimatorSetup.cs` - Batch processing for sprite animations
- `AddLandingBuffersToTilemap.cs` - Adds landing buffer colliders to prevent edge-falling

### Key Dependencies
- **Universal Render Pipeline (URP)** - Modern rendering pipeline
- **Cinemachine** - Camera system for smooth following
- **DOTween Pro** - Animation tweening library
- **TextMeshPro** - Advanced text rendering