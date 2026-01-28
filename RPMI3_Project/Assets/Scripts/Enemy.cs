using UnityEngine;

public class SimpleEnemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float visionRange = 5f;
    [SerializeField] float attackRange = 1f;
    [SerializeField] float searchRange = 3f;

    [Header("Patrol Settings")]
    [SerializeField] bool enablePatrol = true;
    [SerializeField] float patrolDistance = 5f;
    [SerializeField] float waitTimeAtPoint = 1f;
    [SerializeField] Transform[] patrolPoints;

    [Header("Search Behavior")]
    [SerializeField] float searchIntensity = 1f;
    [SerializeField] float searchCooldown = 3f;
    [SerializeField] bool canSearchWhenHidden = true;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private Transform player;
    private PlayerController playerController;
    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 patrolTarget;
    private bool movingRight = true;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private int currentPatrolIndex = 0;
    private float lastSearchTime = 0f;
    private EnemyState currentState = EnemyState.Patrolling;

    private enum EnemyState
    {
        Patrolling,
        Chasing,
        Searching,
        Attacking
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        patrolTarget = GetNextPatrolTarget();
        FindPlayer();

        if (debugMode) Debug.Log($"👾 Enemigo iniciado en estado: {currentState}");
    }

    void Update()
    {
        if (player == null || playerController == null)
        {
            FindPlayer();
            if (player == null)
            {
                PatrolBehavior();
                return;
            }
        }

        // Si el jugador está muerto
        if (playerController.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            PatrolBehavior();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Patrolling:
                PatrolBehavior();

                // Transiciones
                if (distanceToPlayer <= visionRange && playerController.CanBeSeenByEnemy())
                {
                    currentState = EnemyState.Chasing;
                    if (debugMode) Debug.Log("👀 Jugador detectado - Persiguiendo");
                }
                else if (distanceToPlayer <= searchRange && playerController.IsInSafeZone)
                {
                    currentState = EnemyState.Searching;
                    if (debugMode) Debug.Log("🔍 Jugador escondido cerca - Buscando");
                }
                break;

            case EnemyState.Chasing:
                ChasePlayer(distanceToPlayer);

                if (distanceToPlayer > visionRange || !playerController.CanBeSeenByEnemy())
                {
                    currentState = EnemyState.Patrolling;
                    if (debugMode) Debug.Log("👣 Perdió al jugador - Patrullando");
                }
                else if (distanceToPlayer <= attackRange)
                {
                    currentState = EnemyState.Attacking;
                }
                break;

            case EnemyState.Searching:
                InvestigateHidingSpot(distanceToPlayer);

                if (distanceToPlayer > searchRange || !playerController.IsInSafeZone)
                {
                    currentState = EnemyState.Patrolling;
                    if (debugMode) Debug.Log("❌ Fuera de rango de búsqueda - Patrullando");
                }
                else if (playerController.CanBeSeenByEnemy())
                {
                    currentState = EnemyState.Chasing;
                    if (debugMode) Debug.Log("👀 Jugador visible - Persiguiendo");
                }
                break;

            case EnemyState.Attacking:
                if (distanceToPlayer <= attackRange && playerController.CanBeAttackedByEnemy())
                {
                    AttackPlayer();
                }
                else
                {
                    currentState = EnemyState.Chasing;
                }
                break;
        }
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = player.GetComponent<PlayerController>();
            if (debugMode) Debug.Log("✅ Jugador encontrado");
        }
    }

    void ChasePlayer(float distance)
    {
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        rb.linearVelocity = directionToPlayer * moveSpeed;

        // Rotar sprite según dirección
        if (directionToPlayer.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (directionToPlayer.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    void InvestigateHidingSpot(float distance)
    {
        // Detenerse
        rb.linearVelocity = Vector2.zero;

        // Revisar el escondite (con cooldown)
        if (Time.time - lastSearchTime >= searchCooldown)
        {
            SearchForPlayer();
            lastSearchTime = Time.time;
        }
    }

    void SearchForPlayer()
    {
        if (playerController == null || playerController.IsDead) return;

        if (debugMode) Debug.Log("🔦 Revisando escondite...");

        bool found = playerController.CheckIfFound(transform.position, searchRange, searchIntensity);

        if (found)
        {
            currentState = EnemyState.Chasing;
            if (debugMode) Debug.Log("🎯 ¡Encontrado! Persiguiendo...");
        }
    }

    void AttackPlayer()
    {
        if (playerController == null || playerController.IsDead) return;

        if (debugMode) Debug.Log("⚔️ Atacando al jugador!");
        playerController.TakeDamage();
    }

    void PatrolBehavior()
    {
        if (!enablePatrol)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            PatrolBetweenPoints();
        }
        else
        {
            SimpleBackAndForthPatrol();
        }
    }

    void PatrolBetweenPoints()
    {
        if (patrolPoints.Length == 0) return;

        Transform targetPoint = patrolPoints[currentPatrolIndex];

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            rb.linearVelocity = Vector2.zero;

            if (waitTimer >= waitTimeAtPoint)
            {
                isWaiting = false;
                waitTimer = 0f;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }
        else
        {
            Vector2 direction = (targetPoint.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;

            // Rotar sprite según dirección
            if (direction.x > 0)
                transform.localScale = new Vector3(1, 1, 1);
            else if (direction.x < 0)
                transform.localScale = new Vector3(-1, 1, 1);

            if (Vector2.Distance(transform.position, targetPoint.position) < 0.5f)
            {
                isWaiting = true;
            }
        }
    }

    void SimpleBackAndForthPatrol()
    {
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            rb.linearVelocity = Vector2.zero;

            if (waitTimer >= waitTimeAtPoint)
            {
                isWaiting = false;
                waitTimer = 0f;
                movingRight = !movingRight;
                patrolTarget = GetNextPatrolTarget();
            }
        }
        else
        {
            Vector2 direction = (patrolTarget - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;

            // Rotar sprite
            if (direction.x > 0)
                transform.localScale = new Vector3(1, 1, 1);
            else if (direction.x < 0)
                transform.localScale = new Vector3(-1, 1, 1);

            if (Vector2.Distance(transform.position, patrolTarget) < 0.5f)
            {
                isWaiting = true;
            }
        }
    }

    Vector2 GetNextPatrolTarget()
    {
        if (movingRight)
        {
            return startPosition + Vector2.right * patrolDistance;
        }
        else
        {
            return startPosition + Vector2.left * patrolDistance;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Rango de visión
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Rango de ataque
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Rango de búsqueda
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRange);
    }
}