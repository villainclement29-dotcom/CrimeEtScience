using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyController : MonoBehaviour
{
    [Header("UI Status - Display 1 (Écran Principal)")]
    public TextMeshProUGUI statusTextJ1_D1;
    public TextMeshProUGUI statusTextJ2_D1;

    [Header("UI Status - Display 2 (Écran Secondaire)")]
    public TextMeshProUGUI statusTextJ1_D2;
    public TextMeshProUGUI statusTextJ2_D2;

    [Header("Paramètres")]
    public string nextSceneName = "MiniGame2";
    public float delayBeforeLoad = 2f;

    private bool j1Ready = false;
    private bool j2Ready = false;

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        // 1. SÉCURITÉ ANTI-FANTÔME : On n'accepte QUE les Gamepads
        // Si tu veux aussi autoriser le clavier, enlève la condition "is Gamepad"
        if (playerInput.devices.Count == 0 || !(playerInput.devices[0] is Gamepad))
        {
            Debug.LogWarning("<color=orange>Lobby :</color> Connexion bloquée (pas une manette).");
            Destroy(playerInput.gameObject); // On supprime le joueur fantôme
            return;
        }

        // 2. Sécurité de scène
        if (SceneManager.GetActiveScene().name != "Lobby") return;

        int id = playerInput.playerIndex;
        InputDevice device = playerInput.devices[0];

        // 3. Enregistrement dans le manager persistant
        if (GlobalPlayerManager.Instance != null)
        {
            GlobalPlayerManager.Instance.AssignPlayer(id, device);
        }

        // 4. Mise à jour de l'UI sur les deux écrans
        if (id == 0)
        {
            j1Ready = true;
            UpdatePlayerStatus(statusTextJ1_D1, statusTextJ1_D2, "Joueur 1 CONNECTÉ");
            Debug.Log($"<color=green>Lobby :</color> J1 capturé ({device.displayName})");
        }
        else if (id == 1)
        {
            j2Ready = true;
            UpdatePlayerStatus(statusTextJ2_D1, statusTextJ2_D2, "Joueur 2 CONNECTÉ");
            Debug.Log($"<color=green>Lobby :</color> J2 capturé ({device.displayName})");
        }

        // 5. Lancement automatique
        if (j1Ready && j2Ready)
        {
            if (!IsInvoking("LoadNextScene"))
            {
                Invoke("LoadNextScene", delayBeforeLoad);
            }
        }
    }

    private void UpdatePlayerStatus(TextMeshProUGUI textD1, TextMeshProUGUI textD2, string message)
    {
        string formattedMessage = $"<color=green>{message}</color>";

        if (textD1 != null)
        {
            textD1.text = formattedMessage;
            textD1.color = Color.green;
        }

        if (textD2 != null)
        {
            textD2.text = formattedMessage;
            textD2.color = Color.green;
        }
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}