using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Custom tile that applies transform offsets for precise grid positioning.
/// Used by the Offset Tile Slicer Tool to create grid-aligned tiles.
/// </summary>
[CreateAssetMenu(fileName = "OffsetTile", menuName = "Tiles/OffsetTile")]
public class OffsetTile : TileBase
{
    [Header("Tile Settings")]
    public Sprite sprite;
    
    [Header("Transform Offset")]
    public Matrix4x4 transform = Matrix4x4.identity;
    
    [Header("Collision")]
    public Tile.ColliderType colliderType = Tile.ColliderType.Sprite;
    
    [Header("Custom Collision (Optional)")]
    [SerializeField] private Vector2[] customCollisionPoints;
    [Tooltip("Use custom collision shape instead of sprite bounds. Leave empty to use sprite collision.")]
    
    /// <summary>
    /// Set custom collision points for precise L-shape collision boundaries
    /// </summary>
    public void SetCustomCollisionPoints(Vector2[] points)
    {
        customCollisionPoints = points;
    }
    
    /// <summary>
    /// Get the custom collision points if defined
    /// </summary>
    public Vector2[] GetCustomCollisionPoints()
    {
        return customCollisionPoints;
    }
    
    /// <summary>
    /// Check if this tile has custom collision defined
    /// </summary>
    public bool HasCustomCollision()
    {
        return customCollisionPoints != null && customCollisionPoints.Length > 2;
    }
    
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = sprite;
        tileData.transform = transform;
        tileData.flags = TileFlags.None;
        
        // For custom tiles (L-shapes and corners), disable collision at tile level - we'll handle it via custom system
        if (IsCustomTile())
        {
            tileData.colliderType = Tile.ColliderType.None;
        }
        else
        {
            tileData.colliderType = colliderType;
        }
    }
    
    /// <summary>
    /// Check if this tile needs custom collision handling (L-shapes and corners)
    /// </summary>
    private bool IsCustomTile()
    {
        if (sprite == null) return false;
        
        string spriteName = sprite.name.ToLower();
        return spriteName.Contains("_l_missing_") || 
               (spriteName.Contains("75_") && spriteName.Contains("missing")) ||
               spriteName.Contains("_corner");
    }
    
    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        // No animation for this tile
        return false;
    }
    
    public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
    {
        if (go != null)
        {
            go.transform.position = go.transform.position;
        }
        return true;
    }
}