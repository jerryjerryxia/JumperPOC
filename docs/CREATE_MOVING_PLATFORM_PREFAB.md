# How to Create Moving Platform Prefabs

**Quick Guide:** Create moving platform prefabs that match your grey tile aesthetic

---

## 🎯 Quick Create (5 Minutes)

### Step 1: Create GameObject

1. In Unity Hierarchy, right-click → **2D Object → Sprites → Square**
2. Name it: `MovingPlatform_Horizontal`

### Step 2: Configure Sprite & Transform

**In Inspector:**

```
Transform:
  Position: 0, 0, 0
  Rotation: 0, 0, 0
  Scale: 3, 0.5, 1 (3 tiles wide, half-tile tall)

Sprite Renderer:
  Sprite: (Click circle) → Search "grey_tile" → Select grey_tile_0
  Color: White (255, 255, 255, 255)
  Sorting Layer: Default
  Order in Layer: 0
```

**Result:** Platform visually matches your grey tilemap!

### Step 3: Set Layer

**In Inspector (top):**
```
Layer: Ground (CRITICAL - player detection requires this!)
```

### Step 4: Add BoxCollider2D

1. Click **Add Component**
2. Search: `BoxCollider2D`
3. It auto-sizes to sprite bounds ✓

### Step 5: Add MovingPlatform Script

1. Click **Add Component**
2. Search: `MovingPlatform`
3. Configure:

```
Movement Configuration:
  Movement Type: Horizontal
  Speed: 2
  Distance: 5
  Auto Reverse: ✓

Diagonal Movement:
  Diagonal Angle: 45 (ignored for Horizontal)

Movement Behavior:
  Pause At Endpoints: 0
  Use Smooth Movement: ☐

Debug:
  Show Debug Gizmos: ✓
  Gizmo Color: Yellow (RGB: 255, 255, 0)
```

### Step 6: Save as Prefab

1. In Project window, navigate to: `Assets/Prefabs/Environment/`
2. Drag `MovingPlatform_Horizontal` from Hierarchy → Environment folder
3. Prefab created! ✓

### Step 7: Test

1. **Hit Play**
2. Platform should move automatically left/right
3. Yellow gizmo line shows movement path
4. Stand on platform → you move with it!

---

## 📦 Create Variant Prefabs

### Vertical Platform

**Duplicate the horizontal prefab:**
1. In Project window, `MovingPlatform_Horizontal` → Ctrl+D
2. Rename: `MovingPlatform_Vertical`
3. Double-click to open prefab
4. In Inspector, change:
   ```
   Movement Type: Vertical
   Speed: 3
   Distance: 10
   ```
5. Save (Ctrl+S)

**Result:** Vertical elevator platform

---

### Fast Horizontal

**Duplicate horizontal:**
1. Rename: `MovingPlatform_Fast`
2. Change:
   ```
   Speed: 5 (faster)
   Distance: 8 (longer travel)
   Pause At Endpoints: 1.5 (pause for boarding)
   ```

**Result:** Fast conveyor-style platform

---

### Diagonal Platform

**Duplicate horizontal:**
1. Rename: `MovingPlatform_Diagonal`
2. Change:
   ```
   Movement Type: Diagonal
   Diagonal Angle: 45 (up-right)
   Speed: 2.5
   Distance: 6
   ```

**Result:** Moves at 45° angle

---

## 🎨 Size Variants

### Single Tile Platform (1x1)

```
Transform Scale: 1, 1, 1
Movement:
  Speed: 1.5
  Distance: 3
```

**Use for:** Precise jumping challenges

### Large Platform (5x1)

```
Transform Scale: 5, 0.5, 1
Movement:
  Speed: 1
  Distance: 10
```

**Use for:** Comfortable standing room, slower feel

### Thick Platform (3x1)

```
Transform Scale: 3, 1, 1
Movement:
  Speed: 2
  Distance: 5
```

**Use for:** More visual presence, looks sturdier

---

## ⚙️ Advanced Configurations

### Smooth Elevator

```
Movement Type: Vertical
Speed: 2
Distance: 15
Use Smooth Movement: ✓
Pause At Endpoints: 2
```

**Effect:** Smooth acceleration/deceleration, feels like real elevator

### Patrol Platform

```
Movement Type: Horizontal
Speed: 1.5
Distance: 8
Auto Reverse: ✓
Pause At Endpoints: 3
```

**Effect:** Patrols back and forth, waits 3 seconds at each end

### One-Way Transport

```
Movement Type: Vertical
Speed: 4
Distance: 20
Auto Reverse: ☐
```

**Effect:** Moves once from bottom to top, stops (good for triggered events)

---

## 🎨 Visual Customization

### Match Different Tiles

**To use different grey tile variant:**
```
Sprite Renderer:
  Sprite: grey_tile_tl_corner_0 (corner tile)
  OR
  Sprite: grey_tile_triangle_bl_0 (slope tile)
```

**Creates visual variety while maintaining style consistency**

### Color Tint

```
Sprite Renderer:
  Color: RGB(200, 200, 255) - Slight blue tint
```

**Effect:** Visually distinguishes moving platforms from static terrain

### Glow Effect (Optional)

If you want players to easily identify moving platforms:

1. Add **Material** to Sprite Renderer
2. Use a slight emission/glow shader
3. Platforms stand out as interactive elements

---

## 📋 Prefab Library (Recommended Setup)

**Create these 5 prefabs to cover most use cases:**

```
Assets/Prefabs/Environment/
├─ MovingPlatform_Horizontal.prefab (standard horizontal, speed 2, distance 5)
├─ MovingPlatform_Vertical.prefab (standard vertical, speed 3, distance 10)
├─ MovingPlatform_Fast.prefab (fast horizontal, speed 5, distance 8)
├─ MovingPlatform_Diagonal.prefab (45° angle, speed 2.5, distance 6)
└─ MovingPlatform_Elevator.prefab (smooth vertical, pauses 2s at endpoints)
```

**Usage:** Drag-and-drop from Project window into your scenes

---

## 🧪 Testing Checklist

After creating prefab, verify:

- [ ] **Visibility:** Platform appears in Scene view
- [ ] **Layer:** Set to "Ground" (Inspector top)
- [ ] **Collider:** BoxCollider2D present and sized correctly
- [ ] **Script:** MovingPlatform component attached
- [ ] **Gizmos:** Yellow line visible in Scene view (shows path)
- [ ] **Play Mode:** Platform moves when you hit Play
- [ ] **Player Interaction:** Player stands on platform → moves with it
- [ ] **Jump:** Can jump off platform normally
- [ ] **Movement:** Can walk/run while on platform

---

## 🎮 Level Design Tips

### Placement

**Good:**
- Near walls (enables wall jump combos)
- Over pits (creates risk/reward timing)
- Between static platforms (creates rhythm)

**Avoid:**
- Too fast for player reaction time
- Too far from jump-reachable position
- Overlapping movement paths (confusing)

### Difficulty Curve

**Easy (Early Levels):**
```
Speed: 1-2
Pause: 2-3 seconds
Width: Large (3-5 tiles)
```

**Medium:**
```
Speed: 2-3
Pause: 1 second
Width: Medium (2-3 tiles)
```

**Hard:**
```
Speed: 4-5
Pause: 0 seconds
Width: Small (1-2 tiles)
Multiple platforms in sequence
```

### Puzzle Design

**Timing Puzzle:**
- 2-3 platforms moving at different speeds
- Player must time jumps between them

**Elevation Puzzle:**
- Vertical platforms that alternate
- Player rides one up, jumps to next

**Precision Puzzle:**
- Fast horizontal platforms
- Small landing zones
- Requires dash/double jump

---

## 🐛 Troubleshooting

### Platform Doesn't Move

**Check:**
1. MovingPlatform script attached? ✓
2. Speed > 0? ✓
3. Distance > 0? ✓
4. Auto Reverse enabled (if you want it to return)? ✓

### Player Doesn't Move With Platform

**Fix:**
1. Platform Layer must be "Ground"
2. Check: Inspector top → Layer dropdown → Select "Ground"

### Platform Too Fast/Slow

**Adjust:**
```
Speed: Lower value = slower
Distance: Affects total travel, not speed
```

**Example:**
- Speed 2, Distance 5 = Takes 2.5 seconds one-way
- Speed 4, Distance 5 = Takes 1.25 seconds one-way

### Gizmos Not Showing

**Fix:**
1. Scene view → Top toolbar → **Gizmos** button (should be highlighted)
2. MovingPlatform Inspector → Show Debug Gizmos: ✓

### Player Slides Off

**Causes:**
- Platform moving too fast (reduce Speed)
- Platform too small (increase Scale X)

**Fix:** Reduce speed or increase platform width

---

## 📝 Script Reference

If you need to adjust MovingPlatform behavior:

**File:** `Assets/Scripts/Environment/MovingPlatform.cs`

**Key variables:**
```csharp
public float speed = 2f;           // Movement speed
public float distance = 5f;        // Travel distance
public bool autoReverse = true;    // Return to start
public float pauseAtEndpoints = 0f;// Pause duration (seconds)
```

**Advanced:**
- `useSmoothMovement` - Sine wave easing
- `diagonalAngle` - Direction for diagonal movement (0-360°)

---

## ✅ Success Criteria

**You know it's working when:**

1. ✅ Platform appears in Scene view with correct sprite
2. ✅ Yellow gizmo line shows movement path
3. ✅ Platform moves automatically in Play mode
4. ✅ Player rides along when standing on it
5. ✅ Player can jump/move independently while on platform
6. ✅ Visually matches your grey tile aesthetic

---

## 🚀 Next Steps

**After creating prefabs:**

1. **Place in Level1_ThePit:**
   - Drag prefab into scene
   - Position above pit or gap
   - Adjust start position

2. **Playtest:**
   - Stand on platform
   - Try jumping while moving
   - Test with enemies on platform

3. **Iterate:**
   - Adjust speed/distance based on feel
   - Add more variants as needed
   - Create interesting platform sequences

---

**Ready to build! Drag prefabs into your levels and create awesome platforming challenges! 🎮**
