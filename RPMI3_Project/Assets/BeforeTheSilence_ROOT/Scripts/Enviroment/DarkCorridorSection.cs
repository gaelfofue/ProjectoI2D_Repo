using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class DarkCorridorSection : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Light2D spotLight;
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private VignetteController vignetteController;

    [Header("CORRIDOR ZONES")]
    [SerializeField] private float lightZoneEnd = 10f;
    [SerializeField] private float darkZoneStart = 20f;
    [SerializeField] private float spotlightX = 75f;
    [SerializeField] private float darkZoneEnd = 90f;

    [Header("LIGHTING")]
    [SerializeField] private float litIntensity = 1f;
    [SerializeField] private float darkIntensity = 0.02f;
    [SerializeField] private Color litColor = Color.white;
    [SerializeField] private Color darkColor = new Color(0.05f, 0.05f, 0.1f);

    [Header("MONSTER PARALLAX")]
    [SerializeField] private Transform monsterSpawnPoint;
    [SerializeField] private Transform monsterEndPoint;
    [SerializeField] private float monsterSpeed = 10f;
    [SerializeField] private float monsterScale = 1.5f;

    [Header("MONSTER VISUAL")]
    [SerializeField] private Color silhouetteColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private int monsterSortingOrder = 100;
    [SerializeField] private string monsterSortingLayer = "Default";

    [Header("AUDIO")]
    [SerializeField] private AudioSource monsterSource;
    [SerializeField] private AudioClip monsterPassSound;
    [SerializeField] private AudioClip distantRoar;

    [Header("TRANSITION")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeOutDuration = 2f;
    [SerializeField] private float waitAfterMonster = 1.5f;
    [SerializeField] private string nextSceneName = "SCN_Casa_Piso2_Normal";

    [Header("DEBUG")]
    [SerializeField] private bool showDebugGUI = true;

    // Estado
    private bool isInDarkZone = false;
    private bool monsterEventTriggered = false;
    private bool spotlightReached = false;
    private bool playerLocked = false;
    private GameObject monsterInstance;
    private string debugStatus = "Esperando...";

    private void Start()
    {
        Debug.Log("[DarkCorridor] ========== START ==========");

        // Buscar Player
        if (player == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                player = playerGO.transform;
                playerController = playerGO.GetComponent<PlayerController>();
            }
        }
        else if (playerController == null)
        {
            playerController = player.GetComponent<PlayerController>();
        }

        if (vignetteController == null)
            vignetteController = FindFirstObjectByType<VignetteController>();

        // Validar referencias críticas
        if (monsterPrefab == null)
            Debug.LogError("[DarkCorridor] ❌ MONSTER PREFAB NO ASIGNADO!");
        else
            Debug.Log($"[DarkCorridor] ✅ Monster Prefab: {monsterPrefab.name}");

        if (monsterSpawnPoint == null)
            Debug.LogError("[DarkCorridor] ❌ SPAWN POINT NO ASIGNADO!");
        if (monsterEndPoint == null)
            Debug.LogError("[DarkCorridor] ❌ END POINT NO ASIGNADO!");
        if (fadeImage == null)
            Debug.LogWarning("[DarkCorridor] ⚠️ FADE IMAGE NO ASIGNADA - No habrá transición fade");

        // Asegurar que el fade empiece transparente
        if (fadeImage != null)
            fadeImage.color = new Color(0, 0, 0, 0);

        Debug.Log($"[DarkCorridor] Silhouette Color: {silhouetteColor}");
        Debug.Log($"[DarkCorridor] Next Scene: {nextSceneName}");
        Debug.Log("[DarkCorridor] ==============================");
    }

    private void Update()
    {
        if (player == null) return;
        if (playerLocked) return; // No actualizar si el player está bloqueado

        float playerX = player.position.x;

        UpdateLighting(playerX);
        CheckSpotlightTrigger(playerX);
    }

    #region PLAYER LOCK
    private void LockPlayer()
    {
        playerLocked = true;
        debugStatus = "Player BLOQUEADO";
        Debug.Log("[DarkCorridor] 🔒 Player bloqueado");

        // Método 1: Desactivar PlayerController
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Método 2: Congelar Rigidbody
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }
    }

    private void UnlockPlayer()
    {
        playerLocked = false;
        Debug.Log("[DarkCorridor] 🔓 Player desbloqueado");

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }
    #endregion

    #region LIGHTING
    private void UpdateLighting(float playerX)
    {
        if (globalLight == null) return;

        float targetIntensity;
        Color targetColor;

        if (playerX < lightZoneEnd)
        {
            targetIntensity = litIntensity;
            targetColor = litColor;
            isInDarkZone = false;
        }
        else if (playerX < darkZoneStart)
        {
            float t = Mathf.InverseLerp(lightZoneEnd, darkZoneStart, playerX);
            targetIntensity = Mathf.Lerp(litIntensity, darkIntensity, t);
            targetColor = Color.Lerp(litColor, darkColor, t);
            isInDarkZone = false;
        }
        else if (playerX < darkZoneEnd)
        {
            targetIntensity = darkIntensity;
            targetColor = darkColor;

            if (!isInDarkZone)
            {
                isInDarkZone = true;
                Debug.Log("[DarkCorridor] Entrando en oscuridad");
            }
        }
        else
        {
            float t = Mathf.InverseLerp(darkZoneEnd, darkZoneEnd + 10f, playerX);
            targetIntensity = Mathf.Lerp(darkIntensity, litIntensity, t);
            targetColor = Color.Lerp(darkColor, litColor, t);
            isInDarkZone = false;
        }

        globalLight.intensity = Mathf.Lerp(globalLight.intensity, targetIntensity, Time.deltaTime * 2f);
        globalLight.color = Color.Lerp(globalLight.color, targetColor, Time.deltaTime * 2f);
    }
    #endregion

    #region MONSTER EVENT
    private void CheckSpotlightTrigger(float playerX)
    {
        if (spotlightReached) return;

        if (Mathf.Abs(playerX - spotlightX) < 3f)
        {
            spotlightReached = true;
            debugStatus = "Spotlight alcanzado!";
            Debug.Log($"[DarkCorridor] ✅ Spotlight alcanzado en X={playerX:F1}");

            // ===== BLOQUEAR PLAYER INMEDIATAMENTE =====
            LockPlayer();

            StartCoroutine(FullEventSequence());
        }
    }

    private IEnumerator FullEventSequence()
    {
        Debug.Log("[DarkCorridor] ========== SECUENCIA COMPLETA ==========");

        // 1. Encender spotlight
        yield return StartCoroutine(TurnOnSpotlight());

        // 2. Pequeña pausa
        yield return new WaitForSeconds(1f);

        // 3. Monstruo pasa
        monsterEventTriggered = true;
        yield return StartCoroutine(MonsterParallaxEvent());

        // 4. Esperar después del monstruo
        debugStatus = "Esperando...";
        yield return new WaitForSeconds(waitAfterMonster);

        // 5. Fade a negro
        debugStatus = "Fade out...";
        yield return StartCoroutine(FadeToBlack());

        // 6. Cargar siguiente escena
        debugStatus = "Cargando escena...";
        Debug.Log($"[DarkCorridor] 🎬 Cargando: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator TurnOnSpotlight()
    {
        if (spotLight == null) yield break;

        debugStatus = "Encendiendo luz...";
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            spotLight.intensity = Mathf.Lerp(0f, 0.8f, elapsed);
            yield return null;
        }

        Debug.Log("[DarkCorridor] 💡 Spotlight encendido");
    }

    private IEnumerator MonsterParallaxEvent()
    {
        Debug.Log("[DarkCorridor] 👹 MONSTRUO APARECIENDO");
        debugStatus = "¡MONSTRUO!";

        if (monsterPrefab == null || monsterSpawnPoint == null || monsterEndPoint == null)
        {
            Debug.LogError("[DarkCorridor] ❌ Faltan referencias del monstruo!");
            yield break;
        }

        // Flicker del spotlight
        if (spotLight != null)
            StartCoroutine(FlickerLight(spotLight, 2f));

        // Crear monstruo
        Vector3 spawnPos = monsterSpawnPoint.position;
        Debug.Log($"[DarkCorridor] Spawning en: {spawnPos}");

        monsterInstance = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
        monsterInstance.name = "MonsterSilhouette";

        // Configurar visibilidad
        ConfigureMonsterVisibility(monsterInstance);

        // Escala
        monsterInstance.transform.localScale = Vector3.one * monsterScale;

        // Dirección
        bool movingRight = monsterEndPoint.position.x > monsterSpawnPoint.position.x;
        SetMonsterDirection(monsterInstance, movingRight);

        // Sonido de paso
        if (monsterSource != null && monsterPassSound != null)
        {
            monsterSource.volume = 0.8f;
            monsterSource.PlayOneShot(monsterPassSound);
        }

        // Mover el monstruo
        Vector3 startPos = monsterInstance.transform.position;
        Vector3 endPos = monsterEndPoint.position;
        float distance = Vector2.Distance(startPos, endPos);
        float duration = distance / monsterSpeed;

        Debug.Log($"[DarkCorridor] Moviendo por {duration:F2} segundos");

        float elapsed = 0f;
        while (elapsed < duration && monsterInstance != null)
        {
            elapsed += Time.deltaTime;
            monsterInstance.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        // Destruir monstruo
        if (monsterInstance != null)
        {
            Debug.Log("[DarkCorridor] Destruyendo monstruo");
            Destroy(monsterInstance);
        }

        debugStatus = "Monstruo pasó";

        // Efecto de pánico en vignette
        if (vignetteController != null)
            vignetteController.PanicFlash();

        // Rugido distante
        yield return new WaitForSeconds(0.3f);
        if (monsterSource != null && distantRoar != null)
        {
            monsterSource.volume = 0.5f;
            monsterSource.PlayOneShot(distantRoar);
        }

        Debug.Log("[DarkCorridor] 👹 Monstruo completado");
    }

    private IEnumerator FadeToBlack()
    {
        Debug.Log("[DarkCorridor] ⬛ Iniciando fade...");

        if (fadeImage == null)
        {
            Debug.LogWarning("[DarkCorridor] No hay fadeImage, saltando fade");
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // Asegurar que quede completamente negro
        fadeImage.color = Color.black;
        Debug.Log("[DarkCorridor] ⬛ Fade completado");
    }

    private void ConfigureMonsterVisibility(GameObject monster)
    {
        Debug.Log("[DarkCorridor] Configurando visibilidad...");

        // 1. Desactivar TODOS los colliders
        Collider2D[] colliders = monster.GetComponentsInChildren<Collider2D>(true);
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
        Debug.Log($"[DarkCorridor] - {colliders.Length} colliders desactivados");

        // 2. Desactivar Rigidbody
        Rigidbody2D rb = monster.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false;
        }

        // 3. Desactivar script de enemigo
        SimpleEnemy enemy = monster.GetComponent<SimpleEnemy>();
        if (enemy != null)
        {
            enemy.enabled = false;
            Debug.Log("[DarkCorridor] - SimpleEnemy desactivado");
        }

        // 4. Configurar TODOS los sprites
        SpriteRenderer[] renderers = monster.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            sr.color = silhouetteColor;
            sr.sortingLayerName = monsterSortingLayer;
            sr.sortingOrder = monsterSortingOrder;
        }
        Debug.Log($"[DarkCorridor] - {renderers.Length} sprites configurados");
    }

    private void SetMonsterDirection(GameObject monster, bool movingRight)
    {
        Vector3 scale = monster.transform.localScale;
        scale.x = movingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        monster.transform.localScale = scale;
    }

    private IEnumerator FlickerLight(Light2D light, float duration)
    {
        float originalIntensity = light.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float noise = Mathf.PerlinNoise(elapsed * 20f, 0f);
            light.intensity = Mathf.Lerp(0.1f, originalIntensity, noise);
            yield return null;
        }

        light.intensity = originalIntensity;
    }
    #endregion

    #region DEBUG
    private void OnGUI()
    {
        if (!showDebugGUI || player == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 320, 220));
        GUILayout.BeginVertical("box");

        GUILayout.Label("<b>=== PASILLO OSCURO ===</b>");
        GUILayout.Label($"Estado: <color=yellow>{debugStatus}</color>");
        GUILayout.Label($"Player X: {player.position.x:F1}");
        GUILayout.Label($"Spotlight X: {spotlightX}");
        GUILayout.Label($"Distancia: {Mathf.Abs(player.position.x - spotlightX):F1}");
        GUILayout.Space(5);
        GUILayout.Label($"Player Locked: {(playerLocked ? "<color=red>🔒 SÍ</color>" : "❌ NO")}");
        GUILayout.Label($"Spotlight Alcanzado: {(spotlightReached ? "✅" : "❌")}");
        GUILayout.Label($"Monstruo Triggered: {(monsterEventTriggered ? "✅" : "❌")}");
        GUILayout.Space(5);
        GUILayout.Label($"Next Scene: {nextSceneName}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void OnDrawGizmosSelected()
    {
        float y = 0f;
        float height = 5f;

        // Zonas de luz
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(lightZoneEnd, y - height, 0), new Vector3(lightZoneEnd, y + height, 0));

        Gizmos.color = Color.gray;
        Gizmos.DrawLine(new Vector3(darkZoneStart, y - height, 0), new Vector3(darkZoneStart, y + height, 0));

        // Spotlight trigger zone
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(new Vector3(spotlightX, y, 0), 3f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(darkZoneEnd, y - height, 0), new Vector3(darkZoneEnd, y + height, 0));

        // Monster path
        if (monsterSpawnPoint != null && monsterEndPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(monsterSpawnPoint.position, 0.5f);
            Gizmos.DrawSphere(monsterEndPoint.position, 0.5f);
            Gizmos.DrawLine(monsterSpawnPoint.position, monsterEndPoint.position);
        }
    }
    #endregion
}