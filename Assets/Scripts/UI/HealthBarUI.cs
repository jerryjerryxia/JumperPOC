using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [Header("Health Bar Components")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text healthText;
        
        [Header("Visual Settings")]
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color midHealthColor = Color.yellow;
        [SerializeField] private Color lowHealthColor = Color.red;
        [SerializeField] private float colorTransitionThresholdHigh = 0.7f;
        [SerializeField] private float colorTransitionThresholdLow = 0.3f;
        
        [Header("Animation")]
        [SerializeField] private float fillSpeed = 2f;
        [SerializeField] private bool animateFill = true;
        
        private float targetFillAmount;
        private float currentHealth;
        private float maxHealth;
        
        private void Awake()
        {
            gameObject.layer = LayerMask.NameToLayer("UI");
            
            if (healthSlider == null)
                healthSlider = GetComponentInChildren<Slider>();
                
            if (fillImage == null && healthSlider != null)
                fillImage = healthSlider.fillRect.GetComponent<Image>();
                
            if (healthText == null)
                healthText = GetComponentInChildren<Text>();
        }
        
        private void Update()
        {
            if (animateFill && healthSlider != null)
            {
                float currentFill = healthSlider.value;
                if (Mathf.Abs(currentFill - targetFillAmount) > 0.01f)
                {
                    healthSlider.value = Mathf.Lerp(currentFill, targetFillAmount, Time.deltaTime * fillSpeed);
                    UpdateHealthColor();
                }
            }
        }
        
        public void SetMaxHealth(float maxHealth)
        {
            this.maxHealth = maxHealth;
            this.currentHealth = maxHealth;
            
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = maxHealth;
                targetFillAmount = maxHealth;
            }
            
            UpdateHealthText();
            UpdateHealthColor();
        }
        
        public void SetHealth(float health)
        {
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            targetFillAmount = currentHealth;
            
            if (!animateFill && healthSlider != null)
            {
                healthSlider.value = targetFillAmount;
                UpdateHealthColor();
            }
            
            UpdateHealthText();
        }
        
        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            this.currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            this.maxHealth = maxHealth;
            
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                targetFillAmount = currentHealth;
                
                if (!animateFill)
                {
                    healthSlider.value = targetFillAmount;
                    UpdateHealthColor();
                }
            }
            
            UpdateHealthText();
        }
        
        private void UpdateHealthColor()
        {
            if (fillImage == null || healthSlider == null) return;
            
            float healthPercentage = healthSlider.value / healthSlider.maxValue;
            
            if (healthPercentage > colorTransitionThresholdHigh)
            {
                fillImage.color = Color.Lerp(midHealthColor, fullHealthColor, 
                    (healthPercentage - colorTransitionThresholdHigh) / (1f - colorTransitionThresholdHigh));
            }
            else if (healthPercentage > colorTransitionThresholdLow)
            {
                fillImage.color = Color.Lerp(lowHealthColor, midHealthColor, 
                    (healthPercentage - colorTransitionThresholdLow) / (colorTransitionThresholdHigh - colorTransitionThresholdLow));
            }
            else
            {
                fillImage.color = lowHealthColor;
            }
        }
        
        private void UpdateHealthText()
        {
            if (healthText != null)
            {
                int displayCurrentHealth = Mathf.CeilToInt(currentHealth);
                int displayMaxHealth = Mathf.CeilToInt(maxHealth);
                healthText.text = $"{displayCurrentHealth}/{displayMaxHealth}";
            }
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}