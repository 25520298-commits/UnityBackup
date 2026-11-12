using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TrainingManager : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject trainingMenuPanel; 
    public Toggle moveToggle;        
    public Toggle shootToggle;       
    public Button spawnButton;       // Nút kích hoạt chế độ đặt Bot
    public Button closeButton;       
    public Button exitButton;        
    public string mainMenuSceneName = "MainMenuUI"; 

    [Header("Spawn Settings")]
    public GameObject enemyPrefab;   
    public LayerMask groundLayer;    
    public Material ghostMaterial;   // (Tùy chọn) Material trong suốt cho bóng ma

    private List<EnemyController> enemies = new List<EnemyController>();
    
    // Biến trạng thái đặt Bot
    public bool IsPlacingModeActive { get; private set; } = false;
    private GameObject ghostEnemy = null;
    private GameManager gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        FindAllEnemies();

        if (moveToggle != null) moveToggle.onValueChanged.AddListener(ToggleMovement);
        if (shootToggle != null) shootToggle.onValueChanged.AddListener(ToggleShooting);
        
        if (spawnButton != null) spawnButton.onClick.AddListener(StartPlacingMode);
        if (closeButton != null) closeButton.onClick.AddListener(CloseMenu);
        if (exitButton != null) exitButton.onClick.AddListener(ExitToMainMenu);

        if (moveToggle != null) moveToggle.isOn = true;
        if (shootToggle != null) shootToggle.isOn = true;
    }

    void Update()
    {
        // Nếu đang trong chế độ đặt Bot
        if (IsPlacingModeActive)
        {
            UpdateGhostPosition();

            // Click chuột trái để đặt
            if (Input.GetMouseButtonDown(0))
            {
                PlaceEnemy();
            }
            // Click chuột phải để hủy
            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacingMode();
            }
        }
    }

    private void FindAllEnemies()
    {
        enemies.Clear();
        GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject obj in enemyObjects)
        {
            EnemyController controller = obj.GetComponent<EnemyController>();
            if (controller != null) enemies.Add(controller);
        }
    }

    // ==================== CHẾ ĐỘ ĐẶT BOT (GHOST MODE) ====================

    private void StartPlacingMode()
    {
        if (enemyPrefab == null) return;

        // 1. Ẩn Menu
        gameManager.CloseMenuFromScript();

        // 2. Hiện chuột
        gameManager.SetCursorState(true);
        
        // 3. Tạo một bản sao mờ (Ghost) của Enemy
        ghostEnemy = Instantiate(enemyPrefab);
        
        // Tắt các script và collider của Ghost để nó không hoạt động
        DisableGhostComponents(ghostEnemy);

        IsPlacingModeActive = true;
    }

    private void UpdateGhostPosition()
    {
        if (ghostEnemy == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            ghostEnemy.transform.position = hit.point;
        }
    }

    private void PlaceEnemy()
    {
        if (ghostEnemy == null) return;

        // Tạo Bot thật tại vị trí của Ghost
        CreateRealEnemy(ghostEnemy.transform.position);
        
        // Xóa Ghost
        Destroy(ghostEnemy);
        
        // Kết thúc chế độ đặt
        IsPlacingModeActive = false;

        // Khóa chuột lại để tiếp tục chơi
        gameManager.SetCursorState(false);
    }

    private void CancelPlacingMode()
    {
        if (ghostEnemy != null) Destroy(ghostEnemy);
        
        IsPlacingModeActive = false;
        gameManager.SetCursorState(false);
        
        // Mở lại menu
        gameManager.SetCursorState(true); 
        // gameManager.OpenMenu(); // Nếu bạn muốn mở lại menu
    }

    private void DisableGhostComponents(GameObject ghost)
    {
        // Tắt các script điều khiển
        var controller = ghost.GetComponent<EnemyController>();
        if (controller != null) controller.enabled = false;
        
        var shooting = ghost.GetComponent<EnemyShooting>();
        if (shooting != null) shooting.enabled = false;
        
        var agent = ghost.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        
        // Tắt va chạm
        var collider = ghost.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        // (Tùy chọn) Đổi Material để trông giống bóng ma
        if (ghostMaterial != null)
        {
            Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.material = ghostMaterial;
            }
        }
    }

    // ==================== CÁC HÀM HỖ TRỢ CŨ ====================

    private void CreateRealEnemy(Vector3 position)
    {
        GameObject newEnemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        
        EnemyController controller = newEnemy.GetComponent<EnemyController>();
        if (controller != null) enemies.Add(controller);
        
        if (controller != null && moveToggle != null) controller.enabled = moveToggle.isOn;
        var shooting = newEnemy.GetComponent<EnemyShooting>();
        if (shooting != null && shootToggle != null) shooting.enabled = shootToggle.isOn;
    }

    private void ToggleMovement(bool isOn)
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.enabled = isOn; 
                var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.isStopped = !isOn;
            }
        }
    }

    private void ToggleShooting(bool isOn)
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                var shooting = enemy.GetComponent<EnemyShooting>(); 
                if (shooting != null) shooting.enabled = isOn;
            }
        }
    }

    private void CloseMenu()
    {
        gameManager.CloseMenuFromScript();
    }

    private void ExitToMainMenu()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}