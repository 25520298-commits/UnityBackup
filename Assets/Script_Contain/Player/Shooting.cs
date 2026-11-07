using UnityEngine;

public class Shooting : MonoBehaviour
{
    private Camera mainCam;

    [Header("References")]
    public Transform rotationPoint;
    public GameObject bullet;
    public Transform bulletTransform;    // Kéo Muzzle vào đây
    public Bullet _bullet;               // Kéo Prefab Bullet vào đây

    [Header("Firing")]
    public bool canFire;
    private float timer;
    public float timeBetweenBullets = 0.2f;

    [Header("Burst / Multi-shot")]
    public int bulletsPerShot = 1;
    public float delayBetweenBullets = 0f;

    [Header("Spread")]
    public float baseSpread = 0f;
    public float maxSpread = 15f;
    public float spreadIncreasePerShot = 1.5f;
    public float spreadDecreaseRate = 10f;

    private float currentSpread = 0f;
    private int bulletsFiredInBurst = 0;
    private float burstTimer = 0f;
    private bool isBursting = false;

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
        // 1. Lấy hướng từ trục Z của Muzzle
        Vector3 fireDirection = bulletTransform.forward;

        // 2. Tính độ tản (Spread)
        float randomSpread = Random.Range(-currentSpread, currentSpread);
        fireDirection = Quaternion.Euler(0f, randomSpread, 0f) * fireDirection;

        // 3. Tạo đạn với góc xoay cơ bản (chỉ xoay theo hướng bay, không cộng offset)
        Quaternion baseRotation = Quaternion.LookRotation(fireDirection.normalized, Vector3.up);

        // 4. Tạo đạn
        GameObject newBullet = Instantiate(bullet, bulletTransform.position, baseRotation);

        // 5. Set damage
        Bullet bulletScript = newBullet.GetComponent<Bullet>();
        if (bulletScript != null && _bullet != null)
        {
            bulletScript.damage = _bullet.damage;
        }

        // 6. Tăng độ tản
        currentSpread = Mathf.Min(currentSpread + spreadIncreasePerShot, maxSpread);
    }
}