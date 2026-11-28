using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{
    [Header("--- LOGIN FORM ---")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginButton;
    public GameObject loginFormGroup;

    [Header("--- REGISTER FORM ---")]
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerPasswordInput;
    public Button registerButton;
    public GameObject registerFormGroup;

    [Header("--- FORGOT PASSWORD FORM ---")]
    public GameObject forgotPasswordGroup;
    public TMP_InputField forgotPasswordEmailInput;
    public Button sendForgotPasswordButton;
    public Button backToLoginButton;
    [Tooltip("Thời gian khóa gửi lại mail (giây).")]
    public float forgotCooldownSeconds = 60f;

    [Header("--- RESET PASSWORD FORM ---")]
    public GameObject resetPasswordGroup;
    public TMP_InputField resetCodeInput;
    public TMP_InputField newPasswordInput;
    public TMP_InputField confirmPasswordInput;
    public Button confirmResetPasswordButton;
    public Button resetBackToLoginButton;

    [Header("--- SHARED ---")]
    public TextMeshProUGUI errorText;

    [Header("--- MESSAGE BOX (POPUP) ---")]
    public GameObject messageBoxPanel;
    public TextMeshProUGUI messageBoxText;
    public Button messageBoxConfirmButton;

    private int localLoginFailCount = 0;
    private bool isSubmitting = false;
    private float forgotCooldownTimer = 0f;
    private string lastSentEmail = "";
    private bool hasSentCodeForCurrentEmail = false;

    // Panel tiếp theo sẽ mở ra sau khi bấm nút trên MessageBox (ban đầu giữ ẩn khi MessageBox đang mở)
    private GameObject pendingNextPanel = null;

    void Start()
    {
        ShowOnlyPanel(loginFormGroup);

        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginButtonClicked);

        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterButtonClicked);

        if (sendForgotPasswordButton != null)
            sendForgotPasswordButton.onClick.AddListener(OnSendForgotPasswordClicked);

        if (backToLoginButton != null)
            backToLoginButton.onClick.AddListener(SwitchToLogin);

        if (confirmResetPasswordButton != null)
            confirmResetPasswordButton.onClick.AddListener(OnConfirmResetPasswordClicked);

        if (resetBackToLoginButton != null)
            resetBackToLoginButton.onClick.AddListener(SwitchToLogin);

        if (messageBoxConfirmButton != null)
            messageBoxConfirmButton.onClick.AddListener(CloseMessageBox);

        HideMessageBoxImmediate();
        SetActionButtonsInteractable(true);
    }

    void Update()
    {
        if (forgotCooldownTimer > 0f)
        {
            forgotCooldownTimer -= Time.deltaTime;
            if (forgotCooldownTimer < 0f)
                forgotCooldownTimer = 0f;

            UpdateSendForgotButtonState();
        }
    }

    // ==================== ĐĂNG NHẬP ====================
    public void OnLoginButtonClicked()
    {
        if (isSubmitting) return;

        string email = ReadInputValue(loginEmailInput);
        string password = ReadInputValue(loginPasswordInput);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowMessageBox("Lỗi", "Vui lòng nhập đầy đủ Email và Mật khẩu!");
            return;
        }

        SetSubmitting(true);
        if (errorText != null) errorText.text = "Đang đăng nhập...";

        AuthManager.Instance.Login(email, password,
            (errorMsg) =>
            {
                SetSubmitting(false);
                localLoginFailCount++;
                if (localLoginFailCount >= 3)
                {
                    localLoginFailCount = 0;
                    ShowMessageBox("Chưa có tài khoản?", "Bạn đã nhập sai 3 lần. Hãy đăng ký ngay!", registerFormGroup);
                }
                else
                {
                    ShowMessageBox("Đăng nhập thất bại", $"{errorMsg}\n(Lần {localLoginFailCount}/3)");
                }
                if (errorText != null) errorText.text = "";
            },
            () =>
            {
                SetSubmitting(false);
                localLoginFailCount = 0;
                if (errorText != null) errorText.text = "";
                SceneManager.LoadScene("MainMenuUI");
            });
    }

    // ==================== ĐĂNG KÝ ====================
    public void OnRegisterButtonClicked()
    {
        if (isSubmitting) return;

        string email = ReadInputValue(registerEmailInput);
        string username = ReadInputValue(registerUsernameInput);
        string password = ReadInputValue(registerPasswordInput);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
        {
            ShowMessageBox("Lỗi", "Vui lòng nhập đầy đủ thông tin!");
            return;
        }

        SetSubmitting(true);
        if (errorText != null) errorText.text = "Đang tạo tài khoản...";

        AuthManager.Instance.Register(email, password, username,
            (successMsg) =>
            {
                SetSubmitting(false);
                ClearRegisterInputs();
                ShowMessageBox("Thành công", successMsg, loginFormGroup);
                if (errorText != null) errorText.text = "";
            },
            (error) =>
            {
                SetSubmitting(false);
                ShowMessageBox("Đăng ký thất bại", error);
                if (errorText != null) errorText.text = "";
            });
    }

    // ==================== QUÊN MẬT KHẨU ====================
    public void OnSendForgotPasswordClicked()
    {
        if (isSubmitting) return;

        string email = ReadInputValue(forgotPasswordEmailInput).Trim();
        if (string.IsNullOrEmpty(email))
        {
            ShowMessageBox("Lỗi", "Vui lòng nhập Email để khôi phục!");
            return;
        }

        if (hasSentCodeForCurrentEmail &&
            string.Equals(email, lastSentEmail, System.StringComparison.OrdinalIgnoreCase) &&
            forgotCooldownTimer > 0f)
        {
            ShowMessageBox("Đã gửi mã", $"Mã đã được gửi tới {email}. Vui lòng kiểm tra hộp thư (còn {Mathf.CeilToInt(forgotCooldownTimer)}s mới có thể gửi lại).", resetPasswordGroup);
            return;
        }

        SetSubmitting(true);
        if (errorText != null) errorText.text = "Đang gửi yêu cầu...";

        AuthManager.Instance.ForgotPassword(email,
            (successMsg) =>
            {
                SetSubmitting(false);
                lastSentEmail = email;
                hasSentCodeForCurrentEmail = true;
                forgotCooldownTimer = forgotCooldownSeconds;
                UpdateSendForgotButtonState();

                ClearResetInputs();
                ShowMessageBox("Kiểm tra Email", "Hệ thống đã gửi mã xác nhận. Hãy nhập mã xác nhận và mật khẩu mới!", resetPasswordGroup);
                if (errorText != null) errorText.text = "";
            },
            (error) =>
            {
                SetSubmitting(false);
                ShowMessageBox("Lỗi", error);
                if (errorText != null) errorText.text = "";
            });
    }

    // ==================== ĐỔI MẬT KHẨU ====================
    public void OnConfirmResetPasswordClicked()
    {
        if (isSubmitting) return;

        string email = ReadInputValue(forgotPasswordEmailInput).Trim();
        if (string.IsNullOrEmpty(email)) email = lastSentEmail;

        string code = ReadInputValue(resetCodeInput).Trim();
        string newPass = ReadInputValue(newPasswordInput);
        string confirmPass = ReadInputValue(confirmPasswordInput);

        if (string.IsNullOrEmpty(email))
        {
            ShowMessageBox("Thiếu Email", "Không tìm thấy email khôi phục. Vui lòng quay lại nhập Email và gửi mã!", forgotPasswordGroup);
            return;
        }

        if (string.IsNullOrEmpty(code))
        {
            ShowMessageBox("Kiểm tra mã xác nhận", "Vui lòng nhập mã xác nhận đã được gửi về email!");
            return;
        }

        if (string.IsNullOrEmpty(newPass))
        {
            ShowMessageBox("Lỗi mật khẩu", "Vui lòng nhập mật khẩu mới!");
            return;
        }

        if (newPass.Length < 6)
        {
            ShowMessageBox("Lỗi mật khẩu", "Mật khẩu mới phải có tối thiểu 6 ký tự!");
            return;
        }

        if (confirmPasswordInput != null && newPass != confirmPass)
        {
            ShowMessageBox("Lỗi mật khẩu", "Mật khẩu xác nhận không khớp với mật khẩu mới!");
            return;
        }

        SetSubmitting(true);
        if (errorText != null) errorText.text = "Đang xác thực mã và đổi mật khẩu...";

        AuthManager.Instance.ResetPassword(email, code, newPass,
            (successMsg) =>
            {
                SetSubmitting(false);
                hasSentCodeForCurrentEmail = false;
                lastSentEmail = "";
                forgotCooldownTimer = 0f;
                UpdateSendForgotButtonState();
                ClearAllInputs();

                string msg = !string.IsNullOrEmpty(successMsg) ? successMsg : "Mã xác nhận hợp lệ. Đổi mật khẩu thành công!";
                ShowMessageBox("Thành công", msg, loginFormGroup);
                if (errorText != null) errorText.text = "";
            },
            (error) =>
            {
                SetSubmitting(false);
                string verifyMsg = string.IsNullOrEmpty(error)
                    ? "Mã xác nhận không đúng hoặc đã hết hạn!"
                    : error;
                ShowMessageBox("Xác nhận thất bại", verifyMsg);
                if (errorText != null) errorText.text = "";
            });
    }

    // ==================== CÁCH ĐỌC INPUT ====================
    private string ReadInputValue(TMP_InputField field)
    {
        if (field == null) return "";
        field.ForceLabelUpdate();
        return field.text ?? "";
    }

    // ==================== CHUYỂN FORM ====================
    public void SwitchToRegister()
    {
        if (isSubmitting) return;
        pendingNextPanel = null;
        HideMessageBoxImmediate();
        ShowOnlyPanel(registerFormGroup);
        if (loginEmailInput != null) loginEmailInput.text = "";
        if (loginPasswordInput != null) loginPasswordInput.text = "";
        if (errorText != null) errorText.text = "";
    }

    public void SwitchToLogin()
    {
        if (isSubmitting) return;
        localLoginFailCount = 0;
        pendingNextPanel = null;
        HideMessageBoxImmediate();
        ShowOnlyPanel(loginFormGroup);
        ClearAllInputs();
        if (errorText != null) errorText.text = "";
    }

    public void SwitchToForgotPassword()
    {
        if (isSubmitting) return;
        pendingNextPanel = null;
        HideMessageBoxImmediate();
        ShowOnlyPanel(forgotPasswordGroup);
        UpdateSendForgotButtonState();
        if (errorText != null) errorText.text = "";
    }

    public void SwitchToResetPassword()
    {
        pendingNextPanel = null;
        HideMessageBoxImmediate();
        ShowOnlyPanel(resetPasswordGroup);
        ClearResetInputs();
        if (errorText != null) errorText.text = "";
    }

    // ==================== MESSAGE BOX & PANEL TIẾP THEO ====================
    public void ShowMessageBox(string title, string content, GameObject nextPanel = null)
    {
        if (messageBoxPanel == null) return;

        pendingNextPanel = nextPanel;

        // Đưa tất cả panel (kể cả nextPanel) về trạng thái ẩn, sortingOrder = 0 trong lúc MessageBox mở
        SetPanelHiddenState(loginFormGroup);
        SetPanelHiddenState(registerFormGroup);
        SetPanelHiddenState(forgotPasswordGroup);
        SetPanelHiddenState(resetPasswordGroup);

        // Bật MessageBox ở sortingOrder = 1
        messageBoxPanel.SetActive(true);
        messageBoxPanel.transform.SetAsLastSibling();

        if (messageBoxText != null)
            messageBoxText.text = string.IsNullOrEmpty(title) ? content : $"{title}\n{content}";

        foreach (var c in messageBoxPanel.GetComponentsInChildren<Canvas>(true))
        {
            c.enabled = true;
            c.overrideSorting = true;
            c.sortingOrder = 1;
        }

        foreach (var cg in messageBoxPanel.GetComponentsInChildren<CanvasGroup>(true))
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }

    public void CloseMessageBox()
    {
        HideMessageBoxImmediate();

        // Chỉ khi bấm nút trên MessageBox thì panel tiếp theo (nextPanel) mới hiện lên
        if (pendingNextPanel != null)
        {
            ShowOnlyPanel(pendingNextPanel);
            pendingNextPanel = null;
        }
    }

    private void HideMessageBoxImmediate()
    {
        if (messageBoxPanel == null) return;

        foreach (var cg in messageBoxPanel.GetComponentsInChildren<CanvasGroup>(true))
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        foreach (var c in messageBoxPanel.GetComponentsInChildren<Canvas>(true))
        {
            c.overrideSorting = true;
            c.sortingOrder = 0;
        }

        messageBoxPanel.SetActive(false);
    }

    private void ShowOnlyPanel(GameObject targetPanel)
    {
        SetPanelVisibility(loginFormGroup, targetPanel == loginFormGroup);
        SetPanelVisibility(registerFormGroup, targetPanel == registerFormGroup);
        SetPanelVisibility(forgotPasswordGroup, targetPanel == forgotPasswordGroup);
        SetPanelVisibility(resetPasswordGroup, targetPanel == resetPasswordGroup);
    }

    private void SetPanelHiddenState(GameObject panel)
    {
        if (panel == null) return;

        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 0;
            canvas.enabled = false;
        }

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        panel.SetActive(false);
    }

    private void SetPanelVisibility(GameObject panel, bool isVisible)
    {
        if (panel == null) return;

        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 0;
            canvas.enabled = isVisible;
        }

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = isVisible ? 1f : 0f;
            cg.interactable = isVisible;
            cg.blocksRaycasts = isVisible;
        }

        panel.SetActive(isVisible);
    }

    // ==================== HÀM HỖ TRỢ ====================
    private void UpdateSendForgotButtonState()
    {
        if (sendForgotPasswordButton == null) return;
        sendForgotPasswordButton.interactable = !isSubmitting && forgotCooldownTimer <= 0f;
    }

    private void SetSubmitting(bool submitting)
    {
        isSubmitting = submitting;
        SetActionButtonsInteractable(!submitting);
    }

    private void SetActionButtonsInteractable(bool interactable)
    {
        if (loginButton != null) loginButton.interactable = interactable;
        if (registerButton != null) registerButton.interactable = interactable;
        if (confirmResetPasswordButton != null) confirmResetPasswordButton.interactable = interactable;

        if (sendForgotPasswordButton != null)
            sendForgotPasswordButton.interactable = interactable && forgotCooldownTimer <= 0f;
    }

    private void ClearRegisterInputs()
    {
        if (registerEmailInput != null) registerEmailInput.text = "";
        if (registerPasswordInput != null) registerPasswordInput.text = "";
        if (registerUsernameInput != null) registerUsernameInput.text = "";
    }

    private void ClearResetInputs()
    {
        if (resetCodeInput != null) resetCodeInput.text = "";
        if (newPasswordInput != null) newPasswordInput.text = "";
        if (confirmPasswordInput != null) confirmPasswordInput.text = "";
    }

    private void ClearAllInputs()
    {
        ClearRegisterInputs();
        ClearResetInputs();
        if (forgotPasswordEmailInput != null) forgotPasswordEmailInput.text = "";
    }
}