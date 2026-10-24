using System;
using System.IO;
using System.Linq;
using System.Xml;
using Ballance2.Services.I18N;
using Ballance2.UI.Core.Controls;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Editor
{
  // Run with -batchmode -executeMethod Ballance2.Editor.LocalizationChecks.Run.
  public static class LocalizationChecks
  {
    private static void Require(bool value, string message)
    {
      if (!value) throw new Exception("Localization check failed: " + message);
    }

    public static void Run()
    {
      try
      {
        I18NProvider.ClearAllLanguageResources();
        I18NProvider.SetCurrentLanguage(SystemLanguage.Russian);
        var files = Directory.GetFiles("Assets", "PackageLanguageRes*.xml", SearchOption.AllDirectories)
          .Concat(new[] { "Assets/Resources/StaticLangResource.xml" }).ToArray();
        foreach (var file in files)
        {
          var xml = File.ReadAllText(file);
          Require(I18NProvider.LoadLanguageResources(xml), file);
          var preloaded = I18NProvider.PreLoadLanguageResources(xml);
          Require(preloaded != null && preloaded.Count > 0, "preload " + file);
        }
        Require(I18N.Tr("core.ui.MainMenuSettings") == "Настройки", "Russian menu");
        Require(I18N.Tr("missing.test.key", "fallback") == "fallback", "default text");
        Require(I18N.TrF("core.ui.SettingsVersion", new object[] { "1.0" }) == "Версия: 1.0", "formatting");
        const string partial = "<I18n><Language name='English'><Text name='test.fallback'>English</Text><Text name='test.override'>Old</Text></Language><Language name='Russian'><!-- comment --><Text name='test.override'>Русский</Text></Language></I18n>";
        Require(I18NProvider.LoadLanguageResources(partial), "partial translation");
        Require(I18N.Tr("test.fallback") == "English" && I18N.Tr("test.override") == "Русский", "per-key fallback");
        var pre = I18NProvider.PreLoadLanguageResources(partial);
        Require(pre["test.fallback"] == "English" && pre["test.override"] == "Русский", "preload fallback");
        Require(I18NProvider.LoadLanguageResources("<I18n/>"), "empty third-party resource");

        var bank = AssetImporter.GetAtPath("Assets/Game/Common/Fonts/bgt65.ttf") as TrueTypeFontImporter;
        var roboto = AssetDatabase.LoadAssetAtPath<Font>("Assets/Plugins/Graphy - Ultimate Stats Monitor/Font/Roboto/Roboto-Regular.ttf");
        Require(bank != null && bank.fontReferences.Contains(roboto) && bank.fontNames.Contains("Roboto"), "embedded font fallback");
        const string alphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        roboto.RequestCharactersInTexture(alphabet, 20);
        foreach (var c in alphabet) Require(roboto.HasCharacter(c), "Cyrillic glyph " + c);

        var languagePage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Menu/Prefabs/Pages/PageLanguage.prefab");
        var button = languagePage.transform.Find("BtnRussian");
        Require(button != null, "Russian button");
        Require(button.GetComponentInChildren<I18NText>().defaultString == "Русский", "native language label");
        var serializedButton = new SerializedObject(button.GetComponent<Button>());
        Require(serializedButton.FindProperty("m_OnClick.m_PersistentCalls.m_Calls.Array.data[0].m_Arguments.m_StringArgument").stringValue == "BtnRussianClick", "button event binding");
        var startPage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Menu/Prefabs/Pages/PageStart.prefab");
        Require(startPage.GetComponentsInChildren<I18NText>(true).Count(t => t.i18N && t.resourceKey.StartsWith("core.level.")) == 13, "all thirteen level buttons localized");

        var checkedTexts = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Menu/Prefabs", "Assets/System/Prefabs" }))
        {
          var path = AssetDatabase.GUIDToAssetPath(guid);
          var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
          foreach (var text in prefab.GetComponentsInChildren<Text>(true))
          {
            var localized = text as I18NText;
            if (localized != null && localized.i18N && !string.IsNullOrEmpty(localized.resourceKey))
            {
              Require(I18NProvider.GetLanguageString(localized.resourceKey) != null, path + " missing " + localized.resourceKey);
              checkedTexts++;
            }
            else if (!string.IsNullOrWhiteSpace(text.text))
              Debug.Log("LOCALIZATION_LITERAL " + path + "/" + text.name + " = " + text.text.Replace("\n", "\\n"));
          }
        }
        // Selecting a different language must replace Russian resources again.
        I18NProvider.ClearAllLanguageResources();
        I18NProvider.SetCurrentLanguage(SystemLanguage.English);
        I18NProvider.LoadLanguageResources(File.ReadAllText("Assets/Game/PackageLanguageRes.xml"));
        Require(I18N.Tr("core.ui.MainMenuSettings") == "Settings", "switch back to English");
        Debug.Log("LOCALIZATION_CHECKS_PASSED: " + checkedTexts + " prefab labels; Cyrillic, resources, fallback and language button.");
      }
      finally
      {
        I18NProvider.ClearAllLanguageResources();
      }
    }

    public static void Render()
    {
      Run();
      I18NProvider.SetCurrentLanguage(SystemLanguage.Russian);
      I18NProvider.LoadLanguageResources(File.ReadAllText("Assets/Resources/StaticLangResource.xml"));
      I18NProvider.LoadLanguageResources(File.ReadAllText("Assets/Game/PackageLanguageRes.xml"));
      var scene = EditorSceneManager.NewPreviewScene();
      var root = new GameObject("LocalizationPreview");
      UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
      var cameraObject = new GameObject("Camera", typeof(Camera));
      cameraObject.transform.SetParent(root.transform);
      var camera = cameraObject.GetComponent<Camera>();
      camera.scene = scene;
      camera.clearFlags = CameraClearFlags.SolidColor;
      camera.backgroundColor = new Color(0.06f, 0.08f, 0.1f);
      camera.orthographic = true;
      var target = new RenderTexture(1024, 768, 24);
      camera.targetTexture = target;
      var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
      canvasObject.transform.SetParent(root.transform);
      var canvas = canvasObject.GetComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceCamera;
      canvas.worldCamera = camera;
      canvas.planeDistance = 10;
      var scaler = canvasObject.AddComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1024, 768);
      scaler.matchWidthOrHeight = 0.5f;
      Directory.CreateDirectory("Builds/checks/localization");
      try
      {
        foreach (var name in new[] { "PageMain", "PageLanguage", "PageStart", "PageSettings", "PageSettingsGraphics", "PageSettingsControls", "PageSettingsAudio", "PageAboutProject", "PageApplyLangDialog", "PageGameWin", "PageHighscoreEntry", "GameTutorialUI" })
        {
          var path = name == "GameTutorialUI" ? "Assets/Game/GamePlay/Prefabs/Tutorial/GameTutorialUI.prefab" : "Assets/Game/Menu/Prefabs/Pages/" + name + ".prefab";
          var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
          var page = UnityEngine.Object.Instantiate(prefab, canvas.transform, false);
          page.SetActive(true);
          foreach (var animator in page.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
          foreach (var group in page.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1;
          foreach (var text in page.GetComponentsInChildren<I18NText>(true))
            if (text.i18N && !string.IsNullOrEmpty(text.resourceKey)) text.text = I18N.Tr(text.resourceKey);
          // Populate representative dynamic labels; these are not prefab literals at runtime.
          foreach (var choose in page.GetComponentsInChildren<KeyChoose>(true)) choose.value = KeyCode.LeftShift;
          if (name == "PageHighscoreEntry") page.transform.Find("TextScore").GetComponent<Text>().text = "1000 " + I18N.Tr("core.ui.WinUIPoints");
          for (int pass = 0; pass < 3; pass++)
          {
            foreach (var rect in page.GetComponentsInChildren<RectTransform>(true).Reverse())
              LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            Canvas.ForceUpdateCanvases();
          }
          foreach (var scroll in page.GetComponentsInChildren<ScrollRect>(true)) scroll.verticalNormalizedPosition = 1;
          if (name == "GameTutorialUI")
          {
            var tutorial = page.transform.Find("Panel/Text").GetComponent<Text>();
            for (int step = 1; step <= 10; step++)
            {
              tutorial.text = I18N.Tr("core.ui.TutorialText" + step);
              Require(tutorial.preferredHeight <= tutorial.rectTransform.rect.height + 1, "tutorial text fits: " + step);
            }
            tutorial.text = I18N.Tr("core.ui.TutorialText7");
          }
          camera.Render();
          var previous = RenderTexture.active;
          RenderTexture.active = target;
          var image = new Texture2D(1024, 768, TextureFormat.RGB24, false);
          image.ReadPixels(new Rect(0, 0, 1024, 768), 0, 0);
          image.Apply();
          File.WriteAllBytes("Builds/checks/localization/" + name + ".png", image.EncodeToPNG());
          RenderTexture.active = previous;
          UnityEngine.Object.DestroyImmediate(image);
          UnityEngine.Object.DestroyImmediate(page);
        }
        Debug.Log("LOCALIZATION_PREVIEWS_RENDERED");
      }
      finally
      {
        UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(target);
        EditorSceneManager.ClosePreviewScene(scene);
        I18NProvider.ClearAllLanguageResources();
      }
    }
  }
}
