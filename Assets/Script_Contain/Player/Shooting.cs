using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Cần thư viện này để dùng Coroutine

public class Shooting : MonoBehaviour
{
    private Camera mainCam;

    [Header("References")]
    public Transform weaponPivot;        
    public Transform bulletTransform;    
    public GameObject bullet;            
    public Bullet _bullet;               
    
    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;   // KÉO OBJECT MUZZLEFLASH VÀO ĐÂY
    public float flashDuration = 0.05f;  // Thời gian lửa hiện ra

    [Header("Weapon Aim Settings")]
    public float maxAimDistance = 100f;
    public float weaponRotateSpeed = 15f; 

    [Header("UI Crosshair Settings")]
    public RectTransform crosshairUI;    
    public float baseCrosshairSize = 40f; 
    public float crosshairPixelPerDegree = 5f; 
    public float crosshairExpandSpeed = 15f; 
    public float crosshairShrinkSpeed = 8f;  
    private float currentCrosshairSize;

    [Header("Firing Settings")]
    public bool canFire = true;
    private float timer;
    public float timeBetweenBullets = 0.15f; 
    public int bulletsPerShot = 1;
    public float delayBetweenBullets = 0f;

    [Header("Spread (Độ tản đạn)")]
    public float baseSpread = 0.5f;      
    public float maxSpread = 5f;         
    public float spreadIncreasePerShot = 0.2f; 
    public float spreadDecreaseRate = 5f;      

    private float currentSpread = 0f;
    private int bulletsFiredInBurst = 0;
    private float burstTimer = 0f;
    private bool isBursting = false;

    void Start()
    {
        mainCam = Camera.main;
        currentSpread = baseSpread;

        if (crosshairUI != null)
        {
            currentCrosshairSize = baseCrosshairSize;
            crosshairUI.sizeDelta = new Vector2(currentCrosshairSize, currentCrosshairSize);
        }
    }

    void Update()
    {
        RotateWeaponPivot(); 
        HandleFiring();
        HandleBurst();
        HandleCooldown();
        HandleSpreadRecovery();
        HandleCrosshairAnimation(); 
    }

    void RotateWeaponPivot()
    {
        if (weaponPivot == null || mainCam == null) return;
        Quaternion cameraRotation = mainCam.transform.rotation;
        weaponPivot.rotation = Quaternion.Slerp(weaponPivot.rotation, cameraRotation, Time.deltaTime * weaponRotateSpeed);
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
            currentSpread = Mathf.MoveTowards(currentSpread, baseSpread, spreadDecreaseRate * Time.deltaTime);
    }

    void StartBurst()
    {
        if (bulletsPerShot <= 1) { FireBullet(); return; }
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
            while (bulletsFiredInBurst < bulletsPerShot) { FireBullet(); bulletsFiredInBurst++; }
            isBursting = false;
            return;
        }
        burstTimer += Time.deltaTime;
        if (burstTimer >= delayBetweenBullets)
        {
            burstTimer = 0f;
            FireBullet();
            bulletsFiredInBurst++;
            if (bulletsFiredInBurst >= bulletsPerShot) isBursting = false;
        }
    }

    void FireBullet()
    {
        if (bulletTransform == null || bullet == null) return;

        // --- HIỆU ỨNG LỬA ĐẦU NÒNG ---
        if (muzzleFlash != null)
        {
            muzzleFlash.Play(); // Bật lửa
            StartCoroutine(StopMuzzleFlash()); // Tự động tắt sau vài mili giây
        }

        // 1. Bắn tia từ Camera qua tâm màn hình
        Ray ray = mainCam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        Vector3 targetPoint;
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(maxAimDistance);

        // 2. Tính hướng bay
        Vector3 fireDirection = (targetPoint - bulletTransform.position).normalized;

        // 3. Thêm độ tản
        float randomSpreadY = Random.Range(-currentSpread, currentSpread);
        float randomSpreadX = Random.Range(-currentSpread, currentSpread);
        fireDirection = Quaternion.Euler(randomSpreadX, randomSpreadY, 0f) * fireDirection;

        // 4. Tạo đạn
        GameObject newBullet = Instantiate(bullet, bulletTransform.position, Quaternion.LookRotation(fireDirection));
        
        if (_bullet != null && newBullet.GetComponent<Bullet>() != null)
            newBullet.GetComponent<Bullet>().damage = _bullet.damage;

        // 5. Tăng độ tản
        currentSpread = Mathf.Min(currentSpread + spreadIncreasePerShot, maxSpread);
    }

    // Coroutine để tắt lửa sau một khoảng thời gian ngắn
    IEnumerator StopMuzzleFlash()
    {
        yield return new WaitForSeconds(flashDuration);
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop();
        }
    }

    void HandleCrosshairAnimation()
    {
        if (crosshairUI == null) return;
        float targetSize = baseCrosshairSize + (currentSpread * crosshairPixelPerDegree);
        float speed = (targetSize > currentCrosshairSize) ? crosshairExpandSpeed : crosshairShrinkSpeed;
        currentCrosshairSize = Mathf.Lerp(currentCrosshairSize, targetSize, Time.deltaTime * speed);
        crosshairUI.sizeDelta = new Vector2(currentCrosshairSize, currentCrosshairSize);
    }
}