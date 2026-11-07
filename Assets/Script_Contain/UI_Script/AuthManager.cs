using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public bool IsLoggedIn { get; private set; } = false;
    public string PlayFabId { get; private set; }
    public string PlayerDisplayName { get; private set; }
    private int loginFailCount = 0; 

    [Header("Scene Settings")]
    public string loginSceneName = "LoginUI"; 
    public string gameSceneName = "MainMenuUI";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            PlayFabSettings.TitleId = "1703F9"; 
        }
    }

    private void Start()
    {
        Debug.Log("Yêu cầu đăng nhập.");
    }

    // Hàm mới: Reset số lần đăng nhập sai khi người dùng đổi form
    public void ResetLoginAttempts()
    {
        loginFailCount = 0;
    }

    // ==================== ĐĂNG NHẬP ====================
    public void Login(string email, string password, Action<string> onError, Action onRequireRegister)
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetPlayerProfile = true }
        };

        PlayFabClientAPI.LoginWithEmailAddress(request, 
            result => {
                IsLoggedIn = true;
                PlayFabId = result.PlayFabId;
                loginFailCount = 0; 

                if (result.InfoResultPayload != null && result.InfoResultPayload.PlayerProfile != null)
                {
                    PlayerDisplayName = result.InfoResultPayload.PlayerProfile.DisplayName;
                }

                SceneManager.LoadScene(gameSceneName);
            }, 
            error => {
                if (error.Error == PlayFabErrorCode.InvalidParams || 
                    error.Error == PlayFabErrorCode.AccountNotFound || 
                    error.Error == PlayFabErrorCode.InvalidEmailOrPassword)
                {
                    loginFailCount++;
                    if (loginFailCount >= 3)
                    {
                        onRequireRegister?.Invoke(); 
                        loginFailCount = 0; 
                    }
                    else
                    {
                        onError?.Invoke("Sai email hoặc mật khẩu. Vui lòng thử lại.");
                    }
                }
                else
                {
                    onError?.Invoke("Lỗi kết nối: " + error.ErrorMessage);
                }
            });
    }

    // ==================== ĐĂNG KÝ ====================
    public void Register(string email, string password, string username, Action<string> onSuccess, Action<string> onError)
    {
        var request = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,
            Username = username,
            RequireBothUsernameAndEmail = true 
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, 
            result => {
                LoginAfterRegister(email, password, username, onSuccess, onError);
            }, 
            error => {
                string errorMsg = error.ErrorMessage;
                if (error.Error == PlayFabErrorCode.EmailAddressNotAvailable) errorMsg = "Email này đã được sử dụng!";
                else if (error.Error == PlayFabErrorCode.UsernameNotAvailable) errorMsg = "Tên đăng nhập này đã có người sử dụng!";
                else if (error.Error == PlayFabErrorCode.InvalidPassword) errorMsg = "Mật khẩu không hợp lệ!";
                onError?.Invoke(errorMsg);
            });
    }

    private void LoginAfterRegister(string email, string password, string username, Action<string> onSuccess, Action<string> onError)
    {
        var loginRequest = new LoginWithEmailAddressRequest { Email = email, Password = password };

        PlayFabClientAPI.LoginWithEmailAddress(loginRequest,
            loginResult => {
                SetDisplayName(username, 
                    () => {
                        onSuccess?.Invoke("Tạo tài khoản thành công! Vui lòng đăng nhập.");
                        PlayFabClientAPI.ForgetAllCredentials(); 
                    },
                    (displayError) => { onError?.Invoke("Lỗi đặt tên hiển thị: " + displayError); });
            },
            loginError => { onError?.Invoke("Lỗi đăng nhập tự động: " + loginError.ErrorMessage); });
    }

    private void SetDisplayName(string username, Action onSuccess, Action<string> onError)
    {
        var request = new UpdateUserTitleDisplayNameRequest { DisplayName = username };
        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            result => { onSuccess?.Invoke(); },
            error => { onError?.Invoke(error.ErrorMessage); });
    }

    // ==================== QUÊN MẬT KHẨU ====================
    public void ForgotPassword(string email, Action<string> onSuccess, Action<string> onError)
    {
        var request = new SendAccountRecoveryEmailRequest { Email = email, TitleId = PlayFabSettings.TitleId };

        PlayFabClientAPI.SendAccountRecoveryEmail(request, 
            result => { onSuccess?.Invoke("Đã gửi email khôi phục!"); }, 
            error => { onError?.Invoke("Không thể gửi email."); });
    }

    // ==================== ĐĂNG XUẤT ====================
    public void Logout()
    {
        IsLoggedIn = false;
        PlayFabId = null;
        PlayerDisplayName = null;
        loginFailCount = 0;
        PlayerPrefs.DeleteKey("PlayFabSessionTicket");
        PlayerPrefs.DeleteKey("PlayFabId");
        PlayerPrefs.Save();
        PlayFabClientAPI.ForgetAllCredentials();
        SceneManager.LoadScene(loginSceneName);
    }

    // ==================== QUẢN LÝ PLAYER DATA ====================
    public void SavePlayerData(string key, string value)
    {
        if (!IsLoggedIn) return;
        var request = new UpdateUserDataRequest { Data = new Dictionary<string, string> { { key, value } } };
        PlayFabClientAPI.UpdateUserData(request, result => Debug.Log($"Đã lưu {key}"), error => Debug.LogError(error.ErrorMessage));
    }

    public void LoadPlayerData(string key, Action<string> onSuccess)
    {
        if (!IsLoggedIn) return;
        var request = new GetUserDataRequest { Keys = new List<string> { key } };
        PlayFabClientAPI.GetUserData(request, 
            result => {
                if (result.Data != null && result.Data.ContainsKey(key)) onSuccess?.Invoke(result.Data[key].Value);
                else onSuccess?.Invoke(null);
            }, 
            error => Debug.LogError(error.ErrorMessage));
    }
}