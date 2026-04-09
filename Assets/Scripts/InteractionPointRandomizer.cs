using UnityEngine;
using System.Collections.Generic;

public class InteractionPointRandomizer : MonoBehaviour
{
    [Header("Interaction Points à déplacer")]
    public Transform[] interactionPoints;

    [Header("8 emplacements possibles sur la carte")]
    public Transform[] spawnSpots;

    // Persistants entre rechargements de scène
    private static Vector3[] savedPositions = null;
    private static HashSet<int> usedIndices = new HashSet<int>();

    public static void ResetSavedPositions()
    {
        savedPositions = null;
        usedIndices.Clear();
    }

    /// <summary>
    /// Appelé par SceneChanger avant de quitter vers un minijeu.
    /// Identifie le point par sa position dans savedPositions.
    /// </summary>
    public static void MarkPointUsed(Vector3 worldPosition)
    {
        if (savedPositions == null) return;
        for (int i = 0; i < savedPositions.Length; i++)
        {
            if (Vector3.Distance(savedPositions[i], worldPosition) < 0.01f)
            {
                usedIndices.Add(i);
                return;
            }
        }
    }

    void Start()
    {
        if (savedPositions != null && savedPositions.Length == interactionPoints.Length)
            RestorePositions();
        else
            RandomizeAndSave();

        ApplyUsedState();
    }

    void RestorePositions()
    {
        for (int i = 0; i < interactionPoints.Length; i++)
        {
            if (interactionPoints[i] != null)
                interactionPoints[i].position = savedPositions[i];
        }
    }

    void RandomizeAndSave()
    {
        if (interactionPoints == null || spawnSpots == null) return;
        if (spawnSpots.Length < interactionPoints.Length)
        {
            Debug.LogWarning("[InteractionPointRandomizer] Pas assez de spots pour placer tous les interaction points.");
            return;
        }

        List<Transform> shuffled = new List<Transform>(spawnSpots);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Transform tmp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = tmp;
        }

        savedPositions = new Vector3[interactionPoints.Length];
        for (int i = 0; i < interactionPoints.Length; i++)
        {
            if (interactionPoints[i] != null)
            {
                interactionPoints[i].position = shuffled[i].position;
                savedPositions[i] = shuffled[i].position;
            }
        }
    }

    void ApplyUsedState()
    {
        int activeCount = 0;
        for (int i = 0; i < interactionPoints.Length; i++)
        {
            if (interactionPoints[i] == null) continue;

            if (usedIndices.Contains(i))
                interactionPoints[i].gameObject.SetActive(false);
            else
                activeCount++;
        }

        if (activeCount == 0 && GameTimer.Instance != null)
            GameTimer.Instance.TriggerEnd();
    }
}
