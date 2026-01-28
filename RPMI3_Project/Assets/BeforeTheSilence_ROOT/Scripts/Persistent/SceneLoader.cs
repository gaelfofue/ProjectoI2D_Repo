using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
/// Maneja todas las transiciones entre escenas con efectos de fade y pantallas de carga.
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private Slider loadingProgressBar;
    [SerializeField] private Text loadingText;
    [SerializeField] private float minimumLoadTime = 0.5f;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string LobbySceneName = "Lobby";
    [SerializeField] private string firstLevelSceneName = "Level_01";
    [SerializeField] private string secondLevelSceneName = "Level_02";
    [SerializeField] private string thirdLevelSceneName = "Level_03";
    [SerializeField] private string finalSceneName = "Level_Final";


    private bool isLoading = false;
    private Canvas fadeCanvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupFadeCanvas();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void SetupFadeCanvas()
    {
        // Crear Canvas para fade si no existe
        if (fadeCanvasGroup == null)
        {
            GameObject canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(transform);

            fadeCanvas = canvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 999;

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Crear panel de fade
            GameObject panelObj = new GameObject("FadePanel");
            panelObj.transform.SetParent(canvasObj.transform);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = fadeColor;

            RectTransform rect = panelObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            fadeCanvasGroup = panelObj.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"SceneLoader: Escena '{scene.name}' cargada");

        // Asegurar que el fade esté correcto
        if (fadeCanvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
    }

    #region SCENE LOADING
    public void LoadScene(string sceneName, bool showLoadingScreen = false)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneRoutine(sceneName, showLoadingScreen));
    }

    public void LoadScene(int sceneIndex, bool showLoadingScreen = false)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneRoutine(sceneIndex, showLoadingScreen));
    }

    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().buildIndex, false);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        LoadScene(mainMenuSceneName, false);
    }

    public void LoadFirstLevel()
    {
        LoadScene(firstLevelSceneName, true);
    }

    private IEnumerator LoadSceneRoutine(object scene, bool showLoading)
    {
        isLoading = true;

        // Asegurar timeScale normal para la transición
        Time.timeScale = 1f;

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Mostrar pantalla de carga si es necesario
        if (showLoading && loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(true);
            if (loadingProgressBar != null) loadingProgressBar.value = 0f;
        }

        // Cargar escena
        AsyncOperation operation;
        if (scene is string sceneName)
        {
            operation = SceneManager.LoadSceneAsync(sceneName);
        }
        else
        {
            operation = SceneManager.LoadSceneAsync((int)scene);
        }

        operation.allowSceneActivation = false;

        float elapsedTime = 0f;

        while (!operation.isDone)
        {
            elapsedTime += Time.unscaledDeltaTime;

            // Actualizar barra de progreso
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = progress;
            }

            if (loadingText != null)
            {
                loadingText.text = $"Cargando... {(progress * 100):F0}%";
            }

            // Activar escena cuando esté lista
            if (operation.progress >= 0.9f && elapsedTime >= minimumLoadTime)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // Ocultar pantalla de carga
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(false);
        }

        isLoading = false;

        // Fade in se hace automáticamente en OnSceneLoaded
    }
    #endregion

    #region FADE EFFECTS
    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    public IEnumerator FlashScreen(Color color, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        Image fadeImage = fadeCanvasGroup.GetComponent<Image>();
        Color originalColor = fadeImage != null ? fadeImage.color : fadeColor;

        if (fadeImage != null)
        {
            fadeImage.color = color;
        }

        // Flash in
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 0.8f, elapsed / halfDuration);
            yield return null;
        }

        // Flash out
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0.8f, 0f, elapsed / halfDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;

        if (fadeImage != null)
        {
            fadeImage.color = originalColor;
        }
    }
    #endregion

    #region CUTSCENE SUPPORT
    public void LoadCutscene(string cutsceneSceneName, string nextSceneName)
    {
        StartCoroutine(CutsceneRoutine(cutsceneSceneName, nextSceneName));
    }

    private IEnumerator CutsceneRoutine(string cutscene, string next)
    {
        yield return StartCoroutine(LoadSceneRoutine(cutscene, true));

        // El CutsceneManager de la escena llamará a ContinueAfterCutscene cuando termine
    }

    public void ContinueAfterCutscene(string nextScene)
    {
        LoadScene(nextScene, true);
    }
    #endregion

    // Propiedad para verificar si está cargando
    public bool IsLoading => isLoading;
}
