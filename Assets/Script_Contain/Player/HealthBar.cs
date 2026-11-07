using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public float maxHealth = 100.0f;
    public float currentHealth;
    public Slider healthSlider;

    void Start()
    {
        // Nếu chưa gán trong Inspector, tự tìm trên chính GameObject này
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
    }

    public void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth; // Slider dùng 0..1
        }
    }

    void Update()
    {
        // Chỉ cập nhật UI nếu slider tồn tại và giá trị chưa đồng bộ
        if (healthSlider != null && !Mathf.Approximately(healthSlider.value, currentHealth / maxHealth))
        {
            UpdateHealthUI();
        }
    }
}
