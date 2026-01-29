using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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
        Time.timeScale = 1f;

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

        // ✅ CORREGIDO: Usar nuevo Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeKey();
        }
    }
    #endregion

    #region INITIALIZATION
    private void InitializeMenu()
    {
        ShowPanel(mainMenuPanel);
        HidePanel(settingsPanel);
        HidePanel(creditsPanel);
        HidePanel(confirmPanel);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void SetupButtons()
    {
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
        if (GameData.instance != null)
        {
            hasSaveData = GameData.instance.currentLevel > 1 ||
                          GameData.instance.totalPlayTime > 0 ||
                          GameData.instance.hasCompletedTutorial;
        }
        else
        {
            hasSaveData = PlayerPrefs.GetInt("CurrentLevel", 1) > 1 ||
                          PlayerPrefs.GetFloat("PlayTime", 0f) > 0;
        }

        if (continueButton != null)
        {
            continueButton.interactable = hasSaveData;

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
        if (GameData.instance != null)
        {
            GameData.instance.ResetAllData();
        }
        else
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        LoadGameScene(firstLevelScene);
    }

    private void ContinueGame()
    {
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
        Debug.Log($"[MainMenu] Intentando cargar escena: {sceneName}");

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(sceneName, true);
        }
        else
        {
            // Fallback directo
            Debug.LogWarning("[MainMenu] SceneLoader no encontrado, usando carga directa");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }

    private void QuitGame()
    {
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
        float pulse = Mathf.Sin(Time.time * titlePulseSpeed) * titlePulseIntensity;
        float alpha = titleOriginalAlpha + pulse;

        Color newColor = titleOriginalColor;
        newColor.a = Mathf.Clamp01(alpha);
        titleText.color = newColor;
    }

    private void AnimateBreathing()
    {
        float breath = Mathf.Sin(Time.time * breathingSpeed) * breathingIntensity;
        fadeOverlay.alpha = Mathf.Clamp01(breathingIntensity + breath);
    }
    #endregion

    #region AUDIO
    private void AddButtonSounds(Button button)
    {
        if (AudioManager.Instance == null) return;

        UnityEngine.EventSystems.EventTrigger trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        if (buttonHoverSound != null)
        {
            UnityEngine.EventSystems.EventTrigger.Entry hoverEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            hoverEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            hoverEntry.callback.AddListener((data) => { AudioManager.Instance.PlaySFX(buttonHoverSound, 0.5f); });
            trigger.triggers.Add(hoverEntry);
        }

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