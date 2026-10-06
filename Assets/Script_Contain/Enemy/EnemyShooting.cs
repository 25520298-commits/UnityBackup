using UnityEngine;

public class EnemyShooting : MonoBehaviour
{
    // ==================== REFERENCES ====================
    [Header("References")]
    public Transform rotationPoint;
    public GameObject bullet;
    public Transform bulletTransform;

    [Header("Muzzle Offset")]
    public float muzzleOffset = 0.5f;

    // ==================== DETECTION ====================
    [Header("Detection")]
    public float detectionRange = 8f;
    public float reactionTime = 0.3f;

    // ==================== FIRING ====================
    [Header("Firing")]
    public float timeBetweenBullets = 1f;
    public float bulletSpeed = 10f;

    // ==================== BURST ====================
    [Header("Burst / Multi-shot")]
    public int bulletsPerShot = 1;
    public float delayBetweenBullets = 0f;

    // ==================== SPREAD ====================
    [Header("Spread")]
    public float baseSpread = 0f;
    public float maxSpread = 10f;
    public float spreadIncreasePerShot = 1f;
    public float spreadDecreaseRate = 8f;

    // ==================== AIMING ====================
    [Header("Aiming")]
    [Tooltip("Bù hướng sprite súng. Đổi 90 → -90 → 180 → 0 nếu súng xoay sai")]
    public float rotationOffset = 90f;   // ✅ ĐÃ SỬA: từ -90f thành 90f
    public float rotationSpeed = 0f;

    // ==================== DEBUG ====================
    [Header("Debug")]
    public bool debugMode = true;
    public float debugLogInterval = 0.5f;

    // ==================== PRIVATE ====================
    private EnemyController enemyController;
    private Transform playerTarget;

    private bool canFire = true;
    private float fireTimer = 0f;

    private float currentSpread = 0f;
    private int bulletsFiredInBurst = 0;
    private float burstTimer = 0f;
    private bool isBursting = false;

    private float reactionTimer = 0f;
    private bool hasReacted = false;
    private bool playerInRange = false;

    private float debugTimer = 0f;
    private int fireCount = 0;

    // ==================== START ====================
    void Start()
    {
        Debug.Log($"<color=cyan>[{name}] ===== EnemyShooting START ====</color>");

        enemyController = GetComponent<EnemyController>();
        Debug.Log($"[{name}] enemyController: {(enemyController != null ? "✅ OK" : "❌ NULL")}");

        if (rotationPoint == null)
        {
            rotationPoint = transform;
            Debug.LogWarning($"[{name}] rotationPoint NULL → dùng transform");
        }
        Debug.Log($"[{name}] rotationPoint: {rotationPoint.name}");

        if (bulletTransform == null)
        {
            bulletTransform = rotationPoint;
            Debug.LogWarning($"[{name}] bulletTransform NULL → dùng rotationPoint");
        }
        Debug.Log($"[{name}] bulletTransform: {bulletTransform.name}");

        if (bullet == null)
            Debug.LogError($"[{name}] ❌ bullet prefab NULL — kéo prefab vào field Bullet!");
        else
            Debug.Log($"[{name}] bullet prefab: {bullet.name}");

        currentSpread = baseSpread;

        if (enemyController != null && enemyController.Player != null)
        {
            playerTarget = enemyController.Player;
            Debug.Log($"[{name}] playerTarget từ EnemyController: {playerTarget.name}");
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
                Debug.Log($"[{name}] playerTarget từ tag: {playerTarget.name}");
            }
            else
            {
                Debug.LogError($"[{name}] ❌ Không tìm thấy Player! Gán tag 'Player' cho Player.");
            }
        }

        Debug.Log($"<color=cyan>[{name}] ===== KẾT THÚC START =====</color>");
    }

    // ==================== UPDATE ====================
    void Update()
    {
        if (enemyController != null && enemyController.IsDead)
        {
            if (fireCount >= 0)
            {
                Debug.LogWarning($"[{name}] Enemy đã chết → EnemyShooting ngừng hoạt động");
                fireCount = -1;
            }
            return;
        }

        HandleDetection();
        HandleBurst();
        HandleCooldown();
        HandleSpreadRecovery();
    }

    // ==================== DETECTION & AIMING ====================
    void HandleDetection()
    {
        if (playerTarget == null)
        {
            LogPeriodic($"❌ playerTarget NULL");
            return;
        }
        if (bulletTransform == null)
        {
            LogPeriodic($"❌ bulletTransform NULL");
            return;
        }

        Vector3 toPlayer = playerTarget.position - rotationPoint.position;
        toPlayer.y = 0f;
        float distanceToPlayer = toPlayer.magnitude;

        bool wasInRange = playerInRange;
        playerInRange = distanceToPlayer <= detectionRange
                        && toPlayer.sqrMagnitude > 0.001f;

        if (playerInRange != wasInRange)
        {
            if (playerInRange)
                Debug.Log($"<color=green>[{name}] ✅ Player VÀO tầm bắn (dist={distanceToPlayer:F2}, range={detectionRange})</color>");
            else
                Debug.Log($"<color=orange>[{name}] ⚠ Player RA khỏi tầm bắn (dist={distanceToPlayer:F2}, range={detectionRange})</color>");
        }

        if (!playerInRange)
        {
            hasReacted = false;
            reactionTimer = 0f;
            LogPeriodic($"Ngoài tầm: dist={distanceToPlayer:F2}, range={detectionRange}");
            return;
        }

        // ✅ Tính góc — Atan2(x, z) + rotationOffset (đã đổi thành +90)
        float targetAngle = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg + rotationOffset;
        Quaternion targetRot = Quaternion.Euler(0f, targetAngle, 0f);

        if (rotationSpeed > 0f)
            rotationPoint.rotation = Quaternion.RotateTowards(
                rotationPoint.rotation, targetRot, rotationSpeed * Time.deltaTime);
        else
            rotationPoint.rotation = targetRot;

        if (!hasReacted)
        {
            reactionTimer += Time.deltaTime;
            LogPeriodic($"Đang reaction: {reactionTimer:F2}/{reactionTime:F2}");
            if (reactionTimer >= reactionTime)
            {
                hasReacted = true;
                Debug.Log($"<color=green>[{name}] ✅ Đã reaction xong → bắt đầu bắn</color>");
            }
            else
                return;
        }

        if (canFire && !isBursting)
        {
            canFire = false;
            Debug.Log($"[{name}] 🔫 Gọi StartBurst()");
            StartBurst();
        }
        else
        {
            LogPeriodic($"Chờ: canFire={canFire}, isBursting={isBursting}");
        }
    }

    // ==================== COOLDOWN ====================
    void HandleCooldown()
    {
        if (!canFire)
        {
            fireTimer += Time.deltaTime;
            if (fireTimer > timeBetweenBullets)
            {
                canFire = true;
                fireTimer = 0f;
                Debug.Log($"[{name}] ✅ Cooldown xong, có thể bắn tiếp");
            }
        }
    }

    // ==================== SPREAD RECOVERY ====================
    void HandleSpreadRecovery()
    {
        if (!playerInRange)
        {
            currentSpread = Mathf.MoveTowards(
                currentSpread, baseSpread, spreadDecreaseRate * Time.deltaTime);
        }
    }

    // ==================== BURST ====================
    void StartBurst()
    {
        if (bulletsPerShot <= 1)
        {
            FireBullet();
            return;
        }

        isBursting = true;
        bulletsFiredInBurst = 0;
        burstTimer = 0f;

        FireBullet();
        bulletsFiredInBurst++;
    }

    void HandleBurst()
    {
        if (!isBursting) return;

        if (delayBetweenBullets <= 0f)
        {
            while (bulletsFiredInBurst < bulletsPerShot)
            {
                FireBullet();
                bulletsFiredInBurst++;
            }
            isBursting = false;
            return;
        }

        burstTimer += Time.deltaTime;
        if (burstTimer >= delayBetweenBullets)
        {
            burstTimer = 0f;
            FireBullet();
            bulletsFiredInBurst++;

            if (bulletsFiredInBurst >= bulletsPerShot)
                isBursting = false;
        }
    }

    // ==================== FIRE ====================
    void FireBullet()
    {
        fireCount++;
        Debug.Log($"<color=yellow>[{name}] 🔫 FireBullet lần {fireCount}</color>");

        if (bullet == null)
        {
            Debug.LogError($"[{name}] ❌ FireBullet: bullet prefab NULL!");
            return;
        }
        if (bulletTransform == null)
        {
            Debug.LogError($"[{name}] ❌ FireBullet: bulletTransform NULL!");
            return;
        }

        float randomSpread = Random.Range(-currentSpread, currentSpread);

        // ✅ ĐÃ DỌN: bỏ dòng Quaternion.Euler(0,0,0) thừa
        Quaternion finalRotation = bulletTransform.rotation
                                 * Quaternion.Euler(0f, randomSpread, 0f);

        Vector3 spawnPos = bulletTransform.position
                         + bulletTransform.forward * muzzleOffset;

        Debug.Log($"[{name}] Spawn đạn tại {spawnPos}, rotation Y={finalRotation.eulerAngles.y:F1}");

        GameObject spawned = Instantiate(bullet, spawnPos, finalRotation);
        Debug.Log($"[{name}] ✅ Đã spawn: {spawned.name}");

        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = spawned.transform.forward * bulletSpeed;
            Debug.Log($"[{name}] Gán Rigidbody.velocity = forward * {bulletSpeed}");
        }
        else
        {
            Bullet bulletScript = spawned.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.speed = bulletSpeed;
                Debug.Log($"[{name}] Gán Bullet.speed = {bulletSpeed}");
            }
            else
            {
                Debug.LogWarning($"[{name}] ⚠ Đạn không có Rigidbody cũng không có script Bullet → đạn sẽ đứng yên!");
            }
        }

        currentSpread = Mathf.Min(currentSpread + spreadIncreasePerShot, maxSpread);
    }

    // ==================== HELPER ====================
    void LogPeriodic(string msg)
    {
        if (!debugMode) return;

        if (debugLogInterval <= 0f)
        {
            Debug.Log($"[{name}] {msg}");
            return;
        }

        debugTimer += Time.deltaTime;
        if (debugTimer >= debugLogInterval)
        {
            debugTimer = 0f;
            Debug.Log($"[{name}] {msg}");
        }
    }

    // ==================== GIZMOS ====================
    void OnDrawGizmosSelected()
    {
        if (rotationPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(rotationPoint.position, detectionRange);

        if (bulletTransform != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 spawnPos = bulletTransform.position
                             + bulletTransform.forward * muzzleOffset;
            Gizmos.DrawWireSphere(spawnPos, 0.15f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(bulletTransform.position, spawnPos);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(bulletTransform.position, bulletTransform.forward * 2f);
        }
    }
}