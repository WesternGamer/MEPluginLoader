using Sandbox.Game.GUI;
using Sandbox.Game.GUI.Controls;
using Sandbox.Graphics.GUI;
using Sandbox.Gui.Layouts;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Input.Devices.Keyboard;
using VRage.Utils;
using VRageMath;
using static Sandbox.Graphics.GUI.MyGuiControlTable;

namespace MEPluginLoader.GUI
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public abstract class TableDialogBase : MyGuiScreenBase
    {
        public override string GetFriendlyName()
        {
            return "ListDialog";
        }

        protected MyGuiControlImageButton LoadButton;
        protected MyGuiControlImageButton RenameButton;
        protected MyGuiControlImageButton DeleteButton;
        protected MyGuiControlImageButton CancelButton;

        protected readonly string Caption;
        protected readonly string DefaultKey;

        protected MyGuiControlTable Table;
        protected readonly Dictionary<string, string> NamesByKey = new();

        protected abstract string ItemName { get; }
        protected abstract string[] ColumnHeaders { get; }
        protected abstract float[] ColumnWidths { get; }
        protected virtual string NormalizeName(string name)
        {
            return name.Trim();
        }

        protected virtual int CompareNames(string a, string b)
        {
            return string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
        }

        protected abstract IEnumerable<string> IterItemKeys();
        protected abstract ItemView GetItemView(string key);
        protected abstract object[] ExampleValues { get; }

        protected abstract void OnLoad(string key);
        protected abstract void OnRenamed(string key, string name);
        protected abstract void OnDelete(string key);

        private readonly Action OnEnterCallback;

        protected int ColumnCount;

        protected TableDialogBase(
            string caption,
            string defaultKey = null)
            : base(new Vector2(0.5f, 0.5f), size: new Vector2(1f, 0.8f))
        {
            Caption = caption;
            DefaultKey = defaultKey;

            // ReSharper disable once VirtualMemberCallInConstructor
            RecreateControls(true);

            CanBeHidden = true;
            CanHideOthers = true;
            m_isTopScreen = true;
            EnabledBackgroundFade = true;

            OnEnterCallback = LoadAndClose;
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            MyDecoratedPanel panel = new MyDecoratedPanel(MyDecoratedPanel.PanelStyle.TopRightButton | MyDecoratedPanel.PanelStyle.BottomDecoration | MyDecoratedPanel.PanelStyle.TitleBar)
            {
                Size = new Vector2(1f, 0.8f),
                Title = Caption
            };
            panel.Layout = new MyHorizontalLayoutBehavior(MyVerticalAlignment.CENTER, panel.Padding, 30f / MyGuiConstants.GUI_OPTIMAL_SIZE.X);
            panel.OnTopRightButtonClicked += delegate { CloseScreen(); };
            base.Controls.Add(panel);

            CreateTable();
            CreateButtons();
        }

        private Vector2 DialogSize => m_size ?? Vector2.One;

        private void CreateTable()
        {
            string[] columnHeaders = ColumnHeaders;
            float[] columnWidths = ColumnWidths;

            if (columnHeaders == null || columnWidths == null)
            {
                return;
            }

            ColumnCount = columnHeaders.Length;

            Table = new MyGuiControlTable
            {
                Position = new Vector2(0.001f, -0.5f * DialogSize.Y + 0.1f),
                Size = new Vector2(0.85f * DialogSize.X, DialogSize.Y - 0.25f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                ColumnsCount = ColumnCount,
                VisibleRowsCount = 15,
            };

            Table.SetCustomColumnWidths(columnWidths);

            object[] exampleValues = ExampleValues;
            for (int colIdx = 0; colIdx < ColumnCount; colIdx++)
            {
                Table.SetColumnName(colIdx, new StringBuilder(columnHeaders[colIdx]));

                switch (exampleValues[colIdx])
                {
                    case int _:
                        Table.SetColumnComparison(colIdx, CellIntComparison);
                        break;

                    default:
                        Table.SetColumnComparison(colIdx, CellTextComparison);
                        break;
                }
            }

            AddItems();

            Table.SortByColumn(0);

            Table.ItemDoubleClicked += OnItemDoubleClicked;

            Controls.Add(Table);
        }

        private int CellTextComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
            string a = NormalizeName(x.Text.ToString());
            string b = NormalizeName(y.Text.ToString());
            return CompareNames(a, b);
        }

        private int CellIntComparison(MyGuiControlTable.Cell x, MyGuiControlTable.Cell y)
        {
            return (x.UserData as int? ?? 0) - (y.UserData as int? ?? 0);
        }

        private void CreateButtons()
        {
            LoadButton = new MyGuiControlImageButton("LoadButton", null, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null,
                new StringBuilder("Load"), onButtonClick: OnLoadButtonClick);

            RenameButton = new MyGuiControlImageButton("LoadButton", null, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null,
                new StringBuilder("Rename"), onButtonClick: OnRenameButtonClick);

            DeleteButton = new MyGuiControlImageButton("LoadButton", null, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null,
                new StringBuilder("Delete"), onButtonClick: OnDeleteButtonClick);

            CancelButton = new MyGuiControlImageButton("LoadButton", null, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null,
                MyTexts.Get(MyCommonTexts.Cancel), onButtonClick: OnCancelButtonClick);

            LoadButton.ApplyStyle("DecoratedPanel_Button");
            LoadButton.AllowBoundKey = true;

            RenameButton.ApplyStyle("DecoratedPanel_Button");
            RenameButton.AllowBoundKey = true;

            DeleteButton.ApplyStyle("DecoratedPanel_Button");
            DeleteButton.AllowBoundKey = true;

            CancelButton.ApplyStyle("DecoratedPanel_Button");
            CancelButton.AllowBoundKey = true;

            RenameButton.Size = new Vector2(0.09f, 0.04f);
            DeleteButton.Size = new Vector2(0.09f, 0.04f);

            float xs = 0.85f * DialogSize.X;
            float y = 0.5f * (DialogSize.Y - 0.15f);
            LoadButton.Position = new Vector2(-0.39f * xs, y);
            RenameButton.Position = new Vector2(-0.08f * xs, y);
            DeleteButton.Position = new Vector2(0.08f * xs, y);
            CancelButton.Position = new Vector2(0.39f * xs, y);

            LoadButton.SetToolTip($"Loads the selected {ItemName}");
            RenameButton.SetToolTip($"Renames the selected {ItemName}");
            DeleteButton.SetToolTip($"Deletes the selected {ItemName}");
            CancelButton.SetToolTip("Cancel");

            Controls.Add(LoadButton);
            Controls.Add(RenameButton);
            Controls.Add(DeleteButton);
            Controls.Add(CancelButton);
        }

        private void AddItems()
        {
            NamesByKey.Clear();

            foreach (string key in IterItemKeys())
            {
                AddRow(key);
            }

            if (TryFindRow(DefaultKey, out int rowIdx))
            {
                Table.SelectedRowIndex = rowIdx;
            }
        }

        private void AddRow(string key)
        {
            ItemView view = GetItemView(key);
            if (view == null)
            {
                return;
            }

            Row row = new MyGuiControlTable.Row(key);
            for (int i = 0; i < ColumnCount; i++)
            {
                row.AddCell(new MyGuiControlTable.Cell(view.Labels[i], view.Values[i]));
            }

            Table.Add(row);
            NamesByKey[key] = view.Labels[0];
        }

        private void OnItemDoubleClicked(MyGuiControlTable table, MyGuiControlTable.EventArgs args)
        {
            LoadAndClose();
        }

        private void OnLoadButtonClick(MyGuiControlImageButton _)
        {
            LoadAndClose();
        }

        private void LoadAndClose()
        {
            if (string.IsNullOrEmpty(SelectedKey))
            {
                return;
            }

            OnLoad(SelectedKey);
            CloseScreen();
        }

        private void OnCancelButtonClick(MyGuiControlImageButton _)
        {
            CloseScreen();
        }

        private void OnRenameButtonClick(MyGuiControlImageButton _)
        {
            if (string.IsNullOrEmpty(SelectedKey))
            {
                return;
            }

            if (!NamesByKey.TryGetValue(SelectedKey, out string oldName))
            {
                return;
            }

            MyGuiSandbox.AddScreen(new NameDialog(newName => OnNewNameSpecified(SelectedKey, newName), $"Rename saved {ItemName}", oldName));
        }

        private void OnNewNameSpecified(string key, string newName)
        {
            newName = NormalizeName(newName);

            if (!TryFindRow(key, out int rowIdx))
            {
                return;
            }

            OnRenamed(key, newName);

            ItemView view = GetItemView(key);

            NamesByKey[key] = view.Labels[0];

            Row row = Table.GetRow(rowIdx);
            for (int colIdx = 0; colIdx < ColumnCount; colIdx++)
            {
                Cell cell = row.GetCell(colIdx);
                StringBuilder sb = cell.Text;
                sb.Clear();
                sb.Append(view.Labels[colIdx]);
            }

            Table.Sort();
        }

        private void OnDeleteButtonClick(MyGuiControlImageButton _)
        {
            string key = SelectedKey;
            if (key == "")
            {
                return;
            }

            string name = NamesByKey.GetValueOrDefault(key) ?? "?";

            MyMessageBox.Show($"Are you sure to delete this saved {ItemName}?\r\n\r\n{name}", "Confirmation", MyMessageBoxButtons.YesNo, MyMessageBoxIcon.Warning, MyMessageBoxDefaultButton.Button2, result => OnDeleteForSure(result, key));
        }

        private void OnDeleteForSure(MyDialogResult result, string key)
        {
            if (result != MyDialogResult.Yes)
            {
                return;
            }

            NamesByKey.Remove(key);

            if (TryFindRow(key, out int rowIdx))
            {
                Predicate<Row> predicate = x => GetRow(x, rowIdx);
                Table.Remove(predicate);
            }


            OnDelete(key);
        }

        private bool GetRow(Row row, int rowIdx)
        {
            return row == Table.GetRow(rowIdx);
        }

        private string SelectedKey => Table.SelectedRow?.UserData as string;

        private bool TryFindRow(string key, out int index)
        {
            if (key == null)
            {
                index = -1;
                return false;
            }

            int count = Table.RowsCount;
            for (index = 0; index < count; index++)
            {
                if (Table.GetRow(index).UserData as string == key)
                {
                    return true;
                }
            }

            index = -1;
            return false;
        }

        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            if (OnEnterCallback != null && MyInput.Static.IsKeyPressed(MyKeys.Enter))
            {
                OnEnterCallback();
            }
        }
    }
}