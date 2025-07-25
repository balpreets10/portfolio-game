using Portfolio.CameraSystem;
using Portfolio.InputSystem;

using Reflex.Core;

using UnityEngine;
using Portfolio.InputSystem.Mobile;

namespace Portfolio.InputSystem
{
    public interface IPlatformRotationFactory
    {
        IPlatformRotationInputHandler GetHandler(GamePlatform platform);
    }

    public class PlatformRotationFactory : IPlatformRotationFactory
    {
        private readonly Container _container;

        public PlatformRotationFactory(Container container)
        {
            _container = container;
        }

        public IPlatformRotationInputHandler GetHandler(GamePlatform platform)
        {
            return platform switch
            {
                GamePlatform.PC => _container.Single<PCRotationInputHandler>(),
                GamePlatform.Mobile => _container.Single<MobileRotationInputHandler>(),
                _ => _container.Single<PCRotationInputHandler>()
            };
        }
    }
}