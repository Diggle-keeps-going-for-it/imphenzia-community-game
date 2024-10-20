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

        [SerializeField] private float rotationSpeed = 0.6f;

        //private vars
        private CharacterController _controller;
        private Vector3 _velocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            _gravity = Gravity;
        }

        private void Update()
        {
            SetRunningAnimation(PlayerController.Movement.sqrMagnitude > 0);
        }

        private void FixedUpdate()
        {
            //get the input direction for the camera position.
            var forward = Camera.main.transform.TransformDirection(Vector3.forward);
            forward.y = 0f;
            forward = forward.normalized;
            var right = new Vector3(forward.z, 0.0f, -forward.x);

            var rawMovementInput = new Vector2(PlayerController.GetHorizontalValue(), PlayerController.GetVerticalValue());
            Vector2 cappedMovementInput = GetCappedMovementInput(rawMovementInput);

            var worldRelativeMovementInput = (cappedMovementInput.x * right + cappedMovementInput.y * forward);

            //move the player if no is active the slow fall(this avoid change the speed for the fall)
            if (_controller.enabled)
            {
                _controller.Move(Time.fixedDeltaTime * RunningSpeed * worldRelativeMovementInput);
            }

            RotateCharacter(worldRelativeMovementInput);
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
            transform.forward = Vector3.Lerp(transform.forward, newForward.normalized, rotationSpeed);
        }

        private Vector2 GetCappedMovementInput(Vector2 rawMovementInput)
        {
            var movementMagnitude = rawMovementInput.magnitude;
            var targetMovementMagnitude = Mathf.Min(rawMovementInput.magnitude, 1f);
            var targetMagnitudeScale = targetMovementMagnitude / movementMagnitude;
            var cappedMovementInput = rawMovementInput * targetMagnitudeScale;
            return cappedMovementInput;
        }

        //change the speed for the player
        public void ChangeSpeed(float speed)
        {
            RunningSpeed = speed;
        }

        //Animation

        #region Animator

        private void SetRunningAnimation(bool run)
        {
            PlayerAnimator.SetBool("Running", run);
        }

        #endregion
    }
}
