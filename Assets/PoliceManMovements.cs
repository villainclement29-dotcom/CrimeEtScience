using UnityEngine;
using UnityEngine.InputSystem;

namespace Cainos.PixelArtTopDown_Basic
{
    public class PoliceManMovements : MonoBehaviour
    {
        [Header("Identification")]
        public int playerId = 0; // 0 pour J1, 1 pour J2

        public float speed = 5f;

        [Header("Sprites de direction")]
        public Sprite spriteUp;
        public Sprite spriteDown;
        public Sprite spriteLeft;
        public Sprite spriteRight;

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Vector2 moveInput;
        private PlayerInput playerInput; // Ajouté pour la reconnexion

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
            playerInput = GetComponent<PlayerInput>();

            // --- RECONNEXION À LA SESSION ---
            if (GlobalPlayerManager.Instance != null && playerInput != null)
            {
                InputDevice device = (playerId == 0) ?
                    GlobalPlayerManager.Instance.Player1Device :
                    GlobalPlayerManager.Instance.Player2Device;

                if (device != null)
                {
                    playerInput.SwitchCurrentControlScheme(device);
                    Debug.Log($"<color=green>[Police J{playerId}]</color> Connecté à {device.displayName}");
                }
            }

            if (rb != null) rb.gravityScale = 0;
        }

        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        private void Update()
        {
            UpdateSprite(moveInput);
            rb.linearVelocity = moveInput * speed;
        }

        private void UpdateSprite(Vector2 dir)
        {
            if (dir.magnitude < 0.1f) return;

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                if (dir.x > 0) sr.sprite = spriteRight;
                else if (dir.x < 0) sr.sprite = spriteLeft;
            }
            else
            {
                if (dir.y > 0) sr.sprite = spriteUp;
                else if (dir.y < 0) sr.sprite = spriteDown;
            }
        }
    }
}