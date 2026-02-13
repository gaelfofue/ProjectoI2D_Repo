using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Zona que activa persecución con buildup de tensión.
/// Shake progresivo: suave en tensión, fuerte al revelar, medio en persecución.
/// </summary>
public class EnemyTriggerZone : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [SerializeField] private SimpleEnemy enemy;

    [Header("=== TENSION BUILDUP ===")]
    [SerializeField] private float tensionDuration = 4f;

    [Header("=== SHAKE INTENSITIES ===")]
    [Tooltip("Shake durante la fase de tensión (antes de ver al enemigo)")]
    [SerializeField] private float tensionShakeStart = 0.05f;
    [SerializeField] private float tensionShakeEnd = 0.2f;
    [Tooltip("Shake fuerte cuando el enemigo aparece")]
    [SerializeField] private float revealShakeIntensity = 1.5f;
    [SerializeField] private float revealShakeDuration = 0.5f;
    [Tooltip("Shake durante la persecución")]
    [SerializeField] private float chaseShakeIntensity = 0.4f;

    [Header("=== TENSION AUDIO ===")]
    [SerializeField] private AudioSource tensionAudioSource;
    [SerializeField] private AudioClip[] approachingSounds;
    [SerializeField] private AudioClip tensionBuildupLoop;
    [SerializeField] private AudioClip monsterRevealSound;
    [SerializeField] private float maxTensionVolume = 0.8f;

    [Header("=== CHASE SETTINGS ===")]
    [SerializeField] private float chaseDuration = 8f;
    [SerializeField] private Transform enemyExitPoint;
    [SerializeField] private float enemyExitSpeed = 6f;

    [Header("=== AUDIO CHASE ===")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip chaseStartSound;
    [SerializeField] private AudioClip enemyLeaveSound;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebug = true;

    // Estado
    private bool chaseActivated = false;
    private bool tensionStarted = false;
    private bool enemyLeft = false;
    private bool sequenceComplete = false;
    private PlayerController playerController;
    private Rigidbody2D enemyRb;
    private SpriteRenderer[] enemyRenderers;
    private string debugStatus = "Esperando al player...";

    // Propiedad pública para LockedDoor
    public bool IsChaseActive => chaseActivated;

    private void Start()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            playerController = playerGO.GetComponent<PlayerController>();

        if (enemy != null)
        {
            enemy.enabled = false;
            enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null) enemyRb.linearVelocity = Vector2.zero;

            enemyRenderers = enemy.GetComponentsInChildren<SpriteRenderer>(true);
            SetEnemyVisible(false);
        }

        if (tensionAudioSource == null)
        {
            tensionAudioSource = gameObject.AddComponent<AudioSource>();
            tensionAudioSource.playOnAwake = false;
            tensionAudioSource.loop = false;
        }

        Debug.Log("[TriggerZone] ✅ Iniciado - Enemigo dormido e invisible");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (tensionStarted) return;

        tensionStarted = true;
        debugStatus = "Tensión...";
        Debug.Log("[TriggerZone] 😰 Player entró - Iniciando tensión");

        StartCoroutine(TensionAndChaseSequence());
    }

    private void SetEnemyVisible(bool visible)
    {
        if (enemyRenderers == null) return;

        foreach (var sr in enemyRenderers)
        {
            if (sr != null)
            {
                Color c = sr.color;
                c.a = visible ? 1f : 0f;
                sr.color = c;
            }
        }
    }

    private IEnumerator TensionAndChaseSequence()
    {
        Debug.Log("[TriggerZone] === FASE 1: TENSIÓN ===");

        // Loop de tensión audio
        if (tensionAudioSource != null && tensionBuildupLoop != null)
        {
            tensionAudioSource.clip = tensionBuildupLoop;
            tensionAudioSource.loop = true;
            tensionAudioSource.volume = 0f;
            tensionAudioSource.Play();
        }

        // Buildup gradual
        float elapsed = 0f;
        int soundIndex = 0;

        while (elapsed < tensionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / tensionDuration;

            debugStatus = $"Tensión: {progress * 100:F0}%";

            // === SHAKE PROGRESIVO DURANTE TENSIÓN ===
            if (CinemachineShake.Instance != null)
            {
                float currentShake = Mathf.Lerp(tensionShakeStart, tensionShakeEnd, progress);
                CinemachineShake.Instance.StartConstantShake(currentShake);
            }

            // Subir volumen gradualmente
            if (tensionAudioSource != null && tensionBuildupLoop != null)
            {
                tensionAudioSource.volume = Mathf.Lerp(0f, maxTensionVolume, progress);
            }

            // Reproducir sonidos de aproximación
            if (approachingSounds != null && approachingSounds.Length > 0)
            {
                float interval = tensionDuration / (approachingSounds.Length + 1);
                int expectedIndex = Mathf.FloorToInt(elapsed / interval);

                if (expectedIndex > soundIndex && soundIndex < approachingSounds.Length)
                {
                    soundIndex = expectedIndex;
                    if (audioSource != null)
                    {
                        int clipIndex = Mathf.Min(soundIndex - 1, approachingSounds.Length - 1);
                        audioSource.volume = 0.3f + (progress * 0.5f);
                        audioSource.PlayOneShot(approachingSounds[clipIndex]);
                    }
                }
            }

            yield return null;
        }

        // === FASE 2: MONSTER REVEAL ===
        Debug.Log("[TriggerZone] === FASE 2: ENEMIGO APARECE ===");
        debugStatus = "¡¡¡MONSTRUO!!!";

        // === SHAKE FUERTE AL REVELAR ===
        if (CinemachineShake.Instance != null)
        {
            CinemachineShake.Instance.ShakeOnce(revealShakeIntensity, revealShakeDuration);
        }

        // Parar loop de tensión
        if (tensionAudioSource != null)
            tensionAudioSource.Stop();

        // Sonido de reveal
        if (audioSource != null && monsterRevealSound != null)
        {
            audioSource.volume = 1f;
            audioSource.PlayOneShot(monsterRevealSound);
        }

        // Hacer enemigo VISIBLE
        SetEnemyVisible(true);

        // Esperar a que termine el shake del reveal
        yield return new WaitForSeconds(revealShakeDuration);

        // Activar persecución
        StartChase();

        // === SHAKE MEDIO DURANTE PERSECUCIÓN ===
        if (CinemachineShake.Instance != null)
        {
            CinemachineShake.Instance.StartConstantShake(chaseShakeIntensity);
        }
    }

    private void StartChase()
    {
        chaseActivated = true;
        Debug.Log("[TriggerZone] 🏃 ¡PERSECUCIÓN ACTIVA!");

        if (enemy != null)
        {
            enemy.enabled = true;
            Debug.Log("[TriggerZone] 👹 Enemigo ACTIVADO y VISIBLE");
        }

        if (audioSource != null && chaseStartSound != null)
            audioSource.PlayOneShot(chaseStartSound);

        StartCoroutine(ChaseTimeout());
    }

    private IEnumerator ChaseTimeout()
    {
        yield return new WaitForSeconds(chaseDuration);

        if (!sequenceComplete)
        {
            Debug.Log("[TriggerZone] ⏰ Timeout - Enemigo se va");
            StartCoroutine(EnemyLeaves());
        }
    }

    private IEnumerator EnemyLeaves()
    {
        if (enemyLeft) yield break;
        enemyLeft = true;

        debugStatus = "Enemigo yéndose...";

        // Parar shake
        if (CinemachineShake.Instance != null)
            CinemachineShake.Instance.StopShake();

        if (enemy != null)
            enemy.enabled = false;

        if (audioSource != null && enemyLeaveSound != null)
            audioSource.PlayOneShot(enemyLeaveSound);

        if (enemyRb != null && enemyExitPoint != null)
        {
            Vector2 dir = ((Vector2)enemyExitPoint.position - (Vector2)enemy.transform.position).normalized;
            float elapsed = 0f;

            while (elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                enemyRb.linearVelocity = dir * enemyExitSpeed;

                if (Vector2.Distance(enemy.transform.position, enemyExitPoint.position) < 1f)
                    break;

                yield return null;
            }

            enemyRb.linearVelocity = Vector2.zero;
        }

        if (enemy != null)
            enemy.gameObject.SetActive(false);

        debugStatus = "Enemigo se fue";
        sequenceComplete = true;
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>=== TRIGGER ZONE ===</b>");
        GUILayout.Label($"Estado: <color=yellow>{debugStatus}</color>");
        GUILayout.Label($"Tensión: {(tensionStarted ? "✅" : "❌")}");
        GUILayout.Label($"Chase Activo: {(chaseActivated ? "<color=red>✅ SÍ</color>" : "❌ NO")}");
        GUILayout.Label($"Enemigo fue: {(enemyLeft ? "✅" : "❌")}");
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = chaseActivated ? Color.red : new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        if (enemyExitPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(enemyExitPoint.position, 0.5f);
        }
    }
}