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

        //get direction for the camera
        private Transform _cameraTransform;
        private Vector3 _forward;
        private Vector3 _right;

        //temporal vars
        private float _originalRunningSpeed;
        private float _gravity;
        private bool _invertedControl;
        private Vector3 _hitNormal;
        private Vector3 _move;

        public bool IsMoving => _move != Vector3.zero;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _originalRunningSpeed = RunningSpeed;
        }

        private void Start()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
            _gravity = Gravity;
        }

        private void Update()
        {
            //capture input from direct input
            //this is for normal movement
            Horizontal = PlayerController.GetHorizontalValue();
            Vertical = PlayerController.GetVerticalValue();

            //this invert controls 
            if (_invertedControl)
            {
                Horizontal *= -1;
                Vertical *= -1;
            }

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
            _forward = _cameraTransform.TransformDirection(Vector3.forward);
            _forward.y = 0f;
            _forward = _forward.normalized;
            _right = new Vector3(_forward.z, 0.0f, -_forward.x);

            Vector2 cappedMovementInput = GetCappedMovementInput();

            _move = (cappedMovementInput.x * _right + cappedMovementInput.y * _forward);

            //move the player if no is active the slow fall(this avoid change the speed for the fall)
            if (_controller.enabled)
            {
                _controller.Move(Time.fixedDeltaTime * RunningSpeed * _move);
            }

            if (_move != Vector3.zero)
            {
                transform.forward = Vector3.Lerp(transform.forward, _move, 0.6f);
            }

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

        public void ResetOriginalSpeed()
        {
            RunningSpeed = _originalRunningSpeed;
        }

        //change the speed for the player
        public void ChangeSpeed(float speed)
        {
            RunningSpeed = speed;
        }

        //change the speed for the player for a time period
        public void ChangeSpeedInTime(float speedPlus, float time)
        {
            StartCoroutine(ModifySpeedByTime(speedPlus, time));
        }

        //invert player control(like a confuse skill)
        public void InvertPlayerControls(float invertTime)
        {
            //check if not are already inverted
            if (!_invertedControl)
            {
                StartCoroutine(InvertControls(invertTime));
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            _hitNormal = hit.normal;
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

        //dash coroutine.
        private IEnumerator Dashing(float time)
        {
            CanControl = false;
            if (!_controller.isGrounded)
            {
                Gravity = 0;
                _velocity.y = 0;
            }

            //animate hear to true
            yield return new WaitForSeconds(time);
            CanControl = true;
            //animate hear to false
            Gravity = _gravity;
        }

        //modify speed by time coroutine.
        private IEnumerator ModifySpeedByTime(float speedPlus, float time)
        {
            if (RunningSpeed + speedPlus > 0)
            {
                RunningSpeed += speedPlus;
            }
            else
            {
                RunningSpeed = 0;
            }

            yield return new WaitForSeconds(time);
            RunningSpeed = _originalRunningSpeed;
        }

        private IEnumerator InvertControls(float invertTime)
        {
            yield return new WaitForSeconds(0.1f);
            _invertedControl = true;
            yield return new WaitForSeconds(invertTime);
            _invertedControl = false;
        }

        #endregion
    }
}
