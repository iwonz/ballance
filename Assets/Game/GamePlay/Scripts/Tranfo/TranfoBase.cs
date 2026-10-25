
using UnityEngine;

namespace Ballance2.Game.GamePlay.Tranfo
{
  /// <summary>
  /// Ball transformer interface.
  /// </summary>
  public interface ITranfoBase
  {
    /// <summary>
    /// Get the transformer's target ball type.
    /// </summary>
    /// <returns></returns>
    string GetTargetBallType();
    /// <summary>
    /// Get the transformer animation controller.
    /// </summary>
    /// <returns></returns>
    ITranfoAminControl GetTranfoAminControl();
    /// <summary>
    /// Get the transformer color.
    /// </summary>
    /// <returns></returns>
    Color GetTranfoColor();
    /// <summary>
    /// Get the Transform.
    /// </summary>
    Transform GetTransform();
    /// <summary>
    /// Reset callback after transformation finishes.
    /// </summary>
    void ResetTranfo();
  }

  /// <summary>
  /// Base transformer.
  /// </summary>
  public class TranfoBase : ModulBase, ITranfoBase
  {
    public TiggerTester _Tigger;
    public Color _Color;
    public string _TargetBallType;

    public string GetTargetBallType() { return _TargetBallType; }
    public ITranfoAminControl GetTranfoAminControl() { return TranfoAminControl.Instance; }
    public Color GetTranfoColor() { return _Color; }
    public Transform GetTransform() { return transform; }

    public TranfoBase() {
      this.AutoActiveBaseGameObject = true;
    }

    protected override void Start()
    {
      base.Start();

      _Tigger.onTriggerEnter = (_, other) => {
        // A ball whose type differs from the target type.
        if (!_TranfoActived && other.tag == "Ball" && other.name != _TargetBallType)
        {
          // Trigger transformation.
          _TranfoActived = true;
          GamePlayManager.Instance.ActiveTranfo(this, _TargetBallType);
        }
      };
    }

    public void ResetTranfo() {
      this.Reset(ModulBaseResetType.SectorRestart);
    }

    protected bool _TranfoActived = false;

    public override void ActiveForPreview()
    {
      base.ActiveForPreview();
      gameObject.SetActive(true);
    }
    public override void DeactiveForPreview()
    {
      base.DeactiveForPreview();
      gameObject.SetActive(false);
    }
    public override void Reset(ModulBaseResetType type)
    {
      base.Reset(type);
      _TranfoActived = false;
    }
  }
}