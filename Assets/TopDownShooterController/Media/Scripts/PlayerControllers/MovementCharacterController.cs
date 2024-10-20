using System;
using System.Collections;
using UnityEngine;

namespace TopDownShooter
{
    public class MovementCharacterController : MonoBehaviour
    {
        [Header("Player Controller Settings")] [Tooltip("Speed for the player.")]
        public float RunningSpeed = 5f;

        [Header("Speed when player is shooting")] [Range(0.2f, 1)] [Tooltip("This is the proportion of normal player speed while shooting their weapon.")]
        public float RunningShootSpeed;

        [Tooltip("Slope angle limit to slide.")]
        public float SlopeLimit = 45;

        [Range(0.1f, 0.9f)]
        public float SlideFriction = 0.3f;

        [Range(0, -100)]
        public float Gravity = -30f;

        [Range(0, 100)]
        public float MaxDownYVelocity = 15;

        [Tooltip("Can the user control the player?")]
        public bool CanControl = true;

        [SerializeField] [Range(0f, 1f)] private float lookDirectionDeadzone = 0.1f;
        [SerializeField] [Range(0f, 1f)] private float moveDirectionToLookDeadzone = 0f;

        public Vector3 DragForce;

        public Animator PlayerAnimator;

        public PlayerController PlayerController;
        [SerializeField] private CharacterController controller;

        [SerializeField] private float rotationSpeed = 0.6f;

        private void Update()
        {
            var worldRelativeMovementInput = GetWorldRelativeCappedMovementInput();
            RotateCharacter(worldRelativeMovementInput);
            SetRunningAnimation(PlayerController.Movement.sqrMagnitude > 0);
        }

        private void FixedUpdate()
        {
            var worldRelativeMovementInput = GetWorldRelativeCappedMovementInput();

            controller.Move(Time.fixedDeltaTime * RunningSpeed * worldRelativeMovementInput);
        }

        private void RotateCharacter(Vector3 movementInputInWorldSpace)
        {
            var lookDirection = PlayerController.GetWorldSpaceLookDirection(Camera.main, transform);
            if (lookDirection.sqrMagnitude > lookDirectionDeadzone)
            {
                RotateTowards(lookDirection);
                return;
            }

            if (movementInputInWorldSpace.sqrMagnitude > moveDirectionToLookDeadzone)
            {
                RotateTowards(movementInputInWorldSpace);
                return;
            }
        }

        private void RotateTowards(Vector3 newForward)
        {
            var originalAngle = transform.eulerAngles.y;
            var targetAngle = Mathf.Rad2Deg * Mathf.Atan2(newForward.x, newForward.z);
            var newAngle = Mathf.MoveTowardsAngle(originalAngle, targetAngle, rotationSpeed * Time.deltaTime);
            transform.eulerAngles = Vector3.up * newAngle;
        }

        private Vector3 GetWorldRelativeCappedMovementInput()
        {
            var rawMovementInput = new Vector2(PlayerController.GetHorizontalValue(), PlayerController.GetVerticalValue());

            if (rawMovementInput.sqrMagnitude == 0f)
            {
                // if there's no input then the following calculations include a divide-by-zero
                return Vector3.zero;
            }

            var forward = Camera.main.transform.TransformDirection(Vector3.forward);
            forward.y = 0f;
            forward = forward.normalized;
            var right = new Vector3(forward.z, 0.0f, -forward.x);

            var movementMagnitude = rawMovementInput.magnitude;
            var targetMovementMagnitude = Mathf.Min(rawMovementInput.magnitude, 1f);
            var targetMagnitudeScale = targetMovementMagnitude / movementMagnitude;
            var cappedMovementInput = rawMovementInput * targetMagnitudeScale;

            var worldRelativeMovementInput = (cappedMovementInput.x * right + cappedMovementInput.y * forward);

            return worldRelativeMovementInput;
        }

        private void SetRunningAnimation(bool run)
        {
            PlayerAnimator.SetBool("Running", run);
        }
    }
}
