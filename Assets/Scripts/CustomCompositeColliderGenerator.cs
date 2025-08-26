using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Creates custom PolygonCollider2D shapes for special tiles (L-shapes and corners) that integrate seamlessly with CompositeCollider2D.
/// Custom tiles are excluded from auto-generation and replaced with precise collision shapes matching their visual content.
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
    /// Create custom collision shapes for special tiles (L-shapes and corners)
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
    /// Find all custom tile positions (L-shapes and corners)
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
    /// Check if tile needs custom collision (L-shapes and corners)
    /// </summary>
    private bool IsCustomTile(OffsetTile offsetTile)
    {
        if (offsetTile.sprite == null) return false;
        
        string spriteName = offsetTile.sprite.name.ToLower();
        return spriteName.Contains("_l_missing_") || 
               (spriteName.Contains("75_") && spriteName.Contains("missing")) ||
               spriteName.Contains("_corner");
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
        polygonCollider.usedByComposite = true;
        
        // Generate precise custom collision points
        Vector2[] collisionPoints = GenerateCustomCollisionPoints(offsetTile.sprite.name);
        polygonCollider.points = collisionPoints;
    }
    
    /// <summary>
    /// Generate precise collision points for L-shapes and corners
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
            if (child.name.StartsWith("CustomTile_") || child.name.StartsWith("LShape_"))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
        
        Debug.Log("CustomCompositeColliderGenerator: Removed all custom colliders");
    }
}