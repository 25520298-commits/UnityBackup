using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public GetHit getHit;
    public int MaxHealth;
    public int currentHealth;
    public Slider healthSlider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = MaxHealth;
        UpdateHealthUI();
    }
    
    
    public void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (healthSlider.value != currentHealth)
        {
            UpdateHealthUI();
        }
        
        if (healthSlider.value == 0)
        {
            Die();
        }
    }

    void Die()
    {
        getHit.Die();
        
    }
}
