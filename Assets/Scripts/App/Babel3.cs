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
  private int2[] _positions;
  private int[,] _positionToIndex;

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
  public Texture2D LookupTex;
  public Texture2D DataTex;

  private MaterialPropertyBlock _block;
  private float _prevPercent;
  private float _prevAdjustedPercent;
  private int _prevOffset;

  public BigInteger Index;
  public BigInteger MaxIndex;

  private BigInteger[] _factorial;

  private BabelCodec _codec;

  public void SetPercent(float percent) {
    Percent = percent;
  }

  private void OnEnable() {
    _block = new();

    _codec = new BabelCodec(ImageWidth * ImageWidth);

    _array = new byte[ImageWidth * ImageWidth];
    _positions = new int2[ImageWidth * ImageWidth];
    InitPositions(0, _array.Length, 0, 0, ImageWidth);

    _positionToIndex = new int[ImageWidth, ImageWidth];
    for (int i = 0; i < _positions.Length; i++) {
      var pos = _positions[i];
      _positionToIndex[pos.x, pos.y] = i;
    }

    MaxIndex = BigInteger.Pow(2, _array.Length);

    UpdateLookupTexture();

    _factorial = new BigInteger[ImageWidth * ImageWidth + 1];
    BigInteger factorial = 1;
    _factorial[0] = 1;
    for (int i = 1; i < _factorial.Length; i++) {
      factorial *= i;
      _factorial[i] = factorial;
    }

    _setFromIndexCache.Clear();
    _totalComboCache.Clear();
  }

  private bool _isAnimating;
  private BigInteger _prevKeyframe;
  private float _prevKeyframeTime;
  private List<BigInteger> _animFrames = new();

  public bool IsAnimating => _isAnimating;
  public BigInteger AnimStart, AnimEnd;

  public void SetFromAdjustedPercent(float adjustedPercent) {
    int imageSize = ImageWidth * ImageWidth;

    _isAnimating = false;
    _animFrames.Clear();

    int slots = imageSize + 1;
    int slot = Mathf.FloorToInt(adjustedPercent * slots);
    slot = Mathf.Min(slots - 1, slot);

    float inSlotT = Mathf.InverseLerp(slot, slot + 1, adjustedPercent * slots);

    BigInteger startIndex = 0;
    for (int i = 0; i < slot; i++) {
      startIndex += NPermuteK(imageSize, i);
    }

    BigInteger endIndex = startIndex + NPermuteK(imageSize, slot);

    BigInteger delta = (endIndex - startIndex);
    BigInteger percentDelta = delta * new BigInteger(Mathf.RoundToInt(inSlotT * 1000000)) / 1000000;

    Index = startIndex + percentDelta;

    SetFromIndex(Index);
    UpdateDataTexture();
  }

  private void Update() {
    IndexIt = 0;

    int imageSize = ImageWidth * ImageWidth;
    if (imageSize != _array.Length) {
      _array = new byte[imageSize];
      _positions = new int2[imageSize];
      InitPositions(0, _array.Length, 0, 0, ImageWidth);
      UpdateLookupTexture();
    }

    if (ClearCache) {
      ClearCache = false;
      _setFromIndexCache.Clear();
      _totalComboCache.Clear();
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
          int index = _positionToIndex[x, y];
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
    int imageSize = ImageWidth * ImageWidth;
    int slots = imageSize + 1;

    BigInteger index = 0;
    for (int i = 0; i < slots; i++) {
      BigInteger nextIndex = index + NPermuteK(imageSize, i);
      if (value < nextIndex) {
        float slotA = (i + 0f) / slots;
        float slotB = (i + 1f) / slots;
        float slotT = (long)((value - index) * 10000 / (nextIndex - index)) / 10000.0f;
        return Mathf.Lerp(slotA, slotB, slotT);
      }
      index = nextIndex;
    }

    return 1f;
  }

  public void UpdateLookupTexture() {
    if (LookupTex == null || LookupTex.width != ImageWidth) {
      if (LookupTex != null) {
        DestroyImmediate(LookupTex);
      }

      LookupTex = new Texture2D(ImageWidth, ImageWidth, TextureFormat.RGHalf, mipChain: false, linear: true);
      LookupTex.filterMode = FilterMode.Point;
    }

    var data = LookupTex.GetPixelData<half2>(mipLevel: 0);

    for (int i = 0; i < _positions.Length; i++) {
      int2 dstPos = _positions[i];
      int srcX = i % ImageWidth;
      int srcY = i / ImageWidth;
      float2 srcUv = new float2(srcX + 0.5f, srcY + 0.5f) / ImageWidth;
      data[dstPos.x + dstPos.y * ImageWidth] = (half2)srcUv;
    }

    LookupTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);

    TargetMaterial.SetTexture("_Lookup", LookupTex);
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

  public void InitPositions(int start, int end, int sign, int2 min, int2 max) {
    int length = end - start;
    if (length == 1) {
      _positions[start] = min;
      return;
    }

    int middleIndex = start + (end - start) / 2;
    int middleAxis = (min + (max - min) / 2)[sign];

    int2 leftMax = max;
    int2 rightMin = min;

    leftMax[sign] = middleAxis;
    rightMin[sign] = middleAxis;

    InitPositions(start, middleIndex, 1 - sign, min, leftMax);
    InitPositions(middleIndex, end, 1 - sign, rightMin, max);
  }

  public BigInteger GetIndexFromPercent(float percent) {
    var maxPossibilities = BigInteger.Pow(2, _array.Length);
    var index = (maxPossibilities * new BigInteger(Mathf.RoundToInt(percent * 1000000))) / 1000000;

    if (index == maxPossibilities) {
      index = maxPossibilities - 1;
    }

    return index;
  }

  void SetFromIndex(BigInteger index) {
    _codec.Decode(index, _array);
  }

  BigInteger CalculateIndex() {
    return _codec.Encode(_array);
  }

  public Dictionary<NKKey, BigInteger> NPermuteKCache = new();
  public BigInteger NPermuteK(int n, int k) {
    var key = new NKKey() {
      N = n,
      K = k
    };

    if (NPermuteKCache.TryGetValue(key, out var early)) {
      return early;
    }

    var result = _factorial[n] / _factorial[k] / _factorial[n - k];

    NPermuteKCache[key] = result;

    return result;
  }

  public struct NKKey : IEquatable<NKKey> {

    public int N, K;

    public override int GetHashCode() {
      return N + K;
    }

    public bool Equals(NKKey other) {
      return N == other.N && K == other.K;
    }
  }
}
