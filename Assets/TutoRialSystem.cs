using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class TutorialReadySystem : MonoBehaviour
{
    [Header("UI Status")]
    public TextMeshProUGUI statusJ1;
    public TextMeshProUGUI statusJ2;

    [Header("Navigation")]
    public string nextSceneName;
    public float delayAfterReady = 1.5f;

    private bool isJ1Ready = false;
    private bool isJ2Ready = false;
    private bool isStarting = false;

    void Update()
    {
        if (isStarting) return;

        CheckInputs();
        UpdateUI();

        if (isJ1Ready && isJ2Ready)
        {
            StartCoroutine(StartGameRoutine());
        }
    }

    void CheckInputs()
    {
        // On récupère les manettes stockées dans ton Manager Global
        InputDevice dev1 = GlobalPlayerManager.Instance.Player1Device;
        InputDevice dev2 = GlobalPlayerManager.Instance.Player2Device;

        // Lecture J1
        if (!isJ1Ready && dev1 != null)
        {
            if (CheckButtonSouth(dev1)) isJ1Ready = true;
        }

        // Lecture J2
        if (!isJ2Ready && dev2 != null)
        {
            if (CheckButtonSouth(dev2)) isJ2Ready = true;
        }
    }

    // Fonction utilitaire pour vérifier le bouton A/Sud sur n'importe quel type de device
    bool CheckButtonSouth(InputDevice device)
    {
        if (device is Gamepad pad) return pad.buttonSouth.wasPressedThisFrame;
        if (device is Keyboard kb) return kb.spaceKey.wasPressedThisFrame; // Fallback clavier
        return false;
    }

    void UpdateUI()
    {
        if (statusJ1) statusJ1.text = isJ1Ready ? "<color=green>PRÊT</color>" : "<color=red>APPUYEZ SUR A</color>";
        if (statusJ2) statusJ2.text = isJ2Ready ? "<color=green>PRÊT</color>" : "<color=red>APPUYEZ SUR A</color>";
    }

    IEnumerator StartGameRoutine()
    {
        isStarting = true;
        Debug.Log("<color=yellow>[Tutorial]</color> Les deux joueurs sont prêts !");

        // Optionnel : Un petit son ou un effet visuel ici
        yield return new WaitForSeconds(delayAfterReady);

        SceneManager.LoadScene(nextSceneName);
    }
}