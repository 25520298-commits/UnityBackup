using UnityEngine;

public class RunEffect : MonoBehaviour
{
    private Controller controller;
    
    [Header("Run Dust Effect")]
    public ParticleSystem dustParticles; // Kéo object RunDustEffect vào đây

    void Start()
    {
        controller = GetComponent<Controller>();
        
        if (dustParticles != null)
        {
            // Tắt hiệu ứng lúc mới vào game
            var emission = dustParticles.emission;
            emission.enabled = false;
        }
    }

    void Update()
    {
        if (controller == null || dustParticles == null) return;

        // Lấy module phát hạt (Emission)
        var emission = dustParticles.emission;

        // Nếu đang chạy -> Bật phát hạt
        if (controller.IsRunning)
        {
            if (!emission.enabled)
            {
                emission.enabled = true;
                dustParticles.Play(); // Đảm bảo nó đang chạy
            }
        }
        // Nếu không chạy -> Tắt phát hạt
        else
        {
            if (emission.enabled)
            {
                emission.enabled = false;
            }
        }
    }
}