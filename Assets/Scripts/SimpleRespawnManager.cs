using UnityEngine;

/// <summary>
/// Simple singleton that tracks the current respawn point
/// </summary>
public class SimpleRespawnManager : MonoBehaviour
{
    public static SimpleRespawnManager Instance { get; private set; }
    
    [Header("Default Respawn")]
    [SerializeField] private Vector3 defaultRespawnPosition = Vector3.zero;
    
    private Vector3 currentRespawnPosition;
    private string currentSavePointId = "Default";
    private bool hasInitializedPosition = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentRespawnPosition = defaultRespawnPosition;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void SetRespawnPoint(Vector3 position, string savePointId)
    {
        currentRespawnPosition = position;
        currentSavePointId = savePointId;
        hasInitializedPosition = true; // Mark as initialized when a save point is set
        Debug.Log($"Respawn point set to: {savePointId} at {position}");
    }
    
    public Vector3 GetRespawnPosition()
    {
        // If we haven't been properly initialized, return Vector3.zero as fallback
        // This will cause PlayerController to use its own initialPosition fallback
        if (!hasInitializedPosition)
        {
            return Vector3.zero;
        }
        return currentRespawnPosition;
    }
    
    /// <summary>
    /// Initialize the respawn manager with the player's actual starting position
    /// This should be called by the PlayerController during its Start()
    /// </summary>
    public void InitializeWithPlayerStartPosition(Vector3 playerStartPosition)
    {
        if (!hasInitializedPosition)
        {
            currentRespawnPosition = playerStartPosition;
            defaultRespawnPosition = playerStartPosition;
            hasInitializedPosition = true;
            Debug.Log($"SimpleRespawnManager initialized with player start position: {playerStartPosition}");
        }
    }
    
    /// <summary>
    /// Check if the respawn manager has been properly initialized
    /// </summary>
    public bool IsInitialized()
    {
        return hasInitializedPosition;
    }
    
    public string GetCurrentSavePointId()
    {
        return currentSavePointId;
    }
}