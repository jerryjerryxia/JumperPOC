using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

namespace Environment
{
    /// <summary>
    /// Makes terrain breakable when touched by player from specified directions.
    /// Integrates with tilemap system (including Composite Collider 2D) and provides visual/audio feedback.
    /// Encourages exploration through "touch to discover" mechanics.
    /// 
    /// COMPOSITE COLLIDER SUPPORT:
    /// - Works with tilemaps that use Composite Collider 2D
    /// - Automatically detects composite collider setup
    /// - Uses separate trigger collider for break detection
    /// - Maintains tilemap collision while enabling breakable functionality
    /// </summary>
    public class BreakableTerrain : MonoBehaviour
    {
        [System.Flags]
        public enum BreakDirection
        {
            None = 0,
            Top = 1,
            Bottom = 2,
            Left = 4,
            Right = 8,
            Sides = Left | Right,
            Vertical = Top | Bottom,
            All = Top | Bottom | Left | Right
        }

        [Header("Break Conditions")]
        [SerializeField] private BreakDirection allowedBreakDirections = BreakDirection.All;
        [SerializeField] private bool requireMinimumVelocity = true;
        [SerializeField] private float minimumVelocity = 2f;
        [SerializeField] private bool requireSpecificPlayerState = false;
        [SerializeField] private bool requireDashState = false;

        [Header("Effects")]
        [SerializeField] private GameObject breakEffectPrefab;
        [SerializeField] private AudioClip breakSound;
        [SerializeField] private bool addCameraShake = true;
        [SerializeField] private float cameraShakeDuration = 0.3f;
        [SerializeField] private float cameraShakeIntensity = 0.1f;

        [Header("Restoration")]
        [SerializeField] private bool canRestore = false;
        [SerializeField] private float restoreTime = 30f;
        [SerializeField] private bool oneTimeOnly = true;
        [SerializeField] private GameObject restoreEffectPrefab;

        [Header("Visual Feedback")]
        [SerializeField] private bool showBreakDirectionGizmos = true;
        [SerializeField] private Color gizmoColor = Color.red;
        [SerializeField] private bool highlightOnPlayerNear = false;
        [SerializeField] private float highlightDistance = 2f;
        
        [Header("Layer Setup")]
        [Tooltip("Leave as -1 for auto-detection of Player layer")]
        [SerializeField] private LayerMask playerLayers = -1;
        
        [Header("Trigger Setup")]
        [Tooltip("Position offset for the trigger collider (relative to GameObject)")]
        [SerializeField] private Vector2 triggerPosition = Vector2.zero;
        [Tooltip("Size of the trigger collider")]
        [SerializeField] private Vector2 triggerSize = Vector2.one;
        [Tooltip("Auto-calculate position and size from composite collider bounds")]
        [SerializeField] private bool autoCalculateFromBounds = true;

        // State
        private bool isBroken = false;
        private bool hasBeenBroken = false;
        private Collider2D terrainCollider;
        private CompositeCollider2D compositeCollider;
        private SpriteRenderer spriteRenderer;
        private Tilemap parentTilemap;
        private Vector3Int? tilemapPosition;
        private TileBase originalTile;
        private Coroutine restoreCoroutine;
        
        // Components
        private AudioSource audioSource;
        
        // Composite collider support
        private bool isCompositeColliderSetup = false;
        private bool autoCreateTriggerChild = true;
        
        // Events for integration
        public System.Action<BreakableTerrain> OnTerrainBroken;
        public System.Action<BreakableTerrain> OnTerrainRestored;

        private void Awake()
        {
            // Detect composite collider setup first
            DetectCompositeColliderSetup();
            
            // Initialize colliders based on setup type
            InitializeColliders();
            
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();
            
            // Auto-setup audio source if needed
            if (breakSound != null && audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.7f; // 3D spatial audio
            }

            // Check if we're part of a tilemap system
            InitializeTilemapIntegration();
            
            // Auto-configure player layers if not set
            if (playerLayers == -1)
            {
                ConfigurePlayerLayers();
            }
        }

        private void InitializeTilemapIntegration()
        {
            // Check if this GameObject is a tilemap or if we're part of one
            Tilemap tilemap = GetComponent<Tilemap>();
            if (tilemap == null)
                tilemap = GetComponentInParent<Tilemap>();
                
            if (tilemap != null)
            {
                parentTilemap = tilemap;
                
                // For composite colliders, use the center of the composite bounds to find the tile
                Vector3 positionForTileCalc;
                if (isCompositeColliderSetup && compositeCollider != null)
                {
                    positionForTileCalc = compositeCollider.bounds.center;
                }
                else
                {
                    positionForTileCalc = transform.position;
                }
                
                // Calculate tilemap position from world position
                tilemapPosition = parentTilemap.WorldToCell(positionForTileCalc);
                originalTile = parentTilemap.GetTile(tilemapPosition.Value);
                
                Debug.Log($"[BreakableTerrain] Tilemap integration: tile position {tilemapPosition.Value} at world {positionForTileCalc}");
            }
        }

        private void DetectCompositeColliderSetup()
        {
            // Check if this GameObject already has a composite collider
            compositeCollider = GetComponent<CompositeCollider2D>();
            
            // If not, check if it's part of a tilemap with composite collider
            if (compositeCollider == null)
            {
                Tilemap tilemap = GetComponent<Tilemap>();
                if (tilemap == null)
                    tilemap = GetComponentInParent<Tilemap>();
                    
                if (tilemap != null)
                {
                    TilemapCollider2D tilemapCollider = tilemap.GetComponent<TilemapCollider2D>();
                    compositeCollider = tilemap.GetComponent<CompositeCollider2D>();
                    
                    if (compositeCollider != null && tilemapCollider != null && tilemapCollider.usedByComposite)
                    {
                        isCompositeColliderSetup = true;
                    }
                }
            }
            else
            {
                // This GameObject itself has a composite collider
                isCompositeColliderSetup = true;
            }
        }
        
        private void InitializeColliders()
        {
            // First, try to use any existing collider on this GameObject
            terrainCollider = GetComponent<Collider2D>();
            
            if (terrainCollider != null && terrainCollider != compositeCollider)
            {
                // Use existing collider, make sure it's a trigger
                terrainCollider.isTrigger = true;
                Debug.Log($"[BreakableTerrain] Using existing collider on {name}");
            }
            else if (isCompositeColliderSetup)
            {
                // For composite collider setups, we need a separate trigger collider
                SetupCompositeColliderIntegration();
            }
            else
            {
                // Standard setup - create a new trigger collider
                terrainCollider = gameObject.AddComponent<BoxCollider2D>();
                terrainCollider.isTrigger = true;
            }
        }
        
        private void SetupCompositeColliderIntegration()
        {
            // Look for existing trigger collider on this GameObject (excluding composite)
            Collider2D[] allColliders = GetComponents<Collider2D>();
            foreach (Collider2D col in allColliders)
            {
                if (col != compositeCollider && col.isTrigger)
                {
                    terrainCollider = col;
                    Debug.Log($"[BreakableTerrain] Using existing trigger collider on {name}");
                    return;
                }
            }
            
            if (autoCreateTriggerChild)
            {
                // Create a simple trigger collider on this GameObject
                CreateTriggerCollider();
            }
            else
            {
                Debug.LogWarning($"[BreakableTerrain] Composite collider detected on {name} but no trigger found. Break detection may not work.");
            }
        }
        
        private void CreateTriggerCollider()
        {
            // For composite colliders, we need to position the trigger based on the composite collider bounds
            if (isCompositeColliderSetup && compositeCollider != null)
            {
                CreateTriggerForCompositeCollider();
            }
            else
            {
                // Create a simple trigger collider on this GameObject
                CreateTriggerForRegularCollider();
            }
        }
        
        private void CreateTriggerForCompositeCollider()
        {
            // Create trigger collider
            BoxCollider2D triggerBox = gameObject.AddComponent<BoxCollider2D>();
            triggerBox.isTrigger = true;
            
            if (autoCalculateFromBounds)
            {
                // Auto-calculate from composite collider bounds
                Bounds compositeBounds = compositeCollider.bounds;
                
                // Size and position based on composite collider bounds
                triggerBox.size = compositeBounds.size;
                
                // Calculate offset from GameObject position to composite bounds center
                Vector3 boundsCenter = compositeBounds.center;
                Vector3 gameObjectPosition = transform.position;
                Vector2 offset = boundsCenter - gameObjectPosition;
                triggerBox.offset = offset;
                
                // Update inspector values to match calculated values
                triggerPosition = offset;
                triggerSize = compositeBounds.size;
                
                Debug.Log($"[BreakableTerrain] Auto-calculated trigger for composite collider on {name}");
                Debug.Log($"  Composite bounds: center={compositeBounds.center}, size={compositeBounds.size}");
                Debug.Log($"  Trigger: offset={offset}, size={triggerBox.size}");
            }
            else
            {
                // Use manual values from inspector
                triggerBox.size = triggerSize;
                triggerBox.offset = triggerPosition;
                
                Debug.Log($"[BreakableTerrain] Created manual trigger for composite collider on {name}");
                Debug.Log($"  Trigger: offset={triggerPosition}, size={triggerSize}");
            }
            
            terrainCollider = triggerBox;
        }
        
        private void CreateTriggerForRegularCollider()
        {
            // Create a simple trigger collider on this GameObject
            BoxCollider2D triggerBox = gameObject.AddComponent<BoxCollider2D>();
            triggerBox.isTrigger = true;
            
            if (autoCalculateFromBounds)
            {
                // Size appropriately - check for sprite renderer first
                SpriteRenderer renderer = GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    // Size based on sprite bounds
                    Bounds spriteBounds = renderer.sprite.bounds;
                    triggerBox.size = spriteBounds.size;
                    triggerBox.offset = Vector2.zero;
                    
                    // Update inspector values
                    triggerSize = spriteBounds.size;
                    triggerPosition = Vector2.zero;
                }
                else
                {
                    // Default to tile size
                    triggerBox.size = Vector2.one;
                    triggerBox.offset = Vector2.zero;
                    
                    // Update inspector values
                    triggerSize = Vector2.one;
                    triggerPosition = Vector2.zero;
                }
                
                Debug.Log($"[BreakableTerrain] Auto-calculated regular trigger on {name} (size: {triggerBox.size})");
            }
            else
            {
                // Use manual values from inspector
                triggerBox.size = triggerSize;
                triggerBox.offset = triggerPosition;
                
                Debug.Log($"[BreakableTerrain] Created manual trigger on {name} (offset: {triggerPosition}, size: {triggerSize})");
            }
            
            terrainCollider = triggerBox;
        }
        
        private void ConfigurePlayerLayers()
        {
            // Try to find player layer
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer == -1)
                playerLayer = LayerMask.NameToLayer("PlayerHitbox");
            if (playerLayer == -1)
                playerLayer = 0; // Default layer fallback
                
            playerLayers = 1 << playerLayer;
            
            Debug.Log($"[BreakableTerrain] Configured to detect player on layer: {LayerMask.LayerToName(playerLayer)} (mask: {playerLayers})");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isBroken) return;
            if (oneTimeOnly && hasBeenBroken) return;

            // Check if it's a player
            if (((1 << other.gameObject.layer) & playerLayers) == 0) return;

            // Get player components for state checking
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController == null) return;

            // Check break conditions
            if (ShouldBreak(other, playerController))
            {
                BreakTerrain(other, playerController);
            }
        }

        private bool ShouldBreak(Collider2D playerCollider, PlayerController playerController)
        {
            // Check specific player state requirements
            if (requireSpecificPlayerState)
            {
                if (requireDashState && !playerController.IsDashing)
                {
                    return false;
                }
            }

            // Get contact direction
            Vector2 contactDirection = GetContactDirection(playerCollider);
            
            // Check if contact direction is allowed
            if (!IsDirectionAllowed(contactDirection)) 
                return false;

            // Check velocity requirement
            if (requireMinimumVelocity)
            {
                Rigidbody2D playerRb = playerController.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    float currentSpeed = playerRb.linearVelocity.magnitude;
                    if (currentSpeed < minimumVelocity)
                        return false;
                }
            }

            return true;
        }

        private Vector2 GetContactDirection(Collider2D playerCollider)
        {
            // Calculate direction from terrain to player
            Vector2 terrainCenter = terrainCollider.bounds.center;
            Vector2 playerCenter = playerCollider.bounds.center;
            
            Vector2 direction = (playerCenter - terrainCenter).normalized;
            return direction;
        }

        private bool IsDirectionAllowed(Vector2 contactDirection)
        {
            if (allowedBreakDirections == BreakDirection.None) return false;
            if (allowedBreakDirections == BreakDirection.All) return true;

            // Convert direction to break direction flags
            BreakDirection contactDir = GetBreakDirectionFromVector(contactDirection);
            
            // Check if the contact direction matches allowed directions
            return (allowedBreakDirections & contactDir) != 0;
        }

        private BreakDirection GetBreakDirectionFromVector(Vector2 direction)
        {
            // Determine primary direction based on strongest component
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);

            if (absX > absY)
            {
                // Horizontal contact
                return direction.x > 0 ? BreakDirection.Right : BreakDirection.Left;
            }
            else
            {
                // Vertical contact  
                return direction.y > 0 ? BreakDirection.Top : BreakDirection.Bottom;
            }
        }

        private void BreakTerrain(Collider2D playerCollider, PlayerController playerController)
        {
            if (isBroken) return;

            isBroken = true;
            hasBeenBroken = true;

            // Disable the trigger collider (but keep composite collider active)
            if (terrainCollider != null && terrainCollider != compositeCollider)
            {
                terrainCollider.enabled = false;
            }
            
            // Simplified - sprite already handled above

            // Handle tilemap integration
            if (parentTilemap != null && tilemapPosition.HasValue)
            {
                // Remove tile from tilemap
                parentTilemap.SetTile(tilemapPosition.Value, null);
                
                // For composite colliders, regenerate the composite shape
                if (isCompositeColliderSetup && compositeCollider != null)
                {
                    StartCoroutine(RegenerateCompositeColliderDelayed());
                }
                
            }

            // Hide sprite if present
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            // Trigger effects
            PlayBreakEffects();

            // Trigger camera shake
            if (addCameraShake)
            {
                TriggerCameraShake();
            }

            // Notify listeners
            OnTerrainBroken?.Invoke(this);

            // Handle restoration
            if (canRestore && restoreTime > 0)
            {
                if (restoreCoroutine != null)
                {
                    StopCoroutine(restoreCoroutine);
                }
                restoreCoroutine = StartCoroutine(RestoreAfterDelay());
            }
        }

        // Removed landing buffer complexity - not needed for breakable terrain

        private void PlayBreakEffects()
        {
            // Spawn break effect
            if (breakEffectPrefab != null)
            {
                GameObject effect = Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
                
                // Auto-destroy effect after some time
                ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
                }
                else
                {
                    Destroy(effect, 5f); // Fallback cleanup
                }
            }

            // Play break sound
            if (breakSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(breakSound);
            }
        }

        private void TriggerCameraShake()
        {
            // Find camera shake component or system
            // This assumes you have a camera shake system - implement as needed
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Simple implementation - you can replace with your camera shake system
                StartCoroutine(CameraShakeCoroutine(mainCamera));
            }
        }

        private IEnumerator CameraShakeCoroutine(Camera camera)
        {
            Vector3 originalPos = camera.transform.position;
            float elapsed = 0f;

            while (elapsed < cameraShakeDuration)
            {
                float x = Random.Range(-1f, 1f) * cameraShakeIntensity;
                float y = Random.Range(-1f, 1f) * cameraShakeIntensity;
                
                camera.transform.position = originalPos + new Vector3(x, y, 0);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            camera.transform.position = originalPos;
        }

        private IEnumerator RestoreAfterDelay()
        {
            yield return new WaitForSeconds(restoreTime);
            RestoreTerrain();
        }

        public void RestoreTerrain()
        {
            if (!isBroken) return;

            isBroken = false;

            // Re-enable the trigger collider
            if (terrainCollider != null && terrainCollider != compositeCollider)
            {
                terrainCollider.enabled = true;
            }
            
            // Simplified - sprite already handled above

            // Restore tilemap tile
            if (parentTilemap != null && tilemapPosition.HasValue && originalTile != null)
            {
                parentTilemap.SetTile(tilemapPosition.Value, originalTile);
                
                // For composite colliders, regenerate the composite shape
                if (isCompositeColliderSetup && compositeCollider != null)
                {
                    StartCoroutine(RegenerateCompositeColliderDelayed());
                }
                
            }

            // Show sprite
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            // Play restore effect
            if (restoreEffectPrefab != null)
            {
                GameObject effect = Instantiate(restoreEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 5f);
            }

            // Notify listeners
            OnTerrainRestored?.Invoke(this);

            restoreCoroutine = null;
        }

        // Public API for external control
        public void ForceBreak()
        {
            if (isBroken) return;
            
            // Find player for breaking context
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                Collider2D playerCollider = player.GetComponent<Collider2D>();
                if (playerCollider != null)
                {
                    BreakTerrain(playerCollider, player);
                }
            }
        }

        public void ForceRestore()
        {
            if (restoreCoroutine != null)
            {
                StopCoroutine(restoreCoroutine);
                restoreCoroutine = null;
            }
            RestoreTerrain();
        }

        public bool IsBroken => isBroken;
        public bool HasBeenBroken => hasBeenBroken;
        
        // Reset for save system integration
        public void ResetState()
        {
            if (restoreCoroutine != null)
            {
                StopCoroutine(restoreCoroutine);
                restoreCoroutine = null;
            }
            
            isBroken = false;
            hasBeenBroken = false;
            
            // Reset collider state
            if (terrainCollider != null && terrainCollider != compositeCollider)
            {
                terrainCollider.enabled = true;
            }
            
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
            
            if (parentTilemap != null && tilemapPosition.HasValue && originalTile != null)
            {
                parentTilemap.SetTile(tilemapPosition.Value, originalTile);
                
                // For composite colliders, regenerate the composite shape
                if (isCompositeColliderSetup && compositeCollider != null)
                {
                    StartCoroutine(RegenerateCompositeColliderDelayed());
                }
            }
        }

        // Editor visualization
        private void OnDrawGizmos()
        {
            if (!showBreakDirectionGizmos) return;

            // Calculate trigger center for gizmo display
            Vector3 triggerCenter = transform.position + (Vector3)triggerPosition;
            
            // Draw trigger bounds
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Vector3 triggerSize3D = new Vector3(triggerSize.x, triggerSize.y, 0.1f);
            Gizmos.DrawCube(triggerCenter, triggerSize3D);
            
            // Draw trigger outline
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(triggerCenter, triggerSize3D);

            // Draw direction indicators from trigger center
            float arrowSize = 0.5f;
            if ((allowedBreakDirections & BreakDirection.Top) != 0)
            {
                Gizmos.DrawRay(triggerCenter, Vector3.up * arrowSize);
                Gizmos.DrawWireCube(triggerCenter + Vector3.up * arrowSize, Vector3.one * 0.1f);
            }
            if ((allowedBreakDirections & BreakDirection.Bottom) != 0)
            {
                Gizmos.DrawRay(triggerCenter, Vector3.down * arrowSize);
                Gizmos.DrawWireCube(triggerCenter + Vector3.down * arrowSize, Vector3.one * 0.1f);
            }
            if ((allowedBreakDirections & BreakDirection.Left) != 0)
            {
                Gizmos.DrawRay(triggerCenter, Vector3.left * arrowSize);
                Gizmos.DrawWireCube(triggerCenter + Vector3.left * arrowSize, Vector3.one * 0.1f);
            }
            if ((allowedBreakDirections & BreakDirection.Right) != 0)
            {
                Gizmos.DrawRay(triggerCenter, Vector3.right * arrowSize);
                Gizmos.DrawWireCube(triggerCenter + Vector3.right * arrowSize, Vector3.one * 0.1f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            OnDrawGizmos();
            
            // Show additional info when selected
            Gizmos.color = Color.yellow;
            if (highlightOnPlayerNear)
            {
                Gizmos.DrawWireSphere(transform.position, highlightDistance);
            }
            
            // Show composite collider info
            if (isCompositeColliderSetup)
            {
                Gizmos.color = Color.cyan;
                Vector3 pos = transform.position + Vector3.up * 0.7f;
                // Unity Editor handles label display
            }
        }
        
        /// <summary>
        /// Regenerates the composite collider shape after tile changes.
        /// Required when tiles are added/removed from composite collider tilemaps.
        /// </summary>
        private IEnumerator RegenerateCompositeColliderDelayed()
        {
            // Wait a frame for tilemap changes to be processed
            yield return null;
            
            if (compositeCollider != null)
            {
                // Force regeneration of composite collider
                compositeCollider.GenerateGeometry();
            }
        }
        
        // Public API for composite collider integration
        public bool IsCompositeColliderSetup => isCompositeColliderSetup;
        public CompositeCollider2D GetCompositeCollider() => compositeCollider;
        public Collider2D GetTriggerCollider() => terrainCollider;
        
        /// <summary>
        /// Manually set up composite collider integration for existing setups
        /// </summary>
        public void SetupForCompositeCollider(CompositeCollider2D composite)
        {
            compositeCollider = composite;
            isCompositeColliderSetup = true;
            
            if (autoCreateTriggerChild && terrainCollider == null)
            {
                CreateTriggerCollider();
            }
        }
    }
}