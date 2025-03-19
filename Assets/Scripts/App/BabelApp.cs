using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Assertions;

public class BabelApp : MonoBehaviour {

  public int ImageWidth;
  public Material TargetMaterial;

  public BabelCodec Codec;
  public BabelImage Image;

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

  public bool IsAnimating => _isAnimating;
  public BigInteger AnimStart, AnimEnd;

  private byte[] _bitVector;
  private Texture2D _rawBitTexture;

  private bool _isAnimating;
  private BigInteger _prevKeyframe;
  private float _prevKeyframeTime;
  private List<BigInteger> _animFrames = new();

  public void LoadImage(Texture2D texture) {
    Assert.AreEqual(texture.width, ImageWidth);
    Assert.AreEqual(texture.height, ImageWidth);

    for (int x = 0; x < texture.width; x++) {
      for (int y = 0; y < texture.height; y++) {
        int index = Image.ImagePositionToBitPosition(x, y);
        if (texture.GetPixel(x, y).r > 0.5f) {
          _bitVector[index] = 1;
        } else {
          _bitVector[index] = 0;
        }
      }
    }

    _index = Codec.Encode(_bitVector);
    UpdateDataTexture();
  }

  public void AnimateToImage(Texture2D texture) {
    Assert.AreEqual(texture.width, ImageWidth);
    Assert.AreEqual(texture.height, ImageWidth);

    var encodedTexture = new byte[texture.width * texture.height];

    for (int x = 0; x < texture.width; x++) {
      for (int y = 0; y < texture.height; y++) {
        int index = Image.ImagePositionToBitPosition(x, y);
        if (texture.GetPixel(x, y).r > 0.5f) {
          encodedTexture[index] = 1;
        } else {
          encodedTexture[index] = 0;
        }
      }
    }

    AnimStart = Index;
    AnimEnd = Codec.Encode(encodedTexture);

    if (AnimStart == AnimEnd) {
      return;
    }

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

  private void OnEnable() {
    int bits = ImageWidth * ImageWidth;
    Codec = new BabelCodec(bits);
    Image = new BabelImage(bits);
    _bitVector = new byte[bits];

    _rawBitTexture = new Texture2D(ImageWidth, ImageWidth, TextureFormat.R8, mipChain: false, linear: false);
    _rawBitTexture.filterMode = FilterMode.Point;

    TargetMaterial.SetTexture("_Data", _rawBitTexture);
    TargetMaterial.SetTexture("_Lookup", Image.GenerateLookupTexture());
  }

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
  }

  public void UpdateDataTexture() {
    _rawBitTexture.SetPixelData(_bitVector, 0);
    _rawBitTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
  }
}
