using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float lineOfSight = 10f;

    [Header("Wander")]
    public Transform moveSpots;
    [SerializeField] private float startWaitTime = 3f;
    [SerializeField] private float minX = -10f, maxX = 10f;
    [SerializeField] private float minZ = -10f, maxZ = 10f;

    [Header("Target & Raycast")]
    public Transform player;
    public LayerMask obstacleMask; 
    public Transform eyePoint; 

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 30f; 
    [SerializeField] private float iframeDuration = 0.2f;

    private NavMeshAgent agent;
    private Renderer rend;
    private Collider col;
    private Color originalColor;

    private float currentHealth;
    private float waitTime;
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

        agent.speed = speed;
        agent.stoppingDistance = 1f;
        currentHealth = maxHealth; 

        if (eyePoint == null) eyePoint = transform;

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

        if (distanceToPlayer < lineOfSight && CanSeePlayer())
        {
            agent.SetDestination(player.position);
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
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

    private bool CanSeePlayer()
    {
        Vector3 dirToPlayer = (player.position - eyePoint.position).normalized;
        if (Physics.Raycast(eyePoint.position, dirToPlayer, out RaycastHit hit, lineOfSight))
        {
            if (hit.transform == player || hit.transform.IsChildOf(player)) return true;
        }
        return false;
    }

    private void SetNewWanderTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * 10f;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 10f, 1))
        {
            agent.SetDestination(hit.position);
            if (moveSpots != null) moveSpots.position = hit.position;
        }
    }

    private void OnTriggerEnter(Collider hitInfo)
    {
        // --- DEBUG LOG: Kiểm tra xem Enemy có bị bắn trúng không ---
        Debug.Log($"<color=cyan>[Enemy] Bị chạm bởi: {hitInfo.gameObject.name} | Tag: {hitInfo.tag}</color>");

        if (isDead || isInvincible || hitInfo == null) return;
        
        // Nếu trúng đạn của Player
        if (hitInfo.CompareTag("Bullet"))
        {
            Debug.Log("<color=green>[Enemy] Đã nhận diện đạn của Player! Tiến hành trừ máu.</color>");
            
            float damage = 10f; 
            Bullet playerBullet = hitInfo.GetComponent<Bullet>();
            if (playerBullet != null)
            {
                damage = playerBullet.damage;
            }

            Destroy(hitInfo.gameObject);
            TakeDamage(damage);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        Debug.Log($"<color=red>[Enemy] Nhận {damage} sát thương. Máu còn: {currentHealth}/{maxHealth}</color>");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibleRoutine());
        }
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
        Debug.Log($"<color=red>[Enemy] {name} đã chết!</color>");
        
        if (col != null) col.enabled = false;
        if (agent != null) agent.isStopped = true;
        Destroy(gameObject, 0.1f);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (player == null || eyePoint == null) return;

        Vector3 dirToPlayer = (player.position - eyePoint.position).normalized;
        if (Physics.Raycast(eyePoint.position, dirToPlayer, out RaycastHit hit, lineOfSight, obstacleMask))
        {
            Gizmos.color = (hit.transform == player || hit.transform.IsChildOf(player)) ? Color.green : Color.red;
            Gizmos.DrawLine(eyePoint.position, hit.point);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(eyePoint.position, eyePoint.position + dirToPlayer * lineOfSight);
        }
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(eyePoint.position, lineOfSight);
    }
}