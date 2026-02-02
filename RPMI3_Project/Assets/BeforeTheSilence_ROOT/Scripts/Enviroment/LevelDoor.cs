using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Puerta que transporta al jugador a un nivel específico.
/// </summary>
public class LevelDoor : MonoBehaviour
{
    #region SETTINGS
    [Header("Level Settings")]
    [SerializeField] private string levelSceneName;
    [SerializeField] private int levelIndex = 1;
    [SerializeField] private string levelDisplayName = "Nivel 1";

    [Header("Lock Settings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private int requiredLevelToUnlock = 0;
    [SerializeField] private string lockedMessage = "Completa el nivel anterior primero";

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool useNewInputSystem = true;

    [Header("Prompt Settings")]
    [SerializeField] private bool showPrompt = true;
    [SerializeField] private string promptText = "E";
    [SerializeField] private string lockedPromptText = "🔒";
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color highlightColor = new Color(0.8f, 1f, 0.8f);
    [SerializeField] private Color enteringColor = new Color(0.5f, 1f, 0.5f);

    [Header("Animation")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float pulseAmount = 0.05f;

    [Header("Audio")]
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip lockedSound;

    [Header("Transition")]
    [SerializeField] private bool useLoadingScreen = true;
    [SerializeField] private float transitionDelay = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    #endregion

    #region PRIVATE VARIABLES
    private bool playerInRange = false;
    private bool isTransitioning = false;
    private PlayerController player;
    private InteractionPrompt interactionPrompt;
    private Vector3 originalScale;
    private float pulseTimer = 0f;
    #endregion

    #region UNITY METHODS
    private void Start()
    {
        // Guardar escala original
        originalScale = transform.localScale;

        // Obtener renderer si no está asignado
        if (doorRenderer == null)
        {
            doorRenderer = GetComponent<SpriteRenderer>();
        }

        // Buscar o crear InteractionPrompt
        SetupInteractionPrompt();

        // Verificar si está bloqueada
        CheckLockStatus();

        // Aplicar color inicial
        UpdateDoorVisual();

        if (debugMode)
        {
            Debug.Log($"[LevelDoor] '{levelDisplayName}' inicializada. Locked: {isLocked}");
        }
    }

    private void Update()
    {
        if (isTransitioning) return;

        // Animación de pulso
        if (enablePulse && playerInRange && !isLocked)
        {
            AnimatePulse();
        }

        // Detectar input de interacción
        if (playerInRange && !isLocked)
        {
            CheckInteractionInput();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        player = other.GetComponent<PlayerController>();
        if (player == null || player.IsDead) return;

        playerInRange = true;
        UpdateDoorVisual();

        // Mostrar prompt
        if (interactionPrompt != null)
        {
            interactionPrompt.Show();
        }

        if (debugMode)
        {
            Debug.Log($"[LevelDoor] Jugador entró en rango de '{levelDisplayName}'");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        player = null;

        // Restaurar escala
        transform.localScale = originalScale;

        UpdateDoorVisual();

        // Ocultar prompt
        if (interactionPrompt != null)
        {
            interactionPrompt.Hide();
        }

        if (debugMode)
        {
            Debug.Log($"[LevelDoor] Jugador salió del rango de '{levelDisplayName}'");
        }
    }
    #endregion

    #region SETUP
    private void SetupInteractionPrompt()
    {
        // Buscar prompt existente
        interactionPrompt = GetComponent<InteractionPrompt>();

        if (interactionPrompt == null && showPrompt)
        {
            // Crear uno básico si no existe
            interactionPrompt = gameObject.AddComponent<InteractionPrompt>();
        }

        // Actualizar texto según estado
        UpdatePromptText();
    }

    private void CheckLockStatus()
    {
        if (requiredLevelToUnlock <= 0)
        {
            isLocked = false;
            return;
        }

        // Verificar progreso del jugador
        int playerProgress = 1;

        if (GameData.instance != null)
        {
            playerProgress = GameData.instance.currentLevel;
        }
        else
        {
            playerProgress = PlayerPrefs.GetInt("CurrentLevel", 1);
        }

        isLocked = playerProgress < requiredLevelToUnlock;

        if (debugMode)
        {
            Debug.Log($"[LevelDoor] '{levelDisplayName}' - Progreso: {playerProgress}, Requerido: {requiredLevelToUnlock}, Locked: {isLocked}");
        }
    }
    #endregion

    #region INTERACTION
    private void CheckInteractionInput()
    {
        bool interactPressed = false;

        if (useNewInputSystem)
        {
            // Nuevo Input System
            if (Keyboard.current != null)
            {
                interactPressed = Keyboard.current.eKey.wasPressedThisFrame;
            }
        }
        else
        {
            // Input System antiguo
            interactPressed = Input.GetKeyDown(interactKey);
        }

        if (interactPressed)
        {
            TryEnterDoor();
        }
    }

    private void TryEnterDoor()
    {
        if (isTransitioning) return;

        if (isLocked)
        {
            OnDoorLocked();
            return;
        }

        EnterDoor();
    }

    private void EnterDoor()
    {
        isTransitioning = true;

        if (debugMode)
        {
            Debug.Log($"[LevelDoor] Entrando a '{levelDisplayName}' (Escena: {levelSceneName})");
        }

        // Efecto visual
        if (doorRenderer != null)
        {
            doorRenderer.color = enteringColor;
        }

        // Sonido
        if (doorOpenSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(doorOpenSound);
        }

        // Flash del prompt
        if (interactionPrompt != null)
        {
            interactionPrompt.FlashConfirm();
        }

        // Cargar nivel después de delay
        if (transitionDelay > 0)
        {
            Invoke(nameof(LoadLevel), transitionDelay);
        }
        else
        {
            LoadLevel();
        }
    }

    private void LoadLevel()
    {
        // Usar SceneLoader si existe
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(levelSceneName, useLoadingScreen);
        }
        else
        {
            // Fallback directo
            UnityEngine.SceneManagement.SceneManager.LoadScene(levelSceneName);
        }
    }

    private void OnDoorLocked()
    {
        if (debugMode)
        {
            Debug.Log($"[LevelDoor] '{levelDisplayName}' está bloqueada. {lockedMessage}");
        }

        // Sonido de bloqueado
        if (lockedSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(lockedSound);
        }

        // Efecto visual de "no puedes pasar"
        StartCoroutine(LockedShakeEffect());

        // Mostrar mensaje (puedes conectar esto a tu UI)
        ShowLockedMessage();
    }

    private System.Collections.IEnumerator LockedShakeEffect()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        float shakeAmount = 0.1f;

        Vector3 originalPos = transform.position;
        Color originalColor = doorRenderer != null ? doorRenderer.color : Color.white;

        // Flash rojo
        if (doorRenderer != null)
        {
            doorRenderer.color = Color.red;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Sin(elapsed * 50f) * shakeAmount * (1f - elapsed / duration);
            transform.position = originalPos + new Vector3(x, 0f, 0f);
            yield return null;
        }

        transform.position = originalPos;

        if (doorRenderer != null)
        {
            doorRenderer.color = originalColor;
        }
    }

    private void ShowLockedMessage()
    {
        // Por ahora solo debug, pero puedes conectar a un sistema de UI
        Debug.Log($"🔒 {lockedMessage}");

        // TODO: Mostrar en UI
        // UIManager.Instance?.ShowMessage(lockedMessage);
    }
    #endregion

    #region VISUAL
    private void UpdateDoorVisual()
    {
        if (doorRenderer == null) return;

        Color targetColor;

        if (isLocked)
        {
            targetColor = lockedColor;
        }
        else if (playerInRange)
        {
            targetColor = highlightColor;
        }
        else
        {
            targetColor = normalColor;
        }

        doorRenderer.color = targetColor;
    }

    private void UpdatePromptText()
    {
        if (interactionPrompt == null) return;

        if (isLocked)
        {
            interactionPrompt.SetText(lockedPromptText);
        }
        else
        {
            interactionPrompt.SetText(promptText);
        }
    }

    private void AnimatePulse()
    {
        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulse = 1f + Mathf.Sin(pulseTimer * Mathf.PI * 2f) * pulseAmount;
        transform.localScale = originalScale * pulse;
    }
    #endregion

    #region PUBLIC METHODS
    /// <summary>
    /// Desbloquear la puerta
    /// </summary>
    public void Unlock()
    {
        isLocked = false;
        UpdateDoorVisual();
        UpdatePromptText();

        if (debugMode)
        {
            Debug.Log($"[LevelDoor] '{levelDisplayName}' desbloqueada");
        }
    }

    /// <summary>
    /// Bloquear la puerta
    /// </summary>
    public void Lock()
    {
        isLocked = true;
        UpdateDoorVisual();
        UpdatePromptText();

        if (debugMode)
        {
            Debug.Log($"[LevelDoor] '{levelDisplayName}' bloqueada");
        }
    }

    /// <summary>
    /// Verificar si está bloqueada
    /// </summary>
    public bool IsLocked() => isLocked;

    /// <summary>
    /// Forzar entrada (ignora bloqueo)
    /// </summary>
    public void ForceEnter()
    {
        isLocked = false;
        EnterDoor();
    }

    /// <summary>
    /// Refrescar estado de bloqueo según progreso
    /// </summary>
    public void RefreshLockStatus()
    {
        CheckLockStatus();
        UpdateDoorVisual();
        UpdatePromptText();
    }
    #endregion

    #region GIZMOS
    private void OnDrawGizmos()
    {
        // Dibujar área de la puerta
        Gizmos.color = isLocked ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        // Icono
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + promptOffset, 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        // Mostrar nombre del nivel
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"{levelDisplayName}\n({levelSceneName})"
        );
#endif
    }
    #endregion
}