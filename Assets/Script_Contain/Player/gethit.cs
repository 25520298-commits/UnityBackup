using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GetHit : MonoBehaviour
{
    [Header("Hit Settings")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float iframeDuration = 0.2f;
    
    [Header("Damage")]
    [Tooltip("Sát thương mỗi lần trúng đòn")]
    
    public HealthBar healthBar;

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

    public Shooting dmg;

    private Renderer rend;
    private Color originalColor;

    public int MaxHealth;
    public int currentHealth;

    
    public EnemyBullet _bullet;
   
    void Start()
    {
        healthBar = GetComponent<HealthBar>();
     
        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            if (rend.material.HasProperty("_Color"))
                originalColor = rend.material.color;
            else
                originalColor = Color.white;
        }

        currentHealth = MaxHealth;
    }
    

    private void OnTriggerEnter(Collider hitInfo)
    {
        if (isDead || isInvincible) return;
        if (hitInfo == null) return;

        if (hitInfo.CompareTag("Enemy"))
        {
            TakeHit();
        }
        else if (hitInfo.CompareTag("EnemyBullet"))
        {
            Destroy(hitInfo.gameObject);
            TakeHit();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead || isInvincible) return;
        if (collision == null) return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeHit();
        }
    }

    
    
    public void TakeHit()
    {
        currentHits++;
        Debug.Log($"Player ăn hit {currentHits}/{maxHits}");
        
        healthBar.currentHealth -= _bullet.damage;
        healthBar.healthSlider.value -= _bullet.damage;
        healthBar.UpdateHealthUI();
      
        healthBar.currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        

        // Chết khi hết máu HOẶC hết số lần chịu đòn
        if (currentHealth <= 0 || currentHits >= maxHits)
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