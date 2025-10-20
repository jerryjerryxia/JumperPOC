using UnityEngine;

namespace Tests.Helpers
{
    /// <summary>
    /// Mock InputManager for testing components that depend on input.
    /// Allows programmatic control of input events without Unity's Input System.
    /// </summary>
    public class MockInputManager
    {
        // Input events matching InputManager interface
        public System.Action OnJumpPressed;
        public System.Action OnJumpReleased;
        public System.Action OnDashPressed;
        public System.Action OnAttackPressed;
        public System.Action<Vector2> OnMoveInput;

        // Input state properties
        public Vector2 MoveInput { get; set; }
        public bool JumpPressed { get; set; }
        public bool JumpHeld { get; set; }
        public bool DashPressed { get; set; }
        public bool AttackPressed { get; set; }

        /// <summary>
        /// Simulate move input change
        /// </summary>
        public void SimulateMoveInput(Vector2 input)
        {
            MoveInput = input;
            OnMoveInput?.Invoke(input);
        }

        /// <summary>
        /// Simulate jump button press
        /// </summary>
        public void SimulateJumpPress()
        {
            JumpPressed = true;
            JumpHeld = true;
            OnJumpPressed?.Invoke();
        }

        /// <summary>
        /// Simulate jump button release
        /// </summary>
        public void SimulateJumpRelease()
        {
            JumpPressed = false;
            JumpHeld = false;
            OnJumpReleased?.Invoke();
        }

        /// <summary>
        /// Simulate dash button press
        /// </summary>
        public void SimulateDashPress()
        {
            DashPressed = true;
            OnDashPressed?.Invoke();
        }

        /// <summary>
        /// Simulate attack button press
        /// </summary>
        public void SimulateAttackPress()
        {
            AttackPressed = true;
            OnAttackPressed?.Invoke();
        }

        /// <summary>
        /// Reset all input state
        /// </summary>
        public void ResetInput()
        {
            MoveInput = Vector2.zero;
            JumpPressed = false;
            JumpHeld = false;
            DashPressed = false;
            AttackPressed = false;
        }
    }
}
