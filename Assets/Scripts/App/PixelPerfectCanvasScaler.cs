using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PixelPerfectCanvasScaler : MonoBehaviour {

  public int2 BaseCanvasSize;
  public CanvasScaler Scaler;

  private void Update() {
    int xScale = Mathf.FloorToInt(Screen.width / BaseCanvasSize.x);
    int yScale = Mathf.FloorToInt(Screen.height / BaseCanvasSize.y);
    int scale = Mathf.Min(xScale, yScale);
    if (scale != Scaler.scaleFactor) {
      Scaler.scaleFactor = scale;
    }
  }
}
