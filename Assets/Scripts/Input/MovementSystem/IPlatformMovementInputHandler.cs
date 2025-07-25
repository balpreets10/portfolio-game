using System;

using UnityEngine;

namespace Portfolio.InputSystem
{
    public interface IPlatformMovementInputHandler
    {
        /// <summary>
        /// Event fired when movement input is detected
        /// </summary>
        event Action<Vector2> OnMovementInput;

        /// <summary>
        /// Event fired when speed boost is requested
        /// </summary>
        event Action<BoostData> OnSpeedBoostRequested;

        /// <summary>
        /// Initialize the input handler
        /// </summary>
        void Initialize();

        /// <summary>
        /// Update input processing (called per frame)
        /// </summary>
        void UpdateInput();

        /// <summary>
        /// Enable or disable input processing
        /// </summary>
        /// <param name="enabled">Input processing state</param>
        void SetEnabled(bool enabled);

        /// <summary>
        /// Get if input is currently active
        /// </summary>
        /// <returns>True if input is being processed</returns>
        bool IsInputActive();

        /// <summary>
        /// Block input for a specified duration
        /// </summary>
        /// <param name="duration">Duration to block input</param>
        void BlockInput(float duration);

        /// <summary>
        /// Check if input is currently blocked
        /// </summary>
        /// <returns>True if input is blocked</returns>
        bool IsInputBlocked();

        /// <summary>
        /// Cleanup resources when switching platforms
        /// </summary>
        void Cleanup();
    }
}