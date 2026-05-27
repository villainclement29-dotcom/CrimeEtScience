using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GlobalPlayerManager : MonoBehaviour
{
    public static GlobalPlayerManager Instance;

    [Header("Stockage des Manettes")]
    public InputDevice Player1Device; // Index 0
    public InputDevice Player2Device; // Index 1

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (Display.displays.Length > 1) Display.displays[1].Activate();
            if (Display.displays.Length > 2) Display.displays[2].Activate();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Fonction appelée par le LobbyController pour enregistrer la manette
    public void AssignPlayer(int index, InputDevice device)
    {
        if (device == null) return;

        if (index == 0) Player1Device = device;
        else if (index == 1) Player2Device = device;

        Debug.Log($"<color=green><b>[GlobalPlayerManager]</b> : J{index} lié à {device.displayName}</color>");
    }

    public void DebugCurrentPlayers()
    {
        string p1 = (Player1Device != null) ? Player1Device.displayName : "AUCUN";
        string p2 = (Player2Device != null) ? Player2Device.displayName : "AUCUN";
        Debug.Log($"<color=yellow>J0: {p1} | J1: {p2}</color>");
    }
}