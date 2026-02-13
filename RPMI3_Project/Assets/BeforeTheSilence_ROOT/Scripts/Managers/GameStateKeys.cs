using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    public bool hasMotherRoomKey = false;
    public bool hasExitKey = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PickUpKey(string keyName)
    {
        if (keyName == "MotherRoomKey") hasMotherRoomKey = true;
        if (keyName == "ExitKey") hasExitKey = true;
        Debug.Log($"Llave recogida: {keyName}");
    }

    public void ResetKeys()
    {
        hasMotherRoomKey = false;
        hasExitKey = false;
    }
}