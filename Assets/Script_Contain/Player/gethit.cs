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

    [Header("Respawn Settings")]
    public Transform spawnPoint; 
    public float respawnDelay = 1.5f; 

    private bool isInvincible = false;
    private bool isDead = false;

    private Renderer rend;
    private Collider col; 
    private Color originalColor;
    
    // THÊM BIẾN NÀY: Để lưu lại trạng thái hiển thị của súng, tay, v.v.
    private Transform[] allChildren;
    private bool[] childrenActiveStates; 

    void Start()
    {
        playerController = GetComponent<Controller>();
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();

        if (healthBar == null)
            Debug.LogError("GetHit: HealthBar chưa được gán!", this);
        else if (playerController != null)
            healthBar.SetMaxHealth(playerController.maxHealth);

        if (rend != null && rend.material.HasProperty("_Color"))
            originalColor = rend.material.color;

        // Lưu lại danh sách tất cả các object con (súng, pivot, muzzle...)
        allChildren = GetComponentsInChildren<Transform>(true); 
        childrenActiveStates = new bool[allChildren.Length];
    }

    private void OnTriggerEnter(Collider hitInfo)
    {
        if (isDead || isInvincible) return;
        if (hitInfo == null) return;

        if (hitInfo.CompareTag("EnemyBullet"))
        {
            float damage = defaultDamage;
            EnemyBullet bullet = hitInfo.GetComponent<EnemyBullet>();
            if (bullet != null) damage = bullet.damage;

            Destroy(hitInfo.gameObject);
            TakeHit(damage);
        }
        else if (hitInfo.CompareTag("Enemy"))
        {
            TakeHit(defaultDamage);
        }
    }

    public void TakeHit(float damage)
    {
        if (isDead) return;

        if (healthBar != null)
            healthBar.TakeDamage(damage); 

        if (healthBar != null && healthBar.currentHealth <= 0)
            StartCoroutine(DieAndRespawn());
        else
            StartCoroutine(Invincible());
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

    // ==================== LOGIC CHẾT VÀ HỒI SINH ====================
    private IEnumerator DieAndRespawn()
    {
        if (isDead) yield break;
        isDead = true;
        Debug.Log("<color=red>[Player] Đã chết! Đang chờ hồi sinh...</color>");

        // 1. Vô hiệu hóa Player
        if (rend != null) rend.enabled = false;
        if (col != null) col.enabled = false;
        
        if (playerController != null) 
        {
            playerController.enabled = false;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        // --- SỬA LỖI SÚNG LƠ LỬNG: Tắt toàn bộ object con (Súng, Pivot, Muzzle...) ---
        for (int i = 0; i < allChildren.Length; i++)
        {
            // Bỏ qua chính object Player (để không bị lỗi khi bật lại)
            if (allChildren[i] == transform) continue; 

            // Lưu lại trạng thái hiện tại (đang bật hay tắt)
            childrenActiveStates[i] = allChildren[i].gameObject.activeSelf;
            
            // Tắt object con
            allChildren[i].gameObject.SetActive(false);
        }

        // 2. Chờ 1.5 giây
        yield return new WaitForSeconds(respawnDelay);

        // 3. Hồi sinh
        Respawn();
    }

    private void Respawn()
    {
        // Dịch chuyển về Spawn Point
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        // Hồi đầy máu
        if (healthBar != null && playerController != null)
            healthBar.SetMaxHealth(playerController.maxHealth);

        // Bật lại Player
        if (rend != null) rend.enabled = true;
        if (col != null) col.enabled = true;
        if (playerController != null) playerController.enabled = true;

        // --- KHÔI PHỤC LẠI CÁC OBJECT CON (Súng, Pivot...) ---
        for (int i = 0; i < allChildren.Length; i++)
        {
            if (allChildren[i] == transform) continue;

            // Chỉ bật lại những object mà lúc trước đang bật (ví dụ Muzzle Flash đang tắt thì vẫn tắt)
            if (childrenActiveStates[i])
                allChildren[i].gameObject.SetActive(true);
        }

        isDead = false;
        isInvincible = false; 
        Debug.Log("<color=green>[Player] Đã hồi sinh!</color>");
    }
}