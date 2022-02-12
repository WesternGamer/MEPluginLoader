using MEPluginLoader.GUI;
using HarmonyLib;
using Medieval.GUI.MainMenu;
using Sandbox;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI.Controls;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.Gui.Layouts;
using System;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Game;
using VRage.GameServices;
using VRage.Input.Devices.Keyboard;
using VRage.Utils;
using VRageMath;

// ReSharper disable InconsistentNaming

namespace MEPluginLoader.Patch
{
    [HarmonyPatch(typeof(MyMainMenuScreen), "RecreateControls")]
    public class Patch_CreateMainMenu
    {
        public static bool Prefix(MyMainMenuScreen __instance,
            ref MyGuiControlLabel ___m_versionLabel,
            ref MyDecoratedPanel ___m_mainPanel,
            ref MyGuiControlLabel ___m_steamUnavailableLabel,
            StringBuilder ___STEAM_INACTIVE,
            ref bool ___m_enabledBackgroundFade,
            ref MyLoadWorldInfoListResult ___m_loadWorldInfo,
            ref MyGuiControlImageButton ___m_continueGame,
            ref MyGuiControlImage ___m_continueGameThumbnail,
            MyGuiControlImage.StyleDefinition ___m_imageStyle,
            ref MyNewsControl ___m_newsControl,
            ref bool ___m_performMemoryCheck,
            ref bool ___m_performGDPRCheck)
        {
            __instance.Controls.Clear();
            float y = ((MySession.Static == null) ? 720 : 650);
            ___m_versionLabel = new MyGuiControlLabel();
            ___m_versionLabel.ApplyStyle("MainMenu_VersionLabel");
            ___m_versionLabel.Position = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            ___m_versionLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            string text = MyFinalBuildConstants.GAME_VERSION_STRINGBUILDER.ToString();
            if (MyGameService.BranchName != null && MyGameService.BranchName != "default")
            {
                text = $"{text} ({MyGameService.BranchName})";
            }
            ___m_versionLabel.Text = text;
            __instance.Controls.Add(___m_versionLabel);
            ___m_mainPanel = new MyDecoratedPanel
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM,
                Size = new Vector2(400f, y) / MyGuiConstants.GUI_OPTIMAL_SIZE,
                Position = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM)
            };
            __instance.Controls.Add(___m_mainPanel);
            ___m_steamUnavailableLabel = new MyGuiControlLabel();
            ___m_steamUnavailableLabel.ApplyStyle("MainMenu_SteamUnavailableLabel");
            ___m_steamUnavailableLabel.Text = ___STEAM_INACTIVE.ToString();
            ___m_steamUnavailableLabel.LayoutStyle = MyGuiControlLayoutStyle.Ignore;
            ___m_steamUnavailableLabel.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP;
            ___m_steamUnavailableLabel.Position = new Vector2(0f, -0.5f * ___m_mainPanel.Size.Y + 0.015f);
            ___m_steamUnavailableLabel.Visible = !MyGameService.IsOnline || !MyGameService.IsActive;
            ___m_mainPanel.Controls.Add(___m_steamUnavailableLabel);
            if (MySession.Static == null)
            {
                ___m_enabledBackgroundFade = true;
                ___m_loadWorldInfo = new MyLoadWorldInfoListResult();
                ___m_continueGame = MakeButtonMedieval(Vector2.Zero, MyStringId.GetOrCompute("MainMenu_Continue"), x => OnContinueClicked(__instance, x), MyStringId.GetOrCompute("MainMenu_Continue_Tooltip"));
                ___m_continueGame.Enabled = false;
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, ___m_continueGame);
                ___m_continueGameThumbnail = new MyGuiControlImage(___m_continueGame.Position - new Vector2(0f, ___m_continueGame.Size.Y), new Vector2(___m_continueGame.Size.X, ___m_continueGame.Size.X), null, null, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM);
                ___m_continueGameThumbnail.ApplyStyle(___m_imageStyle);
                ___m_continueGameThumbnail.CanHaveFocus = false;
                ___m_continueGameThumbnail.Visible = false;
                __instance.Controls.Add(___m_continueGameThumbnail);
                MyGuiControlParent myGuiControlParent = new MyGuiControlParent
                {
                    LayoutStyle = MyGuiControlLayoutStyle.Dynamic
                };
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, myGuiControlParent);
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyCommonTexts.ScreenMenuButtonNewWorld, x => OnNewWorldClicked(__instance, x), MyStringId.GetOrCompute("ScreenMenuButtonNewWorld_Tooltip")));
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyCommonTexts.ScreenMenuButtonLoadWorld, x => OnClickLoad(__instance, x), MyStringId.GetOrCompute("ScreenMenuButtonLoadWorld_Tooltip")));
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyCommonTexts.ScreenMenuButtonJoinWorld, x => OnJoinWorldClicked(__instance, x), MyStringId.GetOrCompute("ScreenMenuButtonJoinWorld_Tooltip")));
                MyGuiControlParent myGuiControlParent2 = new MyGuiControlParent
                {
                    LayoutStyle = MyGuiControlLayoutStyle.Dynamic
                };
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, myGuiControlParent2);
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyCommonTexts.ScreenMenuButtonOptions, x => OnOptionsButtonClicked(__instance, x)));
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyCommonTexts.ScreenMenuButtonCredits, x => OnClickCredits(__instance, x)));
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyStringId.GetOrCompute("Plugins"), _ => MyGuiScreenPluginConfig.OpenMenu()));
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyCommonTexts.ScreenMenuButtonExitToWindows, x => OnClickExitToWindows(__instance, x)));
            }
            else
            {
                ___m_enabledBackgroundFade = true;
                MyGuiControlImageButton myGuiControlImageButton = MakeButtonMedieval(Vector2.Zero, MyCommonTexts.ScreenMenuButtonSave, x => OnClickSaveWorld(__instance, x));
                MyGuiControlImageButton myGuiControlImageButton2 = MakeButtonMedieval(Vector2.Zero, MyCommonTexts.LoadScreenButtonSaveAs, x => OnClickSaveAs(__instance, x));
                if (!Sync.IsServer)
                {
                    myGuiControlImageButton.Enabled = false;
                    myGuiControlImageButton.ShowTooltipWhenDisabled = true;
                    myGuiControlImageButton.SetToolTip(MyCommonTexts.NotificationClientCannotSave);
                    myGuiControlImageButton2.Enabled = false;
                    myGuiControlImageButton.ShowTooltipWhenDisabled = true;
                    myGuiControlImageButton.SetToolTip(MyCommonTexts.NotificationClientCannotSave);
                }
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, myGuiControlImageButton);
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, myGuiControlImageButton2);
                MyGuiControlParent myGuiControlParent3 = new MyGuiControlParent
                {
                    LayoutStyle = MyGuiControlLayoutStyle.Dynamic
                };
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, myGuiControlParent3);
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyCommonTexts.LoadScreenButtonLoad, x => OnClickLoad(__instance, x)));
                MyGuiControlParent myGuiControlParent4 = new MyGuiControlParent
                {
                    LayoutStyle = MyGuiControlLayoutStyle.Dynamic
                };
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, myGuiControlParent4);
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyCommonTexts.ScreenMenuButtonOptions, x => OnOptionsButtonClicked(__instance, x)));
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyCommonTexts.ScreenMenuButtonHelp, x => OnHelpButtonClicked(__instance, x)));
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyStringId.GetOrCompute("Plugins"), _ => MyGuiScreenPluginConfig.OpenMenu()));
                ___m_mainPanel.Controls.Insert(___m_mainPanel.DecorationLayer, MakeButtonMedieval(Vector2.Zero, MyCommonTexts.ScreenMenuButtonExitToMainMenu, x => OnExitToMainMenuClick(__instance, x)));
            }
            ___m_mainPanel.Layout = new MyVerticalLayoutBehavior(MyHorizontalAlignment.CENTER, new MyGuiBorderThickness(50f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 50f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y), 10f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y);
            Vector2 vector = new Vector2(285f, 65f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            _ = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM) + new Vector2(vector.X / 2f, 0f);
            _ = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM) + new Vector2((0f - vector.X) / 2f, 0f);
            Vector2 minSizeGui = MyGuiConstants.TEXTURE_KEEN_LOGO.MinSizeGui;
            minSizeGui = new Vector2(285f, 126f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            MyGuiControlImage myGuiControlImage = new MyGuiControlImage(MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP, 54, 84), minSizeGui, null, null, null, null, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            myGuiControlImage.SetTexture("Textures\\Gui\\KeenLogo.png");
            __instance.Controls.Add(myGuiControlImage);
            MyGuiControlImage myGuiControlImage2 = new MyGuiControlImage(MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP), new Vector2(660f, 256f) / MyGuiConstants.GUI_OPTIMAL_SIZE, null, null, null, null, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            myGuiControlImage2.SetTexture("Textures\\GUI\\GameLogoLarge.png");
            __instance.Controls.Add(myGuiControlImage2);
            Vector2 vector2 = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            ___m_newsControl = new MyNewsControl
            {
                Size = new Vector2(0.44f, 0.32f),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM
            };
            ___m_newsControl.Position = new Vector2(vector2.X, ___m_mainPanel.Position.Y);
            __instance.Controls.Add(___m_newsControl);
            ___m_performMemoryCheck = true;
            if (!MySandboxGame.Config.GDPRConsent.HasValue)
            {
                ___m_performGDPRCheck = true;
            }
            else if (!MySandboxGame.Config.GDPRConsentSent.HasValue || !MySandboxGame.Config.GDPRConsentSent.Value)
            {
                TrySendConsent(__instance);
            }

            return false;
        }

        private static MyGuiControlImageButton MakeButtonMedieval(Vector2 position, MyStringId text, Action<MyGuiControlImageButton> onClick, MyStringId? tooltip = null)
        {
            return MakeButtonMedieval(position, MyTexts.Get(text), onClick, tooltip);
        }

        private static MyGuiControlImageButton MakeButtonMedieval(Vector2 position, StringBuilder text, Action<MyGuiControlImageButton> onClick, MyStringId? tooltip = null)
        {
            MyGuiControlImageButton myGuiControlImageButton = new MyGuiControlImageButton("Button", position, null, null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM, null, text, 1.08f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, MyGuiControlHighlightType.WHEN_CURSOR_OVER, onClick);
            if (tooltip.HasValue)
            {
                myGuiControlImageButton.SetToolTip(MyTexts.GetString(tooltip.Value));
            }
            myGuiControlImageButton.ApplyStyle("DecoratedPanel_Button");
            myGuiControlImageButton.LayoutStyle = MyGuiControlLayoutStyle.DynamicX;
            myGuiControlImageButton.AllowBoundKey = true;
            myGuiControlImageButton.BoundKey = MyKeys.Enter;
            return myGuiControlImageButton;
        }

        private static void OnContinueClicked(MyMainMenuScreen __instance, MyGuiControlBase sender)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "OnContinueClicked");
            method.Invoke(__instance, new object[] { sender });
        }

        private static void OnNewWorldClicked(MyMainMenuScreen __instance, MyGuiControlBase sender)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "OnNewWorldClicked");
            method.Invoke(__instance, new object[] { sender });
        }

        private static void OnClickLoad(MyMainMenuScreen __instance, MyGuiControlBase sender)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "OnClickLoad");
            method.Invoke(__instance, new object[] { sender });
        }

        private static void OnJoinWorldClicked(MyMainMenuScreen __instance, MyGuiControlBase sender)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "OnJoinWorldClicked");
            method.Invoke(__instance, new object[] { sender });
        }

        private static void OnOptionsButtonClicked(MyMainMenuScreen __instance, MyGuiControlBase sender)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "OnOptionsButtonClicked");
            method.Invoke(__instance, new object[] { sender });
        }

        private static void OnClickCredits(MyMainMenuScreen __instance, MyGuiControlBase sender)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "OnClickCredits");
            method.Invoke(__instance, new object[] { sender });
        }

        private static void OnClickExitToWindows(MyMainMenuScreen __instance, MyGuiControlBase sender)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "OnClickExitToWindows");
            method.Invoke(__instance, new object[] { sender });
        }

        private static void OnClickSaveWorld(MyMainMenuScreen __instance, MyGuiControlBase sender)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "OnClickSaveWorld");
            method.Invoke(__instance, new object[] { sender });
        }

        private static void OnClickSaveAs(MyMainMenuScreen __instance, MyGuiControlBase sender)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "OnClickSaveAs");
            method.Invoke(__instance, new object[] { sender });
        }

        private static void OnHelpButtonClicked(MyMainMenuScreen __instance, MyGuiControlBase sender)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "OnHelpButtonClicked");
            method.Invoke(__instance, new object[] { sender });
        }

        private static void OnExitToMainMenuClick(MyMainMenuScreen __instance, MyGuiControlBase sender)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "OnExitToMainMenuClick");
            method.Invoke(__instance, new object[] { sender });
        }

        private static void TrySendConsent(MyMainMenuScreen __instance)
        {
            MethodInfo method = AccessTools.Method(typeof(MyMainMenuScreen), "TrySendConsent");
            method.Invoke(__instance, null);
        }
    }


    /*[HarmonyPatch(typeof(MyGuiScreenMainMenu), "CreateInGameMenu")]
    public static class Patch_CreateInGameMenu
    {
        public static void Postfix(MyGuiScreenMainMenu __instance, Vector2 leftButtonPositionOrigin, ref Vector2 lastButtonPosition)
        {
            Patch_CreateMainMenu.Postfix(__instance, leftButtonPositionOrigin, ref lastButtonPosition);
        }
    }*/
}