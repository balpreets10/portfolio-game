using System;

namespace Portfolio.InputSystem
{
    /// <summary>
    /// Common input state flags shared across all input handlers
    /// </summary>
    [Flags]
    public enum CommonInputState
    {
        None = 0,
        Initialized = 1 << 0,
        Enabled = 1 << 1,
        InputActive = 1 << 2
    }
}

namespace Portfolio.InputSystem.PC
{
    /// <summary>
    /// PC Movement-specific input state flags
    /// </summary>
    [Flags]
    public enum MovementInputState
    {
        None = 0,
        FirstFrame = 1 << 0
    }

    /// <summary>
    /// PC Rotation-specific input state flags
    /// </summary>
    [Flags]
    public enum RotationInputState
    {
        None = 0,
        FirstFrame = 1 << 0,
        CursorLocked = 1 << 1,
        RotationActive = 1 << 2,
        CursorTransitioning = 1 << 3
    }
}

namespace Portfolio.InputSystem.Mobile
{
    /// <summary>
    /// Movement-specific input state flags
    /// </summary>
    [Flags]
    public enum MovementInputState
    {
        None = 0,
        MouseEmulation = 1 << 0  // Editor only
    }

    /// <summary>
    /// Rotation-specific input state flags
    /// </summary>
    [Flags]
    public enum RotationInputState
    {
        None = 0,
        TouchActive = 1 << 0,
        RotationActive = 1 << 1,
        MouseDown = 1 << 2,      // Editor only
        PinchMode = 1 << 3       // Editor only
    }
}