using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BabelScroller : MonoBehaviour, IPointerDownHandler, IDragHandler {

  public Babel3 Babel;
  public RectTransform Cursor;
  public float CursorMin, CursorMax;

  public float CalcPercent(PointerEventData eventData) {
    var scaler = GetComponentInParent<CanvasScaler>();
    float delta = eventData.position.x - transform.position.x;
    float p = delta / GetComponent<RectTransform>().rect.width;
    return p / scaler.scaleFactor;
  }

  public void OnDrag(PointerEventData eventData) {
    if (eventData.pointerPressRaycast.gameObject != gameObject) {
      return;
    }

    Babel.AdjustedPercent = Mathf.Clamp01(CalcPercent(eventData));
  }

  public void OnPointerDown(PointerEventData eventData) {
    Babel.AdjustedPercent = Mathf.Clamp01(CalcPercent(eventData));
  }

  void Update() {
    float factor = (int)(Babel.Index * 10000 / Babel.MaxIndex) / 10000.0f;
    Cursor.localPosition = new Vector3(Mathf.Lerp(CursorMin, CursorMax, factor), 0, 0);
  }

}
