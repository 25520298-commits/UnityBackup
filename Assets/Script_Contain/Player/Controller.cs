using UnityEngine;

public class Controller : MonoBehaviour
{
    private Rigidbody rb;
    
    [Header("Stats")]
    public float maxHealth = 100f; // Khai báo máu tối đa
    
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float rotationSpeed = 10f;
    
    [Header("Camera Reference")]
    public Transform cameraTransform; 

    public bool IsRunning { get; private set; }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Khóa trục Y và Rotation X/Z để nhân vật không bị ngã
        rb.constraints = RigidbodyConstraints.FreezePositionY
                         | RigidbodyConstraints.FreezeRotationX
                         | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Hướng di chuyển dựa trên Camera
        Vector3 moveDirection = (camForward * v + camRight * h).normalized;

        // Kiểm tra chạy
        IsRunning = Input.GetKey(KeyCode.LeftShift) && moveDirection.sqrMagnitude > 0.01f;
        
        float currentSpeed = IsRunning ? runSpeed : walkSpeed;

        // Di chuyển
        rb.linearVelocity = new Vector3(moveDirection.x * currentSpeed, rb.linearVelocity.y, moveDirection.z * currentSpeed);

        // --- SỬA LẠI PHẦN XOAY ---
        // Thay vì xoay theo hướng di chuyển, hãy xoay theo hướng Camera (hoặc giữ nguyên)
        // Nếu bạn muốn nhân vật xoay theo hướng Camera:
        Vector3 camLookDir = cameraTransform.forward;
        camLookDir.y = 0f;
        if (camLookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camLookDir);
            // Làm mượt việc xoay
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed));
        }
    }
}