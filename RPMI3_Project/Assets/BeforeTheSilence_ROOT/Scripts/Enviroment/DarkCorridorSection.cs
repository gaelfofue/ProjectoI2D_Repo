using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class DarkCorridorSection : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private Transform player;
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
    [Tooltip("Color de la silueta. Usa gris oscuro para que se vea")]
    [SerializeField] private Color silhouetteColor = new Color(0.1f, 0.1f, 0.1f, 1f); // Gris muy oscuro, NO negro puro
    [Tooltip("Sorting Order del monstruo. Más alto = más adelante")]
    [SerializeField] private int monsterSortingOrder = 100;
    [Tooltip("Sorting Layer del monstruo")]
    [SerializeField] private string monsterSortingLayer = "Default";

    [Header("AUDIO")]
    [SerializeField] private AudioSource monsterSource;
    [SerializeField] private AudioClip monsterPassSound;
    [SerializeField] private AudioClip distantRoar;

    [Header("DEBUG")]
    [SerializeField] private bool showDebugGUI = true;

    private bool isInDarkZone = false;
    private bool monsterEventTriggered = false;
    private bool spotlightReached = false;
    private GameObject monsterInstance;
    private string debugStatus = "Esperando...";

    private void Start()
    {
        Debug.Log("[DarkCorridor] ========== START ==========");

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (vignetteController == null)
            vignetteController = FindFirstObjectByType<VignetteController>();

        // Validar
        if (monsterPrefab == null)
            Debug.LogError("[DarkCorridor] ❌ MONSTER PREFAB NO ASIGNADO!");
        else
            Debug.Log($"[DarkCorridor] ✅ Monster Prefab: {monsterPrefab.name}");

        if (monsterSpawnPoint == null)
            Debug.LogError("[DarkCorridor] ❌ SPAWN POINT NO ASIGNADO!");
        if (monsterEndPoint == null)
            Debug.LogError("[DarkCorridor] ❌ END POINT NO ASIGNADO!");

        Debug.Log($"[DarkCorridor] Silhouette Color: {silhouetteColor}");
        Debug.Log($"[DarkCorridor] Sorting Order: {monsterSortingOrder}");
        Debug.Log("[DarkCorridor] ==============================");
    }

    private void Update()
    {
        if (player == null) return;

        float playerX = player.position.x;

        UpdateLighting(playerX);
        CheckSpotlightTrigger(playerX);
    }

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
            StartCoroutine(SpotlightSequence());
        }
    }

    private IEnumerator SpotlightSequence()
    {
        // Encender spotlight
        if (spotLight != null)
        {
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                spotLight.intensity = Mathf.Lerp(0f, 0.8f, elapsed);
                yield return null;
            }
            Debug.Log("[DarkCorridor] Spotlight encendido");
        }

        yield return new WaitForSeconds(1.5f);

        if (!monsterEventTriggered)
        {
            monsterEventTriggered = true;
            StartCoroutine(MonsterParallaxEvent());
        }
    }

    private IEnumerator MonsterParallaxEvent()
    {
        Debug.Log("[DarkCorridor] ========== MONSTRUO APARECIENDO ==========");
        debugStatus = "¡MONSTRUO!";

        if (monsterPrefab == null || monsterSpawnPoint == null || monsterEndPoint == null)
        {
            Debug.LogError("[DarkCorridor] ❌ Faltan referencias!");
            yield break;
        }

        // Flicker
        if (spotLight != null)
            StartCoroutine(FlickerLight(spotLight, 2f));

        // Crear monstruo en posición del spawn (Z = 0, usamos sorting order)
        Vector3 spawnPos = monsterSpawnPoint.position;

        Debug.Log($"[DarkCorridor] Spawning en: {spawnPos}");

        monsterInstance = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
        monsterInstance.name = "MonsterSilhouette_VISIBLE";

        // ===== CONFIGURAR VISIBILIDAD =====
        ConfigureMonsterVisibility(monsterInstance);

        // Escala
        monsterInstance.transform.localScale = Vector3.one * monsterScale;

        // Dirección
        bool movingRight = monsterEndPoint.position.x > monsterSpawnPoint.position.x;
        SetMonsterDirection(monsterInstance, movingRight);

        // Sonido
        if (monsterSource != null && monsterPassSound != null)
        {
            monsterSource.volume = 0.8f;
            monsterSource.PlayOneShot(monsterPassSound);
        }

        // Mover
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

        // Destruir
        if (monsterInstance != null)
        {
            Debug.Log("[DarkCorridor] Destruyendo monstruo");
            Destroy(monsterInstance);
        }

        debugStatus = "Monstruo pasó";

        // Vignette
        if (vignetteController != null)
            vignetteController.PanicFlash();

        // Rugido
        yield return new WaitForSeconds(0.5f);
        if (monsterSource != null && distantRoar != null)
        {
            monsterSource.volume = 0.5f;
            monsterSource.PlayOneShot(distantRoar);
        }

        Debug.Log("[DarkCorridor] ========== EVENTO COMPLETADO ==========");
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
            // Color de silueta (NO negro puro para que se vea)
            sr.color = silhouetteColor;

            // Sorting para que esté ADELANTE de todo
            sr.sortingLayerName = monsterSortingLayer;
            sr.sortingOrder = monsterSortingOrder;
        }
        Debug.Log($"[DarkCorridor] - {renderers.Length} sprites configurados");
        Debug.Log($"[DarkCorridor] - Color: {silhouetteColor}");
        Debug.Log($"[DarkCorridor] - Sorting: {monsterSortingLayer} / Order: {monsterSortingOrder}");
    }

    private void SetMonsterDirection(GameObject monster, bool movingRight)
    {
        // Para personajes riggeados, usar escala
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

        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(10, 10, 350, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("<b>=== DARK CORRIDOR ===</b>");
        GUILayout.Label($"Estado: <color=yellow>{debugStatus}</color>");
        GUILayout.Label($"Player X: {player.position.x:F1}");
        GUILayout.Label($"Spotlight X: {spotlightX} (dist: {Mathf.Abs(player.position.x - spotlightX):F1})");
        GUILayout.Label($"Prefab: {(monsterPrefab != null ? "✅" : "❌")}");
        GUILayout.Label($"Spawn: {(monsterSpawnPoint != null ? monsterSpawnPoint.position.ToString() : "❌")}");
        GUILayout.Label($"End: {(monsterEndPoint != null ? monsterEndPoint.position.ToString() : "❌")}");
        GUILayout.Label($"Silhouette Color: {silhouetteColor}");
        GUILayout.Label($"Sorting Order: {monsterSortingOrder}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void OnDrawGizmosSelected()
    {
        // Spotlight
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(new Vector3(spotlightX, 0, 0), 3f);

        // Ruta del monstruo
        if (monsterSpawnPoint != null && monsterEndPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(monsterSpawnPoint.position, 0.5f);
            Gizmos.DrawSphere(monsterEndPoint.position, 0.5f);
            Gizmos.DrawLine(monsterSpawnPoint.position, monsterEndPoint.position);
        }
    }
    #endregion
}