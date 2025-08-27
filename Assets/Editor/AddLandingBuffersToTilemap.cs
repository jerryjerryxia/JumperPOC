using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class AddLandingBuffersToTilemap : EditorWindow
{
    private float bufferWidth = 0.28f;
    private float bufferHeight = 0.05f;
    private GameObject targetObject;

    [MenuItem("Tools/Add Landing Buffers to Tilemap Platforms and Walls")]
    public static void ShowWindow()
    {
        GetWindow<AddLandingBuffersToTilemap>("Add Landing Buffers");
    }

    void OnGUI()
    {
        GUILayout.Label("Landing Buffer Settings", EditorStyles.boldLabel);
        
        // Target selection
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object (optional)", targetObject, typeof(GameObject), true);
        EditorGUILayout.Space();
        
        bufferWidth = EditorGUILayout.FloatField("Buffer Width", bufferWidth);
        bufferHeight = EditorGUILayout.FloatField("Buffer Height", bufferHeight);
        
        // Dynamic button text based on target selection
        string addButtonText = targetObject != null ? "Add Buffers to Target" : "Add Buffers to All Grids";
        if (GUILayout.Button(addButtonText))
        {
            AddBuffers(targetObject, bufferWidth, bufferHeight);
        }
        
        string removeButtonText = targetObject != null ? "Remove Buffers from Target" : "Remove All Buffers";
        if (GUILayout.Button(removeButtonText))
        {
            RemoveAllBuffers(targetObject);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("\n✨ SMART SLICED TILE SUPPORT:\n• Automatically detects OffsetTile sliced tiles\n• Places buffers at actual visual edges (not grid edges)\n• Multi-segment support for L-shaped tiles (75% coverage)\n• Works with quarters (25%), halves (50%), L-shapes (75%)\n• Smart triangle detection: Only flat-bottom triangles get buffers\n• Backward compatible with regular tiles\n\n📍 TILEMAP NAMING:\nThe tool processes tilemaps with these keywords in their names:\n• 'platform', 'wall', 'floor' (original)\n• 'tilemap', 'level', 'lv1', 'lv2' (added for flexibility)\n• If your tilemap isn't processed, check the Console for warnings\n\nL-SHAPED TILES: Each L-shape gets 3 precise buffers:\n• Top horizontal segment: left + right edges\n• Bottom vertical segment: appropriate side edge\n\nTRIANGLE TILES: Slope detection ensures proper behavior:\n• triangle_tl & triangle_tr: Landing buffers (flat bottom edges)\n• triangle_bl & triangle_br: No buffers (sloped tops)\n\nTo make the buffers work, your player ground check should look like:\n\nint groundMask = (1 << LayerMask.NameToLayer(\"Ground\")) | (1 << LayerMask.NameToLayer(\"LandingBuffer\"));\nisGrounded = Physics2D.OverlapCircle(feetPos, 0.02f, groundMask);\n\nReplace 'Ground' with your actual ground layer name if different.", MessageType.Info);
    }

    private static void AddBuffers(GameObject targetObject, float bufferWidth, float bufferHeight)
    {
        Tilemap[] tilemaps = GetTilemapsFromTarget(targetObject);
        
        if (tilemaps.Length == 0)
        {
            if (targetObject != null)
                Debug.LogError($"No tilemaps found in target '{targetObject.name}' or its children.");
            else
                Debug.LogError("No Grid found in the scene and no target specified.");
            return;
        }

        int bufferLayer = LayerMask.NameToLayer("LandingBuffer");
        if (bufferLayer == -1)
        {
            Debug.LogError("LandingBuffer layer does not exist! Please add it to your project.");
            return;
        }

        int count = 0;
        foreach (var tilemap in tilemaps)
        {
            string name = tilemap.name.ToLower();
            // Check if this tilemap should have buffers (platforms, walls, floors, or level tilemaps)
            bool shouldAddBuffers = name.Contains("platform") || name.Contains("wall") || name.Contains("floor") || 
                                  name.Contains("tilemap") || name.Contains("lv1") || name.Contains("lv2") || 
                                  name.Contains("level");
            if (!shouldAddBuffers)
            {
                Debug.LogWarning($"Skipping tilemap '{tilemap.name}' - name doesn't match expected patterns (platform/wall/floor/tilemap/level)");
                continue;
            }
            
            Debug.Log($"Processing tilemap: {tilemap.name}");

            BoundsInt bounds = tilemap.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    if (!tilemap.HasTile(pos)) continue;

                    // Only add buffers to topmost tiles (tiles with no tile above them)
                    if (tilemap.HasTile(pos + Vector3Int.up)) continue; // Skip if not topmost
                    
                    // Smart edge detection for both regular and sliced tiles
                    CreateSmartBuffersForTile(tilemap, pos, bufferLayer, bufferWidth, bufferHeight, ref count);
                }
            }
        }

        string targetName = targetObject != null ? $"target '{targetObject.name}'" : "all grids";
        Debug.Log($"Added {count} landing buffer colliders to {targetName}.");
    }

    private static void RemoveAllBuffers(GameObject targetObject)
    {
        Tilemap[] tilemaps = GetTilemapsFromTarget(targetObject);
        
        if (tilemaps.Length == 0) return;

        int removed = 0;
        foreach (var tilemap in tilemaps)
        {
            string name = tilemap.name.ToLower();
            if (!(name.Contains("platform") || name.Contains("wall") || name.Contains("floor") || 
                  name.Contains("tilemap") || name.Contains("lv1") || name.Contains("lv2") || 
                  name.Contains("level")))
                continue;
                
            Transform t = tilemap.transform;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                GameObject child = t.GetChild(i).gameObject;
                if (child.name.StartsWith("LandingBuffer_Left") || child.name.StartsWith("LandingBuffer_Right"))
                {
                    GameObject.DestroyImmediate(child);
                    removed++;
                }
            }
        }
        
        string targetName = targetObject != null ? $"target '{targetObject.name}'" : "all grids";
        Debug.Log($"Removed {removed} landing buffer colliders from {targetName}.");
    }

    private static Tilemap[] GetTilemapsFromTarget(GameObject targetObject)
    {
        if (targetObject != null)
        {
            // Check if target itself is a Tilemap
            Tilemap targetTilemap = targetObject.GetComponent<Tilemap>();
            if (targetTilemap != null)
            {
                return new Tilemap[] { targetTilemap };
            }
            
            // Otherwise get all Tilemaps from children
            Tilemap[] childTilemaps = targetObject.GetComponentsInChildren<Tilemap>();
            return childTilemaps;
        }
        else
        {
            // Fallback to current behavior - find Grid automatically
            Grid grid = GameObject.FindFirstObjectByType<Grid>();
            if (grid == null)
                return new Tilemap[0];
            
            return grid.GetComponentsInChildren<Tilemap>();
        }
    }

    private static void CreateSmartBuffersForTile(Tilemap tilemap, Vector3Int pos, int bufferLayer, float bufferWidth, float bufferHeight, ref int count)
    {
        TileBase tileBase = tilemap.GetTile(pos);
        
        // Check if this is an OffsetTile (sliced tile)
        if (tileBase is OffsetTile offsetTile)
        {
            CreateBuffersForOffsetTile(tilemap, pos, offsetTile, bufferLayer, bufferWidth, bufferHeight, ref count);
        }
        else
        {
            // Regular tile - use original logic with edge detection
            CreateBuffersForRegularTile(tilemap, pos, bufferLayer, bufferWidth, bufferHeight, ref count);
        }
    }
    
    private static void CreateBuffersForRegularTile(Tilemap tilemap, Vector3Int pos, int bufferLayer, float bufferWidth, float bufferHeight, ref int count)
    {
        // Original logic - check left and right edges
        if (!tilemap.HasTile(pos + Vector3Int.left))
        {
            CreateBuffer(tilemap, pos, bufferLayer, -1, bufferWidth, bufferHeight);
            count++;
        }
        if (!tilemap.HasTile(pos + Vector3Int.right))
        {
            CreateBuffer(tilemap, pos, bufferLayer, 1, bufferWidth, bufferHeight);
            count++;
        }
    }
    
    private static void CreateBuffersForOffsetTile(Tilemap tilemap, Vector3Int pos, OffsetTile offsetTile, int bufferLayer, float bufferWidth, float bufferHeight, ref int count)
    {
        // EARLY TRIANGLE DETECTION - Handle all triangles before complex processing
        if (offsetTile.sprite != null && offsetTile.sprite.name.ToLower().Contains("_triangle_"))
        {
            string spriteName = offsetTile.sprite.name.ToLower();
            
            // Top triangles (tl/tr) have flat bottom edge - need landing buffers
            if (spriteName.Contains("_triangle_tl") || spriteName.Contains("_triangle_tr"))
            {
                CreateBuffersForRegularTile(tilemap, pos, bufferLayer, bufferWidth, bufferHeight, ref count);
                return; // Use regular logic for flat-bottom triangles
            }
            
            // Bottom triangles (bl/br) have sloped top - no landing buffers needed
            if (spriteName.Contains("_triangle_bl") || spriteName.Contains("_triangle_br"))
            {
                return; // No buffers for slope-top triangles
            }
        }
        
        // Check if this is a complex shape that needs multi-segment handling
        if (IsComplexShape(offsetTile))
        {
            CreateBuffersForComplexShape(tilemap, pos, offsetTile, bufferLayer, bufferWidth, bufferHeight, ref count);
            return;
        }
        
        // Simple shape - use single-segment logic
        TileVisualBounds visualBounds = CalculateOffsetTileVisualBounds(tilemap, pos, offsetTile);
        
        if (!visualBounds.isValid) 
        {
            // Fallback to regular tile logic if we can't determine bounds
            CreateBuffersForRegularTile(tilemap, pos, bufferLayer, bufferWidth, bufferHeight, ref count);
            return;
        }
        
        // Check edges based on actual visual content, not grid edges
        bool needsLeftBuffer = ShouldCreateEdgeBuffer(tilemap, pos, visualBounds, EdgeDirection.Left);
        bool needsRightBuffer = ShouldCreateEdgeBuffer(tilemap, pos, visualBounds, EdgeDirection.Right);
        
        if (needsLeftBuffer)
        {
            CreateSmartBuffer(tilemap, pos, visualBounds, bufferLayer, EdgeDirection.Left, bufferWidth, bufferHeight);
            count++;
        }
        
        if (needsRightBuffer)
        {
            CreateSmartBuffer(tilemap, pos, visualBounds, bufferLayer, EdgeDirection.Right, bufferWidth, bufferHeight);
            count++;
        }
    }
    
    private enum EdgeDirection { Left = -1, Right = 1 }
    
    private struct TileVisualBounds
    {
        public bool isValid;
        public float leftEdgeX;   // Left edge of visual content in world space
        public float rightEdgeX;  // Right edge of visual content in world space
        public float topEdgeY;    // Top edge of visual content in world space
        public Vector3 centerWorld; // Center of the tile in world space
    }
    
    private struct TileSegment
    {
        public Rect localBounds; // Bounds in tile-local coordinates (0-64)
        public bool isValid;
        
        public TileSegment(Rect bounds)
        {
            localBounds = bounds;
            isValid = true;
        }
    }
    
    private static TileVisualBounds CalculateOffsetTileVisualBounds(Tilemap tilemap, Vector3Int pos, OffsetTile offsetTile)
    {
        TileVisualBounds bounds = new TileVisualBounds();
        
        if (offsetTile.sprite == null)
        {
            bounds.isValid = false;
            return bounds;
        }
        
        // Get tile center in world space
        Vector3 tileCenter = tilemap.GetCellCenterWorld(pos);
        Vector3 tileSize = tilemap.cellSize;
        
        // Get sprite bounds in Unity units (sprite.bounds gives us the size)
        Bounds spriteBounds = offsetTile.sprite.bounds;
        
        // Apply the OffsetTile's transform (translation) to the sprite bounds
        Matrix4x4 transform = offsetTile.transform;
        Vector3 offsetFromTransform = new Vector3(transform.m03, transform.m13, transform.m23);
        
        // Calculate actual visual bounds in world space
        Vector3 visualCenter = tileCenter + offsetFromTransform;
        
        bounds.centerWorld = tileCenter;
        bounds.leftEdgeX = visualCenter.x - (spriteBounds.size.x * 0.5f);
        bounds.rightEdgeX = visualCenter.x + (spriteBounds.size.x * 0.5f);
        bounds.topEdgeY = visualCenter.y + (spriteBounds.size.y * 0.5f);
        bounds.isValid = true;
        
        return bounds;
    }
    
    private static bool ShouldCreateEdgeBuffer(Tilemap tilemap, Vector3Int pos, TileVisualBounds visualBounds, EdgeDirection direction)
    {
        Vector3Int adjacentPos = pos + new Vector3Int((int)direction, 0, 0);
        
        // If no adjacent tile, definitely need buffer
        if (!tilemap.HasTile(adjacentPos))
            return true;
        
        // If adjacent tile exists, check if it would visually connect
        TileBase adjacentTile = tilemap.GetTile(adjacentPos);
        
        if (adjacentTile is OffsetTile adjacentOffsetTile)
        {
            // Both tiles are OffsetTiles - check if their visual content connects
            TileVisualBounds adjacentBounds = CalculateOffsetTileVisualBounds(tilemap, adjacentPos, adjacentOffsetTile);
            
            if (!adjacentBounds.isValid)
                return true; // Safe fallback
            
            // Check if there's a gap between the visual edges
            if (direction == EdgeDirection.Left)
            {
                // Check if adjacent tile's right edge connects to our left edge
                float gap = visualBounds.leftEdgeX - adjacentBounds.rightEdgeX;
                return gap > 0.05f; // Small tolerance for floating point precision
            }
            else // EdgeDirection.Right
            {
                // Check if our right edge connects to adjacent tile's left edge
                float gap = adjacentBounds.leftEdgeX - visualBounds.rightEdgeX;
                return gap > 0.05f; // Small tolerance for floating point precision
            }
        }
        else
        {
            // Adjacent tile is regular tile - check if our visual content reaches the shared edge
            Vector3 tileCenter = tilemap.GetCellCenterWorld(pos);
            Vector3 tileSize = tilemap.cellSize;
            
            if (direction == EdgeDirection.Left)
            {
                float gridLeftEdge = tileCenter.x - (tileSize.x * 0.5f);
                return visualBounds.leftEdgeX > gridLeftEdge + 0.05f; // Visual content doesn't reach grid edge
            }
            else // EdgeDirection.Right
            {
                float gridRightEdge = tileCenter.x + (tileSize.x * 0.5f);
                return visualBounds.rightEdgeX < gridRightEdge - 0.05f; // Visual content doesn't reach grid edge
            }
        }
    }
    
    private static void CreateSmartBuffer(Tilemap tilemap, Vector3Int pos, TileVisualBounds visualBounds, int bufferLayer, EdgeDirection direction, float bufferWidth, float bufferHeight)
    {
        Vector3 tileSize = tilemap.cellSize;
        
        // Position buffer at the actual visual edge, not the grid edge
        float edgeX = (direction == EdgeDirection.Left) ? visualBounds.leftEdgeX : visualBounds.rightEdgeX;
        
        // Offset buffer slightly outside the visual edge
        float bufferOffsetX = edgeX + ((float)direction * bufferWidth * 0.5f);
        
        Vector3 bufferPosition = new Vector3(bufferOffsetX, visualBounds.topEdgeY, 0);
        
        GameObject buffer = new GameObject($"LandingBuffer_{(direction == EdgeDirection.Left ? "Left" : "Right")}_Smart_{pos.x}_{pos.y}");
        buffer.transform.position = bufferPosition;
        buffer.transform.parent = tilemap.transform;
        
        BoxCollider2D col = buffer.AddComponent<BoxCollider2D>();
        col.size = new Vector2(bufferWidth, bufferHeight);
        col.isTrigger = true;
        buffer.layer = bufferLayer;
    }
    
    private static bool IsComplexShape(OffsetTile offsetTile)
    {
        if (offsetTile.sprite == null) return false;
        
        string spriteName = offsetTile.sprite.name.ToLower();
        return spriteName.Contains("_l_missing_"); // Triangles are now handled early
    }
    
    private static void CreateBuffersForComplexShape(Tilemap tilemap, Vector3Int pos, OffsetTile offsetTile, int bufferLayer, float bufferWidth, float bufferHeight, ref int count)
    {
        TileSegment[] segments = GetTileSegments(offsetTile);
        
        if (segments == null || segments.Length == 0)
        {
            // Fallback to simple logic for L-shapes that couldn't be segmented
            CreateBuffersForRegularTile(tilemap, pos, bufferLayer, bufferWidth, bufferHeight, ref count);
            return;
        }
        
        // Process each segment independently
        for (int i = 0; i < segments.Length; i++)
        {
            CreateBuffersForSegment(tilemap, pos, segments[i], offsetTile, bufferLayer, bufferWidth, bufferHeight, ref count, i);
        }
    }
    
    private static TileSegment[] GetTileSegments(OffsetTile offsetTile)
    {
        if (offsetTile.sprite == null) return null;
        
        string spriteName = offsetTile.sprite.name.ToLower();
        
        // L-shaped tiles (75% coverage)
        if (spriteName.Contains("_l_missing_tl"))
        {
            // Missing top-left: ┐ shape
            // Top-right segment + bottom-left segment + bottom-right segment
            return new TileSegment[]
            {
                new TileSegment(new Rect(32, 32, 32, 32)),  // Top-right segment (landable)
                new TileSegment(new Rect(0, 0, 32, 32)),    // Bottom-left segment (landable)
                new TileSegment(new Rect(32, 0, 32, 32))    // Bottom-right segment (NOT landable - wall)
            };
        }
        else if (spriteName.Contains("_l_missing_tr"))
        {
            // Missing top-right: ┌ shape  
            // Top-left segment + bottom-left segment + bottom-right segment
            return new TileSegment[]
            {
                new TileSegment(new Rect(0, 32, 32, 32)),  // Top-left segment (landable)
                new TileSegment(new Rect(0, 0, 32, 32)),   // Bottom-left segment (NOT landable - wall)
                new TileSegment(new Rect(32, 0, 32, 32))   // Bottom-right segment (landable)
            };
        }
        else if (spriteName.Contains("_l_missing_bl"))
        {
            // Missing bottom-left: ┘ shape
            // Top-left segment + top-right segment + bottom-right segment
            return new TileSegment[]
            {
                new TileSegment(new Rect(0, 32, 32, 32)),   // Top-left segment (landable)
                new TileSegment(new Rect(32, 32, 32, 32)),  // Top-right segment (landable)
                new TileSegment(new Rect(32, 0, 32, 32))    // Bottom-right segment (NOT landable - wall)
            };
        }
        else if (spriteName.Contains("_l_missing_br"))
        {
            // Missing bottom-right: └ shape
            // Top-left segment + top-right segment + bottom-left segment
            return new TileSegment[]
            {
                new TileSegment(new Rect(0, 32, 32, 32)),   // Top-left segment (landable)
                new TileSegment(new Rect(32, 32, 32, 32)),  // Top-right segment (landable) 
                new TileSegment(new Rect(0, 0, 32, 32))     // Bottom-left segment (NOT landable - wall)
            };
        }
        
        return null; // Unknown shape, use fallback
    }
    
    
    private static void CreateBuffersForSegment(Tilemap tilemap, Vector3Int pos, TileSegment segment, OffsetTile offsetTile, int bufferLayer, float bufferWidth, float bufferHeight, ref int count, int segmentIndex)
    {
        // CRITICAL: Only add buffers to segments that are both topmost AND represent landing surfaces
        if (!IsSegmentTopmost(tilemap, pos, segment))
            return;
            
        // ADDITIONAL CHECK: Only create buffers for segments that are actual landing surfaces (top of L-shape)
        if (!IsSegmentLandingSurface(segment, offsetTile))
            return;
        
        // Convert segment local bounds to world space
        TileSegmentBounds worldBounds = CalculateSegmentWorldBounds(tilemap, pos, segment, offsetTile);
        
        if (!worldBounds.isValid) return;
        
        // Simple logic: check if other segments exist to the left/right of this segment
        bool needsLeftBuffer = !DoesSegmentHaveNeighborInDirection(tilemap, pos, segment, offsetTile, EdgeDirection.Left);
        bool needsRightBuffer = !DoesSegmentHaveNeighborInDirection(tilemap, pos, segment, offsetTile, EdgeDirection.Right);
        
        if (needsLeftBuffer)
        {
            CreateSegmentBuffer(tilemap, pos, worldBounds, bufferLayer, EdgeDirection.Left, bufferWidth, bufferHeight, segmentIndex);
            count++;
        }
        
        if (needsRightBuffer)
        {
            CreateSegmentBuffer(tilemap, pos, worldBounds, bufferLayer, EdgeDirection.Right, bufferWidth, bufferHeight, segmentIndex);
            count++;
        }
    }
    
    private struct TileSegmentBounds
    {
        public bool isValid;
        public float leftEdgeX;
        public float rightEdgeX;
        public float topEdgeY;
        public Vector3 centerWorld;
    }
    
    private static TileSegmentBounds CalculateSegmentWorldBounds(Tilemap tilemap, Vector3Int pos, TileSegment segment, OffsetTile offsetTile)
    {
        TileSegmentBounds bounds = new TileSegmentBounds();
        
        // Get tile center and size
        Vector3 tileCenter = tilemap.GetCellCenterWorld(pos);
        Vector3 tileSize = tilemap.cellSize;
        
        // Apply OffsetTile transform
        Matrix4x4 transform = offsetTile.transform;
        Vector3 offsetFromTransform = new Vector3(transform.m03, transform.m13, transform.m23);
        
        // Convert segment local bounds (0-64) to world space
        // Segment bounds are in tile-local coordinates where (0,0) is bottom-left, (64,64) is top-right
        float segmentCenterX = segment.localBounds.center.x - 32f; // Convert to centered coordinates (-32 to +32)
        float segmentCenterY = segment.localBounds.center.y - 32f;
        
        // Scale to Unity units (assuming 64 pixels per unit)
        float unitsPerPixel = tileSize.x / 64f;
        segmentCenterX *= unitsPerPixel;
        segmentCenterY *= unitsPerPixel;
        
        // Calculate segment world center
        Vector3 segmentWorldCenter = tileCenter + offsetFromTransform + new Vector3(segmentCenterX, segmentCenterY, 0);
        
        // Calculate segment edges
        float segmentHalfWidth = (segment.localBounds.width * 0.5f) * unitsPerPixel;
        float segmentHalfHeight = (segment.localBounds.height * 0.5f) * unitsPerPixel;
        
        bounds.centerWorld = segmentWorldCenter;
        bounds.leftEdgeX = segmentWorldCenter.x - segmentHalfWidth;
        bounds.rightEdgeX = segmentWorldCenter.x + segmentHalfWidth;
        bounds.topEdgeY = segmentWorldCenter.y + segmentHalfHeight;
        bounds.isValid = true;
        
        return bounds;
    }
    
    
    private static bool ShouldCreateSegmentEdgeBuffer(Tilemap tilemap, Vector3Int pos, TileSegmentBounds segmentBounds, TileSegment segment, EdgeDirection direction)
    {
        Vector3Int adjacentPos = pos + new Vector3Int((int)direction, 0, 0);
        
        // If no adjacent tile, definitely need buffer
        if (!tilemap.HasTile(adjacentPos))
            return true;
        
        // If adjacent tile exists, check if it would visually connect to this segment
        TileBase adjacentTile = tilemap.GetTile(adjacentPos);
        
        if (adjacentTile is OffsetTile adjacentOffsetTile)
        {
            // Complex logic - would need to check if adjacent tile's segments connect to this segment
            // For now, use simple bounds checking with tolerance
            TileVisualBounds adjacentBounds = CalculateOffsetTileVisualBounds(tilemap, adjacentPos, adjacentOffsetTile);
            
            if (!adjacentBounds.isValid)
                return true;
            
            // Check if there's a gap between the segment edge and adjacent tile
            if (direction == EdgeDirection.Left)
            {
                float gap = segmentBounds.leftEdgeX - adjacentBounds.rightEdgeX;
                return gap > 0.05f;
            }
            else // EdgeDirection.Right
            {
                float gap = adjacentBounds.leftEdgeX - segmentBounds.rightEdgeX;
                return gap > 0.05f;
            }
        }
        else
        {
            // Adjacent tile is regular tile - check if segment reaches the shared grid edge
            Vector3 tileCenter = tilemap.GetCellCenterWorld(pos);
            Vector3 tileSize = tilemap.cellSize;
            
            if (direction == EdgeDirection.Left)
            {
                float gridLeftEdge = tileCenter.x - (tileSize.x * 0.5f);
                return segmentBounds.leftEdgeX > gridLeftEdge + 0.05f;
            }
            else // EdgeDirection.Right
            {
                float gridRightEdge = tileCenter.x + (tileSize.x * 0.5f);
                return segmentBounds.rightEdgeX < gridRightEdge - 0.05f;
            }
        }
    }
    
    private static bool IsSegmentLandingSurface(TileSegment segment, OffsetTile offsetTile)
    {
        // A segment can be landed on if it has an exposed top surface
        // This means no other segment in the same tile is directly above it
        
        TileSegment[] allSegments = GetTileSegments(offsetTile);
        if (allSegments == null) return true;
        
        // Check if any other segment is directly above this one
        foreach (var otherSegment in allSegments)
        {
            if (otherSegment.localBounds.Equals(segment.localBounds)) continue; // Skip self
            
            // Check if the other segment is directly above (overlaps horizontally and is higher)
            bool overlapsHorizontally = !(otherSegment.localBounds.xMax <= segment.localBounds.xMin || 
                                         otherSegment.localBounds.xMin >= segment.localBounds.xMax);
            bool isDirectlyAbove = otherSegment.localBounds.yMin >= segment.localBounds.yMax - 1f; // Small tolerance
            
            if (overlapsHorizontally && isDirectlyAbove)
            {
                // There's a segment above this one - this segment cannot be landed on
                return false;
            }
        }
        
        // No segment above - this segment can be landed on
        return true;
    }
    
    private static bool DoesSegmentHaveNeighborInDirection(Tilemap tilemap, Vector3Int pos, TileSegment segment, OffsetTile offsetTile, EdgeDirection direction)
    {
        // Get all segments from this tile
        TileSegment[] allSegments = GetTileSegments(offsetTile);
        if (allSegments == null || allSegments.Length <= 1) return false;
        
        // Check if any other segment in this tile is adjacent to this segment in the given direction
        foreach (var otherSegment in allSegments)
        {
            if (otherSegment.localBounds.Equals(segment.localBounds)) continue; // Skip self
            
            // Check if the other segment is adjacent in the specified direction
            if (direction == EdgeDirection.Left)
            {
                // Check if other segment is to the left and at similar height
                bool isToTheLeft = otherSegment.localBounds.xMax <= segment.localBounds.xMin + 1f; // Small tolerance
                bool isSimilarHeight = Mathf.Abs(otherSegment.localBounds.center.y - segment.localBounds.center.y) < 16f;
                if (isToTheLeft && isSimilarHeight) return true;
            }
            else // EdgeDirection.Right
            {
                // Check if other segment is to the right and at similar height
                bool isToTheRight = otherSegment.localBounds.xMin >= segment.localBounds.xMax - 1f; // Small tolerance
                bool isSimilarHeight = Mathf.Abs(otherSegment.localBounds.center.y - segment.localBounds.center.y) < 16f;
                if (isToTheRight && isSimilarHeight) return true;
            }
        }
        
        // Also check adjacent tiles in the tilemap
        Vector3Int adjacentPos = pos + new Vector3Int((int)direction, 0, 0);
        if (tilemap.HasTile(adjacentPos))
        {
            // There's a tile adjacent - assume it connects (simplified)
            return true;
        }
        
        return false;
    }
    
    private static bool IsSegmentTopmost(Tilemap tilemap, Vector3Int pos, TileSegment segment)
    {
        // Check if there's any tile directly above this segment's area
        Vector3Int abovePos = pos + Vector3Int.up;
        
        if (!tilemap.HasTile(abovePos))
        {
            // No tile above at all - segment is definitely topmost
            return true;
        }
        
        // There is a tile above - check if it overlaps with this segment's area
        TileBase aboveTile = tilemap.GetTile(abovePos);
        
        if (aboveTile is OffsetTile aboveOffsetTile)
        {
            // Above tile is also an OffsetTile - need to check if its content overlaps with our segment
            return !DoesOffsetTileOverlapSegment(tilemap, abovePos, aboveOffsetTile, pos, segment);
        }
        else
        {
            // Above tile is a regular tile (covers full grid cell) - check if it overlaps our segment
            return !DoesFullTileOverlapSegment(segment);
        }
    }
    
    private static bool DoesFullTileOverlapSegment(TileSegment segment)
    {
        // A full tile (regular tile) covers the entire 64x64 grid cell
        // Check if our segment's area would be covered by a full tile above
        Rect fullTileRect = new Rect(0, 0, 64, 64);
        return segment.localBounds.Overlaps(fullTileRect);
    }
    
    private static bool DoesOffsetTileOverlapSegment(Tilemap tilemap, Vector3Int aboveTilePos, OffsetTile aboveOffsetTile, Vector3Int segmentTilePos, TileSegment segment)
    {
        // Calculate the actual visual bounds of the above OffsetTile
        TileVisualBounds aboveBounds = CalculateOffsetTileVisualBounds(tilemap, aboveTilePos, aboveOffsetTile);
        
        if (!aboveBounds.isValid)
            return false; // Can't determine overlap, assume no overlap
        
        // Convert above tile's world bounds to segment tile's local coordinates for comparison
        Vector3 segmentTileCenter = tilemap.GetCellCenterWorld(segmentTilePos);
        Vector3 tileSize = tilemap.cellSize;
        
        // Convert above bounds to local coordinates of segment tile
        float unitsPerPixel = tileSize.x / 64f;
        
        float aboveLeftLocal = ((aboveBounds.leftEdgeX - segmentTileCenter.x) / unitsPerPixel) + 32f;
        float aboveRightLocal = ((aboveBounds.rightEdgeX - segmentTileCenter.x) / unitsPerPixel) + 32f;
        float aboveBottomLocal = ((aboveBounds.centerWorld.y - (tileSize.y * 0.5f) - segmentTileCenter.y) / unitsPerPixel) + 32f;
        float aboveTopLocal = ((aboveBounds.topEdgeY - segmentTileCenter.y) / unitsPerPixel) + 32f;
        
        Rect aboveLocalRect = new Rect(aboveLeftLocal, aboveBottomLocal, aboveRightLocal - aboveLeftLocal, aboveTopLocal - aboveBottomLocal);
        
        // Check if the above tile's content overlaps with our segment
        return segment.localBounds.Overlaps(aboveLocalRect);
    }
    
    private static void CreateSegmentBuffer(Tilemap tilemap, Vector3Int pos, TileSegmentBounds segmentBounds, int bufferLayer, EdgeDirection direction, float bufferWidth, float bufferHeight, int segmentIndex)
    {
        // Position buffer at the segment's edge
        float edgeX = (direction == EdgeDirection.Left) ? segmentBounds.leftEdgeX : segmentBounds.rightEdgeX;
        float bufferOffsetX = edgeX + ((float)direction * bufferWidth * 0.5f);
        
        Vector3 bufferPosition = new Vector3(bufferOffsetX, segmentBounds.topEdgeY, 0);
        
        GameObject buffer = new GameObject($"LandingBuffer_{(direction == EdgeDirection.Left ? "Left" : "Right")}_Segment{segmentIndex}_{pos.x}_{pos.y}");
        buffer.transform.position = bufferPosition;
        buffer.transform.parent = tilemap.transform;
        
        BoxCollider2D col = buffer.AddComponent<BoxCollider2D>();
        col.size = new Vector2(bufferWidth, bufferHeight);
        col.isTrigger = true;
        buffer.layer = bufferLayer;
    }
    
    private static void CreateBuffer(Tilemap tilemap, Vector3Int tilePos, int bufferLayer, int direction, float bufferWidth, float bufferHeight)
    {
        Vector3 worldPos = tilemap.GetCellCenterWorld(tilePos);
        Vector3 tileSize = tilemap.cellSize;

        // Place buffer at the top left or right edge of the tile
        Vector3 offset = new Vector3(direction * (tileSize.x / 2f + bufferWidth / 2f), tileSize.y / 2f, 0);
        GameObject buffer = new GameObject($"LandingBuffer_{(direction == -1 ? "Left" : "Right")}_{tilePos.x}_{tilePos.y}");
        buffer.transform.position = worldPos + offset;
        buffer.transform.parent = tilemap.transform;
        BoxCollider2D col = buffer.AddComponent<BoxCollider2D>();
        col.size = new Vector2(bufferWidth, bufferHeight);
        col.isTrigger = true;
        buffer.layer = bufferLayer;
    }
} 