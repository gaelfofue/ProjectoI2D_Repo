using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameOverUI : MonoBehaviour
{
    #region REFERENCES
    [Header("Panel References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI reasonText;
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Button References")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private bool animateOnShow = true;

    [Header("Default Texts")]
    [SerializeField] private string defaultTitle = "GAME OVER";
    [SerializeField] private string defaultReason = "¡Te atraparon!";
    #endregion

    #region PRIVATE VARIABLES
    private bool isSubscribed = false;
    private Coroutine subscribeCoroutine;
    #endregion

    #region UNITY METHODS
    private void Awake()
    {
        Debug.Log("💀 [GameOverUI] Awake");

        // Setup CanvasGroup
        if (panelCanvasGroup == null && gameOverPanel != null)
        {
            panelCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
        }

        // Hide panel
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;

        // Setup buttons
        SetupButtons();
    }

    private void Start()
    {
        Debug.Log("💀 [GameOverUI] Start");

        // Start subscription process
        StartSubscription();
    }

    private void OnEnable()
    {
        Debug.Log("💀 [GameOverUI] OnEnable");

        // Also try to subscribe when enabled (helps with scene transitions)
        if (!isSubscribed)
        {
            StartSubscription();
        }

        // Subscribe to scene loaded event for re-subscription
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        Debug.Log("💀 [GameOverUI] OnDestroy");

        StopSubscription();
        UnsubscribeFromEvents();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"💀 [GameOverUI] Escena '{scene.name}' cargada, verificando suscripción...");

        // Re-verify subscription after scene load
        if (!isSubscribed)
        {
            StartSubscription();
        }
    }
    #endregion

    #region SETUP
    private void SetupButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
            Debug.Log("💀 ✅ Restart button configurado");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            Debug.Log("💀 ✅ MainMenu button configurado");
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
            Debug.Log("💀 ✅ Quit button configurado");
        }
    }
    #endregion

    #region SUBSCRIPTION
    private void StartSubscription()
    {
        if (isSubscribed) return;

        // Stop any existing subscription attempt
        StopSubscription();

        // Start new subscription coroutine
        subscribeCoroutine = StartCoroutine(SubscribeWithRetry());
    }

    private void StopSubscription()
    {
        if (subscribeCoroutine != null)
        {
            StopCoroutine(subscribeCoroutine);
            subscribeCoroutine = null;
        }
    }

    private IEnumerator SubscribeWithRetry()
    {
        int attempts = 0;
        int maxAttempts = 50; // 5 seconds max (50 * 0.1s)

        while (!isSubscribed && attempts < maxAttempts)
        {
            attempts++;

            if (GameManager.Instance != null)
            {
                // Subscribe to events
                GameManager.Instance.OnGameOver.RemoveListener(ShowGameOver); // Prevent duplicates
                GameManager.Instance.OnGameRestart.RemoveListener(HideGameOver);

                GameManager.Instance.OnGameOver.AddListener(ShowGameOver);
                GameManager.Instance.OnGameRestart.AddListener(HideGameOver);

                isSubscribed = true;
                Debug.Log($"💀 ✅ GameOverUI suscrito al GameManager (intento {attempts})");
                yield break;
            }

            if (attempts % 10 == 0) // Log every 1 second
            {
                Debug.Log($"💀 ⏳ Esperando GameManager... (intento {attempts}/{maxAttempts})");
            }

            yield return new WaitForSecondsRealtime(0.1f);
        }

        if (!isSubscribed)
        {
            Debug.LogError("💀 ❌ ERROR: No se pudo suscribir al GameManager después de 5 segundos!");
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (!isSubscribed) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.RemoveListener(ShowGameOver);
            GameManager.Instance.OnGameRestart.RemoveListener(HideGameOver);
        }

        isSubscribed = false;
        Debug.Log("💀 GameOverUI desuscrito del GameManager");
    }
    #endregion

    #region SHOW/HIDE
    public void ShowGameOver(string reason)
    {
        Debug.Log($"💀 ShowGameOver llamado con razón: '{reason}'");

        if (gameOverPanel == null)
        {
            Debug.LogError("💀 ❌ gameOverPanel es NULL!");
            return;
        }

        // Activate panel
        gameOverPanel.SetActive(true);

        // Set texts
        if (titleText != null)
            titleText.text = defaultTitle;

        if (reasonText != null)
            reasonText.text = string.IsNullOrEmpty(reason) ? defaultReason : reason;

        if (statsText != null && GameData.instance != null)
        {
            statsText.text = $"Muertes: {GameData.instance.totalDeaths}\n" +
                           $"Tiempo: {FormatTime(GameData.instance.totalPlayTime)}";
        }

        // Enable buttons
        if (restartButton != null) restartButton.interactable = true;
        if (mainMenuButton != null) mainMenuButton.interactable = true;
        if (quitButton != null) quitButton.interactable = true;

        // Animate
        if (animateOnShow && panelCanvasGroup != null)
            StartCoroutine(FadeInPanel());
        else if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 1f;

        Debug.Log("💀 ✅ Panel de Game Over mostrado");
    }

    public void HideGameOver()
    {
        Debug.Log("💀 HideGameOver llamado");

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;
    }

    private IEnumerator FadeInPanel()
    {
        if (panelCanvasGroup == null) yield break;

        float elapsed = 0f;
        panelCanvasGroup.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
    }
    #endregion

    #region BUTTON HANDLERS
    private void OnRestartClicked()
    {
        Debug.Log("💀 🔄 Botón Reiniciar presionado");

        if (restartButton != null)
            restartButton.interactable = false;

        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("💀 🏠 Botón Menú Principal presionado");

        if (mainMenuButton != null)
            mainMenuButton.interactable = false;

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMainMenu();
        else
            SceneManager.LoadScene(0);
    }

    private void OnQuitClicked()
    {
        Debug.Log("💀 🚪 Botón Salir presionado");

        if (GameData.instance != null)
            GameData.instance.SaveData();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
    #endregion

    #region HELPERS
    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
    #endregion

    #region DEBUG - Remove in production
    [ContextMenu("Test Show GameOver")]
    private void TestShowGameOver()
    {
        ShowGameOver("Test - Debug GameOver");
    }

    [ContextMenu("Force Subscribe")]
    private void ForceSubscribe()
    {
        isSubscribed = false;
        StartSubscription();
    }

    [ContextMenu("Check Status")]
    private void CheckStatus()
    {
        Debug.Log($"=== GameOverUI Status ===");
        Debug.Log($"isSubscribed: {isSubscribed}");
        Debug.Log($"GameManager.Instance: {(GameManager.Instance != null ? "EXISTS" : "NULL")}");
        Debug.Log($"gameOverPanel: {(gameOverPanel != null ? "EXISTS" : "NULL")}");
        Debug.Log($"panelCanvasGroup: {(panelCanvasGroup != null ? "EXISTS" : "NULL")}");
    }
    #endregion
}