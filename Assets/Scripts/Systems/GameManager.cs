using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main game manager that oversees game state, scene management, and system coordination.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private bool pauseOnFocusLoss = true;
    [SerializeField] private bool enableDebugMode = false;
    
    // Singleton instance
    public static GameManager Instance { get; private set; }
    
    // Game state
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver,
        Loading
    }
    
    private GameState currentState = GameState.Playing;
    public GameState CurrentState => currentState;
    
    // Events
    public System.Action<GameState> OnGameStateChanged;
    public System.Action OnGamePaused;
    public System.Action OnGameResumed;
    
    // System references
    private InputManager inputManager;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Find or create input manager
        inputManager = FindFirstObjectByType<InputManager>();
        if (inputManager == null)
        {
            GameObject inputManagerGO = new GameObject("InputManager");
            inputManager = inputManagerGO.AddComponent<InputManager>();
        }
        
        // Set initial game state
        ChangeGameState(GameState.Playing);
    }
    
    private void InitializeGame()
    {
        // Set target frame rate for consistent performance
        Application.targetFrameRate = 60;
        
        // Set quality settings
        QualitySettings.vSyncCount = 1;
        
        if (enableDebugMode)
        {
            Debug.Log("GameManager: Game initialized in debug mode");
        }
    }
    
    private void Update()
    {
        // Handle pause input (ESC key for testing)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
        
        // Debug input
        if (enableDebugMode)
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleDebugMode();
            }
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (pauseOnFocusLoss && !hasFocus && currentState == GameState.Playing)
        {
            PauseGame();
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseOnFocusLoss && pauseStatus && currentState == GameState.Playing)
        {
            PauseGame();
        }
    }
    
    public void ChangeGameState(GameState newState)
    {
        if (currentState == newState) return;
        
        GameState previousState = currentState;
        currentState = newState;
        
        // Handle state transitions
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                if (inputManager != null) inputManager.EnableInput();
                break;
                
            case GameState.Paused:
                Time.timeScale = 0f;
                if (inputManager != null) inputManager.DisableInput();
                break;
                
            case GameState.GameOver:
                Time.timeScale = 0f;
                if (inputManager != null) inputManager.DisableInput();
                break;
                
            case GameState.Loading:
                if (inputManager != null) inputManager.DisableInput();
                break;
        }
        
        OnGameStateChanged?.Invoke(newState);
        
        if (enableDebugMode)
        {
            Debug.Log($"GameManager: State changed from {previousState} to {newState}");
        }
    }
    
    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            ChangeGameState(GameState.Paused);
            OnGamePaused?.Invoke();
        }
    }
    
    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            ChangeGameState(GameState.Playing);
            OnGameResumed?.Invoke();
        }
    }
    
    public void RestartLevel()
    {
        ChangeGameState(GameState.Loading);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void LoadScene(string sceneName)
    {
        ChangeGameState(GameState.Loading);
        SceneManager.LoadScene(sceneName);
    }
    
    public void QuitGame()
    {
        if (enableDebugMode)
        {
            Debug.Log("GameManager: Quitting game");
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    public bool IsGamePlaying()
    {
        return currentState == GameState.Playing;
    }
    
    public bool IsGamePaused()
    {
        return currentState == GameState.Paused;
    }
    
    private void ToggleDebugMode()
    {
        enableDebugMode = !enableDebugMode;
        
        // Toggle debug mode on input manager if it exists
        if (inputManager != null)
        {
            inputManager.SetInputDebugging(enableDebugMode);
        }
        
        Debug.Log($"Debug mode: {(enableDebugMode ? "Enabled" : "Disabled")}");
    }
    
    #if UNITY_EDITOR
    private void OnGUI()
    {
        if (!enableDebugMode) return;
        
        GUILayout.BeginArea(new Rect(10, Screen.height - 120, 200, 110));
        GUILayout.Label("Game Manager Debug:", GUI.skin.label);
        GUILayout.Label($"State: {currentState}");
        GUILayout.Label($"Time Scale: {Time.timeScale:F2}");
        GUILayout.Label($"FPS: {(1f / Time.unscaledDeltaTime):F1}");
        
        if (GUILayout.Button("Toggle Pause"))
        {
            if (currentState == GameState.Playing)
                PauseGame();
            else if (currentState == GameState.Paused)
                ResumeGame();
        }
        
        GUILayout.EndArea();
    }
    #endif
}