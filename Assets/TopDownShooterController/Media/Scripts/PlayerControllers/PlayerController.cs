using UnityEngine;

/* Script to easy setup your own input configurations.
 * You can use the virtual joystick solution in this pack or use another solution.
 * Note: if you go to use joystick like a Xbox controller you need add this two
 * new axis to the input manager.
 * */

namespace TopDownShooter
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Scripts reference")] public MovementCharacterController MovCharController;
        public ShooterController ShooterController;
        public SwimmingController SwimmingController;

        [Header("Use mouse to shoot and rotate player")]
        public bool UseMouseToRotate = true;

        [Tooltip("This is the layer for the ground.")]
        public LayerMask GroundLayer;

        public float GetHorizontalValue()
        {
            return Input.GetAxis("Horizontal");
        }

        public float GetVerticalValue()
        {
            return Input.GetAxis("Vertical");
        }

        public float GetHorizontal2Value()
        {
            if (UseMouseToRotate)
            {
                return GetMouseDirection().x;
            }

            //if you go to use a joystick like a Xbox joystick replace "GetMouseDirection().x" put you new Horizontal axis in this place and uncheck mouse and virtual joystick like this:
            //return Input.GetAxis("NewControlAxis");
            return Input.GetAxis("Horizontal");
        }

        public float GetVertical2Value()
        {
            if (UseMouseToRotate)
            {
                return GetMouseDirection().z;
            }

            //if you go to use a joystick like a Xbox joystick replace "Input.GetAxis("Vertical")" put you new Vertical axis in this place and uncheck mouse and virtual joystick like this:
            //return Input.GetAxis("NewControlAxis");

            return Input.GetAxis("Vertical");
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

        public Vector3 GetMouseDirection()
        {
            if (Camera.main == null) return Vector3.zero;
            var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            var planeAtFeet = new Plane(Vector3.up, transform.position.y);

            //check if the player press mouse button and the ray hit the ground
            if (planeAtFeet.Raycast(mouseRay, out var groundHitDistance))
            {
                var hitPoint = mouseRay.GetPoint(groundHitDistance);
                var playerToMouse = hitPoint - transform.position;

                playerToMouse.y = 0f;

                return playerToMouse;
            }

            return Vector3.zero;
        }
    }
}
