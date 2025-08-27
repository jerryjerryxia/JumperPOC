using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Environment
{
    /// <summary>
    /// Optional manager for coordinating multiple breakable terrain objects.
    /// Useful for save/load systems and global terrain management.
    /// </summary>
    public class BreakableTerrainManager : MonoBehaviour
    {
        [Header("Management Settings")]
        [SerializeField] private bool autoRegisterTerrainInScene = true;
        [SerializeField] private bool enableGlobalRestore = true;
        [SerializeField] private KeyCode globalRestoreKey = KeyCode.F9; // Debug key
        
        [Header("Save System Integration")]
        [SerializeField] private bool enableSaveSystem = false;
        [SerializeField] private string saveFileName = "breakable_terrain_state.json";
        
        // Registered terrain objects
        private List<BreakableTerrain> registeredTerrain = new List<BreakableTerrain>();
        
        // Events for external systems
        public System.Action<BreakableTerrain> OnAnyTerrainBroken;
        public System.Action<BreakableTerrain> OnAnyTerrainRestored;
        public System.Action<int> OnTerrainCountChanged; // Useful for achievement systems
        
        // Singleton for easy access
        public static BreakableTerrainManager Instance { get; private set; }
        
        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            if (autoRegisterTerrainInScene)
            {
                RegisterAllTerrainInScene();
            }
        }
        
        private void Update()
        {
            // Debug key for global restore
            if (enableGlobalRestore && Input.GetKeyDown(globalRestoreKey))
            {
                RestoreAllTerrain();
                Debug.Log("BreakableTerrainManager: Restored all terrain via debug key");
            }
        }
        
        /// <summary>
        /// Register a breakable terrain object with the manager
        /// </summary>
        public void RegisterTerrain(BreakableTerrain terrain)
        {
            if (terrain == null) return;
            if (registeredTerrain.Contains(terrain)) return;
            
            registeredTerrain.Add(terrain);
            
            // Subscribe to events
            terrain.OnTerrainBroken += HandleTerrainBroken;
            terrain.OnTerrainRestored += HandleTerrainRestored;
            
            OnTerrainCountChanged?.Invoke(registeredTerrain.Count);
        }
        
        /// <summary>
        /// Unregister a breakable terrain object
        /// </summary>
        public void UnregisterTerrain(BreakableTerrain terrain)
        {
            if (terrain == null) return;
            if (!registeredTerrain.Contains(terrain)) return;
            
            registeredTerrain.Remove(terrain);
            
            // Unsubscribe from events
            terrain.OnTerrainBroken -= HandleTerrainBroken;
            terrain.OnTerrainRestored -= HandleTerrainRestored;
            
            OnTerrainCountChanged?.Invoke(registeredTerrain.Count);
        }
        
        /// <summary>
        /// Find and register all BreakableTerrain objects in the scene
        /// </summary>
        public void RegisterAllTerrainInScene()
        {
            BreakableTerrain[] allTerrain = FindObjectsOfType<BreakableTerrain>();
            
            foreach (BreakableTerrain terrain in allTerrain)
            {
                RegisterTerrain(terrain);
            }
            
            Debug.Log($"BreakableTerrainManager: Registered {allTerrain.Length} terrain objects");
        }
        
        /// <summary>
        /// Restore all registered terrain to unbroken state
        /// </summary>
        public void RestoreAllTerrain()
        {
            int restoredCount = 0;
            
            foreach (BreakableTerrain terrain in registeredTerrain)
            {
                if (terrain != null && terrain.IsBroken)
                {
                    terrain.ForceRestore();
                    restoredCount++;
                }
            }
            
            Debug.Log($"BreakableTerrainManager: Restored {restoredCount} terrain objects");
        }
        
        /// <summary>
        /// Reset all terrain to initial state (as if never broken)
        /// </summary>
        public void ResetAllTerrain()
        {
            int resetCount = 0;
            
            foreach (BreakableTerrain terrain in registeredTerrain)
            {
                if (terrain != null)
                {
                    terrain.ResetState();
                    resetCount++;
                }
            }
            
            Debug.Log($"BreakableTerrainManager: Reset {resetCount} terrain objects");
        }
        
        /// <summary>
        /// Get statistics about terrain states
        /// </summary>
        public TerrainStatistics GetStatistics()
        {
            var stats = new TerrainStatistics();
            
            foreach (BreakableTerrain terrain in registeredTerrain)
            {
                if (terrain == null) continue;
                
                stats.totalCount++;
                if (terrain.IsBroken) stats.brokenCount++;
                if (terrain.HasBeenBroken) stats.everBrokenCount++;
            }
            
            stats.intactCount = stats.totalCount - stats.brokenCount;
            stats.neverBrokenCount = stats.totalCount - stats.everBrokenCount;
            
            return stats;
        }
        
        private void HandleTerrainBroken(BreakableTerrain terrain)
        {
            OnAnyTerrainBroken?.Invoke(terrain);
            
            if (enableSaveSystem)
            {
                SaveTerrainState();
            }
        }
        
        private void HandleTerrainRestored(BreakableTerrain terrain)
        {
            OnAnyTerrainRestored?.Invoke(terrain);
            
            if (enableSaveSystem)
            {
                SaveTerrainState();
            }
        }
        
        // Simple save system implementation
        private void SaveTerrainState()
        {
            if (!enableSaveSystem) return;
            
            var saveData = new TerrainSaveData();
            saveData.brokenTerrainIds = new List<string>();
            saveData.everBrokenTerrainIds = new List<string>();
            
            foreach (BreakableTerrain terrain in registeredTerrain)
            {
                if (terrain == null) continue;
                
                string terrainId = GetTerrainId(terrain);
                
                if (terrain.IsBroken)
                {
                    saveData.brokenTerrainIds.Add(terrainId);
                }
                
                if (terrain.HasBeenBroken)
                {
                    saveData.everBrokenTerrainIds.Add(terrainId);
                }
            }
            
            string jsonData = JsonUtility.ToJson(saveData, true);
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);
            
            try
            {
                System.IO.File.WriteAllText(savePath, jsonData);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save terrain state: {e.Message}");
            }
        }
        
        public void LoadTerrainState()
        {
            if (!enableSaveSystem) return;
            
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);
            
            if (!System.IO.File.Exists(savePath))
            {
                Debug.Log("No terrain save file found");
                return;
            }
            
            try
            {
                string jsonData = System.IO.File.ReadAllText(savePath);
                TerrainSaveData saveData = JsonUtility.FromJson<TerrainSaveData>(jsonData);
                
                // Reset all terrain first
                ResetAllTerrain();
                
                // Apply saved states
                foreach (BreakableTerrain terrain in registeredTerrain)
                {
                    if (terrain == null) continue;
                    
                    string terrainId = GetTerrainId(terrain);
                    
                    if (saveData.brokenTerrainIds.Contains(terrainId))
                    {
                        terrain.ForceBreak();
                    }
                }
                
                Debug.Log($"Loaded terrain state: {saveData.brokenTerrainIds.Count} broken terrain objects");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load terrain state: {e.Message}");
            }
        }
        
        private string GetTerrainId(BreakableTerrain terrain)
        {
            // Simple ID generation - you might want something more robust
            return terrain.transform.position.ToString() + "_" + terrain.gameObject.name;
        }
        
        // Public API for external systems
        public int GetTotalTerrainCount() => registeredTerrain.Count;
        public int GetBrokenTerrainCount() => registeredTerrain.Count(t => t != null && t.IsBroken);
        public int GetIntactTerrainCount() => registeredTerrain.Count(t => t != null && !t.IsBroken);
        
        public List<BreakableTerrain> GetBrokenTerrain() => 
            registeredTerrain.Where(t => t != null && t.IsBroken).ToList();
        
        public List<BreakableTerrain> GetIntactTerrain() => 
            registeredTerrain.Where(t => t != null && !t.IsBroken).ToList();
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            foreach (BreakableTerrain terrain in registeredTerrain)
            {
                if (terrain != null)
                {
                    terrain.OnTerrainBroken -= HandleTerrainBroken;
                    terrain.OnTerrainRestored -= HandleTerrainRestored;
                }
            }
        }
        
        // Debug GUI
        #if UNITY_EDITOR
        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            // Simple debug display
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label("=== BREAKABLE TERRAIN DEBUG ===");
            
            var stats = GetStatistics();
            GUILayout.Label($"Total: {stats.totalCount}");
            GUILayout.Label($"Broken: {stats.brokenCount}");
            GUILayout.Label($"Intact: {stats.intactCount}");
            GUILayout.Label($"Ever Broken: {stats.everBrokenCount}");
            
            if (GUILayout.Button("Restore All"))
            {
                RestoreAllTerrain();
            }
            
            if (GUILayout.Button("Reset All"))
            {
                ResetAllTerrain();
            }
            
            GUILayout.EndArea();
        }
        #endif
    }
    
    // Data structures for statistics and saving
    [System.Serializable]
    public struct TerrainStatistics
    {
        public int totalCount;
        public int brokenCount;
        public int intactCount;
        public int everBrokenCount;
        public int neverBrokenCount;
    }
    
    [System.Serializable]
    public class TerrainSaveData
    {
        public List<string> brokenTerrainIds;
        public List<string> everBrokenTerrainIds;
    }
}