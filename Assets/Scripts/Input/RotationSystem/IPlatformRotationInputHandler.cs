using System;

using UnityEngine;

namespace Portfolio.InputSystem
{
    /// <summary>
    /// Interface for platform-specific input handling
    /// </summary>
    public interface IPlatformRotationInputHandler
    {
        /// <summary>
        /// Event fired when rotation input is detected
        /// </summary>
        event Action<Vector2> OnRotationInput;

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
        /// Cleanup resources when switching platforms
        /// </summary>
        void Cleanup();
    }
}