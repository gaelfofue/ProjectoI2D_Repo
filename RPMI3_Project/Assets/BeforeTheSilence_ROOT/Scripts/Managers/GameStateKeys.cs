using UnityEngine;
using System.Collections.Generic;

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    private HashSet<string> collectedKeys = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PickUpKey(string keyName)
    {
        if (string.IsNullOrEmpty(keyName)) return;

        if (collectedKeys.Add(keyName))
        {
            Debug.Log($"[GameProgressManager] Llave recogida: {keyName}");
        }
    }

    public bool HasKey(string keyName)
    {
        return collectedKeys.Contains(keyName);
    }

    public bool UseKey(string keyName)
    {
        if (collectedKeys.Remove(keyName))
        {
            Debug.Log($"[GameProgressManager] Llave usada: {keyName}");
            return true;
        }
        return false;
    }

    public void ResetProgress()
    {
        collectedKeys.Clear();
    }
}