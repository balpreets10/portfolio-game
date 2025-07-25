using Portfolio.CameraSystem;
using Portfolio.InputSystem;

using Reflex.Core;

using UnityEngine;

public class ProjectInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        // Register PlatformDetector as singleton
        builder.AddSingleton(typeof(PlatformDetector), typeof(IPlatformDetector));
    }
}