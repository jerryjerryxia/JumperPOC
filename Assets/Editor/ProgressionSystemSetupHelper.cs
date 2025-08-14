using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor helper to quickly set up the progression system in the scene.
/// Adds PlayerAbilities component and creates debug UI for testing.
/// </summary>
public class ProgressionSystemSetupHelper : EditorWindow
{
    [MenuItem("Tools/Setup Progression System")]
    public static void ShowWindow()
    {
        GetWindow<ProgressionSystemSetupHelper>("Progression System Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Progression System Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("This tool helps you set up the Metroidvania progression system:");
        GUILayout.Label("• Adds PlayerAbilities component to player");
        GUILayout.Label("• Creates debug UI for testing abilities");
        GUILayout.Label("• Integrates with existing PlayerController");
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("1. Setup Player Abilities", GUILayout.Height(30)))
        {
            SetupPlayerAbilities();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("2. Create Ability Debug UI (Disabled)", GUILayout.Height(30)))
        {
            EditorUtility.DisplayDialog("Feature Disabled", 
                "The AbilityDebugUI system has been removed from the project.\n\n" +
                "This feature is no longer available.", 
                "OK");
        }
        
        GUILayout.Space(20);
        
        EditorGUILayout.HelpBox(
            "Instructions:\n" +
            "1. Click 'Setup Player Abilities' to add the progression system to your player\n" +
            "2. The AbilityDebugUI system has been removed from the project\n" +
            "3. Use the PlayerAbilities component directly for ability management\n" +
            "4. Test abilities through code or inspector modifications",
            MessageType.Info);
    }
    
    private void SetupPlayerAbilities()
    {
        // Find player GameObject
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Try to find by name
            player = GameObject.Find("Player");
        }
        
        if (player == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "Could not find Player GameObject!\n\n" +
                "Make sure your player has the 'Player' tag or is named 'Player'.", 
                "OK");
            return;
        }
        
        // Check if PlayerAbilities already exists
        PlayerAbilities existingAbilities = player.GetComponent<PlayerAbilities>();
        if (existingAbilities != null)
        {
            EditorUtility.DisplayDialog("Already Setup", 
                "PlayerAbilities component already exists on the player!", 
                "OK");
            return;
        }
        
        // Add PlayerAbilities component
        PlayerAbilities abilities = player.AddComponent<PlayerAbilities>();
        
        // Mark as dirty for saving
        EditorUtility.SetDirty(player);
        
        Debug.Log($"[ProgressionSystemSetup] Added PlayerAbilities component to {player.name}");
        EditorUtility.DisplayDialog("Success", 
            $"PlayerAbilities component added to {player.name}!\n\n" +
            "The progression system is now active. All abilities start unlocked for testing.", 
            "OK");
    }
    
    private void CreateAbilityDebugUI()
    {
        // Check if Canvas exists
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Set to UI layer
            canvasObj.layer = LayerMask.NameToLayer("UI");
        }
        
        // AbilityDebugUI system has been removed - skip this check
        // Check if AbilityDebugUI already exists
        // AbilityDebugUI existingUI = FindFirstObjectByType<AbilityDebugUI>();
        // if (existingUI != null)
        // {
        //     EditorUtility.DisplayDialog("Already Setup", 
        //         "AbilityDebugUI already exists in the scene!", 
        //         "OK");
        //     Selection.activeGameObject = existingUI.gameObject;
        //     return;
        // }
        
        // Create main UI object
        GameObject uiObj = new GameObject("AbilityDebugUI");
        uiObj.transform.SetParent(canvas.transform, false);
        uiObj.layer = LayerMask.NameToLayer("UI");
        
        // AbilityDebugUI system has been removed - skip component creation
        // Add AbilityDebugUI component
        // AbilityDebugUI debugUI = uiObj.AddComponent<AbilityDebugUI>();
        
        // Create main panel
        GameObject panelObj = CreateUIPanel("AbilityPanel", uiObj.transform);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        
        // Position panel in top-right corner
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-10, -10);
        panelRect.sizeDelta = new Vector2(250, 400);
        
        // Add background
        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        // Create title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        titleObj.layer = LayerMask.NameToLayer("UI");
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "ABILITY DEBUG";
        titleText.fontSize = 16;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(0, 30);
        
        // Create scroll view for buttons
        GameObject scrollViewObj = CreateScrollView("ScrollView", panelObj.transform);
        RectTransform scrollRect = scrollViewObj.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(10, 10);
        scrollRect.offsetMax = new Vector2(-10, -50);
        
        // Get the content transform for buttons
        Transform content = scrollViewObj.transform.Find("Viewport/Content");
        
        // Create button prefab
        GameObject buttonPrefab = CreateButtonPrefab();
        
        // AbilityDebugUI system has been removed - skip reference setup
        // Setup references in AbilityDebugUI
        // var serializedObject = new SerializedObject(debugUI);
        // serializedObject.FindProperty("abilityPanel").objectReferenceValue = panelObj;
        // serializedObject.FindProperty("buttonContainer").objectReferenceValue = content;
        // serializedObject.FindProperty("buttonPrefab").objectReferenceValue = buttonPrefab;
        // serializedObject.ApplyModifiedProperties();
        
        // Mark as dirty
        EditorUtility.SetDirty(uiObj);
        
        Debug.Log("[ProgressionSystemSetup] Created UI placeholder (AbilityDebugUI system removed)");
        EditorUtility.DisplayDialog("Note", 
            "UI elements created but AbilityDebugUI component is not available.\n\n" +
            "The AbilityDebugUI system has been removed from the project.\n" +
            "This is a placeholder UI structure only.", 
            "OK");
        
        // Select the created object
        Selection.activeGameObject = uiObj;
    }
    
    private GameObject CreateUIPanel(string name, Transform parent)
    {
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);
        panelObj.layer = LayerMask.NameToLayer("UI");
        
        RectTransform rect = panelObj.AddComponent<RectTransform>();
        Image image = panelObj.AddComponent<Image>();
        
        return panelObj;
    }
    
    private GameObject CreateScrollView(string name, Transform parent)
    {
        GameObject scrollViewObj = new GameObject(name);
        scrollViewObj.transform.SetParent(parent, false);
        scrollViewObj.layer = LayerMask.NameToLayer("UI");
        
        ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        Image image = scrollViewObj.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        
        // Create Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);
        viewportObj.layer = LayerMask.NameToLayer("UI");
        
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        
        Image viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = Color.clear;
        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        // Create Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        contentObj.layer = LayerMask.NameToLayer("UI");
        
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 400);
        
        VerticalLayoutGroup layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 5;
        layoutGroup.padding = new RectOffset(5, 5, 5, 5);
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        
        ContentSizeFitter sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Setup ScrollRect
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        
        return scrollViewObj;
    }
    
    private GameObject CreateButtonPrefab()
    {
        GameObject buttonObj = new GameObject("ButtonPrefab");
        buttonObj.layer = LayerMask.NameToLayer("UI");
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 30);
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.3f, 0.5f, 1f);
        
        Button button = buttonObj.AddComponent<Button>();
        
        // Create text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        textObj.layer = LayerMask.NameToLayer("UI");
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Button Text";
        text.fontSize = 12;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        
        // Save as prefab asset
        string prefabPath = "Assets/Prefabs/UI/AbilityButtonPrefab.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(buttonObj, prefabPath);
        DestroyImmediate(buttonObj);
        
        return prefab;
    }
}