using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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

    [Header("Transition Settings")]
    [SerializeField] private bool useLoadingScreen = false;
    [SerializeField] private float delayBeforeFade = 0.3f;
    [SerializeField] private bool freezePlayerOnEnter = true;

    [Header("Custom Fade (Si no hay SceneLoader)")]
    [SerializeField] private bool useCustomFade = false;
    [SerializeField] private float customFadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;

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

    // Custom fade
    private GameObject fadeCanvasObject;
    private CanvasGroup fadeCanvasGroup;
    private UnityEngine.UI.Image fadeImage;
    #endregion

    #region UNITY METHODS
    private void Start()
    {
        originalScale = transform.localScale;

        if (doorRenderer == null)
        {
            doorRenderer = GetComponent<SpriteRenderer>();
        }

        SetupInteractionPrompt();
        CheckLockStatus();
        UpdateDoorVisual();

        // Preparar fade custom si es necesario
        if (useCustomFade || SceneLoader.Instance == null)
        {
            SetupCustomFade();
        }

        if (debugMode)
        {
            Debug.Log($"[LevelDoor] '{levelDisplayName}' inicializada. Locked: {isLocked}");
        }
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (enablePulse && playerInRange && !isLocked)
        {
            AnimatePulse();
        }

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
        transform.localScale = originalScale;
        UpdateDoorVisual();

        if (interactionPrompt != null)
        {
            interactionPrompt.Hide();
        }
    }

    private void OnDestroy()
    {
        // Limpiar fade custom
        if (fadeCanvasObject != null)
        {
            Destroy(fadeCanvasObject);
        }
    }
    #endregion

    #region SETUP
    private void SetupInteractionPrompt()
    {
        interactionPrompt = GetComponent<InteractionPrompt>();

        if (interactionPrompt == null && showPrompt)
        {
            interactionPrompt = gameObject.AddComponent<InteractionPrompt>();
        }

        UpdatePromptText();
    }

    private void CheckLockStatus()
    {
        if (requiredLevelToUnlock <= 0)
        {
            isLocked = false;
            return;
        }

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
    }

    private void SetupCustomFade()
    {
        // Crear canvas para fade
        fadeCanvasObject = new GameObject("DoorFadeCanvas");
        fadeCanvasObject.transform.SetParent(null);
        DontDestroyOnLoad(fadeCanvasObject);

        Canvas canvas = fadeCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        fadeCanvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        fadeCanvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Crear imagen de fade
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(fadeCanvasObject.transform);

        fadeImage = imageObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        // Stretch to fill
        RectTransform rect = imageObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeCanvasGroup = imageObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        fadeCanvasObject.SetActive(false);
    }
    #endregion

    #region INTERACTION
    private void CheckInteractionInput()
    {
        bool interactPressed = false;

        if (useNewInputSystem)
        {
            if (Keyboard.current != null)
            {
                interactPressed = Keyboard.current.eKey.wasPressedThisFrame;
            }
        }
        else
        {
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

        // Congelar jugador
        if (freezePlayerOnEnter && player != null)
        {
            FreezePlayer();
        }

        // Efecto visual de la puerta
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
            interactionPrompt.Hide();
        }

        // Iniciar transición
        StartCoroutine(TransitionToLevel());
    }

    private IEnumerator TransitionToLevel()
    {
        // Delay inicial (para que se vea la animación de la puerta)
        if (delayBeforeFade > 0)
        {
            yield return new WaitForSeconds(delayBeforeFade);
        }

        // Verificar si hay SceneLoader
        if (SceneLoader.Instance != null && !useCustomFade)
        {
            // Usar el fade del SceneLoader
            if (debugMode)
            {
                Debug.Log("[LevelDoor] Usando SceneLoader para transición");
            }

            SceneLoader.Instance.LoadScene(levelSceneName, useLoadingScreen);
        }
        else
        {
            // Usar fade custom
            if (debugMode)
            {
                Debug.Log("[LevelDoor] Usando fade custom para transición");
            }

            yield return StartCoroutine(CustomFadeAndLoad());
        }
    }

    private IEnumerator CustomFadeAndLoad()
    {
        // Activar canvas
        if (fadeCanvasObject != null)
        {
            fadeCanvasObject.SetActive(true);
        }

        // Fade out (pantalla se oscurece)
        yield return StartCoroutine(CustomFadeOut());

        // Cargar escena
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(levelSceneName);

        if (asyncLoad == null)
        {
            Debug.LogError($"[LevelDoor] No se pudo cargar la escena '{levelSceneName}'");
            isTransitioning = false;
            yield break;
        }

        // Esperar a que cargue
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Fade in (nueva escena aparece)
        yield return StartCoroutine(CustomFadeIn());

        // Desactivar canvas
        if (fadeCanvasObject != null)
        {
            fadeCanvasObject.SetActive(false);
        }
    }

    private IEnumerator CustomFadeOut()
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < customFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / customFadeDuration;

            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            if (fadeImage != null)
            {
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, t);
            }

            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator CustomFadeIn()
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsed = 0f;

        while (elapsed < customFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / customFadeDuration;

            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            if (fadeImage != null)
            {
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f - t);
            }

            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    private void FreezePlayer()
    {
        if (player == null) return;

        // Desactivar movimiento
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    private void OnDoorLocked()
    {
        if (debugMode)
        {
            Debug.Log($"[LevelDoor] '{levelDisplayName}' está bloqueada. {lockedMessage}");
        }

        if (lockedSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(lockedSound);
        }

        StartCoroutine(LockedShakeEffect());
    }

    private IEnumerator LockedShakeEffect()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        float shakeAmount = 0.1f;

        Vector3 originalPos = transform.position;
        Color originalColor = doorRenderer != null ? doorRenderer.color : Color.white;

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

        interactionPrompt.SetText(isLocked ? lockedPromptText : promptText);
    }

    private void AnimatePulse()
    {
        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulse = 1f + Mathf.Sin(pulseTimer * Mathf.PI * 2f) * pulseAmount;
        transform.localScale = originalScale * pulse;
    }
    #endregion

    #region PUBLIC METHODS
    public void Unlock()
    {
        isLocked = false;
        UpdateDoorVisual();
        UpdatePromptText();
    }

    public void Lock()
    {
        isLocked = true;
        UpdateDoorVisual();
        UpdatePromptText();
    }

    public bool IsLocked() => isLocked;

    public void ForceEnter()
    {
        isLocked = false;
        EnterDoor();
    }

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
        Gizmos.color = isLocked ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + promptOffset, 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"{levelDisplayName}\n({levelSceneName})"
        );
#endif
    }
    #endregion
}