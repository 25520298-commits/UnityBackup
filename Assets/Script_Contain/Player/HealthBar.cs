using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nếu bạn muốn hiển thị cả chữ số máu

public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Slider slider;
    public TextMeshProUGUI healthText; // (Tùy chọn) Kéo Text hiển thị số máu vào đây

    public float currentHealth { get; private set; }
    private float maxHealth;

    // Hàm khởi tạo thanh máu (Gọi 1 lần khi bắt đầu game)
    public void SetMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
        this.currentHealth = maxHealth;
        
        if (slider != null)
        {
            slider.maxValue = maxHealth;
            slider.value = maxHealth;
        }

        UpdateHealthText();
    }

    // Hàm trừ máu khi bị bắn
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return; // Đã chết thì không trừ nữa

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0; // Không để máu âm

        if (slider != null)
        {
            slider.value = currentHealth;
        }

        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }
}