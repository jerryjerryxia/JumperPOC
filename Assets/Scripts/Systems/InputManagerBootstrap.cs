using UnityEngine;

/// <summary>
/// Bootstrap script that ensures an InputManager exists at runtime.
/// Add this to any GameObject in your scene as a failsafe.
/// </summary>
public class InputManagerBootstrap : MonoBehaviour
{
    [Header("Bootstrap Settings")]
    [SerializeField] private bool enableInputDebugging = true;
    [SerializeField] private bool destroyAfterBootstrap = true;
    
    void Awake()
    {
        // Check if InputManager already exists
        if (InputManager.Instance == null)
        {
            // Debug.Log("[InputManagerBootstrap] InputManager not found. Creating one...");
            
            // Create InputManager GameObject
            GameObject inputManagerGO = new GameObject("InputManager (Bootstrap)");
            InputManager inputManager = inputManagerGO.AddComponent<InputManager>();
            
            // Enable debugging if requested
            if (enableInputDebugging)
            {
                inputManager.SetInputDebugging(true);
            }
            
            // Make it persistent across scenes
            DontDestroyOnLoad(inputManagerGO);
            
            // Debug.Log("[InputManagerBootstrap] InputManager created successfully!");
        }
        else
        {
            // Debug.Log("[InputManagerBootstrap] InputManager already exists. Bootstrap not needed.");
        }
        
        // Destroy this bootstrap object if configured to do so
        if (destroyAfterBootstrap)
        {
            Destroy(gameObject);
        }
    }
}