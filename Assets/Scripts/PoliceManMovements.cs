using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users; // Nécessaire pour le couplage de périphériques
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace Cainos.PixelArtTopDown_Basic
{
    public class PoliceManMovements : MonoBehaviour
    {
        [Header("Configuration")]
        public int playerId = 0; // 0 pour le joueur de gauche, 1 pour celui de droite
        public float speed = 3f;

        [Header("Sprites de direction")]
        public Sprite spriteUp;
        public Sprite spriteDown;
        public Sprite spriteLeft;
        public Sprite spriteRight;

        [Header("Système de Lampe Torche")]
        public GameObject flashlight;

        [Tooltip("Glisse ici les 4 objets vides 'Anchor' créés sous le joueur")]
        public Transform anchorUp;
        public Transform anchorDown;
        public Transform anchorLeft;
        public Transform anchorRight;

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Vector2 moveInput;
        private PlayerInput playerInput;

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
            playerInput = GetComponent<PlayerInput>();

            // --- LOGIQUE D'ASSIGNATION DE LA MANETTE ---
            if (playerInput != null && GlobalPlayerManager.Instance != null)
            {
                // On récupère le périphérique stocké dans le Manager selon l'ID
                InputDevice targetDevice = (playerId == 0) ?
                    GlobalPlayerManager.Instance.Player1Device :
                    GlobalPlayerManager.Instance.Player2Device;

                if (targetDevice != null)
                {
                    // 1. On retire toutes les manettes assignées par défaut
                    playerInput.user.UnpairDevices();

                    // 2. On "marie" spécifiquement ce personnage avec la bonne manette
                    InputUser.PerformPairingWithDevice(targetDevice, playerInput.user);

                    Debug.Log($"<color=magenta>[PoliceMovements]</color> Joueur {playerId} lié à : {targetDevice.displayName}");
                }
                else
                {
                    Debug.LogWarning($"<color=red>[PoliceMovements]</color> Aucune manette trouvée pour le Joueur {playerId} dans le GlobalPlayerManager !");
                }
            }

            // Désactive la gravité pour le top-down
            if (rb != null) rb.gravityScale = 0;

            // Positionnement initial de la lampe
            if (flashlight != null && anchorDown != null)
            {
                flashlight.transform.position = anchorDown.position;
                flashlight.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
        }

        // Action déclenchée par le bouton 'A' (Input System Message)
        public void OnPhoto(InputValue value)
        {
            if (flashlight != null && value.isPressed)
            {
                flashlight.SetActive(!flashlight.activeSelf);
            }
        }

        // Action de mouvement (Input System Message)
        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        private void Update()
        {
            UpdateSpriteAndFlashlight(moveInput);

            if (rb != null)
            {
                // Note: 'linearVelocity' est pour les versions récentes d'Unity (2023+). 
                // Si tu es sur une version plus ancienne, utilise : rb.velocity = moveInput * speed;
                rb.linearVelocity = moveInput * speed;
            }
        }

        private void UpdateSpriteAndFlashlight(Vector2 dir)
        {
            if (dir.magnitude < 0.1f) return;

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                if (dir.x > 0)
                {
                    sr.sprite = spriteRight;
                    AttachFlashlight(anchorRight, new Vector3(0, 0, -90));
                }
                else
                {
                    sr.sprite = spriteLeft;
                    AttachFlashlight(anchorLeft, new Vector3(0, 0, 90));
                }
            }
            else
            {
                if (dir.y > 0)
                {
                    sr.sprite = spriteUp;
                    AttachFlashlight(anchorUp, new Vector3(0, 0, 0));
                }
                else
                {
                    sr.sprite = spriteDown;
                    AttachFlashlight(anchorDown, new Vector3(0, 0, 180));
                }
            }
        }

        private void AttachFlashlight(Transform anchor, Vector3 rotationOffset)
        {
            if (flashlight == null || anchor == null) return;
            flashlight.transform.position = anchor.position;
            flashlight.transform.rotation = Quaternion.Euler(rotationOffset);
        }
    }
}