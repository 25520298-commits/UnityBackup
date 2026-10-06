using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // ==================== MOVEMENT ====================
    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float lineOfSight = 6f;

    // ==================== WANDER ====================
    [Header("Wander")]
    public Transform moveSpots;
    [SerializeField] private float startWaitTime = 3f;
    [SerializeField] private float minX = -10f, maxX = 10f;
    [SerializeField] private float minZ = -10f, maxZ = 10f;

    // ==================== TARGET ====================
    [Header("Target")]
    public Transform player;

    // ==================== HIT ====================
    [Header("Hit Settings")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float iframeDuration = 0.2f;

    // ==================== PRIVATE ====================
    private Rigidbody rb;
    private Renderer rend;
    private Collider col;
    private Color originalColor;

    private Vector3 moveDirection;
    private float waitTime;

    private int currentHits = 0;
    private bool isInvincible = false;
    private bool isDead = false;

    // ==================== PUBLIC API (cho EnemyShooting) ====================
    public bool IsDead => isDead;
    public Transform Player => player;

    // ==================== START ====================
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();

        if (rb == null)
        {
            Debug.LogError($"[{name}] EnemyController cần Rigidbody!");
            enabled = false;
            return;
        }

        // Khóa trục Y và rotation X/Z
        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;

        // Lưu màu gốc
        if (rend != null)
        {
            if (rend.material.HasProperty("_Color"))
                originalColor = rend.material.color;
            else if (rend.material.HasProperty("_BaseColor"))
                originalColor = rend.material.GetColor("_BaseColor");
            else
                originalColor = Color.white;
        }

        // Tìm Player nếu chưa gán
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning($"[{name}] Không tìm thấy Player!");
        }

        waitTime = startWaitTime;

        if (moveSpots != null)
            moveSpots.position = new Vector3(
                Random.Range(minX, maxX), 0f, Random.Range(minZ, maxZ));
    }

    // ==================== FIXED UPDATE ====================
    void FixedUpdate()
    {
        if (isDead) return;

        // Ép Y = 1
        Vector3 lockedPos = rb.position;
        lockedPos.y = 1f;

        if (player == null)
        {
            rb.MovePosition(lockedPos);
            return;
        }

        float distanceToPlayer = Vector3.Distance(player.position, lockedPos);

        // === CHASE PLAYER ===
        if (distanceToPlayer < lineOfSight)
        {
            Vector3 dir = player.position - lockedPos;
            dir.y = 0f;
            moveDirection = dir.normalized;
            FaceTarget(player.position);
        }
        // === WANDER ===
        else if (moveSpots != null)
        {
            Vector3 dir = moveSpots.position - lockedPos;
            dir.y = 0f;
            moveDirection = dir.normalized;

            if (moveDirection.sqrMagnitude > 0.0001f)
                FaceTarget(moveSpots.position);

            if (Vector3.Distance(lockedPos, moveSpots.position) < 0.3f)
            {
                moveDirection = Vector3.zero;

                if (waitTime <= 0f)
                {
                    moveSpots.position = new Vector3(
                        Random.Range(minX, maxX), 0f, Random.Range(minZ, maxZ));
                    waitTime = startWaitTime;
                }
                else
                {
                    waitTime -= Time.fixedDeltaTime;
                }
            }
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        // Di chuyển
        Vector3 newPos = lockedPos + moveDirection * speed * Time.fixedDeltaTime;
        newPos.y = 1f;
        rb.MovePosition(newPos);
    }

    // ==================== HIT DETECTION ====================
    private void OnTriggerEnter(Collider hitInfo)
    {
        if (isDead || isInvincible || hitInfo == null) return;

        if (hitInfo.CompareTag("Bullet"))
        {
            Debug.Log($"[{name}] Ăn hit từ Bullet!");
            Destroy(hitInfo.gameObject);
            TakeHit();
        }
    }

    // ==================== HIT LOGIC ====================
    public void TakeHit()
    {
        if (isDead) return;

        currentHits++;
        Debug.Log($"[{name}] Ăn hit {currentHits}/{maxHits}");

        if (currentHits >= maxHits)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibleRoutine());
        }
    }

    private IEnumerator InvincibleRoutine()
    {
        isInvincible = true;

        for (int i = 0; i < 3; i++)
        {
            SetColor(Color.red);
            yield return new WaitForSeconds(iframeDuration * 0.5f);
            SetColor(originalColor);
            yield return new WaitForSeconds(iframeDuration * 0.5f);
        }

        isInvincible = false;
    }

    private void SetColor(Color color)
    {
        if (rend == null) return;

        if (rend.material.HasProperty("_Color"))
            rend.material.color = color;
        else if (rend.material.HasProperty("_BaseColor"))
            rend.material.SetColor("_BaseColor", color);
    }

    // ==================== DEATH ====================
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[{name}] Chết!");

        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Delay 1 frame để mọi thứ update xong, rồi huỷ
        Destroy(gameObject, 0.1f);
    }

    // ==================== HELPERS ====================
    private void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - rb.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        rb.MoveRotation(targetRot);
    }

    // ==================== GIZMOS ====================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, lineOfSight);
    }
}