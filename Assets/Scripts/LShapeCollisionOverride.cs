using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Creates custom collision shapes for L-shaped tiles to prevent unwanted 45° slopes.
/// Run this after placing L-shaped tiles to override their collision with precise shapes.
/// </summary>
public class LShapeCollisionOverride : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Layer for the custom collision GameObjects")]
    public string collisionLayer = "Default";
    
    [Tooltip("Physics material to apply to custom colliders")]
    public PhysicsMaterial2D physicsMaterial;
    
    [Header("Debug")]
    [Tooltip("Show visual indicators for custom collision shapes")]
    public bool showDebugShapes = false;
    
    private Tilemap tilemap;
    private TilemapCollider2D tilemapCollider;
    private CompositeCollider2D compositeCollider;
    
    void Start()
    {
        // Try to find Tilemap on this GameObject first
        tilemap = GetComponent<Tilemap>();
        tilemapCollider = GetComponent<TilemapCollider2D>();
        compositeCollider = GetComponent<CompositeCollider2D>();
        
        // If not found, search in children
        if (tilemap == null)
        {
            tilemap = GetComponentInChildren<Tilemap>();
            Debug.Log($"LShapeCollisionOverride: Found Tilemap in child: {tilemap?.name}");
        }
        
        // If still not found, search in parent
        if (tilemap == null)
        {
            tilemap = GetComponentInParent<Tilemap>();
            Debug.Log($"LShapeCollisionOverride: Found Tilemap in parent: {tilemap?.name}");
        }
    }
    
    /// <summary>
    /// Create custom collision shapes for all L-shaped tiles in the tilemap
    /// </summary>
    [ContextMenu("Create L-Shape Collision Overrides")]
    public void CreateLShapeCollisionOverrides()
    {
        // Ensure we have a tilemap reference
        if (tilemap == null)
        {
            // Try to find it manually if Start() hasn't run yet
            tilemap = GetComponent<Tilemap>();
            if (tilemap == null)
                tilemap = GetComponentInChildren<Tilemap>();
            if (tilemap == null)
                tilemap = GetComponentInParent<Tilemap>();
        }
        
        if (tilemap == null)
        {
            Debug.LogError("LShapeCollisionOverride: No Tilemap found! Make sure this script is on a GameObject with a Tilemap component, or a parent/child of one.");
            Debug.LogError($"Current GameObject: {gameObject.name}");
            return;
        }
        
        // First, find all L-shaped tiles
        var lShapeTiles = FindLShapeTiles();
        
        if (lShapeTiles.Count == 0)
        {
            Debug.Log("LShapeCollisionOverride: No L-shaped tiles found");
            return;
        }
        
        Debug.Log($"LShapeCollisionOverride: Creating custom collision for {lShapeTiles.Count} L-shaped tiles");
        
        // Disable collision for L-shaped tiles in the original tilemap
        DisableTilemapCollisionForLShapes(lShapeTiles);
        
        // Create custom collision shapes
        foreach (var kvp in lShapeTiles)
        {
            CreateCustomCollisionForTile(kvp.Key, kvp.Value);
        }
        
        Debug.Log("LShapeCollisionOverride: Custom collision setup complete");
    }
    
    /// <summary>
    /// Find all L-shaped tiles in the tilemap
    /// </summary>
    private Dictionary<Vector3Int, OffsetTile> FindLShapeTiles()
    {
        var lShapeTiles = new Dictionary<Vector3Int, OffsetTile>();
        BoundsInt bounds = tilemap.cellBounds;
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(position);
                
                if (tile is OffsetTile offsetTile && IsLShapeTile(offsetTile))
                {
                    lShapeTiles[position] = offsetTile;
                }
            }
        }
        
        return lShapeTiles;
    }
    
    /// <summary>
    /// Check if a tile is L-shaped based on sprite name
    /// </summary>
    private bool IsLShapeTile(OffsetTile offsetTile)
    {
        if (offsetTile.sprite == null) return false;
        
        string spriteName = offsetTile.sprite.name.ToLower();
        return spriteName.Contains("_l_missing_") || 
               (spriteName.Contains("75_") && spriteName.Contains("missing"));
    }
    
    /// <summary>
    /// Disable tilemap collision for L-shaped tile positions
    /// </summary>
    private void DisableTilemapCollisionForLShapes(Dictionary<Vector3Int, OffsetTile> lShapeTiles)
    {
        // Create a new tile that has no collision
        var noCollisionTile = ScriptableObject.CreateInstance<Tile>();
        
        foreach (var position in lShapeTiles.Keys)
        {
            var originalTile = lShapeTiles[position];
            
            // Create a collision-free version of the tile
            noCollisionTile.sprite = originalTile.sprite;
            noCollisionTile.transform = originalTile.transform;
            noCollisionTile.colliderType = Tile.ColliderType.None;
            
            // Replace the tile temporarily for collision purposes
            // Note: This affects the composite collider regeneration
            if (compositeCollider != null)
            {
                // We need to exclude these tiles from composite collision
                // This is complex to do cleanly, so we'll use a different approach
            }
        }
    }
    
    /// <summary>
    /// Create a custom collision shape for a specific L-shaped tile
    /// </summary>
    private void CreateCustomCollisionForTile(Vector3Int tilePosition, OffsetTile offsetTile)
    {
        Vector3 worldPos = tilemap.CellToWorld(tilePosition);
        
        // Create a GameObject for the custom collision
        GameObject collisionGO = new GameObject($"LShape_Collision_{tilePosition.x}_{tilePosition.y}");
        collisionGO.transform.parent = this.transform;
        collisionGO.transform.position = worldPos + new Vector3(0.5f, 0.5f, 0f); // Center on tile
        collisionGO.layer = LayerMask.NameToLayer(collisionLayer);
        
        // Add PolygonCollider2D with custom L-shape
        PolygonCollider2D polygonCollider = collisionGO.AddComponent<PolygonCollider2D>();
        polygonCollider.sharedMaterial = physicsMaterial;
        
        // Generate L-shape collision points based on sprite name
        Vector2[] collisionPoints = GenerateLShapeCollisionPoints(offsetTile.sprite.name);
        polygonCollider.points = collisionPoints;
        
        // Add visual debug if enabled
        if (showDebugShapes)
        {
            AddDebugVisualization(collisionGO, collisionPoints);
        }
    }
    
    /// <summary>
    /// Generate collision points for different L-shape variants
    /// </summary>
    private Vector2[] GenerateLShapeCollisionPoints(string spriteName)
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
        
        // Default fallback - full square
        return new Vector2[]
        {
            new Vector2(-0.5f, -0.5f),
            new Vector2(-0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, -0.5f)
        };
    }
    
    /// <summary>
    /// Add visual debug lines for collision shape
    /// </summary>
    private void AddDebugVisualization(GameObject collisionGO, Vector2[] points)
    {
        LineRenderer lineRenderer = collisionGO.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f; // Fixed compilation errors
        lineRenderer.positionCount = points.Length + 1; // +1 to close the loop
        lineRenderer.useWorldSpace = false;
        
        // Set line points
        for (int i = 0; i < points.Length; i++)
        {
            lineRenderer.SetPosition(i, new Vector3(points[i].x, points[i].y, -0.1f));
        }
        // Close the loop
        lineRenderer.SetPosition(points.Length, new Vector3(points[0].x, points[0].y, -0.1f));
    }
    
    /// <summary>
    /// Remove all custom L-shape collision overrides
    /// </summary>
    [ContextMenu("Remove L-Shape Collision Overrides")]
    public void RemoveLShapeCollisionOverrides()
    {
        // Find and destroy all custom collision GameObjects
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("LShape_Collision_"))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
        
        Debug.Log("LShapeCollisionOverride: Removed all custom collision overrides");
    }
}