using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    #region SERIALIZED FIELDS
    [Header("Music Clips (Para uso directo - Opcional)")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip tensionMusic;
    [SerializeField] private AudioClip gameOverMusic;

    [Header("Breathing Clips")]
    [SerializeField] private AudioClip normalBreathing;
    [SerializeField] private AudioClip heavyBreathing;
    [SerializeField] private AudioClip exhaustedBreathing;
    [SerializeField] private AudioClip scaredBreathing;
    [SerializeField] private AudioClip panicBreathing;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip hideSound;
    [SerializeField] private AudioClip unhideSound;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private AudioClip heartbeatSound;
    [SerializeField] private AudioClip discoveredSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Settings")]
    [SerializeField] private float musicFadeSpeed = 1f;
    [SerializeField] private float breathingFadeSpeed = 2f;
    #endregion

    #region PRIVATE VARIABLES
    // Audio Sources
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource breathingSource;
    private AudioSource ambientSource;

    // Volume settings (local backup)
    private float _masterVolume = 1f;
    private float _musicVolume = 1f;
    private float _sfxVolume = 1f;

    // State
    private BreathingState currentBreathingState = BreathingState.Normal;
    private Coroutine breathingCoroutine;
    private Coroutine musicCoroutine;
    private Coroutine heartbeatCoroutine;
    #endregion

    #region PROPERTIES
    public float MasterVolume
    {
        get => GameData.instance != null ? GameData.instance.masterVolume : _masterVolume;
        set
        {
            _masterVolume = Mathf.Clamp01(value);
            if (GameData.instance != null)
                GameData.instance.masterVolume = _masterVolume;
            UpdateAllVolumes();
        }
    }

    public float MusicVolume
    {
        get => GameData.instance != null ? GameData.instance.musicVolume : _musicVolume;
        set
        {
            _musicVolume = Mathf.Clamp01(value);
            if (GameData.instance != null)
                GameData.instance.musicVolume = _musicVolume;
            UpdateAllVolumes();
        }
    }

    public float SFXVolume
    {
        get => GameData.instance != null ? GameData.instance.sfxVolume : _sfxVolume;
        set
        {
            _sfxVolume = Mathf.Clamp01(value);
            if (GameData.instance != null)
                GameData.instance.sfxVolume = _sfxVolume;
            UpdateAllVolumes();
        }
    }
    #endregion

    #region UNITY METHODS
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Asegurar que es objeto raíz
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            LoadVolumeSettings();
            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log("[AudioManager] Inicializado correctamente");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AudioManager] Escena '{scene.name}' cargada");
        UpdateAllVolumes();
    }
    #endregion

    #region INITIALIZATION
    private void InitializeAudioSources()
    {
        // Limpiar referencias potencialmente rotas
        musicSource = null;
        sfxSource = null;
        breathingSource = null;
        ambientSource = null;

        // Crear Music Source
        GameObject musicObj = new GameObject("MusicSource");
        musicObj.transform.SetParent(transform);
        musicSource = musicObj.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        // Crear SFX Source
        GameObject sfxObj = new GameObject("SFXSource");
        sfxObj.transform.SetParent(transform);
        sfxSource = sfxObj.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        // Crear Breathing Source
        GameObject breathObj = new GameObject("BreathingSource");
        breathObj.transform.SetParent(transform);
        breathingSource = breathObj.AddComponent<AudioSource>();
        breathingSource.loop = true;
        breathingSource.playOnAwake = false;

        // Crear Ambient Source
        GameObject ambientObj = new GameObject("AmbientSource");
        ambientObj.transform.SetParent(transform);
        ambientSource = ambientObj.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.playOnAwake = false;

        Debug.Log("[AudioManager] AudioSources creados correctamente");
    }

    private void LoadVolumeSettings()
    {
        if (GameData.instance != null)
        {
            _masterVolume = GameData.instance.masterVolume;
            _musicVolume = GameData.instance.musicVolume;
            _sfxVolume = GameData.instance.sfxVolume;
        }
        else
        {
            _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            _musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            _sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }

        UpdateAllVolumes();
    }

    private bool IsAudioSourceValid(AudioSource source)
    {
        return source != null && source.gameObject != null;
    }
    #endregion

    #region VOLUME CONTROL
    public void UpdateAllVolumes()
    {
        float master = MasterVolume;
        float music = MusicVolume;
        float sfx = SFXVolume;

        if (IsAudioSourceValid(musicSource))
            musicSource.volume = music * master;

        if (IsAudioSourceValid(sfxSource))
            sfxSource.volume = sfx * master;

        if (IsAudioSourceValid(breathingSource))
            breathingSource.volume = sfx * master * 0.7f;

        if (IsAudioSourceValid(ambientSource))
            ambientSource.volume = sfx * master * 0.5f;
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = value;
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = value;
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = value;
    }
    #endregion

    #region MUSIC SYSTEM
    /// <summary>
    /// Reproducir música con clip específico (MÉTODO PRINCIPAL)
    /// </summary>
    public void PlayMusic(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Clip de música es null");
            return;
        }

        // Si es el mismo clip y está reproduciéndose, no hacer nada
        if (IsAudioSourceValid(musicSource) && musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        if (musicCoroutine != null) StopCoroutine(musicCoroutine);
        musicCoroutine = StartCoroutine(CrossfadeMusic(clip, fadeTime));
    }

    /// <summary>
    /// Métodos de conveniencia para música predefinida
    /// </summary>
    public void PlayMenuMusic() => PlayMusic(menuMusic);
    public void PlayGameplayMusic() => PlayMusic(gameplayMusic);
    public void PlayTensionMusic() => PlayMusic(tensionMusic);
    public void PlayGameOverMusic() => PlayMusic(gameOverMusic, 0.5f);

    /// <summary>
    /// Detener música con fade
    /// </summary>
    public void StopMusic(float fadeTime = 1f)
    {
        if (musicCoroutine != null) StopCoroutine(musicCoroutine);
        musicCoroutine = StartCoroutine(FadeOutMusic(fadeTime));
    }

    /// <summary>
    /// Pausar/Reanudar música
    /// </summary>
    public void SetMusicPaused(bool paused)
    {
        if (!IsAudioSourceValid(musicSource)) return;

        if (paused)
            musicSource.Pause();
        else
            musicSource.UnPause();
    }

    /// <summary>
    /// Verificar si hay música reproduciéndose
    /// </summary>
    public bool IsMusicPlaying()
    {
        return IsAudioSourceValid(musicSource) && musicSource.isPlaying;
    }

    /// <summary>
    /// Obtener el clip de música actual
    /// </summary>
    public AudioClip GetCurrentMusicClip()
    {
        return IsAudioSourceValid(musicSource) ? musicSource.clip : null;
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, float fadeTime)
    {
        if (!IsAudioSourceValid(musicSource)) yield break;

        // Fade out si hay música reproduciéndose
        if (musicSource.isPlaying && musicSource.volume > 0)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                if (IsAudioSourceValid(musicSource))
                    musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
                yield return null;
            }
        }

        if (!IsAudioSourceValid(musicSource)) yield break;

        // Cambiar clip
        musicSource.clip = newClip;
        musicSource.volume = 0f;

        if (newClip != null)
        {
            musicSource.Play();

            // Fade in
            float targetVolume = MusicVolume * MasterVolume;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                if (IsAudioSourceValid(musicSource))
                    musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeTime);
                yield return null;
            }

            if (IsAudioSourceValid(musicSource))
                musicSource.volume = targetVolume;
        }

        Debug.Log($"[AudioManager] Música cambiada a: {newClip?.name ?? "None"}");
    }

    private IEnumerator FadeOutMusic(float fadeTime)
    {
        if (!IsAudioSourceValid(musicSource)) yield break;

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            if (IsAudioSourceValid(musicSource))
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
            yield return null;
        }

        if (IsAudioSourceValid(musicSource))
        {
            musicSource.Stop();
            musicSource.volume = 0f;
        }
    }
    #endregion

    #region SFX SYSTEM
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && IsAudioSourceValid(sfxSource))
        {
            float volume = SFXVolume * MasterVolume * volumeScale;
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    public void PlayHideSound() => PlaySFX(hideSound);
    public void PlayUnhideSound() => PlaySFX(unhideSound);
    public void PlayFootstep() => PlaySFX(footstepSound, 0.5f);
    public void PlayDiscoveredSound() => PlaySFX(discoveredSound);
    public void PlayDeathSound() => PlaySFX(deathSound);
    #endregion

    #region BREATHING SYSTEM
    public void SetBreathingState(BreathingState state)
    {
        if (currentBreathingState == state) return;
        currentBreathingState = state;

        if (breathingCoroutine != null) StopCoroutine(breathingCoroutine);

        AudioClip targetClip = state switch
        {
            BreathingState.Normal => normalBreathing,
            BreathingState.Heavy => heavyBreathing,
            BreathingState.Exhausted => exhaustedBreathing,
            BreathingState.Scared => scaredBreathing,
            BreathingState.Panic => panicBreathing,
            _ => normalBreathing
        };

        breathingCoroutine = StartCoroutine(TransitionBreathing(targetClip));
        Debug.Log($"[AudioManager] Respiración: {state}");
    }

    private IEnumerator TransitionBreathing(AudioClip newClip)
    {
        if (!IsAudioSourceValid(breathingSource))
        {
            Debug.LogWarning("[AudioManager] BreathingSource no válido");
            yield break;
        }

        // Fade out
        while (IsAudioSourceValid(breathingSource) && breathingSource.volume > 0)
        {
            breathingSource.volume -= Time.deltaTime * breathingFadeSpeed;
            yield return null;
        }

        if (!IsAudioSourceValid(breathingSource)) yield break;

        breathingSource.clip = newClip;

        if (newClip != null)
        {
            breathingSource.loop = true;
            breathingSource.Play();

            float targetVolume = SFXVolume * MasterVolume * 0.7f;

            while (IsAudioSourceValid(breathingSource) && breathingSource.volume < targetVolume)
            {
                breathingSource.volume += Time.deltaTime * breathingFadeSpeed;
                yield return null;
            }

            if (IsAudioSourceValid(breathingSource))
                breathingSource.volume = targetVolume;
        }
    }

    public void StopBreathing()
    {
        if (breathingCoroutine != null)
        {
            StopCoroutine(breathingCoroutine);
            breathingCoroutine = null;
        }

        if (IsAudioSourceValid(breathingSource))
        {
            breathingSource.Stop();
        }

        currentBreathingState = BreathingState.Normal;
    }

    public BreathingState GetCurrentBreathingState() => currentBreathingState;
    #endregion

    #region HEARTBEAT SYSTEM
    public void StartHeartbeat(float intensity = 1f)
    {
        if (heartbeatCoroutine != null) StopCoroutine(heartbeatCoroutine);
        heartbeatCoroutine = StartCoroutine(HeartbeatLoop(intensity));
    }

    public void StopHeartbeat()
    {
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }
    }

    private IEnumerator HeartbeatLoop(float intensity)
    {
        while (true)
        {
            if (heartbeatSound != null)
                PlaySFX(heartbeatSound, intensity);
            float delay = Mathf.Lerp(1.2f, 0.4f, intensity);
            yield return new WaitForSeconds(delay);
        }
    }
    #endregion

    #region AMBIENT SYSTEM
    public void PlayAmbient(AudioClip clip, float volume = 0.5f)
    {
        if (!IsAudioSourceValid(ambientSource)) return;

        ambientSource.clip = clip;
        ambientSource.volume = volume * SFXVolume * MasterVolume;
        ambientSource.loop = true;

        if (clip != null)
            ambientSource.Play();
    }

    public void StopAmbient()
    {
        if (IsAudioSourceValid(ambientSource))
        {
            ambientSource.Stop();
        }
    }

    public void SetAmbientVolume(float volume)
    {
        if (IsAudioSourceValid(ambientSource))
        {
            ambientSource.volume = volume * SFXVolume * MasterVolume;
        }
    }
    #endregion
}

public enum BreathingState
{
    Normal,
    Heavy,
    Exhausted,
    Scared,
    Panic
}