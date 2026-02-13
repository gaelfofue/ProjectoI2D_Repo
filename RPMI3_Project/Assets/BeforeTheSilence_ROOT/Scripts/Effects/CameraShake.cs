using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

/// <summary>
/// Camera shake compatible con Cinemachine 3.x
/// </summary>
public class CinemachineShake : MonoBehaviour
{
    public static CinemachineShake Instance;

    [Header("References")]
    [SerializeField] private CinemachineCamera virtualCamera;

    private CinemachineBasicMultiChannelPerlin noise;
    private float shakeTimer = 0f;
    private float shakeTimerTotal = 0f;
    private float startingIntensity = 0f;
    private bool isConstantShake = false;

    private void Awake()
    {
        Instance = this;

        // Buscar cámara virtual si no está asignada
        if (virtualCamera == null)
            virtualCamera = FindFirstObjectByType<CinemachineCamera>();

        // Obtener el componente de ruido
        if (virtualCamera != null)
        {
            noise = virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();

            if (noise == null)
            {
                Debug.LogWarning("[CinemachineShake] ⚠️ La cámara no tiene CinemachineBasicMultiChannelPerlin. Añádelo manualmente.");
            }
            else
            {
                noise.AmplitudeGain = 0f;
                Debug.Log("[CinemachineShake] ✅ Inicializado correctamente");
            }
        }
        else
        {
            Debug.LogError("[CinemachineShake] ❌ No se encontró CinemachineCamera");
        }
    }

    private void Update()
    {
        if (noise == null) return;
        if (isConstantShake) return;

        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;

            // Reducir intensidad gradualmente
            float progress = shakeTimer / shakeTimerTotal;
            noise.AmplitudeGain = Mathf.Lerp(0f, startingIntensity, progress);

            if (shakeTimer <= 0f)
            {
                noise.AmplitudeGain = 0f;
            }
        }
    }

    /// <summary>
    /// Shake que sube de intensidad gradualmente
    /// </summary>
    public void StartBuildUpShake(float duration, float maxIntensity)
    {
        if (noise == null) return;

        isConstantShake = false;
        StopAllCoroutines();
        StartCoroutine(BuildUpShakeRoutine(duration, maxIntensity));
    }

    private IEnumerator BuildUpShakeRoutine(float duration, float maxIntensity)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            noise.AmplitudeGain = Mathf.Lerp(0f, maxIntensity, progress);
            yield return null;
        }

        noise.AmplitudeGain = maxIntensity;
    }

    /// <summary>
    /// Shake instantáneo que se desvanece
    /// </summary>
    public void ShakeOnce(float intensity, float duration)
    {
        if (noise == null) return;

        isConstantShake = false;
        startingIntensity = intensity;
        shakeTimerTotal = duration;
        shakeTimer = duration;
        noise.AmplitudeGain = intensity;
    }

    /// <summary>
    /// Shake constante hasta llamar StopShake()
    /// </summary>
    public void StartConstantShake(float intensity)
    {
        if (noise == null) return;

        isConstantShake = true;
        noise.AmplitudeGain = intensity;
    }

    /// <summary>
    /// Detener todo shake
    /// </summary>
    public void StopShake()
    {
        if (noise == null) return;

        StopAllCoroutines();
        isConstantShake = false;
        shakeTimer = 0f;
        noise.AmplitudeGain = 0f;
    }
}