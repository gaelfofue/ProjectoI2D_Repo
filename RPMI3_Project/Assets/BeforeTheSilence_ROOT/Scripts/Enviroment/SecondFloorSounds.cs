using UnityEngine;
using System.Collections;

/// <summary>
/// Control de audio específico para la escena "Piso 2 con cambios".
/// Enna sale del baño, escucha al monstruo abajo, baja las escaleras.
/// Ponlo en un GameObject vacío llamado "AudioController_Piso2Limbo".
/// </summary>
public class Piso2LimboAudio : MonoBehaviour
{
    [Header("=== MÚSICA DE LA ESCENA ===")]
    [SerializeField] private AudioClip ambienciaOscura;

    [Header("=== SONIDOS DEL MONSTRUO (Piso de abajo) ===")]
    [SerializeField] private AudioClip monsterGrowl;
    [SerializeField] private AudioClip monsterFootsteps;
    [SerializeField] private AudioClip monsterBreathing;
    [SerializeField] private AudioClip distantBang;

    [Header("=== SONIDOS AMBIENTALES ===")]
    [SerializeField] private AudioClip floorCreak;
    [SerializeField] private AudioClip windInside;
    [SerializeField] private AudioClip clockTicking;

    [Header("=== CONFIGURACIÓN ===")]
    [SerializeField] private float delayInicial = 1.5f;
    [SerializeField] private float intervaloMonstruo = 4f;
    [SerializeField] private float variacionIntervalo = 2f;
    [SerializeField] private bool iniciarAutomaticamente = true;

    [Header("=== TRIGGER DE ESCALERAS ===")]
    [SerializeField] private Transform zonaCercaEscaleras;
    [SerializeField] private float distanciaEscaleras = 3f;
    [SerializeField] private AudioClip stairCreak;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool mostrarDebug = true;

    // Audio Sources propios
    private AudioSource ambientSource;
    private AudioSource monsterSource;
    private AudioSource sfxSource;

    private Transform playerTransform;
    private bool secuenciaActiva = false;
    private bool cercaDeEscaleras = false;
    private bool intensificado = false;
    private Coroutine secuenciaMonstruo;

    #region UNITY METHODS
    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        CrearAudioSources();

        // ===== Desactivar heartbeat automático del AudioManager =====
        // Esta escena maneja el heartbeat manualmente
        DisableAutoHeartbeat();

        IniciarMusicaBase();

        if (iniciarAutomaticamente)
        {
            StartCoroutine(IniciarSecuenciaConDelay());
        }
    }

    private void Update()
    {
        if (!secuenciaActiva || playerTransform == null) return;

        // Actualizar volúmenes según AudioManager
        UpdateLocalVolumes();

        // Verificar proximidad a escaleras
        if (zonaCercaEscaleras != null)
        {
            float distancia = Vector2.Distance(
                playerTransform.position,
                zonaCercaEscaleras.position
            );

            if (distancia < distanciaEscaleras && !cercaDeEscaleras)
            {
                cercaDeEscaleras = true;
                IntensificarCercaEscaleras();
            }
        }
    }

    private void OnDestroy()
    {
        // Limpiar al salir de la escena
        secuenciaActiva = false;

        if (secuenciaMonstruo != null)
            StopCoroutine(secuenciaMonstruo);
    }
    #endregion

    #region SETUP
    private void CrearAudioSources()
    {
        GameObject ambObj = new GameObject("Ambient_Piso2");
        ambObj.transform.SetParent(transform);
        ambientSource = ambObj.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.playOnAwake = false;
        ambientSource.volume = 0f;
        ambientSource.spatialBlend = 0f;

        GameObject monObj = new GameObject("Monster_Sounds");
        monObj.transform.SetParent(transform);
        monsterSource = monObj.AddComponent<AudioSource>();
        monsterSource.loop = false;
        monsterSource.playOnAwake = false;
        monsterSource.volume = 0f;
        monsterSource.spatialBlend = 0f;

        GameObject sfxObj = new GameObject("SFX_Piso2");
        sfxObj.transform.SetParent(transform);
        sfxSource = sfxObj.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
    }

    /// <summary>
    /// Desactiva el heartbeat automático del AudioManager
    /// para que esta escena lo controle manualmente.
    /// </summary>
    private void DisableAutoHeartbeat()
    {
        if (AudioManager.Instance == null) return;

        // Parar heartbeat si estaba activo de otra escena
        AudioManager.Instance.StopHeartbeat();

        Log("Heartbeat automático desactivado (control manual en esta escena)");
    }
    #endregion

    #region VOLUME SYNC
    /// <summary>
    /// Aplica el volumen master y SFX del AudioManager a los sources locales.
    /// Se llama cada frame para mantener sincronía con sliders de opciones.
    /// </summary>
    private void UpdateLocalVolumes()
    {
        if (AudioManager.Instance == null) return;

        float master = AudioManager.Instance.MasterVolume;
        float sfx = AudioManager.Instance.SFXVolume;
        float globalScale = master * sfx;

        // No sobreescribir volumen si están en medio de un fade
        // Solo aplicar el multiplicador global
        // Los volúmenes base se setean al reproducir cada sonido
    }

    /// <summary>
    /// Calcula el volumen final aplicando el volumen global del AudioManager.
    /// Usar esto en vez de poner volúmenes directos.
    /// </summary>
    private float GetScaledVolume(float baseVolume)
    {
        if (AudioManager.Instance != null)
        {
            return baseVolume * AudioManager.Instance.SFXVolume * AudioManager.Instance.MasterVolume;
        }
        return baseVolume;
    }
    #endregion

    #region INICIO DE AUDIO
    private void IniciarMusicaBase()
    {
        // Música via AudioManager
        if (AudioManager.Instance != null && ambienciaOscura != null)
        {
            AudioManager.Instance.PlayMusic(ambienciaOscura, 2f);
            Log("Música base iniciada via AudioManager");
        }
        else if (ambienciaOscura != null)
        {
            ambientSource.clip = ambienciaOscura;
            ambientSource.Play();
            StartCoroutine(FadeIn(ambientSource, 0.4f, 2f));
            Log("Música base iniciada via source local");
        }

        // Viento interior como ambiente extra
        if (windInside != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayAmbient(windInside, 0.3f);
            Log("Viento interior iniciado");
        }

        // Respiración nerviosa desde el inicio
        if (AudioManager.Instance != null)
        {
            // ===== FIX: Usar ForceStart para asegurar que suena =====
            AudioManager.Instance.ForceStartBreathing(BreathingState.Scared);
            Log("Respiración: Scared (forzada)");
        }
    }
    #endregion

    #region SECUENCIA DEL MONSTRUO
    private IEnumerator IniciarSecuenciaConDelay()
    {
        Log($"Esperando {delayInicial}s antes de iniciar sonidos...");
        yield return new WaitForSeconds(delayInicial);

        secuenciaActiva = true;

        yield return StartCoroutine(PrimerSonidoMonstruo());

        secuenciaMonstruo = StartCoroutine(LoopSonidosMonstruo());
    }

    private IEnumerator PrimerSonidoMonstruo()
    {
        if (distantBang != null)
        {
            // ===== Volumen escalado con AudioManager =====
            monsterSource.volume = GetScaledVolume(0.7f);
            monsterSource.clip = distantBang;
            monsterSource.Play();
            Log("¡BANG! Primer sonido del monstruo");

            yield return new WaitForSeconds(1.5f);
        }

        if (monsterGrowl != null)
        {
            monsterSource.volume = GetScaledVolume(0.5f);
            monsterSource.clip = monsterGrowl;
            monsterSource.Play();
            Log("Gruñido del monstruo");
        }

        yield return new WaitForSeconds(2f);
    }

    private IEnumerator LoopSonidosMonstruo()
    {
        AudioClip[] sonidosMonstruo = GetSonidosDisponibles();

        if (sonidosMonstruo.Length == 0)
        {
            Log("No hay sonidos de monstruo asignados");
            yield break;
        }

        while (secuenciaActiva)
        {
            float espera = intervaloMonstruo
                + Random.Range(-variacionIntervalo, variacionIntervalo);
            espera = Mathf.Max(espera, 1.5f);

            yield return new WaitForSeconds(espera);

            if (!secuenciaActiva) yield break;

            AudioClip clip = sonidosMonstruo[Random.Range(0, sonidosMonstruo.Length)];

            float volumenBase = Random.Range(0.3f, 0.7f);

            if (cercaDeEscaleras)
            {
                volumenBase = Random.Range(0.6f, 0.9f);
            }

            // ===== Volumen escalado =====
            monsterSource.volume = GetScaledVolume(volumenBase);
            monsterSource.pitch = Random.Range(0.85f, 1.15f);
            monsterSource.clip = clip;
            monsterSource.Play();

            Log($"Sonido monstruo: {clip.name} (vol base: {volumenBase:F2}, final: {monsterSource.volume:F2})");

            // Crujido ocasional
            if (Random.value > 0.6f && floorCreak != null)
            {
                yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
                sfxSource.volume = GetScaledVolume(Random.Range(0.2f, 0.4f));
                sfxSource.PlayOneShot(floorCreak);
            }
        }
    }
    #endregion

    #region INTENSIFICACIÓN (ESCALERAS)
    private void IntensificarCercaEscaleras()
    {
        if (intensificado) return;
        intensificado = true;

        Log("¡Jugador cerca de escaleras! Intensificando...");

        StartCoroutine(SecuenciaIntensificacion());
    }

    private IEnumerator SecuenciaIntensificacion()
    {
        // Crujido de escaleras
        if (stairCreak != null)
        {
            sfxSource.volume = GetScaledVolume(0.6f);
            sfxSource.PlayOneShot(stairCreak);
        }

        yield return new WaitForSeconds(0.8f);

        // Gruñido más fuerte y cercano
        if (monsterGrowl != null)
        {
            monsterSource.volume = GetScaledVolume(0.85f);
            monsterSource.pitch = 0.8f;
            monsterSource.clip = monsterGrowl;
            monsterSource.Play();
        }

        // ===== Pánico + Heartbeat via AudioManager =====
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBreathingState(BreathingState.Panic);
            AudioManager.Instance.StartHeartbeat(0.8f);
            Log("Respiración: Panic + Heartbeat 0.8");
        }

        // Reducir intervalo
        intervaloMonstruo = 2f;
        variacionIntervalo = 1f;

        // Música de tensión
        SceneMusicController smc = SceneMusicController.GetCurrent();
        if (smc != null)
        {
            smc.PlayTensionMusic();
        }
    }
    #endregion

    #region MÉTODOS PÚBLICOS
    public void ReproducirSonidoMonstruo(float volumen = 0.6f)
    {
        if (monsterGrowl != null)
        {
            monsterSource.volume = GetScaledVolume(volumen);
            monsterSource.clip = monsterGrowl;
            monsterSource.Play();
        }
    }

    public void ReproducirGolpe(float volumen = 0.7f)
    {
        if (distantBang != null)
        {
            sfxSource.volume = GetScaledVolume(volumen);
            sfxSource.PlayOneShot(distantBang);
        }
    }

    public void PararTodo(float fadeTime = 1f)
    {
        secuenciaActiva = false;

        if (secuenciaMonstruo != null)
        {
            StopCoroutine(secuenciaMonstruo);
            secuenciaMonstruo = null;
        }

        StartCoroutine(FadeOutTodo(fadeTime));
    }

    public void IniciarSecuencia()
    {
        if (!secuenciaActiva)
        {
            StartCoroutine(IniciarSecuenciaConDelay());
        }
    }

    /// <summary>
    /// Para llamar desde triggers: intensificar heartbeat gradualmente.
    /// </summary>
    public void SetHeartbeatFromTrigger(float intensity)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetHeartbeatIntensity(intensity);
            Log($"Heartbeat manual: {intensity:F2}");
        }
    }
    #endregion

    #region UTILIDADES
    private AudioClip[] GetSonidosDisponibles()
    {
        var lista = new System.Collections.Generic.List<AudioClip>();

        if (monsterGrowl != null) lista.Add(monsterGrowl);
        if (monsterFootsteps != null) lista.Add(monsterFootsteps);
        if (monsterBreathing != null) lista.Add(monsterBreathing);
        if (distantBang != null) lista.Add(distantBang);

        return lista.ToArray();
    }

    private IEnumerator FadeIn(AudioSource source, float targetVol, float duration)
    {
        source.volume = 0f;
        float elapsed = 0f;
        float scaledTarget = GetScaledVolume(targetVol);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, scaledTarget, elapsed / duration);
            yield return null;
        }

        source.volume = scaledTarget;
    }

    private IEnumerator FadeOutTodo(float duration)
    {
        float elapsed = 0f;
        float ambVol = ambientSource.volume;
        float monVol = monsterSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            ambientSource.volume = Mathf.Lerp(ambVol, 0f, t);
            monsterSource.volume = Mathf.Lerp(monVol, 0f, t);

            yield return null;
        }

        ambientSource.Stop();
        monsterSource.Stop();

        // Limpiar AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopHeartbeat();
            AudioManager.Instance.StopBreathing();
        }

        Log("Todo parado y limpio");
    }

    private void Log(string message)
    {
        if (mostrarDebug)
        {
            Debug.Log($"[Piso2Audio] {message}");
        }
    }
    #endregion

    #region GIZMOS
    private void OnDrawGizmosSelected()
    {
        if (zonaCercaEscaleras != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(zonaCercaEscaleras.position, distanciaEscaleras);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, zonaCercaEscaleras.position);
        }
    }
    #endregion
}