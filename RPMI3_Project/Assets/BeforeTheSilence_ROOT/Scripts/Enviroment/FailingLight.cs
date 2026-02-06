using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// Luz que parpadea y falla cuando el jugador se acerca.
/// Añadir al GameObject de la Light2D.
/// </summary>
public class FailingLight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light2D light2D;
    [SerializeField] private Transform player;

    [Header("Trigger Settings")]
    [SerializeField] private float triggerDistance = 5f;
    [SerializeField] private bool triggerOnce = true;

    [Header("Flicker Settings")]
    [SerializeField] private float flickerDuration = 3f;
    [SerializeField] private float flickerSpeed = 15f;
    [SerializeField] private int hardFlickerCount = 4;

    [Header("Final State")]
    [SerializeField] private float finalIntensity = 0.1f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip flickerSound;
    [SerializeField] private AudioClip shutdownSound;

    private float originalIntensity;
    private Color originalColor;
    private bool hasTriggered = false;
    private bool isFlickering = false;

    private void Start()
    {
        if (light2D == null)
            light2D = GetComponent<Light2D>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (light2D != null)
        {
            originalIntensity = light2D.intensity;
            originalColor = light2D.color;
        }
    }

    private void Update()
    {
        if (hasTriggered && triggerOnce) return;
        if (isFlickering) return;
        if (player == null || light2D == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= triggerDistance)
        {
            StartCoroutine(FailSequence());
        }
    }

    private IEnumerator FailSequence()
    {
        hasTriggered = true;
        isFlickering = true;

        Debug.Log("[FailingLight] Iniciando secuencia de fallo...");

        // Sonido de flicker
        if (audioSource != null && flickerSound != null)
        {
            audioSource.clip = flickerSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // === FASE 1: Flicker suave (parpadeo nervioso) ===
        float elapsed = 0f;
        while (elapsed < flickerDuration * 0.5f)
        {
            elapsed += Time.deltaTime;

            // Ruido Perlin para flicker orgánico
            float noise = Mathf.PerlinNoise(elapsed * flickerSpeed, 0f);
            light2D.intensity = Mathf.Lerp(originalIntensity * 0.3f, originalIntensity, noise);

            yield return null;
        }

        // === FASE 2: Parpadeos fuertes (on/off) ===
        for (int i = 0; i < hardFlickerCount; i++)
        {
            // Apagar
            light2D.intensity = 0f;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));

            // Encender fuerte
            light2D.intensity = originalIntensity * 1.2f;
            yield return new WaitForSeconds(Random.Range(0.03f, 0.1f));
        }

        // === FASE 3: Últimos intentos desesperados ===
        for (int i = 0; i < 3; i++)
        {
            light2D.intensity = originalIntensity * 0.5f;
            yield return new WaitForSeconds(0.1f);

            light2D.intensity = 0f;
            yield return new WaitForSeconds(Random.Range(0.2f, 0.4f));
        }

        // Un último intento
        light2D.intensity = originalIntensity * 0.7f;
        yield return new WaitForSeconds(0.3f);

        // Parar sonido de flicker
        if (audioSource != null)
            audioSource.Stop();

        // Sonido de apagado
        if (audioSource != null && shutdownSound != null)
            audioSource.PlayOneShot(shutdownSound);

        // === FASE 4: Fade out final ===
        elapsed = 0f;
        float startIntensity = light2D.intensity;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            light2D.intensity = Mathf.Lerp(startIntensity, finalIntensity, t);

            // Cambiar color a más cálido/moribundo
            light2D.color = Color.Lerp(originalColor, new Color(1f, 0.6f, 0.3f), t);

            yield return null;
        }

        light2D.intensity = finalIntensity;
        isFlickering = false;

        Debug.Log("[FailingLight] Luz casi apagada");
    }

    /// <summary>
    /// Llamar manualmente para activar el fallo
    /// </summary>
    public void TriggerFail()
    {
        if (!isFlickering && (!hasTriggered || !triggerOnce))
        {
            StartCoroutine(FailSequence());
        }
    }

    /// <summary>
    /// Restaurar la luz a su estado original
    /// </summary>
    public void Restore()
    {
        StopAllCoroutines();
        isFlickering = false;
        hasTriggered = false;

        if (light2D != null)
        {
            light2D.intensity = originalIntensity;
            light2D.color = originalColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}