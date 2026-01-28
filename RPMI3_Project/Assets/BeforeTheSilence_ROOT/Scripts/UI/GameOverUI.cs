using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Maneja la UI de Game Over. Se conecta al GameManager de la escena.
/// </summary>
public class GameOverUI : MonoBehaviour
{
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

    private bool isSubscribed = false;

    private void Awake()
    {
        // Ocultar panel al inicio
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
        }
    }

    private void Start()
    {
        SetupButtons();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SetupButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    private void SubscribeToEvents()
    {
        if (isSubscribed) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.AddListener(ShowGameOver);
            GameManager.Instance.OnGameRestart.AddListener(HideGameOver);
            isSubscribed = true;
            Debug.Log("GameOverUI conectado al GameManager");
        }
        else
        {
            // Reintentar en el siguiente frame
            Invoke(nameof(SubscribeToEvents), 0.1f);
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
    }

    #region SHOW/HIDE
    public void ShowGameOver(string reason)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        // Configurar textos
        if (titleText != null)
        {
            titleText.text = defaultTitle;
        }

        if (reasonText != null)
        {
            reasonText.text = string.IsNullOrEmpty(reason) ? defaultReason : reason;
        }

        // Mostrar estadísticas
        if (statsText != null && GameData.instance != null)
        {
            statsText.text = $"Muertes: {GameData.instance.totalDeaths}\n" +
                           $"Tiempo: {FormatTime(GameData.instance.totalPlayTime)}";
        }

        // Asegurar botones interactuables
        if (restartButton != null) restartButton.interactable = true;
        if (mainMenuButton != null) mainMenuButton.interactable = true;
        if (quitButton != null) quitButton.interactable = true;

        // Animación
        if (animateOnShow)
        {
            StartCoroutine(FadeInPanel());
        }
        else if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
        }

        Debug.Log("Mostrando Game Over UI");
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
        }
    }

    private System.Collections.IEnumerator FadeInPanel()
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
        Debug.Log("Botón Reintentar presionado");

        // Desactivar botón para evitar múltiples clics
        if (restartButton != null)
            restartButton.interactable = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
        else
        {
            // Fallback
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("Botón Menú Principal presionado");

        if (mainMenuButton != null)
            mainMenuButton.interactable = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
        else if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadMainMenu();
        }
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("Botón Salir presionado");

        // Guardar datos antes de salir
        if (GameData.instance != null)
        {
            GameData.instance.SaveData();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
    #endregion

    #region UTILITY
    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
    #endregion

    // Para testing desde el Inspector
    [ContextMenu("Test Show Game Over")]
    private void TestShowGameOver()
    {
        ShowGameOver("Test de Game Over");
    }
}