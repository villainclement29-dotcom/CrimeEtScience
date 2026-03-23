using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalPlayerManager : MonoBehaviour
{
    // C'est ce "Instance" que ton LobbyController cherche partout !
    public static GlobalPlayerManager Instance;

    [Header("Stockage des Manettes")]
    public InputDevice Player1Device;
    public InputDevice Player2Device;

    void Awake()
    {
        // On vérifie s'il n'y a pas déjà un manager (Singleton)
        if (Instance == null)
        {
            Instance = this;
            // Commande magique : cet objet ne sera pas détruit au changement de scène
            DontDestroyOnLoad(gameObject);
            Debug.Log("<color=cyan>GlobalPlayerManager : Initialisé et Persistant.</color>");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Fonction appelée par le LobbyController pour enregistrer la manette
    public void AssignPlayer(int index, InputDevice device)
    {
        if (index == 0) Player1Device = device;
        if (index == 1) Player2Device = device;

        Debug.Log($"<color=green>Manager :</color> Manette {device.displayName} liée au Joueur {index + 1}");
    }
}