using UnityEngine;

public class Controller : MonoBehaviour
{
    private Rigidbody rb;
    
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f; // Tốc độ khi chạy
    public float rotationSpeed = 10f;
    
    [Header("Camera Reference")]
    public Transform cameraTransform; 

    // Biến này để các script khác biết bạn đang chạy hay không
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

        Vector3 moveDirection = (camForward * v + camRight * h).normalized;

        // KIỂM TRA PHÍM SHIFT
        // IsRunning = true nếu đang giữ Shift VÀ đang có di chuyển
        IsRunning = Input.GetKey(KeyCode.LeftShift) && moveDirection.sqrMagnitude > 0.01f;
        
        // Chọn tốc độ
        float currentSpeed = IsRunning ? runSpeed : walkSpeed;

        // Áp dụng vận tốc di chuyển
        rb.linearVelocity = new Vector3(moveDirection.x * currentSpeed, rb.linearVelocity.y, moveDirection.z * currentSpeed);

        // Xoay nhân vật
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed));
        }
    }
}