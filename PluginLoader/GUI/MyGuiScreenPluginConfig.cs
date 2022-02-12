using MEPluginLoader.Data;
using HarmonyLib;
using Medieval.GUI.Controls;
using Sandbox.Game.GUI;
using Sandbox.Game.GUI.Controls;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.Gui.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Library.Extensions;
using VRage.Utils;
using VRageMath;

namespace MEPluginLoader.GUI
{
    public class MyGuiScreenPluginConfig : MyGuiScreenBase
    {
        private const float BarWidth = 0.85f;
        private const float Spacing = 0.0175f;

        private readonly Dictionary<string, bool> pluginCheckboxes = new();
        private readonly PluginDetailsPanel pluginDetails;

        private MyGuiControlTable pluginTable;
        private MyGuiControlLabel pluginCountLabel;
        private MyGuiControlImageButton buttonMore;
        private MyGuiControlContextMenu contextMenu;

        private static PluginConfig Config => Main.Instance.Config;
        private string[] tableFilter;

        public readonly Dictionary<string, bool> AfterRebootEnableFlags = new();

        private PluginData SelectedPlugin
        {
            get => pluginDetails.Plugin;
            set => pluginDetails.Plugin = value;
        }

        private static bool allItemsVisible = true;

        #region Icons

        private static readonly MyGuiHighlightTexture IconHide = new()
        {
            Normal = "Textures\\GUI\\Book\\QuestBook_Icon_Eye_Closed.png",
            Highlight = "Textures\\GUI\\Book\\QuestBook_Icon_Eye_Closed.png",
            SizePx = new Vector2(40f, 40f)
        };

        private static readonly MyGuiHighlightTexture IconShow = new()
        {
            Normal = "Textures\\GUI\\Book\\QuestBook_Icon_Eye.png",
            Highlight = "Textures\\GUI\\Book\\QuestBook_Icon_Eye.png",
            SizePx = new Vector2(40f, 40f)
        };

        #endregion

        public static void OpenMenu()
        {
            if (Main.Instance.List.HasError)
            {
                MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(buttonType: MyMessageBoxButtonsType.OK, messageText: new StringBuilder("An error occurred while downloading the plugin list.\nPlease send your game log to the developers of Plugin Loader."), messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), callback: (x) => MyGuiSandbox.AddScreen(new MyGuiScreenPluginConfig())));
            }
            else
            {
                MyGuiSandbox.AddScreen(new MyGuiScreenPluginConfig());
            }
        }

        /// <summary>
        /// The plugins screen, the constructor itself sets up the menu properties.
        /// </summary>
        private MyGuiScreenPluginConfig() : base(new Vector2(0.5f, 0.5f))
        {
            m_isTopScreen = true;
            base.EnabledBackgroundFade = true;

            foreach (PluginData plugin in Main.Instance.List)
            {
                AfterRebootEnableFlags[plugin.Id] = plugin.Enabled;
            }

            pluginDetails = new PluginDetailsPanel(this);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenPluginConfig";
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RecreateControls(true);
        }

        public override void UnloadContent()
        {
            pluginDetails.OnPluginToggled -= EnablePlugin;
            base.UnloadContent();
        }

        /// <summary>
        /// Initializes the controls of the menu on the left side of the menu.
        /// </summary>
        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            MyDecoratedPanel panel = new MyDecoratedPanel(MyDecoratedPanel.PanelStyle.TopRightButton | MyDecoratedPanel.PanelStyle.BottomDecoration | MyDecoratedPanel.PanelStyle.TitleBar)
            {
                Size = new Vector2(1f, 0.97f),
                Title = "Plugins List"
            };
            panel.Layout = new MyHorizontalLayoutBehavior(MyVerticalAlignment.CENTER, panel.Padding, 30f / MyGuiConstants.GUI_OPTIMAL_SIZE.X);
            panel.OnTopRightButtonClicked += delegate { CloseScreen(); };
            base.Controls.Add(panel);

            // Sets the origin relative to the center of the caption on the X axis and to the bottom the caption on the y axis.
            Vector2 origin = new Vector2(0.0f, -0.425f);

            origin.Y += Spacing;

            // Change the position of this to move the entire middle section of the menu, the menu bars, menu title, and bottom buttons won't move
            // Adds a search bar right below the bar on the left side of the menu.
            MySearchBox searchBox = new MySearchBox(new Vector2(origin.X - (BarWidth / 2), origin.Y), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            {

                // Changing the search box X size will change the plugin list length.
                Size = new Vector2(0.339f, 0.04f)
            };
            searchBox.OnTextChanged += SearchBox_TextChanged;
            Controls.Add(searchBox);

            #region Visibility Button

            // Adds a button to show only enabled plugins. Located right of the search bar.
            //var buttonVisibility = new MyGuiControlButton(new Vector2(origin.X - (BarWidth / 2) + searchBox.Size.X, origin.Y) + new Vector2(0.03f, 0.02f), MyGuiControlButtonStyleEnum.Rectangular, new Vector2(0.04f * 2.52929769833f), onButtonClick: OnVisibilityClick, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, toolTip: "Show only enabled plugins.", buttonScale: 0.5f);

            MyGuiControlImageButton buttonVisibility = new MyGuiControlImageButton("buttonVisibility", new Vector2(origin.X - (BarWidth / 2) + searchBox.Size.X, origin.Y) + new Vector2(0.01f, 0f), new Vector2(0.05f, 0.04f), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, toolTip: "Show only enabled plugins.", onButtonClick: OnVisibilityClick);

            buttonVisibility.ApplyStyle("DecoratedPanel_Button");
            buttonVisibility.AllowBoundKey = true;

            if (allItemsVisible || Config.Count == 0)
            {
                allItemsVisible = true;
                buttonVisibility.Icon = IconHide;
            }
            else
            {
                buttonVisibility.Icon = IconShow;
            }

            Controls.Add(buttonVisibility);

            #endregion

            origin.Y += searchBox.Size.Y + Spacing;

            #region Plugin List

            // Adds the plugin list on the right of the menu below the search bar.
            pluginTable = new MyGuiControlTable
            {
                Position = new Vector2(origin.X - (BarWidth / 2), origin.Y),
                Size = new Vector2(searchBox.Size.X + buttonVisibility.Size.X + 0.011f, 0.6f), // The y value can be bigger than the visible rows count as the visibleRowsCount controls the height.
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                ColumnsCount = 3,
                VisibleRowsCount = 20
            };

            pluginTable.SetCustomColumnWidths(new[]
            {
                0.175f,
                0.625f,
                0.22f
            });

            pluginTable.SetColumnName(0, new StringBuilder("Source"));
            pluginTable.SetColumnComparison(0, CellTextOrDataComparison);
            pluginTable.SetColumnName(1, new StringBuilder("Name"));
            pluginTable.SetColumnComparison(1, CellTextComparison);
            pluginTable.SetColumnName(2, new StringBuilder("Enabled"));
            pluginTable.SetColumnComparison(2, CellTextComparison);

            // Default sorting
            pluginTable.SortByColumn(2, MyGuiControlTable.SortStateEnum.Ascending);

            // Selecting list items load their details in OnItemSelected
            pluginTable.ItemSelected += OnItemSelected;
            Controls.Add(pluginTable);

            // Double clicking list items toggles the enable flag
            pluginTable.ItemDoubleClicked += OnItemDoubleClicked;

            #endregion

            origin.Y += Spacing + pluginTable.Size.Y;

            // Adds the bar at the bottom between just above the buttons.
            MyGuiControlSeparatorList bottomBar = new MyGuiControlSeparatorList();
            bottomBar.AddHorizontal(new Vector2(origin.X - (BarWidth / 2), origin.Y), BarWidth);
            Controls.Add(bottomBar);

            origin.Y += Spacing;

            // Adds buttons at bottom of menu
            MyGuiControlImageButton buttonRestart = new MyGuiControlImageButton("buttonRestart", origin, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, "Restart the game and apply changes.", new StringBuilder("Apply"), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE, OnRestartButtonClick);
            MyGuiControlImageButton buttonClose = new MyGuiControlImageButton("buttonClose", origin, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, "Closes the dialog without saving changes to plugin selection", new StringBuilder("Cancel"), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE, OnCancelButtonClick);
            buttonMore = new MyGuiControlImageButton("buttonMore", origin, new Vector2(0.04f, 0.04f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP, "Advanced", new StringBuilder("..."), 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_ACTIVE, OnMoreButtonClick);

            buttonRestart.ApplyStyle("DecoratedPanel_Button");
            buttonRestart.AllowBoundKey = true;

            buttonClose.ApplyStyle("DecoratedPanel_Button");
            buttonClose.AllowBoundKey = true;

            buttonMore.ApplyStyle("DecoratedPanel_Button");
            buttonMore.AllowBoundKey = true;

            // FIXME: Use MyLayoutHorizontal instead
            AlignRow(origin, 0.05f, buttonRestart, buttonClose);
            Controls.Add(buttonRestart);
            Controls.Add(buttonClose);
            buttonMore.Position = buttonClose.Position + new Vector2(buttonClose.Size.X / 2 + 0.05f, 0);
            Controls.Add(buttonMore);

            // Adds a place to show the total amount of plugins and to show the total amount of visible plugins.
            pluginCountLabel = new MyGuiControlLabel(new Vector2(origin.X - (BarWidth / 2), buttonRestart.Position.Y), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            Controls.Add(pluginCountLabel);

            // Right side panel showing the details of the selected plugin
            Vector2 rightSideOrigin = buttonVisibility.Position + new Vector2(0.075f + (buttonVisibility.Size.X / 2), -(buttonVisibility.Size.Y / 2));
            pluginDetails.CreateControls(rightSideOrigin);
            Controls.Add(pluginDetails);
            pluginDetails.OnPluginToggled += EnablePlugin;

            // Context menu for the more (...) button
            contextMenu = new MyGuiControlContextMenu();
            contextMenu.Deactivate();
            contextMenu.CreateNewContextMenu();
            contextMenu.AddItem(new StringBuilder("Save profile"), "Saved the current plugin selection", userData: nameof(OnSaveProfile));
            contextMenu.AddItem(new StringBuilder("Load profile"), "Loads a saved plugin selection", userData: nameof(OnLoadProfile));
            contextMenu.AddItem(new StringBuilder("Back"));
            contextMenu.Enabled = true;
            contextMenu.ItemClicked += OnContextMenuItemClicked;
            Controls.Add(contextMenu);

            // Refreshes the table to show plugins on plugin list
            RefreshTable();
        }

        /// <summary>
        /// Event that triggers when the visibility button is clicked. This method shows all plugins or only enabled plugins.
        /// </summary>
        /// <param name="btn">The button to assign this event to.</param>
        private void OnVisibilityClick(MyGuiControlImageButton btn)
        {
            if (allItemsVisible)
            {
                allItemsVisible = false;
                btn.Icon = IconShow;
            }
            else
            {
                allItemsVisible = true;
                btn.Icon = IconHide;
            }

            RefreshTable(tableFilter);
        }

        private static int CellTextOrDataComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
            int result = TextComparison(x.Text, y.Text);
            if (result != 0)
            {
                return result;
            }

            return TextComparison((StringBuilder)x.UserData, (StringBuilder)y.UserData);
        }

        private static int CellTextComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
            return TextComparison(x.Text, y.Text);
        }

        private static int TextComparison(StringBuilder x, StringBuilder y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }

                return 1;
            }

            if (y == null)
            {
                return -1;
            }

            return x.CompareTo(y);
        }

        /// <summary>
        /// Clears the table and adds the list of plugins and their information.
        /// </summary>
        /// <param name="filter">Text filter</param>
        private void RefreshTable(string[] filter = null)
        {
            pluginTable.Clear();
            pluginTable.Controls.Clear();
            pluginCheckboxes.Clear();
            PluginList list = Main.Instance.List;
            bool noFilter = filter == null || filter.Length == 0;
            foreach (PluginData plugin in list)
            {
                bool enabled = AfterRebootEnableFlags[plugin.Id];

                if (noFilter && (plugin.Hidden || !allItemsVisible) && !enabled)
                {
                    continue;
                }

                if (!noFilter && !FilterName(plugin.FriendlyName, filter))
                {
                    continue;
                }

                MyGuiControlTable.Row row = new MyGuiControlTable.Row(plugin);
                pluginTable.Add(row);

                StringBuilder name = new StringBuilder(plugin.FriendlyName);
                row.AddCell(new MyGuiControlTable.Cell(plugin.Source, name));

                string tip = plugin.FriendlyName;
                if (!string.IsNullOrWhiteSpace(plugin.Tooltip))
                {
                    tip += "\n" + plugin.Tooltip;
                }

                row.AddCell(new MyGuiControlTable.Cell(plugin.FriendlyName, toolTip: tip));

                string enabledText;
                if (enabled)
                {
                    enabledText = "Enabled";
                }
                else
                {
                    enabledText = "Disabled";
                }
                row.AddCell(new MyGuiControlTable.Cell(enabledText, null));
                pluginCheckboxes[plugin.Id] = enabled;
            }

            pluginCountLabel.Text = pluginTable.RowsCount + "/" + list.Count + " visible";
            pluginTable.Sort(false);
            pluginTable.SelectedRowIndex = null;
            tableFilter = filter;
            pluginTable.SelectedRowIndex = 0;

            MyGuiControlTable.EventArgs args = new MyGuiControlTable.EventArgs { RowIndex = 0 };
            OnItemSelected(pluginTable, args);
        }

        /// <summary>
        /// Event that triggers when the text in the searchbox is changed.
        /// </summary>
        /// <param name="txt">The text that was entered into the searchbox.</param>
        private void SearchBox_TextChanged(string txt)
        {
            string[] args = txt.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            RefreshTable(args);
        }

        private static bool FilterName(string name, IEnumerable<string> filter)
        {
            return filter.All(s => name.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sets data on right side of screen when a plugin is selected.
        /// </summary>
        /// <param name="table">Table to get the plugin data.</param>
        /// <param name="args">Event arguments.</param>
        private void OnItemSelected(MyGuiControlTable table, MyGuiControlTable.EventArgs args)
        {
            if (!TryGetPluginByRowIndex(args.RowIndex, out PluginData plugin))
            {
                return;
            }

            contextMenu.Deactivate();
            SelectedPlugin = plugin;
        }

        private void OnItemDoubleClicked(MyGuiControlTable table, MyGuiControlTable.EventArgs args)
        {
            if (!TryGetPluginByRowIndex(args.RowIndex, out PluginData data))
            {
                return;
            }

            EnablePlugin(data, !AfterRebootEnableFlags[data.Id]);
        }

        private bool TryGetPluginByRowIndex(int rowIndex, out PluginData plugin)
        {
            if (rowIndex < 0 || rowIndex >= pluginTable.RowsCount)
            {
                plugin = null;
                return false;
            }

            MyGuiControlTable.Row row = pluginTable.GetRow(rowIndex);
            plugin = row.UserData as PluginData;
            return plugin != null;
        }

        private void AlignRow(Vector2 origin, float spacing, params MyGuiControlBase[] elements)
        {
            if (elements.Length == 0)
            {
                return;
            }

            float totalWidth = 0;
            for (int i = 0; i < elements.Length; i++)
            {
                MyGuiControlBase btn = elements[i];
                totalWidth += btn.Size.X;
                if (i < elements.Length - 1)
                {
                    totalWidth += spacing;
                }
            }

            float originX = origin.X - (totalWidth / 2);
            foreach (MyGuiControlBase btn in elements)
            {
                float halfWidth = btn.Size.X / 2;
                originX += halfWidth;
                btn.Position = new Vector2(originX, origin.Y);
                originX += spacing + halfWidth;
            }
        }

        private void EnablePlugin(PluginData plugin, bool enable)
        {
            if (enable == AfterRebootEnableFlags[plugin.Id])
            {
                return;
            }

            AfterRebootEnableFlags[plugin.Id] = enable;

            SetPluginCheckbox(plugin, enable);

            if (enable)
            {
                DisableOtherPluginsInSameGroup(plugin);
            }
        }

        private void SetPluginCheckbox(PluginData plugin, bool enable)
        {
            if (!pluginCheckboxes.TryGetValue(plugin.Id, out bool checkbox))
            {
                return; // The text might not exist if the target plugin is a dependency not currently in the table
            }

            checkbox = enable;

            MyGuiControlTable.Row row = pluginTable.Find(x => ReferenceEquals(x.UserData as PluginData, plugin));

            if (checkbox == true)
            {
                row?.GetCell(2).Text.Clear().Append("Enabled");
            }

            if (checkbox == false)
            {
                row?.GetCell(2).Text.Clear().Append("Disabled");
            }
        }

        private void DisableOtherPluginsInSameGroup(PluginData plugin)
        {
            foreach (PluginData other in plugin.Group)
            {
                if (!ReferenceEquals(other, plugin))
                {
                    EnablePlugin(other, false);
                }
            }
        }

        private void OnCancelButtonClick(MyGuiControlImageButton btn)
        {
            CloseScreen();
        }

        private void OnMoreButtonClick(MyGuiControlImageButton _)
        {
            contextMenu.Enabled = false;
            contextMenu.Activate(false);
            contextMenu.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM;
            contextMenu.Position = new Vector2(0.9f, 0.9f);
            //Activates two times because context menu ends up at top left of screen when opened for the first time.
            contextMenu.Activate(false);
        }

        private void OnContextMenuItemClicked(MyGuiControlContextMenu _, MyGuiControlContextMenu.EventArgs args)
        {
            contextMenu.Deactivate();

            switch ((string)args.UserData)
            {
                case nameof(OnSaveProfile):
                    OnSaveProfile();
                    break;

                case nameof(OnLoadProfile):
                    OnLoadProfile();
                    break;
            }
        }

        private void OnSaveProfile()
        {
            string timestamp = DateTime.Now.ToString("O").Substring(0, 19).Replace('T', ' ');
            MyGuiSandbox.AddScreen(new NameDialog(OnProfileNameProvided, "Save profile", timestamp));
        }

        private void OnProfileNameProvided(string name)
        {
            IEnumerable<string> afterRebootEnablePluginIds = AfterRebootEnableFlags
                .Where(p => p.Value)
                .Select(p => p.Key);

            Profile profile = new Profile(name, afterRebootEnablePluginIds.ToArray());
            Config.ProfileMap[profile.Key] = profile;
            Config.Save();
        }

        private void OnLoadProfile()
        {
            MyGuiSandbox.AddScreen(new ProfilesDialog("Load profile", OnProfileLoaded));
        }

        private void OnProfileLoaded(Profile profile)
        {
            HashSet<string> pluginsEnabledInProfile = profile.Plugins.ToHashSet();

            foreach (PluginData plugin in Main.Instance.List)
            {
                EnablePlugin(plugin, pluginsEnabledInProfile.Contains(plugin.Id));
            }

            pluginTable.SortByColumn(2, MyGuiControlTable.SortStateEnum.Ascending);
        }

        private int ModifiedCount => Main.Instance.List.Count(plugin => plugin.Enabled != AfterRebootEnableFlags[plugin.Id]);

        private void OnRestartButtonClick(MyGuiControlImageButton btn)
        {
            if (ModifiedCount == 0)
            {
                CloseScreen();
                return;
            }

            MyMessageBox.Show("A restart is required to apply changes.Would you like to restart the game now ?", "Apply Changes?", MyMessageBoxButtons.YesNoCancel, MyMessageBoxIcon.Question, MyMessageBoxDefaultButton.Button1, AskRestartResult);
        }

        private void Save()
        {
            if (ModifiedCount == 0)
            {
                return;
            }

            foreach (PluginData plugin in Main.Instance.List)
            {
                Config.SetEnabled(plugin.Id, AfterRebootEnableFlags[plugin.Id]);
            }

            Config.Save();
        }

        #region Restart

        private void AskRestartResult(MyDialogResult result)
        {
            if (result == MyDialogResult.Yes)
            {
                Save();
                if (MySession.Static != null)
                {
                    ShowSaveMenu();
                    return;
                }

                MyScreenManager.CloseAllScreensNowExcept(null);
                LoaderTools.Restart();
            }
            else if (result == MyDialogResult.No)
            {
                Save();
                CloseScreen();
            }
        }

        /// <summary>
        /// From WesternGamer/InGameWorldLoading
        /// </summary>
        /// <param name="afterMenu">Action after code is executed.</param>
        private static void ShowSaveMenu()
        {
            // Sync.IsServer is backwards
            if (!Sync.IsServer)
            {
                UnloadAndRestartGame();
                return;
            }

            MyMessageBox messageMenu = MyMessageBox.Show("Save changes before restarting game?", MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm).ToString(), MyMessageBoxButtons.YesNoCancel, MyMessageBoxIcon.Question, MyMessageBoxDefaultButton.Button1, ShowSaveMenuCallback);

            messageMenu.SetButtonText(2, "Don't Restart");

            void ShowSaveMenuCallback(MyDialogResult callbackReturn)
            {
                switch (callbackReturn)
                {
                    case MyDialogResult.Yes:
                        MyAsyncSaving.Start(delegate
                        {
                            UnloadAndRestartGame();
                        });
                        break;

                    case MyDialogResult.No:
                        MyAudio.Static.Mute = true;
                        MyAudio.Static.StopMusic();
                        UnloadAndRestartGame();
                        break;
                }
            }
        }

        private static void UnloadAndRestartGame()
        {
            MyScreenManager.CloseAllScreensNowExcept(null);
            LoaderTools.Restart();
        }

        #endregion
    }
}