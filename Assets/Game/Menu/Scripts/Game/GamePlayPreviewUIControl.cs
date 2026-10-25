using Ballance2.Base;
using Ballance2.Game;
using Ballance2.Game.GamePlay;
using Ballance2.Game.LevelBuilder;
using RuntimeSceneGizmo;
using Ballance2.Services.I18N;
using UnityEngine.UI;

namespace Ballance2.Menu
{
  /// <summary>
  /// Main gameplay menu controller.
  /// </summary>
  public class GamePlayPreviewUIControl : GameSingletonBehavior<GamePlayPreviewUIControl> 
  {
    public Text _TextLevelInfo;
    public Text _TextCameraSpeed;
    public Toggle _CheckBoxSkyBox;
    public Toggle _CheckBoxWireFrame;
    public Toggle _CheckBoxAudio;
    public Toggle _CheckBoxFog;
    public Toggle _CheckBoxDethTethCubes;
    public Toggle _CheckBoxSkylayer;
    public SceneGizmoRenderer _GizmoRenderer;

    private FreeCamera _FreeCamera;

    private void Start() {
      _CheckBoxSkyBox.onValueChanged.AddListener((check) => 
        _FreeCamera.SkyBox = check
      );
      _CheckBoxWireFrame.onValueChanged.AddListener((check) =>  
        _FreeCamera.Wireframe = check
      );
      _CheckBoxFog.onValueChanged.AddListener((check) =>  
        _FreeCamera.Fog = check
      );
      _CheckBoxAudio.onValueChanged.AddListener((check) =>  
        _FreeCamera.Audio = check
      );
      _CheckBoxDethTethCubes.onValueChanged.AddListener((check) =>  
        GamePreviewManager.Instance.ToggleDethTethCubesVisible(check)
      );
      _CheckBoxSkylayer.onValueChanged.AddListener((check) =>  
        GamePreviewManager.Instance.ToggleSkylayerVisible(check)
      );

    }

    /// <summary>
    /// Set level preview information.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="author"></param>
    /// <param name="version"></param>
    public void SetLevelInfo(string name, string author, string version) {
      var json = LevelBuilder.Instance.CurrentLevelJson;
      var level = json.level;
      _TextLevelInfo.text = I18N.TrF("core.ui.PreviewInfo", "{0}",
         LevelDisplayName.Get(name), author, version,
         I18N.Tr("core.ball." + level.firstBall, level.firstBall),
         level.levelScore, level.startPoint, level.startLife, level.sectorCount,
         level.lightColor, level.skyBox, level.musicTheme);
    }
    /// <summary>
    /// Set the FreeCamera reference.
    /// </summary>
    /// <param name="freeCam"></param>
    public void SetFreeCamera(FreeCamera freeCam) {
      _FreeCamera = freeCam;
      _GizmoRenderer.ReferenceTransform = freeCam.transform;
      freeCam.onCamSpeedChanged = () => {
        _TextCameraSpeed.text = I18N.TrF("core.ui.CameraSpeed", freeCam.cameraSpeed);
      };
      freeCam.onCamSpeedChanged();
    }
  }
}
