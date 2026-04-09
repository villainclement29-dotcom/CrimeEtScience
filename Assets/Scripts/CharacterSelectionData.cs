using UnityEngine;

[System.Serializable]
public class CharacterData
{
    public string characterName;
    public Sprite spriteCarousel; // sprite affiché dans le carousel du lobby
    public Sprite spriteDown;
    public Sprite spriteUp;
    public Sprite spriteLeft;
    public Sprite spriteRight;
    [Tooltip("Scale appliqué au GameObject du joueur en jeu (1 = taille normale)")]
    public float scale = 1f;
}

/// <summary>
/// Données persistantes de sélection de personnage, accessibles depuis n'importe quelle scène.
/// </summary>
public static class CharacterSelectionData
{
    public static int          J1CharacterIndex = -1;
    public static int          J2CharacterIndex = -1;
    public static CharacterData J1Data;
    public static CharacterData J2Data;

    public static void Reset()
    {
        J1CharacterIndex = -1;
        J2CharacterIndex = -1;
        J1Data = null;
        J2Data = null;
    }
}
