using Portfolio.InputSystem;

using Reflex.Core;

using UnityEngine;
using Portfolio.InputSystem.Mobile;

namespace Portfolio.InputSystem
{
    public interface IPlatformMovementFactory
    {
        IPlatformMovementInputHandler GetHandler(GamePlatform platform);
    }

    public class PlatformMovementFactory : IPlatformMovementFactory
    {
        private readonly Container _container;

        public PlatformMovementFactory(Container container)
        {
            _container = container;
        }

        public IPlatformMovementInputHandler GetHandler(GamePlatform platform)
        {
            return platform switch
            {
                GamePlatform.PC => _container.Single<PCMovementInputHandler>(),
                GamePlatform.Mobile => _container.Single<MobileMovementInputHandler>(),
                _ => _container.Single<PCMovementInputHandler>()
            };
        }
    }
}