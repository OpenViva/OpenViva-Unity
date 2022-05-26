using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace viva.profiler
{
  public class PerformanceUI : MonoBehaviour
  {
    [SerializeField]
    private LayoutElement[] rainbow;
    [SerializeField]
    private Text fps;
    [SerializeField]
    private Text ms;
    [SerializeField]
    private Text mb;
    [SerializeField]
    private Text gc;
    public Canvas canvas;
    private float updateTime;

    private void Awake()
    {
      this.canvas = this.GetComponent<Canvas>();
    }
    private void ToggleUI(){
      if(GameDirector.player.controls == Player.ControlType.VR){ return; }
      canvas.enabled = !canvas.enabled;
    }
    private void Update()
    {
      if(Keyboard.current[Key.F8].wasPressedThisFrame){
        ToggleUI();
      }
      if( !canvas.enabled || updateTime-Time.realtimeSinceStartup > 0.0 )
        return;
      updateTime = Time.realtimeSinceStartup + 0.5f;
      if(Performance.FrameCountLastSecond < 30)
        fps.color = Color.red;
      else if (Performance.FrameCountLastSecond < 50)
        fps.color = Color.yellow;
      else
        fps.color = Color.white;
      fps.text = Performance.FrameCountLastSecond.ToString("0");
      ms.text = Performance.AvgFrameTimeLastSecond.ToString("0.00");
      mb.text = Performance.MemoryUsage.ToString("N0");
      gc.text = Performance.GarbageCollections.ToString("N0");
      this.UpdateRainbow();
    }

    private void UpdateRainbow()
    {
      for( int category = 0; category < 6; ++category )
        this.rainbow[category].flexibleWidth = Performance.GetFrameFraction((Performance.FrameRateCategory) category) * 1000f;
    }
  }
}
