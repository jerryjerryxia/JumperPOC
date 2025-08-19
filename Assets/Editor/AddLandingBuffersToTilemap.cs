using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class AddLandingBuffersToTilemap : EditorWindow
{
    private float bufferWidth = 0.28f;
    private float bufferHeight = 0.05f;

    [MenuItem("Tools/Add Landing Buffers to Tilemap Platforms and Walls")]
    public static void ShowWindow()
    {
        GetWindow<AddLandingBuffersToTilemap>("Add Landing Buffers");
    }

    void OnGUI()
    {
        GUILayout.Label("Landing Buffer Settings", EditorStyles.boldLabel);
        bufferWidth = EditorGUILayout.FloatField("Buffer Width", bufferWidth);
        bufferHeight = EditorGUILayout.FloatField("Buffer Height", bufferHeight);
        if (GUILayout.Button("Add Buffers to Tilemaps"))
        {
            AddBuffers(bufferWidth, bufferHeight);
        }
        if (GUILayout.Button("Remove All Buffers"))
        {
            RemoveAllBuffers();
        }
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("\nTo make the buffers work, your player ground check should look like:\n\nint groundMask = (1 << LayerMask.NameToLayer(\"Ground\")) | (1 << LayerMask.NameToLayer(\"LandingBuffer\"));\nisGrounded = Physics2D.OverlapCircle(feetPos, 0.02f, groundMask);\n\nReplace 'Ground' with your actual ground layer name if different.", MessageType.Info);
    }

    private static void AddBuffers(float bufferWidth, float bufferHeight)
    {
        // Find the Grid and all relevant Tilemaps
        Grid grid = GameObject.FindFirstObjectByType<Grid>();
        if (grid == null)
        {
            Debug.LogError("No Grid found in the scene.");
            return;
        }

        Tilemap[] tilemaps = grid.GetComponentsInChildren<Tilemap>();
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
            // Check if this tilemap should have buffers (platforms, walls, or floors)
            bool shouldAddBuffers = name.Contains("platform") || name.Contains("wall") || name.Contains("floor");
            if (!shouldAddBuffers)
                continue;

            BoundsInt bounds = tilemap.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    if (!tilemap.HasTile(pos)) continue;

                    // Only add buffers to topmost tiles (tiles with no tile above them)
                    if (tilemap.HasTile(pos + Vector3Int.up)) continue; // Skip if not topmost
                    
                    // Check left edge
                    if (!tilemap.HasTile(pos + Vector3Int.left))
                    {
                        CreateBuffer(tilemap, pos, bufferLayer, -1, bufferWidth, bufferHeight);
                        count++;
                    }
                    // Check right edge
                    if (!tilemap.HasTile(pos + Vector3Int.right))
                    {
                        CreateBuffer(tilemap, pos, bufferLayer, 1, bufferWidth, bufferHeight);
                        count++;
                    }
                }
            }
        }

        Debug.Log($"Added {count} landing buffer colliders to platform and wall tilemap edges.");
    }

    private static void RemoveAllBuffers()
    {
        Grid grid = GameObject.FindFirstObjectByType<Grid>();
        if (grid == null) return;
        Tilemap[] tilemaps = grid.GetComponentsInChildren<Tilemap>();
        int removed = 0;
        foreach (var tilemap in tilemaps)
        {
            string name = tilemap.name.ToLower();
            if (!(name.Contains("platform") || name.Contains("wall") || name.Contains("floor")))
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
        Debug.Log($"Removed {removed} landing buffer colliders.");
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