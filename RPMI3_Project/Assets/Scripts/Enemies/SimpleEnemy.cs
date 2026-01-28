using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// Enemigo con patrullaje, persecución y sistema de búsqueda en escondites.

public class SimpleEnemy : MonoBehaviour
{
    #region VARIABLES
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer rend;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float searchSpeed = 1.5f;

    [Header("Detection Settings")]
    [SerializeField] private float visionRange = 5f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float searchRange = 2f;
    [SerializeField] private float hearingRange = 3f;

    [Header("Patrol Settings")]
    [SerializeField] private bool enablePatrol = true;
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float waitTimeAtPoint = 1f;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Search Behavior")]
    [SerializeField] private float searchIntensity = 1f;
    [SerializeField] private float searchCooldown = 3f;
    [SerializeField] private float randomSearchChance = 0.2f;
    [SerializeField] private Transform[] hidingSpotsToCheck;

    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color alertColor = Color.yellow;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color searchColor = Color.cyan;

    // Estado
    private EnemyState currentState = EnemyState.Patrol;
    private Transform player;
    private PlayerController playerController;
    private Vector2 startPosition;
    private Vector2 patrolTarget;
    private Vector2 lastKnownPlayerPos;

    // Patrol
    private bool movingRight = true;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private int currentPatrolIndex = 0;

    // Search
    private float lastSearchTime = 0f;
    private float searchTimer = 0f;
    private int currentSearchSpotIndex = -1;
    #endregion

    #region UNITY METHODS
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rend == null) rend = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        startPosition = transform.position;
        patrolTarget = GetNextPatrolTarget();
        FindPlayer();
    }

    private void Update()
    {
        if (player == null || playerController == null)
        {
            FindPlayer();
            if (player == null)
            {
                if (enablePatrol) PatrolBehavior();
                return;
            }
        }

        // Si el jugador está muerto
        if (playerController.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            ChangeState(EnemyState.Patrol);
            return;
        }

        // Máquina de estados
        switch (currentState)
        {
            case EnemyState.Patrol:
                HandlePatrolState();
                break;
            case EnemyState.Chase:
                HandleChaseState();
                break;
            case EnemyState.Search:
                HandleSearchState();
                break;
            case EnemyState.Investigate:
                HandleInvestigateState();
                break;
        }

        UpdateVisuals();
    }
    #endregion

    #region STATE HANDLERS
    private void HandlePatrolState()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // ¿Puede ver al jugador?
        if (distanceToPlayer <= visionRange && playerController.CanBeSeenByEnemy())
        {
            lastKnownPlayerPos = player.position;
            ChangeState(EnemyState.Chase);
            return;
        }

        // ¿Hay un escondite cerca que revisar?
        if (Time.time - lastSearchTime >= searchCooldown)
        {
            if (distanceToPlayer <= searchRange && playerController.IsInSafeZone)
            {
                ChangeState(EnemyState.Investigate);
                return;
            }

            // Búsqueda aleatoria
            if (Random.value < randomSearchChance * Time.deltaTime)
            {
                TrySearchRandomSpot();
            }
        }

        PatrolBehavior();
    }

    private void HandleChaseState()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // ¿Puede seguir viendo al jugador?
        if (playerController.CanBeSeenByEnemy())
        {
            lastKnownPlayerPos = player.position;

            // Moverse hacia el jugador
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * chaseSpeed;

            // ¿Puede atacar?
            if (distanceToPlayer <= attackRange)
            {
                AttackPlayer();
            }
        }
        else
        {
            // Perdió de vista al jugador, buscar
            ChangeState(EnemyState.Search);
        }
    }

    private void HandleSearchState()
    {
        searchTimer -= Time.deltaTime;

        // Moverse hacia la última posición conocida
        Vector2 direction = (lastKnownPlayerPos - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, lastKnownPlayerPos);

        if (distance > 0.5f)
        {
            rb.linearVelocity = direction * searchSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;

            // Mirar alrededor
            if (searchTimer <= 0)
            {
                // ¿El jugador está cerca y visible?
                float distanceToPlayer = Vector2.Distance(transform.position, player.position);

                if (distanceToPlayer <= visionRange && playerController.CanBeSeenByEnemy())
                {
                    ChangeState(EnemyState.Chase);
                    return;
                }

                // Revisar escondites cercanos
                if (distanceToPlayer <= searchRange && playerController.IsHidden)
                {
                    SearchForPlayer();
                }

                // Volver a patrullar
                ChangeState(EnemyState.Patrol);
            }
        }
    }

    private void HandleInvestigateState()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Detenerse para investigar
        rb.linearVelocity = Vector2.zero;

        // Revisar el escondite
        if (Time.time - lastSearchTime >= 0.5f)
        {
            bool found = SearchForPlayer();
            lastSearchTime = Time.time;

            if (!found)
            {
                // No encontró nada, volver a patrullar
                ChangeState(EnemyState.Patrol);
            }
        }
    }
    #endregion

    #region BEHAVIORS
    private void PatrolBehavior()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            PatrolBetweenPoints();
        }
        else
        {
            SimpleBackAndForthPatrol();
        }
    }

    private void PatrolBetweenPoints()
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
            Vector2 direction = ((Vector2)targetPoint.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;

            // Voltear sprite
            if (direction.x != 0)
            {
                rend.flipX = direction.x < 0;
            }

            if (Vector2.Distance(transform.position, targetPoint.position) < 0.5f)
            {
                isWaiting = true;
            }
        }
    }

    private void SimpleBackAndForthPatrol()
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

            // Voltear sprite
            rend.flipX = !movingRight;

            if (Vector2.Distance(transform.position, patrolTarget) < 0.5f)
            {
                isWaiting = true;
            }
        }
    }

    private Vector2 GetNextPatrolTarget()
    {
        return movingRight
            ? startPosition + Vector2.right * patrolDistance
            : startPosition + Vector2.left * patrolDistance;
    }

    private bool SearchForPlayer()
    {
        if (playerController == null || playerController.IsDead) return false;

        Debug.Log("Enemigo revisando escondite...");

        bool found = playerController.CheckIfFound(transform.position, searchRange, searchIntensity);

        if (found)
        {
            Debug.Log("¡Enemigo encontró al jugador!");
            return true;
        }
        else
        {
            Debug.Log("El enemigo no encontró nada...");
            return false;
        }
    }

    private void TrySearchRandomSpot()
    {
        if (hidingSpotsToCheck == null || hidingSpotsToCheck.Length == 0) return;

        // Elegir un escondite aleatorio
        int randomIndex = Random.Range(0, hidingSpotsToCheck.Length);
        Transform spot = hidingSpotsToCheck[randomIndex];

        if (spot == null) return;

        float distanceToSpot = Vector2.Distance(transform.position, spot.position);

        // Solo ir si está relativamente cerca
        if (distanceToSpot <= visionRange * 1.5f)
        {
            lastKnownPlayerPos = spot.position;
            currentSearchSpotIndex = randomIndex;
            ChangeState(EnemyState.Search);

            Debug.Log($"Enemigo va a revisar escondite: {spot.name}");
        }
    }

    private void AttackPlayer()
    {
        if (playerController == null || playerController.IsDead) return;
        if (!playerController.CanBeAttackedByEnemy()) return;

        Debug.Log("¡Enemigo atacando!");
        playerController.TakeDamage();
    }
    #endregion

    #region STATE MANAGEMENT
    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        // Salir del estado actual
        switch (currentState)
        {
            case EnemyState.Chase:
                // Nada especial
                break;
            case EnemyState.Search:
                searchTimer = 0f;
                break;
        }

        currentState = newState;

        // Entrar al nuevo estado
        switch (newState)
        {
            case EnemyState.Patrol:
                Debug.Log("Enemigo: Patrullando");
                break;
            case EnemyState.Chase:
                Debug.Log("Enemigo: ¡Persiguiendo!");
                break;
            case EnemyState.Search:
                Debug.Log("Enemigo: Buscando...");
                searchTimer = 3f;
                break;
            case EnemyState.Investigate:
                Debug.Log("Enemigo: Investigando escondite");
                break;
        }
    }
    #endregion

    #region UTILITY
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = player.GetComponent<PlayerController>();
        }
    }

    private void UpdateVisuals()
    {
        Color targetColor = currentState switch
        {
            EnemyState.Patrol => normalColor,
            EnemyState.Chase => chaseColor,
            EnemyState.Search => searchColor,
            EnemyState.Investigate => alertColor,
            _ => normalColor
        };

        rend.color = Color.Lerp(rend.color, targetColor, Time.deltaTime * 5f);
    }
    #endregion

    #region GIZMOS
    private void OnDrawGizmosSelected()
    {
        // Rango de visión
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Rango de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Rango de búsqueda
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, searchRange);

        // Rango de audición
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        // Puntos de patrulla
        if (patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform point in patrolPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.3f);
                }
            }
        }

        // Escondites a revisar
        if (hidingSpotsToCheck != null)
        {
            Gizmos.color = Color.magenta;
            foreach (Transform spot in hidingSpotsToCheck)
            {
                if (spot != null)
                {
                    Gizmos.DrawWireCube(spot.position, Vector3.one * 0.5f);
                }
            }
        }
    }
    #endregion
}

public enum EnemyState
{
    Patrol,
    Chase,
    Search,
    Investigate
}