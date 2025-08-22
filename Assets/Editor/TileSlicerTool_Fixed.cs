using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;

public class TileSlicerToolFixed : EditorWindow
{
    private Texture2D sourceTexture;
    private string outputFolder = "Assets/RawTile/Sliced";
    private bool createTileAssets = true;
    
    [MenuItem("Tools/Enhanced Tile Slicer (Fixed)")]
    public static void ShowWindow()
    {
        GetWindow<TileSlicerToolFixed>("Enhanced Tile Slicer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Enhanced Tile Slicer - Grid-Aligned Tile Generation", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Creates properly positioned tiles for 64x64 grid alignment", MessageType.Info);
        GUILayout.Space(10);
        
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source 64x64 Tile:", sourceTexture, typeof(Texture2D), false);
        
        GUILayout.Space(5);
        outputFolder = EditorGUILayout.TextField("Output Folder:", outputFolder);
        
        GUILayout.Space(5);
        createTileAssets = EditorGUILayout.Toggle("Create Tile Assets:", createTileAssets);
        
        GUILayout.Space(10);
        
        if (sourceTexture != null)
        {
            if (sourceTexture.width != 64 || sourceTexture.height != 64)
            {
                EditorGUILayout.HelpBox($"Source texture is {sourceTexture.width}x{sourceTexture.height}. Expected 64x64.", MessageType.Warning);
            }
            
            GUILayout.Label("Create Half Tiles (Grid-Aligned):", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Left/Right Halves (32x64)"))
            {
                CreateVerticalHalves();
            }
            
            if (GUILayout.Button("Create Top/Bottom Halves (64x32)"))
            {
                CreateHorizontalHalves();
            }
            
            GUILayout.Space(5);
            GUILayout.Label("Create Quarter Tiles (Grid-Aligned):", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Corner Quarters (32x32)"))
            {
                CreateCornerQuarters();
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button("Create ALL Tile Variations", GUILayout.Height(30)))
            {
                CreateAllVariations();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Please select a 64x64 source texture to begin.", MessageType.Info);
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("• Half tiles will align to edges of 64x64 grid cells\n" +
            "• Quarter tiles will align to corners of 64x64 grid cells\n" +
            "• All tiles use proper pivot points for grid alignment\n" +
            "• Created tiles work with standard 64x64 tilemaps", MessageType.Info);
    }
    
    private void CreateVerticalHalves()
    {
        if (!ValidateSourceTexture()) return;
        
        PrepareTexture();
        EnsureOutputDirectory();
        
        // Create left half (32x64) - positioned on left side of grid cell
        Texture2D leftHalf = new Texture2D(32, 64);
        Color[] leftPixels = sourceTexture.GetPixels(0, 0, 32, 64);
        leftHalf.SetPixels(leftPixels);
        leftHalf.Apply();
        
        // Create right half (32x64) - positioned on right side of grid cell
        Texture2D rightHalf = new Texture2D(32, 64);
        Color[] rightPixels = sourceTexture.GetPixels(32, 0, 32, 64);
        rightHalf.SetPixels(rightPixels);
        rightHalf.Apply();
        
        // Save with basic settings, then manually set pivot
        string leftPath = SaveTextureBasic(leftHalf, $"{outputFolder}/{sourceTexture.name}_left.png");
        string rightPath = SaveTextureBasic(rightHalf, $"{outputFolder}/{sourceTexture.name}_right.png");
        
        // Set proper pivot points
        SetSpritePivot(leftPath, new Vector2(0f, 0.5f)); // Left center
        SetSpritePivot(rightPath, new Vector2(1f, 0.5f)); // Right center
        
        // Create tile assets if requested
        if (createTileAssets)
        {
            CreateTileAsset(leftPath, "Left Half");
            CreateTileAsset(rightPath, "Right Half");
        }
        
        RestoreTexture();
        Debug.Log($"✓ Created grid-aligned vertical halves: {sourceTexture.name}_left/right.png");
    }
    
    private void CreateHorizontalHalves()
    {
        if (!ValidateSourceTexture()) return;
        
        PrepareTexture();
        EnsureOutputDirectory();
        
        // Create top half (64x32) - positioned on top of grid cell
        Texture2D topHalf = new Texture2D(64, 32);
        Color[] topPixels = sourceTexture.GetPixels(0, 32, 64, 32);
        topHalf.SetPixels(topPixels);
        topHalf.Apply();
        
        // Create bottom half (64x32) - positioned on bottom of grid cell
        Texture2D bottomHalf = new Texture2D(64, 32);
        Color[] bottomPixels = sourceTexture.GetPixels(0, 0, 64, 32);
        bottomHalf.SetPixels(bottomPixels);
        bottomHalf.Apply();
        
        // Save with basic settings, then manually set pivot
        string topPath = SaveTextureBasic(topHalf, $"{outputFolder}/{sourceTexture.name}_top.png");
        string bottomPath = SaveTextureBasic(bottomHalf, $"{outputFolder}/{sourceTexture.name}_bottom.png");
        
        // Set proper pivot points
        SetSpritePivot(topPath, new Vector2(0.5f, 1f)); // Top center
        SetSpritePivot(bottomPath, new Vector2(0.5f, 0f)); // Bottom center
        
        // Create tile assets if requested
        if (createTileAssets)
        {
            CreateTileAsset(topPath, "Top Half");
            CreateTileAsset(bottomPath, "Bottom Half");
        }
        
        RestoreTexture();
        Debug.Log($"✓ Created grid-aligned horizontal halves: {sourceTexture.name}_top/bottom.png");
    }
    
    private void CreateCornerQuarters()
    {
        if (!ValidateSourceTexture()) return;
        
        PrepareTexture();
        EnsureOutputDirectory();
        
        // Create four corner quarters (32x32 each)
        Texture2D topLeft = new Texture2D(32, 32);
        Color[] topLeftPixels = sourceTexture.GetPixels(0, 32, 32, 32);
        topLeft.SetPixels(topLeftPixels);
        topLeft.Apply();
        
        Texture2D topRight = new Texture2D(32, 32);
        Color[] topRightPixels = sourceTexture.GetPixels(32, 32, 32, 32);
        topRight.SetPixels(topRightPixels);
        topRight.Apply();
        
        Texture2D bottomLeft = new Texture2D(32, 32);
        Color[] bottomLeftPixels = sourceTexture.GetPixels(0, 0, 32, 32);
        bottomLeft.SetPixels(bottomLeftPixels);
        bottomLeft.Apply();
        
        Texture2D bottomRight = new Texture2D(32, 32);
        Color[] bottomRightPixels = sourceTexture.GetPixels(32, 0, 32, 32);
        bottomRight.SetPixels(bottomRightPixels);
        bottomRight.Apply();
        
        // Save with basic settings, then manually set pivots
        string tlPath = SaveTextureBasic(topLeft, $"{outputFolder}/{sourceTexture.name}_tl.png");
        string trPath = SaveTextureBasic(topRight, $"{outputFolder}/{sourceTexture.name}_tr.png");
        string blPath = SaveTextureBasic(bottomLeft, $"{outputFolder}/{sourceTexture.name}_bl.png");
        string brPath = SaveTextureBasic(bottomRight, $"{outputFolder}/{sourceTexture.name}_br.png");
        
        // Set proper pivot points for corners
        SetSpritePivot(tlPath, new Vector2(0f, 1f)); // Top-left
        SetSpritePivot(trPath, new Vector2(1f, 1f)); // Top-right
        SetSpritePivot(blPath, new Vector2(0f, 0f)); // Bottom-left
        SetSpritePivot(brPath, new Vector2(1f, 0f)); // Bottom-right
        
        // Create tile assets if requested
        if (createTileAssets)
        {
            CreateTileAsset(tlPath, "Top-Left Quarter");
            CreateTileAsset(trPath, "Top-Right Quarter");
            CreateTileAsset(blPath, "Bottom-Left Quarter");
            CreateTileAsset(brPath, "Bottom-Right Quarter");
        }
        
        RestoreTexture();
        Debug.Log($"✓ Created grid-aligned corner quarters: {sourceTexture.name}_tl/tr/bl/br.png");
    }
    
    private void CreateAllVariations()
    {
        Debug.Log($"Creating all tile variations for: {sourceTexture.name}");
        CreateVerticalHalves();
        CreateHorizontalHalves();
        CreateCornerQuarters();
        Debug.Log($"✓ All tile variations created successfully!");
    }
    
    // Helper methods for texture management
    private TextureImporter sourceImporter;
    private bool wasReadable;
    
    private void PrepareTexture()
    {
        string path = AssetDatabase.GetAssetPath(sourceTexture);
        sourceImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        
        if (sourceImporter == null)
        {
            Debug.LogError("Failed to get texture importer");
            return;
        }
        
        wasReadable = sourceImporter.isReadable;
        if (!wasReadable)
        {
            sourceImporter.isReadable = true;
            AssetDatabase.ImportAsset(path);
        }
    }
    
    private void RestoreTexture()
    {
        if (sourceImporter != null && !wasReadable)
        {
            sourceImporter.isReadable = false;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(sourceTexture));
        }
        AssetDatabase.Refresh();
    }
    
    private void EnsureOutputDirectory()
    {
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            System.IO.Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }
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
    
    private string SaveTextureBasic(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        
        // Import with basic sprite settings
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
        
        return path;
    }
    
    private void SetSpritePivot(string spritePath, Vector2 pivot)
    {
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            AssetDatabase.ImportAsset(spritePath);
            
            Debug.Log($"✓ Set pivot {pivot} for: {System.IO.Path.GetFileName(spritePath)}");
        }
    }
    
    private void CreateTileAsset(string spritePath, string tileName)
    {
        // Load the sprite
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogError($"Failed to load sprite at {spritePath}");
            return;
        }
        
        // Create tile asset
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        
        // Save tile asset in same folder as sprite
        string tileFolder = System.IO.Path.GetDirectoryName(spritePath) + "/TileAssets";
        if (!AssetDatabase.IsValidFolder(tileFolder))
        {
            AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(spritePath), "TileAssets");
        }
        
        string tilePath = $"{tileFolder}/{sprite.name}_Tile.asset";
        AssetDatabase.CreateAsset(tile, tilePath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✓ Created tile asset: {System.IO.Path.GetFileName(tilePath)}");
    }
}