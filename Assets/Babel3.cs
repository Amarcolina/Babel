using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

[ExecuteAlways]
public class Babel3 : MonoBehaviour {

  private byte[] _array = new byte[16];
  private int2[] _positions;

  public int ImageWidth;

  [Range(0, 1)]
  public float AdjustedPercent;

  [Range(0, 1)]
  public float Percent;

  public int TotalSetPixels;
  public int Offset = 0;

  public bool Validate;

  [Header("Rendering")]
  public Renderer Renderer;
  public Texture2D LookupTex;
  public Texture2D DataTex;

  private MaterialPropertyBlock _block;
  private float _prevPercent;
  private float _prevAdjustedPercent;
  private int _prevOffset;
  private BigInteger _index;

  private BigInteger[] _factorial;

  private void OnEnable() {
    _block = new();

    _array = new byte[ImageWidth * ImageWidth];
    _positions = new int2[ImageWidth * ImageWidth];
    InitPositions(0, _array.Length, 0, 0, ImageWidth);
    UpdateLookupTexture();

    _factorial = new BigInteger[ImageWidth * ImageWidth + 1];
    BigInteger factorial = 1;
    _factorial[0] = 1;
    for (int i = 1; i < _factorial.Length; i++) {
      factorial *= i;
      _factorial[i] = factorial;
    }
  }

  private void Update() {
    int imageSize = ImageWidth * ImageWidth;
    if (imageSize != _array.Length) {
      _array = new byte[imageSize];
      _positions = new int2[imageSize];
      InitPositions(0, _array.Length, 0, 0, ImageWidth);
      UpdateLookupTexture();
    }

    if (Validate) {
      Validate = false;

      int possiblities = (int)BigInteger.Pow(2, imageSize);
      bool[] map = new bool[possiblities];
      for (BigInteger i = 0; i < BigInteger.Pow(2, imageSize); i++) {
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

    if (_prevAdjustedPercent != AdjustedPercent) {
      _prevAdjustedPercent = AdjustedPercent;

      int slots = imageSize + 1;
      int slot = Mathf.FloorToInt(AdjustedPercent * slots);

      float inSlotT = Mathf.InverseLerp(slot, slot + 1, AdjustedPercent * slots);

      BigInteger startIndex = 0;
      for (int i = 0; i < slot; i++) {
        startIndex += NPermuteK(imageSize, i);
      }

      BigInteger endIndex = startIndex + NPermuteK(imageSize, slot);

      BigInteger delta = (endIndex - startIndex);
      BigInteger percentDelta = delta * new BigInteger(Mathf.RoundToInt(inSlotT * 1000000)) / 1000000;

      _index = startIndex + percentDelta;

      SetFromIndex(_index);
      UpdateDataTexture();
    }

    if (_prevPercent != Percent) {
      _prevPercent = Percent;
      _index = GetIndexFromPercent(Percent);

      SetFromIndex(_index);
      UpdateDataTexture();
    }

    if (Offset != _prevOffset) {
      _index += (Offset - _prevOffset);
      _prevOffset = Offset;

      SetFromIndex(_index);
      UpdateDataTexture();
    }

    SetFromIndex(_index);
    UpdateDataTexture();
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

    Renderer.GetPropertyBlock(_block);
    _block.SetTexture("_Lookup", LookupTex);
    Renderer.SetPropertyBlock(_block);
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

    Renderer.GetPropertyBlock(_block);
    _block.SetTexture("_Data", DataTex);
    Renderer.SetPropertyBlock(_block);
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
    int totalPixels = 0;

    Profiler.BeginSample("Initial Index Calculation");
    while (true) {
      BigInteger possibilities = NPermuteK(_array.Length, totalPixels);

      if (index < possibilities) {
        break;
      }

      if (totalPixels >= _array.Length) {
        break;
      }

      index -= possibilities;
      totalPixels++;
    }
    Profiler.EndSample();

    SetFromIndex(0, _array.Length, totalPixels, index);
  }

  void SetFromIndex(int start, int end, int totalSetPixels, BigInteger index) {
    int length = end - start;

    if (length == 1) {
      _array[start] = (byte)totalSetPixels;
      return;
    }

    int subLength = length / 2;

    int minSubOccupied = Mathf.Max(0, totalSetPixels - subLength);
    int maxSubOccupied = totalSetPixels - minSubOccupied;

    int leftOccupied = minSubOccupied;
    int rightOccupied = maxSubOccupied;

    BigInteger leftCombinations;
    BigInteger rightCombinations;
    BigInteger totalCombinations;

    Profiler.BeginSample("Find Index");
    while (true) {
      leftCombinations = NPermuteK(subLength, leftOccupied);
      rightCombinations = NPermuteK(subLength, rightOccupied);

      totalCombinations = leftCombinations * rightCombinations;

      if (index < totalCombinations) {
        break;
      }

      index -= totalCombinations;

      leftOccupied++;
      rightOccupied--;

      if (rightOccupied < minSubOccupied) {
        throw new System.Exception();
      }
    }
    Profiler.EndSample();

    Profiler.BeginSample("Eval Curve");
    (BigInteger leftIndex, BigInteger rightIndex) = EvalCurve(leftCombinations, rightCombinations, index);
    Profiler.EndSample();

    int middle = start + (end - start) / 2;
    SetFromIndex(start, middle, leftOccupied, leftIndex);
    SetFromIndex(middle, end, rightOccupied, rightIndex);
  }

  public (BigInteger left, BigInteger right) EvalCurve(BigInteger width, BigInteger height, BigInteger index) {
    return DiagonalWrapCurve(width, height, index);
  }

  public (BigInteger left, BigInteger right) SnakeCurve(BigInteger width, BigInteger height, BigInteger index) {
    BigInteger leftIndex = index % width;
    BigInteger rightIndex = index / width;

    if (rightIndex % 2 == 1) {
      leftIndex = (width - 1) - leftIndex;
    }

    return (leftIndex, rightIndex);
  }

  public (BigInteger left, BigInteger right) DiagonalWrapCurve(BigInteger width, BigInteger height, BigInteger index) {
    BigInteger columnCount = width / 2;
    BigInteger columnSize = 2 * height;

    BigInteger columnIndex = index / columnSize;
    BigInteger columnStart = columnIndex * columnSize;

    BigInteger inColumnIndex = index - columnStart;

    if (columnIndex % 2 == 1) {
      inColumnIndex = (columnSize - 1) - inColumnIndex;
    }

    BigInteger left, right;

    if (columnIndex == columnCount) {
      left = columnIndex * 2 + inColumnIndex;
      right = inColumnIndex;
    } else {
      left = columnIndex * 2 + (inColumnIndex + 1) / 2;
      right = inColumnIndex / 2;
    }

    left = left % width;

    return (left, right);
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
