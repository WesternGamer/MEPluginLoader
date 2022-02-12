﻿using MEPluginLoader.Data;
using MEPluginLoader.Network;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;

namespace MEPluginLoader
{
    public class PluginList : IEnumerable<PluginData>
    {
        private Dictionary<string, PluginData> plugins = new Dictionary<string, PluginData>();

        public int Count => plugins.Count;

        public bool HasError { get; private set; }

        public PluginData this[string key]
        {
            get => plugins[key];
            set => plugins[key] = value;
        }

        public bool Contains(string id)
        {
            return plugins.ContainsKey(id);
        }

        public bool TryGetPlugin(string id, out PluginData pluginData)
        {
            return plugins.TryGetValue(id, out pluginData);
        }

        public PluginList(string mainDirectory, PluginConfig config)
        {
            GUI.SplashScreen lbl = Main.Instance.Splash;

            lbl.SetText("Downloading plugin list...");
            DownloadList(mainDirectory, config);

            if (plugins.Count == 0)
            {
                LogFile.WriteLine("WARNING: No plugins in the plugin list. Plugin list will contain local plugins only.");
                HasError = true;
            }

            FindLocalPlugins(mainDirectory);
            LogFile.WriteLine($"Found {plugins.Count} plugins");
            FindPluginGroups();
        }

        public bool Remove(string id)
        {
            return plugins.Remove(id);
        }

        private void FindPluginGroups()
        {
            int groups = 0;
            foreach (IGrouping<string, PluginData> group in plugins.Values.Where(x => !string.IsNullOrWhiteSpace(x.GroupId)).GroupBy(x => x.GroupId))
            {
                groups++;
                foreach (PluginData data in group)
                {
                    data.Group.AddRange(group.Where(x => x != data));
                }
            }
            if (groups > 0)
            {
                LogFile.WriteLine($"Found {groups} plugin groups");
            }
        }

        private void DownloadList(string mainDirectory, PluginConfig config)
        {
            string whitelist = Path.Combine(mainDirectory, "whitelist.bin");

            PluginData[] list;
            string currentHash = config.ListHash;
            if (!TryDownloadWhitelistHash(out string newHash))
            {
                // No connection to plugin hub, read from cache
                if (!TryReadWhitelistFile(whitelist, out list))
                {
                    return;
                }
            }
            else if (currentHash == null || currentHash != newHash)
            {
                // Plugin list changed, try downloading new version first
                if (!TryDownloadWhitelistFile(whitelist, newHash, config, out list)
                    && !TryReadWhitelistFile(whitelist, out list))
                {
                    return;
                }
            }
            else
            {
                // Plugin list did not change, try reading the current version first
                if (!TryReadWhitelistFile(whitelist, out list)
                    && !TryDownloadWhitelistFile(whitelist, newHash, config, out list))
                {
                    return;
                }
            }

            if (list != null)
            {
                plugins = list.ToDictionary(x => x.Id);
            }
        }

        private bool TryReadWhitelistFile(string file, out PluginData[] list)
        {
            list = null;

            if (File.Exists(file) && new FileInfo(file).Length > 0)
            {
                LogFile.WriteLine("Reading whitelist from cache");
                try
                {
                    using (Stream binFile = File.OpenRead(file))
                    {
                        list = Serializer.Deserialize<PluginData[]>(binFile);
                    }
                    LogFile.WriteLine("Whitelist retrieved from disk");
                    return true;
                }
                catch (Exception e)
                {
                    LogFile.WriteLine("Error while reading whitelist: " + e);
                }
            }
            else
            {
                LogFile.WriteLine("No whitelist cache exists");
            }

            return false;
        }

        private bool TryDownloadWhitelistFile(string file, string hash, PluginConfig config, out PluginData[] list)
        {
            list = null;
            Dictionary<string, PluginData> newPlugins = new Dictionary<string, PluginData>();

            try
            {
                using (Stream zipFileStream = GitHub.DownloadRepo(GitHub.listRepoName, GitHub.listRepoCommit, out _))
                using (ZipArchive zipFile = new ZipArchive(zipFileStream))
                {
                    XmlSerializer xml = new XmlSerializer(typeof(PluginData));
                    foreach (ZipArchiveEntry entry in zipFile.Entries)
                    {
                        if (!entry.FullName.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        using (Stream entryStream = entry.Open())
                        using (StreamReader entryReader = new StreamReader(entryStream))
                        {
                            try
                            {
                                PluginData data = (PluginData)xml.Deserialize(entryReader);
                                newPlugins[data.Id] = data;
                            }
                            catch (InvalidOperationException e)
                            {
                                LogFile.WriteLine("An error occurred while reading the plugin xml: " + (e.InnerException ?? e));
                            }
                        }
                    }
                }

                list = newPlugins.Values.ToArray();
                return TrySaveWhitelist(file, list, hash, config);
            }
            catch (Exception e)
            {
                LogFile.WriteLine("Error while downloading whitelist: " + e);
            }

            return false;
        }

        private bool TrySaveWhitelist(string file, PluginData[] list, string hash, PluginConfig config)
        {
            try
            {
                LogFile.WriteLine("Saving whitelist to disk");
                using (MemoryStream mem = new MemoryStream())
                {
                    Serializer.Serialize(mem, list);
                    using (Stream binFile = File.Create(file))
                    {
                        mem.WriteTo(binFile);
                    }
                }

                config.ListHash = hash;
                config.Save();

                LogFile.WriteLine("Whitelist updated");
                return true;
            }
            catch (Exception e)
            {
                LogFile.WriteLine("Error while saving whitelist: " + e);
                try
                {
                    File.Delete(file);
                }
                catch { }
                return false;
            }
        }

        private bool TryDownloadWhitelistHash(out string hash)
        {
            hash = null;
            try
            {
                using (Stream hashStream = GitHub.DownloadFile(GitHub.listRepoName, GitHub.listRepoCommit, GitHub.listRepoHash))
                using (StreamReader hashStreamReader = new StreamReader(hashStream))
                {
                    hash = hashStreamReader.ReadToEnd().Trim();
                }
                return true;
            }
            catch (Exception e)
            {
                LogFile.WriteLine("Error while downloading whitelist hash: " + e);
                return false;
            }
        }

        private void FindLocalPlugins(string mainDirectory)
        {
            foreach (string dll in Directory.EnumerateFiles(mainDirectory, "*.dll", SearchOption.AllDirectories))
            {
                if (!dll.Contains(Path.DirectorySeparatorChar + "GitHub" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    LocalPlugin local = new LocalPlugin(dll);
                    string name = local.FriendlyName;
                    if (!name.StartsWith("0Harmony") && !name.StartsWith("Microsoft"))
                    {
                        plugins[local.Id] = local;
                    }
                }
            }
        }

        public IEnumerator<PluginData> GetEnumerator()
        {
            return plugins.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return plugins.Values.GetEnumerator();
        }
    }
}