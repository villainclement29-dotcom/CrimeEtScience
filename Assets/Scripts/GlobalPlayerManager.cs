using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalPlayerManager : MonoBehaviour
{
    // C'est ce "Instance" que ton LobbyController cherche partout !
    public static GlobalPlayerManager Instance;

    [Header("Stockage des Manettes")]
    public InputDevice Player1Device; // Index 0
    public InputDevice Player2Device; // Index 1

    void Awake()
    {
        // On vérifie s'il n'y a pas déjà un manager (Singleton)
        if (Instance == null)
        {
            Instance = this;
            // Commande magique : cet objet ne sera pas détruit au changement de scène
            DontDestroyOnLoad(gameObject);
            Debug.Log("<color=cyan><b>[GlobalPlayerManager]</b> : Initialisé et Persistant.</color>");
        }
        else
        {
            Debug.LogWarning("<color=orange><b>[GlobalPlayerManager]</b> : Double détecté, destruction du doublon.</color>");
            Destroy(gameObject);
        }
    }

    // Fonction appelée par le LobbyController pour enregistrer la manette
    public void AssignPlayer(int index, InputDevice device)
    {
        if (device == null)
        {
            Debug.LogError($"<color=red><b>[GlobalPlayerManager]</b> : Tentative d'assigner un périphérique NULL au Joueur {index}</color>");
            return;
        }

        if (index == 0) Player1Device = device;
        else if (index == 1) Player2Device = device;

        // Log ultra complet : Index Joueur, Nom de la manette, et ID unique du hardware
        Debug.Log($"<color=green><b>[GlobalPlayerManager]</b> : ASSIGNATION RÉUSSIE</color>\n" +
                  $" -> <b>Joueur :</b> {index}\n" +
                  $" -> <b>Nom :</b> {device.displayName}\n" +
                  $" -> <b>ID Système :</b> {device.deviceId}");
    }

    // Petit bonus : une fonction pour vérifier l'état du manager dans la console à tout moment
    public void DebugCurrentPlayers()
    {
        string p1 = (Player1Device != null) ? Player1Device.displayName : "AUCUN";
        string p2 = (Player2Device != null) ? Player2Device.displayName : "AUCUN";

        Debug.Log($"<color=yellow><b>[GlobalPlayerManager]</b> État Actuel :\n" +
                  $"J0 (Gauche) : {p1}\n" +
                  $"J1 (Droite) : {p2}</color>");
    }
}