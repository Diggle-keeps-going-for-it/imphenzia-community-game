using UnityEngine;
using UnityEngine.InputSystem;

/* Script to easy setup your own input configurations.
 * You can use the virtual joystick solution in this pack or use another solution.
 * Note: if you go to use joystick like a Xbox controller you need add this two
 * new axis to the input manager.
 * */

namespace TopDownShooter
{
    public class PlayerController : MonoBehaviour
    {
        public MovementCharacterController MovCharController;

        [SerializeField] private string keyboardAndMouseControlSchemeName = "Keyboard and Mouse";

        public Vector2 Movement { get; private set; }
        private Vector2 LookDirection { get; set; }
        private Vector2 CursorPosition { get; set; }

        private bool isUsingKeyboardAndMouse = true;

        public float GetHorizontalValue()
        {
            return Movement.x;
        }

        public float GetVerticalValue()
        {
            return Movement.y;
        }

        public Vector3 GetWorldSpaceLookDirection(Camera camera, Transform player)
        {
            if (isUsingKeyboardAndMouse)
            {
                var maybeMouseDirection = GetMouseDirection(camera, player);
                if (maybeMouseDirection is Vector3 mouseDirection)
                {
                    return mouseDirection;
                }
            }

            var worldRight = camera.transform.right;
            var worldForward = camera.transform.forward;
            worldForward.y = 0f;
            worldForward.Normalize();

            var lookDirectionInWorldSpace = LookDirection.x * worldRight + LookDirection.y * worldForward;
            return lookDirectionInWorldSpace;
        }

        public bool GetDropWeaponValue()
        {
            return Input.GetKeyDown(KeyCode.G);
        }

        public bool GetReloadWeaponValue()
        {
            return Input.GetKeyDown(KeyCode.R);
        }

        public Vector3? GetMouseDirection(Camera camera, Transform playerFeet)
        {
            if (camera == null) return null;

            var mouseRay = camera.ScreenPointToRay(CursorPosition);
            var planeAtFeet = new Plane(Vector3.up, -playerFeet.position.y);

            //check if the player press mouse button and the ray hit the ground
            if (planeAtFeet.Raycast(mouseRay, out var groundHitDistance))
            {
                var hitPoint = mouseRay.GetPoint(groundHitDistance);
                var playerToMouse = hitPoint - playerFeet.position;

                return playerToMouse;
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

        public void OnControlsChanged(PlayerInput playerInput)
        {
            isUsingKeyboardAndMouse = playerInput.currentControlScheme == keyboardAndMouseControlSchemeName;
        }
    }
}
