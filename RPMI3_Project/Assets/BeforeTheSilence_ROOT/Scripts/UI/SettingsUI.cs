using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class SettingsMenuUI : MonoBehaviour
{
    #region REFERENCES
    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Volume Labels")]
    [SerializeField] private TextMeshProUGUI masterVolumeLabel;
    [SerializeField] private TextMeshProUGUI musicVolumeLabel;
    [SerializeField] private TextMeshProUGUI sfxVolumeLabel;

    [Header("Display Settings")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Gameplay Settings")]
    [SerializeField] private Toggle screenShakeToggle;
    [SerializeField] private Toggle showHintsToggle;

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;

    [Header("References")]
    [SerializeField] private MainMenuManager mainMenuManager;
    #endregion

    #region PRIVATE VARIABLES
    private Resolution[] availableResolutions;
    private int currentResolutionIndex = 0;
    private bool hasUnsavedChanges = false;
    #endregion

    #region UNITY METHODS
    private void Awake()
    {
        SetupSliders();
        SetupToggles();
        SetupDropdowns();
        SetupButtons();
    }

    private void OnEnable()
    {
        LoadCurrentSettings();
        hasUnsavedChanges = false;
    }

    private void Update()
    {
        // ESC para volver
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnBackClicked();
        }
    }
    #endregion

    #region SETUP
    private void SetupSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    private void SetupToggles()
    {
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        if (screenShakeToggle != null)
            screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);

        if (showHintsToggle != null)
            showHintsToggle.onValueChanged.AddListener(OnShowHintsChanged);
    }

    private void SetupDropdowns()
    {
        if (resolutionDropdown != null)
            SetupResolutionDropdown();

        if (qualityDropdown != null)
            SetupQualityDropdown();
    }

    private void SetupResolutionDropdown()
    {
        resolutionDropdown.ClearOptions();
        availableResolutions = Screen.resolutions;

        System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string option = $"{availableResolutions[i].width} x {availableResolutions[i].height}";

            if (!options.Contains(option))
                options.Add(option);

            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = options.Count - 1;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void SetupQualityDropdown()
    {
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (applyButton != null)
        {
            applyButton.onClick.RemoveAllListeners();
            applyButton.onClick.AddListener(OnApplyClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(OnResetClicked);
        }
    }
    #endregion

    #region LOAD/SAVE SETTINGS
    private void LoadCurrentSettings()
    {
        // ✅ CORREGIDO: Cargar desde AudioManager si existe
        if (AudioManager.Instance != null)
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = AudioManager.Instance.MasterVolume;
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = AudioManager.Instance.MusicVolume;
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = AudioManager.Instance.SFXVolume;
        }
        else if (GameData.instance != null)
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = GameData.instance.masterVolume;
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = GameData.instance.musicVolume;
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = GameData.instance.sfxVolume;
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

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = Screen.fullScreen;

        if (screenShakeToggle != null)
            screenShakeToggle.isOn = PlayerPrefs.GetInt("ScreenShake", 1) == 1;

        if (showHintsToggle != null)
            showHintsToggle.isOn = PlayerPrefs.GetInt("ShowHints", 1) == 1;

        UpdateVolumeLabels();
    }

    private void SaveSettings()
    {
        // Guardar en GameData si existe
        if (GameData.instance != null)
        {
            GameData.instance.SaveData();
        }

        // Guardar en PlayerPrefs como backup
        PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider?.value ?? 1f);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider?.value ?? 1f);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider?.value ?? 1f);

        if (screenShakeToggle != null)
            PlayerPrefs.SetInt("ScreenShake", screenShakeToggle.isOn ? 1 : 0);
        if (showHintsToggle != null)
            PlayerPrefs.SetInt("ShowHints", showHintsToggle.isOn ? 1 : 0);

        PlayerPrefs.Save();
        hasUnsavedChanges = false;

        Debug.Log("[Settings] Configuración guardada");
    }
    #endregion

    #region VALUE CHANGED HANDLERS
    private void OnMasterVolumeChanged(float value)
    {
        hasUnsavedChanges = true;
        UpdateVolumeLabel(masterVolumeLabel, value);

        // ✅ CORREGIDO: Aplicar inmediatamente a través de AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        hasUnsavedChanges = true;
        UpdateVolumeLabel(musicVolumeLabel, value);

        // ✅ CORREGIDO: Aplicar inmediatamente
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        hasUnsavedChanges = true;
        UpdateVolumeLabel(sfxVolumeLabel, value);

        // ✅ CORREGIDO: Aplicar inmediatamente
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        hasUnsavedChanges = true;
        Screen.fullScreen = isFullscreen;
    }

    private void OnResolutionChanged(int index)
    {
        hasUnsavedChanges = true;
        if (index < availableResolutions.Length)
        {
            Resolution res = availableResolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        }
    }

    private void OnQualityChanged(int index)
    {
        hasUnsavedChanges = true;
        QualitySettings.SetQualityLevel(index);
    }

    private void OnScreenShakeChanged(bool value)
    {
        hasUnsavedChanges = true;
    }

    private void OnShowHintsChanged(bool value)
    {
        hasUnsavedChanges = true;
    }
    #endregion

    #region BUTTON HANDLERS
    private void OnBackClicked()
    {
        if (hasUnsavedChanges)
        {
            SaveSettings();
        }

        if (mainMenuManager != null)
        {
            mainMenuManager.BackToMainMenu();
        }
    }

    private void OnApplyClicked()
    {
        SaveSettings();
    }

    private void OnResetClicked()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 1f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (screenShakeToggle != null) screenShakeToggle.isOn = true;
        if (showHintsToggle != null) showHintsToggle.isOn = true;
        if (qualityDropdown != null) qualityDropdown.value = QualitySettings.names.Length - 1;

        hasUnsavedChanges = true;
        UpdateVolumeLabels();
    }
    #endregion

    #region HELPERS
    private void UpdateVolumeLabels()
    {
        if (masterVolumeSlider != null)
            UpdateVolumeLabel(masterVolumeLabel, masterVolumeSlider.value);
        if (musicVolumeSlider != null)
            UpdateVolumeLabel(musicVolumeLabel, musicVolumeSlider.value);
        if (sfxVolumeSlider != null)
            UpdateVolumeLabel(sfxVolumeLabel, sfxVolumeSlider.value);
    }

    private void UpdateVolumeLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
        {
            int percentage = Mathf.RoundToInt(value * 100f);
            label.text = $"{percentage}%";
        }
    }
    #endregion
}