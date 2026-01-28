using UnityEngine;

public class SimpleEnemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float visionRange = 5f;
    [SerializeField] float attackRange = 1f;
    [SerializeField] float searchRange = 2f;

    [Header("Search Behavior")]
    [SerializeField] float searchIntensity = 1f; // 1 = normal, 2 = más peligroso
    [SerializeField] float searchCooldown = 2f;

    private Transform player;
    private PlayerController playerController;
    private float lastSearchTime = 0f;
    private Rigidbody2D rb;
    private bool isSearching = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerController = player.GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Verificar si el jugador existe y está vivo
        if (player == null || playerController == null || playerController.IsDead)
        {
            // Si el jugador murió, detenerse
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        // ¿Puede VER y ATACAR al jugador?
        if (playerController.CanBeSeenByEnemy() && playerController.CanBeAttackedByEnemy())
        {
            // Perseguir en la dirección CORRECTA
            Vector2 directionToPlayer = (player.position - transform.position);

            // Solo perseguir si está dentro del rango de visión
            if (distance <= visionRange)
            {
                // Normalizar la dirección y mover
                directionToPlayer.Normalize();
                rb.linearVelocity = directionToPlayer * moveSpeed;

                // Si está suficientemente cerca, atacar
                if (distance <= attackRange)
                {
                    AttackPlayer();
                }

                isSearching = false;
            }
            else
            {
                // Si está fuera de rango, parar o patrullar
                rb.linearVelocity = Vector2.zero;
            }
        }
        // ¿Está el jugador escondido cerca?
        else if (distance <= visionRange && playerController.IsInSafeZone)
        {
            // Detenerse para revisar
            rb.linearVelocity = Vector2.zero;

            // Revisar el escondite (con cooldown)
            if (Time.time - lastSearchTime >= searchCooldown)
            {
                isSearching = true;
                SearchForPlayer();
                lastSearchTime = Time.time;
            }
        }
        else
        {
            // Comportamiento de patrulla
            Patrol();
        }
    }

    void SearchForPlayer()
    {
        if (playerController == null || playerController.IsDead) return;

        Debug.Log("👀 Enemigo revisando el escondite...");

        // El jugador puede ser descubierto incluso sin collider
        bool found = playerController.CheckIfFound(transform.position, searchRange, searchIntensity);

        if (found)
        {
            Debug.Log("¡Enemigo encontró al jugador!");
            // El Game Over ya se maneja en el PlayerController (GetDiscovered())
        }
    }

    void AttackPlayer()
    {
        if (playerController == null || playerController.IsDead) return;

        Debug.Log("⚔️ Enemigo atacando!");

        // CAMBIO AQUÍ: Usar TakeDamage() en lugar de GameOver()
        playerController.TakeDamage();
    }

    void Patrol()
    {
        // Implementa tu lógica de patrulla aquí
        // Por ahora, solo se queda quieto
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 2f);
        }
    }

    // IMPORTANTE: Esto evita que empuje al jugador
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // No hacer nada - la física no empujará si el collider está desactivado
            // El ataque se maneja por distancia, no por colisión
        }
    }

    // También verificar OnCollisionStay2D por si acaso
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // No hacer nada aquí tampoco
        }
    }

    void OnDrawGizmosSelected()
    {
        // Rango de visión (persecución)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Rango de ataque
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Rango de búsqueda (escondites)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRange);
    }
}