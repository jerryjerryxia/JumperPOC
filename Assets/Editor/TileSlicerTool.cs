using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;

public class TileSlicerTool : EditorWindow
{
    private Texture2D sourceTexture;
    private string outputFolder = "Assets/Tiles/Sliced";
    
    [MenuItem("Tools/Tile Slicer")]
    public static void ShowWindow()
    {
        GetWindow<TileSlicerTool>("Tile Slicer Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tile Slicer - Create 32x64 and 64x32 tiles from 64x64", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source 64x64 Tile:", sourceTexture, typeof(Texture2D), false);
        
        GUILayout.Space(5);
        outputFolder = EditorGUILayout.TextField("Output Folder:", outputFolder);
        
        GUILayout.Space(10);
        
        if (sourceTexture != null)
        {
            if (sourceTexture.width != 64 || sourceTexture.height != 64)
            {
                EditorGUILayout.HelpBox($"Source texture is {sourceTexture.width}x{sourceTexture.height}. Expected 64x64.", MessageType.Warning);
            }
            
            GUILayout.Label("Create Tiles:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create 32x64 Tiles (Vertical Halves)"))
            {
                CreateVerticalHalves();
            }
            
            if (GUILayout.Button("Create 64x32 Tiles (Horizontal Halves)"))
            {
                CreateHorizontalHalves();
            }
            
            if (GUILayout.Button("Create All Tile Variations"))
            {
                CreateVerticalHalves();
                CreateHorizontalHalves();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Please select a 64x64 source texture.", MessageType.Info);
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Alternative: Sprite Editor Method", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("You can also use Unity's Sprite Editor:\n" +
            "1. Select your texture in Project window\n" +
            "2. Set Sprite Mode to 'Multiple'\n" +
            "3. Open Sprite Editor\n" +
            "4. Use 'Slice' with custom grid sizes", MessageType.Info);
    }
    
    private void CreateVerticalHalves()
    {
        if (!ValidateSourceTexture()) return;
        
        string path = AssetDatabase.GetAssetPath(sourceTexture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (importer == null)
        {
            Debug.LogError("Failed to get texture importer");
            return;
        }
        
        // Make texture readable temporarily
        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            AssetDatabase.ImportAsset(path);
        }
        
        // Create output directory
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            System.IO.Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }
        
        // Create left half (32x64)
        Texture2D leftHalf = new Texture2D(32, 64);
        Color[] leftPixels = sourceTexture.GetPixels(0, 0, 32, 64);
        leftHalf.SetPixels(leftPixels);
        leftHalf.Apply();
        
        // Create right half (32x64)
        Texture2D rightHalf = new Texture2D(32, 64);
        Color[] rightPixels = sourceTexture.GetPixels(32, 0, 32, 64);
        rightHalf.SetPixels(rightPixels);
        rightHalf.Apply();
        
        // Save textures
        SaveTexture(leftHalf, $"{outputFolder}/{sourceTexture.name}_32x64_left.png");
        SaveTexture(rightHalf, $"{outputFolder}/{sourceTexture.name}_32x64_right.png");
        
        // Restore original readable state
        if (!wasReadable)
        {
            importer.isReadable = false;
            AssetDatabase.ImportAsset(path);
        }
        
        AssetDatabase.Refresh();
        Debug.Log("Created 32x64 vertical tile halves");
    }
    
    private void CreateHorizontalHalves()
    {
        if (!ValidateSourceTexture()) return;
        
        string path = AssetDatabase.GetAssetPath(sourceTexture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (importer == null)
        {
            Debug.LogError("Failed to get texture importer");
            return;
        }
        
        // Make texture readable temporarily
        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            AssetDatabase.ImportAsset(path);
        }
        
        // Create output directory
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            System.IO.Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }
        
        // Create top half (64x32)
        Texture2D topHalf = new Texture2D(64, 32);
        Color[] topPixels = sourceTexture.GetPixels(0, 32, 64, 32);
        topHalf.SetPixels(topPixels);
        topHalf.Apply();
        
        // Create bottom half (64x32)
        Texture2D bottomHalf = new Texture2D(64, 32);
        Color[] bottomPixels = sourceTexture.GetPixels(0, 0, 64, 32);
        bottomHalf.SetPixels(bottomPixels);
        bottomHalf.Apply();
        
        // Save textures
        SaveTexture(topHalf, $"{outputFolder}/{sourceTexture.name}_64x32_top.png");
        SaveTexture(bottomHalf, $"{outputFolder}/{sourceTexture.name}_64x32_bottom.png");
        
        // Restore original readable state
        if (!wasReadable)
        {
            importer.isReadable = false;
            AssetDatabase.ImportAsset(path);
        }
        
        AssetDatabase.Refresh();
        Debug.Log("Created 64x32 horizontal tile halves");
    }
    
    private bool ValidateSourceTexture()
    {
        if (sourceTexture == null)
        {
            Debug.LogError("No source texture selected");
            return false;
        }
        
        if (sourceTexture.width != 64 || sourceTexture.height != 64)
        {
            Debug.LogWarning($"Source texture is {sourceTexture.width}x{sourceTexture.height}, not 64x64. Proceeding anyway...");
        }
        
        return true;
    }
    
    private void SaveTexture(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        
        // Import the texture and set up sprite settings
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsToUnits = 64;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.ImportAsset(path);
        }
    }
}

public class TilePaletteSetup : EditorWindow
{
    [MenuItem("Tools/Setup Tile Palette for Different Sizes")]
    public static void ShowWindow()
    {
        GetWindow<TilePaletteSetup>("Tile Palette Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Tile Palette Setup Instructions", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("To use tiles of different sizes in Unity Tilemap:\n\n" +
            "1. Create separate Grid GameObjects for each tile size:\n" +
            "   • Grid_32x64: Set Cell Size to (0.5, 1, 1)\n" +
            "   • Grid_64x32: Set Cell Size to (1, 0.5, 1)\n" +
            "   • Grid_64x64: Set Cell Size to (1, 1, 1)\n\n" +
            "2. Create Tilemaps as children of each Grid\n\n" +
            "3. Create Tile Assets from your sliced sprites:\n" +
            "   • Right-click sprite → Create → 2D → Tiles → Tile\n\n" +
            "4. Create separate Tile Palettes for each size\n\n" +
            "5. Paint tiles on the appropriate tilemap", MessageType.Info);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Grid Setup"))
        {
            CreateGridSetup();
        }
    }
    
    private void CreateGridSetup()
    {
        // Create parent object
        GameObject tilemapParent = new GameObject("Tilemap_MultiSize");
        
        // Create 64x64 grid (standard)
        GameObject grid64x64 = new GameObject("Grid_64x64");
        grid64x64.transform.parent = tilemapParent.transform;
        Grid gridComponent64 = grid64x64.AddComponent<Grid>();
        gridComponent64.cellSize = new Vector3(1, 1, 1);
        
        GameObject tilemap64 = new GameObject("Tilemap_64x64");
        tilemap64.transform.parent = grid64x64.transform;
        tilemap64.AddComponent<Tilemap>();
        tilemap64.AddComponent<TilemapRenderer>();
        
        // Create 32x64 grid (vertical halves)
        GameObject grid32x64 = new GameObject("Grid_32x64");
        grid32x64.transform.parent = tilemapParent.transform;
        Grid gridComponent32x64 = grid32x64.AddComponent<Grid>();
        gridComponent32x64.cellSize = new Vector3(0.5f, 1, 1);
        
        GameObject tilemap32x64 = new GameObject("Tilemap_32x64");
        tilemap32x64.transform.parent = grid32x64.transform;
        tilemap32x64.AddComponent<Tilemap>();
        tilemap32x64.AddComponent<TilemapRenderer>();
        
        // Create 64x32 grid (horizontal halves)
        GameObject grid64x32 = new GameObject("Grid_64x32");
        grid64x32.transform.parent = tilemapParent.transform;
        Grid gridComponent64x32 = grid64x32.AddComponent<Grid>();
        gridComponent64x32.cellSize = new Vector3(1, 0.5f, 1);
        
        GameObject tilemap64x32 = new GameObject("Tilemap_64x32");
        tilemap64x32.transform.parent = grid64x32.transform;
        tilemap64x32.AddComponent<Tilemap>();
        tilemap64x32.AddComponent<TilemapRenderer>();
        
        Selection.activeGameObject = tilemapParent;
        Debug.Log("Created multi-size tilemap grid setup");
    }
}