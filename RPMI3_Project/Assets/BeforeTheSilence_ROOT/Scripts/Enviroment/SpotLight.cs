using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Zona de luz focal donde ocurre el evento del monstruo.
/// </summary>
public class SpotlightZone : MonoBehaviour
{
    [Header("Light Reference")]
    [SerializeField] private Light2D spotLight;

    [Header("Zone Settings")]
    [SerializeField] private float activationRadius = 3f;
    [SerializeField] private float lightRadius = 5f;

    [Header("Light Behavior")]
    [SerializeField] private float normalIntensity = 0.8f;
    [SerializeField] private float flickerIntensity = 0.3f;
    [SerializeField] private bool flickerOnMonsterNear = true;

    [Header("Events")]
    [SerializeField] private UnityEngine.Events.UnityEvent OnPlayerEnterZone;
    [SerializeField] private UnityEngine.Events.UnityEvent OnPlayerExitZone;

    private Transform player;
    private bool playerInZone = false;
    private bool isFlickering = false;
    private float flickerTimer = 0f;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (spotLight == null)
            spotLight = GetComponentInChildren<Light2D>();
    }

    private void Update()
    {
        if (player == null || spotLight == null) return;

        CheckPlayerProximity();

        if (isFlickering)
            UpdateFlicker();
    }

    private void CheckPlayerProximity()
    {
        float distance = Vector2.Distance(transform.position, player.position);
        bool inZone = distance <= activationRadius;

        if (inZone && !playerInZone)
        {
            playerInZone = true;
            OnPlayerEnterZone?.Invoke();
        }
        else if (!inZone && playerInZone)
        {
            playerInZone = false;
            OnPlayerExitZone?.Invoke();
        }
    }

    private void UpdateFlicker()
    {
        flickerTimer += Time.deltaTime * 15f;
        float noise = Mathf.PerlinNoise(flickerTimer, 0f);
        spotLight.intensity = Mathf.Lerp(flickerIntensity, normalIntensity, noise);
    }

    public void StartFlicker()
    {
        isFlickering = true;
    }

    public void StopFlicker()
    {
        isFlickering = false;
        spotLight.intensity = normalIntensity;
    }

    public void TurnOff()
    {
        spotLight.intensity = 0f;
        isFlickering = false;
    }

    public void TurnOn()
    {
        spotLight.intensity = normalIntensity;
    }

    public bool IsPlayerInZone => playerInZone;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, lightRadius);
    }
}