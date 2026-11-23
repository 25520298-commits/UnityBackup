using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections; 

public class Shooting : MonoBehaviour
{
    private Camera mainCam;
    private GameManager gameManager; 

    [Header("References")]
    public Transform weaponPivot;        // Kéo bệ súng (GunPivot) vào đây
    public Transform bulletTransform;    // Kéo Muzzle (đầu nòng súng) vào đây
    public GameObject bullet;            
    public Bullet _bullet;               
    
    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;   
    public float flashDuration = 0.05f;  

    [Header("Weapon Aim Settings")]
    public float maxAimDistance = 1000f; 
    public float weaponRotateSpeed = 15f; 

    [Header("UI Crosshair Settings")]
    public RectTransform crosshairUI;    
    public float baseCrosshairSize = 40f; 
    public float crosshairPixelPerDegree = 5f; 
    public float crosshairExpandSpeed = 15f; 
    public float crosshairShrinkSpeed = 8f;  
    private float currentCrosshairSize;

    [Header("Ammo & Magazine Settings")]
    public int magazineSize = 30;       
    public int currentAmmo;             
    public int maxReserveAmmo = 100;    
    public int currentReserveAmmo;      
    public float reloadTime = 2f;
    public bool isReloading = false;
    
    [Header("UI Ammo Settings")]
    public TextMeshProUGUI ammoText; 
    public CanvasGroup ammoCanvasGroup; 

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
        gameManager = FindFirstObjectByType<GameManager>(); 
        currentSpread = baseSpread;

        currentAmmo = magazineSize;
        currentReserveAmmo = maxReserveAmmo;
        UpdateAmmoUI();

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

        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmo < magazineSize && currentReserveAmmo > 0)
        {
            StartCoroutine(Reload());
        }
    }

    // XOAY BỆ SÚNG THEO CAMERA (LÊN, XUỐNG, TRÁI, PHẢI)
    void RotateWeaponPivot()
    {
        if (weaponPivot == null || mainCam == null) return;
        
        // Xoay toàn bộ bệ súng theo hướng Camera
        Quaternion cameraRotation = mainCam.transform.rotation;
        weaponPivot.rotation = Quaternion.Slerp(weaponPivot.rotation, cameraRotation, Time.deltaTime * weaponRotateSpeed);
    }

    void HandleFiring()
    {
        if (gameManager != null && gameManager.isMenuOpen) return;
        if (isReloading) return;

        if (Input.GetMouseButton(0) && canFire && !isBursting)
        {
            if (currentAmmo <= 0)
            {
                if (currentReserveAmmo > 0) StartCoroutine(Reload());
                return;
            }

            canFire = false; 
            currentAmmo--;
            UpdateAmmoUI();
            
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
        if (bulletTransform == null || bullet == null || currentAmmo <= 0) return;

        if (muzzleFlash != null)
        {
            muzzleFlash.Play(); 
            StartCoroutine(StopMuzzleFlash()); 
        }

       

        Ray ray = mainCam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        Vector3 fireDirection = ray.direction;

        float randomSpreadY = Random.Range(-currentSpread, currentSpread);
        float randomSpreadX = Random.Range(-currentSpread, currentSpread);
        fireDirection = Quaternion.Euler(randomSpreadX, randomSpreadY, 0f) * fireDirection;

        Quaternion bulletRotation = Quaternion.LookRotation(fireDirection);

        GameObject newBullet = Instantiate(bullet, bulletTransform.position, bulletRotation);
        
        if (_bullet != null && newBullet.GetComponent<Bullet>() != null)
            newBullet.GetComponent<Bullet>().damage = _bullet.damage;

        currentSpread = Mathf.Min(currentSpread + spreadIncreasePerShot, maxSpread);
    }

    IEnumerator Reload()
    {
        isReloading = true;
        if (ammoText != null) ammoText.text = "Reloading...";

        if (ammoCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < reloadTime / 2f)
            {
                elapsed += Time.deltaTime;
                ammoCanvasGroup.alpha = Mathf.Lerp(1f, 0.2f, elapsed / (reloadTime / 2f));
                yield return null;
            }

            yield return new WaitForSeconds(reloadTime / 2f);

            elapsed = 0f;
            while (elapsed < reloadTime / 2f)
            {
                elapsed += Time.deltaTime;
                ammoCanvasGroup.alpha = Mathf.Lerp(0.2f, 1f, elapsed / (reloadTime / 2f));
                yield return null;
            }
            ammoCanvasGroup.alpha = 1f; 
        }
        else
        {
            yield return new WaitForSeconds(reloadTime);
        }

        int ammoNeeded = magazineSize - currentAmmo; 
        if (currentReserveAmmo >= ammoNeeded)
        {
            currentAmmo += ammoNeeded;
            currentReserveAmmo -= ammoNeeded;
        }
        else
        {
            currentAmmo += currentReserveAmmo;
            currentReserveAmmo = 0;
        }

        isReloading = false;
        UpdateAmmoUI();
    }

    void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {currentReserveAmmo}";
        }
    }

    IEnumerator StopMuzzleFlash()
    {
        yield return new WaitForSeconds(flashDuration);
        if (muzzleFlash != null) muzzleFlash.Stop();
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