using System;

namespace MEPluginLoader.PluginInterface
{
    public interface IPlugin : IDisposable
    {
        void Init();

        void Update();
    }
}
