# Breakable Terrain with Composite Collider 2D - Setup Guide

## Overview

The Breakable Terrain System now **fully supports Unity's Composite Collider 2D**, which is the standard approach for tilemap collision optimization. This guide explains how to create breakable tiles in tilemaps that use composite colliders.

## Understanding the Setup

### **Standard Tilemap Colliders vs Composite Colliders**

**Standard Setup:**
- Each tile has its own individual collider
- Many small colliders = performance issues
- Direct tile collision detection

**Composite Collider Setup (Your Current Setup):**
- All tile colliders merged into one efficient composite shape
- Better performance for large tilemaps
- Requires separate trigger colliders for breakable detection

### **How the System Handles Composite Colliders**

The BreakableTerrain system automatically:
1. **Detects** when a tilemap uses Composite Collider 2D
2. **Creates separate trigger colliders** for breakable functionality
3. **Maintains tilemap collision** through the composite collider
4. **Regenerates composite geometry** when tiles are broken/restored

## Step-by-Step Setup Guide

### **Method 1: Using the Setup Tool (Recommended)**

#### **Step 1: Prepare Your Tilemap**
1. **Ensure your tilemap** has:
   - ✅ `Tilemap` component
   - ✅ `TilemapRenderer` component  
   - ✅ `TilemapCollider2D` component
   - ✅ `CompositeCollider2D` component
   - ✅ `TilemapCollider2D.usedByComposite = true`

#### **Step 2: Create Breakable Tile Objects**
1. **Select your tilemap** in the hierarchy
2. **Create child GameObjects** for each breakable tile:
   - Right-click tilemap → Create Empty
   - Name: "BreakableTile_X_Y" (where X,Y are tile coordinates)
   - Position: Move to exact world position of the tile

#### **Step 3: Use the Setup Tool**
1. **Select all breakable tile GameObjects**
2. Open **`Tools → Breakable Terrain Setup Helper`**
3. The tool will show: **"COMPOSITE COLLIDERS DETECTED"**
4. Configure your settings (Secret Wall, Floor Collapse, etc.)
5. Click **"Add Breakable Terrain to Selected Objects"**

#### **What Happens Automatically:**
- ✅ **System detects** composite collider setup
- ✅ **Creates trigger children** automatically for each breakable tile
- ✅ **Configures layers** properly
- ✅ **Sets up tile removal** integration
- ✅ **Handles composite regeneration**

### **Method 2: Manual Setup for Advanced Users**

#### **Step 1: Add BreakableTerrain Component**
```csharp
// Add to your tile GameObject (child of tilemap)
BreakableTerrain breakableTerrain = tileObject.AddComponent<BreakableTerrain>();
```

#### **Step 2: Setup for Composite Collider**
```csharp
// Get your tilemap's composite collider
CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();

// Configure the breakable terrain
breakableTerrain.SetupForCompositeCollider(composite);
```

#### **Step 3: Configure Trigger Child**
The system will auto-create a trigger child with:
- **BoxCollider2D** (isTrigger = true)
- **Size**: (1, 1) for standard tiles
- **Layer**: "BreakableTrigger" (if exists)

## Layer Setup for Composite Colliders

### **Recommended Layer Configuration:**

1. **Create a "BreakableTrigger" layer**:
   - `Edit → Project Settings → Tags and Layers`
   - Add "BreakableTrigger" to an empty layer slot

2. **Configure collision matrix**:
   - `Edit → Project Settings → Physics 2D`
   - Ensure "BreakableTrigger" collides with "Player" layer
   - Ensure "BreakableTrigger" does NOT collide with "Ground" layer

### **Why Separate Layers?**
- **Ground collision** handled by composite collider
- **Break detection** handled by trigger children
- **No interference** between systems
- **Optimal performance**

## Example: Creating a Secret Wall in Composite Collider Tilemap

### **Scenario:** 
You have a tilemap wall at position (10, 5) that should break when player touches from the sides.

### **Steps:**
1. **Create child GameObject** under your tilemap
2. **Position at (10, 5, 0)** - exact world position of the tile
3. **Name it** "SecretWall_10_5"
4. **Select the GameObject**
5. **Open Tools → Breakable Terrain Setup Helper**
6. **Click "Setup Secret Wall (Side Break)"**
7. **Click "Add Breakable Terrain to Selected Objects"**

### **Result:**
- ✅ **Trigger child** created automatically
- ✅ **Proper layers** configured
- ✅ **Side-break detection** enabled
- ✅ **Composite collider integration** ready

When player touches from left/right:
- ✅ **Tile disappears** from tilemap
- ✅ **Composite collider** regenerates geometry
- ✅ **Trigger child** deactivates
- ✅ **Break effects** play

## Advanced Features

### **Tile Position Auto-Detection**
```csharp
// System automatically calculates tile position from world position
Vector3Int tilePos = tilemap.WorldToCell(transform.position);
TileBase originalTile = tilemap.GetTile(tilePos);
```

### **Composite Collider Regeneration**
```csharp
// Automatically called when tiles are broken/restored
private IEnumerator RegenerateCompositeColliderDelayed()
{
    yield return null; // Wait one frame
    compositeCollider.GenerateGeometry();
}
```

### **Multiple Tile Support**
- **Batch creation**: Select multiple child objects → Setup all at once
- **Different configurations**: Each breakable tile can have different break conditions
- **Mixed setups**: Combine breakable and non-breakable tiles seamlessly

## Troubleshooting Composite Collider Issues

### **Problem: Breakable tiles not responding**
**Check:**
- ✅ Trigger child created and enabled
- ✅ Trigger collider on correct layer
- ✅ Player layer configured in BreakableTerrain
- ✅ Break directions allow the contact direction

### **Problem: Composite collider not updating**
**Solution:**
- ✅ Ensure `RegenerateCompositeColliderDelayed()` is called
- ✅ Check that `CompositeCollider2D.GenerateGeometry()` works
- ✅ Verify tilemap changes are applied before regeneration

### **Problem: Player falling through after tile breaks**
**Check:**
- ✅ Composite collider regeneration is working
- ✅ Adjacent tiles are present to maintain collision
- ✅ No delay issues in composite regeneration

### **Problem: Trigger children not visible in scene**
**Solution:**
- ✅ Check `autoCreateTriggerChild = true`
- ✅ Look for "BreakableTrigger" child objects
- ✅ Enable gizmos to see trigger bounds

## Performance Considerations

### **Composite Collider Benefits:**
- ✅ **Single collider** instead of many individual ones
- ✅ **Optimized collision detection**
- ✅ **Reduced physics overhead**
- ✅ **Better memory usage**

### **Breakable System Overhead:**
- ✅ **Minimal impact**: Only trigger children for breakable tiles
- ✅ **On-demand processing**: Only active when player nearby
- ✅ **Efficient regeneration**: Composite updates only when needed

## Integration with Your Existing Systems

### **Landing Buffers:**
- ✅ **Automatic cleanup**: Removed when tiles break
- ✅ **Composite compatible**: Works with both systems
- ✅ **Regeneration support**: Can be recreated if needed

### **OffsetTile System:**
- ✅ **Full compatibility**: Works with sliced tiles
- ✅ **Smart detection**: Handles L-shapes and triangles
- ✅ **Composite integration**: Respects composite collider setup

### **Save System:**
- ✅ **State persistence**: Broken tiles remembered
- ✅ **Composite restoration**: Geometry rebuilt on load
- ✅ **Trigger recreation**: Children restored properly

## Best Practices for Composite Collider Breakable Terrain

1. **Use BreakableTrigger layer** for optimal collision setup
2. **Position tile objects precisely** at tile centers
3. **Test composite regeneration** in play mode
4. **Batch setup multiple tiles** for efficiency
5. **Consider performance** when adding many breakable tiles
6. **Test with different tile arrangements** (L-shapes, etc.)

## Quick Reference

### **Setup Checklist:**
- ✅ Tilemap has CompositeCollider2D
- ✅ TilemapCollider2D.usedByComposite = true
- ✅ Child GameObjects positioned at tile locations
- ✅ BreakableTerrain components added
- ✅ BreakableTrigger layer created (recommended)
- ✅ Tool shows "COMPOSITE COLLIDERS DETECTED"

### **Runtime Behavior:**
- ✅ Player touches trigger child
- ✅ Break conditions checked
- ✅ Tile removed from tilemap
- ✅ Composite collider regenerated
- ✅ Trigger child deactivated
- ✅ Effects played

**The composite collider integration makes breakable terrain work seamlessly with Unity's optimized tilemap collision system while maintaining full functionality!**