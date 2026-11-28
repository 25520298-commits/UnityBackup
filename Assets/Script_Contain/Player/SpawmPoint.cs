using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Thời gian chờ trước khi hồi sinh (giây)")]
    public float respawnDelay = 1.5f;

    private void OnDrawGizmos()
    {
        // Vẽ một quả cầu màu xanh lá để dễ nhìn thấy điểm spawn trong cửa sổ Scene
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}