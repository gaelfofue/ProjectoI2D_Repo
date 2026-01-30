using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class PauseUI : MonoBehaviour
{
    #region PANEL REFERENCES
    [Header("Panel References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup pausePanelCanvasGroup;
    [SerializeField] private CanvasGroup settingsPanelCanvasGroup;
    #endregion

    #region PAUSE MENU REFERENCES
    [Header("Pause Menu - Text References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Pause Menu - Button References")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    #endregion

    #region SETTINGS PANEL REFERENCES
    [Header("Settings - Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Settings - Volume Labels")]
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    [SerializeField] private TextMeshProUGUI musicVolumeLabel;
    [SerializeField] private TextMeshProUGUI sfxVolumeLabel;

    [Header("Settings - Buttons")]
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button settingsResetButton;
    #endregion

    #region ANIMATION SETTINGS
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private bool animateOnShow = true;
    #endregion

    #region DEFAULT TEXTS
    [Header("Default Texts")]
    [SerializeField] private string defaultTitle = "PAUSA";
    [SerializeField] private string defaultInfo = "Presiona ESC para continuar";
    #endregion

    #region PRIVATE VARIABLES
    private bool isSubscribed = false;
    private bool isShowingSettings = false;
    #endregion

    #region UNITY METHODS
    private void Awake()
    {
        Debug.Log("🔧 [PauseUI] Awake");

        // Setup CanvasGroups
        SetupCanvasGroups();

        // Setup all buttons
        SetupPauseButtons();
        SetupSettingsPanel();

        // Subscribe to GameManager events
        SubscribeToEvents();
    }

    private void Start()
    {
        Debug.Log("🔧 [PauseUI] Start");

        // Ensure panels are hidden at start
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Debug.Log("✅ Pause Panel desactivado en Start");
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("✅ Settings Panel desactivado en Start");
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void Update()
    {
        // Handle ESC key when settings panel is open
        if (isShowingSettings && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            BackToPauseMenu();
        }
    }
    #endregion

    #region SETUP METHODS
    private void SetupCanvasGroups()
    {
        // Pause Panel CanvasGroup
        if (pausePanelCanvasGroup == null && pausePanel != null)
        {
            pausePanelCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
            if (pausePanelCanvasGroup == null)
                pausePanelCanvasGroup = pausePanel.AddComponent<CanvasGroup>();
        }

        if (pausePanelCanvasGroup != null)
            pausePanelCanvasGroup.alpha = 0f;

        // Settings Panel CanvasGroup
        if (settingsPanelCanvasGroup == null && settingsPanel != null)
        {
            settingsPanelCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (settingsPanelCanvasGroup == null)
                settingsPanelCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
        }

        if (settingsPanelCanvasGroup != null)
            settingsPanelCanvasGroup.alpha = 0f;
    }

    private void SetupPauseButtons()
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

    private void SetupSettingsPanel()
    {
        // Volume Sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        // Settings Buttons
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveAllListeners();
            settingsBackButton.onClick.AddListener(BackToPauseMenu);
        }

        if (settingsResetButton != null)
        {
            settingsResetButton.onClick.RemoveAllListeners();
            settingsResetButton.onClick.AddListener(OnResetSettingsClicked);
        }
    }
    #endregion

    #region EVENT SUBSCRIPTION
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
    #endregion

    #region PAUSE MENU SHOW/HIDE
    public void ShowPauseMenu()
    {
        Debug.Log("⏸️ Mostrando menú de pausa");

        if (pausePanel == null) return;

        // Reset to pause menu (not settings)
        isShowingSettings = false;

        // Show pause panel, hide settings
        pausePanel.SetActive(true);
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Update texts
        if (titleText != null) titleText.text = defaultTitle;
        if (infoText != null) infoText.text = defaultInfo;

        // Enable buttons
        SetPauseButtonsInteractable(true);

        // Animate
        if (animateOnShow && pausePanelCanvasGroup != null)
            StartCoroutine(FadeInPanel(pausePanelCanvasGroup));
        else if (pausePanelCanvasGroup != null)
            pausePanelCanvasGroup.alpha = 1f;

        Debug.Log("✅ Menú de pausa mostrado");
    }

    public void HidePauseMenu()
    {
        Debug.Log("▶️ Ocultando menú de pausa");

        isShowingSettings = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanelCanvasGroup != null)
            pausePanelCanvasGroup.alpha = 0f;

        if (settingsPanelCanvasGroup != null)
            settingsPanelCanvasGroup.alpha = 0f;
    }

    private void SetPauseButtonsInteractable(bool interactable)
    {
        if (resumeButton != null) resumeButton.interactable = interactable;
        if (restartButton != null) restartButton.interactable = interactable;
        if (settingsButton != null) settingsButton.interactable = interactable;
        if (mainMenuButton != null) mainMenuButton.interactable = interactable;
        if (quitButton != null) quitButton.interactable = interactable;
    }
    #endregion

    #region SETTINGS PANEL
    private void ShowSettingsPanel()
    {
        Debug.Log("⚙️ Mostrando panel de ajustes");

        isShowingSettings = true;

        // Hide pause, show settings
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);

            // Load current values
            LoadCurrentVolumeSettings();

            // Animate
            if (animateOnShow && settingsPanelCanvasGroup != null)
                StartCoroutine(FadeInPanel(settingsPanelCanvasGroup));
            else if (settingsPanelCanvasGroup != null)
                settingsPanelCanvasGroup.alpha = 1f;
        }
    }

    private void BackToPauseMenu()
    {
        Debug.Log("⬅️ Volviendo al menú de pausa");

        // Save settings before going back
        SaveVolumeSettings();

        isShowingSettings = false;

        // Hide settings, show pause
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);

            if (pausePanelCanvasGroup != null)
                pausePanelCanvasGroup.alpha = 1f;
        }
    }

    private void LoadCurrentVolumeSettings()
    {
        if (AudioManager.Instance != null)
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = AudioManager.Instance.MasterVolume;

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = AudioManager.Instance.MusicVolume;

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = AudioManager.Instance.SFXVolume;
        }
        else
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }

        UpdateAllVolumeLabels();
    }

    private void SaveVolumeSettings()
    {
        if (GameData.instance != null)
        {
            GameData.instance.SaveData();
        }

        PlayerPrefs.Save();
        Debug.Log("💾 Configuración de volumen guardada");
    }
    #endregion

    #region VOLUME HANDLERS
    private void OnMasterVolumeChanged(float value)
    {
        UpdateVolumeLabel(masterVolumeLabel, value);

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        UpdateVolumeLabel(musicVolumeLabel, value);

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        UpdateVolumeLabel(sfxVolumeLabel, value);

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }

    private void UpdateVolumeLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
        {
            int percentage = Mathf.RoundToInt(value * 100f);
            label.text = $"{percentage}%";
        }
    }

    private void UpdateAllVolumeLabels()
    {
        if (masterVolumeSlider != null)
            UpdateVolumeLabel(masterVolumeLabel, masterVolumeSlider.value);

        if (musicVolumeSlider != null)
            UpdateVolumeLabel(musicVolumeLabel, musicVolumeSlider.value);

        if (sfxVolumeSlider != null)
            UpdateVolumeLabel(sfxVolumeLabel, sfxVolumeSlider.value);
    }

    private void OnResetSettingsClicked()
    {
        Debug.Log("🔄 Reseteando configuración");

        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 1f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1f;

        UpdateAllVolumeLabels();
    }
    #endregion

    #region PAUSE BUTTON HANDLERS
    private void OnResumeClicked()
    {
        Debug.Log("▶️ Botón Continuar presionado");

        if (resumeButton != null)
            resumeButton.interactable = false;

        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
        else
        {
            Time.timeScale = 1f;
            HidePauseMenu();
        }
    }

    private void OnRestartClicked()
    {
        Debug.Log("🔄 Botón Reiniciar presionado");

        if (restartButton != null)
            restartButton.interactable = false;

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void OnSettingsClicked()
    {
        Debug.Log("⚙️ Botón Ajustes presionado");
        ShowSettingsPanel();
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("🏠 Botón Menú Principal presionado");

        if (mainMenuButton != null)
            mainMenuButton.interactable = false;

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMainMenu();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private void OnQuitClicked()
    {
        Debug.Log("🚪 Botón Salir presionado");

        if (GameData.instance != null)
            GameData.instance.SaveData();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
    #endregion

    #region ANIMATIONS
    private IEnumerator FadeInPanel(CanvasGroup cg)
    {
        if (cg == null) yield break;

        float elapsed = 0f;
        cg.alpha = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        cg.alpha = 1f;
    }
    #endregion
}