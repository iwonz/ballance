using System;
using System.IO;
using System.Linq;
using Ballance2.Game.Remaster;
using Ballance2.Menu;
using Ballance2.Services.I18N;
using Ballance2.UI.Core.Controls;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Editor
{
  public static class RemasterUIVisualChecks
  {
    private static void RequireCentered(RectTransform rect, SystemLanguage language, Vector2Int size)
    {
      if (Mathf.Abs(rect.anchorMin.x + rect.anchorMax.x - 1) > .001f)
        throw new InvalidOperationException("Off-center main menu: " + language + " " + size);
    }

    public static void Run()
    {
      LocalizationChecks.Run();
      var scene = EditorSceneManager.NewPreviewScene();
      var root = new GameObject("Remaster UI preview");
      UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
      var cameraObject = new GameObject("Camera", typeof(Camera));
      cameraObject.transform.SetParent(root.transform);
      var camera = cameraObject.GetComponent<Camera>();
      camera.scene = scene; camera.clearFlags = CameraClearFlags.SolidColor;
      camera.backgroundColor = new Color(.42f, .57f, .65f); camera.orthographic = true;
      var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
      canvasObject.transform.SetParent(root.transform);
      var canvas = canvasObject.GetComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
      var scaler = canvasObject.GetComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
      string directory = "Builds/checks/remaster-ui";
      Directory.CreateDirectory(directory);
      try {
        foreach (var size in new[] { new Vector2Int(720, 1280), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3840, 2160) }) {
          var target = new RenderTexture(size.x, size.y, 24);
          camera.targetTexture = target;
          try {
            foreach (var language in new[] { SystemLanguage.Russian, SystemLanguage.English, SystemLanguage.ChineseSimplified, SystemLanguage.ChineseTraditional }) {
              I18NProvider.ClearAllLanguageResources(); I18NProvider.SetCurrentLanguage(language);
              I18NProvider.LoadLanguageResources(File.ReadAllText("Assets/Resources/StaticLangResource.xml"));
              I18NProvider.LoadLanguageResources(File.ReadAllText("Assets/Game/PackageLanguageRes.xml"));
              var names = Directory.GetFiles("Assets/Game/Menu/Prefabs/Pages", "*.prefab")
                .Select(Path.GetFileNameWithoutExtension).Where(name => name != "PageSettingsGraphics")
                .Concat(Directory.GetFiles("Assets/Game/Menu/Prefabs/ManagerUI", "Page*.prefab").Select(Path.GetFileNameWithoutExtension))
                .Concat(new[] { "GamePlayUI", "GlobalIngameLoading", "GlobalGameErrorDialog", "IntroUI" })
                .Concat(Directory.GetFiles("Assets/Game/Menu/Prefabs/Keypad", "*.prefab").Select(Path.GetFileNameWithoutExtension));
              foreach (string name in names) {
                string folder = name.StartsWith("Keypad") ? "Assets/Game/Menu/Prefabs/Keypad/" :
                  name == "GlobalGameErrorDialog" ? "Assets/System/Prefabs/System/GlobalDialog/" :
                  name == "GlobalIngameLoading" ? "Assets/System/Prefabs/Loading/" :
                  name == "IntroUI" ? "Assets/Game/Intro/Prefab/" :
                  name == "GamePlayUI" ? "Assets/Game/Menu/Prefabs/Game/" : name.StartsWith("PagePackage") ? "Assets/Game/Menu/Prefabs/ManagerUI/" : "Assets/Game/Menu/Prefabs/Pages/";
                bool full = new[] { "PageMain", "PageStart", "PageAbout", "PageHighscore", "PageStartCustomLevel", "PageLightZone", "PageEndScore", "GamePlayUI", "GlobalIngameLoading", "GlobalGameErrorDialog", "IntroUI", "PagePackageManager", "PagePackageManagerInfo", "PagePackageManagerTrust" }.Contains(name);
                full |= name.StartsWith("Keypad");
                var host = new GameObject("Page container", typeof(RectTransform)).GetComponent<RectTransform>();
                host.SetParent(canvas.transform, false);
                RemasterTheme.Place(host, full ? 0 : .5f, 0, full ? 1 : .5f, 1);
                if (!full) host.sizeDelta = new Vector2(460, 0);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + name + ".prefab");
                if (prefab == null) throw new InvalidOperationException("Missing UI fixture: "+folder+name+".prefab");
                var page = UnityEngine.Object.Instantiate(prefab, host, false);
                try {
                  foreach (var label in page.GetComponentsInChildren<I18NText>(true))
                    if (label.i18N && !string.IsNullOrEmpty(label.resourceKey)) label.text = I18N.Tr(label.resourceKey);
                  RemasterTheme.Place((RectTransform)page.transform, 0, 0, 1, 1);
                  RemasterTheme.Apply(page, name != "GamePlayUI" && !name.StartsWith("Keypad"));
                  Canvas.ForceUpdateCanvases();
                  if (name.StartsWith("Keypad")) {
                    foreach (var direction in page.GetComponentsInChildren<Ballance2.Menu.Touch.SimpleTouchDirectionKeyController>(true))
                      foreach (var icon in new[] { direction.ImageForward, direction.ImageForwardLeft, direction.ImageForwardRight,
                          direction.ImageBack, direction.ImageBackLeft, direction.ImageBackRight, direction.ImageLeft, direction.ImageRight })
                        if (icon.sprite == null || icon.sprite.texture.name != "InterfaceIcons")
                          throw new InvalidOperationException("Touch direction must use the shared icon atlas");
                    foreach (var stick in page.GetComponentsInChildren<Ballance2.Menu.Touch.KeyPadJoystickController>(true))
                      if (stick.JoystickImage.sprite == null || stick.JoystickImage.sprite.texture.name != "InterfaceIcons")
                        throw new InvalidOperationException("Touch thumb must use the shared icon atlas");
                  } else if (name == "GamePlayUI") {
                    var control = page.GetComponent<GamePlayUIControl>();
                    control._ScoreBoardActive.gameObject.SetActive(false);
                    control._TextDebugMode.SetActive(false);
                    RemasterHud.Install(control); control.SetPointText(999);
                    page.transform.Find("Life summary/Lives").GetComponent<Text>().text = "3";
                    if (page.transform.Find("Environment") != null || page.transform.Find("Run eligibility") != null || control._ScoreText.transform.parent.GetComponent<Image>().enabled ||
                        page.transform.Find("Life summary").GetComponent<Image>() != null)
                      throw new InvalidOperationException("HUD must not have block panels, a preset heading or practice warning");
                  } else if (name == "GlobalIngameLoading") page.GetComponent<Ballance2.UI.Utils.GameGlobalIngameLoading>().Install();
                  else if (name == "IntroUI") { RemasterIntro.Install((RectTransform)page.transform); page.GetComponent<RemasterIntro>().Animate(4); }
                  else if (name == "PageAbout") RemasterAbout.Install((RectTransform)page.transform);
                  else if (name == "PageAboutProject") RemasterAbout.ReplaceIllustration((RectTransform)page.transform);
                  else if (name == "PagePackageManager") {
                    var content = page.GetComponent<PackageManagerUIControl>().ScrollView.GetComponent<ScrollRect>().content;
                    var item = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Menu/Prefabs/ManagerUI/ItemPackagePrefab.prefab");
                    var ids = new[] { "core", "core.assets.skys", "core.sounds", "core.sounds.music", "mod.invlife", "mod.selfkill", "mod.speedball" };
                    content.sizeDelta = new Vector2(content.sizeDelta.x,ids.Length*80);
                    for (int i = 0; i < ids.Length; i++) {
                      var row = (RectTransform)UnityEngine.Object.Instantiate(item,content,false).transform;
                      row.gameObject.SetActive(true);
                      row.anchorMin = new Vector2(0,1); row.anchorMax = Vector2.one; row.pivot = new Vector2(.5f,1);
                      row.anchoredPosition = new Vector2(0,-i*80); row.sizeDelta = new Vector2(0,80);
                      row.Find("TextTitle").GetComponent<Text>().text = ids[i];
                      row.Find("TextIntrod").GetComponent<Text>().text = "";
                      foreach (string child in new[] { "TextNotUnloadableText", "ButtonUnload", "ButtonSettings", "ToggleEnable" })
                        row.Find(child).gameObject.SetActive(false);
                      RemasterPackages.Row(row,ids[i]);
                      if (row.Find("ImageLogo").GetComponent<Image>().sprite.texture.name != "InterfaceIcons")
                        throw new InvalidOperationException("Legacy package icon: " + ids[i]);
                    }
                  }
                  else if (name == "PagePackageManagerInfo") {
                    var details = page.GetComponent<PackageManagerContentUIControl>();
                    details.ContentNotFound.gameObject.SetActive(false);
                    details.TextPackageName.text = "Reballance";
                    details.TextPackageVersion.text = Application.version;
                    details.TextPackageMoreInfos.text = "Core materials and mechanisms";
                    details.InputPackageName.text = "core";
                    RemasterPackages.Details(details,"core");
                  }
                  else if (name == "PageHighscore") {
                    page.GetComponent<HighscoreUIControl>().Install();
                    page.transform.Find("Panel/TextLevelName").GetComponent<Text>().text = "01";
                  }
                  else if (name == "PageMain" || name == "PageStart")
                    RemasterMenuLayout.Install((RectTransform)page.transform, name == "PageStart", () => {});
                  Canvas.ForceUpdateCanvases();
                  LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)page.transform);
                  if (name == "PagePackageManagerInfo")
                    page.GetComponent<PackageManagerContentUIControl>().ScrollView.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
                  camera.Render();
                  if (name == "PageMain") {
                    var button = (RectTransform)page.transform.Find("ButtonStart");
                    RequireCentered(button, language, size);
                    RequireCentered((RectTransform)page.transform.Find("Menu shade"), language, size);
                  }
                  if (name == "PageHighscore" && page.transform.Find("Record environment") != null)
                    throw new InvalidOperationException("Records must be independent of environment");
                  if (name == "PageLanguage")
                    foreach (string key in new[] { "Russian", "English", "ChineseSimplified", "ChineseTraditional" }) {
                      var flag = page.transform.Find("Btn" + key + "/Language flag")?.GetComponent<Image>();
                      if (flag == null || flag.sprite == null) throw new InvalidOperationException("Missing language flag: " + key);
                    }
                  if (name == "GamePlayUI") {
                    foreach (string path in new[] { "ScoreBoard/ScoreText", "ScoreBoard/Counter caption", "Life summary/Lives", "Life summary/Counter caption" }) {
                      var label = page.transform.Find(path).GetComponent<Text>();
                      if (label.cachedTextGenerator.vertexCount < 4)
                        throw new InvalidOperationException("Clipped HUD text: " + path + " " + language + " " + size);
                    }
                  }
                  if (name == "GlobalIngameLoading")
                    foreach (var label in page.GetComponentsInChildren<Text>())
                      if (label.cachedTextGenerator.vertexCount < 4) throw new InvalidOperationException("Clipped loading text: " + language + " " + size);
                  if (name == "PageGameQuitAsk") {
                    var message = page.transform.Find("Text").GetComponent<Text>();
                    if (message.preferredHeight > message.rectTransform.rect.height + .5f)
                      throw new InvalidOperationException("Clipped exit warning: " + language + " " + size);
                  }
                  foreach (var button in page.GetComponentsInChildren<Button>()) {
                    if (!button.gameObject.activeInHierarchy) continue;
                    var rect = ((RectTransform)button.transform).rect;
                    if (rect.width <= 0 || rect.height <= 0) throw new InvalidOperationException("Invisible button: " + name + "/" + button.name);
                  }
                  var previous = RenderTexture.active; RenderTexture.active = target;
                  var capture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                  try {
                    capture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); capture.Apply();
                    File.WriteAllBytes(directory + "/" + name + "-" + language + "-" + size.x + "x" + size.y + ".png", capture.EncodeToPNG());
                  } finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(capture); }
                  if (language == SystemLanguage.English && size.x == 1920) {
                    foreach (var button in page.GetComponentsInChildren<Button>(true))
                      if (button.GetComponentInChildren<Text>(true) == null) Debug.Log("ICON_BUTTON: " + name + "/" + button.name);
                    foreach (var image in page.GetComponentsInChildren<Image>())
                      if (image.enabled && image.sprite != null)
                        Debug.Log("UI_IMAGE: " + name + "/" + image.name + " = " + image.sprite.texture.name);
                  }
                } finally { UnityEngine.Object.DestroyImmediate(host.gameObject); }
              }
            }
          } finally { camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(target); }
        }
        Debug.Log("REMASTER_UI_PREVIEWS_RENDERED: four languages at portrait, Full HD, QHD and 4K.");
      } finally {
        UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(scene);
        I18NProvider.ClearAllLanguageResources();
      }
    }
  }
}
