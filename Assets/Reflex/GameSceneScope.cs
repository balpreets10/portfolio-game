using Portfolio.CameraSystem;
using Portfolio.InputSystem;
using Portfolio.InputSystem.Mobile;

using Reflex.Core;

using UnityEngine;

public class GameSceneScope : MonoBehaviour, IInstaller
{
    [Header("Settings Scriptable Objects")]
    [SerializeField] private CameraSettings cameraSettings;

    [SerializeField] private SpeedBoostSettings speedBoostSettings;

    [Header("Player")]
    [SerializeField] private PlayerMovementInput playerMovementHandler;

    [Header("UI")]
    [SerializeField] private VirtualJoystick joystick;

    [SerializeField] private ActionButton actionButton;

    [Header("Platform Testing")]
    [SerializeField] private bool overridePlatformDetection = false;

    [SerializeField] private GamePlatform testPlatform = GamePlatform.PC;

    private void Awake()
    {
        // Validate camera settings
        if (cameraSettings == null)
        {
            Debug.LogError("CameraSettings not assigned in ProjectInstaller!");
        }
    }

    public void InstallBindings(ContainerBuilder builder)
    {
        Debug.Log("Installing GameSceneScope bindings...");

        var platformDetector = Container.ProjectContainer.Single<IPlatformDetector>();

#if UNITY_EDITOR
        if (platformDetector != null && overridePlatformDetection)
        {
            platformDetector.SetPlatform(testPlatform);
            Debug.Log($"Platform overridden to: {testPlatform}");
        }
#endif

        // Register Settings
        if (cameraSettings != null)
            builder.AddSingleton(cameraSettings);
        if (speedBoostSettings != null)
            builder.AddSingleton(speedBoostSettings);

        if (joystick != null)
            builder.AddSingleton(joystick, typeof(IJoystick));

        if (actionButton != null)
            builder.AddSingleton(actionButton, typeof(IActionButton));

        // Register platform-specific input handlers
        RegisterPlatformRotationHandlers(builder);
        RegisterPlatformMovementHandlers(builder);
    }

    private void RegisterPlatformRotationHandlers(ContainerBuilder builder)
    {
        // Register all platform input handlers
        builder.AddTransient(typeof(PCRotationInputHandler));
        builder.AddTransient(typeof(MobileRotationInputHandler));

        // Register factory for platform-specific handlers
        builder.AddSingleton(typeof(PlatformRotationFactory), typeof(IPlatformRotationFactory));
    }

    private void RegisterPlatformMovementHandlers(ContainerBuilder builder)
    {
        // Register all platform input handlers
        builder.AddTransient(typeof(PCMovementInputHandler));
        builder.AddTransient(typeof(MobileMovementInputHandler));

        // Register factory for platform-specific handlers
        builder.AddSingleton(typeof(PlatformMovementFactory), typeof(IPlatformMovementFactory));
    }
}