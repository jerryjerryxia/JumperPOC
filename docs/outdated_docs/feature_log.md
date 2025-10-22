
## Development Workflow

1. **Making Gameplay Changes**
   - Primary scripts are in `Assets/Scripts/`
   - PlayerController.cs contains all movement logic
   - Test changes using Play mode in Unity Editor

2. **Adding New Features**
   - Follow existing patterns in PlayerController for new movement abilities
   - Add corresponding animation states to the Animator Controller
   - Update Controls.inputactions for new input bindings
   - Use the existing state machine pattern for complex behaviors

3. **Working with Animations**
   - Sprite animations go in `Assets/Animations/`
   - Use BatchSpriteAnimatorSetup tool for bulk animation creation
   - Maintain consistent animation transition speeds (default: 0.1f)

4. **Level Design**
   - Use Unity's Tilemap system with assets in `Assets/Tiles/`
   - Tile palettes are in `Assets/Palettes/`
   - Remember to add landing buffers to tilemaps using the editor tool

## Enemy AI Behavior Requirements

### Patrol Mode (Default State)
- **Ground Detection**: Enemy continuously checks for ground below using raycast/overlap detection
- **Movement Logic**: 
  - If ground is detected → continue moving in current direction
  - If no ground detected → stop after brief delay, then turn around
  - Randomized stopping points before platform edges for realistic movement
- **Platform Patrolling**: Enemy moves between edges of the platform it spawned on
- **State**: Enemy remains in patrol mode when no player is detected in detection range

### Chase & Attack Mode (Player Detected)
- **Trigger**: Player enters enemy's detection range
- **Behavior Switch**: Enemy immediately leaves patrol mode and enters chase mode
- **Chase Logic**:
  - Enemy moves toward player position
  - Maintains minimum distance equal to attack range (no closer)
  - Continuously tracks player position while in detection range
- **Attack Logic**:
  - Attack when player is within attack range
  - Respect attack cooldown timer
  - Return to chase if player moves out of attack range
- **State Return**: If player leaves detection range, return to patrol mode

### Detection & Range Systems
- **Patrol Mode Detection**: Directional detection only in front of enemy (180° arc, northern hemisphere only)
- **Chase Mode Detection**: Full 360° circular detection around enemy (northern hemisphere only)
- **Patrol Mode Attack**: Front-facing attack range only (northern hemisphere only)
- **Chase Mode Attack**: Full circular attack range (northern hemisphere only)
- **Northern Hemisphere Rule**: Enemies only detect/attack players at same level or above (configurable tolerance)
- **Attack Range**: Minimum distance enemy maintains from player during chase
- **Line of Sight**: Optional - enemy may require clear line of sight to detect player

### Technical Implementation Notes
- **Ground Check**: Use raycast downward from enemy position, configurable distance
- **Platform Awareness**: Enemy should never fall off platforms during patrol
- **State Machine**: Clear separation between Patrol, Chase, and Attack states
- **Performance**: Ground checks should be optimized for multiple enemies

## Health Bar UI System

### Overview
The project implements a comprehensive health bar system for both player and enemies with visual feedback, animations, and proper UI integration.

### Architecture

#### Core Components
1. **HealthBarUI** (`Assets/Scripts/UI/HealthBarUI.cs`)
   - Base health bar component with animation system
   - Supports color transitions (green → yellow → red) based on health percentage
   - Smooth fill animation using `Update()` loop with `Mathf.Lerp`
   - Dynamic text display showing "currentHealth/maxHealth"

2. **PlayerHealthUI** (`Assets/Scripts/UI/PlayerHealthUI.cs`)
   - Screen-space overlay health bar for player (top-left corner)
   - Automatically finds player via tag/name/component search
   - Subscribes to PlayerHealth.OnHealthChanged events

3. **EnemyHealthUI** (`Assets/Scripts/UI/EnemyHealthUI.cs`)
   - World-space health bars that float above enemies
   - Auto-initializes by finding parent EnemyBase component
   - Shows on damage, hides after delay (configurable)
   - Billboard effect - always faces camera

#### Health System Integration
- **PlayerHealth** (`Assets/Scripts/Player/PlayerHealth.cs`): Provides `GetCurrentHealth()` and `GetMaxHealth()` methods
- **EnemyBase** (`Assets/Scripts/Enemies/EnemyBase.cs`): Provides same health interface methods

### Setup Instructions

#### Using the Editor Tool
1. Go to **Tools → Setup Health UI** in Unity menu
2. Click "Setup Player Health UI" - creates health bar in top-left corner
3. Select an enemy GameObject and click "Setup Enemy Health UI" - creates floating health bar

#### Manual Setup Requirements
- **Player GameObject must have "Player" tag** for automatic detection
- **PlayerHealth component** must be properly attached (namespace: `Player.PlayerHealth`)
- **All UI components** are automatically assigned to "UI" layer

### Key Implementation Patterns

#### Health Bar Initialization
```csharp
// Correct pattern used by both player and enemy
healthBar.SetMaxHealth(maxHealth);
healthBar.SetHealth(currentHealth);
```

#### Animation System
- Uses `targetFillAmount` and `Update()` loop for smooth animations
- Slider values represent actual health values (not normalized 0-1)
- Color transitions at 70% (yellow) and 30% (red) thresholds

#### Layer Configuration
- **UI Layer**: All health bar components
- **PlayerHitbox Layer**: Player attack collision
- **EnemyHitbox Layer**: Enemy attack collision
