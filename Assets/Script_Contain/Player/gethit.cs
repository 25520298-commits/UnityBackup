using System.Collections;
using UnityEngine;

public class GetHit : MonoBehaviour
{
    [Header("References")]
    public HealthBar healthBar;
    private Controller playerController;
    
    [Header("Hit Settings")]
    [SerializeField] private float iframeDuration = 0.5f; 
    [SerializeField] private float defaultDamage = 10f;

    private bool isInvincible = false;
    private bool isDead = false;

    private Renderer rend;
    private Color originalColor;

    void Start()
    {
        playerController = GetComponent<Controller>();
        
        if (healthBar == null)
        {
            Debug.LogError("GetHit: HealthBar chưa được gán!", this);
        }
        else if (playerController != null)
        {
            healthBar.SetMaxHealth(playerController.maxHealth);
        }

        rend = GetComponent<Renderer>();
        if (rend != null && rend.material.HasProperty("_Color"))
        {
            originalColor = rend.material.color;
        }
    }

    private void OnTriggerEnter(Collider hitInfo)
    {
        // --- DEBUG LOG: Kiểm tra xem có va chạm không ---
        Debug.Log($"<color=cyan>[GetHit] Player bị chạm bởi: {hitInfo.gameObject.name} | Tag: {hitInfo.tag}</color>");

        if (isDead || isInvincible) return;
        if (hitInfo == null) return;

        // Nhận sát thương từ đạn của Enemy
        if (hitInfo.CompareTag("EnemyBullet"))
        {
            Debug.Log("<color=red>[GetHit] Đã nhận diện đạn Enemy! Tiến hành trừ máu.</color>");
            
            float damage = defaultDamage;
            EnemyBullet bullet = hitInfo.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                damage = bullet.damage; 
            }

            Destroy(hitInfo.gameObject);
            TakeHit(damage);
        }
        // Va chạm với cơ thể Enemy
        else if (hitInfo.CompareTag("Enemy"))
        {
            Debug.Log("<color=orange>[GetHit] Va chạm với cơ thể Enemy! Tiến hành trừ máu.</color>");
            TakeHit(defaultDamage);
        }
        else
        {
            Debug.Log($"<color=yellow>[GetHit] Va chạm với object không xác định: {hitInfo.tag}</color>");
        }
    }

    public void TakeHit(float damage)
    {
        if (isDead) return;

        Debug.Log($"<color=red>[GetHit] Nhận {damage} sát thương!</color>");

        if (healthBar != null)
        {
            healthBar.TakeDamage(damage); 
        }

        if (healthBar != null && healthBar.currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(Invincible());
        }
    }

    private IEnumerator Invincible()
    {
        isInvincible = true;
        for (int i = 0; i < 3; i++)
        {
            if (rend != null && rend.material.HasProperty("_Color")) rend.material.color = Color.red;
            yield return new WaitForSeconds(iframeDuration * 0.5f);
            if (rend != null && rend.material.HasProperty("_Color")) rend.material.color = originalColor;
            yield return new WaitForSeconds(iframeDuration * 0.5f);
        }
        isInvincible = false;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("<color=red>[GetHit] Player đã chết!</color>");

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Controller pc = GetComponent<Controller>();
        if (pc != null) pc.enabled = false;
    }
}