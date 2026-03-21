using UnityEngine;
using UnityEngine.Rendering;

namespace Cainos.PixelArtTopDown_Basic
{
    public class StairsLayerTrigger : MonoBehaviour
    {
        public Direction direction;

        [Header("Étage du HAUT (Sortie haute)")]
        public string physicsLayerUpper = "Floor2"; // Le layer physique (ex: Floor 2)
        public string sortingLayerUpper = "Entities"; // Reste Entities

        [Space]
        [Header("Étage du BAS (Sortie basse)")]
        public string physicsLayerLower = "Default"; // Repasse en Default
        public string sortingLayerLower = "Entities"; // Reste Entities

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            // Logique de détection de position pour savoir si on "monte" ou "descend"
            if (direction == Direction.South && other.transform.position.y < transform.position.y)
                UpdatePlayer(other.gameObject, physicsLayerUpper, sortingLayerUpper);
            else if (direction == Direction.West && other.transform.position.x < transform.position.x)
                UpdatePlayer(other.gameObject, physicsLayerUpper, sortingLayerUpper);
            else if (direction == Direction.East && other.transform.position.x > transform.position.x)
                UpdatePlayer(other.gameObject, physicsLayerUpper, sortingLayerUpper);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            if (direction == Direction.South && other.transform.position.y < transform.position.y)
                UpdatePlayer(other.gameObject, physicsLayerLower, sortingLayerLower);
            else if (direction == Direction.West && other.transform.position.x < transform.position.x)
                UpdatePlayer(other.gameObject, physicsLayerLower, sortingLayerLower);
            else if (direction == Direction.East && other.transform.position.x > transform.position.x)
                UpdatePlayer(other.gameObject, physicsLayerLower, sortingLayerLower);
        }

        private void UpdatePlayer(GameObject target, string physLayerName, string sortLayerName)
        {
            // 1. CHANGEMENT DU LAYER (Le menu tout en haut de l'objet)
            int layerID = LayerMask.NameToLayer(physLayerName);
            if (layerID != -1)
            {
                target.layer = layerID;
                // Si ton joueur a des enfants (bras, jambes) qui ont aussi des colliders :
                foreach (Transform child in target.transform) child.gameObject.layer = layerID;
            }

            // 2. MAINTIEN DU SORTING LAYER (Visuel)
            SortingGroup sg = target.GetComponent<SortingGroup>();
            if (sg != null)
            {
                sg.sortingLayerName = sortLayerName;
            }
            else
            {
                SpriteRenderer[] srs = target.GetComponentsInChildren<SpriteRenderer>();
                foreach (SpriteRenderer sr in srs) sr.sortingLayerName = sortLayerName;
            }
        }

        public enum Direction { North, South, West, East }
    }
}