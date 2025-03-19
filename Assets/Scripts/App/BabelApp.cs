using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class BabelApp : MonoBehaviour {

  public int ImageWidth;

  public Texture2D ToLoad;
  public bool LoadImage;

  [Header("Rendering")]
  public Material TargetMaterial;
  public Texture2D DataTex;

  private BigInteger _index;
  public BigInteger Index {
    get => _index;
    set {
      value = BigInteger.Max(0, value);
      value = BigInteger.Min(Codec.MaxIndex, value);

      _isAnimating = false;
      _animFrames.Clear();

      if (value == _index) {
        return;
      }

      _index = value;
      Codec.Decode(_index, _bitVector);
      UpdateDataTexture();
    }
  }

  public BabelCodec Codec;
  public BabelImage Image;

  private byte[] _bitVector;

  private void OnEnable() {
    int bits = ImageWidth * ImageWidth;
    Codec = new BabelCodec(bits);
    Image = new BabelImage(bits);
    _bitVector = new byte[bits];

    TargetMaterial.SetTexture("_Lookup", Image.GenerateLookupTexture());
  }

  private bool _isAnimating;
  private BigInteger _prevKeyframe;
  private float _prevKeyframeTime;
  private List<BigInteger> _animFrames = new();

  public bool IsAnimating => _isAnimating;
  public BigInteger AnimStart, AnimEnd;

  private void Update() {
    if (_isAnimating) {
      var nextKeyframe = _animFrames[_animFrames.Count - 1];

      float keyframeT = Mathf.InverseLerp(_prevKeyframeTime, _prevKeyframeTime + 0.1f, Time.time);

      _index = _prevKeyframe + (nextKeyframe - _prevKeyframe) * new BigInteger((int)(keyframeT * 10000)) / 10000;
      Codec.Decode(_index, _bitVector);
      UpdateDataTexture();

      if (keyframeT >= 1f) {
        _prevKeyframe = nextKeyframe;
        _prevKeyframeTime = Time.time;
        _animFrames.RemoveAt(_animFrames.Count - 1);
      }

      if (_animFrames.Count == 0) {
        _isAnimating = false;
      }
    }

    if (LoadImage) {
      LoadImage = false;

      var copy = (byte[])_bitVector.Clone();

      for (int x = 0; x < ToLoad.width; x++) {
        for (int y = 0; y < ToLoad.height; y++) {
          int index = Image.ImagePositionToBitPosition(x, y);
          if (ToLoad.GetPixel(x, y).r > 0.5f) {
            _bitVector[index] = 1;
          } else {
            _bitVector[index] = 0;
          }
        }
      }

      AnimStart = Index;
      AnimEnd = Codec.Encode(_bitVector);

      if (AnimEnd != AnimStart) {
        _animFrames.Clear();

        var firstPart = new List<BigInteger>();
        var secondPart = new List<BigInteger>();

        BigInteger animFrame;
        bool currSign;
        BigInteger step;
        BigInteger stepStep = 300;
        BigInteger stepStepStep = 257;
        BigInteger stepStepStepStep = 256;

        BigInteger halfway = (AnimStart + AnimEnd) / 2;

        animFrame = AnimStart;
        currSign = animFrame > halfway;
        step = (AnimEnd > AnimStart) ? 256 : -256;

        while (animFrame > halfway == currSign) {
          firstPart.Add(animFrame);
          secondPart.Add(AnimEnd - (animFrame - AnimStart));

          animFrame += step / 256;
          step = (step * stepStep / 256);
          stepStep = (stepStep * stepStepStep / 256);
          stepStepStep = (stepStepStep * stepStepStepStep / 256);
          stepStepStepStep = stepStepStepStep * 269 / 256;
        }

        firstPart.Reverse();

        _animFrames.Clear();
        _animFrames.AddRange(secondPart);
        _animFrames.AddRange(firstPart);

        _isAnimating = true;

        _prevKeyframe = Index;
        _prevKeyframeTime = Time.time;
      }

      copy.CopyTo(_bitVector, 0);
    }
  }

  public void UpdateDataTexture() {
    if (DataTex == null || DataTex.width != ImageWidth) {
      if (DataTex != null) {
        DestroyImmediate(DataTex);
      }

      DataTex = new Texture2D(ImageWidth, ImageWidth, TextureFormat.R8, mipChain: false, linear: true);
      DataTex.filterMode = FilterMode.Point;
    }

    DataTex.SetPixelData(_bitVector, 0);
    DataTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);

    TargetMaterial.SetTexture("_Data", DataTex);
  }
}
