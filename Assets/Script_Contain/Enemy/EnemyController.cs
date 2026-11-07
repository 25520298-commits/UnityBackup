using System.Collections;
using UnityEngine;
using UnityEngine.AI; // Thêm thư viện NavMesh

public class EnemyController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float lineOfSight = 6f;

    [Header("Wander")]
    public Transform moveSpots;
    [SerializeField] private float startWaitTime = 3f;
    [SerializeField] private float minX = -10f, maxX = 10f;
    [SerializeField] private float minZ = -10f, maxZ = 10f;

    [Header("Target")]
    public Transform player;

    [Header("Hit Settings")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float iframeDuration = 0.2f;

    private NavMeshAgent agent; // Thay thế Rigidbody bằng NavMeshAgent
    private Renderer rend;
    private Collider col;
    private Color originalColor;

    private float waitTime;
    private int currentHits = 0;
    private bool isInvincible = false;
    private bool isDead = false;

    public bool IsDead => isDead;
    public Transform Player => player;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();

        if (agent == null)
        {
            Debug.LogError($"[{name}] EnemyController cần NavMeshAgent!");
            enabled = false;
            return;
        }

        // Cấu hình Agent
        agent.speed = speed;
        agent.stoppingDistance = 1f; // Dừng lại khi cách mục tiêu 1m

        if (rend != null)
        {
            if (rend.material.HasProperty("_Color")) originalColor = rend.material.color;
            else if (rend.material.HasProperty("_BaseColor")) originalColor = rend.material.GetColor("_BaseColor");
            else originalColor = Color.white;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        waitTime = startWaitTime;
        SetNewWanderTarget();
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < lineOfSight)
        {
            // Đuổi theo Player: NavMesh tự động tìm đường vòng qua tường
            agent.SetDestination(player.position);
        }
        else if (moveSpots != null)
        {
            // Đi tuần tra
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // Đã đến điểm tuần tra, đứng chờ
                agent.isStopped = true;
                
                if (waitTime <= 0f)
                {
                    SetNewWanderTarget();
                    waitTime = startWaitTime;
                    agent.isStopped = false;
                }
                else waitTime -= Time.deltaTime;
            }
        }
    }

    private void SetNewWanderTarget()
    {
        // Tìm một điểm ngẫu nhiên trên NavMesh
        Vector3 randomDirection = Random.insideUnitSphere * 10f;
        randomDirection += transform.position;
        NavMeshHit hit;
        
        if (NavMesh.SamplePosition(randomDirection, out hit, 10f, 1))
        {
            agent.SetDestination(hit.position);
            if (moveSpots != null) moveSpots.position = hit.position;
        }
    }

    // Raycast để kiểm tra xem có nhìn thấy Player không (tránh nhìn xuyên tường)
    private bool HasLineOfSightToPlayer()
    {
        if (player == null) return false;
        
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        RaycastHit hit;
        
        // Bắn tia từ Enemy tới Player
        if (Physics.Raycast(transform.position, dirToPlayer, out hit, lineOfSight))
        {
            // Nếu tia chạm vào Player thì là có thể nhìn thấy
            if (hit.transform == player)
            {
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter(Collider hitInfo)
    {
        if (isDead || isInvincible || hitInfo == null) return;
        if (hitInfo.CompareTag("Bullet"))
        {
            Destroy(hitInfo.gameObject);
            TakeHit();
        }
    }

    public void TakeHit()
    {
        if (isDead) return;
        currentHits++;
        if (currentHits >= maxHits) Die();
        else StartCoroutine(InvincibleRoutine());
    }

    private IEnumerator InvincibleRoutine()
    {
        isInvincible = true;
        for (int i = 0; i < 3; i++)
        {
            SetColor(Color.red);
            yield return new WaitForSeconds(iframeDuration * 0.5f);
            SetColor(originalColor);
            yield return new WaitForSeconds(iframeDuration * 0.5f);
        }
        isInvincible = false;
    }

    private void SetColor(Color color)
    {
        if (rend == null) return;
        if (rend.material.HasProperty("_Color")) rend.material.color = color;
        else if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", color);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        if (col != null) col.enabled = false;
        if (agent != null) agent.isStopped = true; // Dừng di chuyển
        Destroy(gameObject, 0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, lineOfSight);
    }
}