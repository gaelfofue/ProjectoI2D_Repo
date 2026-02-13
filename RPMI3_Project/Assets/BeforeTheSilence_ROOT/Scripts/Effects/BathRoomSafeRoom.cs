using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Escena del baño - zona segura.
/// Efecto de alivio: shake se calma, vignette se relaja, sonidos de calma.
/// El player puede salir cuando quiera.
/// </summary>
public class BathroomSafeRoom : MonoBehaviour
{
    [Header("=== RELIEF EFFECT ===")]
    [SerializeField] private float reliefDuration = 3f;

    [Header("=== SHAKE RELIEF ===")]
    [Tooltip("Shake inicial (como si aún estuviera ansioso)")]
    [SerializeField] private float startShakeIntensity = 0.3f;
    [Tooltip("Shake final (calmado)")]
    [SerializeField] private float endShakeIntensity = 0f;

    [Header("=== VIGNETTE ===")]
    [SerializeField] private VignetteController vignetteController;
    [SerializeField] private float startVignetteIntensity = 0.6f;
    [SerializeField] private float endVignetteIntensity = 0.2f;
    [SerializeField] private Color anxiousColor = new Color(0.3f, 0.02f, 0.08f); // Rojo ansioso
    [SerializeField] private Color calmColor = new Color(0.1f, 0.1f, 0.2f); // Azul calmado

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorCloseSound;
    [SerializeField] private AudioClip relievedBreathSound;
    [SerializeField] private AudioClip heartbeatSlowingSound;
    [SerializeField] private AudioClip safeAmbientLoop;
    [SerializeField] private float ambientVolume = 0.3f;

    [Header("=== EXIT DOOR ===")]
    [SerializeField] private string nextSceneName = "SCN_Casa_Piso2_Limbo1";
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private Transform exitDoorPosition;
    [SerializeField] private float exitTriggerDistance = 1.5f;

    [Header("=== UI ===")]
    [SerializeField] private GameObject exitPromptUI; // "Presiona E para salir"

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebug = true;

    // Estado
    private bool reliefComplete = false;
    private bool canExit = false;
    private bool isExiting = false;
    private Transform player;
    private string debugStatus = "Entrando...";

    private void Start()
    {
        // Buscar referencias
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            player = playerGO.transform;

        if (vignetteController == null)
            vignetteController = FindFirstObjectByType<VignetteController>();

        // Fade transparente
        if (fadeImage != null)
            fadeImage.color = new Color(0, 0, 0, 0);

        // UI oculta
        if (exitPromptUI != null)
            exitPromptUI.SetActive(false);

        // Iniciar secuencia de alivio
        StartCoroutine(ReliefSequence());

        Debug.Log("[Bathroom] ✅ Escena iniciada");
    }

    private void Update()
    {
        if (!reliefComplete || isExiting) return;

        // Verificar si el player está cerca de la salida
        if (exitDoorPosition != null && player != null)
        {
            float distance = Vector2.Distance(player.position, exitDoorPosition.position);
            bool nearExit = distance <= exitTriggerDistance;

            if (nearExit && !canExit)
            {
                canExit = true;
                if (exitPromptUI != null) exitPromptUI.SetActive(true);
                debugStatus = "Presiona E para salir";
            }
            else if (!nearExit && canExit)
            {
                canExit = false;
                if (exitPromptUI != null) exitPromptUI.SetActive(false);
                debugStatus = "Explora o ve a la puerta";
            }
        }

        // Detectar tecla E
        if (canExit)
        {
            bool ePressed = false;

            if (Keyboard.current != null)
                ePressed = Keyboard.current.eKey.wasPressedThisFrame;

            if (!ePressed)
                ePressed = Input.GetKeyDown(KeyCode.E);

            if (ePressed)
            {
                StartCoroutine(ExitBathroom());
            }
        }
    }

    #region RELIEF SEQUENCE
    private IEnumerator ReliefSequence()
    {
        Debug.Log("[Bathroom] === SECUENCIA DE ALIVIO ===");
        debugStatus = "Recuperándose...";

        // Sonido de puerta cerrándose
        if (audioSource != null && doorCloseSound != null)
        {
            audioSource.PlayOneShot(doorCloseSound);
        }

        yield return new WaitForSeconds(0.5f);

        // Iniciar con shake ansioso
        if (CinemachineShake.Instance != null)
        {
            CinemachineShake.Instance.StartConstantShake(startShakeIntensity);
        }

        // Sonido de respiración aliviada
        if (audioSource != null && relievedBreathSound != null)
        {
            audioSource.PlayOneShot(relievedBreathSound);
        }

        // Transición gradual de alivio
        float elapsed = 0f;

        while (elapsed < reliefDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / reliefDuration;

            // Usar curva suave (ease out)
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

            // Reducir shake gradualmente
            if (CinemachineShake.Instance != null)
            {
                float currentShake = Mathf.Lerp(startShakeIntensity, endShakeIntensity, smoothProgress);
                CinemachineShake.Instance.StartConstantShake(currentShake);
            }

            // Reducir vignette gradualmente
            if (vignetteController != null)
            {
                float currentIntensity = Mathf.Lerp(startVignetteIntensity, endVignetteIntensity, smoothProgress);
                vignetteController.SetVignetteIntensity(currentIntensity);

                Color currentColor = Color.Lerp(anxiousColor, calmColor, smoothProgress);
                vignetteController.SetVignetteColor(currentColor);
            }

            yield return null;
        }

        // Asegurar estado final
        if (CinemachineShake.Instance != null)
            CinemachineShake.Instance.StopShake();

        // Sonido de latido calmándose
        if (audioSource != null && heartbeatSlowingSound != null)
        {
            audioSource.PlayOneShot(heartbeatSlowingSound);
        }

        // Iniciar ambient loop
        if (audioSource != null && safeAmbientLoop != null)
        {
            yield return new WaitForSeconds(1f);
            audioSource.clip = safeAmbientLoop;
            audioSource.loop = true;
            audioSource.volume = 0f;
            audioSource.Play();

            // Fade in del ambient
            float ambientElapsed = 0f;
            while (ambientElapsed < 2f)
            {
                ambientElapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(0f, ambientVolume, ambientElapsed / 2f);
                yield return null;
            }
        }

        reliefComplete = true;
        debugStatus = "Seguro - ve a la puerta";
        Debug.Log("[Bathroom] ✅ Alivio completado - player puede salir");
    }
    #endregion

    #region EXIT
    private IEnumerator ExitBathroom()
    {
        isExiting = true;
        debugStatus = "Saliendo...";
        Debug.Log("[Bathroom] 🚪 Saliendo del baño");

        // Ocultar prompt
        if (exitPromptUI != null)
            exitPromptUI.SetActive(false);

        // Fade out del ambient
        if (audioSource != null && audioSource.isPlaying)
        {
            float startVolume = audioSource.volume;
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed);
                yield return null;
            }
            audioSource.Stop();
        }

        // Fade a negro
        if (fadeImage != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeImage.color = new Color(0, 0, 0, elapsed / fadeDuration);
                yield return null;
            }
            fadeImage.color = Color.black;
        }

        yield return new WaitForSeconds(0.5f);

        // Cargar siguiente escena
        Debug.Log($"[Bathroom] 🎬 Cargando: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }
    #endregion
}