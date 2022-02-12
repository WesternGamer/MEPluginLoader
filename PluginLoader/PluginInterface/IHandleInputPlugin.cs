using System;

namespace MEPluginLoader.PluginInterface
{
    public interface IHandleInputPlugin : IPlugin, IDisposable
    {
        void HandleInput();
    }
}
