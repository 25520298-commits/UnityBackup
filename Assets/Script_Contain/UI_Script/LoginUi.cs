using UnityEngine;
using TMPro;

public class LoginUI : MonoBehaviour
{
    [Header("--- LOGIN FORM ---")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public GameObject loginFormGroup;

    [Header("--- REGISTER FORM ---")]
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerPasswordInput;
    public GameObject registerFormGroup;

    [Header("--- FORGOT PASSWORD FORM ---")]
    public GameObject forgotPasswordGroup;
    public TMP_InputField forgotPasswordEmailInput;

    [Header("--- SHARED ---")]
    public TextMeshProUGUI errorText;

    [Header("--- MESSAGE BOX (POPUP) ---")]
    public GameObject messageBoxPanel; 
    public TextMeshProUGUI messageBoxText; 

    void Start()
    {
        SetPanelVisibility(loginFormGroup, true);
        SetPanelVisibility(registerFormGroup, false);
        SetPanelVisibility(forgotPasswordGroup, false);
        
        if (messageBoxPanel != null) 
            messageBoxPanel.SetActive(false);

        if (errorText != null) errorText.text = "";
    }

    // ==================== XỬ LÝ ĐĂNG NHẬP ====================
    public void OnLoginButtonClicked()
    {
        if (string.IsNullOrEmpty(loginEmailInput.text) || string.IsNullOrEmpty(loginPasswordInput.text))
        {
            ShowError("Vui lòng nhập đầy đủ Email và Mật khẩu!");
            return;
        }

        ShowError("Đang đăng nhập...");

        AuthManager.Instance.Login(loginEmailInput.text, loginPasswordInput.text, 
            (errorMsg) => {
                ShowError("Lỗi: " + errorMsg);
            },
            () => {
                ShowError("Sai tài khoản quá 3 lần. Vui lòng đăng ký tài khoản mới!");
                SwitchToRegister(); 
            });
    }

    // ==================== XỬ LÝ ĐĂNG KÝ ====================
    public void OnRegisterButtonClicked()
    {
        if (string.IsNullOrEmpty(registerEmailInput.text) || 
            string.IsNullOrEmpty(registerPasswordInput.text) || 
            string.IsNullOrEmpty(registerUsernameInput.text))
        {
            ShowError("Vui lòng nhập đầy đủ thông tin!");
            return;
        }

        ShowError("Đang tạo tài khoản...");

        AuthManager.Instance.Register(registerEmailInput.text, registerPasswordInput.text, registerUsernameInput.text,
            (successMsg) => {
                ShowMessageBox("Thành công", successMsg);
                SwitchToLogin(); 
            },
            (errorMsg) => {
                ShowError("Lỗi: " + errorMsg);
            });
    }

    // ==================== XỬ LÝ KHÔI PHỤC MẬT KHẨU ====================
    public void OnSendForgotPasswordClicked()
    {
        if (string.IsNullOrEmpty(forgotPasswordEmailInput.text))
        {
            ShowError("Vui lòng nhập Email để khôi phục!");
            return;
        }

        ShowError("Đang gửi yêu cầu...");

        AuthManager.Instance.ForgotPassword(forgotPasswordEmailInput.text,
            (successMsg) => {
                string thongBao = "Hệ thống đã gửi một đường link khôi phục đến Email của bạn.\n\nVui lòng mở hộp thư (kể cả mục Spam/Thư rác) và bấm vào link để tạo mật khẩu mới!";
                ShowMessageBox("Kiểm tra Email", thongBao);
                SwitchToLogin(); 
            },
            (errorMsg) => {
                ShowError(errorMsg);
            });
    }

    // ==================== CHUYỂN FORM ====================
    public void SwitchToRegister()
    {
        if (AuthManager.Instance != null) AuthManager.Instance.ResetLoginAttempts(); // Reset bộ đếm
        SetPanelVisibility(loginFormGroup, false);
        SetPanelVisibility(registerFormGroup, true);
        SetPanelVisibility(forgotPasswordGroup, false);
        if (errorText != null) errorText.text = "";
    }

    public void SwitchToLogin()
    {
        if (AuthManager.Instance != null) AuthManager.Instance.ResetLoginAttempts(); // Reset bộ đếm
        SetPanelVisibility(registerFormGroup, false);
        SetPanelVisibility(loginFormGroup, true);
        SetPanelVisibility(forgotPasswordGroup, false);
        if (errorText != null) errorText.text = "";
    }

    public void SwitchToForgotPassword()
    {
        if (AuthManager.Instance != null) AuthManager.Instance.ResetLoginAttempts(); // Reset bộ đếm
        SetPanelVisibility(loginFormGroup, false);
        SetPanelVisibility(registerFormGroup, false);
        SetPanelVisibility(forgotPasswordGroup, true);
        if (errorText != null) errorText.text = "";
    }

    // ==================== HÀM HỖ TRỢ ====================
    private void SetPanelVisibility(GameObject panel, bool isVisible)
    {
        if (panel == null) return;
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = isVisible ? 1f : 0f;
            cg.interactable = isVisible;
            cg.blocksRaycasts = isVisible;
        }
        else
        {
            panel.SetActive(isVisible);
        }
    }

    private void ShowError(string message)
    {
        if (errorText != null) errorText.text = message;
    }

    public void ShowMessageBox(string title, string content)
    {
        if (messageBoxPanel != null)
        {
            messageBoxPanel.SetActive(true);
            if (messageBoxText != null) 
                messageBoxText.text = content;

            messageBoxPanel.transform.SetAsLastSibling();

            CanvasGroup cg = messageBoxPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }
    }

    public void CloseMessageBox()
    {
        if (messageBoxPanel != null)
        {
            CanvasGroup cg = messageBoxPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
            messageBoxPanel.SetActive(false);
        }
    }
}