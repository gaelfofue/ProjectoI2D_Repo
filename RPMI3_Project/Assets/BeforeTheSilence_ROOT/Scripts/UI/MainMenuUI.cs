using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    #region REFERENCES
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject confirmPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [Header("Confirm Panel")]
    [SerializeField] private TextMeshProUGUI confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Title Animation")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private bool animateTitle = true;
    [SerializeField] private float titlePulseSpeed = 1f;
    [SerializeField] private float titlePulseIntensity = 0.1f;

    [Header("Ambient Effects")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float breathingSpeed = 2f;
    [SerializeField] private float breathingIntensity = 0.05f;

    [Header("Scene Names")]
    [SerializeField] private string firstLevelScene = "Level_01";
    [SerializeField] private string lobbyScene = "Lobby";

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioClip buttonHoverSound;
    [SerializeField] private AudioClip buttonClickSound;
    #endregion

    #region PRIVATE VARIABLES
    private System.Action pendingConfirmAction;
    private bool hasSaveData = false;
    private Color titleOriginalColor;
    private float titleOriginalAlpha;
    #endregion

    #region UNITY METHODS
    private void Awake()
    {
        // Asegurar TimeScale normal
        Time.timeScale = 1f;

        // Guardar color original del título
        if (titleText != null)
        {
            titleOriginalColor = titleText.color;
            titleOriginalAlpha = titleText.color.a;
        }
    }

    private void Start()
    {
        InitializeMenu();
        SetupButtons();
        CheckForSaveData();

        // Reproducir música del menú si AudioManager existe
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }
    }

    private void Update()
    {
        if (animateTitle && titleText != null)
        {
            AnimateTitle();
        }

        if (fadeOverlay != null)
        {
            AnimateBreathing();
        }

        // ESC para cerrar paneles secundarios
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapeKey();
        }
    }
    #endregion

    #region INITIALIZATION
    private void InitializeMenu()
    {
        // Mostrar solo el panel principal
        ShowPanel(mainMenuPanel);
        HidePanel(settingsPanel);
        HidePanel(creditsPanel);
        HidePanel(confirmPanel);

        // Cursor visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void SetupButtons()
    {
        // Main Menu Buttons
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(OnNewGameClicked);
            AddButtonSounds(newGameButton);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
            AddButtonSounds(continueButton);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnSettingsClicked);
            AddButtonSounds(settingsButton);
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(OnCreditsClicked);
            AddButtonSounds(creditsButton);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitClicked);
            AddButtonSounds(quitButton);
        }

        // Confirm Panel Buttons
        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveAllListeners();
            confirmYesButton.onClick.AddListener(OnConfirmYes);
            AddButtonSounds(confirmYesButton);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveAllListeners();
            confirmNoButton.onClick.AddListener(OnConfirmNo);
            AddButtonSounds(confirmNoButton);
        }
    }

    private void CheckForSaveData()
    {
        // Verificar si hay datos guardados
        if (GameData.instance != null)
        {
            hasSaveData = GameData.instance.currentLevel > 1 ||
                          GameData.instance.totalPlayTime > 0 ||
                          GameData.instance.hasCompletedTutorial;
        }
        else
        {
            // Verificar directamente en PlayerPrefs
            hasSaveData = PlayerPrefs.GetInt("CurrentLevel", 1) > 1 ||
                          PlayerPrefs.GetFloat("PlayTime", 0f) > 0;
        }

        // Activar/desactivar botón de continuar
        if (continueButton != null)
        {
            continueButton.interactable = hasSaveData;

            // Opcional: cambiar visualmente si no hay datos
            TextMeshProUGUI buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null && !hasSaveData)
            {
                buttonText.color = new Color(buttonText.color.r, buttonText.color.g, buttonText.color.b, 0.5f);
            }
        }
    }
    #endregion

    #region BUTTON HANDLERS
    private void OnNewGameClicked()
    {
        if (hasSaveData)
        {
            // Preguntar confirmación si hay datos guardados
            ShowConfirmation(
                "¿Empezar nueva partida?\n<size=70%>Se perderá el progreso anterior.</size>",
                StartNewGame
            );
        }
        else
        {
            StartNewGame();
        }
    }

    private void OnContinueClicked()
    {
        if (!hasSaveData) return;
        ContinueGame();
    }

    private void OnSettingsClicked()
    {
        ShowPanel(settingsPanel);
        HidePanel(mainMenuPanel);
    }

    private void OnCreditsClicked()
    {
        ShowPanel(creditsPanel);
        HidePanel(mainMenuPanel);
    }

    private void OnQuitClicked()
    {
        ShowConfirmation(
            "¿Seguro que quieres salir?",
            QuitGame
        );
    }

    private void OnConfirmYes()
    {
        HidePanel(confirmPanel);
        pendingConfirmAction?.Invoke();
        pendingConfirmAction = null;
    }

    private void OnConfirmNo()
    {
        HidePanel(confirmPanel);
        pendingConfirmAction = null;
    }
    #endregion

    #region GAME ACTIONS
    private void StartNewGame()
    {
        // Resetear datos si GameData existe
        if (GameData.instance != null)
        {
            GameData.instance.ResetAllData();
        }
        else
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        // Cargar primera escena
        LoadGameScene(firstLevelScene);
    }

    private void ContinueGame()
    {
        // Determinar qué escena cargar basado en el progreso
        string sceneToLoad = GetSceneForCurrentProgress();
        LoadGameScene(sceneToLoad);
    }

    private string GetSceneForCurrentProgress()
    {
        int currentLevel = 1;

        if (GameData.instance != null)
        {
            currentLevel = GameData.instance.currentLevel;
        }
        else
        {
            currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        }

        // Mapear nivel a escena
        return currentLevel switch
        {
            1 => firstLevelScene,
            2 => "Level_02",
            3 => "Level_03",
            _ => firstLevelScene
        };
    }

    private void LoadGameScene(string sceneName)
    {
        // Usar SceneLoader si existe
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(sceneName, true);
        }
        else
        {
            // Fallback directo
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    private void QuitGame()
    {
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

    #region PANEL MANAGEMENT
    private void ShowPanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);

            // Animar fade in si tiene CanvasGroup
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                StartCoroutine(FadePanel(cg, 0f, 1f, 0.3f));
            }
        }
    }

    private void HidePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void ShowConfirmation(string message, System.Action onConfirm)
    {
        pendingConfirmAction = onConfirm;

        if (confirmText != null)
        {
            confirmText.text = message;
        }

        ShowPanel(confirmPanel);
    }

    private IEnumerator FadePanel(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        cg.alpha = to;
    }

    // Método público para volver al menú principal desde otros paneles
    public void BackToMainMenu()
    {
        HidePanel(settingsPanel);
        HidePanel(creditsPanel);
        HidePanel(confirmPanel);
        ShowPanel(mainMenuPanel);
    }

    private void HandleEscapeKey()
    {
        if (confirmPanel != null && confirmPanel.activeSelf)
        {
            OnConfirmNo();
        }
        else if (settingsPanel != null && settingsPanel.activeSelf)
        {
            BackToMainMenu();
        }
        else if (creditsPanel != null && creditsPanel.activeSelf)
        {
            BackToMainMenu();
        }
    }
    #endregion

    #region ANIMATIONS
    private void AnimateTitle()
    {
        // Efecto de pulso sutil en el título (como si respirara)
        float pulse = Mathf.Sin(Time.time * titlePulseSpeed) * titlePulseIntensity;
        float alpha = titleOriginalAlpha + pulse;

        Color newColor = titleOriginalColor;
        newColor.a = Mathf.Clamp01(alpha);
        titleText.color = newColor;
    }

    private void AnimateBreathing()
    {
        // Efecto de "respiración" en el overlay oscuro
        float breath = Mathf.Sin(Time.time * breathingSpeed) * breathingIntensity;
        fadeOverlay.alpha = Mathf.Clamp01(breathingIntensity + breath);
    }
    #endregion

    #region AUDIO
    private void AddButtonSounds(Button button)
    {
        // Añadir sonidos de hover y click si AudioManager existe
        if (AudioManager.Instance == null) return;

        // Añadir EventTrigger para hover
        UnityEngine.EventSystems.EventTrigger trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        // Hover sound
        if (buttonHoverSound != null)
        {
            UnityEngine.EventSystems.EventTrigger.Entry hoverEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            hoverEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            hoverEntry.callback.AddListener((data) => { AudioManager.Instance.PlaySFX(buttonHoverSound, 0.5f); });
            trigger.triggers.Add(hoverEntry);
        }

        // Click sound
        if (buttonClickSound != null)
        {
            UnityEngine.EventSystems.EventTrigger.Entry clickEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            clickEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            clickEntry.callback.AddListener((data) => { AudioManager.Instance.PlaySFX(buttonClickSound, 0.7f); });
            trigger.triggers.Add(clickEntry);
        }
    }
    #endregion
}