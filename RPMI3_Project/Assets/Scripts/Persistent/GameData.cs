using UnityEngine;
using UnityEngine.SceneManagement;

// Almacena Datos Persistentes del jugador entre escenas y sesiones, simplemente para saber su desempeño

public class GameData : MonoBehaviour
{
    public static GameData instance {  get; private set; }

    [Header("Player Progress")]
    public int currentLevel = 1;
    public int totalDeaths = 0;
    public float totalPlayTime = 0f;
    public int currentCheckpoint = 0;

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;

    [Header("Story Flags")]
    public bool hasSeenIntro = false;
    public bool hasCompletedTutorial = false;

    [Header("Statistics")]
    public int totalHideAttempts = 0;
    public int timesDiscovered = 0;
    public float longestHideTime = 0f;

    private void Awake()
    {
        // Singleton con persistencia
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();

            // Suscribirse a cambios de escena
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
       if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SaveData();
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"GameData: Escena '{scene.name}' cargada");
    }

    private void Update()
    {
        // Solo cuenta el tiempo si el juego no está pausado
        if (Time.timeScale > 0f)
        {
            totalPlayTime += Time.deltaTime;
        }
    }

    #region SAVE/LOAD SYSTEM

    public void SaveData() // Guarda informacion del jugador
    {
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("TotalDeaths", totalDeaths);
        PlayerPrefs.SetFloat("PlayTime", totalPlayTime);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetInt("HasSeenIntro", hasSeenIntro ? 1 : 0);
        PlayerPrefs.SetInt("HasCompletedTutorial", hasCompletedTutorial ? 1 : 0);
        PlayerPrefs.SetInt("TotalHideAttempts", totalHideAttempts);
        PlayerPrefs.SetInt("TimesDiscovered", timesDiscovered);
        PlayerPrefs.SetFloat("LongestHideTime", longestHideTime);
        PlayerPrefs.Save();

        Debug.Log("Datos Guardados...");

    }

    public void LoadData() // Almacena la informacion del jugador
    {
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        totalDeaths = PlayerPrefs.GetInt("TotalDeaths", 0);
        totalPlayTime = PlayerPrefs.GetFloat("PlayTime", 0f);
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        hasSeenIntro = PlayerPrefs.GetInt("HasSeenIntro", 0) == 1;
        hasCompletedTutorial = PlayerPrefs.GetInt("HasCompletedTutorial", 0) == 1;
        totalHideAttempts = PlayerPrefs.GetInt("TotalHideAttempts", 0);
        timesDiscovered = PlayerPrefs.GetInt("TimesDiscovered", 0);
        longestHideTime = PlayerPrefs.GetFloat("LongestHideTime", 0f);

        Debug.Log("Datos cargados...");
    }

    public void ResetAllData () // Borra todos los datos y los deja por default
    {
        PlayerPrefs.DeleteAll();
        currentLevel = 1;
        totalDeaths = 0;
        totalPlayTime = 0f;
        hasSeenIntro = false;
        hasCompletedTutorial = false;
        totalHideAttempts = 0;
        timesDiscovered = 0;
        longestHideTime = 0f;

        Debug.Log("Datos reseteados...");
    }

    #endregion

    #region STATISTICS
    public void RegisterDeath()
    {
        totalDeaths++;
        SaveData();
        Debug.Log($"Muerte #{totalDeaths} registrada");
    }

    public void RegisterHideAttempt(float duration)
    {
        totalHideAttempts++;
        if (duration > longestHideTime)
        {
            longestHideTime = duration;
        }
    }

    public void RegisterDiscovered()
    {
        timesDiscovered++;
    }

    public void CompleteLevel(int levelIndex)
    {
        if (levelIndex >= currentLevel)
        {
            currentLevel = levelIndex + 1;
            SaveData();
        }
    }
    #endregion
}
