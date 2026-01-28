using UnityEngine;

/// Zona donde el jugador puede esconderse.
public class SafeZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private bool autoHideOnEnter = false;
    [SerializeField] private float autoHideDelay = 0.5f;
    [SerializeField] private float discoveryRisk = 0.2f;

    [Header("Visual Settings")]
    [SerializeField] private Color zoneColor = new Color(0, 0, 0.5f, 0.3f);
    [SerializeField] private bool showZoneVisual = true;

    [Header("Effects")]
    [SerializeField] private ParticleSystem enterParticles;
    [SerializeField] private ParticleSystem hideParticles;

    private SpriteRenderer zoneRenderer;
    private PlayerController currentPlayer;

    private void Start()
    {
        // Configurar visual
        if (showZoneVisual)
        {
            zoneRenderer = GetComponent<SpriteRenderer>();
            if (zoneRenderer == null)
            {
                zoneRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            zoneRenderer.color = zoneColor;
        }

        // Asegurar que tenga un collider trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        currentPlayer = other.GetComponent<PlayerController>();
        if (currentPlayer == null) return;

        Debug.Log($"Jugador entró a Safe Zone: {gameObject.name}");

        // Efectos
        if (enterParticles != null)
        {
            Instantiate(enterParticles, other.transform.position, Quaternion.identity);
        }

        // Auto-esconder
        if (autoHideOnEnter)
        {
            Invoke(nameof(AutoHidePlayer), autoHideDelay);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"Jugador salió de Safe Zone: {gameObject.name}");

        CancelInvoke(nameof(AutoHidePlayer));
        currentPlayer = null;
    }

    private void AutoHidePlayer()
    {
        if (currentPlayer != null && !currentPlayer.IsDead && !currentPlayer.IsHidden)
        {
            // Simular input de esconderse
            // Nota: El PlayerController manejará esto a través de su propio sistema
            Debug.Log("Auto-escondiendo jugador...");
        }
    }

    
    /// Retorna el riesgo de descubrimiento de esta zona específica.
    /// Usado por enemigos para decidir si revisar.
    public float GetDiscoveryRisk(Vector3 enemyPosition)
    {
        float distance = Vector3.Distance(transform.position, enemyPosition);
        float zoneSize = Mathf.Max(transform.localScale.x, transform.localScale.y);

        if (distance <= zoneSize * 2f)
        {
            // Más cerca = más riesgo
            float proximityFactor = 1f - (distance / (zoneSize * 2f));
            return discoveryRisk * (1f + proximityFactor);
        }

        return 0f;
    }

    /// Verifica si el jugador está en esta zona.
    public bool HasPlayer()
    {
        return currentPlayer != null && currentPlayer.IsInSafeZone;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0, 1, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}