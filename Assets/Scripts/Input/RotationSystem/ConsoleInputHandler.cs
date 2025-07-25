using System;

using Reflex.Attributes;

using UnityEngine;

namespace Portfolio.InputSystem
{
    public class ConsoleInputHandler// : IPlatformInputHandler
    {
        public event Action<Vector2> OnRotationInput;

        public bool IsInputActive;// => GetJoystickInput().magnitude > _settings.RotationDeadzone;

        private float _sensitivity = 2f;

        public void ProcessInput()
        {
            var input = GetJoystickInput();

            if (input.magnitude > 0)
            {
                OnRotationInput?.Invoke(input * _sensitivity);
            }
        }

        private Vector2 GetJoystickInput()
        {
            return new Vector2(
                Input.GetAxis("RightStickX"),
                Input.GetAxis("RightStickY")
            );
        }

        public void SetSensitivity(float sensitivity)
        {
            _sensitivity = sensitivity;
        }
    }
}