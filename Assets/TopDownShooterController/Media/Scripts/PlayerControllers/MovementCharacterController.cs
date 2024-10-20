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

        //Input.
        public float Horizontal;
        public float Vertical;
        public float Horizontal2;
        public float Vertical2;

        //private vars
        private CharacterController _controller;
        private Vector3 _velocity;

        //temporal vars
        private float _gravity;

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
            //capture input from direct input
            //this is for normal movement
            Horizontal = PlayerController.GetHorizontalValue();
            Vertical = PlayerController.GetVerticalValue();

            //if player can control the character
            if (!CanControl)
            {
                Horizontal = 0;
                Vertical = 0;
            }

            //set running animation
            SetRunningAnimation((Math.Abs(Horizontal) > 0 || Math.Abs(Vertical) > 0));
        }

        private void FixedUpdate()
        {
            //get the input direction for the camera position.
            var forward = Camera.main.transform.TransformDirection(Vector3.forward);
            forward.y = 0f;
            forward = forward.normalized;
            var right = new Vector3(forward.z, 0.0f, -forward.x);

            Vector2 cappedMovementInput = GetCappedMovementInput();

            var worldRelativeMovementInput = (cappedMovementInput.x * right + cappedMovementInput.y * forward);

            //move the player if no is active the slow fall(this avoid change the speed for the fall)
            if (_controller.enabled)
            {
                _controller.Move(Time.fixedDeltaTime * RunningSpeed * worldRelativeMovementInput);
            }

            RotateCharacter();

            //gravity force
            if (_velocity.y >= -MaxDownYVelocity)
            {
                _velocity.y += Gravity * Time.fixedDeltaTime;
            }

            _velocity.x /= 1 + DragForce.x * Time.fixedDeltaTime;
            _velocity.y /= 1 + DragForce.y * Time.fixedDeltaTime;
            _velocity.z /= 1 + DragForce.z * Time.fixedDeltaTime;

            if (_controller.enabled)
            {
                _controller.Move(_velocity * Time.fixedDeltaTime);
            }
        }

        private void RotateCharacter()
        {
            var lookDirection = PlayerController.GetLookDirection(Camera.main, transform);
            if (lookDirection.sqrMagnitude > lookDirectionDeadzone)
            {
                RotateTowards(new Vector3(lookDirection.x, 0f, lookDirection.y));
            }

            var playerVelocity = new Vector3(PlayerController.GetHorizontalValue(), 0f, PlayerController.GetVerticalValue());
            if (playerVelocity.sqrMagnitude > moveDirectionToLookDeadzone)
            {
                RotateTowards(playerVelocity);
            }
        }

        private void RotateTowards(Vector3 newForward)
        {
            transform.forward = Vector3.Lerp(transform.forward, newForward, 0.6f);
        }

        private Vector2 GetCappedMovementInput()
        {
            var rawMovementInput = new Vector2(Horizontal, Vertical);
            var movementMagnitude = rawMovementInput.magnitude;
            var targetMovementMagnitude = Mathf.Min(rawMovementInput.magnitude, 1f);
            var targetMagnitudeScale = targetMovementMagnitude / movementMagnitude;
            var cappedMovementInput = rawMovementInput * targetMagnitudeScale;
            return cappedMovementInput;
        }

        //This check how much the player are pushing the fire stick
        public bool TensionFoRightStickLowerThan(float value)
        {
            return (Mathf.Abs(PlayerController.MovCharController.Horizontal2) > value ||
                    Mathf.Abs(PlayerController.MovCharController.Vertical2) > value);
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

        #region Coroutine

        //Use this to deactivate te player control for a period of time.
        public IEnumerator DeactivatePlayerControlByTime(float time)
        {
            _controller.enabled = false;
            CanControl = false;
            yield return new WaitForSeconds(time);
            CanControl = true;
            _controller.enabled = true;
        }

        #endregion
    }
}
