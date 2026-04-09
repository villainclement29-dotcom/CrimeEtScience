using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class MinutiesNavigator : MonoBehaviour
{
    [Header("Navigation Principale")]
    public RectTransform selecteur;
    public RectTransform[] stations;
    public int playerIndex = 0; // 0 pour J1 (Gauche), 1 pour J2 (Droite)

    [Header("Menu de Sélection (Boutons)")]
    public GameObject menuUI;
    public Button[] optionsBoutons;
    public Color couleurSelection = Color.yellow;
    public Color couleurDefaut = Color.white;

    [Header("Scanner Global")]
    public RectTransform barreScanner;
    public RectTransform zoneEmpreinte;
    public float vitesseScanner = 300f;

    [Header("Sprites de Statut")]
    public Sprite spriteValide;
    public Sprite spriteErreur;
    public Sprite spriteRondAttente;

    public int[] solutionsCorrectes;

    private int currentIndex = 0;
    private int selectionMenuIndex = 0;
    private bool menuOuvert = false;
    private bool enCoursDeScan = false;
    private bool aGagne = false;
    private InputDevice playerDevice;
    private int[] choixJoueur;

    private bool stickUsed = false;
    private const float stickThreshold = 0.5f;

    void Start()
    {
        choixJoueur = new int[stations.Length];
        for (int i = 0; i < choixJoueur.Length; i++) choixJoueur[i] = -1;

        // On utilise directement playerIndex sans faire -1
        if (GlobalPlayerManager.Instance != null)
        {
            playerDevice = (playerIndex == 0) ?
                GlobalPlayerManager.Instance.Player1Device :
                GlobalPlayerManager.Instance.Player2Device;
        }

        if (menuUI) menuUI.SetActive(false);
        if (barreScanner) barreScanner.gameObject.SetActive(false);

        ActualiserPosition();
    }

    void Update()
    {
        if (playerDevice == null || enCoursDeScan || aGagne) return;

        if (!menuOuvert)
        {
            GererNavigationStations();
            if (CheckButtonA()) OuvrirMenu();
            if (CheckButtonX()) StartCoroutine(ProcedureScannerGlobal());
        }
        else
        {
            GererNavigationMenu();
            if (CheckButtonA()) ValiderChoix();
            if (CheckButtonB()) FermerMenu();
        }
    }

    IEnumerator ProcedureScannerGlobal()
    {
        if (barreScanner == null || zoneEmpreinte == null) yield break;

        enCoursDeScan = true;
        barreScanner.gameObject.SetActive(true);

        float haut = zoneEmpreinte.rect.height / 2;
        float bas = -zoneEmpreinte.rect.height / 2;

        Vector3 pos = barreScanner.localPosition;
        pos.y = haut;
        barreScanner.localPosition = pos;

        bool[] dejaValide = new bool[stations.Length];
        int nombreDeJustes = 0;

        while (barreScanner.localPosition.y > bas)
        {
            barreScanner.localPosition += Vector3.down * vitesseScanner * Time.deltaTime;

            for (int i = 0; i < stations.Length; i++)
            {
                if (!dejaValide[i] && barreScanner.localPosition.y <= stations[i].localPosition.y)
                {
                    bool estCorrect = AppliquerResultatScan(i);
                    if (estCorrect) nombreDeJustes++;
                    dejaValide[i] = true;
                }
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        barreScanner.gameObject.SetActive(false);
        enCoursDeScan = false;

        // VERIFICATION DE LA VICTOIRE
        if (nombreDeJustes == stations.Length && WinManager.Instance != null)
        {
            aGagne = true;
            // On passe directement l'index 0 ou 1
            WinManager.Instance.ShowVictory(playerIndex);
        }
    }

    bool AppliquerResultatScan(int index)
    {
        Transform rond = stations[index].Find("RondChoix");
        if (rond != null)
        {
            Image img = rond.GetComponent<Image>();
            bool estCorrect = (choixJoueur[index] == solutionsCorrectes[index]);
            img.sprite = estCorrect ? spriteValide : spriteErreur;
            img.color = Color.white;
            StartCoroutine(AnimerPop(rond));
            return estCorrect;
        }
        return false;
    }

    IEnumerator AnimerPop(Transform t)
    {
        float elapsed = 0;
        while (elapsed < 0.2f)
        {
            t.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, elapsed / 0.2f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    void GererNavigationStations()
    {
        Vector2 dir = Vector2.zero;

        if (playerDevice is Gamepad pad)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.magnitude > stickThreshold && !stickUsed)
            {
                dir = stick;
                stickUsed = true;
            }
            else if (stick.magnitude <= stickThreshold)
            {
                stickUsed = false;
            }

            // D-pad en fallback
            if (pad.dpad.right.wasPressedThisFrame) dir = Vector2.right;
            else if (pad.dpad.left.wasPressedThisFrame)  dir = Vector2.left;
            else if (pad.dpad.up.wasPressedThisFrame)    dir = Vector2.up;
            else if (pad.dpad.down.wasPressedThisFrame)  dir = Vector2.down;
        }
        else if (playerDevice is Keyboard kb)
        {
            if (playerIndex == 0)
            {
                if (kb.rightArrowKey.wasPressedThisFrame) dir = Vector2.right;
                else if (kb.leftArrowKey.wasPressedThisFrame)  dir = Vector2.left;
                else if (kb.upArrowKey.wasPressedThisFrame)    dir = Vector2.up;
                else if (kb.downArrowKey.wasPressedThisFrame)  dir = Vector2.down;
            }
            else
            {
                if (kb.dKey.wasPressedThisFrame) dir = Vector2.right;
                else if (kb.qKey.wasPressedThisFrame) dir = Vector2.left;
                else if (kb.zKey.wasPressedThisFrame) dir = Vector2.up;
                else if (kb.sKey.wasPressedThisFrame) dir = Vector2.down;
            }
        }

        if (dir != Vector2.zero)
        {
            int target = FindNearestInDirection(dir);
            if (target >= 0)
            {
                currentIndex = target;
                ActualiserPosition();
            }
        }
    }

    // Trouve la station la plus proche dans la direction donnée, en fonction
    // de la position réelle des RectTransform sur l'écran.
    int FindNearestInDirection(Vector2 dir)
    {
        Vector2 currentPos = stations[currentIndex].localPosition;
        int best = -1;
        float bestScore = float.MinValue;

        for (int i = 0; i < stations.Length; i++)
        {
            if (i == currentIndex) continue;
            Vector2 toStation = (Vector2)stations[i].localPosition - currentPos;
            float dot = Vector2.Dot(toStation.normalized, dir.normalized);

            // On ne considère que les stations dans un cône de 60° autour de la direction
            if (dot > 0.5f)
            {
                // Favorise l'alignement avec la direction, puis la proximité
                float score = dot / toStation.magnitude;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
        }
        return best;
    }

    void GererNavigationMenu()
    {
        bool u = false; bool d = false;
        if (playerDevice is Gamepad pad)
        {
            u = pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame;
            d = pad.dpad.down.wasPressedThisFrame || pad.leftStick.down.wasPressedThisFrame;
        }
        else if (playerDevice is Keyboard kb)
        {
            u = (playerIndex == 0) ? kb.upArrowKey.wasPressedThisFrame : kb.zKey.wasPressedThisFrame;
            d = (playerIndex == 0) ? kb.downArrowKey.wasPressedThisFrame : kb.sKey.wasPressedThisFrame;
        }
        if (u) { selectionMenuIndex = (selectionMenuIndex - 1 + optionsBoutons.Length) % optionsBoutons.Length; UpdateVisuelMenu(); }
        if (d) { selectionMenuIndex = (selectionMenuIndex + 1) % optionsBoutons.Length; UpdateVisuelMenu(); }
    }

    void ValiderChoix()
    {
        choixJoueur[currentIndex] = selectionMenuIndex;
        Transform rond = stations[currentIndex].Find("RondChoix");
        if (rond != null)
        {
            Image img = rond.GetComponent<Image>();
            if (spriteRondAttente != null) img.sprite = spriteRondAttente;
            img.color = Color.white;
        }
        FermerMenu();
    }

    void UpdateVisuelMenu()
    {
        for (int i = 0; i < optionsBoutons.Length; i++)
            optionsBoutons[i].image.color = (i == selectionMenuIndex) ? couleurSelection : couleurDefaut;
    }

    void ActualiserPosition()
    {
        if (selecteur && stations.Length > 0)
        {
            selecteur.localPosition = stations[currentIndex].localPosition;
            selecteur.sizeDelta = stations[currentIndex].sizeDelta;
        }
    }

    void OuvrirMenu() { menuOuvert = true; if (menuUI) menuUI.SetActive(true); UpdateVisuelMenu(); }
    void FermerMenu() { menuOuvert = false; if (menuUI) menuUI.SetActive(false); }

    bool CheckButtonA()
    {
        if (playerDevice is Gamepad p) return p.buttonSouth.wasPressedThisFrame;
        if (playerDevice is Keyboard k) return k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame;
        return false;
    }
    bool CheckButtonB()
    {
        if (playerDevice is Gamepad p) return p.buttonEast.wasPressedThisFrame;
        if (playerDevice is Keyboard k) return k.escapeKey.wasPressedThisFrame;
        return false;
    }
    bool CheckButtonX()
    {
        if (playerDevice is Gamepad p) return p.buttonWest.wasPressedThisFrame;
        if (playerDevice is Keyboard k) return k.xKey.wasPressedThisFrame;
        return false;
    }
}