using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GetHit : MonoBehaviour
{
    [Header("Hit Settings")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float iframeDuration = 0.2f;
    
    [Header("Health")]
    [Tooltip("Kéo thả HealthBar (trên UI Canvas) vào đây trong Inspector")]
    public HealthBar healthBar;

    [Header("Damage")]
    [Tooltip("Sát thương mặc định nếu không lấy được từ EnemyBullet")]
    [SerializeField] private float defaultDamage = 10f;

    [Header("Explosion")]
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private float explosionRange = 3f;

    [Header("Death")]
    [Tooltip("Thời gian chờ trước khi xoá Player (giây). " +
             "Để 0 nếu xoá ngay lập tức.")]
    [SerializeField] private float destroyDelay = 1.5f;

    private int currentHits = 0;
    private bool isInvincible = false;
    private bool isDead = false;

    private Renderer rend;
    private Color originalColor;

    void Start()
    {
        // HealthBar nằm trên UI Canvas, KHÔNG nằm trên Player
        // → phải gán bằng Inspector (kéo thả), KHÔNG dùng GetComponent
        if (healthBar == null)
        {
            Debug.LogError("GetHit: HealthBar chưa được gán! Hãy kéo thả HealthBar vào Inspector.", this);
        }

        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            if (rend.material.HasProperty("_Color"))
                originalColor = rend.material.color;
            else
                originalColor = Color.white;
        }
    }

    private void OnTriggerEnter(Collider hitInfo)
    {
        if (isDead || isInvincible) return;
        if (hitInfo == null) return;

        if (hitInfo.CompareTag("Enemy"))
        {
            TakeHit(defaultDamage);
        }
        else if (hitInfo.CompareTag("EnemyBullet"))
        {
            // Lấy damage từ viên đạn trước khi destroy
            float damage = defaultDamage;
            EnemyBullet bullet = hitInfo.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                damage = bullet.damage;
            }

            Destroy(hitInfo.gameObject);
            TakeHit(damage);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead || isInvincible) return;
        if (collision == null) return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeHit(defaultDamage);
        }
        else if (collision.gameObject.CompareTag("EnemyBullet"))
        {
            float damage = defaultDamage;
            EnemyBullet bullet = collision.gameObject.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                damage = bullet.damage;
            }

            Destroy(collision.gameObject);
            TakeHit(damage);
        }
    }

    public void TakeHit(float damage)
    {
        currentHits++;
        Debug.Log($"Player ăn hit {currentHits}/{maxHits}");

        // Trừ máu qua HealthBar
        if (healthBar != null)
        {
            healthBar.TakeDamage(damage);
        }

        // Chết khi hết máu HOẶC hết số lần chịu đòn
        if ((healthBar != null && healthBar.currentHealth <= 0) || currentHits >= maxHits)
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
            if (rend != null && rend.material.HasProperty("_Color"))
                rend.material.color = Color.red;

            yield return new WaitForSeconds(iframeDuration * 0.5f);

            if (rend != null && rend.material.HasProperty("_Color"))
                rend.material.color = originalColor;

            yield return new WaitForSeconds(iframeDuration * 0.5f);
        }

        isInvincible = false;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player chết!");

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Controller pc = GetComponent<Controller>();
        if (pc != null) pc.enabled = false;

        if (destroyDelay <= 0f)
            Destroy(gameObject);
        else
            Destroy(gameObject, destroyDelay);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }
}