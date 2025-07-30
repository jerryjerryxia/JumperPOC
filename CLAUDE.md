# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
