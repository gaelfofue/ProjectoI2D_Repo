using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class VignetteController : MonoBehaviour
{
    #region REFERENCES
    [Header("References")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private PlayerController player;
    #endregion

    #region VIGNETTE SETTINGS
    [Header("Vignette - Base")]
    [SerializeField] private float baseVignetteIntensity = 0.3f;
    [SerializeField] private float baseVignetteSmoothness = 0.4f;
    [SerializeField] private Color baseVignetteColor = Color.black;

    [Header("Vignette - States")]
    [SerializeField] private float hiddenVignetteIntensity = 0.5f;
    [SerializeField] private float exhaustedVignetteIntensity = 0.55f;
    [SerializeField] private float scaredVignetteIntensity = 0.6f;
    [SerializeField] private float panicVignetteIntensity = 0.7f;

    [Header("Vignette - Colors")]
    [SerializeField] private Color hiddenVignetteColor = new Color(0.05f, 0.02f, 0.1f);
    [SerializeField] private Color exhaustedVignetteColor = new Color(0.2f, 0.05f, 0.05f);
    [SerializeField] private Color fearVignetteColor = new Color(0.1f, 0.02f, 0.15f);
    [SerializeField] private Color panicVignetteColor = new Color(0.3f, 0.02f, 0.08f);
    #endregion

    #region COLOR SETTINGS
    [Header("Color Adjustments - Base")]
    [SerializeField] private float baseSaturation = -10f;
    [SerializeField] private float baseContrast = 5f;
    [SerializeField] private float baseExposure = 0f;

    [Header("Color Adjustments - Fear")]
    [SerializeField] private float fearSaturation = -35f;
    [SerializeField] private float panicSaturation = -50f;
    [SerializeField] private float fearContrast = 15f;
    [SerializeField] private float panicContrast = 25f;
    [SerializeField] private float fearExposure = -0.2f;
    [SerializeField] private float panicExposure = -0.4f;

    [Header("Color Adjustments - Exhausted")]
    [SerializeField] private float exhaustedSaturation = -25f;
    [SerializeField] private float exhaustedContrast = 10f;
    #endregion

    #region CHROMATIC ABERRATION
    [Header("Chromatic Aberration")]
    [SerializeField] private bool enableChromaticAberration = true;
    [SerializeField] private float baseChromaticIntensity = 0f;
    [SerializeField] private float scaredChromaticIntensity = 0.15f;
    [SerializeField] private float panicChromaticIntensity = 0.5f;
    [SerializeField] private float damageChromaticIntensity = 0.8f;
    #endregion

    #region LENS DISTORTION
    [Header("Lens Distortion")]
    [SerializeField] private bool enableLensDistortion = true;
    [SerializeField] private float baseLensDistortion = 0f;
    [SerializeField] private float panicLensDistortion = -0.3f;
    [SerializeField] private float hiddenLensDistortion = -0.1f;
    #endregion

    #region FILM GRAIN
    [Header("Film Grain")]
    [SerializeField] private bool enableFilmGrain = true;
    [SerializeField] private float baseGrainIntensity = 0.15f;
    [SerializeField] private float fearGrainIntensity = 0.35f;
    [SerializeField] private float panicGrainIntensity = 0.5f;
    #endregion

    #region BLOOM
    [Header("Bloom")]
    [SerializeField] private bool enableBloom = true;
    [SerializeField] private float baseBloomIntensity = 0.5f;
    [SerializeField] private float fearBloomIntensity = 0.3f;
    [SerializeField] private float panicBloomIntensity = 0.8f;
    #endregion

    #region PULSE SETTINGS
    [Header("Pulse Effect - Heartbeat Visual")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float normalPulseSpeed = 1f;
    [SerializeField] private float nervousPulseSpeed = 1.5f;
    [SerializeField] private float scaredPulseSpeed = 2.5f;
    [SerializeField] private float panicPulseSpeed = 5f;
    [SerializeField] private float basePulseIntensity = 0.02f;
    [SerializeField] private float fearPulseIntensity = 0.05f;
    [SerializeField] private float panicPulseIntensity = 0.12f;
    #endregion

    #region TRANSITION
    [Header("Transition Speeds")]
    [SerializeField] private float normalTransitionSpeed = 3f;
    [SerializeField] private float fastTransitionSpeed = 8f;
    [SerializeField] private float flashTransitionSpeed = 20f;
    #endregion

    #region PRIVATE VARIABLES
    // Post-processing effects
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private ChromaticAberration chromaticAberration;
    private FilmGrain filmGrain;
    private Bloom bloom;
    private LensDistortion lensDistortion;

    // Target values - Vignette
    private float targetVignetteIntensity;
    private float targetVignetteSmoothness;
    private Color targetVignetteColor;

    // Target values - Color
    private float targetSaturation;
    private float targetContrast;
    private float targetExposure;

    // Target values - Effects
    private float targetChromaticIntensity;
    private float targetLensDistortion;
    private float targetGrainIntensity;
    private float targetBloomIntensity;

    // State
    private bool isInitialized = false;
    private bool isFlashing = false;
    private float currentTransitionSpeed;

    // Pulse
    private float pulseTimer = 0f;
    #endregion

    #region UNITY METHODS
    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (player == null)
        {
            TryFindPlayer();
            return;
        }

        pulseTimer += Time.deltaTime;

        if (!isFlashing)
        {
            CalculateTargetValues();
        }

        ApplyEffects();
    }

    private void OnDestroy()
    {
        ResetEffectsToDefault();
    }
    #endregion

    #region INITIALIZATION
    private void Initialize()
    {
        // Buscar Volume
        if (postProcessVolume == null)
        {
            postProcessVolume = FindFirstObjectByType<Volume>();
        }

        if (postProcessVolume == null || postProcessVolume.profile == null)
        {
            Debug.LogError("[VignetteController] No se encontró Volume o Profile en la escena!");
            enabled = false;
            return;
        }

        // Obtener o crear efectos
        InitializeVignette();
        InitializeColorAdjustments();
        InitializeChromaticAberration();
        InitializeFilmGrain();
        InitializeBloom();
        InitializeLensDistortion();

        // Buscar player
        TryFindPlayer();

        currentTransitionSpeed = normalTransitionSpeed;
        isInitialized = true;

        Debug.Log("[VignetteController] V2.0 Inicializado correctamente");
    }

    private void InitializeVignette()
    {
        if (!postProcessVolume.profile.TryGet(out vignette))
        {
            vignette = postProcessVolume.profile.Add<Vignette>(true);
            Debug.Log("[VignetteController] Vignette creado");
        }

        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.color.overrideState = true;
        vignette.smoothness.overrideState = true;
        vignette.rounded.overrideState = true;

        vignette.intensity.value = baseVignetteIntensity;
        vignette.color.value = baseVignetteColor;
        vignette.smoothness.value = baseVignetteSmoothness;
        vignette.rounded.value = true;
    }

    private void InitializeColorAdjustments()
    {
        if (!postProcessVolume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = postProcessVolume.profile.Add<ColorAdjustments>(true);
            Debug.Log("[VignetteController] ColorAdjustments creado");
        }

        colorAdjustments.active = true;
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.postExposure.overrideState = true;

        colorAdjustments.saturation.value = baseSaturation;
        colorAdjustments.contrast.value = baseContrast;
        colorAdjustments.postExposure.value = baseExposure;
    }

    private void InitializeChromaticAberration()
    {
        if (!enableChromaticAberration) return;

        if (!postProcessVolume.profile.TryGet(out chromaticAberration))
        {
            chromaticAberration = postProcessVolume.profile.Add<ChromaticAberration>(true);
            Debug.Log("[VignetteController] ChromaticAberration creado");
        }

        chromaticAberration.active = true;
        chromaticAberration.intensity.overrideState = true;
        chromaticAberration.intensity.value = baseChromaticIntensity;
    }

    private void InitializeFilmGrain()
    {
        if (!enableFilmGrain) return;

        if (!postProcessVolume.profile.TryGet(out filmGrain))
        {
            filmGrain = postProcessVolume.profile.Add<FilmGrain>(true);
            Debug.Log("[VignetteController] FilmGrain creado");
        }

        filmGrain.active = true;
        filmGrain.intensity.overrideState = true;
        filmGrain.response.overrideState = true;
        filmGrain.type.overrideState = true;

        filmGrain.type.value = FilmGrainLookup.Thin1;
        filmGrain.intensity.value = baseGrainIntensity;
        filmGrain.response.value = 0.8f;
    }

    private void InitializeBloom()
    {
        if (!enableBloom) return;

        if (!postProcessVolume.profile.TryGet(out bloom))
        {
            bloom = postProcessVolume.profile.Add<Bloom>(true);
            Debug.Log("[VignetteController] Bloom creado");
        }

        bloom.active = true;
        bloom.intensity.overrideState = true;
        bloom.threshold.overrideState = true;
        bloom.scatter.overrideState = true;

        bloom.threshold.value = 0.9f;
        bloom.intensity.value = baseBloomIntensity;
        bloom.scatter.value = 0.7f;
    }

    private void InitializeLensDistortion()
    {
        if (!enableLensDistortion) return;

        if (!postProcessVolume.profile.TryGet(out lensDistortion))
        {
            lensDistortion = postProcessVolume.profile.Add<LensDistortion>(true);
            Debug.Log("[VignetteController] LensDistortion creado");
        }

        lensDistortion.active = true;
        lensDistortion.intensity.overrideState = true;
        lensDistortion.intensity.value = baseLensDistortion;
    }

    private void TryFindPlayer()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();

            if (player != null)
            {
                Debug.Log("[VignetteController] PlayerController encontrado");
            }
        }
    }
    #endregion

    #region CALCULATE VALUES
    private void CalculateTargetValues()
    {
        // Obtener estados del jugador
        float staminaPercent = player.GetStaminaNormalized();
        float fearPercent = player.GetFearNormalized();
        bool isHidden = player.IsHidden;
        bool isExhausted = player.IsExhausted;
        bool isPanicking = player.IsPanicking;
        bool isScared = player.IsScared;

        // Determinar velocidad de transición
        currentTransitionSpeed = isPanicking ? fastTransitionSpeed : normalTransitionSpeed;

        // Calcular cada grupo de efectos
        CalculateVignetteValues(staminaPercent, fearPercent, isHidden, isExhausted, isPanicking, isScared);
        CalculateColorValues(staminaPercent, fearPercent, isHidden, isExhausted, isPanicking, isScared);
        CalculateEffectValues(staminaPercent, fearPercent, isHidden, isExhausted, isPanicking, isScared);

        // Añadir pulso si está habilitado
        if (enablePulse)
        {
            ApplyPulseEffect(staminaPercent, fearPercent, isExhausted, isPanicking, isScared);
        }
    }

    private void CalculateVignetteValues(float stamina, float fear, bool hidden, bool exhausted, bool panic, bool scared)
    {
        // === INTENSIDAD BASE ===
        float intensity = baseVignetteIntensity;
        Color color = baseVignetteColor;
        float smoothness = baseVignetteSmoothness;

        // Estado: Escondido
        if (hidden)
        {
            intensity = Mathf.Max(intensity, hiddenVignetteIntensity);
            color = Color.Lerp(color, hiddenVignetteColor, 0.7f);
            smoothness = 0.3f; // Más cerrado cuando está escondido

            // Añadir miedo encima del escondite
            if (fear > 0.3f)
            {
                float fearInfluence = (fear - 0.3f) / 0.7f;
                intensity = Mathf.Lerp(intensity, scaredVignetteIntensity, fearInfluence);
                color = Color.Lerp(color, fearVignetteColor, fearInfluence * 0.5f);
            }
        }

        // Estado: Pánico (máxima prioridad)
        if (panic)
        {
            intensity = panicVignetteIntensity;
            color = panicVignetteColor;
            smoothness = 0.25f;
        }
        // Estado: Asustada
        else if (scared)
        {
            intensity = Mathf.Max(intensity, scaredVignetteIntensity);
            color = Color.Lerp(color, fearVignetteColor, 0.6f);
            smoothness = 0.3f;
        }
        // Estado: Agotada
        else if (exhausted)
        {
            intensity = Mathf.Max(intensity, exhaustedVignetteIntensity);
            color = Color.Lerp(color, exhaustedVignetteColor, 0.5f);
        }
        // Stamina baja
        else if (stamina < 0.3f)
        {
            float exhaustionFactor = 1f - (stamina / 0.3f);
            intensity = Mathf.Lerp(intensity, exhaustedVignetteIntensity * 0.8f, exhaustionFactor);
            color = Color.Lerp(color, exhaustedVignetteColor, exhaustionFactor * 0.3f);
        }

        targetVignetteIntensity = Mathf.Clamp(intensity, 0f, 0.8f);
        targetVignetteColor = color;
        targetVignetteSmoothness = smoothness;
    }

    private void CalculateColorValues(float stamina, float fear, bool hidden, bool exhausted, bool panic, bool scared)
    {
        // === SATURACIÓN ===
        float saturation = baseSaturation;
        float contrast = baseContrast;
        float exposure = baseExposure;

        if (panic)
        {
            saturation = panicSaturation;
            contrast = panicContrast;
            exposure = panicExposure;
        }
        else if (scared)
        {
            float t = (fear - 0.6f) / 0.3f; // 0.6 a 0.9
            saturation = Mathf.Lerp(fearSaturation * 0.5f, fearSaturation, t);
            contrast = Mathf.Lerp(baseContrast, fearContrast, t);
            exposure = Mathf.Lerp(baseExposure, fearExposure, t);
        }
        else if (exhausted)
        {
            saturation = exhaustedSaturation;
            contrast = exhaustedContrast;
        }
        else if (hidden && fear > 0.3f)
        {
            // Desaturar gradualmente mientras está escondida
            saturation = Mathf.Lerp(baseSaturation, fearSaturation, fear);
            contrast = Mathf.Lerp(baseContrast, fearContrast * 0.5f, fear);
        }
        else if (stamina < 0.3f)
        {
            float t = 1f - (stamina / 0.3f);
            saturation = Mathf.Lerp(baseSaturation, exhaustedSaturation, t);
        }

        targetSaturation = saturation;
        targetContrast = contrast;
        targetExposure = exposure;
    }

    private void CalculateEffectValues(float stamina, float fear, bool hidden, bool exhausted, bool panic, bool scared)
    {
        // === CHROMATIC ABERRATION ===
        if (enableChromaticAberration && chromaticAberration != null)
        {
            if (panic)
            {
                targetChromaticIntensity = panicChromaticIntensity;
            }
            else if (scared)
            {
                targetChromaticIntensity = scaredChromaticIntensity;
            }
            else if (fear > 0.5f)
            {
                targetChromaticIntensity = Mathf.Lerp(0, scaredChromaticIntensity, (fear - 0.5f) * 2f);
            }
            else
            {
                targetChromaticIntensity = baseChromaticIntensity;
            }
        }

        // === LENS DISTORTION ===
        if (enableLensDistortion && lensDistortion != null)
        {
            if (panic)
            {
                targetLensDistortion = panicLensDistortion;
            }
            else if (hidden && fear > 0.5f)
            {
                targetLensDistortion = Mathf.Lerp(hiddenLensDistortion, panicLensDistortion * 0.5f, fear);
            }
            else if (hidden)
            {
                targetLensDistortion = hiddenLensDistortion;
            }
            else
            {
                targetLensDistortion = baseLensDistortion;
            }
        }

        // === FILM GRAIN ===
        if (enableFilmGrain && filmGrain != null)
        {
            if (panic)
            {
                targetGrainIntensity = panicGrainIntensity;
            }
            else if (scared || fear > 0.5f)
            {
                targetGrainIntensity = Mathf.Lerp(baseGrainIntensity, fearGrainIntensity, fear);
            }
            else if (exhausted || stamina < 0.3f)
            {
                targetGrainIntensity = Mathf.Lerp(baseGrainIntensity, fearGrainIntensity * 0.7f, 1f - stamina);
            }
            else
            {
                targetGrainIntensity = baseGrainIntensity;
            }
        }

        // === BLOOM ===
        if (enableBloom && bloom != null)
        {
            if (panic)
            {
                // Bloom alto en pánico para efecto de "ver estrellas"
                targetBloomIntensity = panicBloomIntensity;
            }
            else if (scared)
            {
                // Reducir bloom cuando está asustada (visión más enfocada)
                targetBloomIntensity = fearBloomIntensity;
            }
            else
            {
                targetBloomIntensity = baseBloomIntensity;
            }
        }
    }

    private void ApplyPulseEffect(float stamina, float fear, bool exhausted, bool panic, bool scared)
    {
        float pulseSpeed = normalPulseSpeed;
        float pulseAmount = 0f;

        // Determinar velocidad e intensidad del pulso
        if (panic)
        {
            pulseSpeed = panicPulseSpeed;
            pulseAmount = panicPulseIntensity;
        }
        else if (scared)
        {
            pulseSpeed = scaredPulseSpeed;
            pulseAmount = fearPulseIntensity;
        }
        else if (fear > 0.3f)
        {
            pulseSpeed = nervousPulseSpeed;
            pulseAmount = Mathf.Lerp(basePulseIntensity, fearPulseIntensity, fear);
        }
        else if (exhausted || stamina < 0.3f)
        {
            pulseSpeed = nervousPulseSpeed;
            pulseAmount = basePulseIntensity * 1.5f;
        }

        if (pulseAmount > 0)
        {
            // Pulso sinusoidal (simula latido)
            float pulse = Mathf.Sin(pulseTimer * pulseSpeed * Mathf.PI * 2f);

            // Convertir a pulso tipo latido (más pronunciado en un lado)
            pulse = (pulse + 1f) * 0.5f; // Normalizar a 0-1
            pulse = Mathf.Pow(pulse, 2f); // Hacer más pronunciado

            // Aplicar a vignette
            targetVignetteIntensity += pulse * pulseAmount;

            // También afectar ligeramente chromatic en pánico
            if (panic && enableChromaticAberration)
            {
                targetChromaticIntensity += pulse * 0.1f;
            }
        }
    }
    #endregion

    #region APPLY EFFECTS
    private void ApplyEffects()
    {
        float deltaSpeed = currentTransitionSpeed * Time.deltaTime;
        float speed = isFlashing ? flashTransitionSpeed * Time.deltaTime : deltaSpeed;

        // === VIGNETTE ===
        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetVignetteIntensity, speed);
            vignette.color.value = Color.Lerp(vignette.color.value, targetVignetteColor, speed);
            vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, targetVignetteSmoothness, speed);
        }

        // === COLOR ADJUSTMENTS ===
        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, targetSaturation, speed);
            colorAdjustments.contrast.value = Mathf.Lerp(colorAdjustments.contrast.value, targetContrast, speed);
            colorAdjustments.postExposure.value = Mathf.Lerp(colorAdjustments.postExposure.value, targetExposure, speed);
        }

        // === CHROMATIC ABERRATION ===
        if (chromaticAberration != null && enableChromaticAberration)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity.value, targetChromaticIntensity, speed);
        }

        // === LENS DISTORTION ===
        if (lensDistortion != null && enableLensDistortion)
        {
            lensDistortion.intensity.value = Mathf.Lerp(lensDistortion.intensity.value, targetLensDistortion, speed);
        }

        // === FILM GRAIN ===
        if (filmGrain != null && enableFilmGrain)
        {
            filmGrain.intensity.value = Mathf.Lerp(filmGrain.intensity.value, targetGrainIntensity, speed);
        }

        // === BLOOM ===
        if (bloom != null && enableBloom)
        {
            bloom.intensity.value = Mathf.Lerp(bloom.intensity.value, targetBloomIntensity, speed);
        }
    }
    #endregion

    #region FLASH EFFECTS
    /// <summary>
    /// Flash de daño - rojo intenso con chromatic aberration
    /// </summary>
    public void DamageFlash()
    {
        StartCoroutine(DamageFlashRoutine());
    }

    /// <summary>
    /// Flash de muerte - fade a negro total
    /// </summary>
    public void DeathFlash()
    {
        StartCoroutine(DeathFlashRoutine());
    }

    /// <summary>
    /// Flash de pánico - distorsión y colores intensos
    /// </summary>
    public void PanicFlash()
    {
        StartCoroutine(PanicFlashRoutine());
    }

    /// <summary>
    /// Flash de descubrimiento - shock visual
    /// </summary>
    public void DiscoveryFlash()
    {
        StartCoroutine(DiscoveryFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (vignette == null) yield break;

        isFlashing = true;

        // Guardar valores
        float originalIntensity = vignette.intensity.value;
        Color originalColor = vignette.color.value;
        float originalChromatic = chromaticAberration?.intensity.value ?? 0f;

        // Flash inmediato
        vignette.intensity.value = 0.75f;
        vignette.color.value = new Color(0.6f, 0f, 0f);
        if (chromaticAberration != null)
            chromaticAberration.intensity.value = damageChromaticIntensity;

        yield return new WaitForSecondsRealtime(0.1f);

        // Fade out rápido
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            vignette.intensity.value = Mathf.Lerp(0.75f, originalIntensity, t);
            vignette.color.value = Color.Lerp(new Color(0.6f, 0f, 0f), originalColor, t);
            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(damageChromaticIntensity, originalChromatic, t);

            yield return null;
        }

        isFlashing = false;
    }

    private IEnumerator DeathFlashRoutine()
    {
        if (vignette == null) yield break;

        isFlashing = true;

        // Flash rojo inicial
        vignette.intensity.value = 0.8f;
        vignette.color.value = new Color(0.5f, 0f, 0f);

        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = -100f;
            colorAdjustments.postExposure.value = -0.5f;
        }

        yield return new WaitForSecondsRealtime(0.15f);

        // Fade a negro
        float elapsed = 0f;
        float duration = 0.8f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            vignette.intensity.value = Mathf.Lerp(0.8f, 1f, t);
            vignette.color.value = Color.Lerp(new Color(0.5f, 0f, 0f), Color.black, t);

            if (colorAdjustments != null)
                colorAdjustments.postExposure.value = Mathf.Lerp(-0.5f, -2f, t);

            yield return null;
        }

        // Mantener negro
        vignette.intensity.value = 1f;
        vignette.color.value = Color.black;

        isFlashing = false;
    }

    private IEnumerator PanicFlashRoutine()
    {
        if (vignette == null) yield break;

        isFlashing = true;

        float originalIntensity = vignette.intensity.value;
        Color originalColor = vignette.color.value;
        float originalLens = lensDistortion?.intensity.value ?? 0f;

        // Pulsos rápidos
        for (int i = 0; i < 3; i++)
        {
            vignette.intensity.value = 0.7f;
            vignette.color.value = panicVignetteColor;
            if (lensDistortion != null)
                lensDistortion.intensity.value = -0.4f;

            yield return new WaitForSecondsRealtime(0.08f);

            vignette.intensity.value = 0.5f;
            if (lensDistortion != null)
                lensDistortion.intensity.value = -0.2f;

            yield return new WaitForSecondsRealtime(0.08f);
        }

        // Volver gradualmente
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            vignette.intensity.value = Mathf.Lerp(0.5f, originalIntensity, t);
            vignette.color.value = Color.Lerp(panicVignetteColor, originalColor, t);
            if (lensDistortion != null)
                lensDistortion.intensity.value = Mathf.Lerp(-0.2f, originalLens, t);

            yield return null;
        }

        isFlashing = false;
    }

    private IEnumerator DiscoveryFlashRoutine()
    {
        if (vignette == null) yield break;

        isFlashing = true;

        float originalIntensity = vignette.intensity.value;
        Color originalColor = vignette.color.value;
        float originalExposure = colorAdjustments?.postExposure.value ?? 0f;

        // Flash blanco (shock)
        vignette.intensity.value = 0.3f;
        vignette.color.value = Color.white;
        if (colorAdjustments != null)
            colorAdjustments.postExposure.value = 1f;
        if (chromaticAberration != null)
            chromaticAberration.intensity.value = 0.6f;

        yield return new WaitForSecondsRealtime(0.05f);

        // Transición a rojo
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / 0.15f;

            vignette.color.value = Color.Lerp(Color.white, new Color(0.7f, 0f, 0f), t);
            vignette.intensity.value = Mathf.Lerp(0.3f, 0.75f, t);
            if (colorAdjustments != null)
                colorAdjustments.postExposure.value = Mathf.Lerp(1f, -0.3f, t);

            yield return null;
        }

        // Mantener rojo
        yield return new WaitForSecondsRealtime(0.2f);

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / 0.4f;

            vignette.intensity.value = Mathf.Lerp(0.75f, originalIntensity, t);
            vignette.color.value = Color.Lerp(new Color(0.7f, 0f, 0f), originalColor, t);
            if (colorAdjustments != null)
                colorAdjustments.postExposure.value = Mathf.Lerp(-0.3f, originalExposure, t);
            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(0.6f, 0f, t);

            yield return null;
        }

        isFlashing = false;
    }
    #endregion

    #region RESET
    private void ResetEffectsToDefault()
    {
        if (vignette != null)
        {
            vignette.intensity.value = baseVignetteIntensity;
            vignette.color.value = baseVignetteColor;
            vignette.smoothness.value = baseVignetteSmoothness;
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = 0f;
            colorAdjustments.contrast.value = 0f;
            colorAdjustments.postExposure.value = 0f;
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = 0f;
        }

        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = 0f;
        }

        if (filmGrain != null)
        {
            filmGrain.intensity.value = 0f;
        }

        if (bloom != null)
        {
            bloom.intensity.value = baseBloomIntensity;
        }
    }
    #endregion

    #region PUBLIC METHODS
    public void ForceUpdate()
    {
        if (player != null && isInitialized)
        {
            CalculateTargetValues();
        }
    }

    public Vignette GetVignette() => vignette;
    public ColorAdjustments GetColorAdjustments() => colorAdjustments;
    #endregion

    #region DEBUG
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private void OnGUI()
    {
        if (!showDebugInfo || player == null || !isInitialized) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 12;

        GUILayout.BeginArea(new Rect(10, 10, 280, 320));
        GUILayout.BeginVertical(style);

        GUILayout.Label("<b>=== VignetteController V2 ===</b>");
        GUILayout.Space(5);

        GUILayout.Label($"<b>Player State:</b>");
        GUILayout.Label($"  Stamina: {player.GetStaminaNormalized():P0}");
        GUILayout.Label($"  Fear: {player.GetFearNormalized():P0}");
        GUILayout.Label($"  Hidden: {player.IsHidden}");
        GUILayout.Label($"  Exhausted: {player.IsExhausted}");
        GUILayout.Label($"  Scared: {player.IsScared}");
        GUILayout.Label($"  Panicking: {player.IsPanicking}");

        GUILayout.Space(10);
        GUILayout.Label($"<b>Current Effects:</b>");
        GUILayout.Label($"  Vignette: {vignette?.intensity.value:F2}");
        GUILayout.Label($"  Saturation: {colorAdjustments?.saturation.value:F0}");
        GUILayout.Label($"  Contrast: {colorAdjustments?.contrast.value:F0}");
        GUILayout.Label($"  Chromatic: {chromaticAberration?.intensity.value:F2}");
        GUILayout.Label($"  Lens Dist: {lensDistortion?.intensity.value:F2}");
        GUILayout.Label($"  Film Grain: {filmGrain?.intensity.value:F2}");
        GUILayout.Label($"  Bloom: {bloom?.intensity.value:F2}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    #endregion
}