// SafeZone.cs - Colócalo en tus zonas de escondite
using UnityEngine;

public class SafeZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] bool isDarknessZone = true;
    [SerializeField] Color zoneColor = new Color(0, 0, 0.5f, 0.3f);
    [SerializeField] float discoveryRisk = 0.2f; // 20% de riesgo si revisan

    [Header("Visual Effects")]
    [SerializeField] ParticleSystem hideParticles;

    private SpriteRenderer zoneRenderer;

    private void Start()
    {
        zoneRenderer = GetComponent<SpriteRenderer>();
        if (zoneRenderer != null)
        {
            zoneRenderer.color = zoneColor;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Efecto visual opcional
            if (hideParticles != null)
            {
                Instantiate(hideParticles, other.transform.position, Quaternion.identity);
            }

            if (isDarknessZone)
            {
                Debug.Log("Las sombras te envuelven...");
            }
        }
    }

    // Método para que enemigos revisen ESTA zona específica
    public float CheckZone(Vector3 enemyPosition)
    {
        float distance = Vector3.Distance(transform.position, enemyPosition);
        float zoneSize = Mathf.Max(transform.localScale.x, transform.localScale.y);

        // Si el enemigo está dentro o muy cerca de la zona
        if (distance <= zoneSize * 1.5f)
        {
            return discoveryRisk; // Retorna el riesgo de esta zona
        }

        return 0f; // Sin riesgo
    }
}