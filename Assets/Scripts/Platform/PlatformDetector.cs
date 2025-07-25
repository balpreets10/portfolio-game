using System;

using UnityEngine;

/// <summary>
/// Platform detector implementation
/// </summary>
///

public class PlatformDetector : IPlatformDetector
{
    public GamePlatform CurrentPlatform { get; private set; }
    private bool isOverrideActive = false;

    public PlatformDetector()
    {
        DetectPlatform();
    }

    public event Action<GamePlatform> OnPlatformChanged;

    private void DetectPlatform()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        CurrentPlatform = GamePlatform.PC;
#elif UNITY_ANDROID || UNITY_IOS
            CurrentPlatform = GamePlatform.Mobile;
#elif UNITY_CONSOLE
            CurrentPlatform = GamePlatform.Console;
#elif UNITY_WEBGL
            CurrentPlatform = GamePlatform.PC; // WebGL treated as PC for simplicity
#else
            CurrentPlatform = GamePlatform.PC;
#endif

        // Additional runtime detection
        if (Application.isMobilePlatform)
        {
            CurrentPlatform = GamePlatform.Mobile;
        }

        if (ExtensionMethods.IsTouchSupported())
        {
            CurrentPlatform = GamePlatform.Mobile;
        }
    }

    public void SetPlatform(GamePlatform platform)
    {
        CurrentPlatform = platform;
        isOverrideActive = true;
    }

    public void ResetToAutoDetection()
    {
        DetectPlatform();
    }

    public bool IsOverrideActive()
    {
        return isOverrideActive;
    }
}

/// <summary>
/// Interface for platform detection
/// </summary>
public interface IPlatformDetector
{
    public GamePlatform CurrentPlatform { get; }

    /// <summary>
    /// Set platform override for testing
    /// </summary>
    /// <param name="platform">Platform to override with</param>
    void SetPlatform(GamePlatform platform);

    /// <summary>
    /// Reset to auto-detected platform
    /// </summary>
    void ResetToAutoDetection();

    /// <summary>
    /// Check if platform override is active
    /// </summary>
    /// <returns>True if override is active</returns>
    bool IsOverrideActive();

    /// <summary>
    /// Event fired when platform changes
    /// </summary>
    event Action<GamePlatform> OnPlatformChanged;
}

/// <summary>
/// Platform types supported by the camera system
/// </summary>
public enum GamePlatform
{
    PC,
    Mobile,
    Console,
    VR,
    None
}