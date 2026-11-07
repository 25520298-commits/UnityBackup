using UnityEngine;

public class Controller : MonoBehaviour
{
    private Rigidbody rb;
    public float speed = 5f;
    
    [Header("Camera Reference")]
    [Tooltip("Kéo Main Camera hoặc Cinemachine Camera vào đây để lấy hướng")]
    public Transform cameraTransform; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionY
                         | RigidbodyConstraints.FreezeRotationX
                         | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;

        // Nếu chưa gán Camera, tự động tìm Camera chính
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 1. Lấy hướng trước/sau và trái/phải của Camera (chiếu xuống mặt phẳng XZ)
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // 2. Tính toán hướng di chuyển dựa trên input và hướng Camera
        // v > 0 (bấm W) -> đi theo camForward
        // h > 0 (bấm D) -> đi theo camRight
        Vector3 moveDirection = (camForward * v + camRight * h).normalized;

        // 3. Áp dụng vận tốc
        rb.linearVelocity = new Vector3(moveDirection.x * speed, rb.linearVelocity.y, moveDirection.z * speed);

        // 4. (Tùy chọn) Xoay nhân vật về hướng di chuyển
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            // Tạo góc xoay hướng về phía trước
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            // Xoay mượt mà
            //rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }
    }
}