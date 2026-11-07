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
            SceneManager.LoadScene("LoginUI"); 
            return;
        }

        // 2. Hiển thị tên người chơi
        if (usernameText != null)
        {
            string nameToDisplay = AuthManager.Instance.PlayerDisplayName;
            
            if (string.IsNullOrEmpty(nameToDisplay))
            {
                nameToDisplay = "Player_" + AuthManager.Instance.PlayFabId.Substring(0, 5);
            }

            usernameText.text = "Chào, " + nameToDisplay;
        }
    }

    public void OnPlayGameClicked()
    {
        // SỬA TẠI ĐÂY: Đổi "SampleScene" thành "TestPlace" cho đúng với tên file của bạn
        SceneManager.LoadScene("TestPlace"); 
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