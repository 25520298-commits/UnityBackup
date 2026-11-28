using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using PlayFab;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public bool IsLoggedIn { get; private set; } = false;
    public string PlayFabId { get; private set; }
    public string PlayerDisplayName { get; private set; }

    [Header("Backend Settings")]
    [Tooltip("Địa chỉ IP/URL của Backend (VD: http://IP_AZURE/api/Auth hoặc https://fpsgamebackend.duckdns.org/api/Auth)")]
    public string backendUrl = "https://fpsgamebackend.duckdns.org/api/Auth";

    private string jwtToken;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ==================== 1. ĐĂNG NHẬP ====================
    public void Login(string email, string password, System.Action<string> onError, System.Action onSuccess)
    {
        StartCoroutine(LoginRoutine(email, password, onError, onSuccess));
    }

    IEnumerator LoginRoutine(string email, string password, System.Action<string> onError, System.Action onSuccess)
    {
        var loginData = new LoginRequestDTO { Email = email, Password = password };
        string json = JsonUtility.ToJson(loginData);

        using (UnityWebRequest req = new UnityWebRequest($"{backendUrl}/login", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            string responseText = req.downloadHandler.text;

            if (req.result != UnityWebRequest.Result.Success)
            {
                if (req.responseCode == 429)
                {
                    onError?.Invoke("Bạn thao tác quá nhanh, vui lòng thử lại sau 1 phút!");
                    yield break;
                }

                var errObj = TryParseServerMessage(responseText);
                onError?.Invoke(!string.IsNullOrEmpty(errObj?.message) ? errObj.message : "Sai tài khoản hoặc mật khẩu!");
                yield break;
            }

            var response = JsonUtility.FromJson<LoginResponseDTO>(responseText);
            jwtToken = response.token;
            PlayerDisplayName = response.username;
            PlayFabId = response.playFabId;

            PlayFabSettings.staticPlayer.ClientSessionTicket = response.sessionTicket;
            PlayFabSettings.staticPlayer.PlayFabId = response.playFabId;
            PlayFabSettings.staticPlayer.EntityToken = response.entityToken;

            IsLoggedIn = true;
            onSuccess?.Invoke();
        }
    }

    // ==================== 2. ĐĂNG KÝ ====================
    public void Register(string email, string password, string username, System.Action<string> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(RegisterRoutine(email, password, username, onSuccess, onError));
    }

    IEnumerator RegisterRoutine(string email, string password, string username, System.Action<string> onSuccess, System.Action<string> onError)
    {
        var registerData = new RegisterRequestDTO { Email = email, Password = password, Username = username };
        string json = JsonUtility.ToJson(registerData);

        using (UnityWebRequest req = new UnityWebRequest($"{backendUrl}/register", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            string responseText = req.downloadHandler.text;
            var parsed = TryParseServerMessage(responseText);

            if (req.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(parsed?.message ?? "Tạo tài khoản thành công!");
            }
            else
            {
                if (req.responseCode == 429)
                {
                    onError?.Invoke("Bạn thao tác quá nhanh, vui lòng thử lại sau 1 phút!");
                    yield break;
                }

                string serverMsg = !string.IsNullOrEmpty(parsed?.message) ? parsed.message : req.error;
                onError?.Invoke("Lỗi: " + serverMsg);
            }
        }
    }

    // ==================== 3. QUÊN MẬT KHẨU (BƯỚC 1: GỬI MÃ VỀ EMAIL) ====================
    public void ForgotPassword(string email, System.Action<string> onSuccess, System.Action<string> onError)
    {
        StartCoroutine(ForgotRoutine(email, onSuccess, onError));
    }

    IEnumerator ForgotRoutine(string email, System.Action<string> onSuccess, System.Action<string> onError)
    {
        var forgotData = new ForgotPasswordRequestDTO { Email = email };
        string json = JsonUtility.ToJson(forgotData);

        using (UnityWebRequest req = new UnityWebRequest($"{backendUrl}/forgot-password", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            string responseText = req.downloadHandler.text;
            var parsed = TryParseServerMessage(responseText);

            if (req.result == UnityWebRequest.Result.Success)
            {
                if (!string.IsNullOrEmpty(parsed?.devToken))
                    Debug.Log($"<color=yellow>[DEV] Mã khôi phục của bạn là: {parsed.devToken}</color>");

                onSuccess?.Invoke(parsed?.message ?? "Mã khôi phục đã được gửi vào email!");
            }
            else
            {
                if (req.responseCode == 429)
                {
                    onError?.Invoke("Bạn gửi yêu cầu quá nhanh, vui lòng đợi 1 phút!");
                    yield break;
                }

                string serverMsg = !string.IsNullOrEmpty(parsed?.message) ? parsed.message : req.error;
                onError?.Invoke("Lỗi: " + serverMsg);
            }
        }
    }

    // ==================== 4. ĐẶT LẠI MẬT KHẨU (BƯỚC 2: KIỂM TRA MÃ & ĐỔI MẬT KHẨU) ====================
    public void ResetPassword(string email, string token, string newPassword, System.Action<string> onSuccess, System.Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            onError?.Invoke("Mã xác nhận không được để trống!");
            return;
        }
        StartCoroutine(ResetPasswordRoutine(email, token, newPassword, onSuccess, onError));
    }

    IEnumerator ResetPasswordRoutine(string email, string token, string newPassword, System.Action<string> onSuccess, System.Action<string> onError)
    {
        var resetData = new ResetPasswordRequestDTO
        {
            Email = email.Trim(),
            Token = token.Trim(),
            NewPassword = newPassword
        };
        string json = JsonUtility.ToJson(resetData);

        using (UnityWebRequest req = new UnityWebRequest($"{backendUrl}/reset-password", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            string responseText = req.downloadHandler.text;
            var parsed = TryParseServerMessage(responseText);

            if (req.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(parsed?.message ?? "Đặt lại mật khẩu thành công!");
            }
            else
            {
                if (req.responseCode == 429)
                {
                    onError?.Invoke("Bạn thao tác quá nhanh, vui lòng thử lại sau 1 phút!");
                    yield break;
                }

                string serverMsg = !string.IsNullOrEmpty(parsed?.message) ? parsed.message : req.error;
                onError?.Invoke("Lỗi: " + serverMsg);
            }
        }
    }

    // ==================== 5. ĐỔI MẬT KHẨU KHI ĐÃ ĐĂNG NHẬP (YÊU CẦU JWT) ====================
    public void ChangePassword(string oldPassword, string newPassword, System.Action<string> onSuccess, System.Action<string> onError)
    {
        if (string.IsNullOrEmpty(jwtToken))
        {
            onError?.Invoke("Bạn chưa đăng nhập!");
            return;
        }
        StartCoroutine(ChangePasswordRoutine(oldPassword, newPassword, onSuccess, onError));
    }

    IEnumerator ChangePasswordRoutine(string oldPassword, string newPassword, System.Action<string> onSuccess, System.Action<string> onError)
    {
        var changeData = new ChangePasswordRequestDTO { OldPassword = oldPassword, NewPassword = newPassword };
        string json = JsonUtility.ToJson(changeData);

        using (UnityWebRequest req = new UnityWebRequest($"{backendUrl}/change-password", "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

            yield return req.SendWebRequest();

            string responseText = req.downloadHandler.text;
            var parsed = TryParseServerMessage(responseText);

            if (req.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(parsed?.message ?? "Đổi mật khẩu thành công!");
            }
            else
            {
                string serverMsg = !string.IsNullOrEmpty(parsed?.message) ? parsed.message : req.error;
                onError?.Invoke("Lỗi: " + serverMsg);
            }
        }
    }

    // ==================== 6. ĐĂNG XUẤT ====================
    public void Logout()
    {
        IsLoggedIn = false;
        PlayFabId = null;
        PlayerDisplayName = null;
        jwtToken = null;

        PlayFabSettings.staticPlayer.ClientSessionTicket = null;
        PlayFabSettings.staticPlayer.EntityToken = null;
        PlayFabSettings.staticPlayer.PlayFabId = null;
        PlayFabClientAPI.ForgetAllCredentials();

        PlayerPrefs.DeleteKey("PlayFabSessionTicket");
        PlayerPrefs.DeleteKey("PlayFabId");
        PlayerPrefs.Save();

        Debug.Log("<color=green>Đã đăng xuất!</color>");
    }

    // ==================== HÀM PHỤ TRỢ PARSE JSON AN TOÀN ====================
    private ServerMessageDTO TryParseServerMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonUtility.FromJson<ServerMessageDTO>(json);
        }
        catch
        {
            return null;
        }
    }

    // ==================== CÁC LỚP DTO ====================
    [System.Serializable] private class LoginRequestDTO { public string Email; public string Password; }
    [System.Serializable] private class LoginResponseDTO { public string token; public string username; public string playFabId; public string sessionTicket; public string entityToken; }
    [System.Serializable] private class RegisterRequestDTO { public string Email; public string Password; public string Username; }
    [System.Serializable] private class ForgotPasswordRequestDTO { public string Email; }
    [System.Serializable] private class ResetPasswordRequestDTO { public string Email; public string Token; public string NewPassword; }
    [System.Serializable] private class ChangePasswordRequestDTO { public string OldPassword; public string NewPassword; }
    [System.Serializable] private class ServerMessageDTO { public string message; public string devToken; }
}