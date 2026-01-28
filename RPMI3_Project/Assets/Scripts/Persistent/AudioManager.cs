using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
/// Maneja todo el audio del juego de forma persistente.
/// Incluye música, efectos y sistema de respiración.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource breathingSource;
    [SerializeField] private AudioSource ambientSource;

    [Header("Music Clips")]
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
    [SerializeField] private float crossfadeDuration = 1f;

    // Estado actual
    private BreathingState currentBreathingState = BreathingState.Normal;
    private Coroutine breathingCoroutine;
    private Coroutine musicCoroutine;
    private Coroutine heartbeatCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            SceneManager.sceneLoaded += OnSceneLoaded;
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

    private void InitializeAudioSources()
    {
        // Crear AudioSources si no existen
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (breathingSource == null)
        {
            breathingSource = gameObject.AddComponent<AudioSource>();
            breathingSource.loop = true;
            breathingSource.playOnAwake = false;
        }

        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Actualizar volúmenes según configuración
        UpdateVolumes();
    }

    private void UpdateVolumes()
    {
        if (GameData.instance != null)
        {
            float master = GameData.instance.masterVolume;
            musicSource.volume = GameData.instance.musicVolume * master;
            sfxSource.volume = GameData.instance.sfxVolume * master;
            breathingSource.volume = GameData.instance.sfxVolume * master * 0.7f;
            ambientSource.volume = GameData.instance.sfxVolume * master * 0.5f;
        }
    }

    #region MUSIC SYSTEM
    public void PlayMusic(AudioClip clip, float fadeTime = 1f)
    {
        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
        }
        musicCoroutine = StartCoroutine(CrossfadeMusic(clip, fadeTime));
    }

    public void PlayGameplayMusic() => PlayMusic(gameplayMusic);
    public void PlayTensionMusic() => PlayMusic(tensionMusic);
    public void PlayMenuMusic() => PlayMusic(menuMusic);
    public void PlayGameOverMusic() => PlayMusic(gameOverMusic, 0.5f);

    private IEnumerator CrossfadeMusic(AudioClip newClip, float fadeTime)
    {
        float startVolume = musicSource.volume;

        // Fade out
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.unscaledDeltaTime / fadeTime;
            yield return null;
        }

        // Cambiar clip
        musicSource.clip = newClip;
        if (newClip != null)
        {
            musicSource.Play();
        }

        // Fade in
        float targetVolume = GameData.instance != null
            ? GameData.instance.musicVolume * GameData.instance.masterVolume
            : 1f;

        while (musicSource.volume < targetVolume)
        {
            musicSource.volume += targetVolume * Time.unscaledDeltaTime / fadeTime;
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void StopMusic(float fadeTime = 1f)
    {
        if (musicCoroutine != null)
        {
            StopCoroutine(musicCoroutine);
        }
        musicCoroutine = StartCoroutine(FadeOutMusic(fadeTime));
    }

    private IEnumerator FadeOutMusic(float fadeTime)
    {
        float startVolume = musicSource.volume;

        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.unscaledDeltaTime / fadeTime;
            yield return null;
        }

        musicSource.Stop();
    }
    #endregion

    #region SFX SYSTEM
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
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

        if (breathingCoroutine != null)
        {
            StopCoroutine(breathingCoroutine);
        }

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

        Debug.Log($"Respiración: {state}");
    }

    private IEnumerator TransitionBreathing(AudioClip newClip)
    {
        // Fade out
        while (breathingSource.volume > 0)
        {
            breathingSource.volume -= Time.deltaTime * breathingFadeSpeed;
            yield return null;
        }

        // Cambiar clip
        breathingSource.clip = newClip;

        if (newClip != null)
        {
            breathingSource.loop = true;
            breathingSource.Play();

            // Fade in
            float targetVolume = GameData.instance != null
                ? GameData.instance.sfxVolume * GameData.instance.masterVolume * 0.7f
                : 0.7f;

            while (breathingSource.volume < targetVolume)
            {
                breathingSource.volume += Time.deltaTime * breathingFadeSpeed;
                yield return null;
            }
        }
    }

    public void StopBreathing()
    {
        if (breathingCoroutine != null)
        {
            StopCoroutine(breathingCoroutine);
        }
        breathingSource.Stop();
        currentBreathingState = BreathingState.Normal;
    }

    public BreathingState GetCurrentBreathingState() => currentBreathingState;
    #endregion

    #region HEARTBEAT SYSTEM
    public void StartHeartbeat(float intensity = 1f)
    {
        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
        }
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
            {
                PlaySFX(heartbeatSound, intensity);
            }

            // Más rápido con más intensidad
            float delay = Mathf.Lerp(1.2f, 0.4f, intensity);
            yield return new WaitForSeconds(delay);
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