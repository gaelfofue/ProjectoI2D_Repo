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

    [Header("Heartbeat Settings")]
    [Tooltip("Distancia a la que el heartbeat empieza a sonar")]
    [SerializeField] private float heartbeatStartDistance = 20f;
    [Tooltip("Distancia a la que el heartbeat está al máximo")]
    [SerializeField] private float heartbeatMaxDistance = 5f;
    [Tooltip("Activar heartbeat automático basado en enemigo")]
    [SerializeField] private bool autoHeartbeat = true;

    [Header("Auto Breathing")]
    [Tooltip("Iniciar respiración normal automáticamente en gameplay")]
    [SerializeField] private bool autoStartBreathing = true;
    [Tooltip("Nombres de escenas de gameplay (donde suena la respiración)")]
    [SerializeField] private string[] gameplaySceneNames = { "Gameplay", "Game", "Level" };
    #endregion

    #region PRIVATE VARIABLES
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource breathingSource;
    private AudioSource ambientSource;
    private AudioSource heartbeatSource;

    private float _masterVolume = 1f;
    private float _musicVolume = 1f;
    private float _sfxVolume = 1f;

    // ===== CAMBIO CLAVE: inicializar en un estado "no iniciado" =====
    private BreathingState currentBreathingState = BreathingState.None;
    private bool breathingStarted = false;

    private Coroutine breathingCoroutine;
    private Coroutine musicCoroutine;
    private Coroutine heartbeatCoroutine;

    // Heartbeat tracking
    private float currentHeartbeatIntensity = 0f;
    private bool heartbeatActive = false;
    private Transform enemyTransform;
    private Transform playerTransform;
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

            if (transform.parent != null)
                transform.SetParent(null);

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

    private void Update()
    {
        if (autoHeartbeat)
        {
            UpdateAutoHeartbeat();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AudioManager] Escena '{scene.name}' cargada");
        UpdateAllVolumes();

        // Reset referencias al cambiar de escena
        enemyTransform = null;
        playerTransform = null;

        // Auto-iniciar respiración en escenas de gameplay
        if (autoStartBreathing && IsGameplayScene(scene.name))
        {
            // Pequeño delay para que todo se inicialice
            StartCoroutine(AutoStartBreathingDelayed());
        }
        else
        {
            // En menús, parar respiración
            StopBreathing();
            StopHeartbeat();
        }
    }

    private IEnumerator AutoStartBreathingDelayed()
    {
        yield return new WaitForSeconds(0.5f);

        if (!breathingStarted)
        {
            Debug.Log("[AudioManager] Auto-iniciando respiración normal");
            ForceStartBreathing(BreathingState.Normal);
        }
    }

    private bool IsGameplayScene(string sceneName)
    {
        string lower = sceneName.ToLower();

        foreach (string name in gameplaySceneNames)
        {
            if (lower.Contains(name.ToLower()))
                return true;
        }

        // Si no se configuraron nombres, asumir que todo lo que NO sea "menu" es gameplay
        if (gameplaySceneNames.Length == 0)
        {
            return !lower.Contains("menu") && !lower.Contains("title") && !lower.Contains("main");
        }

        return false;
    }
    #endregion

    #region INITIALIZATION
    private void InitializeAudioSources()
    {
        musicSource = CreateAudioSource("MusicSource", true);
        sfxSource = CreateAudioSource("SFXSource", false);
        breathingSource = CreateAudioSource("BreathingSource", true);
        ambientSource = CreateAudioSource("AmbientSource", true);
        heartbeatSource = CreateAudioSource("HeartbeatSource", false);

        Debug.Log("[AudioManager] AudioSources creados correctamente");
    }

    private AudioSource CreateAudioSource(string name, bool loop)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        AudioSource source = obj.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
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

        if (IsAudioSourceValid(breathingSource) && breathingSource.isPlaying)
        {
            // Recalcular volumen de breathing según estado actual
            float baseVol = GetBreathingBaseVolume(currentBreathingState);
            breathingSource.volume = baseVol * sfx * master;
        }

        if (IsAudioSourceValid(ambientSource))
            ambientSource.volume = sfx * master * 0.5f;

        if (IsAudioSourceValid(heartbeatSource))
            heartbeatSource.volume = sfx * master;
    }

    private float GetBreathingBaseVolume(BreathingState state)
    {
        return state switch
        {
            BreathingState.Normal => 0.3f,
            BreathingState.Heavy => 0.5f,
            BreathingState.Exhausted => 0.65f,
            BreathingState.Scared => 0.7f,
            BreathingState.Panic => 0.85f,
            _ => 0.3f
        };
    }

    public void SetMasterVolume(float value) => MasterVolume = value;
    public void SetMusicVolume(float value) => MusicVolume = value;
    public void SetSFXVolume(float value) => SFXVolume = value;
    #endregion

    #region MUSIC SYSTEM
    public void PlayMusic(AudioClip clip, float fadeTime = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Clip de música es null");
            return;
        }

        if (IsAudioSourceValid(musicSource) && musicSource.clip == clip && musicSource.isPlaying)
            return;

        if (musicCoroutine != null) StopCoroutine(musicCoroutine);
        musicCoroutine = StartCoroutine(CrossfadeMusic(clip, fadeTime));
    }

    public void PlayMenuMusic() => PlayMusic(menuMusic);
    public void PlayGameplayMusic() => PlayMusic(gameplayMusic);
    public void PlayTensionMusic() => PlayMusic(tensionMusic);
    public void PlayGameOverMusic() => PlayMusic(gameOverMusic, 0.5f);

    public void StopMusic(float fadeTime = 1f)
    {
        if (musicCoroutine != null) StopCoroutine(musicCoroutine);
        musicCoroutine = StartCoroutine(FadeOutMusic(fadeTime));
    }

    public void SetMusicPaused(bool paused)
    {
        if (!IsAudioSourceValid(musicSource)) return;
        if (paused) musicSource.Pause();
        else musicSource.UnPause();
    }

    public bool IsMusicPlaying()
    {
        return IsAudioSourceValid(musicSource) && musicSource.isPlaying;
    }

    public AudioClip GetCurrentMusicClip()
    {
        return IsAudioSourceValid(musicSource) ? musicSource.clip : null;
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, float fadeTime)
    {
        if (!IsAudioSourceValid(musicSource)) yield break;

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

        musicSource.clip = newClip;
        musicSource.volume = 0f;

        if (newClip != null)
        {
            musicSource.Play();
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
        // ===== FIX: Solo ignorar si YA está reproduciendo ese estado =====
        if (currentBreathingState == state && breathingStarted)
        {
            return;
        }

        currentBreathingState = state;

        if (breathingCoroutine != null) StopCoroutine(breathingCoroutine);

        AudioClip targetClip = GetBreathingClip(state);

        if (targetClip == null)
        {
            Debug.LogWarning($"[AudioManager] No hay clip para respiración: {state}");
            return;
        }

        breathingCoroutine = StartCoroutine(TransitionBreathing(targetClip, state));
        Debug.Log($"[AudioManager] Respiración → {state}");
    }

    private AudioClip GetBreathingClip(BreathingState state)
    {
        return state switch
        {
            BreathingState.Normal => normalBreathing,
            BreathingState.Heavy => heavyBreathing,
            BreathingState.Exhausted => exhaustedBreathing,
            BreathingState.Scared => scaredBreathing,
            BreathingState.Panic => panicBreathing,
            _ => normalBreathing
        };
    }

    private IEnumerator TransitionBreathing(AudioClip newClip, BreathingState state)
    {
        if (!IsAudioSourceValid(breathingSource))
        {
            Debug.LogWarning("[AudioManager] BreathingSource no válido");
            yield break;
        }

        float baseVolume = GetBreathingBaseVolume(state);
        float targetVolume = baseVolume * SFXVolume * MasterVolume;

        // Fade out si ya está sonando
        if (breathingSource.isPlaying && breathingSource.volume > 0.01f)
        {
            float fadeOutTime = 0.3f;
            float startVol = breathingSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                if (IsAudioSourceValid(breathingSource))
                    breathingSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeOutTime);
                yield return null;
            }
        }

        if (!IsAudioSourceValid(breathingSource)) yield break;

        // Cambiar clip y reproducir
        breathingSource.clip = newClip;
        breathingSource.loop = true;
        breathingSource.volume = 0f;
        breathingSource.Play();

        // ===== Marcar que ya empezó =====
        breathingStarted = true;

        // Fade in
        float fadeInTime = 0.5f;
        float elapsedIn = 0f;
        while (elapsedIn < fadeInTime)
        {
            elapsedIn += Time.deltaTime;
            if (IsAudioSourceValid(breathingSource))
                breathingSource.volume = Mathf.Lerp(0f, targetVolume, elapsedIn / fadeInTime);
            yield return null;
        }

        if (IsAudioSourceValid(breathingSource))
            breathingSource.volume = targetVolume;

        Debug.Log($"[AudioManager] Breathing activo: {newClip.name} vol: {targetVolume:F2}");
    }

    /// <summary>
    /// Forzar inicio de respiración (ignora el estado actual)
    /// </summary>
    public void ForceStartBreathing(BreathingState state)
    {
        // ===== FIX: Resetear ambos flags =====
        currentBreathingState = BreathingState.None;
        breathingStarted = false;
        SetBreathingState(state);
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
            breathingSource.volume = 0f;
        }

        currentBreathingState = BreathingState.None;
        breathingStarted = false;
    }

    public BreathingState GetCurrentBreathingState() => currentBreathingState;
    #endregion

    #region HEARTBEAT SYSTEM

    /// <summary>
    /// Registrar el enemigo y player para heartbeat automático.
    /// </summary>
    public void RegisterHeartbeatTargets(Transform player, Transform enemy)
    {
        playerTransform = player;
        enemyTransform = enemy;
        Debug.Log("[AudioManager] Heartbeat targets registrados");
    }

    public void RegisterEnemy(Transform enemy)
    {
        enemyTransform = enemy;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        Debug.Log($"[AudioManager] Enemigo registrado. Player: {(playerTransform != null ? "OK" : "NO ENCONTRADO")}");
    }

    private void UpdateAutoHeartbeat()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
            else
                return;
        }

        if (enemyTransform == null)
        {
            GameObject enemyObj = GameObject.FindGameObjectWithTag("Enemy");
            if (enemyObj != null)
                enemyTransform = enemyObj.transform;
            else
                return;
        }

        float distance = Vector3.Distance(playerTransform.position, enemyTransform.position);

        if (distance <= heartbeatStartDistance)
        {
            float intensity = Mathf.InverseLerp(heartbeatStartDistance, heartbeatMaxDistance, distance);
            intensity = Mathf.Clamp01(intensity);

            // ===== Solo actualizar intensidad, no crear nuevas corrutinas =====
            currentHeartbeatIntensity = Mathf.Lerp(currentHeartbeatIntensity, intensity, Time.deltaTime * 3f);

            if (!heartbeatActive)
            {
                StartHeartbeat(intensity);
            }
        }
        else
        {
            if (heartbeatActive)
            {
                StopHeartbeat();
            }
        }
    }

    public void StartHeartbeat(float intensity = 1f)
    {
        if (heartbeatSound == null)
        {
            Debug.LogWarning("[AudioManager] No hay clip de heartbeat asignado");
            return;
        }

        if (!IsAudioSourceValid(heartbeatSource))
        {
            Debug.LogWarning("[AudioManager] HeartbeatSource no válido");
            return;
        }

        // ===== FIX: Si ya está activo, solo actualizar intensidad =====
        if (heartbeatActive)
        {
            currentHeartbeatIntensity = Mathf.Clamp01(intensity);
            return;
        }

        intensity = Mathf.Clamp01(intensity);
        currentHeartbeatIntensity = intensity;
        heartbeatActive = true;

        // ===== FIX: Asegurar que solo hay UNA corrutina =====
        if (heartbeatCoroutine != null) StopCoroutine(heartbeatCoroutine);
        heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
        Debug.Log($"[AudioManager] Heartbeat iniciado - intensidad: {intensity:F2}");
    }

    public void StopHeartbeat()
    {
        heartbeatActive = false;

        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }

        if (IsAudioSourceValid(heartbeatSource))
        {
            heartbeatSource.Stop();
            heartbeatSource.volume = 0f;
        }

        currentHeartbeatIntensity = 0f;
        Debug.Log("[AudioManager] Heartbeat detenido");
    }

    public void SetHeartbeatIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        currentHeartbeatIntensity = intensity;

        if (intensity <= 0f)
        {
            StopHeartbeat();
            return;
        }

        if (!heartbeatActive)
        {
            StartHeartbeat(intensity);
        }
        // ===== Si ya está activo, la corrutina lee currentHeartbeatIntensity automáticamente =====
    }

    private IEnumerator HeartbeatLoop()
    {
        // ===== FIX: Esperar a que termine el sonido anterior antes de reproducir otro =====
        while (heartbeatActive)
        {
            if (heartbeatSound != null && IsAudioSourceValid(heartbeatSource))
            {
                // ===== FIX: Volumen más bajo, escalado mejor =====
                float volume = Mathf.Lerp(0.15f, 0.5f, currentHeartbeatIntensity) * SFXVolume * MasterVolume;

                // ===== FIX: Usar Play() en vez de PlayOneShot() para evitar acumulación =====
                heartbeatSource.clip = heartbeatSound;
                heartbeatSource.volume = volume;
                heartbeatSource.pitch = Mathf.Lerp(0.9f, 1.1f, currentHeartbeatIntensity);
                heartbeatSource.Play();
            }

            // Esperar entre latidos
            float delay = Mathf.Lerp(1.2f, 0.45f, currentHeartbeatIntensity);

            // ===== FIX: Esperar el mayor entre el delay y la duración del clip =====
            float clipLength = heartbeatSound != null ? heartbeatSound.length : 0.5f;
            float waitTime = Mathf.Max(delay, clipLength + 0.05f);

            yield return new WaitForSeconds(waitTime);
        }
    }

    public float GetHeartbeatIntensity() => currentHeartbeatIntensity;
    public bool IsHeartbeatActive() => heartbeatActive;
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
            ambientSource.Stop();
    }

    public void SetAmbientVolume(float volume)
    {
        if (IsAudioSourceValid(ambientSource))
            ambientSource.volume = volume * SFXVolume * MasterVolume;
    }
    #endregion

    #region DEBUG
    /// <summary>
    /// Información de estado para debugging
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Breathing: {currentBreathingState} (started: {breathingStarted})\n" +
               $"Heartbeat: {(heartbeatActive ? $"ON ({currentHeartbeatIntensity:F2})" : "OFF")}\n" +
               $"Music: {(IsMusicPlaying() ? musicSource.clip?.name : "None")}\n" +
               $"Enemy dist: {(enemyTransform != null && playerTransform != null ? Vector3.Distance(playerTransform.position, enemyTransform.position).ToString("F1") : "N/A")}";
    }
    #endregion
}

public enum BreathingState
{
    None = -1,  // ← NUEVO: estado "no iniciado"
    Normal,
    Heavy,
    Exhausted,
    Scared,
    Panic
}