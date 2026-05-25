using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using System.Collections;

namespace Cainos.PixelArtTopDown_Basic
{
    public class PoliceManMovements : MonoBehaviour
    {
        [Header("Configuration")]
        public int playerId = 0; 
        public float speed = 3f;

        [Header("Bruits de Pas")]
        public AudioClip[] footstepClips;   
        [Range(0f, 1f)] public float footstepVolume = 0.4f;
        public float footstepInterval = 0.35f; 
        [Range(0f, 0.3f)] public float pitchVariation = 0.15f;
        [Range(0f, 0.2f)] public float volumeVariation = 0.1f;
        private AudioSource footstepSource;
        private float footstepTimer = 0f;
        private int lastClipIndex = -1;

        [Header("Système de Lampe Torche")]
        public GameObject flashlight;
        public Transform anchorUp;
        public Transform anchorDown;
        public Transform anchorLeft;
        public Transform anchorRight;

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private Vector2 moveInput;
        private PlayerInput playerInput;

        private Animator _animator;
        private const string _horizontal = "Horizontal";
        private const string _vertical = "Vertical";
        private const string _lastHorizontal = "lastHorizontal";
        private const string _lastVertical = "lastVertical";


        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
            playerInput = GetComponent<PlayerInput>();

            // --- LIAISON MANETTE ---
            if (playerInput != null && GlobalPlayerManager.Instance != null)
            {
                InputDevice targetDevice = (playerId == 0) ?
                    GlobalPlayerManager.Instance.Player1Device :
                    GlobalPlayerManager.Instance.Player2Device;

                if (targetDevice != null)
                {
                    playerInput.user.UnpairDevices();
                    InputUser.PerformPairingWithDevice(targetDevice, playerInput.user);
                }
            }

            // AudioSource
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;

            if (rb != null) rb.gravityScale = 0;

            // --- CHARGEMENT DES DONNÉES JOUEUR ---
            // On ne change PAS le sr.sprite ici pour laisser l'Animator respirer.
            // L'Animator de chaque joueur est figé sur sa feuille de sprites (terrain ou labo),
            // quel que soit le personnage choisi au Lobby. Pour éviter des tailles incohérentes
            // (ex: feuille terrain multipliée par le scale du labo), on normalise la taille
            // visuelle d'après la hauteur réelle du sprite courant.
            const float TARGET_HEIGHT = 1.4f;
            float normalized = 1.2f;
            if (sr != null && sr.sprite != null)
            {
                float h = sr.sprite.bounds.size.y;
                if (h > 0.01f) normalized = TARGET_HEIGHT / h;
            }
            transform.localScale = Vector3.one * normalized;

            // Position initiale lampe
            if (flashlight != null && anchorDown != null)
            {
                flashlight.transform.position = anchorDown.position;
                flashlight.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
        }

        public void OnPhoto(InputValue value)
        {
            if (flashlight != null && value.isPressed)
            {
                flashlight.SetActive(!flashlight.activeSelf);
            }
        }

        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        private void Update()
        {
            UpdateFlashlightDirection(moveInput);
            UpdateFootsteps();

            if (rb != null)
            {
                // Utilise .velocity si tu es sur une version d'Unity plus ancienne que 2023
                rb.linearVelocity = moveInput * speed;
            }
            _animator.SetFloat(_horizontal, moveInput.x);
            _animator.SetFloat(_vertical, moveInput.y);
            if (moveInput != Vector2.zero)
            {
                _animator.SetFloat(_lastHorizontal, moveInput.x);
                _animator.SetFloat(_lastVertical, moveInput.y);
            }
        }

        private void UpdateFootsteps()
        {
            if (footstepSource == null || footstepClips == null || footstepClips.Length == 0) return;

            if (moveInput.magnitude > 0.1f)
            {
                footstepTimer -= Time.deltaTime;
                if (footstepTimer <= 0f)
                {
                    PlayFootstep();
                    footstepTimer = footstepInterval;
                }
            }
            else { footstepTimer = 0f; }
        }

        private void PlayFootstep()
        {
            int index = lastClipIndex;
            if (footstepClips.Length > 1)
            {
                while (index == lastClipIndex)
                    index = Random.Range(0, footstepClips.Length);
            }
            else { index = 0; }

            lastClipIndex = index;
            AudioClip clip = footstepClips[index];
            if (clip == null) return;

            footstepSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            footstepSource.PlayOneShot(clip);
        }

        // On a renommé la fonction : elle ne gère QUE la lampe maintenant
        private void UpdateFlashlightDirection(Vector2 dir)
        {
            if (dir.magnitude < 0.1f) return;

            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                if (dir.x > 0) AttachFlashlight(anchorRight, new Vector3(0, 0, -90));
                else           AttachFlashlight(anchorLeft, new Vector3(0, 0, 90));
            }
            else
            {
                if (dir.y > 0) AttachFlashlight(anchorUp, new Vector3(0, 0, 0));
                else           AttachFlashlight(anchorDown, new Vector3(0, 0, 180));
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