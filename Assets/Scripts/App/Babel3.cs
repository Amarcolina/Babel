using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

//[ExecuteAlways]
public class Babel3 : MonoBehaviour {

  private byte[] _array = new byte[16];

  public int ImageWidth;

  [Range(0, 1)]
  public float Percent;

  public int TotalSetPixels;
  public int Offset = 0;
  public int OffsetScale;

  public bool Validate;
  public bool ValidateIndex;

  public Texture2D ToLoad;
  public bool LoadImage;
  public bool LoadAnimate;
  public float AnimSigArg;
  public float AnimTime;
  public bool ClearCache;

  [Header("Rendering")]
  public Material TargetMaterial;
  public Texture2D DataTex;

  private float _prevPercent;
  private int _prevOffset;

  public BigInteger Index;
  public BigInteger MaxIndex;

  private BabelCodec _codec;
  private BabelImage _image;

  public void SetPercent(float percent) {
    Percent = percent;
  }

  private void OnEnable() {
    _codec = new BabelCodec(ImageWidth * ImageWidth);
    _image = new BabelImage(ImageWidth * ImageWidth);

    _array = new byte[ImageWidth * ImageWidth];

    MaxIndex = BigInteger.Pow(2, _array.Length);

    TargetMaterial.SetTexture("_Lookup", _image.GenerateLookupTexture());
  }

  private bool _isAnimating;
  private BigInteger _prevKeyframe;
  private float _prevKeyframeTime;
  private List<BigInteger> _animFrames = new();

  public bool IsAnimating => _isAnimating;
  public BigInteger AnimStart, AnimEnd;

  public void SetFromAdjustedPercent(float adjustedPercent) {
    _isAnimating = false;
    _animFrames.Clear();

    Index = _codec.CalculateNormalizedIndexFromPercent(adjustedPercent);
    SetFromIndex(Index);
    UpdateDataTexture();
  }

  private void Update() {
    int imageSize = ImageWidth * ImageWidth;
    if (imageSize != _array.Length) {
      _array = new byte[imageSize];
    }

    if (_isAnimating) {
      var nextKeyframe = _animFrames[_animFrames.Count - 1];

      float keyframeT = Mathf.InverseLerp(_prevKeyframeTime, _prevKeyframeTime + 0.1f, Time.time);

      Index = _prevKeyframe + (nextKeyframe - _prevKeyframe) * new BigInteger((int)(keyframeT * 10000)) / 10000;

      if (keyframeT >= 1f) {
        _prevKeyframe = nextKeyframe;
        _prevKeyframeTime = Time.time;
        _animFrames.RemoveAt(_animFrames.Count - 1);
      }

      SetFromIndex(Index);
      UpdateDataTexture();

      if (_animFrames.Count == 0) {
        _isAnimating = false;
      }
    }

    if (LoadImage) {
      LoadImage = false;

      var copy = (byte[])_array.Clone();

      for (int x = 0; x < ToLoad.width; x++) {
        for (int y = 0; y < ToLoad.height; y++) {
          int index = _image.ImagePositionToBitPosition(x, y);
          if (ToLoad.GetPixel(x, y).r > 0.5f) {
            _array[index] = 1;
          } else {
            _array[index] = 0;
          }
        }
      }

      AnimStart = Index;
      AnimEnd = CalculateIndex();

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

      copy.CopyTo(_array, 0);
    }

    if (Validate) {
      Validate = false;

      int possiblities = (int)BigInteger.Pow(2, imageSize);
      bool[] map = new bool[possiblities];

      var bound = BigInteger.Pow(2, imageSize);

      for (BigInteger i = 0; i < bound; i++) {
        SetFromIndex(i);

        ulong possibility = 0;
        for (int j = 0; j < imageSize; j++) {
          possibility = (possibility << 1) | _array[j];
        }

        if (map[possibility]) {
          Debug.LogError("Found duplicate " + possibility + " at index " + i);
          break;
        }

        map[possibility] = true;
      }

      Debug.Log("No duplicates found!");
    }

    if (ValidateIndex) {
      ValidateIndex = false;

      var bound = BigInteger.Pow(2, imageSize);
      for (BigInteger i = 0; i < bound; i++) {
        SetFromIndex(i);

        var index = CalculateIndex();

        if (index != i) {
          Debug.LogError("Indices were not the same! " + i + " : " + index);
          break;
        }
      }

      Debug.Log("all indices validated");
    }

    if (_prevPercent != Percent) {
      _prevPercent = Percent;
      Index = GetIndexFromPercent(Percent);

      SetFromIndex(Index);
      UpdateDataTexture();
    }

    if (Offset != _prevOffset) {
      Index += (Offset - _prevOffset) * BigInteger.Pow(2, OffsetScale);
      Index = BigInteger.Max(0, Index);
      Index = BigInteger.Min(MaxIndex, Index);
      _prevOffset = Offset;

      SetFromIndex(Index);
      UpdateDataTexture();
    }
  }

  public float CalculateAdjustedPercent(BigInteger value) {
    return _codec.CalculateNormalizedPercent(value);
  }

  public void UpdateDataTexture() {
    if (DataTex == null || DataTex.width != ImageWidth) {
      if (DataTex != null) {
        DestroyImmediate(DataTex);
      }

      DataTex = new Texture2D(ImageWidth, ImageWidth, TextureFormat.R8, mipChain: false, linear: true);
      DataTex.filterMode = FilterMode.Point;
    }

    DataTex.SetPixelData(_array, 0);
    DataTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);

    TargetMaterial.SetTexture("_Data", DataTex);
  }

  public BigInteger GetIndexFromPercent(float percent) {
    return _codec.CalculateIndexFromPercent(percent);
  }

  void SetFromIndex(BigInteger index) {
    _codec.Decode(index, _array);
  }

  BigInteger CalculateIndex() {
    return _codec.Encode(_array);
  }
}
