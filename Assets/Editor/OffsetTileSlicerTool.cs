using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;

public class OffsetTileSlicerTool : EditorWindow
{
    private Texture2D sourceTexture;
    private string outputFolder = "Assets/RawTile/Sliced";
    private bool createTileAssets = true;
    
    [MenuItem("Tools/Offset Tile Slicer (WORKING)")]
    public static void ShowWindow()
    {
        GetWindow<OffsetTileSlicerTool>("Offset Tile Slicer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Offset Tile Slicer - TRUE Grid-Aligned Tiles", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Creates tiles that align to grid edges using sprite positioning + custom tile transforms", MessageType.Info);
        GUILayout.Space(10);
        
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source 64x64 Tile:", sourceTexture, typeof(Texture2D), false);
        
        GUILayout.Space(5);
        outputFolder = EditorGUILayout.TextField("Output Folder:", outputFolder);
        
        GUILayout.Space(5);
        createTileAssets = EditorGUILayout.Toggle("Create Offset Tile Assets:", createTileAssets);
        
        GUILayout.Space(10);
        
        if (sourceTexture != null)
        {
            if (sourceTexture.width != 64 || sourceTexture.height != 64)
            {
                EditorGUILayout.HelpBox($"Source texture is {sourceTexture.width}x{sourceTexture.height}. Expected 64x64.", MessageType.Warning);
            }
            
            GUILayout.Label("Create Grid-Aligned Tiles:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Left/Right Halves (32x64)"))
            {
                CreateVerticalHalves();
            }
            
            if (GUILayout.Button("Create Top/Bottom Halves (64x32)"))
            {
                CreateHorizontalHalves();
            }
            
            if (GUILayout.Button("Create Corner Quarters (32x32)"))
            {
                CreateCornerQuarters();
            }
            
            GUILayout.Space(5);
            GUILayout.Label("Create L-Shaped Tiles (75% Coverage):", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create L-Shaped Tiles (Missing Corners)"))
            {
                CreateLShapedTiles();
            }
            
            GUILayout.Space(5);
            GUILayout.Label("Create Triangle Tiles (50% Coverage):", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Triangle Slices (Diagonal Cuts)"))
            {
                CreateTriangleTiles();
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button("Create ALL Grid-Aligned Variations", GUILayout.Height(30)))
            {
                CreateAllVariations();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Please select a 64x64 source texture to begin.", MessageType.Info);
        }
        
        GUILayout.Space(10);
        GUILayout.Label("How It Works:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("• Creates 64x64 sprites with content positioned at edges/corners\n" +
            "• Uses OffsetTile class that applies transform offsets\n" +
            "• Works with standard 64x64 tilemaps\n" +
            "• Half tiles align to grid edges, quarters to corners\n" +
            "• L-shaped tiles provide 75% coverage (perfect for platforms)\n" +
            "• Triangle tiles provide 50% coverage (great for slopes)", MessageType.Info);
    }
    
    private void CreateVerticalHalves()
    {
        if (!ValidateSourceTexture()) return;
        
        PrepareTexture();
        EnsureOutputDirectory();
        
        // Create 64x64 textures with 32x64 content positioned at edges
        Texture2D leftSprite = Create64x64WithContentAt(32, 64, 0, 0); // Content at left edge
        Texture2D rightSprite = Create64x64WithContentAt(32, 64, 32, 0); // Content at right edge
        
        // Extract the 32x64 content and position it correctly
        Color[] leftPixels = sourceTexture.GetPixels(0, 0, 32, 64);
        Color[] rightPixels = sourceTexture.GetPixels(32, 0, 32, 64);
        
        // Set pixels in the 64x64 sprites at correct positions
        leftSprite.SetPixels(0, 0, 32, 64, leftPixels); // Left side
        rightSprite.SetPixels(32, 0, 32, 64, rightPixels); // Right side
        
        leftSprite.Apply();
        rightSprite.Apply();
        
        // Save sprites
        string leftPath = SaveTextureBasic(leftSprite, $"{outputFolder}/{sourceTexture.name}_left_edge.png");
        string rightPath = SaveTextureBasic(rightSprite, $"{outputFolder}/{sourceTexture.name}_right_edge.png");
        
        // Create offset tile assets
        if (createTileAssets)
        {
            CreateOffsetTileAsset(leftPath, "Left Edge", new Vector3(-0.243f, 0f, 0f));
            CreateOffsetTileAsset(rightPath, "Right Edge", new Vector3(0.243f, 0f, 0f));
        }
        
        RestoreTexture();
        Debug.Log($"✓ Created edge-aligned vertical halves: {sourceTexture.name}_left/right_edge.png");
    }
    
    private void CreateHorizontalHalves()
    {
        if (!ValidateSourceTexture()) return;
        
        PrepareTexture();
        EnsureOutputDirectory();
        
        // Create 64x64 textures with 64x32 content positioned at edges
        Texture2D topSprite = Create64x64WithContentAt(64, 32, 0, 32); // Content at top edge
        Texture2D bottomSprite = Create64x64WithContentAt(64, 32, 0, 0); // Content at bottom edge
        
        // Extract the 64x32 content and position it correctly
        Color[] topPixels = sourceTexture.GetPixels(0, 32, 64, 32);
        Color[] bottomPixels = sourceTexture.GetPixels(0, 0, 64, 32);
        
        // Set pixels in the 64x64 sprites at correct positions
        topSprite.SetPixels(0, 32, 64, 32, topPixels); // Top side
        bottomSprite.SetPixels(0, 0, 64, 32, bottomPixels); // Bottom side
        
        topSprite.Apply();
        bottomSprite.Apply();
        
        // Save sprites
        string topPath = SaveTextureBasic(topSprite, $"{outputFolder}/{sourceTexture.name}_top_edge.png");
        string bottomPath = SaveTextureBasic(bottomSprite, $"{outputFolder}/{sourceTexture.name}_bottom_edge.png");
        
        // Create offset tile assets
        if (createTileAssets)
        {
            CreateOffsetTileAsset(topPath, "Top Edge", new Vector3(0f, 0.243f, 0f));
            CreateOffsetTileAsset(bottomPath, "Bottom Edge", new Vector3(0f, -0.243f, 0f));
        }
        
        RestoreTexture();
        Debug.Log($"✓ Created edge-aligned horizontal halves: {sourceTexture.name}_top/bottom_edge.png");
    }
    
    private void CreateCornerQuarters()
    {
        if (!ValidateSourceTexture()) return;
        
        PrepareTexture();
        EnsureOutputDirectory();
        
        // Create 64x64 textures with 32x32 content positioned at corners
        Texture2D tlSprite = Create64x64WithContentAt(32, 32, 0, 32);   // Top-left corner
        Texture2D trSprite = Create64x64WithContentAt(32, 32, 32, 32);  // Top-right corner
        Texture2D blSprite = Create64x64WithContentAt(32, 32, 0, 0);    // Bottom-left corner
        Texture2D brSprite = Create64x64WithContentAt(32, 32, 32, 0);   // Bottom-right corner
        
        // Extract the 32x32 content from each corner
        Color[] tlPixels = sourceTexture.GetPixels(0, 32, 32, 32);
        Color[] trPixels = sourceTexture.GetPixels(32, 32, 32, 32);
        Color[] blPixels = sourceTexture.GetPixels(0, 0, 32, 32);
        Color[] brPixels = sourceTexture.GetPixels(32, 0, 32, 32);
        
        // Set pixels in the 64x64 sprites at corner positions
        tlSprite.SetPixels(0, 32, 32, 32, tlPixels);   // Top-left
        trSprite.SetPixels(32, 32, 32, 32, trPixels);  // Top-right
        blSprite.SetPixels(0, 0, 32, 32, blPixels);    // Bottom-left
        brSprite.SetPixels(32, 0, 32, 32, brPixels);   // Bottom-right
        
        tlSprite.Apply();
        trSprite.Apply();
        blSprite.Apply();
        brSprite.Apply();
        
        // Save sprites (using default SaveTextureBasic like working methods)
        string tlPath = SaveTextureBasic(tlSprite, $"{outputFolder}/{sourceTexture.name}_tl_corner.png");
        string trPath = SaveTextureBasic(trSprite, $"{outputFolder}/{sourceTexture.name}_tr_corner.png");
        string blPath = SaveTextureBasic(blSprite, $"{outputFolder}/{sourceTexture.name}_bl_corner.png");
        string brPath = SaveTextureBasic(brSprite, $"{outputFolder}/{sourceTexture.name}_br_corner.png");
        
        // Create offset tile assets - fine-tuned offsets for precise grid alignment
        if (createTileAssets)
        {
            CreateOffsetTileAsset(tlPath, "Top-Left Corner", new Vector3(-0.243f, 0.243f, 0f));
            CreateOffsetTileAsset(trPath, "Top-Right Corner", new Vector3(0.243f, 0.243f, 0f));
            CreateOffsetTileAsset(blPath, "Bottom-Left Corner", new Vector3(-0.243f, -0.243f, 0f));
            CreateOffsetTileAsset(brPath, "Bottom-Right Corner", new Vector3(0.243f, -0.243f, 0f));
        }
        
        RestoreTexture();
        Debug.Log($"✓ Created corner-aligned quarters: {sourceTexture.name}_tl/tr/bl/br_corner.png");
    }
    
    private void CreateLShapedTiles()
    {
        if (!ValidateSourceTexture()) return;
        
        PrepareTexture();
        EnsureOutputDirectory();
        
        // Create L-shaped tiles (75% coverage) - each missing one corner
        Texture2D missingTL = Create64x64LShape("tl"); // Missing top-left corner
        Texture2D missingTR = Create64x64LShape("tr"); // Missing top-right corner
        Texture2D missingBL = Create64x64LShape("bl"); // Missing bottom-left corner
        Texture2D missingBR = Create64x64LShape("br"); // Missing bottom-right corner
        
        // Fill each L-shape with the source content, leaving one corner transparent
        FillLShapeFromSource(missingTL, "tl");
        FillLShapeFromSource(missingTR, "tr");
        FillLShapeFromSource(missingBL, "bl");
        FillLShapeFromSource(missingBR, "br");
        
        missingTL.Apply();
        missingTR.Apply();
        missingBL.Apply();
        missingBR.Apply();
        
        // Save sprites
        string missingTLPath = SaveTextureBasic(missingTL, $"{outputFolder}/{sourceTexture.name}_L_missing_tl.png");
        string missingTRPath = SaveTextureBasic(missingTR, $"{outputFolder}/{sourceTexture.name}_L_missing_tr.png");
        string missingBLPath = SaveTextureBasic(missingBL, $"{outputFolder}/{sourceTexture.name}_L_missing_bl.png");
        string missingBRPath = SaveTextureBasic(missingBR, $"{outputFolder}/{sourceTexture.name}_L_missing_br.png");
        
        // Create offset tile assets (L-shapes don't need offsets as they fill most of the grid cell)
        if (createTileAssets)
        {
            CreateOffsetTileAsset(missingTLPath, "L-Shape Missing TL", Vector3.zero);
            CreateOffsetTileAsset(missingTRPath, "L-Shape Missing TR", Vector3.zero);
            CreateOffsetTileAsset(missingBLPath, "L-Shape Missing BL", Vector3.zero);
            CreateOffsetTileAsset(missingBRPath, "L-Shape Missing BR", Vector3.zero);
        }
        
        RestoreTexture();
        Debug.Log($"✓ Created L-shaped tiles (75% coverage): {sourceTexture.name}_L_missing_[corner].png");
    }
    
    private void CreateTriangleTiles()
    {
        if (!ValidateSourceTexture()) return;
        
        PrepareTexture();
        EnsureOutputDirectory();
        
        // Create triangle tiles (50% coverage) - CORRECTED vertex-based triangles
        Texture2D triangleTL = Create64x64Triangle("tl"); // TL: top-left, top-right, bottom-left vertices
        Texture2D triangleTR = Create64x64Triangle("tr"); // TR: top-right, top-left, bottom-right vertices
        Texture2D triangleBL = Create64x64Triangle("bl"); // BL: bottom-left, bottom-right, top-left vertices
        Texture2D triangleBR = Create64x64Triangle("br"); // BR: bottom-right, bottom-left, top-right vertices
        
        // Fill each triangle with the source content
        FillTriangleFromSource(triangleTL, "tl");
        FillTriangleFromSource(triangleTR, "tr");
        FillTriangleFromSource(triangleBL, "bl");
        FillTriangleFromSource(triangleBR, "br");
        
        triangleTL.Apply();
        triangleTR.Apply();
        triangleBL.Apply();
        triangleBR.Apply();
        
        // Save sprites - FIXED MAPPING: each triangle saves with correct filename
        string triangleTLPath = SaveTextureBasic(triangleTL, $"{outputFolder}/{sourceTexture.name}_triangle_tl.png");  // tl texture → tl filename
        string triangleTRPath = SaveTextureBasic(triangleTR, $"{outputFolder}/{sourceTexture.name}_triangle_tr.png");  // tr texture → tr filename
        string triangleBLPath = SaveTextureBasic(triangleBL, $"{outputFolder}/{sourceTexture.name}_triangle_bl.png");  // bl texture → bl filename
        string triangleBRPath = SaveTextureBasic(triangleBR, $"{outputFolder}/{sourceTexture.name}_triangle_br.png");  // br texture → br filename
        
        // Create offset tile assets - LABELS match the correct filenames
        if (createTileAssets)
        {
            CreateOffsetTileAsset(triangleTLPath, "Triangle Top-Left", Vector3.zero);     // tl texture → tl asset
            CreateOffsetTileAsset(triangleTRPath, "Triangle Top-Right", Vector3.zero);    // tr texture → tr asset
            CreateOffsetTileAsset(triangleBLPath, "Triangle Bottom-Left", Vector3.zero);  // bl texture → bl asset
            CreateOffsetTileAsset(triangleBRPath, "Triangle Bottom-Right", Vector3.zero); // br texture → br asset
        }
        
        RestoreTexture();
        Debug.Log($"✓ Created triangle tiles (50% coverage): {sourceTexture.name}_triangle_[corner].png");
    }
    
    private Texture2D Create64x64Triangle(string triangleType)
    {
        Texture2D texture = new Texture2D(64, 64);
        
        // Fill with transparent pixels initially
        Color[] clearPixels = new Color[64 * 64];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.clear;
        }
        texture.SetPixels(clearPixels);
        
        return texture;
    }
    
    private void FillTriangleFromSource(Texture2D triangleTexture, string triangleType)
    {
        // Get all pixels from source
        Color[] sourcePixels = sourceTexture.GetPixels();
        
        // Create triangle by copying source pixels based on CORRECTED vertex requirements
        Color[] trianglePixels = new Color[64 * 64];
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                int index = y * 64 + x;
                bool isInTriangle = false;
                
                // SLICE-BASED triangle logic - precise diagonal cuts:
                // tl: slice from bottom-left to top-right, keep TOP half
                // tr: slice from top-left to bottom-right, keep TOP half  
                // bl: slice from top-left to bottom-right, keep BOTTOM half
                // br: slice from bottom-left to top-right, keep BOTTOM half
                switch (triangleType)
                {
                    case "tl": // Slice from bottom-left(0,0) to top-right(63,63), keep TOP half
                        isInTriangle = y >= x; // Above/on diagonal from bottom-left to top-right
                        break;
                    case "tr": // Slice from top-left(0,63) to bottom-right(63,0), keep TOP half
                        isInTriangle = y >= (63 - x); // Above/on diagonal from top-left to bottom-right
                        break;
                    case "bl": // Slice from top-left(0,63) to bottom-right(63,0), keep BOTTOM half
                        isInTriangle = y <= (63 - x); // Below/on diagonal from top-left to bottom-right
                        break;
                    case "br": // Slice from bottom-left(0,0) to top-right(63,63), keep BOTTOM half
                        isInTriangle = y <= x; // Below/on diagonal from bottom-left to top-right
                        break;
                }
                
                if (isInTriangle)
                {
                    trianglePixels[index] = sourcePixels[index]; // Copy from source
                }
                else
                {
                    trianglePixels[index] = Color.clear; // Transparent outside triangle
                }
            }
        }
        
        triangleTexture.SetPixels(trianglePixels);
    }
    
    private Texture2D Create64x64LShape(string missingCorner)
    {
        Texture2D texture = new Texture2D(64, 64);
        
        // Fill with transparent pixels initially
        Color[] clearPixels = new Color[64 * 64];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.clear;
        }
        texture.SetPixels(clearPixels);
        
        return texture;
    }
    
    private void FillLShapeFromSource(Texture2D lShapeTexture, string missingCorner)
    {
        // Get all pixels from source
        Color[] sourcePixels = sourceTexture.GetPixels();
        
        // Create L-shape by copying source pixels except for the missing corner
        Color[] lShapePixels = new Color[64 * 64];
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                int index = y * 64 + x;
                bool isInMissingCorner = false;
                
                // Determine if this pixel is in the missing corner (32x32 area)
                switch (missingCorner)
                {
                    case "tl": // Missing top-left corner
                        isInMissingCorner = x < 32 && y >= 32;
                        break;
                    case "tr": // Missing top-right corner
                        isInMissingCorner = x >= 32 && y >= 32;
                        break;
                    case "bl": // Missing bottom-left corner
                        isInMissingCorner = x < 32 && y < 32;
                        break;
                    case "br": // Missing bottom-right corner
                        isInMissingCorner = x >= 32 && y < 32;
                        break;
                }
                
                if (isInMissingCorner)
                {
                    lShapePixels[index] = Color.clear; // Transparent for missing corner
                }
                else
                {
                    lShapePixels[index] = sourcePixels[index]; // Copy from source
                }
            }
        }
        
        lShapeTexture.SetPixels(lShapePixels);
    }
    
    private void CreateAllVariations()
    {
        Debug.Log($"Creating all grid-aligned tile variations for: {sourceTexture.name}");
        CreateVerticalHalves();
        CreateHorizontalHalves();
        CreateCornerQuarters();
        CreateLShapedTiles();
        CreateTriangleTiles();
        Debug.Log($"✓ All grid-aligned tile variations created successfully!");
    }
    
    private Texture2D Create64x64WithContentAt(int contentWidth, int contentHeight, int offsetX, int offsetY)
    {
        Texture2D texture = new Texture2D(64, 64);
        
        // Fill with transparent pixels
        Color[] clearPixels = new Color[64 * 64];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.clear;
        }
        texture.SetPixels(clearPixels);
        
        return texture;
    }
    
    // Helper methods
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
    
    private string SaveTextureBasic(Texture2D texture, string path, int spriteSize = 64)
    {
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        
        // Import with sprite settings - SMART GRID ALIGNMENT
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            // CRITICAL FIX: Match pixels per unit to actual sprite content size for perfect grid alignment
            importer.spritePixelsToUnits = spriteSize; // 32 for 32x32 sprites, 64 for 64x64 sprites
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            // CRITICAL FIX: Set extrude edges to 0 to prevent 33x33 tiles from 32x32 content
            importer.spriteBorder = Vector4.zero;
            AssetDatabase.ImportAsset(path);
        }
        
        return path;
    }
    
    /// <summary>
    /// Generate precise collision points for custom tiles (L-shapes and corners) to prevent unwanted physics
    /// </summary>
    private Vector2[] GenerateCustomCollisionPoints(string spriteName)
    {
        string name = spriteName.ToLower();
        
        if (name.Contains("_l_missing_tl"))
        {
            // Missing top-left: ┐ shape
            // Define exact outline to prevent diagonal fill
            return new Vector2[]
            {
                new Vector2(0.5f, 0.5f),   // Top-right corner
                new Vector2(0.5f, -0.5f),  // Bottom-right corner  
                new Vector2(-0.5f, -0.5f), // Bottom-left corner
                new Vector2(-0.5f, 0f),    // Left middle (where missing corner starts)
                new Vector2(0f, 0f),       // Inner corner point
                new Vector2(0f, 0.5f)      // Top middle (complete the L)
            };
        }
        else if (name.Contains("_l_missing_tr"))
        {
            // Missing top-right: ┌ shape
            return new Vector2[]
            {
                new Vector2(-0.5f, 0.5f),  // Top-left corner
                new Vector2(0f, 0.5f),     // Top middle (where missing corner starts)
                new Vector2(0f, 0f),       // Inner corner point
                new Vector2(0.5f, 0f),     // Right middle
                new Vector2(0.5f, -0.5f),  // Bottom-right corner
                new Vector2(-0.5f, -0.5f)  // Bottom-left corner
            };
        }
        else if (name.Contains("_l_missing_bl"))
        {
            // Missing bottom-left: ┘ shape
            return new Vector2[]
            {
                new Vector2(-0.5f, 0.5f),  // Top-left corner
                new Vector2(0.5f, 0.5f),   // Top-right corner
                new Vector2(0.5f, -0.5f),  // Bottom-right corner
                new Vector2(0f, -0.5f),    // Bottom middle (where missing corner starts)
                new Vector2(0f, 0f),       // Inner corner point
                new Vector2(-0.5f, 0f)     // Left middle (complete the L)
            };
        }
        else if (name.Contains("_l_missing_br"))
        {
            // Missing bottom-right: └ shape
            return new Vector2[]
            {
                new Vector2(-0.5f, 0.5f),  // Top-left corner
                new Vector2(0.5f, 0.5f),   // Top-right corner
                new Vector2(0.5f, 0f),     // Right middle (where missing corner starts)
                new Vector2(0f, 0f),       // Inner corner point
                new Vector2(0f, -0.5f),    // Bottom middle
                new Vector2(-0.5f, -0.5f)  // Bottom-left corner
            };
        }
        
        // Handle corner tiles - precise 32x32 collision bounds within 64x64 sprite
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
        
        // Handle triangular tiles - EXACT MATCH TO SPRITE VISUALS
        else if (name.Contains("_triangle_tl"))  
        {
            // TL: Grey triangle in top-left corner (90° at top-left)
            return new Vector2[]
            {
                new Vector2(-0.5f, 0.5f),   // Top-left corner (90° angle)
                new Vector2(-0.5f, -0.5f),  // Bottom-left corner
                new Vector2(0.5f, 0.5f)     // Top-right corner
            };
        }
        else if (name.Contains("_triangle_tr"))
        {
            // TR: Grey triangle in top-right corner (90° at top-right)
            return new Vector2[]
            {
                new Vector2(0.5f, 0.5f),    // Top-right corner (90° angle)
                new Vector2(-0.5f, 0.5f),   // Top-left corner
                new Vector2(0.5f, -0.5f)    // Bottom-right corner
            };
        }
        else if (name.Contains("_triangle_bl"))
        {
            // BL: Grey triangle in bottom-left corner (90° at bottom-left)
            return new Vector2[]
            {
                new Vector2(-0.5f, -0.5f),  // Bottom-left corner (90° angle)
                new Vector2(-0.5f, 0.5f),   // Top-left corner
                new Vector2(0.5f, -0.5f)    // Bottom-right corner
            };
        }
        else if (name.Contains("_triangle_br"))
        {
            // BR: Grey triangle in bottom-right corner (90° at bottom-right)
            return new Vector2[]
            {
                new Vector2(0.5f, -0.5f),   // Bottom-right corner (90° angle)
                new Vector2(-0.5f, -0.5f),  // Bottom-left corner
                new Vector2(0.5f, 0.5f)     // Top-right corner
            };
        }
        
        // Return null for unknown tile types (use sprite collision)
        return null;
    }
    
    private void CreateOffsetTileAsset(string spritePath, string tileName, Vector3 offset)
    {
        // Load the sprite
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogError($"Failed to load sprite at {spritePath}");
            return;
        }
        
        // Create offset tile asset
        OffsetTile tile = ScriptableObject.CreateInstance<OffsetTile>();
        tile.sprite = sprite;
        tile.transform = Matrix4x4.Translate(offset);
        
        // Generate custom collision for L-shapes, corners, and triangles to prevent unwanted physics
        Vector2[] customCollision = GenerateCustomCollisionPoints(sprite.name);
        if (customCollision != null)
        {
            tile.SetCustomCollisionPoints(customCollision);
            Debug.Log($"✓ Set custom collision for {sprite.name}: {customCollision.Length} points");
        }
        else
        {
            Debug.Log($"⚠ No custom collision set for {sprite.name} - will use sprite collision");
        }
        
        // Save tile asset to Assets/Tiles folder
        string tileFolder = "Assets/Tiles";
        if (!AssetDatabase.IsValidFolder(tileFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Tiles");
        }
        
        string tilePath = $"{tileFolder}/{sprite.name}.asset";
        
        // Check if asset already exists and delete it to prevent corruption
        if (AssetDatabase.LoadAssetAtPath<OffsetTile>(tilePath) != null)
        {
            AssetDatabase.DeleteAsset(tilePath);
            AssetDatabase.Refresh();
        }
        
        AssetDatabase.CreateAsset(tile, tilePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✓ Created offset tile asset with offset {offset}: {System.IO.Path.GetFileName(tilePath)}");
    }
}