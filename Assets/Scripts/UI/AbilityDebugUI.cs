using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Debug UI for testing ability toggles during gameplay.
/// Shows ability status and provides buttons to toggle them on/off.
/// </summary>
public class AbilityDebugUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject abilityPanel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Key toggleKey = Key.F1;
    
    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateButtons = true;
    
    private bool isUIVisible = false;
    
    // Ability definitions for UI creation
    private readonly string[] abilities = {
        "doublejump", "dash", "wallstick", "ledgegrab",
        "airattack", "dashattack", "comboattack"
    };
    
    private readonly string[] abilityDisplayNames = {
        "Double Jump", "Dash", "Wall Stick", "Ledge Grab",
        "Air Attack", "Dash Attack", "Combo Attack"
    };
    
    void Start()
    {
        // DISABLED: Don't create UI panel - use PlayerAbilities OnGUI instead
        // Hide panel completely from scene and game view
        if (abilityPanel != null)
        {
            abilityPanel.SetActive(false);
            // Also hide from scene view by disabling the GameObject entirely
            gameObject.SetActive(false);
        }
        return;
        
        if (autoCreateButtons)
        {
            CreateAbilityButtons();
        }
        
        // Start with UI hidden
        if (abilityPanel != null)
        {
            abilityPanel.SetActive(false);
        }
    }
    
    void Update()
    {
        // DISABLED: F1 toggle functionality - use PlayerAbilities OnGUI instead
        return;
        
        // Toggle UI visibility using new Input System
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            ToggleUI();
        }
    }
    
    public void ToggleUI()
    {
        isUIVisible = !isUIVisible;
        if (abilityPanel != null)
        {
            abilityPanel.SetActive(isUIVisible);
        }
        
        if (isUIVisible)
        {
            RefreshButtonStates();
        }
    }
    
    private void CreateAbilityButtons()
    {
        if (buttonContainer == null || buttonPrefab == null)
        {
            Debug.LogWarning("[AbilityDebugUI] Missing UI references for auto button creation");
            return;
        }
        
        // Clear existing buttons
        foreach (Transform child in buttonContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        
        // Create buttons for each ability
        for (int i = 0; i < abilities.Length; i++)
        {
            CreateAbilityButton(abilities[i], abilityDisplayNames[i]);
        }
    }
    
    private void CreateAbilityButton(string abilityName, string displayName)
    {
        GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
        Button button = buttonObj.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        
        if (button == null || buttonText == null)
        {
            Debug.LogError("[AbilityDebugUI] Button prefab must have Button component and TextMeshProUGUI child!");
            Destroy(buttonObj);
            return;
        }
        
        // Set up button
        buttonText.text = GetButtonText(abilityName, displayName);
        button.onClick.AddListener(() => ToggleAbility(abilityName, button, buttonText, displayName));
        
        // Store references for updates
        buttonObj.name = $"Button_{abilityName}";
    }
    
    private void ToggleAbility(string abilityName, Button button, TextMeshProUGUI buttonText, string displayName)
    {
        if (PlayerAbilities.Instance == null)
        {
            Debug.LogWarning("[AbilityDebugUI] PlayerAbilities instance not found!");
            return;
        }
        
        PlayerAbilities.Instance.ToggleAbility(abilityName);
        buttonText.text = GetButtonText(abilityName, displayName);
        
        // Visual feedback
        StartCoroutine(ButtonFeedback(button));
    }
    
    private string GetButtonText(string abilityName, string displayName)
    {
        if (PlayerAbilities.Instance == null) return $"{displayName}: ???";
        
        bool isUnlocked = PlayerAbilities.Instance.GetAbility(abilityName);
        string status = isUnlocked ? "<color=green>ON</color>" : "<color=red>OFF</color>";
        return $"{displayName}: {status}";
    }
    
    private void RefreshButtonStates()
    {
        if (buttonContainer == null) return;
        
        for (int i = 0; i < abilities.Length; i++)
        {
            Transform buttonTransform = buttonContainer.Find($"Button_{abilities[i]}");
            if (buttonTransform != null)
            {
                TextMeshProUGUI buttonText = buttonTransform.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = GetButtonText(abilities[i], abilityDisplayNames[i]);
                }
            }
        }
    }
    
    private System.Collections.IEnumerator ButtonFeedback(Button button)
    {
        ColorBlock colors = button.colors;
        Color originalColor = colors.normalColor;
        
        colors.normalColor = Color.yellow;
        button.colors = colors;
        
        yield return new WaitForSeconds(0.1f);
        
        colors.normalColor = originalColor;
        button.colors = colors;
    }
    
    void OnGUI()
    {
        // DISABLED: Hide instruction box - use PlayerAbilities OnGUI instead
        return;
        
        if (!isUIVisible) return;
        
        // Show instructions
        GUI.Box(new Rect(10, Screen.height - 60, 300, 50), 
               $"Press {toggleKey} to toggle ability panel\nClick buttons to enable/disable abilities");
    }
}