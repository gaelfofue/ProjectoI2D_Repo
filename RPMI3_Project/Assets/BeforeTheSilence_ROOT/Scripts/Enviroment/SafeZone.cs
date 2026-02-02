using UnityEngine;

public class SafeZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private bool autoHideOnEnter = false;
    [SerializeField] private float autoHideDelay = 0.5f;
    [SerializeField] private float discoveryRisk = 0.2f;

    [Header("Visual Settings")]
    [SerializeField] private Color zoneColor = new Color(0, 0, 0.5f, 0.3f);
    [SerializeField] private bool showZoneVisual = false;

    [Header("Interaction Prompt")]
    [Tooltip("Arrastra aquí el InteractionPrompt si quieres uno. Si está vacío, no se mostrará prompt.")]
    [SerializeField] private InteractionPrompt interactionPrompt;

    [Header("Effects")]
    [SerializeField] private ParticleSystem enterParticles;
    [SerializeField] private ParticleSystem hideParticles;

    private SpriteRenderer zoneRenderer;
    private PlayerController currentPlayer;

    private void Start()
    {
        // Visual de la zona
        if (showZoneVisual)
        {
            zoneRenderer = GetComponent<SpriteRenderer>();
            if (zoneRenderer == null)
                zoneRenderer = gameObject.AddComponent<SpriteRenderer>();
            zoneRenderer.color = zoneColor;
        }

        // Asegurar que es trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // Buscar InteractionPrompt si no está asignado
        if (interactionPrompt == null)
        {
            interactionPrompt = GetComponent<InteractionPrompt>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        currentPlayer = other.GetComponent<PlayerController>();
        if (currentPlayer == null) return;

        Debug.Log($"Jugador entró a Safe Zone: {gameObject.name}");

        if (enterParticles != null)
            Instantiate(enterParticles, other.transform.position, Quaternion.identity);

        if (autoHideOnEnter)
            Invoke(nameof(AutoHidePlayer), autoHideDelay);
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
            Debug.Log("Auto-escondiendo jugador...");
    }

    public float GetDiscoveryRisk(Vector3 enemyPosition)
    {
        float distance = Vector3.Distance(transform.position, enemyPosition);
        float zoneSize = Mathf.Max(transform.localScale.x, transform.localScale.y);
        if (distance <= zoneSize * 2f)
        {
            float proximityFactor = 1f - (distance / (zoneSize * 2f));
            return discoveryRisk * (1f + proximityFactor);
        }
        return 0f;
    }

    public bool HasPlayer() => currentPlayer != null && currentPlayer.IsInSafeZone;

    /// <summary>
    /// Obtener referencia al InteractionPrompt (para uso externo)
    /// </summary>
    public InteractionPrompt GetInteractionPrompt() => interactionPrompt;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 0, 1, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}