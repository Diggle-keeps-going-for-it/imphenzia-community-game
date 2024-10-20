using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace TopDownShooter
{
    public class MovementCharacterController : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("RunningSpeed")] public float runningSpeed = 5f;

        [SerializeField] [Range(0f, 1f)] private float lookDirectionDeadzone = 0.1f;
        [SerializeField] [Range(0f, 1f)] private float moveDirectionToLookDeadzone = 0f;

        [SerializeField] [FormerlySerializedAs("PlayerAnimator")] private Animator playerAnimator;

        [SerializeField] [FormerlySerializedAs("PlayerController")] private PlayerController playerController;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform rotatableTransform;

        [SerializeField] [Min(0f)] private float rotationSpeed = 360f;
        [SerializeField] [Min(0f)] private float acceleration = 10f;

        private void Update()
        {
            var worldRelativeMovementInput = GetWorldRelativeCappedMovementInput();
            RotateCharacter(worldRelativeMovementInput);
            SetRunningAnimation(playerController.Movement.sqrMagnitude > 0);
        }

        private void FixedUpdate()
        {
            var worldRelativeMovementInput = GetWorldRelativeCappedMovementInput();

            var lateralVelocity = body.velocity;
            lateralVelocity.y = 0f;
            var newLateralVelocity = Vector3.MoveTowards(lateralVelocity, runningSpeed * worldRelativeMovementInput, Time.fixedDeltaTime * acceleration);
            var newVelocity = newLateralVelocity + Vector3.up * body.velocity.y;

            body.velocity = newVelocity;
        }

        private void RotateCharacter(Vector3 movementInputInWorldSpace)
        {
            var lookDirection = playerController.GetWorldSpaceLookDirection(Camera.main, transform);
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
            var originalAngle = rotatableTransform.eulerAngles.y;
            var targetAngle = Mathf.Rad2Deg * Mathf.Atan2(newForward.x, newForward.z);
            var newAngle = Mathf.MoveTowardsAngle(originalAngle, targetAngle, rotationSpeed * Time.deltaTime);
            rotatableTransform.eulerAngles = Vector3.up * newAngle;
        }

        private Vector3 GetWorldRelativeCappedMovementInput()
        {
            var rawMovementInput = new Vector2(playerController.GetHorizontalValue(), playerController.GetVerticalValue());

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
            playerAnimator.SetBool("Running", run);
        }
    }
}
