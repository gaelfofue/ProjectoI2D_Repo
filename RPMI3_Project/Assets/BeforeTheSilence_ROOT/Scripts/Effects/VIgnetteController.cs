using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// Controla el efecto de viñeta basado en stamina y miedo del jugador.
public class VignetteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private PlayerController player;

    [Header("Stamina Vignette")]
    [SerializeField] private float minVignetteIntensity = 0.1f;
    [SerializeField] private float maxVignetteIntensity = 0.6f;
    [SerializeField] private Color normalVignetteColor = Color.black;
    [SerializeField] private Color exhaustedVignetteColor = new Color(0.3f, 0, 0);

    [Header("Fear Vignette")]
    [SerializeField] private float fearVignetteMultiplier = 1.5f;
    [SerializeField] private Color fearVignetteColor = new Color(0.1f, 0, 0.2f);
    [SerializeField] private Color panicVignetteColor = new Color(0.4f, 0, 0);

    [Header("Pulse Effect")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 0.1f;
    [SerializeField] private float panicPulseSpeed = 4f;

    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 3f;

    private Vignette vignette;
    private float targetIntensity;
    private Color targetColor;

    private void Start()
    {
        // Buscar el Volume si no está asignado
        if (postProcessVolume == null)
        {
            postProcessVolume = FindFirstObjectByType<Volume>();
        }

        // Obtener el componente Vignette
        if (postProcessVolume != null &&
            postProcessVolume.profile.TryGet(out Vignette v))
        {
            vignette = v;
            vignette.active = true;
        }
        else
        {
            Debug.LogWarning("No se encontró Vignette en el Volume");
            enabled = false;
            return;
        }

        // Buscar player si no está asignado
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player == null)
        {
            Debug.LogWarning("No se encontró PlayerController para VignetteController");
        }
    }

    private void Update()
    {
        if (vignette == null || player == null) return;

        UpdateVignetteValues();
        ApplyVignette();
    }

    private void UpdateVignetteValues()
    {
        float staminaPercent = player.GetStaminaNormalized();
        float fearPercent = player.GetFearNormalized();

        // Calcular intensidad base (inversa de stamina - menos stamina = más viñeta)
        float staminaIntensity = Mathf.Lerp(maxVignetteIntensity, minVignetteIntensity, staminaPercent);

        // Añadir intensidad por miedo
        float fearIntensity = fearPercent * fearVignetteMultiplier * maxVignetteIntensity;

        // Usar el mayor
        targetIntensity = Mathf.Max(staminaIntensity, fearIntensity);

        // Añadir pulso si está bajo de stamina o tiene miedo
        if (enablePulse)
        {
            bool shouldPulse = staminaPercent < 0.3f || fearPercent > 0.5f;

            if (shouldPulse)
            {
                float currentPulseSpeed = player.IsPanicking ? panicPulseSpeed : pulseSpeed;
                float pulse = Mathf.Sin(Time.time * currentPulseSpeed) * pulseIntensity;

                // Pulso más intenso en pánico
                if (player.IsPanicking)
                {
                    pulse *= 2f;
                }

                targetIntensity += pulse;
            }
        }

        targetIntensity = Mathf.Clamp01(targetIntensity);

        // Calcular color objetivo
        if (player.IsPanicking)
        {
            targetColor = panicVignetteColor;
        }
        else if (fearPercent > 0.5f)
        {
            targetColor = Color.Lerp(normalVignetteColor, fearVignetteColor, fearPercent);
        }
        else if (staminaPercent < 0.3f)
        {
            targetColor = Color.Lerp(normalVignetteColor, exhaustedVignetteColor, 1f - staminaPercent);
        }
        else
        {
            targetColor = normalVignetteColor;
        }
    }

    private void ApplyVignette()
    {
        // Transición suave
        vignette.intensity.value = Mathf.Lerp(
            vignette.intensity.value,
            targetIntensity,
            Time.deltaTime * transitionSpeed
        );

        vignette.color.value = Color.Lerp(
            vignette.color.value,
            targetColor,
            Time.deltaTime * transitionSpeed
        );
    }

    #region SPECIAL EFFECTS
    /// Flash de viñeta para eventos especiales (muerte, daño, etc.)
    public void FlashVignette(Color color, float intensity, float duration)
    {
        StartCoroutine(FlashRoutine(color, intensity, duration));
    }

    private IEnumerator FlashRoutine(Color color, float intensity, float duration)
    {
        if (vignette == null) yield break;

        Color originalColor = vignette.color.value;
        float originalIntensity = vignette.intensity.value;

        // Flash in
        vignette.color.value = color;
        vignette.intensity.value = intensity;

        yield return new WaitForSecondsRealtime(duration * 0.3f);

        // Fade out
        float elapsed = 0f;
        float fadeTime = duration * 0.7f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeTime;

            vignette.color.value = Color.Lerp(color, originalColor, t);
            vignette.intensity.value = Mathf.Lerp(intensity, originalIntensity, t);

            yield return null;
        }
    }

    /// Flash rojo para daño
    public void DamageFlash()
    {
        FlashVignette(Color.red, 0.8f, 0.3f);
    }

    /// Flash oscuro para muerte
    public void DeathFlash()
    {
        FlashVignette(Color.black, 1f, 1f);
    }
    #endregion

    private void OnDestroy()
    {
        // Restaurar viñeta al destruirse
        if (vignette != null)
        {
            vignette.intensity.value = minVignetteIntensity;
            vignette.color.value = normalVignetteColor;
        }
    }
}
