using System.Numerics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BabelScroller : MonoBehaviour, IPointerDownHandler, IDragHandler {

  public BabelApp Babel;
  public RectTransform Cursor;
  public RectTransform AnimPreview;
  public float CursorMin, CursorMax;
  public float RangeMin, RangeMax;

  private BigInteger _currIndex;
  private BigInteger _prevAnimEnd;
  private float _posAnimEnd;

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

    Babel.SetFromAdjustedPercent(Mathf.Clamp01(CalcPercent(eventData)));
  }

  public void OnPointerDown(PointerEventData eventData) {
    Babel.SetFromAdjustedPercent(Mathf.Clamp01(CalcPercent(eventData)));
  }

  void Update() {
    if (_currIndex != Babel.Index) {
      float factor = Babel.CalculateAdjustedPercent(Babel.Index);
      Cursor.localPosition = new UnityEngine.Vector3(Mathf.Lerp(CursorMin, CursorMax, factor), 0, 0);

      _currIndex = Babel.Index;
    }

    if (Babel.IsAnimating) {
      AnimPreview.gameObject.SetActive(true);
      if (_prevAnimEnd != Babel.AnimEnd) {
        _posAnimEnd = Babel.CalculateAdjustedPercent(Babel.AnimEnd);
        _prevAnimEnd = Babel.AnimEnd;
      }

      float p0 = Babel.CalculateAdjustedPercent(Babel.Index);
      float p1 = _posAnimEnd;

      float min = Mathf.Lerp(RangeMin, RangeMax, Mathf.Min(p0, p1));
      float max = Mathf.Lerp(RangeMin, RangeMax, Mathf.Max(p0, p1));

      AnimPreview.localPosition = new UnityEngine.Vector3(min, 0, 0);
      AnimPreview.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, max - min);
    } else {
      AnimPreview.gameObject.SetActive(false);
      _prevAnimEnd = default;
    }
  }

}
