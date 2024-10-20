using UnityEngine;
using UnityEngine.InputSystem;

/* Script to easy setup your own input configurations.
 * You can use the virtual joystick solution in this pack or use another solution.
 * Note: if you go to use joystick like a Xbox controller you need add this two
 * new axis to the input manager.
 * */

namespace TopDownShooter
{
    public class PlayerController : MonoBehaviour, PlayerControls.ITopdownActions
    {
        public MovementCharacterController MovCharController;
        public ShooterController ShooterController;

        private PlayerControls playerControls;

        public Vector2 Movement { get; private set; }
        private Vector2 LookDirection { get; set; }
        private Vector2 CursorPosition { get; set; }

        private void Awake()
        {
            playerControls = new();
            playerControls.Topdown.AddCallbacks(this);
        }

        private void OnEnable()
        {
            playerControls.Enable();
        }

        private void OnDisable()
        {
            playerControls.Disable();
        }

        public float GetHorizontalValue()
        {
            return Movement.x;
        }

        public float GetVerticalValue()
        {
            return Movement.y;
        }

        public Vector2 GetLookDirection(Camera camera, Transform player)
        {
            if (IsUsingKeyboardAndMouse())
            {
                var maybeMouseDirection = GetMouseDirection(camera, player);
                if (maybeMouseDirection is Vector2 mouseDirection)
                {
                    return mouseDirection;
                }
            }

            return LookDirection;
        }

        private bool IsUsingKeyboardAndMouse()
        {
            return true;
        }

        public bool GetGrabThrowValue()
        {
            return Input.GetButtonDown("GrabThrow");
        }

        public bool GetInteractValue()
        {
            return Input.GetButtonDown("Use");
        }

        public bool GetJumpValue()
        {
            return Input.GetKeyDown(KeyCode.Space);
        }

        public bool GetDashValue()
        {
            return Input.GetKeyDown(KeyCode.F);
        }

        public bool GetDropWeaponValue()
        {
            return Input.GetKeyDown(KeyCode.G);
        }

        public bool GetReloadWeaponValue()
        {
            return Input.GetKeyDown(KeyCode.R);
        }

        public Vector2? GetMouseDirection(Camera camera, Transform playerFeet)
        {
            if (camera == null) return null;

            var mouseRay = camera.ScreenPointToRay(CursorPosition);
            var planeAtFeet = new Plane(Vector3.up, -playerFeet.position.y);

            //check if the player press mouse button and the ray hit the ground
            if (planeAtFeet.Raycast(mouseRay, out var groundHitDistance))
            {
                var hitPoint = mouseRay.GetPoint(groundHitDistance);
                var playerToMouse = hitPoint - playerFeet.position;

                return new Vector2(playerToMouse.x, playerToMouse.z);
            }

            return null;
        }

        public void OnMovement(InputAction.CallbackContext context)
        {
            Movement = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            LookDirection = context.ReadValue<Vector2>();
        }

        public void OnCursorLook(InputAction.CallbackContext context)
        {
            CursorPosition = context.ReadValue<Vector2>();
        }
    }
}
