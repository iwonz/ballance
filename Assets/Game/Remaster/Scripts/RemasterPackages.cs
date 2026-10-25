using Ballance2.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace Ballance2.Game.Remaster
{
  public static class RemasterPackages
  {
    private static void Icon(Image image, string id)
    {
      RemasterIcon icon;
      switch (id) {
        case "core": icon = RemasterIcon.Ball; break;
        case "core.assets.skys": icon = RemasterIcon.Cloud; break;
        case "core.sounds": icon = RemasterIcon.Sound; break;
        case "core.sounds.music": icon = RemasterIcon.Music; break;
        case "mod.invlife": icon = RemasterIcon.InfiniteLives; break;
        case "mod.selfkill": icon = RemasterIcon.Restart; break;
        case "mod.speedball": icon = RemasterIcon.Speed; break;
        default: icon = RemasterIcon.Package; break;
      }
      RemasterIcons.Set(image, icon);
    }

    public static void Row(RectTransform item, string id)
    {
      RemasterTheme.Apply(item.gameObject);
      foreach (var shadow in item.GetComponentsInChildren<Shadow>(true)) shadow.enabled = false;
      var logo = item.Find("ImageLogo").GetComponent<Image>();
      RemasterTheme.Place(logo.rectTransform,.015f,.16f,.08f,.84f); Icon(logo,id);
      var title = item.Find("TextTitle").GetComponent<Text>();
      RemasterTheme.Place(title.rectTransform,.095f,.55f,.68f,.94f);
      title.fontSize = 18; title.resizeTextForBestFit = true; title.resizeTextMinSize = 13; title.resizeTextMaxSize = 18;
      var introduction = item.Find("TextIntrod").GetComponent<Text>();
      RemasterTheme.Place(introduction.rectTransform,.095f,.08f,.68f,.54f);
      introduction.fontSize = 14; introduction.resizeTextForBestFit = true; introduction.resizeTextMinSize = 11; introduction.resizeTextMaxSize = 14;
      var fixedPackage = item.Find("TextNotUnloadableText").GetComponent<Text>();
      RemasterTheme.Place(fixedPackage.rectTransform,.71f,.12f,.98f,.88f);
      fixedPackage.fontSize = 13; fixedPackage.resizeTextForBestFit = true; fixedPackage.resizeTextMinSize = 11; fixedPackage.resizeTextMaxSize = 13;
      RemasterTheme.Place((RectTransform)item.Find("ToggleEnable"),.93f,.25f,.985f,.75f);
      RemasterTheme.Place((RectTransform)item.Find("ButtonUnload"),.71f,.08f,.91f,.46f);
      RemasterTheme.Place((RectTransform)item.Find("ButtonSettings"),.71f,.54f,.91f,.92f);
    }

    public static void Details(PackageManagerContentUIControl control, string id = null)
    {
      RemasterTheme.Place(control.TextPackageName.rectTransform,.21f,.68f,.98f,.98f);
      control.TextPackageName.resizeTextForBestFit = true;
      control.TextPackageName.resizeTextMinSize = 14; control.TextPackageName.resizeTextMaxSize = 22;
      RemasterTheme.Place(control.TextPackageVersion.rectTransform,.21f,.48f,.98f,.67f);
      RemasterTheme.Place(control.TextPackageMoreInfos.rectTransform,.21f,.22f,.98f,.48f);
      control.TextPackageMoreInfos.resizeTextForBestFit = true;
      control.TextPackageMoreInfos.resizeTextMinSize = 11; control.TextPackageMoreInfos.resizeTextMaxSize = 14;
      RemasterTheme.Place((RectTransform)control.LinkVisitDoc.transform,.21f,.02f,.46f,.2f);
      RemasterTheme.Place((RectTransform)control.LinkVisiAuthor.transform,.47f,.02f,.71f,.2f);
      RemasterTheme.Place((RectTransform)control.LinkVisitSite.transform,.72f,.02f,.98f,.2f);
      if (id != null) Icon(control.ImagePackageLogo,id);
      var scroll = control.ScrollView.GetComponent<ScrollRect>();
      if (scroll != null) { scroll.horizontal = false; if (scroll.horizontalScrollbar != null) scroll.horizontalScrollbar.gameObject.SetActive(false); }
    }
  }
}
