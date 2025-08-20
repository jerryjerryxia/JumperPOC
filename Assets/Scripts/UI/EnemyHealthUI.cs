using UnityEngine;
using UnityEngine.UI;
using Enemies;

namespace UI
{
    [ExecuteInEditMode]
    public class EnemyHealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HealthBarUI healthBar;
        [SerializeField] private Canvas worldCanvas;
        
        [Header("Positioning")]
        [SerializeField] private Vector3 offset = new Vector3(0, 0.35f, 0);
        [SerializeField] private bool followEnemy = true;
        [SerializeField] private bool useDynamicOffset = false;
        [SerializeField] private float additionalOffset = 0f;
        [SerializeField] private float hideDelay = 2f;
        [SerializeField] private bool showOnStart = false;
        [SerializeField] private bool alwaysVisible = false;
        
        [Header("Size Settings")]
        [SerializeField] private Vector2 healthBarSize = new Vector2(100, 20);
        
        private IEnemyBase enemyInterface;
        private Transform enemyTransform;
        private SpriteRenderer enemySprite;
        private Camera mainCamera;
        private float hideTimer;
        private bool isVisible;
        private Vector3 dynamicOffset;
        
        private void Awake()
        {
            if (!Application.isPlaying)
            {
                InitializeComponents();
            }
        }
        
        private void Start()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            
            if (worldCanvas == null)
            {
                worldCanvas = GetComponentInParent<Canvas>();
            }
            
            if (healthBar == null)
            {
                healthBar = GetComponentInChildren<HealthBarUI>();
            }
            
            if (enemyInterface == null)
            {
                // Get enemy using IEnemyBase interface
                var enemyInterfaceComponent = GetComponentInParent<IEnemyBase>();
                if (enemyInterfaceComponent != null)
                {
                    Initialize(enemyInterfaceComponent);
                }
                else if (enemySprite == null)
                {
                    // Fallback: Try to find sprite renderer in parent if no enemy found yet
                    enemySprite = GetComponentInParent<SpriteRenderer>();
                    if (enemyTransform == null && enemySprite != null)
                    {
                        enemyTransform = enemySprite.transform;
                    }
                    CalculateDynamicOffset();
                }
            }
            
            SetupWorldCanvas();
            SetupHealthBarSize();
            
            if (showOnStart || alwaysVisible)
            {
                if (healthBar != null)
                {
                    healthBar.Show();
                    isVisible = true;
                    if (alwaysVisible)
                        hideTimer = -1;
                }
            }
            else
            {
                if (healthBar != null)
                    healthBar.Hide();
                isVisible = false;
            }
        }
        
        public void Initialize(IEnemyBase enemyInterfaceComponent)
        {
            if (enemyInterfaceComponent == null) return;
            
            enemyInterface = enemyInterfaceComponent;
            
            // Get transform from the MonoBehaviour that implements the interface
            if (enemyInterfaceComponent is MonoBehaviour monoBehaviour)
            {
                enemyTransform = monoBehaviour.transform;
                enemySprite = monoBehaviour.GetComponent<SpriteRenderer>();
            }
            
            if (enemyInterface != null && healthBar != null)
            {
                // Subscribe to interface events
                enemyInterface.OnHealthChanged -= UpdateHealthBar;
                enemyInterface.OnDamageTaken -= ShowHealthBar;
                enemyInterface.OnHealthChanged += UpdateHealthBar;
                enemyInterface.OnDamageTaken += ShowHealthBar;
                
                float maxHealth = enemyInterface.GetMaxHealth();
                float currentHealth = enemyInterface.GetCurrentHealth();
                
                healthBar.SetMaxHealth(maxHealth);
                healthBar.SetHealth(currentHealth);
                
                if (showOnStart || alwaysVisible)
                {
                    healthBar.Show();
                    isVisible = true;
                }
            }
            
            CalculateDynamicOffset();
        }
        
        private void Update()
        {
            if (!Application.isPlaying)
            {
                // In editor mode
                if (enemyInterface == null && enemyTransform == null)
                {
                    // Try interface approach in editor
                    var parentEnemyInterface = GetComponentInParent<IEnemyBase>();
                    if (parentEnemyInterface != null && parentEnemyInterface is MonoBehaviour mb)
                    {
                        enemyTransform = mb.transform;
                        enemySprite = mb.GetComponent<SpriteRenderer>();
                        CalculateDynamicOffset();
                    }
                }
                
                // Always update position in editor for preview
                if (followEnemy && enemyTransform != null)
                {
                    Vector3 currentOffset = useDynamicOffset ? dynamicOffset : offset;
                    transform.position = enemyTransform.position + currentOffset;
                }
                
                return; // Skip the rest in editor mode
            }
            
            if (Application.isPlaying && enemyInterface == null)
            {
                Destroy(gameObject);
                return;
            }
            
            if (followEnemy && enemyTransform != null)
            {
                Vector3 currentOffset = useDynamicOffset ? dynamicOffset : offset;
                transform.position = enemyTransform.position + currentOffset;
            }
            
            if (mainCamera != null)
            {
                transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up);
            }
            
            if (isVisible && hideTimer > 0 && !alwaysVisible)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0)
                {
                    healthBar.Hide();
                    isVisible = false;
                }
            }
        }
        
        private void OnDestroy()
        {
            if (enemyInterface != null)
            {
                enemyInterface.OnHealthChanged -= UpdateHealthBar;
                enemyInterface.OnDamageTaken -= ShowHealthBar;
            }
        }
        
        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (healthBar != null)
            {
                healthBar.UpdateHealth(currentHealth, maxHealth);
            }
        }
        
        private void ShowHealthBar()
        {
            if (healthBar != null)
            {
                healthBar.Show();
                isVisible = true;
                hideTimer = hideDelay;
            }
        }
        
        private void SetupWorldCanvas()
        {
            if (worldCanvas == null)
            {
                // Check if we already have a canvas as a child
                worldCanvas = GetComponentInChildren<Canvas>();
                
                if (worldCanvas == null)
                {
                    GameObject canvasObject = new GameObject("EnemyHealthCanvas");
                    canvasObject.transform.SetParent(transform);
                    canvasObject.transform.localPosition = Vector3.zero;
                    
                    worldCanvas = canvasObject.AddComponent<Canvas>();
                    worldCanvas.renderMode = RenderMode.WorldSpace;
                    worldCanvas.sortingLayerName = "Default"; // Use Default if UI layer doesn't exist
                    worldCanvas.sortingOrder = 10;
                    
                    CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                    scaler.dynamicPixelsPerUnit = 10;
                    
                }
            }
        }
        
        private void SetupHealthBarSize()
        {
            RectTransform rectTransform = healthBar.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = healthBarSize;
                rectTransform.localScale = Vector3.one * 0.01f;
            }
        }
        
        public void ShowPermanently()
        {
            if (healthBar != null)
            {
                healthBar.Show();
                isVisible = true;
                hideTimer = -1;
            }
        }
        
        private void CalculateDynamicOffset()
        {
            if (!useDynamicOffset || enemySprite == null)
            {
                dynamicOffset = offset;
                return;
            }
            
            // Calculate the offset based on the sprite's bounds
            Bounds spriteBounds = enemySprite.bounds;
            float spriteTop = spriteBounds.max.y - enemyTransform.position.y;
            
            // Set the dynamic offset to position the health bar above the sprite
            dynamicOffset = new Vector3(0, spriteTop + additionalOffset, 0);
            
            // If manual offset was set, use it as a minimum
            if (offset.y > dynamicOffset.y)
            {
                dynamicOffset.y = offset.y;
            }
            
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Debug.Log($"[Editor] EnemyHealthUI - Sprite bounds: {spriteBounds}, Sprite top: {spriteTop}, Dynamic offset: {dynamicOffset}, Transform pos: {enemyTransform.position}");
            }
#endif
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Update position in editor when values change
            if (!Application.isPlaying)
            {
                if (enemyTransform == null)
                {
                    // Try interface approach in editor
                    var parentEnemyInterface = GetComponentInParent<IEnemyBase>();
                    if (parentEnemyInterface != null && parentEnemyInterface is MonoBehaviour mb)
                    {
                        enemyTransform = mb.transform;
                        enemySprite = mb.GetComponent<SpriteRenderer>();
                    }
                }
                
                if (enemySprite != null && enemyTransform != null)
                {
                    CalculateDynamicOffset();
                }
                
                // Update visibility settings in editor
                if (healthBar == null)
                {
                    healthBar = GetComponentInChildren<HealthBarUI>();
                }
                
                if (healthBar != null)
                {
                    // Show in editor if alwaysVisible is true for testing
                    if (alwaysVisible)
                    {
                        healthBar.Show();
                    }
                    else if (!showOnStart)
                    {
                        healthBar.Hide();
                    }
                }
            }
        }
#endif
    }
}