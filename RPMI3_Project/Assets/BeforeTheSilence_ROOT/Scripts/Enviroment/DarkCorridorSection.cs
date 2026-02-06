using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// Controlador completo para la sección del pasillo oscuro.
/// Maneja: iluminación progresiva, evento del monstruo en parallax, y audio.
/// </summary>
public class DarkCorridorSection : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private Transform player;
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Light2D spotLight; // La luz donde pasa el monstruo
    [SerializeField] private GameObject monsterVisual;
    [SerializeField] private VignetteController vignetteController;

    [Header("CORRIDOR ZONES (Posiciones X)")]
    [SerializeField] private float lightZoneEnd = 10f;      // Donde termina la luz
    [SerializeField] private float darkZoneStart = 15f;     // Donde empieza oscuridad total
    [SerializeField] private float spotlightX = 50f;        // Donde está la luz focal
    [SerializeField] private float darkZoneEnd = 85f;       // Donde termina la oscuridad

    [Header("LIGHTING")]
    [SerializeField] private float litIntensity = 1f;
    [SerializeField] private float darkIntensity = 0.02f;
    [SerializeField] private Color litColor = Color.white;
    [SerializeField] private Color darkColor = new Color(0.05f, 0.05f, 0.1f);

    [Header("MONSTER EVENT")]
    [SerializeField] private Transform monsterSpawnPoint;
    [SerializeField] private Transform monsterEndPoint;
    [SerializeField] private float monsterSpeed = 20f;
    [SerializeField] private float monsterZ = -2f; // Parallax depth

    [Header("AUDIO")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource monsterSource;
    [SerializeField] private AudioClip darkAmbientLoop;
    [SerializeField] private AudioClip monsterPassSound;
    [SerializeField] private AudioClip distantRoar;
    [SerializeField] private float maxMonsterAudioDistance = 15f;

    // Estado
    private bool isInDarkZone = false;
    private bool monsterEventTriggered = false;
    private bool spotlightReached = false;

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (vignetteController == null)
            vignetteController = FindFirstObjectByType<VignetteController>();

        if (monsterVisual != null)
            monsterVisual.SetActive(false);

        // Iniciar ambient
        if (ambientSource != null && darkAmbientLoop != null)
        {
            ambientSource.clip = darkAmbientLoop;
            ambientSource.loop = true;
            ambientSource.volume = 0f;
            ambientSource.Play();
        }
    }

    private void Update()
    {
        if (player == null) return;

        float playerX = player.position.x;

        UpdateLighting(playerX);
        UpdateAmbientAudio(playerX);
        CheckSpotlightTrigger(playerX);
    }

    #region LIGHTING
    private void UpdateLighting(float playerX)
    {
        if (globalLight == null) return;

        float targetIntensity;
        Color targetColor;

        // Antes de la zona oscura - transición gradual
        if (playerX < lightZoneEnd)
        {
            targetIntensity = litIntensity;
            targetColor = litColor;
            isInDarkZone = false;
        }
        // Transición a oscuridad
        else if (playerX < darkZoneStart)
        {
            float t = Mathf.InverseLerp(lightZoneEnd, darkZoneStart, playerX);
            targetIntensity = Mathf.Lerp(litIntensity, darkIntensity, t);
            targetColor = Color.Lerp(litColor, darkColor, t);
            isInDarkZone = false;
        }
        // Zona oscura completa
        else if (playerX < darkZoneEnd)
        {
            targetIntensity = darkIntensity;
            targetColor = darkColor;

            if (!isInDarkZone)
            {
                isInDarkZone = true;
                OnEnterDarkness();
            }
        }
        // Saliendo de la oscuridad
        else
        {
            float t = Mathf.InverseLerp(darkZoneEnd, darkZoneEnd + 10f, playerX);
            targetIntensity = Mathf.Lerp(darkIntensity, litIntensity, t);
            targetColor = Color.Lerp(darkColor, litColor, t);

            if (isInDarkZone)
            {
                isInDarkZone = false;
                OnExitDarkness();
            }
        }

        // Aplicar suavemente
        globalLight.intensity = Mathf.Lerp(globalLight.intensity, targetIntensity, Time.deltaTime * 2f);
        globalLight.color = Color.Lerp(globalLight.color, targetColor, Time.deltaTime * 2f);
    }

    private void OnEnterDarkness()
    {
        Debug.Log("[DarkCorridor] Entrando en oscuridad...");
    }

    private void OnExitDarkness()
    {
        Debug.Log("[DarkCorridor] Saliendo de oscuridad");
    }
    #endregion

    #region SPOTLIGHT & MONSTER EVENT
    private void CheckSpotlightTrigger(float playerX)
    {
        // Detectar cuando llega a la zona del spotlight
        if (!spotlightReached && Mathf.Abs(playerX - spotlightX) < 3f)
        {
            spotlightReached = true;
            StartCoroutine(SpotlightSequence());
        }
    }

    private IEnumerator SpotlightSequence()
    {
        Debug.Log("[DarkCorridor] Llegó al spotlight");

        // Encender spotlight gradualmente
        if (spotLight != null)
        {
            spotLight.intensity = 0f;
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                spotLight.intensity = Mathf.Lerp(0f, 0.8f, elapsed);
                yield return null;
            }
        }

        // Esperar un momento
        yield return new WaitForSeconds(1.5f);

        // Trigger del monstruo
        if (!monsterEventTriggered)
        {
            monsterEventTriggered = true;
            StartCoroutine(MonsterPassEvent());
        }
    }

    private IEnumerator MonsterPassEvent()
    {
        Debug.Log("[DarkCorridor] ¡Monstruo pasando!");

        // Flicker del spotlight
        if (spotLight != null)
            StartCoroutine(FlickerLight(spotLight, 2f));

        // Mostrar monstruo
        if (monsterVisual != null && monsterSpawnPoint != null && monsterEndPoint != null)
        {
            monsterVisual.SetActive(true);
            monsterVisual.transform.position = new Vector3(
                monsterSpawnPoint.position.x,
                monsterSpawnPoint.position.y,
                monsterZ
            );

            // Sonido de paso
            if (monsterSource != null && monsterPassSound != null)
                monsterSource.PlayOneShot(monsterPassSound);

            // Mover rápidamente
            Vector3 startPos = monsterVisual.transform.position;
            Vector3 endPos = new Vector3(monsterEndPoint.position.x, monsterEndPoint.position.y, monsterZ);
            float duration = Vector2.Distance(startPos, endPos) / monsterSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                monsterVisual.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                yield return null;
            }

            monsterVisual.SetActive(false);
        }

        // Efecto de pánico en post-procesado
        if (vignetteController != null)
            vignetteController.PanicFlash();

        // Rugido distante
        yield return new WaitForSeconds(0.5f);
        if (monsterSource != null && distantRoar != null)
        {
            monsterSource.volume = 0.5f;
            monsterSource.PlayOneShot(distantRoar);
        }
    }

    private IEnumerator FlickerLight(Light2D light, float duration)
    {
        float originalIntensity = light.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float noise = Mathf.PerlinNoise(elapsed * 20f, 0f);
            light.intensity = Mathf.Lerp(0.1f, originalIntensity, noise);
            yield return null;
        }

        light.intensity = originalIntensity;
    }
    #endregion

    #region AUDIO
    private void UpdateAmbientAudio(float playerX)
    {
        if (ambientSource == null) return;

        // Subir volumen del ambient oscuro conforme entra en la zona
        float targetVolume = 0f;

        if (playerX > lightZoneEnd && playerX < darkZoneEnd)
        {
            float t = Mathf.InverseLerp(lightZoneEnd, darkZoneStart, playerX);
            targetVolume = Mathf.Clamp01(t) * 0.5f;
        }

        ambientSource.volume = Mathf.Lerp(ambientSource.volume, targetVolume, Time.deltaTime * 2f);
    }
    #endregion

    #region DEBUG
    private void OnDrawGizmosSelected()
    {
        float y = 0f;
        float height = 5f;

        // Zona iluminada
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(0, y - height, 0), new Vector3(0, y + height, 0));
        Gizmos.DrawLine(new Vector3(lightZoneEnd, y - height, 0), new Vector3(lightZoneEnd, y + height, 0));

        // Transición
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(new Vector3(darkZoneStart, y - height, 0), new Vector3(darkZoneStart, y + height, 0));

        // Zona oscura
        Gizmos.color = Color.black;
        Gizmos.DrawCube(
            new Vector3((darkZoneStart + darkZoneEnd) / 2, y, 0),
            new Vector3(darkZoneEnd - darkZoneStart, height * 2, 1)
        );

        // Spotlight
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(new Vector3(spotlightX, y, 0), 2f);

        // Fin zona oscura
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(darkZoneEnd, y - height, 0), new Vector3(darkZoneEnd, y + height, 0));

        // Ruta del monstruo
        if (monsterSpawnPoint != null && monsterEndPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(monsterSpawnPoint.position, 0.5f);
            Gizmos.DrawSphere(monsterEndPoint.position, 0.5f);
            Gizmos.DrawLine(monsterSpawnPoint.position, monsterEndPoint.position);
        }
    }
    #endregion
}