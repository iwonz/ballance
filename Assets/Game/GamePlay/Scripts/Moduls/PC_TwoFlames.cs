using UnityEngine;

namespace Ballance2.Game.GamePlay.Moduls
{
  /// <summary>
  /// Checkpoint flame component.
  /// </summary>
  public class PC_TwoFlames : ModulBase 
  {
    public GameObject FlameSmallLeft;
    public GameObject FlameSmallRight;
    public GameObject Flame;
    public TiggerTester CheckPointTigger;
    public bool CheckPointActived = false;

    protected override void Start()
    {
      base.Start();
      this.FlameSmallLeft.SetActive(false);
      this.FlameSmallRight.SetActive(false);
      this.Flame.SetActive(false);

      if (!IsPreviewMode) 
      {
        // Trigger-enter event.
        CheckPointTigger.onTriggerEnter = (s, otherBody) => {
          if (!CheckPointActived && otherBody.tag == "Ball")
          {
            // Advance to the next sector.
            this.CheckPointActived = true;
            this.Flame.SetActive(false);
            GamePlayManager.Instance.SectorManager.NextSector();
          }
        };
      }
    }
    public override void Active()
    {
      base.Active();
      this.CheckPointActived = true;
      this.FlameSmallLeft.SetActive(true);
      this.FlameSmallRight.SetActive(true);
    }
    public override void Deactive()
    {
      base.Deactive();
      this.FlameSmallLeft.SetActive(false);
      this.FlameSmallRight.SetActive(false);
      this.Flame.SetActive(false);
    }

    public override void ActiveForPreview()
    {
      this.FlameSmallLeft.SetActive(true);
      this.FlameSmallRight.SetActive(true);
      this.gameObject.SetActive(true);
    }
    public override void DeactiveForPreview()
    {
      this.FlameSmallLeft.SetActive(false);
      this.FlameSmallRight.SetActive(false);
      this.gameObject.SetActive(false);
    }

    // Set flame activation state.
    public void InternalActive()
    {
      this.FlameSmallLeft.SetActive(false);
      this.FlameSmallRight.SetActive(false);
      this.Flame.SetActive(true);
      this.CheckPointActived = false;
    }
  }
}