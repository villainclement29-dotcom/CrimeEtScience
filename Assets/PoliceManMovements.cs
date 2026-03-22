using UnityEngine;
using UnityEngine.InputSystem; 

namespace Cainos.PixelArtTopDown_Basic
{
    public class PoliceManMovements : MonoBehaviour
    {
        public float speed = 5f;
        
        [Header("Sprites de direction")]
        public Sprite spriteUp;
        public Sprite spriteDown;
        public Sprite spriteLeft;
        public Sprite spriteRight;

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Vector2 moveInput; 

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();

            // Sécurité pour le Top-Down
            if (rb != null) rb.gravityScale = 0;
        }

        // Cette fonction reçoit le signal du composant "Player Input"
        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        private void Update()
        {
            // On change le sprite uniquement si on a une direction
            UpdateSprite(moveInput);

            // Application du mouvement (Unity 6 utilise linearVelocity)
            rb.linearVelocity = moveInput * speed;
        }

        private void UpdateSprite(Vector2 dir)
        {
            // Si on ne bouge pas, on ne change rien (le perso garde son dernier regard)
            if (dir.magnitude < 0.1f) return;

            // Logique de priorité pour le changement de sprite
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) // Mouvement horizontal dominant
            {
                if (dir.x > 0) sr.sprite = spriteRight;
                else if (dir.x < 0) sr.sprite = spriteLeft;
            }
            else // Mouvement vertical dominant
            {
                if (dir.y > 0) sr.sprite = spriteUp;
                else if (dir.y < 0) sr.sprite = spriteDown;
            }
        }
    }
}