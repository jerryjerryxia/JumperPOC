using UnityEngine;

/// <summary>
/// Simple save point that sets respawn location when player touches it
/// </summary>
public class SavePoint : MonoBehaviour
{
    [Header("Save Point Settings")]
    [SerializeField] private string savePointId = "SavePoint_01";
    [SerializeField] private bool activateOnTrigger = true;
    
    [Header("Respawn Positioning")]
    [SerializeField] private Vector3 respawnOffset = new Vector3(-0.5f, 0f, 0f); // Offset respawn position from save point center
    [SerializeField] private bool showRespawnPosition = true; // Visualize respawn position in scene view
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject activatedVisual;
    [SerializeField] private ParticleSystem activationEffect;
    
    private bool isActivated = false;
    
    private void Start()
    {
        // Set initial visual state
        if (activatedVisual != null)
            activatedVisual.SetActive(false);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activateOnTrigger || isActivated) return;
        
        if (other.CompareTag("Player"))
        {
            ActivateSavePoint();
        }
    }
    
    public void ActivateSavePoint()
    {
        if (isActivated) return;
        
        isActivated = true;
        
        // Set this as the current respawn point with offset applied
        Vector3 respawnPosition = transform.position + respawnOffset;
        if (SimpleRespawnManager.Instance != null)
        {
            SimpleRespawnManager.Instance.SetRespawnPoint(respawnPosition, savePointId);
        }
        else
        {
            Debug.LogWarning($"SimpleRespawnManager not found! Save point {savePointId} cannot be activated.");
        }
        
        // Visual feedback
        if (activatedVisual != null)
            activatedVisual.SetActive(true);
            
        if (activationEffect != null)
            activationEffect.Play();
            
        Debug.Log($"Save Point Activated: {savePointId} | SavePoint Position: {transform.position} | Respawn Position: {respawnPosition} | Offset Applied: {respawnOffset}");
    }
    
    /// <summary>
    /// Visualize the respawn position in scene view
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showRespawnPosition) return;
        
        Vector3 respawnPosition = transform.position + respawnOffset;
        
        // Draw save point position (blue circle)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
        
        // Draw respawn position (green circle)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(respawnPosition, 0.15f);
        
        // Draw arrow from save point to respawn position
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, respawnPosition);
        
        // Draw small cross at respawn position for precise positioning
        Gizmos.color = Color.red;
        Gizmos.DrawLine(respawnPosition + Vector3.left * 0.1f, respawnPosition + Vector3.right * 0.1f);
        Gizmos.DrawLine(respawnPosition + Vector3.up * 0.1f, respawnPosition + Vector3.down * 0.1f);
        
        #if UNITY_EDITOR
        // Label the positions
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, $"SavePoint\n{savePointId}");
        UnityEditor.Handles.Label(respawnPosition + Vector3.up * 0.3f, $"Respawn\nOffset: {respawnOffset}");
        #endif
    }
}