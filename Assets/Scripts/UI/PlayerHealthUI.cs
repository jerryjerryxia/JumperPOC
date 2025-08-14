using UnityEngine;
using Player;

namespace UI
{
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HealthBarUI healthBar;
        [SerializeField] private PlayerHealth playerHealth;
        
        [Header("UI Settings")]
        [SerializeField] private Vector2 anchorPosition = new Vector2(10, -10);
        [SerializeField] private Vector2 sizeDelta = new Vector2(300, 40);
        
        private void Start()
        {
            gameObject.layer = LayerMask.NameToLayer("UI");
            
            if (playerHealth == null)
            {
                // Try multiple ways to find the player
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    // Fallback: try to find by name
                    player = GameObject.Find("Player");
                }
                if (player == null)
                {
                    // Fallback: find any PlayerHealth component in scene
                    playerHealth = GameObject.FindFirstObjectByType<PlayerHealth>();
                }
                else
                {
                    playerHealth = player.GetComponent<PlayerHealth>();
                }
            }
            
            if (healthBar == null)
            {
                healthBar = GetComponentInChildren<HealthBarUI>();
            }
            
            if (playerHealth != null && healthBar != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthBar;
                playerHealth.OnHealthChanged += UpdateHealthBar;
                
                float maxHealth = playerHealth.GetMaxHealth();
                float currentHealth = playerHealth.GetCurrentHealth();
                
                // Debug.Log($"PLAYER HEALTH DEBUG: maxHealth={maxHealth}, currentHealth={currentHealth}");
                
                healthBar.SetMaxHealth(maxHealth);
                healthBar.SetHealth(currentHealth);
            }
            else
            {
                // Debug.LogError($"PLAYER HEALTH UI FAILED: playerHealth={playerHealth}, healthBar={healthBar}");
            }
            
            SetupUIPosition();
        }
        
        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthBar;
            }
        }
        
        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (healthBar != null)
            {
                healthBar.UpdateHealth(currentHealth, maxHealth);
            }
        }
        
        private void SetupUIPosition()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.anchoredPosition = anchorPosition;
                rectTransform.sizeDelta = sizeDelta;
            }
        }
    }
}