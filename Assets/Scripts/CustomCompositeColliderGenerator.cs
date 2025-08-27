using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Creates custom PolygonCollider2D shapes for special tiles (L-shapes, corners, triangular slopes, and edge tiles) that integrate seamlessly with CompositeCollider2D.
/// Custom tiles are excluded from auto-generation and replaced with precise collision shapes matching their visual content.
/// Triangular tiles create smooth slopes perfect for platformer movement.
/// Edge tiles (50% coverage) provide precise collision for left/right/top/bottom positioned content.
/// </summary>
[RequireComponent(typeof(TilemapCollider2D))]
public class CustomCompositeColliderGenerator : MonoBehaviour
{
    [Header("Custom Collision Settings")]
    [Tooltip("Physics material for custom tile collision")]
    public PhysicsMaterial2D physicsMaterial;
    
    private Tilemap tilemap;
    private TilemapCollider2D tilemapCollider;
    private CompositeCollider2D compositeCollider;
    
    void Start()
    {
        SetupComponents();
        CreateCustomColliders();
    }
    
    void SetupComponents()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapCollider = GetComponent<TilemapCollider2D>();
        compositeCollider = GetComponent<CompositeCollider2D>();
        
        if (tilemap == null)
        {
            Debug.LogError("CustomCompositeColliderGenerator: No Tilemap component found!");
            return;
        }
    }
    
    /// <summary>
    /// Create custom collision shapes for special tiles (L-shapes, corners, triangular slopes, and edge tiles)
    /// </summary>
    [ContextMenu("Create Custom Colliders")]
    public void CreateCustomColliders()
    {
        if (tilemap == null) SetupComponents();
        
        var customTilePositions = FindCustomTilePositions();
        
        if (customTilePositions.Count == 0)
        {
            Debug.Log("CustomCompositeColliderGenerator: No custom tiles found");
            return;
        }
        
        Debug.Log($"CustomCompositeColliderGenerator: Creating custom colliders for {customTilePositions.Count} tiles");
        
        // Create individual PolygonCollider2D for each custom tile that will be used by Composite Collider
        foreach (var kvp in customTilePositions)
        {
            CreateCustomCollider(kvp.Key, kvp.Value);
        }
        
        Debug.Log("CustomCompositeColliderGenerator: Custom collider generation complete");
    }
    
    /// <summary>
    /// Find all custom tile positions (L-shapes, corners, triangular slopes, and edge tiles)
    /// </summary>
    private Dictionary<Vector3Int, OffsetTile> FindCustomTilePositions()
    {
        var customTilePositions = new Dictionary<Vector3Int, OffsetTile>();
        BoundsInt bounds = tilemap.cellBounds;
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(position);
                
                if (tile is OffsetTile offsetTile && IsCustomTile(offsetTile))
                {
                    customTilePositions[position] = offsetTile;
                }
            }
        }
        
        return customTilePositions;
    }
    
    /// <summary>
    /// Check if tile needs custom collision (L-shapes, corners, triangular slopes, and edge tiles)
    /// </summary>
    private bool IsCustomTile(OffsetTile offsetTile)
    {
        if (offsetTile.sprite == null) return false;
        
        string spriteName = offsetTile.sprite.name.ToLower();
        return spriteName.Contains("_l_missing_") || 
               (spriteName.Contains("75_") && spriteName.Contains("missing")) ||
               spriteName.Contains("_corner") ||
               spriteName.Contains("_triangle_") || // Triangular tiles need custom PolygonCollider2D
               spriteName.Contains("_edge") || // Edge tiles (left/right/top/bottom)
               spriteName.Contains("_left_edge") ||
               spriteName.Contains("_right_edge") ||
               spriteName.Contains("_top_edge") ||
               spriteName.Contains("_bottom_edge");
    }
    
    /// <summary>
    /// Create a PolygonCollider2D for a specific custom tile that integrates with Composite Collider
    /// </summary>
    private void CreateCustomCollider(Vector3Int tilePosition, OffsetTile offsetTile)
    {
        Vector3 worldPos = tilemap.CellToWorld(tilePosition);
        
        // Create GameObject that will be used by Composite Collider
        GameObject customTileGO = new GameObject($"CustomTile_{tilePosition.x}_{tilePosition.y}");
        customTileGO.transform.SetParent(transform, false);
        customTileGO.transform.position = worldPos + new Vector3(0.5f, 0.5f, 0f);
        customTileGO.layer = gameObject.layer; // Same layer as tilemap
        
        // Add PolygonCollider2D with precise custom shape
        PolygonCollider2D polygonCollider = customTileGO.AddComponent<PolygonCollider2D>();
        polygonCollider.sharedMaterial = physicsMaterial;
        
        // Set to be used by composite collider
        polygonCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        
        // Generate precise custom collision points
        Vector2[] collisionPoints = GenerateCustomCollisionPoints(offsetTile.sprite.name);
        polygonCollider.points = collisionPoints;
    }
    
    /// <summary>
    /// Generate precise collision points for L-shapes, corners, triangular slopes, and edge tiles
    /// </summary>
    private Vector2[] GenerateCustomCollisionPoints(string spriteName)
    {
        string name = spriteName.ToLower();
        
        // ┌ shape (missing top-right)
        if (name.Contains("_l_missing_tr"))
        {
            return new Vector2[]
            {
                new Vector2(-0.5f, 0.5f),   // Top-left
                new Vector2(0f, 0.5f),      // Top-middle  
                new Vector2(0f, 0f),        // Inner corner
                new Vector2(0.5f, 0f),      // Right-middle
                new Vector2(0.5f, -0.5f),   // Bottom-right
                new Vector2(-0.5f, -0.5f)   // Bottom-left
            };
        }
        // ┐ shape (missing top-left)
        else if (name.Contains("_l_missing_tl"))
        {
            return new Vector2[]
            {
                new Vector2(0f, 0.5f),      // Top-middle
                new Vector2(0.5f, 0.5f),    // Top-right
                new Vector2(0.5f, -0.5f),   // Bottom-right
                new Vector2(-0.5f, -0.5f),  // Bottom-left
                new Vector2(-0.5f, 0f),     // Left-middle
                new Vector2(0f, 0f)         // Inner corner
            };
        }
        // └ shape (missing bottom-right)  
        else if (name.Contains("_l_missing_br"))
        {
            return new Vector2[]
            {
                new Vector2(-0.5f, 0.5f),   // Top-left
                new Vector2(0.5f, 0.5f),    // Top-right
                new Vector2(0.5f, 0f),      // Right-middle
                new Vector2(0f, 0f),        // Inner corner
                new Vector2(0f, -0.5f),     // Bottom-middle
                new Vector2(-0.5f, -0.5f)   // Bottom-left
            };
        }
        // ┘ shape (missing bottom-left)
        else if (name.Contains("_l_missing_bl"))
        {
            return new Vector2[]
            {
                new Vector2(-0.5f, 0.5f),   // Top-left
                new Vector2(0.5f, 0.5f),    // Top-right
                new Vector2(0.5f, -0.5f),   // Bottom-right
                new Vector2(0f, -0.5f),     // Bottom-middle
                new Vector2(0f, 0f),        // Inner corner
                new Vector2(-0.5f, 0f)      // Left-middle
            };
        }
        
        // Handle corner tiles - precise 32x32 collision bounds
        else if (name.Contains("_tl_corner"))
        {
            // Top-left corner: 32x32 area in top-left quadrant
            return new Vector2[]
            {
                new Vector2(-0.5f, 0f),    // Bottom-left of visible area
                new Vector2(-0.5f, 0.5f),  // Top-left corner
                new Vector2(0f, 0.5f),     // Top-right of visible area
                new Vector2(0f, 0f)        // Bottom-right of visible area
            };
        }
        else if (name.Contains("_tr_corner"))
        {
            // Top-right corner: 32x32 area in top-right quadrant
            return new Vector2[]
            {
                new Vector2(0f, 0f),       // Bottom-left of visible area
                new Vector2(0f, 0.5f),     // Top-left of visible area
                new Vector2(0.5f, 0.5f),   // Top-right corner
                new Vector2(0.5f, 0f)      // Bottom-right of visible area
            };
        }
        else if (name.Contains("_bl_corner"))
        {
            // Bottom-left corner: 32x32 area in bottom-left quadrant
            return new Vector2[]
            {
                new Vector2(-0.5f, -0.5f), // Bottom-left corner
                new Vector2(-0.5f, 0f),    // Top-left of visible area
                new Vector2(0f, 0f),       // Top-right of visible area
                new Vector2(0f, -0.5f)     // Bottom-right of visible area
            };
        }
        else if (name.Contains("_br_corner"))
        {
            // Bottom-right corner: 32x32 area in bottom-right quadrant
            return new Vector2[]
            {
                new Vector2(0f, -0.5f),    // Bottom-left of visible area
                new Vector2(0f, 0f),       // Top-left of visible area
                new Vector2(0.5f, 0f),     // Top-right of visible area
                new Vector2(0.5f, -0.5f)   // Bottom-right corner
            };
        }
        
        // Handle triangular slope tiles - SLICE-BASED LOGIC
        else if (name.Contains("_triangle_tl"))  
        {
            // TL triangle: slice bottom-left to top-right, keep top half
            // Vertices: top-left, top-right, bottom-left
            return new Vector2[]
            {
                new Vector2(-0.5f, 0.5f),   // Top-left vertex
                new Vector2(0.5f, 0.5f),    // Top-right vertex  
                new Vector2(-0.5f, -0.5f)   // Bottom-left vertex
            };
        }
        else if (name.Contains("_triangle_tr"))
        {
            // TR triangle: slice top-left to bottom-right, keep top half
            // Vertices: top-right, top-left, bottom-right
            return new Vector2[]
            {
                new Vector2(0.5f, 0.5f),    // Top-right vertex
                new Vector2(-0.5f, 0.5f),   // Top-left vertex
                new Vector2(0.5f, -0.5f)    // Bottom-right vertex
            };
        }
        else if (name.Contains("_triangle_bl"))
        {
            // BL triangle: slice top-left to bottom-right, keep bottom half
            // Vertices: bottom-left, bottom-right, top-left
            return new Vector2[]
            {
                new Vector2(-0.5f, -0.5f),  // Bottom-left vertex
                new Vector2(0.5f, -0.5f),   // Bottom-right vertex
                new Vector2(-0.5f, 0.5f)    // Top-left vertex
            };
        }
        else if (name.Contains("_triangle_br"))
        {
            // BR triangle: slice bottom-left to top-right, keep bottom half
            // Vertices: bottom-right, bottom-left, top-right
            return new Vector2[]
            {
                new Vector2(0.5f, -0.5f),   // Bottom-right vertex
                new Vector2(-0.5f, -0.5f),  // Bottom-left vertex
                new Vector2(0.5f, 0.5f)     // Top-right vertex
            };
        }
        
        // Handle edge tiles - 50% coverage tiles positioned at edges
        else if (name.Contains("_left_edge"))
        {
            // Left edge: 32x64 content positioned at left side
            return new Vector2[]
            {
                new Vector2(-0.5f, -0.5f),   // Bottom-left corner
                new Vector2(-0.5f, 0.5f),    // Top-left corner
                new Vector2(0f, 0.5f),       // Top-right of visible area
                new Vector2(0f, -0.5f)       // Bottom-right of visible area
            };
        }
        else if (name.Contains("_right_edge"))
        {
            // Right edge: 32x64 content positioned at right side
            return new Vector2[]
            {
                new Vector2(0f, -0.5f),      // Bottom-left of visible area
                new Vector2(0f, 0.5f),       // Top-left of visible area
                new Vector2(0.5f, 0.5f),     // Top-right corner
                new Vector2(0.5f, -0.5f)     // Bottom-right corner
            };
        }
        else if (name.Contains("_top_edge"))
        {
            // Top edge: 64x32 content positioned at top side
            return new Vector2[]
            {
                new Vector2(-0.5f, 0f),      // Bottom-left of visible area
                new Vector2(-0.5f, 0.5f),    // Top-left corner
                new Vector2(0.5f, 0.5f),     // Top-right corner
                new Vector2(0.5f, 0f)        // Bottom-right of visible area
            };
        }
        else if (name.Contains("_bottom_edge"))
        {
            // Bottom edge: 64x32 content positioned at bottom side
            return new Vector2[]
            {
                new Vector2(-0.5f, -0.5f),   // Bottom-left corner
                new Vector2(-0.5f, 0f),      // Top-left of visible area
                new Vector2(0.5f, 0f),       // Top-right of visible area
                new Vector2(0.5f, -0.5f)     // Bottom-right corner
            };
        }
        
        // Fallback - should not happen
        return new Vector2[]
        {
            new Vector2(-0.5f, -0.5f),
            new Vector2(-0.5f, 0.5f), 
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, -0.5f)
        };
    }
    
    /// <summary>
    /// Remove all custom collider objects
    /// </summary>
    [ContextMenu("Remove Custom Colliders")]
    public void RemoveCustomColliders()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("CustomTile_") || child.name.StartsWith("LShape_") || child.name.StartsWith("EdgeTile_"))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
        
        Debug.Log("CustomCompositeColliderGenerator: Removed all custom colliders");
    }
    
    /// <summary>
    /// Get descriptive name for triangle orientation (for debugging)
    /// </summary>
    private string GetTriangleDescription(string spriteName)
    {
        string name = spriteName.ToLower();
        if (name.Contains("_triangle_tl")) return "Slope Up-Right ↗"; // TL: slice bottom-left to top-right, keep top
        if (name.Contains("_triangle_tr")) return "Slope Down-Right ↘"; // TR: slice top-left to bottom-right, keep top
        if (name.Contains("_triangle_bl")) return "Slope Up-Left ↖"; // BL: slice top-left to bottom-right, keep bottom
        if (name.Contains("_triangle_br")) return "Slope Down-Left ↙"; // BR: slice bottom-left to top-right, keep bottom
        return "Unknown Triangle";
    }
}