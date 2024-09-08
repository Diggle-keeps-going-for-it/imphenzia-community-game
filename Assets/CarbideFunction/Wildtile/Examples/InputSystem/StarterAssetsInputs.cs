using UnityEngine;

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		public Vector2 look;
		public Vector2 mousePosition;
		public bool isLookModifierHeld;
		public bool isDeleteModifierHeld;

        public delegate void OnEvent();
        public OnEvent createTile;
        public OnEvent deleteTile;

        private void Update()
        {
            look = GetLookInput();
            isLookModifierHeld = IsLookModifierHeld();
            isDeleteModifierHeld = IsDeleteModifierHeld();

            if (IsBuildPressedThisFrame())
            {
                OnBuildPressed();
            }
        }

        private void OnBuildPressed()
        {
            if (isDeleteModifierHeld)
            {
                deleteTile?.Invoke();
            }
            else
            {
                createTile?.Invoke();
            }
        }

        private Vector2 GetLookInput()
        {
            var newMousePosition = (Vector2)Input.mousePosition;
            var mouseDelta = newMousePosition - mousePosition;
            mousePosition = newMousePosition;

            return mouseDelta;
        }

        private bool IsLookModifierHeld()
        {
            return Input.GetKey(KeyCode.Mouse1);
        }

        private bool IsDeleteModifierHeld()
        {
            return Input.GetKey(KeyCode.LeftControl)
                || Input.GetKey(KeyCode.RightCommand)
                || Input.GetKey(KeyCode.LeftCommand);
        }

        private bool IsBuildPressedThisFrame()
        {
            return Input.GetKeyDown(KeyCode.Mouse0);
        }
	}
}
