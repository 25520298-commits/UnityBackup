using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("--- UI References ---")]
    [Tooltip("Kéo object Text hiển thị tên người chơi vào đây")]
    public TextMeshProUGUI usernameText; 

    void Start()
    {
        // 1. Kiểm tra bảo mật: Nếu chưa đăng nhập thì đá về Login
        if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("Chưa đăng nhập! Chuyển về màn hình Login.");
            
            // Kiểm tra xem Scene LoginUI có tồn tại trong Build Settings không trước khi load
            if (Application.CanStreamedLevelBeLoaded("LoginUI"))
            {
                SceneManager.LoadScene("LoginUI"); 
            }
            else
            {
                Debug.LogError("LỖI: Scene 'LoginUI' chưa được thêm vào Build Settings!");
            }
            return;
        }

        // 2. Hiển thị tên người chơi
        if (usernameText != null)
        {
            string nameToDisplay = AuthManager.Instance.PlayerDisplayName;
            
            if (string.IsNullOrEmpty(nameToDisplay) && !string.IsNullOrEmpty(AuthManager.Instance.PlayFabId))
            {
                // Cắt chuỗi an toàn (tránh lỗi nếu PlayFabId quá ngắn)
                string shortId = AuthManager.Instance.PlayFabId.Length > 5 
                    ? AuthManager.Instance.PlayFabId.Substring(0, 5) 
                    : AuthManager.Instance.PlayFabId;
                nameToDisplay = "Player_" + shortId;
            }
            else if (string.IsNullOrEmpty(nameToDisplay))
            {
                nameToDisplay = "Khách";
            }

            usernameText.text = "Chào, " + nameToDisplay;
        }
    }

    public void OnPlayGameClicked()
    {
        // Kiểm tra xem Scene TestPlace có tồn tại không
        if (Application.CanStreamedLevelBeLoaded("TestPlace"))
        {
            SceneManager.LoadScene("TestPlace"); 
        }
        else
        {
            Debug.LogError("LỖI: Scene 'TestPlace' chưa được thêm vào Build Settings!");
        }
    }

    public void OnLogoutClicked()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.Logout();
        }
    }

    public void OnExitClicked()
    {
        Debug.Log("Đang thoát game...");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}