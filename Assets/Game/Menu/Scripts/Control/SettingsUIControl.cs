using Ballance2.Game.Remaster;
using Ballance2.Game;
using Ballance2.Package;
using Ballance2.Services;
using Ballance2.Services.I18N;
using Ballance2.UI.Core;
using Ballance2.UI.Core.Controls;
using System;
using System.Text;
using UnityEngine;

namespace Ballance2.Menu
{
    public class SettingsUIControl
    {
        public static void Create()
        {
            var GameUIManager = Ballance2.Services.GameUIManager.Instance;
            var MessageCenter = MenuManager.MessageCenter;
            var package = GamePackage.GetSystemPackage();

            var PageSettings = GameUIManager.RegisterPage("PageSettings", "PageCommon");
            var PageSettingsInGame = GameUIManager.RegisterPage("PageSettingsInGame", "PageCommon");
            var PageSettingsAudio = GameUIManager.RegisterPage("PageSettingsAudio", "PageCommon");
            var PageSettingsControls = GameUIManager.RegisterPage("PageSettingsControls", "PageCommon");
            var PageSettingsControlsMobile = GameUIManager.RegisterPage("PageSettingsControlsMobile", "PageCommon");
            var PageSettingsGraphics = GameUIManager.RegisterPage("PageSettingsGraphics", "PageFull");
            var PageLanguage = GameUIManager.RegisterPage("PageLanguage", "PageCommon");
            var PageApplyLangDialog = GameUIManager.RegisterPage("PageApplyLangDialog", "PageCommon");
            var PagePackageManager = GameUIManager.RegisterPage("PagePackageManager", "PageFull");
            var PagePackageManagerInfo = GameUIManager.RegisterPage("PagePackageManagerInfo", "PageFull");
            var PagePackageManagerTrust = GameUIManager.RegisterPage("PagePackageManagerTrust", "PageFull");

            PageSettings.CreateContent(package);
            PageSettingsAudio.CreateContent(package);
            PageSettingsControls.CreateContent(package);
            PageSettingsControlsMobile.CreateContent(package);
            PageSettingsGraphics.CreateContent(package);
            RemasterSettingsPanel.Install(PageSettingsGraphics.Content);
            PageSettingsInGame.CreateContent(package);
            PageLanguage.CreateContent(package);
            PageApplyLangDialog.CreateContent(package);
            PagePackageManager.CreateContent(package);
            PagePackageManagerInfo.CreateContent(package);
            PagePackageManagerTrust.CreateContent(package);

            PageSettingsControls.OnShow += (options) =>
            {
                if (!GameManager.DebugMode)
                {
                    PageSettingsControls.Content.Find("ScrollViewVetical/Viewport/ContentView/ButtonKeyChooseControlKeyUp").gameObject.SetActive(false);
                    PageSettingsControls.Content.Find("ScrollViewVetical/Viewport/ContentView/ButtonKeyChooseControlKeyDown").gameObject.SetActive(false);
                }
            };

            GameTimer.Delay(1.0f, () =>
            {
                BindSettingsUI(MessageCenter);
            });
        }

        private static void BindSettingsUI(GameUIMessageCenter MessageCenter)
        {
            var GameSettings = GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME);
            var GameUIManager = Ballance2.Services.GameUIManager.Instance;

            // Set up data bindings.
            // Audio.
            var updateVoiceMain = MessageCenter.SubscribeValueBinder("VoiceMain", (val) =>
            {
                GameSettings.SetFloat(SettingConstants.SettingsVoiceMain, (float)val);
                GameSettings.NotifySettingsUpdate(SettingConstants.SettingsVoice);
            });
            var updateVoiceUI = MessageCenter.SubscribeValueBinder("VoiceUI", (val) =>
            {
                GameSettings.SetFloat(SettingConstants.SettingsVoiceUI, (float)val);
                GameSettings.NotifySettingsUpdate(SettingConstants.SettingsVoice);
            });
            var updateVioceBackground = MessageCenter.SubscribeValueBinder("VioceBackground", (val) =>
            {
                GameSettings.SetFloat(SettingConstants.SettingsVoiceBackground, (float)val);
                GameSettings.NotifySettingsUpdate(SettingConstants.SettingsVoice);
            });
            // Controls.
            var updateControlKeyForward = MessageCenter.SubscribeValueBinder("ControlKeyForward", (val) =>
            {
                GameSettings.SetInt(SettingConstants.SettingsControlKeyFront, (int)val);
            });
            var updateControlKeyBack = MessageCenter.SubscribeValueBinder("ControlKeyBack", (val) =>
            {
                GameSettings.SetInt(SettingConstants.SettingsControlKeyBack, (int)val);
            });
            var updateControlKeyUp = MessageCenter.SubscribeValueBinder("ControlKeyUp", (val) =>
            {
                GameSettings.SetInt(SettingConstants.SettingsControlKeyUp, (int)val);
            });
            var updateControlKeyDown = MessageCenter.SubscribeValueBinder("ControlKeyDown", (val) =>
            {
                GameSettings.SetInt(SettingConstants.SettingsControlKeyDown, (int)val);
            });
            var updateControlKeyLeft = MessageCenter.SubscribeValueBinder("ControlKeyLeft", (val) =>
            {
                GameSettings.SetInt(SettingConstants.SettingsControlKeyLeft, (int)val);
            });
            var updateControlKeyRight = MessageCenter.SubscribeValueBinder("ControlKeyRight", (val) =>
            {
                GameSettings.SetInt(SettingConstants.SettingsControlKeyRight, (int)val);
            });
            var updateControlKeyOverlookCam = MessageCenter.SubscribeValueBinder("ControlKeyOverlookCam", (val) =>
            {
                GameSettings.SetInt(SettingConstants.SettingsControlKeyUpCam, (int)val);
            });
            var updateControlKeyRoateCam = MessageCenter.SubscribeValueBinder("ControlKeyRoateCam", (val) =>
            {
                GameSettings.SetInt(SettingConstants.SettingsControlKeyRoate, (int)val);
            });
            var updateControlKeyRoateCam2 = MessageCenter.SubscribeValueBinder("ControlKeyRoateCam2", (val) =>
            {
                GameSettings.SetInt(SettingConstants.SettingsControlKeyRoate2, (int)val);
            });
            var updateControlReverse = MessageCenter.SubscribeValueBinder("ControlReverse", (val) =>
            {
                GameSettings.SetBool(SettingConstants.SettingsControlKeyReverse, (bool)val);
            });
            var updateControlUISize = MessageCenter.SubscribeValueBinder("ControlUISize", (val) =>
            {
                GameSettings.SetFloat(SettingConstants.SettingsControlKeySize, (float)val);
                GameSettings.NotifySettingsUpdate(SettingConstants.SettingsControl);
            });

            MessageCenter.SubscribeEvent("BtnSettingsGraphicsClick", () => GameUIManager.GoPage("PageSettingsGraphics"));
            MessageCenter.SubscribeEvent("BtnSettingsControlsClick", () =>
            {
                if (updateControlKeyForward != null) updateControlKeyForward.Invoke(GameSettings.GetInt(SettingConstants.SettingsControlKeyFront));
                if (updateControlKeyBack != null) updateControlKeyBack.Invoke(GameSettings.GetInt(SettingConstants.SettingsControlKeyBack));
                if (updateControlKeyUp != null) updateControlKeyUp.Invoke(GameSettings.GetInt(SettingConstants.SettingsControlKeyUp));
                if (updateControlKeyDown != null) updateControlKeyDown.Invoke(GameSettings.GetInt(SettingConstants.SettingsControlKeyDown));
                if (updateControlKeyLeft != null) updateControlKeyLeft.Invoke(GameSettings.GetInt(SettingConstants.SettingsControlKeyLeft));
                if (updateControlKeyRight != null) updateControlKeyRight.Invoke(GameSettings.GetInt(SettingConstants.SettingsControlKeyRight));
                if (updateControlKeyOverlookCam != null) updateControlKeyOverlookCam.Invoke(GameSettings.GetInt(SettingConstants.SettingsControlKeyUpCam));
                if (updateControlKeyRoateCam != null) updateControlKeyRoateCam2.Invoke(GameSettings.GetInt(SettingConstants.SettingsControlKeyRoate));
                if (updateControlKeyRoateCam2 != null) updateControlKeyRoateCam2.Invoke(GameSettings.GetInt(SettingConstants.SettingsControlKeyRoate2));
                if (updateControlReverse != null) updateControlReverse.Invoke(GameSettings.GetBool(SettingConstants.SettingsControlKeyReverse));
                if (updateControlUISize != null) updateControlUISize.Invoke(GameSettings.GetFloat(SettingConstants.SettingsControlKeySize));

#if UNITY_IOS || UNITY_ANDROID
                GameUIManager.GoPage("PageSettingsControlsMobile");
#else
                GameUIManager.GoPage("PageSettingsControls") ;
#endif
            });
            MessageCenter.SubscribeEvent("BtnSettingsAudioClick", () =>
            {
                if (updateVioceBackground != null) updateVioceBackground.Invoke(GameSettings.GetFloat(SettingConstants.SettingsVoiceBackground));
                if (updateVoiceUI != null) updateVoiceUI.Invoke(GameSettings.GetFloat(SettingConstants.SettingsVoiceUI));
                if (updateVoiceMain != null) updateVoiceMain.Invoke(GameSettings.GetFloat(SettingConstants.SettingsVoiceMain));
                GameUIManager.GoPage("PageSettingsAudio");
            });
            MessageCenter.SubscribeEvent("BtnSettingsPackageClick", () =>
            {
                GameUIManager.GoPage("PagePackageManager");
            });

            // Language.
            MessageCenter.SubscribeEvent("BtnSettingsLanguageClick", () => { GameUIManager.GoPage("PageLanguage"); });
            MessageCenter.SubscribeEvent("BtnChineseSimplifiedClick", () => { ApplyLanguage(SystemLanguage.ChineseSimplified); });
            MessageCenter.SubscribeEvent("BtnChineseTraditionalClick", () => { ApplyLanguage(SystemLanguage.ChineseTraditional); });
            MessageCenter.SubscribeEvent("BtnEnglishClick", () => { ApplyLanguage(SystemLanguage.English); });
            MessageCenter.SubscribeEvent("BtnRussianClick", () => { ApplyLanguage(SystemLanguage.Russian); });

            MessageCenter.SubscribeEvent("BtnLangBackClick", () =>
            {
                GameUIManager.BackPreviusPage();
                GameUIManager.BackPreviusPage();
            });

            // Save settings.
            MessageCenter.SubscribeEvent("BtnSettingsGraphicsBackClick", () =>
            {
                GameSettings.NotifySettingsUpdate(SettingConstants.SettingsVideo);
                GameUIManager.BackPreviusPage();
            });
            MessageCenter.SubscribeEvent("BtnSettingsControlsBackClick", () =>
            {
                GameSettings.NotifySettingsUpdate(SettingConstants.SettingsControl);
                GameUIManager.BackPreviusPage();
            });
            MessageCenter.SubscribeEvent("BtnSettingsAudioBackClick", () =>
            {
                GameSettings.NotifySettingsUpdate(SettingConstants.SettingsVoice);
                GameUIManager.BackPreviusPage();
            });

            // Restart the game.
            MessageCenter.SubscribeEvent("BtnRestartGameClick", () => { GameManager.Instance.RestartGame(); });
        }

        private static void ApplyLanguage(SystemLanguage language)
        {
            GameSettingsManager.GetSettings(GamePackageManager.SYSTEM_PACKAGE_NAME).SetInt("language", (int)language);
            PlayerPrefs.Save();
            if (I18NProvider.GetCurrentLanguage() != language)
                GameUIManager.Instance.GoPage("PageApplyLangDialog");
            else
                GameUIManager.Instance.BackPreviusPage();
        }
    }
}
