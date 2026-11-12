using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Camera Sensitivity Settings")]
    public float mouseSensitivity = 1f; 
    public float sensitivityMultiplier = 50f; 
    public bool invertY = true; 

    [Header("UI Settings")]
    public GameObject trainingMenuPanel; 
    private CanvasGroup trainingMenuCanvasGroup;
    
    private Unity.Cinemachine.CinemachineInputAxisController inputAxisController;
    private bool isPaused = false;

    void Start()
    {
        inputAxisController = FindFirstObjectByType<Unity.Cinemachine.CinemachineInputAxisController>();
        
        if (PlayerPrefs.HasKey("MouseSensitivity"))
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
        
        ApplySensitivity();

        if (trainingMenuPanel != null)
        {
            trainingMenuCanvasGroup = trainingMenuPanel.GetComponent<CanvasGroup>();
            trainingMenuPanel.SetActive(true); // Đảm bảo Panel Active

            // Ẩn Panel lúc đầu
            trainingMenuCanvasGroup.alpha = 0f;
            trainingMenuCanvasGroup.interactable = false;
            trainingMenuCanvasGroup.blocksRaycasts = false;
        }

        // Khóa chuột
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Đảm bảo Camera có thể xoay lúc mới vào
        if (inputAxisController != null) inputAxisController.enabled = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TrainingManager tm = FindFirstObjectByType<TrainingManager>();
            if (tm != null && tm.IsPlacingModeActive) return;

            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // TẮT XOAY CAMERA BẰNG CHUỘT
        if (inputAxisController != null) inputAxisController.enabled = false;

        if (trainingMenuCanvasGroup != null)
        {
            trainingMenuCanvasGroup.alpha = 1f;
            trainingMenuCanvasGroup.interactable = true;
            trainingMenuCanvasGroup.blocksRaycasts = true;
            trainingMenuPanel.transform.SetAsLastSibling();
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // BẬT LẠI XOAY CAMERA BẰNG CHUỘT
        if (inputAxisController != null) inputAxisController.enabled = true;

        if (trainingMenuCanvasGroup != null)
        {
            trainingMenuCanvasGroup.alpha = 0f;
            trainingMenuCanvasGroup.interactable = false;
            trainingMenuCanvasGroup.blocksRaycasts = false;
        }
    }

    public void CloseMenuFromScript()
    {
        isPaused = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Bật lại Camera
        if (inputAxisController != null) inputAxisController.enabled = true;

        if (trainingMenuCanvasGroup != null)
        {
            trainingMenuCanvasGroup.alpha = 0f;
            trainingMenuCanvasGroup.interactable = false;
            trainingMenuCanvasGroup.blocksRaycasts = false;
        }
    }

    public void SetCursorState(bool showCursor)
    {
        if (showCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            // Nếu hiện chuột (để đặt Bot) thì cũng tắt xoay Camera
            if (inputAxisController != null) inputAxisController.enabled = false;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            // Nếu khóa chuột (tiếp tục chơi) thì bật lại xoay Camera
            if (inputAxisController != null) inputAxisController.enabled = true;
        }
    }

    // ==================== QUẢN LÝ ĐỘ NHẠY ====================
    public void SetSensitivity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
        ApplySensitivity();
        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
        PlayerPrefs.Save();
    }

    private void ApplySensitivity()
    {
        if (inputAxisController == null) return;

        if (inputAxisController.Controllers.Count > 0)
            inputAxisController.Controllers[0].Input.Gain = mouseSensitivity * sensitivityMultiplier;
        
        if (inputAxisController.Controllers.Count > 1)
        {
            float yGain = mouseSensitivity * sensitivityMultiplier;
            if (invertY) yGain *= -1f;
            inputAxisController.Controllers[1].Input.Gain = yGain;
        }
    }
}