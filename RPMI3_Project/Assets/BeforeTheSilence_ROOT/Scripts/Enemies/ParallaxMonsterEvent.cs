using UnityEngine;
using System.Collections;

/// <summary>
/// Controla el evento de parallax donde el monstruo pasa rápidamente.
/// </summary>
public class ParallaxMonsterEvent : MonoBehaviour
{
    [Header("Monster Settings")]
    [SerializeField] private GameObject monsterVisual;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float passSpeed = 25f;

    [Header("Parallax Settings")]
    [SerializeField] private float parallaxLayer = -5f; // Z position (fondo)
    [SerializeField] private float scaleMultiplier = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip passbySound;
    [SerializeField] private AudioClip distantRoar;

    [Header("Visual Effects")]
    [SerializeField] private bool flickerLightOnPass = true;
    [SerializeField] private SpotlightZone spotlightZone;

    [Header("Trigger")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private float triggerDelay = 0.5f;

    private bool hasTriggered = false;
    private bool isRunning = false;

    private void Start()
    {
        if (monsterVisual != null)
            monsterVisual.SetActive(false);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Llamar desde SpotlightZone.OnPlayerEnterZone
    /// </summary>
    public void TriggerEvent()
    {
        if (hasTriggered && triggerOnce) return;
        if (isRunning) return;

        StartCoroutine(MonsterPassSequence());
    }

    private IEnumerator MonsterPassSequence()
    {
        isRunning = true;
        hasTriggered = true;

        yield return new WaitForSeconds(triggerDelay);

        // Preparar monstruo
        if (monsterVisual != null)
        {
            monsterVisual.transform.position = new Vector3(
                spawnPoint.position.x,
                spawnPoint.position.y,
                parallaxLayer
            );
            monsterVisual.transform.localScale = Vector3.one * scaleMultiplier;
            monsterVisual.SetActive(true);
        }

        // Iniciar flicker
        if (flickerLightOnPass && spotlightZone != null)
            spotlightZone.StartFlicker();

        // Reproducir sonido
        if (audioSource != null && passbySound != null)
            audioSource.PlayOneShot(passbySound);

        // Mover monstruo
        float duration = Vector2.Distance(spawnPoint.position, endPoint.position) / passSpeed;
        float elapsed = 0f;

        Vector3 startPos = new Vector3(spawnPoint.position.x, spawnPoint.position.y, parallaxLayer);
        Vector3 endPos = new Vector3(endPoint.position.x, endPoint.position.y, parallaxLayer);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (monsterVisual != null)
                monsterVisual.transform.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        // Finalizar
        if (monsterVisual != null)
            monsterVisual.SetActive(false);

        if (flickerLightOnPass && spotlightZone != null)
            spotlightZone.StopFlicker();

        // Rugido distante después
        yield return new WaitForSeconds(1f);

        if (audioSource != null && distantRoar != null)
        {
            audioSource.volume = 0.4f;
            audioSource.PlayOneShot(distantRoar);
        }

        isRunning = false;
    }

    public void ResetEvent()
    {
        hasTriggered = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(spawnPoint.position, 0.5f);
        }

        if (endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(endPoint.position, 0.5f);
        }

        if (spawnPoint != null && endPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnPoint.position, endPoint.position);
        }
    }
}