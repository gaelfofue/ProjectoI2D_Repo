using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Opcional, si usas TextMeshPro

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private TextMeshProUGUI reasonText; // Opcional

    [Header("Settings")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private bool isSubscribed = false;

    private void Awake()
    {
        // Asegurar que el panel esté oculto desde el inicio
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void Start()
    {
        SetupButtons();
        SubscribeToEvents();
    }

    private void SetupButtons()
    {
        // Configurar botones con RemoveAllListeners para evitar duplicados
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
    }

    private void SubscribeToEvents()
    {
        if (isSubscribed) return;

        // Buscar GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.AddListener(ShowGameOverPanel);
            GameManager.Instance.OnGameRestart.AddListener(HideGameOverPanel);
            isSubscribed = true;
            Debug.Log("✅ GameOverUI conectado al GameManager");
        }
        else
        {
            Debug.LogWarning("⚠️ GameManager.Instance es null, reintentando...");
            Invoke(nameof(SubscribeToEvents), 0.1f);
        }
    }

    private void OnDestroy()
    {
        // ⭐ CRÍTICO: Desuscribirse al destruirse
        if (GameManager.Instance != null && isSubscribed)
        {
            GameManager.Instance.OnGameOver.RemoveListener(ShowGameOverPanel);
            GameManager.Instance.OnGameRestart.RemoveListener(HideGameOverPanel);
            isSubscribed = false;
        }
    }

    public void ShowGameOverPanel()
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        // Mostrar razón de muerte si está disponible
        if (reasonText != null && GameManager.Instance != null)
        {
            reasonText.text = GameManager.Instance.GameOverReason;
        }

        // Asegurar botones interactuables
        if (restartButton != null) restartButton.interactable = true;
        if (mainMenuButton != null) mainMenuButton.interactable = true;

        Debug.Log("🖥️ Mostrando pantalla de Game Over");
    }

    public void HideGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnRestartClicked()
    {
        Debug.Log("🔄 Botón Reintentar presionado");

        // Desactivar botón para evitar múltiples clics
        if (restartButton != null)
            restartButton.interactable = false;

        // ⭐ USAR GameManager.Instance directamente
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("❌ GameManager.Instance es null");
            // Fallback
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("🏠 Botón Menú Principal presionado");

        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(mainMenuScene))
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}