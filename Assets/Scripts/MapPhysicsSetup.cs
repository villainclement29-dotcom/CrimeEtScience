using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Ajoute automatiquement Rigidbody2D + TilemapCollider2D (composite merge) + CompositeCollider2D
/// sur chaque Tilemap du prefab map, sauf "rugs" et "ground".
/// S'auto-détruit après l'initialisation.
/// </summary>
public class MapPhysicsSetup : MonoBehaviour
{
    private static readonly string[] EXCLUDED = { "rugs", "ground" };

    void Awake()
    {
        Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>(true);

        foreach (Tilemap tm in tilemaps)
        {
            string name = tm.gameObject.name.ToLower();

            bool excluded = false;
            foreach (string ex in EXCLUDED)
                if (name == ex) { excluded = true; break; }

            if (excluded) continue;

            GameObject go = tm.gameObject;

            // Rigidbody2D statique (nécessaire pour CompositeCollider2D)
            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            // TilemapCollider2D avec Composite Merge
            TilemapCollider2D tc = go.GetComponent<TilemapCollider2D>();
            if (tc == null) tc = go.AddComponent<TilemapCollider2D>();
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;

            // CompositeCollider2D
            if (go.GetComponent<CompositeCollider2D>() == null)
                go.AddComponent<CompositeCollider2D>();

            Debug.Log($"[MapPhysics] Colliders ajoutés sur : {go.name}");
        }

        Destroy(this);
    }
}
