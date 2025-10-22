# Breakable Terrain System - Implementation Guide

## Overview

The Breakable Terrain System provides a flexible way to create terrain that disappears when touched by the player, encouraging exploration and discovery in your Metroidvania game.

## Core Components

### 1. BreakableTerrain Component
The main component that makes any GameObject with a Collider2D breakable.

**Key Features:**
- Direction-based breaking (top, bottom, left, right, or combinations)
- Velocity requirements to prevent accidental breaking
- Player state requirements (e.g., must be dashing)
- Optional restoration system
- Tilemap integration
- Visual and audio effects

### 2. BreakableTerrainManager (Optional)
Manages multiple breakable terrain objects for save systems and global operations.

### 3. Editor Tools
Helper tools for easy setup and configuration in the Unity Editor.

## Quick Setup Guide

### Method 1: Using the Setup Helper (Recommended)
1. Open **Tools → Breakable Terrain Setup Helper**
2. Configure your desired settings
3. Select GameObjects with Collider2D components
4. Click **"Add Breakable Terrain to Selected Objects"**

### Method 2: Manual Setup
1. Add a **Collider2D** component to your GameObject (if not present)
2. Set the collider as **isTrigger = true**
3. Add the **BreakableTerrain** component
4. Configure the settings in the inspector

## Common Configurations

### Secret Wall (Touch from Sides)
```csharp
allowedBreakDirections = BreakDirection.Sides;
requireMinimumVelocity = false;
requireDashState = false;
oneTimeOnly = true;
```

### Floor Collapse (Jump/Land on Top)
```csharp
allowedBreakDirections = BreakDirection.Top;
requireMinimumVelocity = true;
minimumVelocity = 5f;
requireDashState = false;
oneTimeOnly = true;
```

### Dash Wall (High-Speed Impact)
```csharp
allowedBreakDirections = BreakDirection.All;
requireMinimumVelocity = true;
minimumVelocity = 15f; // Dash speed
requireDashState = true;
oneTimeOnly = true;
```

### Temporary Break (Auto-Restore)
```csharp
allowedBreakDirections = BreakDirection.All;
canRestore = true;
restoreTime = 30f;
oneTimeOnly = false;
```

## Tilemap Integration

The system automatically integrates with Unity's Tilemap system:

1. **Automatic Tile Removal**: When terrain breaks, corresponding tilemap tiles are automatically removed
2. **Landing Buffer Cleanup**: Associated landing buffers are cleaned up
3. **OffsetTile Support**: Works with your existing OffsetTile system
4. **Restoration**: Can optionally restore tiles when terrain is restored

### Setting Up Tilemap Breakable Terrain
1. Create your tilemap as usual
2. For specific tiles you want to be breakable, create child GameObjects
3. Position them over the tiles
4. Add BoxCollider2D (set as trigger) matching the tile size
5. Add BreakableTerrain component
6. The system will automatically handle tile removal

## Effects and Feedback

### Visual Effects
- **Break Effect Prefab**: Particle system or animation played when terrain breaks
- **Restore Effect Prefab**: Effect played when terrain is restored
- **Gizmos**: Show break directions in the editor

### Audio Effects
- **Break Sound**: Audio clip played when terrain breaks
- **Spatial Audio**: Automatically configured for 3D positioning

### Camera Shake
- **Auto Shake**: Configurable camera shake on break
- **Customizable**: Duration and intensity settings

## Integration with Player Systems

The system integrates seamlessly with your existing player controller:

### Player Detection
```csharp
// Auto-detects player layers
LayerMask playerLayers; // Configurable in inspector

// Supports multiple player layers:
// - "Player"
// - "PlayerHitbox" 
// - Custom layers
```

### Player State Checking
```csharp
// Can require specific player states
bool requireDashState = true; // Must be dashing to break
// Future: Can be extended for other abilities
```

### Velocity Detection
```csharp
// Prevents accidental breaking
bool requireMinimumVelocity = true;
float minimumVelocity = 2f; // Units per second
```

## Save System Integration

### Automatic State Tracking
```csharp
// Each BreakableTerrain tracks:
bool IsBroken;          // Currently broken
bool HasBeenBroken;     // Ever been broken (for one-time secrets)
```

### Manager-Based Save System
```csharp
// Enable in BreakableTerrainManager
enableSaveSystem = true;
saveFileName = "breakable_terrain_state.json";

// Automatic save on state changes
// Load with: manager.LoadTerrainState();
```

## Performance Considerations

### Optimized Detection
- Uses trigger-based collision (more efficient than continuous checks)
- Only processes when player is in contact
- Minimal overhead when not interacting

### Memory Management
- Automatic cleanup of effects
- Optional restoration system
- Efficient state tracking

## Advanced Usage

### Custom Break Conditions
You can extend the system by modifying the `ShouldBreak` method:

```csharp
private bool ShouldBreak(Collider2D playerCollider, PlayerController playerController)
{
    // Add custom conditions here
    // Example: Require specific ability
    PlayerAbilities abilities = playerController.GetComponent<PlayerAbilities>();
    if (requireWallBreakAbility && !abilities.HasWallBreak)
        return false;
    
    // Original logic...
    return CheckOriginalConditions();
}
```

### Event System Integration
```csharp
// Subscribe to break events
breakableTerrain.OnTerrainBroken += HandleSecretDiscovered;
breakableTerrain.OnTerrainRestored += HandleSecretResealed;

// Manager events for global tracking
manager.OnAnyTerrainBroken += UpdateExplorationProgress;
```

### Achievement/Progress Tracking
```csharp
// Get statistics
TerrainStatistics stats = manager.GetStatistics();
int secretsFound = stats.everBrokenCount;
int totalSecrets = stats.totalCount;

// Calculate completion percentage
float completionPercent = (float)secretsFound / totalSecrets * 100f;
```

## Troubleshooting

### Common Issues

**Terrain not breaking:**
- Ensure Collider2D is set as trigger
- Check layer configuration matches player
- Verify direction settings allow the contact direction
- Check velocity requirements

**Tiles not disappearing:**
- Ensure GameObject is child of tilemap or has tilemap reference
- Check that tilemap position is calculated correctly
- Verify tilemap has the tiles at the expected positions

**No visual effects:**
- Check that effect prefabs are assigned
- Ensure particle systems have proper settings
- Verify audio source is configured

### Debug Tools

**Runtime Debug Panel:**
- Shows terrain statistics
- Provides restore/reset buttons
- Displays current state

**Editor Gizmos:**
- Show break directions
- Display terrain bounds
- Highlight configuration

**Console Logging:**
- Enable debug logging in components
- Check for error messages
- Monitor state changes

## Best Practices

### Level Design
1. **Subtle Visual Cues**: Make breakable terrain slightly different but not obvious
2. **Logical Placement**: Put secrets where players naturally explore
3. **Progressive Disclosure**: Gate some secrets behind ability requirements
4. **Reward Discovery**: Always put something valuable behind breakable terrain

### Technical Implementation
1. **Layer Organization**: Use consistent layers for breakable terrain
2. **Effect Consistency**: Use similar effects across all breakable terrain
3. **Performance**: Don't overuse restoration system in large levels
4. **Save Integration**: Plan for save system early if needed

### Player Experience
1. **Natural Discovery**: Players should break terrain through normal movement
2. **No Punishment**: Breaking terrain should never block progress
3. **Clear Feedback**: Always provide immediate visual/audio feedback
4. **Exploration Reward**: Make secrets feel worthwhile to discover

## Future Extensions

The system is designed to be easily extensible:

- **Conditional Breaking**: Require specific items or abilities
- **Partial Breaking**: Multi-hit terrain that breaks in stages
- **Interactive Hints**: Show subtle hints when player is near
- **Network Support**: Multiplayer synchronization
- **Advanced Effects**: Complex particle systems and animations