using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 20f;
    public int damage = 10;
    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += transform.forward * (speed * Time.deltaTime);
    }

    // ❌ KHÔNG có OnTriggerEnter — để EnemyGetHit tự xử lý
}