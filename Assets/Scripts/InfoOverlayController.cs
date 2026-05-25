using UnityEngine;
using UnityEngine.InputSystem;

public class InfoOverlayController : MonoBehaviour
{
    [Tooltip("0 = Joueur 1, 1 = Joueur 2")]
    public int playerId = 0;

    [Tooltip("Overlay GameObject à afficher quand on appuie sur Y")]
    public GameObject overlay;

    void Update()
    {
        if (overlay == null) return;

        InputDevice device = null;
        if (GlobalPlayerManager.Instance != null)
        {
            device = playerId == 0
                ? GlobalPlayerManager.Instance.Player1Device
                : GlobalPlayerManager.Instance.Player2Device;
        }

        // Fallback : si aucun device n'a été assigné via le Lobby,
        // on tape sur la manette correspondante dans Gamepad.all.
        if (device == null)
        {
            if (Gamepad.all.Count > playerId) device = Gamepad.all[playerId];
            else if (Gamepad.all.Count > 0)   device = Gamepad.all[0];
        }

        bool toggle = false;

        if (device is Gamepad gp && gp.buttonNorth.wasPressedThisFrame)
            toggle = true;

        // Fallback clavier (utile en debug Editor sans manette)
        if (Keyboard.current != null)
        {
            Key fallback = playerId == 0 ? Key.Y : Key.U;
            if (Keyboard.current[fallback].wasPressedThisFrame) toggle = true;
        }

        if (toggle) overlay.SetActive(!overlay.activeSelf);
    }
}
