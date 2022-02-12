﻿using ProtoBuf;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace MEPluginLoader.Data
{
    [XmlInclude(typeof(GitHubPlugin))]
    [ProtoContract]
    [ProtoInclude(103, typeof(GitHubPlugin))]
    public abstract class PluginData : IEquatable<PluginData>
    {
        public abstract string Source { get; }

        [XmlIgnore]
        public Version Version { get; protected set; }

        [XmlIgnore]
        public virtual PluginStatus Status { get; set; } = PluginStatus.None;
        public virtual string StatusString
        {
            get
            {
                switch (Status)
                {
                    case PluginStatus.PendingUpdate:
                        return "Pending Update";
                    case PluginStatus.Updated:
                        return "Updated";
                    case PluginStatus.Error:
                        return "Error!";
                    case PluginStatus.Blocked:
                        return "Not whitelisted!";
                    default:
                        return "";
                }
            }
        }

        [XmlIgnore] public bool IsLocal => Source == "Local";

        [ProtoMember(1)]
        public virtual string Id { get; set; }

        [ProtoMember(2)]
        public string FriendlyName { get; set; } = "Unknown";

        [ProtoMember(3)]
        public bool Hidden { get; set; } = false;

        [ProtoMember(4)]
        public string GroupId { get; set; }

        [ProtoMember(5)]
        public string Tooltip { get; set; }

        [ProtoMember(6)]
        public string Author { get; set; }

        [ProtoMember(7)]
        public string Description { get; set; }

        [XmlIgnore]
        public List<PluginData> Group { get; } = new List<PluginData>();

        [XmlIgnore]
        public bool Enabled => Main.Instance.Config.IsEnabled(Id);

        protected PluginData()
        {
        }

        public abstract Assembly GetAssembly();

        public virtual bool TryLoadAssembly(out Assembly a)
        {
            if (Status == PluginStatus.Error)
            {
                a = null;
                return false;
            }

            try
            {
                // Get the file path
                a = GetAssembly();
                if (Status == PluginStatus.Blocked)
                {
                    return false;
                }

                if (a == null)
                {
                    LogFile.WriteLine("Failed to load " + ToString());
                    Error();
                    return false;
                }

                // Precompile the entire assembly in order to force any missing method exceptions
                LogFile.WriteLine("Precompiling " + a);
                LoaderTools.Precompile(a);
                return true;
            }
            catch (Exception e)
            {
                string name = ToString();
                LogFile.WriteLine($"Failed to load {name} because of an error: " + e);
                if (e is MissingMemberException)
                {
                    LogFile.WriteLine($"Is {name} up to date?");
                }

                Error();
                a = null;
                return false;
            }
        }


        public override bool Equals(object obj)
        {
            return Equals(obj as PluginData);
        }

        public bool Equals(PluginData other)
        {
            return other != null &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return 2108858624 + EqualityComparer<string>.Default.GetHashCode(Id);
        }

        public static bool operator ==(PluginData left, PluginData right)
        {
            return EqualityComparer<PluginData>.Default.Equals(left, right);
        }

        public static bool operator !=(PluginData left, PluginData right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return Id + '|' + FriendlyName;
        }

        public void Error()
        {
            Status = PluginStatus.Error;
            MessageBox.Show(LoaderTools.GetMainForm(), $"The plugin '{this}' caused an error. It is recommended that you disable this plugin and restart. The game may be unstable beyond this point. See loader.log or the game log for details.", "ME Plugin Loader", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected void ErrorSecurity(string hash)
        {
            Status = PluginStatus.Blocked;
            MessageBox.Show(LoaderTools.GetMainForm(), $"Unable to load the plugin {this} because it is not whitelisted!", " ME Plugin Loader", MessageBoxButtons.OK, MessageBoxIcon.Error);
            LogFile.WriteLine("Error: " + this + " with an sha256 of " + hash + " is not on the whitelist!");
        }

        public abstract void Show();

        public void GetDescriptionText(MyGuiControlMultilineText textbox)
        {
            if (string.IsNullOrEmpty(Description))
            {
                if (string.IsNullOrEmpty(Tooltip))
                {
                    textbox.AppendText("No description");
                }
                else
                {
                    textbox.AppendText(CapLength(Tooltip, 1000));
                }

                return;
            }
            else
            {
                string text = CapLength(Description, 1000);
                int textStart = 0;
                foreach (Match m in Regex.Matches(text, @"https?:\/\/(www\.)?[\w-.]{2,256}\.[a-z]{2,4}\b[\w-.@:%\+~#?&//=]*"))
                {
                    int textLen = m.Index - textStart;
                    if (textLen > 0)
                    {
                        textbox.AppendText(text.Substring(textStart, textLen));
                    }

                    textbox.AppendLink(m.Value, m.Value);
                    textStart = m.Index + m.Length;
                }

                if (textStart < text.Length)
                {
                    textbox.AppendText(text.Substring(textStart));
                }
            }
        }

        private string CapLength(string s, int len)
        {
            if (s.Length > len)
            {
                return s.Substring(0, len);
            }

            return s;
        }
    }
}