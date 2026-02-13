using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Puerta que solo abre cuando hay persecución activa.
/// Compatible con New Input System.
/// </summary>
public class LockedDoor : MonoBehaviour
{
    [Header("SETTINGS")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private EnemyTriggerZone triggerZone;

    [Header("FADE")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    [Header("AUDIO")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorLockedSound;

    [Header("DEBUG")]
    [SerializeField] private bool showDebug = true;

    private bool playerInRange = false;
    private bool isTransitioning = false;
    private string debugStatus = "Esperando...";

    private void Start()
    {
        if (fadeImage != null)
            fadeImage.color = new Color(0, 0, 0, 0);

        Debug.Log("[LockedDoor] ✅ Iniciada");
    }

    private void Update()
    {
        if (!playerInRange || isTransitioning) return;

        // Detectar tecla E con AMBOS sistemas
        bool ePressed = false;

        // New Input System
        if (Keyboard.current != null)
        {
            ePressed = Keyboard.current.eKey.wasPressedThisFrame;
        }

        // Old Input System (backup)
        if (!ePressed)
        {
            ePressed = Input.GetKeyDown(KeyCode.E);
        }

        if (ePressed)
        {
            Debug.Log("[LockedDoor] 🔑 Tecla E detectada!");
            TryOpenDoor();
        }
    }

    private void TryOpenDoor()
    {
        bool canOpen = false;

        if (triggerZone != null)
        {
            canOpen = triggerZone.IsChaseActive;
            Debug.Log($"[LockedDoor] IsChaseActive: {canOpen}");
        }
        else
        {
            Debug.LogWarning("[LockedDoor] ⚠️ TriggerZone no asignada - permitiendo abrir");
            canOpen = true;
        }

        if (canOpen)
        {
            debugStatus = "¡Abriendo!";
            Debug.Log("[LockedDoor] ✅ ¡Puerta abierta!");

            if (audioSource != null && doorOpenSound != null)
                audioSource.PlayOneShot(doorOpenSound);

            StartCoroutine(OpenDoor());
        }
        else
        {
            debugStatus = "🔒 Cerrada";
            Debug.Log("[LockedDoor] 🔒 Puerta cerrada - no hay persecución");

            if (audioSource != null && doorLockedSound != null)
                audioSource.PlayOneShot(doorLockedSound);
        }
    }

    private IEnumerator OpenDoor()
    {
        isTransitioning = true;
        debugStatus = "Cargando...";

        // Fade
        if (fadeImage != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeImage.color = new Color(0, 0, 0, elapsed / fadeDuration);
                yield return null;
            }
            fadeImage.color = Color.black;
        }

        yield return new WaitForSeconds(0.3f);

        Debug.Log($"[LockedDoor] 🎬 Cargando escena: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        debugStatus = "Presiona E";
        Debug.Log("[LockedDoor] ✅ Player en rango de la puerta");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        debugStatus = "Esperando...";
        Debug.Log("[LockedDoor] Player salió del rango");
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 120));
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>=== PUERTA ===</b>");
        GUILayout.Label($"Estado: <color=yellow>{debugStatus}</color>");
        GUILayout.Label($"En rango: {(playerInRange ? "<color=green>✅ SÍ</color>" : "❌ NO")}");

        string chaseStatus = "NO REF";
        if (triggerZone != null)
        {
            chaseStatus = triggerZone.IsChaseActive ? "<color=green>✅ ACTIVO</color>" : "<color=red>❌ NO</color>";
        }
        GUILayout.Label($"Chase: {chaseStatus}");
        GUILayout.Label($"Escena: {nextSceneName}");
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = playerInRange ? Color.green : Color.cyan;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}