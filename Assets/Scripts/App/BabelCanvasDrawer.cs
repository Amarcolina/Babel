using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BabelCanvasDrawer : MonoBehaviour, IDragHandler, IPointerDownHandler {

  public BabelApp Babel;

  public void OnDrag(PointerEventData eventData) {
    PaintAt(eventData);
  }

  public void OnPointerDown(PointerEventData eventData) {
    if (eventData.pointerPressRaycast.gameObject == gameObject) {
      PaintAt(eventData);
    }
  }

  private void PaintAt(PointerEventData eventData) {
    Vector2 delta = (eventData.position - (Vector2)transform.position) /
                    GetComponentInParent<CanvasScaler>().scaleFactor;

    int x = Mathf.Clamp(Mathf.FloorToInt(delta.x), 0, 63);
    int y = Mathf.Clamp(Mathf.FloorToInt(delta.y), 0, 63);

    switch (eventData.button) {
      case PointerEventData.InputButton.Left:
        Babel.SetPixelAt(x, y, 1);
        break;
      case PointerEventData.InputButton.Right:
        Babel.SetPixelAt(x, y, 0);
        break;
    }
  }
}
