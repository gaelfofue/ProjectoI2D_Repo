using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Maneja el menú de pausa. Se conecta automáticamente al GameManager.
/// </summary>
public class PauseUI : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Button References")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button settingsButton; // Opcional
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private bool animateOnShow = true;

    [Header("Default Texts")]
    [SerializeField] private string defaultTitle = "PAUSA";
    [SerializeField] private string defaultInfo = "Presiona ESC para continuar";

    private bool isSubscribed = false;

    private void Awake()
    {
        Debug.Log("🔧 [PauseUI] Awake");

        // Auto-obtener CanvasGroup si no está asignado
        if (panelCanvasGroup == null && pausePanel != null)
        {
            panelCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = pausePanel.AddComponent<CanvasGroup>();
                Debug.Log("✅ CanvasGroup añadido automáticamente");
            }
        }

        // Configurar alpha inicial
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
        }

        // Configurar botones
        SetupButtons();

        // Suscribirse a eventos
        SubscribeToEvents();
    }

    private void Start()
    {
        Debug.Log("🔧 [PauseUI] Start");

        // Asegurar que el panel esté desactivado al inicio
        if (pausePanel != null && pausePanel != this.gameObject)
        {
            pausePanel.SetActive(false);
            Debug.Log("✅ Pause Panel desactivado en Start");
        }
        else if (pausePanel == this.gameObject)
        {
            gameObject.SetActive(false);
            Debug.Log("✅ Este GameObject (pause panel) desactivado en Start");
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SetupButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeClicked);
            Debug.Log("✅ Resume button configurado");
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
            Debug.Log("✅ Restart button configurado");
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnSettingsClicked);
            Debug.Log("✅ Settings button configurado");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            Debug.Log("✅ MainMenu button configurado");
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
            Debug.Log("✅ Quit button configurado");
        }
    }

    private void SubscribeToEvents()
    {
        if (isSubscribed) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPause.AddListener(ShowPauseMenu);
            GameManager.Instance.OnResume.AddListener(HidePauseMenu);
            isSubscribed = true;
            Debug.Log("✅ PauseUI suscrito al GameManager");
        }
        else
        {
            Debug.Log("⚠️ GameManager.Instance NULL, reintentando...");
            Invoke(nameof(RetrySubscribe), 0.1f);
        }
    }

    private void RetrySubscribe()
    {
        if (!isSubscribed)
        {
            SubscribeToEvents();
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (!isSubscribed) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPause.RemoveListener(ShowPauseMenu);
            GameManager.Instance.OnResume.RemoveListener(HidePauseMenu);
        }

        isSubscribed = false;
    }

    #region SHOW/HIDE
    public void ShowPauseMenu()
    {
        Debug.Log("⏸️ Mostrando menú de pausa");

        if (pausePanel == null)
        {
            Debug.LogError("❌ pausePanel es NULL!");
            return;
        }

        // ACTIVAR EL PANEL PRIMERO
        pausePanel.SetActive(true);

        // Configurar textos
        if (titleText != null)
        {
            titleText.text = defaultTitle;
        }

        if (infoText != null)
        {
            infoText.text = defaultInfo;
        }

        // Botones interactuables
        if (resumeButton != null) resumeButton.interactable = true;
        if (restartButton != null) restartButton.interactable = true;
        if (settingsButton != null) settingsButton.interactable = true;
        if (mainMenuButton != null) mainMenuButton.interactable = true;
        if (quitButton != null) quitButton.interactable = true;

        // Animación
        if (animateOnShow && panelCanvasGroup != null)
        {
            StartCoroutine(FadeInPanel());
        }
        else if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
        }

        Debug.Log("✅ Menú de pausa mostrado");
    }

    public void HidePauseMenu()
    {
        Debug.Log("▶️ Ocultando menú de pausa");

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
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
    private void OnResumeClicked()
    {
        Debug.Log("▶️ Botón Reanudar presionado");

        if (resumeButton != null)
            resumeButton.interactable = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
        else
        {
            // Fallback
            Time.timeScale = 1f;
            HidePauseMenu();
        }
    }

    private void OnRestartClicked()
    {
        Debug.Log("🔄 Botón Reiniciar presionado desde pausa");

        if (restartButton != null)
            restartButton.interactable = false;

        // Primero reanudar el juego para que la escena pueda cargarse
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }

    private void OnSettingsClicked()
    {
        Debug.Log("⚙️ Botón Ajustes presionado");

        // Aquí puedes abrir un panel de ajustes
        // Por ahora solo mostramos un mensaje
        Debug.Log("💡 Panel de ajustes aún no implementado");

        // Ejemplo de cómo podrías implementarlo:
        // if (settingsPanel != null)
        // {
        //     settingsPanel.SetActive(true);
        //     pausePanel.SetActive(false);
        // }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("🏠 Botón Menú Principal presionado desde pausa");

        if (mainMenuButton != null)
            mainMenuButton.interactable = false;

        // Reanudar el juego antes de cambiar de escena
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("🚪 Botón Salir presionado desde pausa");

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

    // TEST - Click derecho en el script → Test Show Pause
    [ContextMenu("Test Show Pause")]
    private void TestShowPause()
    {
        Debug.Log("🧪 TEST: Mostrando menú de pausa manualmente");
        ShowPauseMenu();
    }

    [ContextMenu("Test Hide Pause")]
    private void TestHidePause()
    {
        Debug.Log("🧪 TEST: Ocultando menú de pausa manualmente");
        HidePauseMenu();
    }
}