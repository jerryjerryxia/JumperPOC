using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Simple and elegant solution: Creates PolygonCollider2D shapes for L-tiles that integrate seamlessly with CompositeCollider2D.
/// L-shaped tiles are excluded from auto-generation and replaced with precise collision shapes.
/// </summary>
[RequireComponent(typeof(TilemapCollider2D))]
public class LShapeCompositeIntegration : MonoBehaviour
{
    [Header("Integration Settings")]
    [Tooltip("Physics material for L-shape collision")]
    public PhysicsMaterial2D physicsMaterial;
    
    private Tilemap tilemap;
    private TilemapCollider2D tilemapCollider;
    private CompositeCollider2D compositeCollider;
    
    void Start()
    {
        SetupComponents();
        CreateLShapeIntegration();
    }
    
    void SetupComponents()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapCollider = GetComponent<TilemapCollider2D>();
        compositeCollider = GetComponent<CompositeCollider2D>();
        
        if (tilemap == null)
        {
            Debug.LogError("LShapeCompositeIntegration: No Tilemap component found!");
            return;
        }
    }
    
    /// <summary>
    /// Create L-shape collision integration with Composite Collider
    /// </summary>
    [ContextMenu("Create L-Shape Integration")]
    public void CreateLShapeIntegration()
    {
        if (tilemap == null) SetupComponents();
        
        var lShapePositions = FindLShapePositions();
        
        if (lShapePositions.Count == 0)
        {
            Debug.Log("LShapeCompositeIntegration: No L-shaped tiles found");
            return;
        }
        
        Debug.Log($"LShapeCompositeIntegration: Creating integration for {lShapePositions.Count} L-shaped tiles");
        
        // Create individual PolygonCollider2D for each L-shape that will be used by Composite Collider
        foreach (var kvp in lShapePositions)
        {
            CreateLShapeCollider(kvp.Key, kvp.Value);
        }
        
        Debug.Log("LShapeCompositeIntegration: Integration complete");
    }
    
    /// <summary>
    /// Find all L-shaped tile positions
    /// </summary>
    private Dictionary<Vector3Int, OffsetTile> FindLShapePositions()
    {
        var lShapePositions = new Dictionary<Vector3Int, OffsetTile>();
        BoundsInt bounds = tilemap.cellBounds;
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(position);
                
                if (tile is OffsetTile offsetTile && IsLShapeTile(offsetTile))
                {
                    lShapePositions[position] = offsetTile;
                }
            }
        }
        
        return lShapePositions;
    }
    
    /// <summary>
    /// Check if tile is L-shaped
    /// </summary>
    private bool IsLShapeTile(OffsetTile offsetTile)
    {
        if (offsetTile.sprite == null) return false;
        
        string spriteName = offsetTile.sprite.name.ToLower();
        return spriteName.Contains("_l_missing_") || 
               (spriteName.Contains("75_") && spriteName.Contains("missing"));
    }
    
    /// <summary>
    /// Create a PolygonCollider2D for a specific L-shaped tile that integrates with Composite Collider
    /// </summary>
    private void CreateLShapeCollider(Vector3Int tilePosition, OffsetTile offsetTile)
    {
        Vector3 worldPos = tilemap.CellToWorld(tilePosition);
        
        // Create GameObject that will be used by Composite Collider
        GameObject lShapeGO = new GameObject($"LShape_{tilePosition.x}_{tilePosition.y}");
        lShapeGO.transform.SetParent(transform, false);
        lShapeGO.transform.position = worldPos + new Vector3(0.5f, 0.5f, 0f);
        lShapeGO.layer = gameObject.layer; // Same layer as tilemap
        
        // Add PolygonCollider2D with precise L-shape
        PolygonCollider2D polygonCollider = lShapeGO.AddComponent<PolygonCollider2D>();
        polygonCollider.sharedMaterial = physicsMaterial;
        
        // Set to be used by composite collider
        polygonCollider.usedByComposite = true;
        
        // Generate precise L-shape collision points
        Vector2[] collisionPoints = GenerateLShapePoints(offsetTile.sprite.name);
        polygonCollider.points = collisionPoints;
    }
    
    /// <summary>
    /// Generate precise collision points for L-shapes
    /// </summary>
    private Vector2[] GenerateLShapePoints(string spriteName)
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
    /// Remove all L-shape integration objects
    /// </summary>
    [ContextMenu("Remove L-Shape Integration")]
    public void RemoveLShapeIntegration()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("LShape_"))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
        
        Debug.Log("LShapeCompositeIntegration: Removed all L-shape integrations");
    }
}