using MEPluginLoader.Compiler;
using MEPluginLoader.Data;
using MEPluginLoader.GUI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using VRage.Engine;
using VRage.FileSystem;

namespace MEPluginLoader
{
    [System("ME Plugin Loader")]
    public class Main : EngineSystem
    {
        public static Main Instance;

        public PluginList List { get; set; }
        public PluginConfig Config { get; set; }
        public SplashScreen Splash { get; set; }

        private bool init;

        private readonly List<PluginInstance> plugins = new List<PluginInstance>();

        protected override void Init()
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();

                Splash = new SplashScreen();

                Instance = this;

                Cursor temp = Cursor.Current;
                Cursor.Current = Cursors.AppStarting;

                string pluginsDir = Path.GetFullPath(Path.Combine(MyFileSystem.ExePath, "Plugins"));
                Directory.CreateDirectory(pluginsDir);

                LogFile.Init(pluginsDir);
                LogFile.WriteLine("Starting - v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

                Splash.SetText("Finding references...");
                RoslynReferences.GenerateAssemblyList();

                AppDomain.CurrentDomain.AssemblyResolve += ResolveDependencies;

                Config = PluginConfig.Load(pluginsDir);
                List = new PluginList(pluginsDir, Config);

                Config.Init(List);

                Splash.SetText("Patching...");
                LogFile.WriteLine("Patching");
                new Harmony("MEPluginLoader").PatchAll(Assembly.GetExecutingAssembly());

                Splash.SetText("Instantiating plugins...");
                LogFile.WriteLine("Instantiating plugins");
                foreach (string id in Config)
                {
                    PluginData data = List[id];
                    if (data is GitHubPlugin github)
                    {
                        github.Init(pluginsDir);
                    }

                    if (PluginInstance.TryGet(data, out PluginInstance p))
                    {
                        plugins.Add(p);
                    }
                }

                InstantiatePlugins();

                sw.Stop();

                LogFile.WriteLine($"Finished startup. Took {sw.ElapsedMilliseconds}ms");
                Cursor.Current = temp;
            }
            catch (Exception ex)
            {
                LogFile.WriteLine($"CRITICAL: Unable to start due to exception: {ex}");
                throw ex;
            }
        }

        public void InstantiatePlugins()
        {
            LogFile.WriteLine($"Loading {plugins.Count} plugins");
            for (int i = plugins.Count - 1; i >= 0; i--)
            {
                PluginInstance p = plugins[i];
                if (!p.Instantiate())
                {
                    plugins.RemoveAtFast(i);
                }
            }
        }

        protected override void Start()
        {
            Splash.Delete();
            Splash = null;

            LogFile.WriteLine($"Initializing {plugins.Count} plugins");
            for (int i = plugins.Count - 1; i >= 0; i--)
            {
                PluginInstance p = plugins[i];
                if (!p.Init())
                {
                    plugins.RemoveAtFast(i);
                }
            }
            init = true;
        }

        public void Update()
        {
            if (init)
            {
                for (int i = plugins.Count - 1; i >= 0; i--)
                {
                    PluginInstance p = plugins[i];
                    if (!p.Update())
                    {
                        plugins.RemoveAtFast(i);
                    }
                }
            }

            HandleInput();
        }

        public void HandleInput()
        {
            if (init)
            {
                for (int i = plugins.Count - 1; i >= 0; i--)
                {
                    PluginInstance p = plugins[i];
                    if (!p.HandleInput())
                    {
                        plugins.RemoveAtFast(i);
                    }
                }
            }
        }

        protected override void Shutdown()
        {
            foreach (PluginInstance p in plugins)
            {
                try
                {
                    p.Dispose();
                }
                catch (Exception ex)
                {
                    LogFile.WriteLine($"An error occured while trying to unload {p.data} => {ex}", true);
                    MessageBox.Show(LoaderTools.GetMainForm(), $"An error occured while trying to unload {p.data}", "ME Plugin Loader", MessageBoxButtons.OK);
                }
            }

            try
            {
                plugins.Clear();
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveDependencies;
                LogFile.Dispose();
                Instance = null;
            }
            catch { }

            base.Shutdown();
        }


        private Assembly ResolveDependencies(object sender, ResolveEventArgs args)
        {
            string assembly = args.RequestingAssembly?.GetName().ToString();
            if (args.Name.Contains("0Harmony"))
            {
                if (assembly != null)
                {
                    LogFile.WriteLine("Resolving 0Harmony for " + assembly);
                }
                else
                {
                    LogFile.WriteLine("Resolving 0Harmony");
                }

                return typeof(Harmony).Assembly;
            }
            if (args.Name.Contains("MEPluginLoader"))
            {
                if (assembly != null)
                {
                    LogFile.WriteLine("Resolving itself for " + assembly);
                }
                else
                {
                    LogFile.WriteLine("Resolving itself");
                }

                return typeof(Main).Assembly;
            }

            return null;
        }
    }
}