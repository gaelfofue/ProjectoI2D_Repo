using UnityEngine;

/// <summary>
/// Controlador de música por escena. Añadir a cada escena para definir su música.
/// </summary>
public class SceneMusicController : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private AudioClip sceneMusic;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float fadeInTime = 1f;
    [SerializeField] private float startDelay = 0f;

    [Header("Volume Settings")]
    [SerializeField][Range(0f, 1f)] private float musicVolumeMultiplier = 1f;

    [Header("Ambient Sound (Optional)")]
    [SerializeField] private AudioClip ambientSound;
    [SerializeField][Range(0f, 1f)] private float ambientVolume = 0.3f;
    [SerializeField] private bool loopAmbient = true;

    [Header("Tension Music (Optional)")]
    [SerializeField] private AudioClip tensionMusic;
    [SerializeField] private AudioClip chaseMusic;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private AudioSource ambientSource;
    private bool isPlayingTension = false;
    private bool isPlayingChase = false;
    private AudioClip originalMusic;

    private void Start()
    {
        if (playOnStart && sceneMusic != null)
        {
            if (startDelay > 0)
            {
                Invoke(nameof(PlaySceneMusic), startDelay);
            }
            else
            {
                PlaySceneMusic();
            }
        }

        // Iniciar ambient si existe
        if (ambientSound != null)
        {
            SetupAmbientSound();
        }

        originalMusic = sceneMusic;
    }

    private void PlaySceneMusic()
    {
        if (AudioManager.Instance != null && sceneMusic != null)
        {
            AudioManager.Instance.PlayMusic(sceneMusic, fadeInTime);

            if (debugMode)
            {
                Debug.Log($"[SceneMusic] Reproduciendo: {sceneMusic.name}");
            }
        }
        else if (debugMode)
        {
            if (AudioManager.Instance == null)
                Debug.LogWarning("[SceneMusic] AudioManager.Instance es NULL");
            if (sceneMusic == null)
                Debug.LogWarning("[SceneMusic] No hay música asignada para esta escena");
        }
    }

    private void SetupAmbientSound()
    {
        if (ambientSource == null)
        {
            GameObject ambientObj = new GameObject("SceneAmbient");
            ambientObj.transform.SetParent(transform);
            ambientSource = ambientObj.AddComponent<AudioSource>();
        }

        ambientSource.clip = ambientSound;
        ambientSource.loop = loopAmbient;
        ambientSource.volume = ambientVolume;
        ambientSource.playOnAwake = false;
        ambientSource.Play();

        if (debugMode)
        {
            Debug.Log($"[SceneMusic] Ambient iniciado: {ambientSound.name}");
        }
    }

    #region PUBLIC METHODS
    /// <summary>
    /// Cambiar a música de tensión
    /// </summary>
    public void PlayTensionMusic()
    {
        if (tensionMusic == null || isPlayingTension) return;

        isPlayingTension = true;
        isPlayingChase = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(tensionMusic, 0.5f);
        }

        if (debugMode)
        {
            Debug.Log("[SceneMusic] Cambiando a música de tensión");
        }
    }

    /// <summary>
    /// Cambiar a música de persecución
    /// </summary>
    public void PlayChaseMusic()
    {
        if (chaseMusic == null || isPlayingChase) return;

        isPlayingChase = true;
        isPlayingTension = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(chaseMusic, 0.3f);
        }

        if (debugMode)
        {
            Debug.Log("[SceneMusic] Cambiando a música de persecución");
        }
    }

    /// <summary>
    /// Volver a la música normal de la escena
    /// </summary>
    public void PlayNormalMusic()
    {
        if (!isPlayingTension && !isPlayingChase) return;

        isPlayingTension = false;
        isPlayingChase = false;

        if (AudioManager.Instance != null && originalMusic != null)
        {
            AudioManager.Instance.PlayMusic(originalMusic, 1f);
        }

        if (debugMode)
        {
            Debug.Log("[SceneMusic] Volviendo a música normal");
        }
    }

    /// <summary>
    /// Reproducir una música específica temporalmente
    /// </summary>
    public void PlayCustomMusic(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(clip, fadeTime);
        }
    }

    /// <summary>
    /// Detener toda la música
    /// </summary>
    public void StopMusic(float fadeTime = 1f)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic(fadeTime);
        }
    }

    /// <summary>
    /// Ajustar volumen del ambient
    /// </summary>
    public void SetAmbientVolume(float volume)
    {
        if (ambientSource != null)
        {
            ambientSource.volume = Mathf.Clamp01(volume);
        }
    }

    /// <summary>
    /// Pausar/Reanudar ambient
    /// </summary>
    public void SetAmbientPaused(bool paused)
    {
        if (ambientSource != null)
        {
            if (paused)
                ambientSource.Pause();
            else
                ambientSource.UnPause();
        }
    }
    #endregion

    #region STATIC HELPER
    /// <summary>
    /// Obtener el controlador de música de la escena actual
    /// </summary>
    public static SceneMusicController GetCurrent()
    {
        return FindFirstObjectByType<SceneMusicController>();
    }
    #endregion

    private void OnDestroy()
    {
        // Limpiar ambient
        if (ambientSource != null)
        {
            Destroy(ambientSource.gameObject);
        }
    }
}