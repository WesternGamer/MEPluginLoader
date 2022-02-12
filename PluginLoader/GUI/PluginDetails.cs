using MEPluginLoader.Data;
using Sandbox.Graphics.GUI;
using Sandbox.Gui.Layouts;
using System;
using VRage.Utils;
using VRageMath;

namespace MEPluginLoader.GUI
{
    public class PluginDetailsPanel : MyGuiControlParent
    {
        public event Action<PluginData, bool> OnPluginToggled;

        // Panel controls
        private MyGuiControlLabel pluginNameLabel;
        private MyGuiControlLabel pluginNameText;
        private MyGuiControlLabel authorLabel;
        private MyGuiControlLabel authorText;
        private MyGuiControlLabel versionLabel;
        private MyGuiControlLabel versionText;
        private MyGuiControlLabel statusLabel;
        private MyGuiControlLabel statusText;
        private MyGuiControlMultilineText descriptionText;
        private MyGuiControlCompositePanel descriptionPanel;
        private MyGuiControlLabel enableLabel;
        private MyGuiControlCheckbox enableCheckbox;
        private MyGuiControlImageButton infoButton;
        private MyGuiControlImageButton configButton;

        // Layout management
        private MyLayoutTable layoutTable;

        // Plugin currently loaded into the panel or null if none are loaded
        private PluginData plugin;
        private PluginInstance instance;

        private readonly MyGuiScreenPluginConfig pluginsDialog;

        public PluginDetailsPanel(MyGuiScreenPluginConfig dialog)
        {
            pluginsDialog = dialog;
        }

        public PluginData Plugin
        {
            get => plugin;
            set
            {
                if (ReferenceEquals(value, Plugin))
                {
                    return;
                }

                plugin = value;
                if (Main.Instance.TryGetPluginInstance(plugin.Id, out PluginInstance instance))
                    this.instance = instance;

                if (plugin == null)
                {
                    DisableControls();
                    ClearPluginData();
                    return;
                }

                EnableControls();
                LoadPluginData();
            }
        }

        private void DisableControls()
        {
            foreach (MyGuiControlBase control in Controls)
            {
                control.Enabled = false;
            }
        }

        private void EnableControls()
        {
            foreach (MyGuiControlBase control in Controls)
            {
                control.Enabled = true;
            }
        }

        private void ClearPluginData()
        {
            pluginNameText.Text = "";
            authorText.Text = "";
            versionText.Text = "";
            statusText.Text = "";
            descriptionText.Text.Clear();
            enableCheckbox.IsChecked = false;
        }

        public void LoadPluginData()
        {
            if (plugin == null)
            {
                return;
            }

            bool nonLocal = !plugin.IsLocal;

            pluginNameText.Text = plugin.FriendlyName ?? "N/A";

            authorText.Text = plugin.Author ?? (plugin.IsLocal ? "Local" : "N/A");

            versionText.Text = plugin.Version?.ToString() ?? "N/A";

            statusLabel.Visible = nonLocal;
            statusText.Visible = nonLocal;
            statusText.Text = plugin.Status == PluginStatus.None ? (plugin.Enabled ? "Up to date" : "N/A") : plugin.StatusString;

            descriptionPanel.Visible = nonLocal;
            descriptionText.Visible = nonLocal;
            descriptionText.Clear();
            plugin.GetDescriptionText(descriptionText);

            enableCheckbox.IsChecked = pluginsDialog.AfterRebootEnableFlags[plugin.Id];

            configButton.Enabled = instance != null && instance.HasConfigDialog;
        }

        public virtual void CreateControls(Vector2 rightSideOrigin)
        {
            // Plugin name
            pluginNameLabel = new MyGuiControlLabel
            {
                Text = "Name",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            pluginNameText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            // Author
            authorLabel = new MyGuiControlLabel
            {
                Text = "Author",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            authorText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            // Version
            versionLabel = new MyGuiControlLabel
            {
                Text = "Version",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            versionText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            // Status
            statusLabel = new MyGuiControlLabel
            {
                Text = "Status",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            statusText = new MyGuiControlLabel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };

            // Plugin description
            descriptionText = new MyGuiControlMultilineText
            {
                Name = "DescriptionText",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                TextAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                TextBoxAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            };
            descriptionText.OnLinkClicked += (x, url) => MyGuiSandbox.OpenUrl(url, UrlOpenMode.SteamOrExternalWithConfirm);
            descriptionPanel = new MyGuiControlCompositePanel
            {
                BackgroundTexture = MyGuiConstants.TEXTURE_RECTANGLE_DARK
            };

            // Enable checkbox
            enableLabel = new MyGuiControlLabel
            {
                Text = "Enabled",
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP
            };
            enableCheckbox = new MyGuiControlCheckbox(toolTip: "Enables loading the plugin when ME is started.")
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                Enabled = false
            };
            enableCheckbox.OnCheckedChanged += TogglePlugin;

            // Info button
            infoButton = new MyGuiControlImageButton(onButtonClick: _ => Plugin?.Show())
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                Text = "Plugin Info"
            };

            infoButton.ApplyStyle("DecoratedPanel_Button");
            infoButton.AllowBoundKey = true;

            // Plugin config button
            configButton = new MyGuiControlImageButton(onButtonClick: _ => instance?.OpenConfig())
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                Text = "Plugin Config"
            };

            configButton.ApplyStyle("DecoratedPanel_Button");
            configButton.AllowBoundKey = true;

            LayoutControls(rightSideOrigin);
        }

        private void LayoutControls(Vector2 rightSideOrigin)
        {
            layoutTable = new MyLayoutTable(this, rightSideOrigin, new Vector2(1f, 1f));
            layoutTable.SetColumnWidths(175f, 475f);
            layoutTable.SetRowHeights(60f, 60f, 60f, 60f, 420f, 60f, 60f, 60f);

            int row = 0;

            layoutTable.Add(pluginNameLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(pluginNameText, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            layoutTable.Add(authorLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(authorText, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            layoutTable.Add(versionLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(versionText, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            layoutTable.Add(statusLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(statusText, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            layoutTable.AddWithSize(descriptionPanel, MyAlignH.Center, MyAlignV.Top, row, 0, 1, 2);
            layoutTable.AddWithSize(descriptionText, MyAlignH.Center, MyAlignV.Center, row, 0, 1, 2);
            row++;

            layoutTable.Add(enableLabel, MyAlignH.Left, MyAlignV.Center, row, 0);
            layoutTable.Add(enableCheckbox, MyAlignH.Left, MyAlignV.Center, row, 1);
            row++;

            layoutTable.AddWithSize(infoButton, MyAlignH.Right, MyAlignV.Center, row, 0, 1, colSpan: 2);

            row++;

            layoutTable.AddWithSize(configButton, MyAlignH.Right, MyAlignV.Center, row, 0, 1, colSpan: 2);

            Vector2 border = 0.002f * Vector2.One;
            descriptionPanel.Position -= border;
            descriptionPanel.Size += 2 * border;

            DisableControls();
        }

        private void TogglePlugin(MyGuiControlCheckbox obj)
        {
            if (plugin == null)
            {
                return;
            }

            OnPluginToggled?.Invoke(plugin, enableCheckbox.IsChecked);
        }
    }
}