using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Camera Sensitivity Settings")]
    public float mouseSensitivity = 1f; 
    public float sensitivityMultiplier = 10f; 
    public bool invertY = true; 

    [Header("UI Settings")]
    public GameObject trainingMenuPanel; 
    public Slider sensitivitySlider; 
    public TMPro.TextMeshProUGUI sensitivityValueText; 

    // Biến này để Shooting biết Menu đang mở hay đóng
    public bool isMenuOpen { get; private set; } = false; 

    private CanvasGroup trainingMenuCanvasGroup;
    private Unity.Cinemachine.CinemachineInputAxisController inputAxisController;

    void Start()
    {
        inputAxisController = FindFirstObjectByType<Unity.Cinemachine.CinemachineInputAxisController>();
        
        if (PlayerPrefs.HasKey("MouseSensitivity"))
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
        
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = mouseSensitivity;
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }

        ApplySensitivity();
        UpdateSensitivityText();

        if (trainingMenuPanel != null)
        {
            trainingMenuCanvasGroup = trainingMenuPanel.GetComponent<CanvasGroup>();
            trainingMenuCanvasGroup.alpha = 0f;
            trainingMenuCanvasGroup.interactable = false;
            trainingMenuCanvasGroup.blocksRaycasts = false;
        }

        SetCursorState(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TrainingManager tm = FindFirstObjectByType<TrainingManager>();
            if (tm != null && tm.IsPlacingModeActive) return;

            if (isMenuOpen) ResumeGame();
            else PauseGame();
        }
    }

    // ==================== QUẢN LÝ ĐỘ NHẠY ====================
    public void SetSensitivity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
        ApplySensitivity();
        UpdateSensitivityText();
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

    private void UpdateSensitivityText()
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = $"Độ nhạy: {mouseSensitivity:F1}";
    }

    // ==================== QUẢN LÝ CHUỘT & MENU ====================
    public void SetCursorState(bool showCursor)
    {
        if (showCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (inputAxisController != null) inputAxisController.enabled = false;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (inputAxisController != null) inputAxisController.enabled = true;
        }
    }

    public void PauseGame()
    {
        isMenuOpen = true; // Bật cờ báo hiệu Menu đang mở
        SetCursorState(true);

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
        isMenuOpen = false; // Tắt cờ
        SetCursorState(false);

        if (trainingMenuCanvasGroup != null)
        {
            trainingMenuCanvasGroup.alpha = 0f;
            trainingMenuCanvasGroup.interactable = false;
            trainingMenuCanvasGroup.blocksRaycasts = false;
        }
    }

    public void CloseMenuFromScript()
    {
        ResumeGame();
    }
}