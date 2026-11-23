using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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

    private int localLoginFailCount = 0;

    void Start()
    {
        SetPanelVisibility(loginFormGroup, true);
        SetPanelVisibility(registerFormGroup, false);
        SetPanelVisibility(forgotPasswordGroup, false);
        
        if (messageBoxPanel != null) 
            messageBoxPanel.SetActive(false);
    }

    // ==================== ĐĂNG NHẬP ====================
    public void OnLoginButtonClicked()
    {
        if (string.IsNullOrEmpty(loginEmailInput.text) || string.IsNullOrEmpty(loginPasswordInput.text))
        {
            ShowMessageBox("Lỗi", "Vui lòng nhập đầy đủ Email và Mật khẩu!");
            return;
        }

        if (errorText != null) errorText.text = "Đang đăng nhập...";

        AuthManager.Instance.Login(loginEmailInput.text, loginPasswordInput.text, 
            (errorMsg) => {
                localLoginFailCount++;
                if (localLoginFailCount >= 3)
                {
                    localLoginFailCount = 0;
                    SwitchToRegister();
                    ShowMessageBox("Chưa có tài khoản?", "Bạn đã nhập sai 3 lần. Hãy đăng ký ngay!");
                }
                else
                {
                    ShowMessageBox("Đăng nhập thất bại", $"{errorMsg}\n(Lần {localLoginFailCount}/3)");
                }
                if (errorText != null) errorText.text = "";
            },
            () => {
                localLoginFailCount = 0;
                if (errorText != null) errorText.text = "";
                SceneManager.LoadScene("MainMenuUI"); 
            });
    }

    // ==================== ĐĂNG KÝ ====================
    public void OnRegisterButtonClicked()
    {
        if (string.IsNullOrEmpty(registerEmailInput.text) || 
            string.IsNullOrEmpty(registerPasswordInput.text) || 
            string.IsNullOrEmpty(registerUsernameInput.text))
        {
            ShowMessageBox("Lỗi", "Vui lòng nhập đầy đủ thông tin!");
            return;
        }

        if (errorText != null) errorText.text = "Đang tạo tài khoản...";

        AuthManager.Instance.Register(registerEmailInput.text, registerPasswordInput.text, registerUsernameInput.text,
            (successMsg) => {
                ShowMessageBox("Thành công", successMsg);
                if (errorText != null) errorText.text = "";
                SwitchToLogin();
            },
            (error) => {
                ShowMessageBox("Đăng ký thất bại", error);
                if (errorText != null) errorText.text = "";
            });
    }

    // ==================== QUÊN MẬT KHẨU ====================
    public void OnSendForgotPasswordClicked()
    {
        if (string.IsNullOrEmpty(forgotPasswordEmailInput.text))
        {
            ShowMessageBox("Lỗi", "Vui lòng nhập Email để khôi phục!");
            return;
        }

        if (errorText != null) errorText.text = "Đang gửi yêu cầu...";

        AuthManager.Instance.ForgotPassword(forgotPasswordEmailInput.text,
            (successMsg) => {
                ShowMessageBox("Kiểm tra Email", "Hệ thống đã gửi yêu cầu khôi phục. Hãy kiểm tra hộp thư!");
                if (errorText != null) errorText.text = "";
                SwitchToLogin();
            },
            (error) => {
                ShowMessageBox("Lỗi", error);
                if (errorText != null) errorText.text = "";
            });
    }

    // ==================== CHUYỂN FORM ====================
    public void SwitchToRegister()
    {
        SetPanelVisibility(loginFormGroup, false);
        SetPanelVisibility(registerFormGroup, true);
        SetPanelVisibility(forgotPasswordGroup, false);
        
        if (loginEmailInput != null) loginEmailInput.text = "";
        if (loginPasswordInput != null) loginPasswordInput.text = "";
        if (errorText != null) errorText.text = "";
    }

    public void SwitchToLogin()
    {
        localLoginFailCount = 0; 
        SetPanelVisibility(registerFormGroup, false);
        SetPanelVisibility(loginFormGroup, true);
        SetPanelVisibility(forgotPasswordGroup, false);
        
        if (registerEmailInput != null) registerEmailInput.text = "";
        if (registerPasswordInput != null) registerPasswordInput.text = "";
        if (registerUsernameInput != null) registerUsernameInput.text = "";
        if (errorText != null) errorText.text = "";
    }

    public void SwitchToForgotPassword()
    {
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