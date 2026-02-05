using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controla la iluminación del pasillo basada en la posición X del jugador.
/// Coloca este script en un GameObject vacío en la escena.
/// </summary>
public class CorridorLightingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Light2D globalLight;

    [Header("Corridor Settings")]
    [SerializeField] private float corridorStartX = 0f;
    [SerializeField] private float corridorEndX = 100f;
    [SerializeField] private float darkZoneStartX = 20f;
    [SerializeField] private float darkZoneEndX = 80f;

    [Header("Light Settings")]
    [SerializeField] private float litIntensity = 1f;
    [SerializeField] private float darkIntensity = 0.05f;
    [SerializeField] private float transitionDistance = 10f;

    [Header("Color Settings")]
    [SerializeField] private Color litColor = Color.white;
    [SerializeField] private Color darkColor = new Color(0.1f, 0.1f, 0.2f);

    [Header("Events")]
    [SerializeField] private UnityEngine.Events.UnityEvent OnEnterDarkness;
    [SerializeField] private UnityEngine.Events.UnityEvent OnExitDarkness;

    private bool isInDarkness = false;
    private float targetIntensity;
    private Color targetColor;

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (globalLight == null)
            globalLight = FindFirstObjectByType<Light2D>();

        targetIntensity = litIntensity;
        targetColor = litColor;
    }

    private void Update()
    {
        if (player == null || globalLight == null) return;

        CalculateLighting();
        ApplyLighting();
    }

    private void CalculateLighting()
    {
        float playerX = player.position.x;

        // Antes de la zona oscura
        if (playerX < darkZoneStartX)
        {
            float t = Mathf.InverseLerp(darkZoneStartX - transitionDistance, darkZoneStartX, playerX);
            targetIntensity = Mathf.Lerp(litIntensity, darkIntensity, t);
            targetColor = Color.Lerp(litColor, darkColor, t);

            if (isInDarkness)
            {
                isInDarkness = false;
                OnExitDarkness?.Invoke();
            }
        }
        // En la zona oscura
        else if (playerX >= darkZoneStartX && playerX <= darkZoneEndX)
        {
            targetIntensity = darkIntensity;
            targetColor = darkColor;

            if (!isInDarkness)
            {
                isInDarkness = true;
                OnEnterDarkness?.Invoke();
            }
        }
        // Después de la zona oscura
        else if (playerX > darkZoneEndX)
        {
            float t = Mathf.InverseLerp(darkZoneEndX, darkZoneEndX + transitionDistance, playerX);
            targetIntensity = Mathf.Lerp(darkIntensity, litIntensity, t);
            targetColor = Color.Lerp(darkColor, litColor, t);

            if (isInDarkness)
            {
                isInDarkness = false;
                OnExitDarkness?.Invoke();
            }
        }
    }

    private void ApplyLighting()
    {
        globalLight.intensity = Mathf.Lerp(globalLight.intensity, targetIntensity, Time.deltaTime * 3f);
        globalLight.color = Color.Lerp(globalLight.color, targetColor, Time.deltaTime * 3f);
    }

    public bool IsInDarkness => isInDarkness;
    public float GetDarknessProgress()
    {
        if (player == null) return 0f;
        return Mathf.InverseLerp(darkZoneStartX, darkZoneEndX, player.position.x);
    }

    private void OnDrawGizmosSelected()
    {
        // Zona iluminada inicial
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(corridorStartX, -5, 0), new Vector3(corridorStartX, 5, 0));
        Gizmos.DrawLine(new Vector3(darkZoneStartX, -5, 0), new Vector3(darkZoneStartX, 5, 0));

        // Zona oscura
        Gizmos.color = Color.black;
        Gizmos.DrawCube(new Vector3((darkZoneStartX + darkZoneEndX) / 2, 0, 0),
                        new Vector3(darkZoneEndX - darkZoneStartX, 10, 1));

        // Zona iluminada final
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(darkZoneEndX, -5, 0), new Vector3(darkZoneEndX, 5, 0));
        Gizmos.DrawLine(new Vector3(corridorEndX, -5, 0), new Vector3(corridorEndX, 5, 0));
    }
}