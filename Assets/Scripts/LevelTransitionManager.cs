using UnityEngine;

/// <summary>
/// Simple static manager for coordinating level transitions.
/// Stores transition data between scene loads with minimal overhead.
/// </summary>
public static class LevelTransitionManager
{
    private static string pendingSpawnPointId;
    private static bool hasPendingTransition;
    
    /// <summary>
    /// Set pending transition data for the next scene
    /// </summary>
    public static void SetPendingTransition(string spawnPointId)
    {
        pendingSpawnPointId = spawnPointId;
        hasPendingTransition = true;
        
        Debug.Log($"Pending transition set: Spawn at '{spawnPointId}'");
    }
    
    /// <summary>
    /// Check if there's a pending transition
    /// </summary>
    public static bool HasPendingTransition()
    {
        return hasPendingTransition;
    }
    
    /// <summary>
    /// Get the pending spawn point ID
    /// </summary>
    public static string GetPendingSpawnPointId()
    {
        return pendingSpawnPointId;
    }
    
    /// <summary>
    /// Clear pending transition data (called after successful spawn)
    /// </summary>
    public static void ClearPendingTransition()
    {
        hasPendingTransition = false;
        pendingSpawnPointId = null;
        
        Debug.Log("Pending transition cleared");
    }
    
    /// <summary>
    /// Reset all transition data (for cleanup)
    /// </summary>
    public static void Reset()
    {
        ClearPendingTransition();
    }
}