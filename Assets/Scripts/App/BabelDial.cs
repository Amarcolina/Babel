using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BabelDial : MonoBehaviour,
  IPointerEnterHandler,
  IPointerExitHandler,
  IPointerDownHandler,
  IPointerUpHandler,
  IDragHandler,
  IEndDragHandler {

  public Babel3 Babel;
  public Image Graphic;
  public Image Face;
  public Sprite[] FaceSprites;
  public Color NormalColor;
  public Color HoverColor;
  public Color PressColor;
  public float Sensitivity;
  public float Power;
  public float RotationSensitivity;

  private double _startOffset;
  private double _startAngle;
  private double _angle;

  private double _increment;

  void Start() {
    Graphic.color = NormalColor;
  }

  public void OnDrag(PointerEventData eventData) {
    Vector2 dragDelta = eventData.position - eventData.pressPosition;
    _increment = Math.Sign(dragDelta.x) * Math.Pow(Math.Abs(dragDelta.x), Power);

    _angle = _startAngle + _increment * RotationSensitivity;

    while (_angle < 0) {
      _angle += 360;
    }

    while (_angle > 360) {
      _angle -= 360;
    }

    double tmpAngle = _angle;
    Face.transform.localRotation = Quaternion.identity;
    while (tmpAngle >= 90) {
      Face.transform.Rotate(0, 0, 90);
      tmpAngle -= 90;
    }

    Face.sprite = FaceSprites[(int)Math.Floor(tmpAngle / 90 * FaceSprites.Length)];

    Babel.Offset = (int)Math.Round(_startOffset + _increment * Sensitivity);
  }

  public void OnPointerDown(PointerEventData eventData) {
    _startOffset = Babel.Offset;
    _startAngle = _angle;
    Graphic.color = PressColor;
  }

  public void OnPointerUp(PointerEventData eventData) {
    Graphic.color = HoverColor;
    _increment = 0;
  }

  public void OnPointerEnter(PointerEventData eventData) {
    if (!eventData.dragging) {
      Graphic.color = HoverColor;
    }
  }

  public void OnPointerExit(PointerEventData eventData) {
    if (!eventData.dragging) {
      Graphic.color = NormalColor;
    }
  }

  public void OnEndDrag(PointerEventData eventData) {
    Graphic.color = NormalColor;
  }
}
