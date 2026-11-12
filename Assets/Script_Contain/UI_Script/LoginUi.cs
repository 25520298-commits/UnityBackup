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
    // Ô text lỗi cũ (nếu bạn không dùng nữa thì có thể để trống hoặc xóa, 
    // nhưng tôi vẫn giữ lại để code không bị lỗi nếu bạn chưa xóa object)
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
            ShowMessageBox("Lỗi", "Vui lòng nhập đầy đủ Email và Mật khẩu!");
            return;
        }

        // Có thể bỏ dòng này nếu không muốn hiện chữ "Đang đăng nhập"
        if (errorText != null) errorText.text = "Đang đăng nhập...";

        AuthManager.Instance.Login(loginEmailInput.text, loginPasswordInput.text, 
            (errorMsg) => {
                // ĐÃ SỬA: Hiện lỗi sai mật khẩu/tài khoản bằng Message Box
                ShowMessageBox("Đăng nhập thất bại", errorMsg);
                if (errorText != null) errorText.text = ""; // Xóa chữ đang đăng nhập
            },
            () => {
                // ĐÃ SỬA: Hiện cảnh báo sai 3 lần bằng Message Box
                ShowMessageBox("Cảnh báo", "Sai tài khoản quá 3 lần. Vui lòng đăng ký tài khoản mới!");
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
            ShowMessageBox("Lỗi", "Vui lòng nhập đầy đủ thông tin!");
            return;
        }

        if (errorText != null) errorText.text = "Đang kiểm tra tài khoản...";

        AuthManager.Instance.CheckAccountExists(registerEmailInput.text, (exists) => 
        {
            if (exists)
            {
                ShowMessageBox("Lỗi", "Email này đã được đăng ký! Vui lòng đăng nhập hoặc dùng email khác.");
                if (errorText != null) errorText.text = "";
            }
            else
            {
                if (errorText != null) errorText.text = "Đang tạo tài khoản...";
                
                AuthManager.Instance.Register(registerEmailInput.text, registerPasswordInput.text, registerUsernameInput.text,
                    (successMsg) => {
                        // THÀNH CÔNG -> Hiện Message Box
                        ShowMessageBox("Thành công", successMsg);
                        if (errorText != null) errorText.text = "";
                        SwitchToLogin(); 
                    },
                    (errorMsg) => {
                        // LỖI (VD: Tên đã tồn tại) -> Hiện Message Box
                        ShowMessageBox("Đăng ký thất bại", errorMsg);
                        if (errorText != null) errorText.text = "";
                    });
            }
        });
    }

    // ==================== XỬ LÝ KHÔI PHỤC MẬT KHẨU ====================
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
                string thongBao = "Hệ thống đã gửi một đường link khôi phục đến Email của bạn.\n\nVui lòng mở hộp thư (kể cả mục Spam/Thư rác) và bấm vào link để tạo mật khẩu mới!";
                ShowMessageBox("Kiểm tra Email", thongBao);
                if (errorText != null) errorText.text = "";
                SwitchToLogin(); 
            },
            (errorMsg) => {
                ShowMessageBox("Lỗi", errorMsg);
                if (errorText != null) errorText.text = "";
            });
    }

    // ==================== CHUYỂN FORM ====================
    public void SwitchToRegister()
    {
        if (AuthManager.Instance != null) AuthManager.Instance.ResetLoginAttempts();
        SetPanelVisibility(loginFormGroup, false);
        SetPanelVisibility(registerFormGroup, true);
        SetPanelVisibility(forgotPasswordGroup, false);
        if (errorText != null) errorText.text = "";
    }

    public void SwitchToLogin()
    {
        if (AuthManager.Instance != null) AuthManager.Instance.ResetLoginAttempts();
        SetPanelVisibility(registerFormGroup, false);
        SetPanelVisibility(loginFormGroup, true);
        SetPanelVisibility(forgotPasswordGroup, false);
        if (errorText != null) errorText.text = "";
    }

    public void SwitchToForgotPassword()
    {
        if (AuthManager.Instance != null) AuthManager.Instance.ResetLoginAttempts();
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
        else
        {
            Debug.LogError("[LoginUI] LỖI: messageBoxPanel bị null! Bạn chưa kéo Panel vào Inspector.");
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