using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 20f;
    
    [Tooltip("Sát thương viên đạn này gây ra")]
    public int damage = 10;

    [Header("Xoay bù Model")]
    [Tooltip("Chỉnh góc xoay này nếu thân đạn bị nằm ngang. Thử X = 90 hoặc Y = 90.")]
    public Vector3 modelRotationOffset = Vector3.zero;

    [Header("Hiệu ứng phát sáng")]
    [Tooltip("Màu của vệt sáng và ánh sáng đạn")]
    public Color glowColor = Color.yellow;
    [Tooltip("Độ dài của vệt sáng")]
    public float trailTime = 0.1f;

    void Start()
    {
        // 1. Xoay bù model
        transform.rotation = transform.rotation * Quaternion.Euler(modelRotationOffset);

        // 2. Tự động thêm hiệu ứng phát sáng
        SetupGlowEffect();

        // 3. Tự hủy sau khoảng thời gian lifetime
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Bay thẳng theo trục Z (forward) của chính nó
        transform.position += transform.forward * (speed * Time.deltaTime);
    }

    private void SetupGlowEffect()
    {
        // --- Tạo vệt sáng (Trail Renderer) ---
        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = 0.1f; // Độ to của vệt sáng ở đầu đạn
        trail.endWidth = 0f;     // Thu nhỏ dần về đuôi
        trail.material = new Material(Shader.Find("Sprites/Default")); // Dùng shader cơ bản để không bị lỗi màu hồng
        trail.startColor = glowColor;
        trail.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f); // Mờ dần ở đuôi

        // --- Tạo ánh sáng (Point Light) ---
        GameObject lightObj = new GameObject("BulletLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;
        
        Light pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = glowColor;
        pointLight.range = 3f;    // Phạm vi chiếu sáng
        pointLight.intensity = 2f; // Độ sáng
    }
}