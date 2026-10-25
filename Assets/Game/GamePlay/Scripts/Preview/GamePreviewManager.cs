using Ballance2.Base;
using Ballance2.Game.Utils;
using Ballance2.Menu;
using Ballance2.Package;
using Ballance2.Services;
using BallancePhysics.Wapper;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace Ballance2.Game.GamePlay
{
  /// <summary>
  /// Control level preview behavior.
  /// </summary>
  public class GamePreviewManager : GameSingletonBehavior<GamePreviewManager>
  {
    private const string TAG = "GamePreviewManager";

    private bool _IsGamePlaying = false;
    private bool _IsLoaded = false;
    public bool IsPlaying => _IsGamePlaying;

    public Camera GamePreviewCamera;
    public Camera GamePreviewMinimapCamera;
    public FreeCamera GamePreviewFreeCamera;
    public Skybox GamePreviewCameraSkyBox;
    public GameObject[] GameDepthTestCubes;
    public MusicManager MusicManager;
    public CamManager CamManager;
    public SectorManager SectorManager;

    private int escKeyId = 0;
    private GameMediator Mediator;
    private GameUIManager UIManager;
    private GameSoundManager SoundManager;
    private GamePlayPreviewUIControl GamePreviewUI;

    private void Awake() {
      Mediator = GameMediator.Instance;
      UIManager = GameUIManager.Instance;
      SoundManager = GameSoundManager.Instance;
      var SystemPackage = GamePackage.GetSystemPackage();

      // Register global events.
      Mediator.SubscribeSingleEvent(SystemPackage, "CoreGamePreviewManagerInitAndStart", "GamePreviewManager", (evtName, param) => {
        GamePreviewUI = GamePlayPreviewUIControl.Instance;
        SoundManager.PlayFastVoice("core.sounds:Misc_StartLevel.wav", GameSoundType.Normal); // Play the level-start sound.
        GamePreviewUI.gameObject.SetActive(true);
        GamePreviewUI.SetLevelInfo(param[0] as string, param[1] as string, param[2] as string);
        GamePreviewUI.SetFreeCamera(GamePreviewFreeCamera);
        CamManager.gameObject.SetActive(false);
        CamManager._CamTarget.gameObject.SetActive(false);
        MusicManager.EnableBackgroundMusic();
        GamePreviewMinimapCamera.gameObject.SetActive(true);
        GamePreviewCamera.gameObject.SetActive(true);
        SectorManager.ActiveAllModulsForPreview(); // Activate mechanisms.

        UIManager.CloseAllPage();
        UIManager.MaskBlackFadeOut(1);
        _IsLoaded = true;
        _IsGamePlaying = true;

        // Escape key.
        escKeyId = UIManager.ListenKey(KeyCode.Escape, (key, down) => {
          if (down) {
            if (_IsGamePlaying)
              PauseLevel();
            else
              ResumeLevel();
          }
        },GamepadButton.Select,GamepadButton.Start);
        return false;
      });
    }
    protected override void OnDestroy() {
      if (escKeyId > 0) {
        GameUIManager.Instance.DeleteKeyListen(escKeyId);
        escKeyId = 0;
      }
      // Unregister global events.
      Mediator.UnRegisterSingleEvent("CoreGamePreviewManagerInitAndStart");
    }

    public void PauseLevel() {
      SoundManager.PlayFastVoice("core.sounds:Menu_click.wav", GameSoundType.UI);
      UIManager.GoPage("PageGamePreviewPause") ;
      _IsGamePlaying = false;
    }
    public void QuitLevel() {
      if (_IsLoaded) {
        _IsLoaded = false;
        SoundManager.PlayFastVoice("core.sounds:Menu_load.wav", GameSoundType.Normal);
        UIManager.MaskBlackFadeIn(1);
        GameTimer.Delay(1, () => {
          GamePreviewUI.gameObject.SetActive(false);
          MusicManager.DisableBackgroundMusic();
          CamManager.gameObject.SetActive(true);
          LevelBuilder.LevelBuilder.Instance.UnLoadLevel(null);
          GamePreviewMinimapCamera.gameObject.SetActive(false);
          GamePreviewCamera.gameObject.SetActive(false);
        });
      }
    }
    public void ResumeLevel() {
      _IsGamePlaying = true;
      SoundManager.PlayFastVoice("core.sounds:Menu_click.wav", GameSoundType.UI);
      UIManager.CloseAllPage();
    }

    /// <summary>
    /// Initialize lighting and skybox.
    /// </summary>
    /// <param name="skyBoxPre">A-K, or empty to use customSkyMat.</param>
    /// <param name="customSkyMat">Custom skybox material.</param>
    /// <param name="lightColor">Light color.</param>
    public void CreateSkyAndLight(string skyBoxPre, Material customSkyMat, Color lightColor) {
      GamePreviewCameraSkyBox.material = customSkyMat == null ? SkyBoxUtils.MakeSkyBox(skyBoxPre) : customSkyMat;
      Ballance2.Services.GameManager.GameLight.color = lightColor;
    }
    /// <summary>
    /// Hide the skybox and level lighting.
    /// </summary>
    public void HideSkyAndLight() {
      GamePreviewCameraSkyBox.material = null;
    }

    public void ToggleDethTethCubesVisible(bool visible) {
      foreach (var value in GameDepthTestCubes)
        value.SetActive(visible);
    }
    public void ToggleSkylayerVisible(bool visible) {
      if (LevelBuilder.LevelBuilder.Instance.CurrentLevelSkyLayer)
        LevelBuilder.LevelBuilder.Instance.CurrentLevelSkyLayer.SetActive(visible);
    }
  }
}
