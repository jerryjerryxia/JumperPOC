using UnityEngine;
using System.Collections;

/// <summary>
/// Spawn point for level transitions. Automatically positions player when entering from another level.
/// Integrates with existing SimpleRespawnManager to maintain save system functionality.
/// </summary>
public class LevelSpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private string spawnPointId = "LevelEntry";
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    
    [Header("Save System Integration")]
    [SerializeField] private bool setAsRespawnPoint = true;
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.green;
    
    private void Start()
    {
        // Check if this spawn point should be activated due to level transition
        if (LevelTransitionManager.HasPendingTransition() && 
            LevelTransitionManager.GetPendingSpawnPointId() == spawnPointId)
        {
            SpawnPlayerHere();
            LevelTransitionManager.ClearPendingTransition();
        }
    }
    
    private void SpawnPlayerHere()
    {
        Vector3 spawnPosition = transform.position + spawnOffset;

        // Find player (including DontDestroyOnLoad objects)
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError($"LevelSpawnPoint '{spawnPointId}': Player not found in scene!");
            return;
        }

        Debug.Log($"[LevelSpawnPoint] BEFORE SPAWN - Player position: {player.transform.position}");

        // Get Rigidbody2D and clear all physics state
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"[LevelSpawnPoint] BEFORE SPAWN - Player velocity: {rb.linearVelocity}");
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            Debug.Log($"[LevelSpawnPoint] Physics state cleared");
        }

        // Update save system FIRST (before calling ResetToRespawnPoint)
        // This ensures SimpleRespawnManager has the correct position
        if (setAsRespawnPoint && SimpleRespawnManager.Instance != null)
        {
            SimpleRespawnManager.Instance.SetRespawnPoint(spawnPosition, spawnPointId);
            Debug.Log($"[LevelSpawnPoint] Updated respawn point to: {spawnPosition}");
        }

        // Reset player state via controller's respawn system
        player.SetRespawnPoint(spawnPosition);
        player.ResetToRespawnPoint();
        Debug.Log($"[LevelSpawnPoint] Player state reset via PlayerController");

        Debug.Log($"[LevelSpawnPoint] AFTER SPAWN - Player position: {player.transform.position}");

        Debug.Log($"[LevelSpawnPoint] Player spawned at '{spawnPointId}' - Position: {spawnPosition}");

        // Force position update on next frame to ensure it sticks
        StartCoroutine(VerifySpawnPosition(player, spawnPosition));
    }

    private IEnumerator VerifySpawnPosition(PlayerController player, Vector3 expectedPosition)
    {
        yield return new WaitForFixedUpdate();

        if (Vector3.Distance(player.transform.position, expectedPosition) > 0.1f)
        {
            Debug.LogWarning($"[LevelSpawnPoint] Player position drifted! Expected: {expectedPosition}, Actual: {player.transform.position}. Re-applying position.");
            player.transform.position = expectedPosition;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        else
        {
            Debug.Log($"[LevelSpawnPoint] Spawn position verified: {player.transform.position}");
        }
    }
    
    // Manual activation method for testing or other uses
    [ContextMenu("Test Spawn Player Here")]
    public void TestSpawnPlayer()
    {
        SpawnPlayerHere();
    }
    
    // Editor tool support methods
    public void SetSpawnData(string id, Vector3 position)
    {
        spawnPointId = id;
        transform.position = position;
    }
    
    public string GetSpawnPointId() => spawnPointId;
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Vector3 spawnPosition = transform.position + spawnOffset;
        
        // Simple spawn point marker
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPosition, 0.5f);
        
        // Center cross
        Gizmos.color = Color.white;
        float crossSize = 0.3f;
        Gizmos.DrawLine(spawnPosition + Vector3.left * crossSize, spawnPosition + Vector3.right * crossSize);
        Gizmos.DrawLine(spawnPosition + Vector3.up * crossSize, spawnPosition + Vector3.down * crossSize);
        
        // Offset connection if not zero
        if (spawnOffset != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, spawnPosition);
        }
        
        #if UNITY_EDITOR
        // Simple label
        Vector3 labelPos = spawnPosition + Vector3.up * 0.8f;
        var style = new GUIStyle();
        style.normal.textColor = Color.green;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        UnityEditor.Handles.Label(labelPos, $"SPAWN: {spawnPointId}", style);
        #endif
    }
}