using MEPluginLoader.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MEPluginLoader.GUI
{
    public class ProfilesDialog : TableDialogBase
    {
        private static PluginConfig Config => Main.Instance.Config;
        private static Dictionary<string, Profile> ProfileMap => Config.ProfileMap;
        private static PluginList PluginList => Main.Instance.List;

        private readonly Action<Profile> onProfileLoaded;

        protected override string ItemName => "profile";
        protected override string[] ColumnHeaders => new[] { "Name", "Enabled plugins" };
        protected override float[] ColumnWidths => new[] { 0.55f, 0.43f };

        public ProfilesDialog(string caption, Action<Profile> onProfileLoaded) : base(caption)
        {
            this.onProfileLoaded = onProfileLoaded;
        }

        protected override IEnumerable<string> IterItemKeys()
        {
            return ProfileMap.Keys.ToArray();
        }

        protected override ItemView GetItemView(string key)
        {
            if (!ProfileMap.TryGetValue(key, out Profile profile))
            {
                return null;
            }

            int locals = 0;
            int plugins = 0;
            foreach (string id in profile.Plugins)
            {
                if (!PluginList.TryGetPlugin(id, out PluginData plugin))
                {
                    continue;
                }

                switch (plugin)
                {

                    case LocalPlugin:
                        locals++;
                        break;

                    default:
                        plugins++;
                        break;
                }
            }

            List<string> infoItems = new List<string>();
            if (locals > 0)
            {
                infoItems.Add(locals > 1 ? $"{locals} local plugins" : "1 local plugin");
            }

            if (plugins > 0)
            {
                infoItems.Add(plugins > 1 ? $"{plugins} plugins" : "1 plugin");
            }

            string info = string.Join(", ", infoItems);
            string[] labels = new[] { profile.Name, info };

            int total = locals + plugins;
            object[] values = new object[] { null, total };

            return new ItemView(labels, values);
        }

        protected override object[] ExampleValues => new object[] { null, 0 };

        protected override void OnLoad(string key)
        {
            if (!ProfileMap.TryGetValue(key, out Profile profile))
            {
                return;
            }

            onProfileLoaded(profile);
        }

        protected override void OnRenamed(string key, string name)
        {
            if (!ProfileMap.TryGetValue(key, out Profile profile))
            {
                return;
            }

            profile.Name = name;
        }

        protected override void OnDelete(string key)
        {
            ProfileMap.Remove(key);
            Config.Save();
        }
    }
}