using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem; // On garde bien ça !
using UnityEngine;

namespace Cainos.PixelArtTopDown_Basic
{
    public class TopDownCharacterController : MonoBehaviour
    {
        public float speed;
        private Animator animator;
        private Rigidbody2D rb;
        private Vector2 moveInput; // Stocke la direction reçue

        private void Start()
        {
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
        }

        // Cette fonction est appelée par le composant "Player Input"
        // quand on bouge le joystick ou les touches configurées
        public void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        private void Update()
        {
            // On utilise moveInput au lieu des touches A, D, W, S
            Vector2 dir = moveInput;

            // Logique d'animation (on garde tes IDs de direction)
            if (dir.x < 0) animator.SetInteger("Direction", 3);      // Gauche
            else if (dir.x > 0) animator.SetInteger("Direction", 2); // Droite

            if (dir.y > 0) animator.SetInteger("Direction", 1);      // Haut
            else if (dir.y < 0) animator.SetInteger("Direction", 0); // Bas

            animator.SetBool("IsMoving", dir.magnitude > 0);

            // Application du mouvement
            rb.linearVelocity = speed * dir;
        }
    }
}