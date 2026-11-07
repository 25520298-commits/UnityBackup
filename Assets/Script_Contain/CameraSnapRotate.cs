using UnityEngine;
using Unity.Cinemachine; // Bắt buộc phải có thư viện này

public class CameraSnapRotate : MonoBehaviour
{
    public float snapAngle = 90f; // Góc xoay mỗi lần bấm E
    public float rotateSpeed = 2f; // Tốc độ xoay mượt

    private CinemachinePanTilt panTilt;
    private float targetPanValue;
    private float currentPanValue;

    void Start()
    {
        // Lấy component Pan Tilt trên Camera
        panTilt = GetComponent<CinemachinePanTilt>();
        
        if (panTilt != null)
        {
            // Lấy giá trị xoay hiện tại
            currentPanValue = panTilt.PanAxis.Value;
            targetPanValue = currentPanValue;
        }
        else
        {
            Debug.LogError("Không tìm thấy Cinemachine Pan Tilt trên Camera!");
        }
    }

    void Update()
    {
        if (panTilt == null) return;

        // Nếu bấm E, cộng thêm góc xoay
        if (Input.GetKeyDown(KeyCode.E))
        {
            targetPanValue += snapAngle;
        }

        // Làm mượt giá trị xoay
        currentPanValue = Mathf.Lerp(currentPanValue, targetPanValue, Time.deltaTime * rotateSpeed);

        // Gán giá trị ngược lại vào Pan Tilt của Cinemachine
        panTilt.PanAxis.Value = currentPanValue;
    }
}