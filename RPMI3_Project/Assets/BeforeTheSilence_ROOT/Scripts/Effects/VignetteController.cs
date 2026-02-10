using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// VERSIÓN MEJORADA - Viñeta dinámica continua basada en stamina y miedo
/// </summary>
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

    [Header("Vignette - Dynamic Ranges")]
    [Tooltip("Intensidad máxima cuando stamina = 0")]
    [SerializeField] private float staminaDepleteIntensity = 0.65f;
    [Tooltip("Intensidad máxima cuando fear = 100%")]
    [SerializeField] private float maxFearIntensity = 0.75f;
    [Tooltip("Intensidad cuando está escondido")]
    [SerializeField] private float hiddenVignetteIntensity = 0.5f;

    [Header("Vignette - Colors")]
    [SerializeField] private Color hiddenVignetteColor = new Color(0.05f, 0.02f, 0.1f);
    [SerializeField] private Color lowStaminaColor = new Color(0.2f, 0.05f, 0.05f); // Rojo oscuro
    [SerializeField] private Color fearColor = new Color(0.1f, 0.02f, 0.15f); // Morado oscuro
    [SerializeField] private Color panicColor = new Color(0.3f, 0.02f, 0.08f); // Rojo-morado
    #endregion

    #region COLOR SETTINGS
    [Header("Color Adjustments - Dynamic")]
    [SerializeField] private float baseSaturation = -10f;
    [SerializeField] private float baseContrast = 5f;
    [SerializeField] private float baseExposure = 0f;

    [Range(-100f, 0f)]
    [SerializeField] private float maxFearSaturation = -50f;
    [Range(0f, 50f)]
    [SerializeField] private float maxFearContrast = 25f;
    [Range(-1f, 0f)]
    [SerializeField] private float maxFearExposure = -0.4f;

    [Range(-100f, 0f)]
    [SerializeField] private float lowStaminaSaturation = -25f;
    [Range(0f, 50f)]
    [SerializeField] private float lowStaminaContrast = 10f;
    #endregion

    #region CHROMATIC ABERRATION
    [Header("Chromatic Aberration")]
    [SerializeField] private bool enableChromaticAberration = true;
    [SerializeField] private float baseChromaticIntensity = 0f;
    [SerializeField] private float maxFearChromaticIntensity = 0.5f;
    [SerializeField] private float damageChromaticIntensity = 0.8f;
    #endregion

    #region LENS DISTORTION
    [Header("Lens Distortion")]
    [SerializeField] private bool enableLensDistortion = true;
    [SerializeField] private float baseLensDistortion = 0f;
    [SerializeField] private float maxFearLensDistortion = -0.3f;
    [SerializeField] private float hiddenLensDistortion = -0.1f;
    #endregion

    #region FILM GRAIN
    [Header("Film Grain")]
    [SerializeField] private bool enableFilmGrain = true;
    [SerializeField] private float baseGrainIntensity = 0.15f;
    [SerializeField] private float maxFearGrainIntensity = 0.5f;
    [SerializeField] private float lowStaminaGrainIntensity = 0.35f;
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
    [SerializeField] private float maxPulseSpeed = 5f;
    [SerializeField] private float basePulseIntensity = 0.02f;
    [SerializeField] private float maxPulseIntensity = 0.12f;
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

        Debug.Log("[VignetteController] Versión DINÁMICA inicializada correctamente");
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
        vignette.smoothness.overrideState = true;
        vignette.color.overrideState = true;

        vignette.intensity.value = baseVignetteIntensity;
        vignette.smoothness.value = baseVignetteSmoothness;
        vignette.color.value = baseVignetteColor;
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
        filmGrain.intensity.value = baseGrainIntensity;
        filmGrain.type.value = FilmGrainLookup.Medium1;
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
        bloom.threshold.overrideState = true;
        bloom.intensity.overrideState = true;
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

    #region CALCULATE VALUES - VERSIÓN DINÁMICA MEJORADA
    private void CalculateTargetValues()
    {
        // Obtener valores CONTINUOS del jugador
        float stamina = player.GetStaminaNormalized(); // 0 = sin stamina, 1 = llena
        float fear = player.GetFearNormalized();       // 0 = sin miedo, 1 = pánico total
        bool isHidden = player.IsHidden;

        // Determinar velocidad de transición (más rápida si hay pánico)
        currentTransitionSpeed = (fear > 0.9f) ? fastTransitionSpeed : normalTransitionSpeed;

        // === CÁLCULO DINÁMICO DE VIÑETA ===
        CalculateDynamicVignette(stamina, fear, isHidden);
        CalculateDynamicColor(stamina, fear, isHidden);
        CalculateDynamicEffects(stamina, fear, isHidden);

        // Añadir pulso si está habilitado
        if (enablePulse)
        {
            ApplyDynamicPulse(stamina, fear);
        }
    }

    /// <summary>
    /// Calcula la viñeta de manera CONTINUA basándose en stamina y fear
    /// </summary>
    private void CalculateDynamicVignette(float stamina, float fear, bool hidden)
    {
        // === INTENSIDAD BASE ===
        float intensity = baseVignetteIntensity;
        Color color = baseVignetteColor;
        float smoothness = baseVignetteSmoothness;

        // === CONTRIBUCIÓN DE STAMINA (gradual e inversa) ===
        // Stamina baja = más viñeta
        float staminaContribution = 0f;
        if (stamina < 1f)
        {
            // Curvatura: empieza suave, se acelera al final
            float staminaFactor = 1f - stamina; // 0 = llena, 1 = vacía
            staminaFactor = Mathf.Pow(staminaFactor, 1.5f); // Curva exponencial
            staminaContribution = staminaFactor * (staminaDepleteIntensity - baseVignetteIntensity);
        }

        // === CONTRIBUCIÓN DE FEAR (gradual) ===
        // Fear alto = más viñeta
        float fearContribution = 0f;
        if (fear > 0f)
        {
            // Curva suave al principio, agresiva al final
            float fearFactor = Mathf.Pow(fear, 1.3f);
            fearContribution = fearFactor * (maxFearIntensity - baseVignetteIntensity);
        }

        // === COMBINAR CONTRIBUCIONES ===
        // No usar "else if" - SUMAR los efectos
        intensity = baseVignetteIntensity + staminaContribution + fearContribution;

        // === COLOR DINÁMICO ===
        // Mezclar colores basándose en qué está afectando más
        if (staminaContribution > 0f || fearContribution > 0f)
        {
            float totalContribution = staminaContribution + fearContribution;

            if (totalContribution > 0f)
            {
                float staminaWeight = staminaContribution / totalContribution;
                float fearWeight = fearContribution / totalContribution;

                // Mezcla ponderada de colores
                Color mixedColor = lowStaminaColor * staminaWeight + fearColor * fearWeight;

                // Si está en pánico extremo (>90%), mezclamos con color de pánico
                if (fear > 0.9f)
                {
                    float panicBlend = (fear - 0.9f) * 10f; // 0 a 1
                    mixedColor = Color.Lerp(mixedColor, panicColor, panicBlend);
                }

                // Aplicar la mezcla
                float mixStrength = Mathf.Min(totalContribution / maxFearIntensity, 1f);
                color = Color.Lerp(baseVignetteColor, mixedColor, mixStrength);
            }
        }

        // === ESTADO: ESCONDIDO ===
        if (hidden)
        {
            // Añadir intensidad extra cuando está escondido
            intensity = Mathf.Max(intensity, hiddenVignetteIntensity);
            color = Color.Lerp(color, hiddenVignetteColor, 0.5f);
            smoothness = 0.3f; // Más cerrado
        }

        // === APLICAR LÍMITES ===
        targetVignetteIntensity = Mathf.Clamp(intensity, 0f, 0.85f);
        targetVignetteColor = color;
        targetVignetteSmoothness = smoothness;
    }

    /// <summary>
    /// Calcula ajustes de color dinámicos
    /// </summary>
    private void CalculateDynamicColor(float stamina, float fear, bool hidden)
    {
        float saturation = baseSaturation;
        float contrast = baseContrast;
        float exposure = baseExposure;

        // === CONTRIBUCIÓN DE FEAR ===
        if (fear > 0f)
        {
            saturation = Mathf.Lerp(baseSaturation, maxFearSaturation, fear);
            contrast = Mathf.Lerp(baseContrast, maxFearContrast, fear);
            exposure = Mathf.Lerp(baseExposure, maxFearExposure, fear);
        }

        // === CONTRIBUCIÓN DE STAMINA ===
        // Solo afecta si la stamina está baja Y el miedo no es dominante
        if (stamina < 0.5f && fear < 0.5f)
        {
            float staminaFactor = 1f - (stamina * 2f); // 0.5 a 0 → 0 a 1
            saturation = Mathf.Lerp(saturation, lowStaminaSaturation, staminaFactor * 0.5f);
            contrast = Mathf.Lerp(contrast, lowStaminaContrast, staminaFactor * 0.3f);
        }

        // === SI ESTÁ ESCONDIDO Y ASUSTADO ===
        if (hidden && fear > 0.3f)
        {
            saturation = Mathf.Lerp(saturation, maxFearSaturation, fear * 0.7f);
        }

        targetSaturation = saturation;
        targetContrast = contrast;
        targetExposure = exposure;
    }

    /// <summary>
    /// Calcula efectos adicionales dinámicos
    /// </summary>
    private void CalculateDynamicEffects(float stamina, float fear, bool hidden)
    {
        // === CHROMATIC ABERRATION ===
        if (enableChromaticAberration && chromaticAberration != null)
        {
            // Aumenta con el miedo
            targetChromaticIntensity = Mathf.Lerp(
                baseChromaticIntensity,
                maxFearChromaticIntensity,
                Mathf.Pow(fear, 1.2f) // Curva suave
            );
        }

        // === LENS DISTORTION ===
        if (enableLensDistortion && lensDistortion != null)
        {
            if (hidden)
            {
                // Distorsión cuando está escondido, más fuerte si tiene miedo
                targetLensDistortion = Mathf.Lerp(
                    hiddenLensDistortion,
                    maxFearLensDistortion,
                    fear
                );
            }
            else
            {
                // Distorsión gradual con miedo extremo
                targetLensDistortion = Mathf.Lerp(
                    baseLensDistortion,
                    maxFearLensDistortion,
                    Mathf.Max(0f, fear - 0.7f) * 3.33f // Solo >70% fear
                );
            }
        }

        // === FILM GRAIN ===
        if (enableFilmGrain && filmGrain != null)
        {
            float grainFromFear = fear * maxFearGrainIntensity;
            float grainFromStamina = (1f - stamina) * lowStaminaGrainIntensity * 0.5f;

            targetGrainIntensity = baseGrainIntensity + grainFromFear + grainFromStamina;
            targetGrainIntensity = Mathf.Clamp(targetGrainIntensity, baseGrainIntensity, maxFearGrainIntensity);
        }

        // === BLOOM ===
        if (enableBloom && bloom != null)
        {
            if (fear > 0.9f)
            {
                // Bloom explosivo en pánico
                targetBloomIntensity = panicBloomIntensity;
            }
            else if (fear > 0.5f)
            {
                // Reducir bloom con miedo (visión más enfocada)
                targetBloomIntensity = Mathf.Lerp(baseBloomIntensity, fearBloomIntensity, (fear - 0.5f) * 2f);
            }
            else
            {
                targetBloomIntensity = baseBloomIntensity;
            }
        }
    }

    /// <summary>
    /// Pulso dinámico del corazón
    /// </summary>
    private void ApplyDynamicPulse(float stamina, float fear)
    {
        // Velocidad del pulso aumenta con miedo y baja stamina
        float stressFactor = Mathf.Max(fear, 1f - stamina);
        float pulseSpeed = Mathf.Lerp(normalPulseSpeed, maxPulseSpeed, stressFactor);
        float pulseAmount = Mathf.Lerp(basePulseIntensity, maxPulseIntensity, stressFactor);

        // Calcular onda sinusoidal
        float pulse = Mathf.Sin(pulseTimer * pulseSpeed * Mathf.PI * 2f) * pulseAmount;

        // Aplicar al viñeta
        targetVignetteIntensity += pulse;
    }
    #endregion

    #region APPLY EFFECTS
    private void ApplyEffects()
    {
        float deltaSpeed = currentTransitionSpeed * Time.deltaTime;

        // Aplicar Vignette
        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetVignetteIntensity, deltaSpeed);
            vignette.smoothness.value = Mathf.Lerp(vignette.smoothness.value, targetVignetteSmoothness, deltaSpeed);
            vignette.color.value = Color.Lerp(vignette.color.value, targetVignetteColor, deltaSpeed);
        }

        // Aplicar Color Adjustments
        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, targetSaturation, deltaSpeed);
            colorAdjustments.contrast.value = Mathf.Lerp(colorAdjustments.contrast.value, targetContrast, deltaSpeed);
            colorAdjustments.postExposure.value = Mathf.Lerp(colorAdjustments.postExposure.value, targetExposure, deltaSpeed);
        }

        // Aplicar Chromatic Aberration
        if (enableChromaticAberration && chromaticAberration != null)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity.value, targetChromaticIntensity, deltaSpeed);
        }

        // Aplicar Lens Distortion
        if (enableLensDistortion && lensDistortion != null)
        {
            lensDistortion.intensity.value = Mathf.Lerp(lensDistortion.intensity.value, targetLensDistortion, deltaSpeed);
        }

        // Aplicar Film Grain
        if (enableFilmGrain && filmGrain != null)
        {
            filmGrain.intensity.value = Mathf.Lerp(filmGrain.intensity.value, targetGrainIntensity, deltaSpeed);
        }

        // Aplicar Bloom
        if (enableBloom && bloom != null)
        {
            bloom.intensity.value = Mathf.Lerp(bloom.intensity.value, targetBloomIntensity, deltaSpeed);
        }
    }
    #endregion

    #region FLASH EFFECTS
    public void TriggerDamageFlash()
    {
        StartCoroutine(DamageFlashCoroutine());
    }

    public void TriggerDiscoveryFlash()
    {
        StartCoroutine(DiscoveryFlashCoroutine());
    }

    public void TriggerDeathFlash()
    {
        StartCoroutine(DeathFlashCoroutine());
    }

    private IEnumerator DamageFlashCoroutine()
    {
        isFlashing = true;

        // Flash rojo intenso
        targetVignetteIntensity = 0.9f;
        targetVignetteColor = Color.red;
        if (enableChromaticAberration) targetChromaticIntensity = damageChromaticIntensity;

        yield return new WaitForSeconds(0.1f);

        isFlashing = false;
    }

    private IEnumerator DiscoveryFlashCoroutine()
    {
        isFlashing = true;

        targetVignetteIntensity = 0.85f;
        targetVignetteColor = new Color(1f, 0.5f, 0f); // Naranja
        if (enableChromaticAberration) targetChromaticIntensity = damageChromaticIntensity * 0.6f;

        yield return new WaitForSeconds(0.2f);

        isFlashing = false;
    }

    private IEnumerator DeathFlashCoroutine()
    {
        isFlashing = true;

        // Flash blanco cegador que se vuelve negro
        targetVignetteIntensity = 1f;
        targetVignetteColor = Color.white;
        currentTransitionSpeed = flashTransitionSpeed;

        yield return new WaitForSeconds(0.15f);

        targetVignetteColor = Color.black;

        yield return new WaitForSeconds(0.3f);

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
            colorAdjustments.saturation.value = baseSaturation;
            colorAdjustments.contrast.value = baseContrast;
            colorAdjustments.postExposure.value = baseExposure;
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = baseChromaticIntensity;
        }

        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = baseLensDistortion;
        }

        if (filmGrain != null)
        {
            filmGrain.intensity.value = baseGrainIntensity;
        }

        if (bloom != null)
        {
            bloom.intensity.value = baseBloomIntensity;
        }
    }
    #endregion
}