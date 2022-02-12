using Sandbox.Game.GUI.Controls;
using Sandbox.Graphics.GUI;
using Sandbox.Gui.Layouts;
using System;
using VRage;
using VRage.Input;
using VRage.Input.Devices.Keyboard;
using VRage.Utils;
using VRageMath;

// ReSharper disable VirtualMemberCallInConstructor
#pragma warning disable 618

namespace MEPluginLoader.GUI
{
    internal class NameDialog : MyGuiScreenBase
    {
        private MyGuiControlTextbox nameBox;
        private MyGuiControlImageButton okButton;
        private MyGuiControlImageButton cancelButton;

        private readonly Action<string> onOk;
        private readonly Action OnEnterCallback;

        private readonly string caption;
        private readonly string defaultName;
        private readonly int maxLength;

        public NameDialog(
            Action<string> onOk,
            string caption = "Name",
            string defaultName = "",
            int maxLength = 40)
            : base(new Vector2(0.5f, 0.5f), null, new Vector2(0.5f, 0.28f))
        {
            this.onOk = onOk;
            this.caption = caption;
            this.defaultName = defaultName;
            this.maxLength = maxLength;

            RecreateControls(true);

            CanBeHidden = true;
            CanHideOthers = true;
            m_isTopScreen = true;
            EnabledBackgroundFade = true;
            OnEnterCallback = ReturnOk;
        }

        private Vector2 DialogSize => m_size ?? Vector2.One;

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            MyDecoratedPanel panel = new MyDecoratedPanel(MyDecoratedPanel.PanelStyle.TopRightButton | MyDecoratedPanel.PanelStyle.BottomDecoration | MyDecoratedPanel.PanelStyle.TitleBar)
            {
                Size = new Vector2(0.5f, 0.28f),
                Title = caption
            };
            panel.Layout = new MyHorizontalLayoutBehavior(MyVerticalAlignment.CENTER, panel.Padding, 30f / MyGuiConstants.GUI_OPTIMAL_SIZE.X);
            panel.OnTopRightButtonClicked += delegate { CloseScreen(); };
            base.Controls.Add(panel);

            MyGuiControlSeparatorList controlSeparatorList1 = new MyGuiControlSeparatorList();
            controlSeparatorList1.AddHorizontal(new Vector2(-0.39f * DialogSize.X, -0.5f * DialogSize.Y + 0.075f), DialogSize.X * 0.78f);
            Controls.Add(controlSeparatorList1);

            MyGuiControlSeparatorList controlSeparatorList2 = new MyGuiControlSeparatorList();
            controlSeparatorList2.AddHorizontal(new Vector2(-0.39f * DialogSize.X, +0.5f * DialogSize.Y - 0.123f), DialogSize.X * 0.78f);
            Controls.Add(controlSeparatorList2);

            nameBox = new MyGuiControlTextbox(new Vector2(0.0f, -0.027f), maxLength: maxLength)
            {
                Text = defaultName,
                Size = new Vector2(0.385f, 0.04f)
            };
            nameBox.SelectAll();
            Controls.Add(nameBox);

            okButton = new MyGuiControlImageButton("okButton", null, null, null, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
                text: MyTexts.Get(MyCommonTexts.Ok), onButtonClick: OnOk);

            cancelButton = new MyGuiControlImageButton("cancelButton", null, null, null, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                text: MyTexts.Get(MyCommonTexts.Cancel), onButtonClick: OnCancel);

            okButton.ApplyStyle("DecoratedPanel_Button");
            okButton.AllowBoundKey = true;

            cancelButton.ApplyStyle("DecoratedPanel_Button");
            cancelButton.AllowBoundKey = true;

            Vector2 okPosition = new Vector2(0.001f, 0.5f * DialogSize.Y - 0.071f);
            Vector2 halfDistance = new Vector2(0.018f, 0.0f);

            okButton.Position = okPosition - halfDistance;
            cancelButton.Position = okPosition + halfDistance;

            okButton.SetToolTip("Ok");
            cancelButton.SetToolTip("Cancel");

            Controls.Add(okButton);
            Controls.Add(cancelButton);
        }

        private void CallResultCallback(string text)
        {
            if (text == null)
            {
                return;
            }

            onOk(text);
        }

        private void ReturnOk()
        {
            if (nameBox.Text.Length <= 0)
            {
                return;
            }

            CallResultCallback(nameBox.Text);
            CloseScreen();
        }

        private void OnOk(MyGuiControlImageButton button)
        {
            ReturnOk();
        }

        private void OnCancel(MyGuiControlImageButton button)
        {
            CloseScreen();
        }

        public override string GetFriendlyName()
        {
            return "NameDialog";
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