using UnityEngine;

public class Shooting : MonoBehaviour
{
    private Camera mainCam;
    
    [Header("Damage")]
    [Tooltip("Sát thương mỗi viên đạn bắn ra")]
   

    [Header("References")]
    public Transform rotationPoint;
    public GameObject bullet;
    public Transform bulletTransform;    // đầu nòng súng

    [Header("Firing")]
    public bool canFire;
    private float timer;
    public float timeBetweenBullets = 0.2f;

    [Header("Burst / Multi-shot")]
    [Tooltip("Số viên đạn bắn ra mỗi lần bóp cò")]
    public int bulletsPerShot = 1;
    [Tooltip("Độ trễ giữa các viên trong cùng 1 lần bắn (giây) — để 0 nếu muốn bắn cùng lúc")]
    public float delayBetweenBullets = 0f;

    [Header("Spread")]
    [Tooltip("Độ tản ban đầu (độ)")]
    public float baseSpread = 0f;
    [Tooltip("Độ tản tối đa (độ)")]
    public float maxSpread = 15f;
    [Tooltip("Độ tản tăng thêm mỗi viên đạn")]
    public float spreadIncreasePerShot = 1.5f;
    [Tooltip("Tốc độ hồi phục độ tản khi ngừng bắn")]
    public float spreadDecreaseRate = 10f;

    private float currentSpread = 0f;
    private int bulletsFiredInBurst = 0;
    private float burstTimer = 0f;
    private bool isBursting = false;
    public EnemyBullet _bullet;
    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null) Debug.LogError("Không tìm thấy Main Camera!");
        if (rotationPoint == null) rotationPoint = transform;

        currentSpread = baseSpread;
    }

    void Update()
    {
        RotateToMouse();
        HandleFiring();
        HandleBurst();
        HandleCooldown();
        HandleSpreadRecovery();
    }

    void RotateToMouse()
    {
        Plane groundPlane = new Plane(Vector3.up, rotationPoint.position);
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        if (!groundPlane.Raycast(ray, out float distance)) return;

        Vector3 mouseWorld = ray.GetPoint(distance);
        mouseWorld.y = rotationPoint.position.y;

        Vector3 direction = mouseWorld - rotationPoint.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        rotationPoint.rotation = Quaternion.Euler(0f, angle - 90, 0f);
    }

    void HandleFiring()
    {
        if (Input.GetMouseButton(0) && canFire && !isBursting)
        {
            canFire = false;
            StartBurst();
        }
    }

    void HandleCooldown()
    {
        if (!canFire)
        {
            timer += Time.deltaTime;
            if (timer > timeBetweenBullets)
            {
                canFire = true;
                timer = 0f;
            }
        }
    }

    void HandleSpreadRecovery()
    {
        // Hồi phục độ tản khi nhả chuột
        if (!Input.GetMouseButton(0))
        {
            currentSpread = Mathf.MoveTowards(currentSpread, baseSpread, spreadDecreaseRate * Time.deltaTime);
        }
    }

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

    void FireBullet()
    {
        float randomSpread = Random.Range(-currentSpread, currentSpread);

        Quaternion baseRotation = bulletTransform.rotation * Quaternion.Euler(0f, -90f, 0f);
        Quaternion spreadRotation = Quaternion.Euler(0f, randomSpread, 0f);
        Quaternion finalRotation = baseRotation * spreadRotation;
        GameObject newBullet = Instantiate(bullet, bulletTransform.position, finalRotation);
        Bullet bulletScript = newBullet.GetComponent<Bullet>();
        Instantiate(bullet, bulletTransform.position, finalRotation);
        if (bulletScript != null)
        {
            bulletScript.damage = _bullet.damage;
        }
        else
        {
            Debug.LogWarning("Prefab đạn chưa gắn script Bullet!");
        }
        currentSpread = Mathf.Min(currentSpread + spreadIncreasePerShot, maxSpread);
    }
}