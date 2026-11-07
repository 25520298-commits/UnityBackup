using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 20f;
    public int damage = 10;

    [Header("Hiệu ứng phát sáng")]
    [Tooltip("Màu của vệt sáng và ánh sáng đạn (Mặc định màu đỏ cho Enemy)")]
    public Color glowColor = Color.red; 
    [Tooltip("Độ dài của vệt sáng")]
    public float trailTime = 0.1f;

    void Start()
    {
        // Tự động thêm hiệu ứng phát sáng
        SetupGlowEffect();

        // Tự hủy sau khoảng thời gian lifetime
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
        trail.startWidth = 0.15f; // Đạn Enemy cho vệt to hơn một chút để dễ nhìn
        trail.endWidth = 0f;     
        trail.material = new Material(Shader.Find("Sprites/Default")); 
        trail.startColor = glowColor;
        trail.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f); 

        // --- Tạo ánh sáng (Point Light) ---
        GameObject lightObj = new GameObject("EnemyBulletLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;
        
        Light pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = glowColor;
        pointLight.range = 3f;    
        pointLight.intensity = 2f; 
    }

    // ❌ KHÔNG có OnTriggerEnter — để EnemyGetHit tự xử lý (hoặc PlayerGetHit)
}