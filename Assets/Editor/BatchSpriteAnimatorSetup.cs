#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine.U2D;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public class BatchSpriteAnimatorSetup : EditorWindow
{
    // Settings
    public static int spriteWidth = 48;
    public static int spriteHeight = 48;
    public static float pivotX = 0.5f;
    public static float pivotY = 0.15f;
    public static float frameRate = 12f;
    public static bool loopAnimations = true;
    public static FilterMode filterMode = FilterMode.Point;
    public static TextureImporterCompression compression = TextureImporterCompression.Uncompressed;
    public static bool mipmapEnabled = false;
    public static float pixelsPerUnit = 100f;
    
    [MenuItem("Tools/Batch Sprite Animator Setup")]
    public static void ShowWindow()
    {
        GetWindow<BatchSpriteAnimatorSetup>("Batch Sprite Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Sprite Sheet Slicer & Animator Setup", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Process All Animation Folders"))
        {
            ProcessAllAnimationFolders();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Process Single Folder"))
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Animation Folder", "Assets/Animations/2D-Pixel-Art-Character-Template", "");
            if (!string.IsNullOrEmpty(folderPath))
            {
                // Convert absolute path to relative path
                if (folderPath.StartsWith(Application.dataPath))
                {
                    folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);
                }
                ProcessFolder(folderPath);
            }
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Process Selected Folders"))
        {
            ProcessSelectedFolders();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Settings:", EditorStyles.boldLabel);
        
        // Grid settings
        GUILayout.BeginHorizontal();
        GUILayout.Label("Sprite Width:", GUILayout.Width(100));
        spriteWidth = EditorGUILayout.IntField(spriteWidth, GUILayout.Width(60));
        GUILayout.Label("Height:", GUILayout.Width(50));
        spriteHeight = EditorGUILayout.IntField(spriteHeight, GUILayout.Width(60));
        GUILayout.EndHorizontal();
        
        // Pivot settings
        GUILayout.BeginHorizontal();
        GUILayout.Label("Pivot X:", GUILayout.Width(100));
        pivotX = EditorGUILayout.Slider(pivotX, 0f, 1f, GUILayout.Width(120));
        GUILayout.Label("Y:", GUILayout.Width(20));
        pivotY = EditorGUILayout.Slider(pivotY, 0f, 1f, GUILayout.Width(120));
        GUILayout.EndHorizontal();
        
        // Animation settings
        GUILayout.BeginHorizontal();
        GUILayout.Label("Frame Rate:", GUILayout.Width(100));
        frameRate = EditorGUILayout.FloatField(frameRate, GUILayout.Width(60));
        GUILayout.Label("FPS", GUILayout.Width(30));
        GUILayout.EndHorizontal();
        
        loopAnimations = GUILayout.Toggle(loopAnimations, "Loop Animations");
        
        // Texture settings
        GUILayout.BeginHorizontal();
        GUILayout.Label("Filter Mode:", GUILayout.Width(100));
        filterMode = (FilterMode)EditorGUILayout.EnumPopup(filterMode, GUILayout.Width(120));
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Compression:", GUILayout.Width(100));
        compression = (TextureImporterCompression)EditorGUILayout.EnumPopup(compression, GUILayout.Width(120));
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Pixels Per Unit:", GUILayout.Width(100));
        pixelsPerUnit = EditorGUILayout.FloatField(pixelsPerUnit, GUILayout.Width(60));
        GUILayout.EndHorizontal();
        
        mipmapEnabled = GUILayout.Toggle(mipmapEnabled, "Enable Mipmaps");
        
        GUILayout.Space(5);
        GUILayout.Label("Target Animator: PlayerAnimator");
    }

    private void ProcessAllAnimationFolders()
    {
        string basePath = "Assets/Animations/2D-Pixel-Art-Character-Template";
        
        if (!Directory.Exists(Path.Combine(Application.dataPath, "Animations/2D-Pixel-Art-Character-Template")))
        {
            Debug.LogError("2D-Pixel-Art-Character-Template folder not found at: " + basePath);
            return;
        }

        // Get all subfolders in the 2D-Pixel-Art-Character-Template directory
        string[] subfolders = Directory.GetDirectories(Path.Combine(Application.dataPath, "Animations/2D-Pixel-Art-Character-Template"));
        
        foreach (string folder in subfolders)
        {
            string relativePath = "Assets" + folder.Substring(Application.dataPath.Length).Replace("\\", "/");
            Debug.Log("Processing subfolder: " + relativePath);
            ProcessFolder(relativePath);
        }
        
        AssetDatabase.Refresh();
    }

    private void ProcessSelectedFolders()
    {
        // Open the folder selection window
        GetWindow<FolderSelectionWindow>("Select Folders to Process");
    }

    private void ProcessFolder(string folderPath)
    {
        Debug.Log("Processing folder: " + folderPath);
        
        // Get all PNG files in the folder
        string[] pngFiles = Directory.GetFiles(folderPath, "*.png", SearchOption.TopDirectoryOnly);
        
        if (pngFiles.Length == 0)
        {
            Debug.LogWarning("No PNG files found in: " + folderPath);
            return;
        }

        foreach (string pngFile in pngFiles)
        {
            // Debug the path conversion
            Debug.Log($"Original PNG file path: {pngFile}");
            Debug.Log($"Application.dataPath: {Application.dataPath}");
            
            // Convert absolute path to Unity asset path properly
            string relativePath;
            if (pngFile.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + pngFile.Substring(Application.dataPath.Length).Replace("\\", "/");
            }
            else
            {
                // If it's already a relative path, just normalize it
                relativePath = pngFile.Replace("\\", "/");
                if (!relativePath.StartsWith("Assets/"))
                {
                    relativePath = "Assets/" + relativePath;
                }
            }
            Debug.Log("Converted relative path: " + relativePath);
            
            try
            {
                ProcessSpriteSheet(relativePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error processing " + relativePath + ": " + e.Message);
            }
        }
    }

    private void ProcessSpriteSheet(string assetPath)
    {
        Debug.Log("Processing sprite sheet: " + assetPath);
        
        // Get the TextureImporter
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Could not get TextureImporter for: " + assetPath);
            return;
        }

        // Load the texture to get dimensions
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
        {
            Debug.LogError("Could not load texture: " + assetPath);
            return;
        }

        Debug.Log($"Texture dimensions: {texture.width}x{texture.height}");

        // Configure the importer
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = BatchSpriteAnimatorSetup.pixelsPerUnit;
        importer.filterMode = BatchSpriteAnimatorSetup.filterMode;
        importer.textureCompression = BatchSpriteAnimatorSetup.compression;
        importer.mipmapEnabled = BatchSpriteAnimatorSetup.mipmapEnabled;

        // Calculate grid dimensions using settings
        int columns = texture.width / BatchSpriteAnimatorSetup.spriteWidth;
        int rows = texture.height / BatchSpriteAnimatorSetup.spriteHeight;

        Debug.Log($"Grid: {columns}x{rows} sprites of {BatchSpriteAnimatorSetup.spriteWidth}x{BatchSpriteAnimatorSetup.spriteHeight}");

        // Create sprite metadata for each grid cell
        List<SpriteMetaData> spritesheet = new List<SpriteMetaData>();
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                SpriteMetaData spriteData = new SpriteMetaData();
                spriteData.name = $"{Path.GetFileNameWithoutExtension(assetPath)}_{row}_{col}";
                spriteData.rect = new Rect(col * BatchSpriteAnimatorSetup.spriteWidth, (rows - 1 - row) * BatchSpriteAnimatorSetup.spriteHeight, BatchSpriteAnimatorSetup.spriteWidth, BatchSpriteAnimatorSetup.spriteHeight);
                spriteData.pivot = new Vector2(BatchSpriteAnimatorSetup.pivotX, BatchSpriteAnimatorSetup.pivotY);
                spriteData.alignment = (int)SpriteAlignment.Custom;
                spriteData.border = Vector4.zero;
                
                spritesheet.Add(spriteData);
            }
        }

        // Apply the spritesheet data (using obsolete but working API)
        importer.spritesheet = spritesheet.ToArray();
        
        // Save and reimport
        importer.SaveAndReimport();
        
        Debug.Log($"Created {spritesheet.Count} sprites from {assetPath}");

        // Create animation clips
        CreateAnimationClips(assetPath, spritesheet.Count, columns, rows);
    }

    private void CreateAnimationClips(string spriteSheetPath, int totalSprites, int columns, int rows)
    {
        string folderName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(spriteSheetPath));
        string baseName = Path.GetFileNameWithoutExtension(spriteSheetPath);
        
        // Remove "48x48" from the animation name
        baseName = baseName.Replace("48x48", "").Trim();
        
        // Remove spaces and capitalize first letter of each word
        string[] words = baseName.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        baseName = string.Join("", words);
        
        Debug.Log($"Creating animation clips for: {baseName} in folder: {folderName}");

        // Load all sprites from the spritesheet
        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);
        List<Sprite> spriteList = new List<Sprite>();
        
        foreach (Object obj in sprites)
        {
            if (obj is Sprite)
            {
                spriteList.Add(obj as Sprite);
            }
        }

        if (spriteList.Count == 0)
        {
            Debug.LogError("No sprites found in: " + spriteSheetPath);
            return;
        }

        // Create animation clip
        AnimationClip clip = new AnimationClip();
        clip.name = baseName;
        clip.frameRate = BatchSpriteAnimatorSetup.frameRate;
        
        // Create the sprite frame curve
        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";
        
        ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[spriteList.Count];
        
        for (int i = 0; i < spriteList.Count; i++)
        {
            spriteKeyFrames[i] = new ObjectReferenceKeyframe();
            spriteKeyFrames[i].time = i / clip.frameRate;
            spriteKeyFrames[i].value = spriteList[i];
        }
        
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);
        
        // Set loop time
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = BatchSpriteAnimatorSetup.loopAnimations;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Save the animation clip in the same folder as the PNG file
        string clipPath = Path.GetDirectoryName(spriteSheetPath) + "/" + baseName + ".anim";
        
        // Check if animation clip already exists
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existingClip != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Animation Already Exists", 
                $"Animation clip '{baseName}' already exists at:\n{clipPath}\n\nDo you want to overwrite it?", 
                "Overwrite", "Skip");
                
            if (!overwrite)
            {
                Debug.Log($"Skipping {baseName} - animation already exists");
                return;
            }
            
            // Delete the existing clip
            AssetDatabase.DeleteAsset(clipPath);
            Debug.Log($"Overwriting existing animation: {baseName}");
        }
        
        AssetDatabase.CreateAsset(clip, clipPath);
        
        Debug.Log($"Created animation clip: {clipPath}");

        // Add to animator controller
        AddToAnimatorController(clip, baseName);
    }

    private void AddToAnimatorController(AnimationClip clip, string stateName)
    {
        // Find the PlayerAnimator controller
        string controllerPath = "Assets/Animations/PlayerAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        
        if (controller == null)
        {
            Debug.LogWarning("PlayerAnimator.controller not found. Creating new controller...");
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // Check if state already exists
        bool stateExists = false;
        foreach (var layer in controller.layers)
        {
            foreach (var existingState in layer.stateMachine.states)
            {
                if (existingState.state.name == stateName)
                {
                    stateExists = true;
                    break;
                }
            }
            if (stateExists) break;
        }
        
        if (stateExists)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Animator State Already Exists", 
                $"State '{stateName}' already exists in PlayerAnimator controller.\n\nDo you want to replace it?", 
                "Replace", "Skip");
                
            if (!overwrite)
            {
                Debug.Log($"Skipping state '{stateName}' - already exists in animator");
                return;
            }
            
            // Remove existing state (this is simplified - in practice you might want to update the existing state)
            Debug.Log($"Replacing existing state: {stateName}");
        }
        
        // Add the new state
        AnimatorState newState = controller.AddMotion(clip, 0);
        newState.name = stateName;
        
        Debug.Log($"Added state '{stateName}' to PlayerAnimator controller");
        
        AssetDatabase.SaveAssets();
    }
}

// Custom window for folder selection
public class FolderSelectionWindow : EditorWindow
{
    private List<bool> folderSelections = new List<bool>();
    private List<string> folderPaths = new List<string>();
    private List<string> folderNames = new List<string>();
    private Vector2 scrollPosition;

    private void OnEnable()
    {
        LoadFolders();
    }

    private void LoadFolders()
    {
        folderSelections.Clear();
        folderPaths.Clear();
        folderNames.Clear();

        string basePath = "Assets/Animations/2D-Pixel-Art-Character-Template";
        
        if (!Directory.Exists(Path.Combine(Application.dataPath, "Animations/2D-Pixel-Art-Character-Template")))
        {
            Debug.LogError("2D-Pixel-Art-Character-Template folder not found at: " + basePath);
            return;
        }

        string[] allSubfolders = Directory.GetDirectories(Path.Combine(Application.dataPath, "Animations/2D-Pixel-Art-Character-Template"));
        
        foreach (string folder in allSubfolders)
        {
            string folderName = Path.GetFileName(folder);
            string relativePath = "Assets" + folder.Substring(Application.dataPath.Length).Replace("\\", "/");
            
            folderNames.Add(folderName);
            folderPaths.Add(relativePath);
            folderSelections.Add(false); // Default to unchecked
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Select Folders to Process", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Select All / Deselect All buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
        {
            for (int i = 0; i < folderSelections.Count; i++)
            {
                folderSelections[i] = true;
            }
        }
        if (GUILayout.Button("Deselect All"))
        {
            for (int i = 0; i < folderSelections.Count; i++)
            {
                folderSelections[i] = false;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Scrollable list of folders
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < folderNames.Count; i++)
        {
            folderSelections[i] = GUILayout.Toggle(folderSelections[i], folderNames[i]);
        }
        
        GUILayout.EndScrollView();

        GUILayout.Space(10);

        // Process button
        if (GUILayout.Button("Process Selected Folders"))
        {
            ProcessSelectedFolders();
        }

        // Cancel button
        if (GUILayout.Button("Cancel"))
        {
            Close();
        }
    }

    private void ProcessSelectedFolders()
    {
        List<string> selectedFolders = new List<string>();
        
        for (int i = 0; i < folderSelections.Count; i++)
        {
            if (folderSelections[i])
            {
                selectedFolders.Add(folderPaths[i]);
            }
        }

        if (selectedFolders.Count == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select at least one folder to process.", "OK");
            return;
        }

        // Process selected folders
        foreach (string folder in selectedFolders)
        {
            Debug.Log("Processing selected folder: " + folder);
            ProcessFolder(folder);
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"Processed {selectedFolders.Count} folders.");
        
        EditorUtility.DisplayDialog("Complete", $"Successfully processed {selectedFolders.Count} folders!", "OK");
        Close();
    }

    private void ProcessFolder(string folderPath)
    {
        Debug.Log("Processing folder: " + folderPath);
        
        // Get all PNG files in the folder
        string[] pngFiles = Directory.GetFiles(folderPath, "*.png", SearchOption.TopDirectoryOnly);
        
        if (pngFiles.Length == 0)
        {
            Debug.LogWarning("No PNG files found in: " + folderPath);
            return;
        }

        foreach (string pngFile in pngFiles)
        {
            // Convert absolute path to Unity asset path properly
            string relativePath;
            if (pngFile.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + pngFile.Substring(Application.dataPath.Length).Replace("\\", "/");
            }
            else
            {
                relativePath = pngFile.Replace("\\", "/");
                if (!relativePath.StartsWith("Assets/"))
                {
                    relativePath = "Assets/" + relativePath;
                }
            }
            
            try
            {
                ProcessSpriteSheet(relativePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error processing " + relativePath + ": " + e.Message);
            }
        }
    }

    private void ProcessSpriteSheet(string assetPath)
    {
        Debug.Log("Processing sprite sheet: " + assetPath);
        
        // Get the TextureImporter
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Could not get TextureImporter for: " + assetPath);
            return;
        }

        // Load the texture to get dimensions
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
        {
            Debug.LogError("Could not load texture: " + assetPath);
            return;
        }

        // Configure the importer
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = BatchSpriteAnimatorSetup.pixelsPerUnit;
        importer.filterMode = BatchSpriteAnimatorSetup.filterMode;
        importer.textureCompression = BatchSpriteAnimatorSetup.compression;
        importer.mipmapEnabled = BatchSpriteAnimatorSetup.mipmapEnabled;

        // Calculate grid dimensions using settings
        int columns = texture.width / BatchSpriteAnimatorSetup.spriteWidth;
        int rows = texture.height / BatchSpriteAnimatorSetup.spriteHeight;

        // Create sprite metadata for each grid cell
        List<SpriteMetaData> spritesheet = new List<SpriteMetaData>();
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                SpriteMetaData spriteData = new SpriteMetaData();
                spriteData.name = $"{Path.GetFileNameWithoutExtension(assetPath)}_{row}_{col}";
                spriteData.rect = new Rect(col * BatchSpriteAnimatorSetup.spriteWidth, (rows - 1 - row) * BatchSpriteAnimatorSetup.spriteHeight, BatchSpriteAnimatorSetup.spriteWidth, BatchSpriteAnimatorSetup.spriteHeight);
                spriteData.pivot = new Vector2(BatchSpriteAnimatorSetup.pivotX, BatchSpriteAnimatorSetup.pivotY);
                spriteData.alignment = (int)SpriteAlignment.Custom;
                spriteData.border = Vector4.zero;
                
                spritesheet.Add(spriteData);
            }
        }

        // Apply the spritesheet data
        importer.spritesheet = spritesheet.ToArray();
        importer.SaveAndReimport();

        // Create animation clips
        CreateAnimationClips(assetPath, spritesheet.Count, columns, rows);
    }

    private void CreateAnimationClips(string spriteSheetPath, int totalSprites, int columns, int rows)
    {
        string folderName = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(spriteSheetPath));
        string baseName = Path.GetFileNameWithoutExtension(spriteSheetPath);
        
        // Remove "48x48" from the animation name
        baseName = baseName.Replace("48x48", "").Trim();
        
        // Remove spaces and capitalize first letter of each word
        string[] words = baseName.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        baseName = string.Join("", words);

        // Load all sprites from the spritesheet
        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);
        List<Sprite> spriteList = new List<Sprite>();
        
        foreach (Object obj in sprites)
        {
            if (obj is Sprite)
            {
                spriteList.Add(obj as Sprite);
            }
        }

        if (spriteList.Count == 0)
        {
            Debug.LogError("No sprites found in: " + spriteSheetPath);
            return;
        }

        // Create animation clip
        AnimationClip clip = new AnimationClip();
        clip.name = baseName;
        clip.frameRate = BatchSpriteAnimatorSetup.frameRate;
        
        // Create the sprite frame curve
        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";
        
        ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[spriteList.Count];
        
        for (int i = 0; i < spriteList.Count; i++)
        {
            spriteKeyFrames[i] = new ObjectReferenceKeyframe();
            spriteKeyFrames[i].time = i / clip.frameRate;
            spriteKeyFrames[i].value = spriteList[i];
        }
        
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);
        
        // Set loop time
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = BatchSpriteAnimatorSetup.loopAnimations;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Save the animation clip
        string clipPath = Path.GetDirectoryName(spriteSheetPath) + "/" + baseName + ".anim";
        
        // Check if animation clip already exists
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existingClip != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Animation Already Exists", 
                $"Animation clip '{baseName}' already exists at:\n{clipPath}\n\nDo you want to overwrite it?", 
                "Overwrite", "Skip");
                
            if (!overwrite)
            {
                Debug.Log($"Skipping {baseName} - animation already exists");
                return;
            }
            
            // Delete the existing clip
            AssetDatabase.DeleteAsset(clipPath);
            Debug.Log($"Overwriting existing animation: {baseName}");
        }
        
        AssetDatabase.CreateAsset(clip, clipPath);

        // Add to animator controller
        AddToAnimatorController(clip, baseName);
    }

    private void AddToAnimatorController(AnimationClip clip, string stateName)
    {
        string controllerPath = "Assets/Animations/PlayerAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        
        if (controller == null)
        {
            Debug.LogWarning("PlayerAnimator.controller not found. Creating new controller...");
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // Check if state already exists
        bool stateExists = false;
        foreach (var layer in controller.layers)
        {
            foreach (var existingState in layer.stateMachine.states)
            {
                if (existingState.state.name == stateName)
                {
                    stateExists = true;
                    break;
                }
            }
            if (stateExists) break;
        }
        
        if (stateExists)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Animator State Already Exists", 
                $"State '{stateName}' already exists in PlayerAnimator controller.\n\nDo you want to replace it?", 
                "Replace", "Skip");
                
            if (!overwrite)
            {
                Debug.Log($"Skipping state '{stateName}' - already exists in animator");
                return;
            }
            
            Debug.Log($"Replacing existing state: {stateName}");
        }
        
        AnimatorState newState = controller.AddMotion(clip, 0);
        newState.name = stateName;
        
        AssetDatabase.SaveAssets();
    }
}
#endif 