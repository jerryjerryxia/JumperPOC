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
        Debug.Log($"Respawn point set to: {savePointId} at {position}");
    }
    
    public Vector3 GetRespawnPosition()
    {
        return currentRespawnPosition;
    }
    
    public string GetCurrentSavePointId()
    {
        return currentSavePointId;
    }
}