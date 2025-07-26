using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UI;
using Enemies;
using Player;

public class HealthUISetupHelper : EditorWindow
{
    [MenuItem("Tools/Setup Health UI")]
    public static void ShowWindow()
    {
        GetWindow<HealthUISetupHelper>("Health UI Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Health UI Setup Helper", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Setup Player Health UI"))
        {
            SetupPlayerHealthUI();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("Setup Enemy Health UI on Selected"))
        {
            SetupEnemyHealthUI();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("1. Click 'Setup Player Health UI' to create player health bar");
        GUILayout.Label("2. Select an enemy GameObject and click 'Setup Enemy Health UI'");
        GUILayout.Label("3. Health bars will be automatically configured");
    }
    
    private static void SetupPlayerHealthUI()
    {
        // Clear selection to avoid any interference
        Selection.activeGameObject = null;
        
        // Find or create main canvas - be specific about screen space canvas
        Canvas mainCanvas = null;
        Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                mainCanvas = canvas;
                break;
            }
        }
        
        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("MainCanvas");
            canvasGO.layer = LayerMask.NameToLayer("UI");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create player health UI container
        GameObject healthUIContainer = new GameObject("PlayerHealthUI");
        healthUIContainer.layer = LayerMask.NameToLayer("UI");
        healthUIContainer.transform.SetParent(mainCanvas.transform, false);
        
        Debug.Log($"Created PlayerHealthUI under: {mainCanvas.name} (Canvas renderMode: {mainCanvas.renderMode})");
        
        // Setup anchoring for top-left FIRST
        RectTransform containerRect = healthUIContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 1);
        containerRect.anchoredPosition = new Vector2(10, -10);
        containerRect.sizeDelta = new Vector2(300, 40);
        
        // Create health bar
        GameObject healthBar = CreateHealthBar("PlayerHealthBar");
        healthBar.layer = LayerMask.NameToLayer("UI");
        healthBar.transform.SetParent(healthUIContainer.transform, false);
        
        // Add PlayerHealthUI component and connect references
        PlayerHealthUI playerHealthUI = healthUIContainer.AddComponent<PlayerHealthUI>();
        SerializedObject playerHealthSO = new SerializedObject(playerHealthUI);
        playerHealthSO.FindProperty("healthBar").objectReferenceValue = healthBar.GetComponent<HealthBarUI>();
        playerHealthSO.ApplyModifiedProperties();
        
        Debug.Log("Player Health UI created successfully!");
        
        // Select the created object
        Selection.activeGameObject = healthUIContainer;
    }
    
    private static void SetupEnemyHealthUI()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select an enemy GameObject", "OK");
            return;
        }
        
        EnemyBase enemy = selected.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            EditorUtility.DisplayDialog("Invalid Selection", "Selected GameObject must have an EnemyBase component", "OK");
            return;
        }
        
        // Create enemy health UI container
        GameObject healthUIContainer = new GameObject("EnemyHealthUI");
        healthUIContainer.transform.SetParent(selected.transform, false);
        healthUIContainer.transform.localPosition = new Vector3(0, 1.0f, 0);
        
        // Create world space canvas
        GameObject canvasGO = new GameObject("HealthCanvas");
        canvasGO.transform.SetParent(healthUIContainer.transform, false);
        Canvas worldCanvas = canvasGO.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingLayerName = "UI";
        worldCanvas.sortingOrder = 10;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        // Add EnemyHealthUI component
        EnemyHealthUI enemyHealthUI = healthUIContainer.AddComponent<EnemyHealthUI>();
        
        // Create health bar
        GameObject healthBar = CreateHealthBar("EnemyHealthBar");
        healthBar.transform.SetParent(canvasGO.transform, false);
        
        // Scale for world space
        RectTransform healthBarRect = healthBar.GetComponent<RectTransform>();
        healthBarRect.sizeDelta = new Vector2(100, 20);
        healthBarRect.localScale = Vector3.one * 0.01f;
        healthBarRect.anchoredPosition = Vector2.zero;
        
        // Set references
        SerializedObject enemyHealthSO = new SerializedObject(enemyHealthUI);
        enemyHealthSO.FindProperty("healthBar").objectReferenceValue = healthBar.GetComponent<HealthBarUI>();
        enemyHealthSO.FindProperty("worldCanvas").objectReferenceValue = worldCanvas;
        enemyHealthSO.FindProperty("showOnStart").boolValue = true;
        enemyHealthSO.FindProperty("alwaysVisible").boolValue = true; // Make it always visible for testing
        enemyHealthSO.ApplyModifiedProperties();
        
        // Mark objects as dirty for proper saving
        EditorUtility.SetDirty(enemyHealthUI);
        EditorUtility.SetDirty(healthBar.GetComponent<HealthBarUI>());
        
        // Initialize the enemy health UI (this should be called in play mode)
        if (Application.isPlaying)
        {
            enemyHealthUI.Initialize(enemy);
        }
        else
        {
            Debug.Log("Enemy Health UI will initialize when play mode starts");
        }
        
        Debug.Log($"Enemy Health UI created for {selected.name}!");
        
        // Select the created object
        Selection.activeGameObject = healthUIContainer;
    }
    
    private static GameObject CreateHealthBar(string name)
    {
        // Create health bar container
        GameObject healthBarGO = new GameObject(name);
        healthBarGO.layer = LayerMask.NameToLayer("UI");
        RectTransform healthBarRect = healthBarGO.AddComponent<RectTransform>();
        healthBarRect.sizeDelta = new Vector2(300, 40);
        
        // Add HealthBarUI component
        HealthBarUI healthBarUI = healthBarGO.AddComponent<HealthBarUI>();
        
        // Create background
        GameObject background = new GameObject("Background");
        background.layer = LayerMask.NameToLayer("UI");
        background.transform.SetParent(healthBarGO.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Create slider
        GameObject sliderGO = new GameObject("HealthSlider");
        sliderGO.layer = LayerMask.NameToLayer("UI");
        sliderGO.transform.SetParent(healthBarGO.transform, false);
        Slider slider = sliderGO.AddComponent<Slider>();
        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = new Vector2(-10, -10);
        sliderRect.anchoredPosition = Vector2.zero;
        
        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.layer = LayerMask.NameToLayer("UI");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-10, 0);
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.layer = LayerMask.NameToLayer("UI");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.green;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.sizeDelta = new Vector2(10, 0);
        fillRect.anchoredPosition = Vector2.zero;
        
        // Configure slider
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 1f;
        slider.value = 1f;
        
        // Create health text
        GameObject textGO = new GameObject("HealthText");
        textGO.layer = LayerMask.NameToLayer("UI");
        textGO.transform.SetParent(healthBarGO.transform, false);
        Text healthText = textGO.AddComponent<Text>();
        healthText.text = "0/0"; // Will be updated by Initialize
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.color = Color.white;
        healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthText.fontSize = 14;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Assign references to HealthBarUI
        SerializedObject healthBarSO = new SerializedObject(healthBarUI);
        healthBarSO.FindProperty("healthSlider").objectReferenceValue = slider;
        healthBarSO.FindProperty("fillImage").objectReferenceValue = fillImage;
        healthBarSO.FindProperty("healthText").objectReferenceValue = healthText;
        healthBarSO.ApplyModifiedProperties();
        
        return healthBarGO;
    }
}