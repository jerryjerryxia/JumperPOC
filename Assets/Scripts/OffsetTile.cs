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
    
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = sprite;
        tileData.transform = transform;
        tileData.flags = TileFlags.None;
        tileData.colliderType = colliderType;
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