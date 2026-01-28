// GameOverUI.cs - UN SOLO ARCHIVO
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Settings")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private void Start()
    {
        // Ocultar panel al inicio
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Configurar botones
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        // Suscribirse a eventos del GameManager
        SubscribeToGameManagerEvents();
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar memory leaks
        UnsubscribeFromGameManagerEvents();
    }

    private void SubscribeToGameManagerEvents()
    {
        if (FindObjectOfType<GameManager>() != null)
        {
            GameManager.Instance.OnGameOver.AddListener(ShowGameOverPanel);
            GameManager.Instance.OnGameRestart.AddListener(HideGameOverPanel);
        }
        else
        {
            Debug.LogWarning("GameManager no encontrado en la escena");
            // Intentar suscribirse más tarde
            Invoke("SubscribeToGameManagerEvents", 0.5f);
        }
    }

    private void UnsubscribeFromGameManagerEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver.RemoveListener(ShowGameOverPanel);
            GameManager.Instance.OnGameRestart.RemoveListener(HideGameOverPanel);
        }
    }

    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null && !gameOverPanel.activeSelf)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("🖥️ Mostrando pantalla de Game Over");
        }
    }

    public void HideGameOverPanel()
    {
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            gameOverPanel.SetActive(false);
            Debug.Log("🖥️ Ocultando pantalla de Game Over");
        }
    }

    private void OnRestartClicked()
    {
        Debug.Log("🔄 Botón Reintentar presionado");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("GameManager no encontrado");
            // Fallback: recargar escena manualmente
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("🏠 Botón Menú Principal presionado");
        if (!string.IsNullOrEmpty(mainMenuScene))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
        }
        else
        {
            Debug.LogWarning("Nombre de escena del menú principal no configurado");
        }
    }

    // Método público para forzar mostrar/ocultar
    public void SetGameOverPanelActive(bool active)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(active);
        }
    }
}