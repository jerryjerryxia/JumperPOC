using UnityEngine;

/// <summary>
/// Simple save point that sets respawn location when player touches it
/// </summary>
public class SavePoint : MonoBehaviour
{
    [Header("Save Point Settings")]
    [SerializeField] private string savePointId = "SavePoint_01";
    [SerializeField] private bool activateOnTrigger = true;
    
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
        
        // Set this as the current respawn point
        SimpleRespawnManager.Instance.SetRespawnPoint(transform.position, savePointId);
        
        // Visual feedback
        if (activatedVisual != null)
            activatedVisual.SetActive(true);
            
        if (activationEffect != null)
            activationEffect.Play();
            
        Debug.Log($"Save Point Activated: {savePointId}");
    }
}