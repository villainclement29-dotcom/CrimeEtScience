using UnityEngine;
using System.Collections.Generic;

public class InteractionPointRandomizer : MonoBehaviour
{
    [Header("Interaction Points à déplacer")]
    public Transform[] interactionPoints;

    [Header("8 emplacements possibles sur la carte")]
    public Transform[] spawnSpots;

    void Start()
    {
        RandomizePositions();
    }

    void RandomizePositions()
    {
        if (interactionPoints == null || spawnSpots == null) return;
        if (spawnSpots.Length < interactionPoints.Length)
        {
            Debug.LogWarning("[InteractionPointRandomizer] Pas assez de spots pour placer tous les interaction points.");
            return;
        }

        // Copie et mélange aléatoire des spots (Fisher-Yates)
        List<Transform> shuffled = new List<Transform>(spawnSpots);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Transform tmp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = tmp;
        }

        // Place chaque interaction point sur un spot aléatoire unique
        for (int i = 0; i < interactionPoints.Length; i++)
        {
            if (interactionPoints[i] != null)
                interactionPoints[i].position = shuffled[i].position;
        }
    }
}
