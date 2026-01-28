using UnityEngine;

public class SafeZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] bool isDarknessZone = true;
    [SerializeField] bool autoHide = true;
    [SerializeField] float autoHideDelay = 0.5f;
    [SerializeField] float discoveryRisk = 0.2f;

    [Header("Visual")]
    [SerializeField] Color zoneColor = new Color(0, 0, 0.5f, 0.3f);
    [SerializeField] ParticleSystem hideParticles;

    private SpriteRenderer zoneRenderer;
    private Collider2D zoneCollider;

    private void Start()
    {
        zoneRenderer = GetComponent<SpriteRenderer>();
        zoneCollider = GetComponent<Collider2D>();

        if (zoneRenderer != null)
        {
            zoneRenderer.color = zoneColor;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Efecto visual opcional
                if (hideParticles != null)
                {
                    Instantiate(hideParticles, other.transform.position, Quaternion.identity);
                }

                if (isDarknessZone)
                {
                    Debug.Log("🌑 Las sombras te envuelven...");
                }
            }
        }
    }

    // Método para que enemigos revisen ESTA zona
    public float CheckZone(Vector3 enemyPosition)
    {
        if (zoneCollider == null) return 0f;

        // Calcular distancia al centro de la zona
        float distance = Vector3.Distance(transform.position, enemyPosition);

        // Si el enemigo está cerca de la zona
        if (distance <= zoneCollider.bounds.size.magnitude * 1.5f)
        {
            // Ajustar riesgo según distancia
            float normalizedDistance = Mathf.Clamp01(distance / (zoneCollider.bounds.size.magnitude * 2f));
            float adjustedRisk = discoveryRisk * (1f - normalizedDistance);

            return adjustedRisk;
        }

        return 0f;
    }
}