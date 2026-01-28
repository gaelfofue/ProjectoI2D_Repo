using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private bool isGameOver = false;
    [SerializeField] private string gameOverReason = "";

    [Header("Events")]
    public UnityEvent OnGameStart;
    public UnityEvent OnGameOver;
    public UnityEvent OnGameRestart;

    [Header("Input")]
    [SerializeField] private bool useInputSystem = true;

    // Propiedades públicas
    public bool IsGameOver => isGameOver;
    public string GameOverReason => gameOverReason;

    private void Awake()
    {
        // Singleton MEJORADO
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ⭐ IMPORTANTE: Suscribirse al evento de carga de escena
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        // Limpiar suscripción
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Se llama cada vez que una escena termina de cargar
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"📍 Escena cargada: {scene.name}");

        // ⭐ CRÍTICO: Limpiar TODOS los listeners antiguos
        // Esto elimina referencias a objetos destruidos
        OnGameStart.RemoveAllListeners();
        OnGameOver.RemoveAllListeners();
        OnGameRestart.RemoveAllListeners();

        // Pequeño delay para que los objetos de la nueva escena se inicialicen
        StartCoroutine(InitializeAfterSceneLoad());
    }

    private System.Collections.IEnumerator InitializeAfterSceneLoad()
    {
        // Esperar un frame para que todos los objetos se inicialicen
        yield return null;

        // Resetear estado del juego
        isGameOver = false;
        gameOverReason = "";
        Time.timeScale = 1f;

        // Notificar que el juego comenzó
        OnGameStart?.Invoke();

        Debug.Log("🎮 Juego iniciado correctamente");
    }

    public void StartGame()
    {
        isGameOver = false;
        gameOverReason = "";
        Time.timeScale = 1f;
        OnGameStart?.Invoke();
        Debug.Log("🎮 Juego iniciado");
    }

    public void GameOver(string reason = "")
    {
        if (isGameOver) return;

        isGameOver = true;
        gameOverReason = reason;
        Time.timeScale = 0f;

        Debug.Log($"💀 GAME OVER: {reason}");

        // Invocar evento
        OnGameOver?.Invoke();
    }

    public void RestartGame()
    {
        Debug.Log("🔄 Reiniciando juego...");

        // ⭐ IMPORTANTE: Restaurar timeScale ANTES de cargar escena
        Time.timeScale = 1f;
        isGameOver = false;
        gameOverReason = "";

        // Invocar evento de restart
        OnGameRestart?.Invoke();

        // Cargar escena
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Update()
    {
        if (!isGameOver) return;

        // Input System moderno
        if (useInputSystem && Keyboard.current != null)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }
        }
    }
}