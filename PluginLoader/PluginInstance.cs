﻿using MEPluginLoader.Data;
using MEPluginLoader.PluginInterface;
using System;
using System.Linq;
using System.Reflection;

namespace MEPluginLoader
{
    public class PluginInstance
    {
        private readonly Type mainType;
        public readonly PluginData data;
        private readonly Assembly mainAssembly;
        private IPlugin plugin;
        private IHandleInputPlugin inputPlugin;

        private PluginInstance(PluginData data, Assembly mainAssembly, Type mainType)
        {
            this.data = data;
            this.mainAssembly = mainAssembly;
            this.mainType = mainType;
        }

        public bool Instantiate()
        {
            try
            {
                plugin = (IPlugin)Activator.CreateInstance(mainType);
                inputPlugin = plugin as IHandleInputPlugin;
                return true;
            }
            catch (Exception e)
            {
                ThrowError($"Failed to instantiate {data} because of an error: {e}");
                return false;
            }
        }

        public bool Init()
        {
            if (plugin == null)
            {
                return false;
            }

            try
            {
                plugin.Init();
                return true;
            }
            catch (Exception e)
            {
                ThrowError($"Failed to initialize {data} because of an error: {e}");
                return false;
            }
        }

        /*public void RegisterSession(MySession session)
        {
            if (plugin != null)
            {
                try
                {
                    Type descType = typeof(MySessionComponentDescriptor);
                    int count = 0;
                    foreach (Type t in mainAssembly.GetTypes().Where(t => Attribute.IsDefined(t, descType)))
                    {
                        MySessionComponentBase comp = (MySessionComponentBase)Activator.CreateInstance(t);
                        session.RegisterComponent(comp, comp.UpdateOrder, comp.Priority);
                        count++;
                    }
                    if(count > 0)
                        LogFile.WriteLine($"Registered {count} session components from: {mainAssembly.FullName}");
                }
                catch (Exception e)
                {
                    ThrowError($"Failed to register {data} because of an error: {e}");
                }
            }
                
        }*/

        public bool Update()
        {
            if (plugin == null)
            {
                return false;
            }

            plugin.Update();
            return true;
        }

        public bool HandleInput()
        {
            if (plugin == null)
            {
                return false;
            }

            inputPlugin?.HandleInput();
            return true;
        }

        public void Dispose()
        {
            if (plugin != null)
            {
                try
                {
                    plugin.Dispose();
                    plugin = null;
                    inputPlugin = null;
                }
                catch (Exception e)
                {
                    data.Status = PluginStatus.Error;
                    LogFile.WriteLine($"Failed to dispose {data} because of an error: {e}");
                }
            }
        }

        private void ThrowError(string error)
        {
            LogFile.WriteLine(error);
            data.Error();
            Dispose();
        }

        public static bool TryGet(PluginData data, out PluginInstance instance)
        {
            instance = null;
            if (data.Status == PluginStatus.Error || !data.TryLoadAssembly(out Assembly a))
            {
                return false;
            }

            Type pluginType = a.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t));
            if (pluginType == null)
            {
                LogFile.WriteLine($"Failed to load {data} because it does not contain an IPlugin");
                data.Error();
                return false;
            }

            instance = new PluginInstance(data, a, pluginType);
            return true;
        }

        public override string ToString()
        {
            return data.ToString();
        }

    }
}