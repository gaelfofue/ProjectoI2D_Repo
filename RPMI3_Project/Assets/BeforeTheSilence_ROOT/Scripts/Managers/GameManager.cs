using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// VERSIÓN CORREGIDA - Eventos configurados correctamente
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level State")]
    [SerializeField] private bool isGameOver = false;
    [SerializeField] private bool isPaused = false;
    [SerializeField] private string gameOverReason = "";

    [Header("Level Settings")]
    [SerializeField] private int levelIndex = 0;
    [SerializeField] private string levelName = "Test Level";

    [Header("Events")]
    public UnityEvent OnLevelStart = new UnityEvent();
    public UnityEvent<string> OnGameOver = new UnityEvent<string>();  // ← IMPORTANTE
    public UnityEvent OnGameRestart = new UnityEvent();
    public UnityEvent OnPause = new UnityEvent();
    public UnityEvent OnResume = new UnityEvent();

    [Header("Input")]
    public bool allowPauseInput = true;
    public bool allowRestartInput = true;

    // Propiedades públicas
    public bool IsGameOver => isGameOver;
    public bool IsPaused => isPaused;
    public string GameOverReason => gameOverReason;
    public int LevelIndex => levelIndex;
    public string LevelName => levelName;

    private void Awake()
    {
        Debug.Log("🎮 [GameManager] Awake");

        // Singleton POR ESCENA (sin DontDestroyOnLoad)
        if (Instance == null)
        {
            Instance = this;
            Debug.Log(" GameManager Instance creado");
            if (!allowPauseInput) allowPauseInput = true;
            if (!allowRestartInput) allowRestartInput = true;
        }
        else
        {
            Debug.LogWarning("⚠️ Ya existe un GameManager, destruyendo este");
            Destroy(gameObject);
            return;
        }

        // IMPORTANTE: Inicializar eventos si son null
        if (OnLevelStart == null) OnLevelStart = new UnityEvent();
        if (OnGameOver == null) OnGameOver = new UnityEvent<string>();
        if (OnGameRestart == null) OnGameRestart = new UnityEvent();
        if (OnPause == null) OnPause = new UnityEvent();
        if (OnResume == null) OnResume = new UnityEvent();
    }

    private void Start()
    {
        Debug.Log("🎮 [GameManager] Start");
        StartLevel();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.Log("🎮 [GameManager] Destruyendo Instance");
            Instance = null;
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Keyboard.current == null) return;

        // Pausa
        if (allowPauseInput && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!isGameOver)
            {
                TogglePause();
            }
        }

        // Reinicio rápido
        if (allowRestartInput && isGameOver)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                RestartLevel();
            }
        }
    }

    #region LEVEL CONTROL
    public void StartLevel()
    {
        isGameOver = false;
        isPaused = false;
        gameOverReason = "";
        Time.timeScale = 1f;

        // Configurar cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Iniciar audio si existe
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
            AudioManager.Instance.SetBreathingState(BreathingState.Normal);
        }

        Debug.Log($"Nivel '{levelName}' iniciado");
        OnLevelStart?.Invoke();
    }

    public void GameOver(string reason = "")
    {
        Debug.Log($"🎮 [GameManager] GameOver llamado. isGameOver={isGameOver}, reason='{reason}'");

        if (isGameOver) return;

        isGameOver = true;
        gameOverReason = string.IsNullOrEmpty(reason) ? "¡Te atraparon!" : reason;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Registrar muerte
        if (GameData.instance != null)
        {
            GameData.instance.RegisterDeath();
        }

        // ✅ CORREGIDO: Audio en try-catch para no bloquear el GameOver
        try
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBreathing();
                AudioManager.Instance.PlayGameOverMusic();
                AudioManager.Instance.PlayDeathSound();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GameManager] Error en audio de GameOver (ignorado): {e.Message}");
        }

        Debug.Log($"🎮 GAME OVER: {gameOverReason}");

        // ✅ IMPORTANTE: Esto debe ejecutarse siempre
        if (OnGameOver != null)
        {
            Debug.Log($"🎮 Invocando OnGameOver con {OnGameOver.GetPersistentEventCount()} listeners persistentes");
            OnGameOver.Invoke(gameOverReason);
            Debug.Log("🎮 OnGameOver.Invoke() completado");
        }
        else
        {
            Debug.LogError("🎮 ❌ OnGameOver es NULL!");
        }
    }
    public void RestartLevel()
    {
        Debug.Log("Reiniciando nivel...");

        // Restaurar tiempo
        Time.timeScale = 1f;
        isGameOver = false;
        isPaused = false;

        OnGameRestart?.Invoke();

        // Recargar escena actual
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void LoadNextLevel()
    {
        if (GameData.instance != null)
        {
            GameData.instance.CompleteLevel(levelIndex);
        }

        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1
        );
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
    #endregion

    #region PAUSE SYSTEM
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isGameOver) return;

        isPaused = true;
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnPause?.Invoke();
        Debug.Log("⏸️ Juego pausado");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        OnResume?.Invoke();
        Debug.Log("▶️ Juego reanudado");
    }
    #endregion

    #region UTILITY
    public void CompleteLevel()
    {
        if (isGameOver) return;

        Debug.Log($"Nivel '{levelName}' completado!");

        if (GameData.instance != null)
        {
            GameData.instance.CompleteLevel(levelIndex);
        }
    }
    #endregion

    // TESTING
    [ContextMenu("Test Game Over")]
    private void TestGameOver()
    {
        Debug.Log("TEST: Forzando Game Over desde GameManager");
        GameOver("Test manual desde GameManager");
    }
}