using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;

[ExecuteAlways]
public class Babel3 : MonoBehaviour {

  private byte[] _array = new byte[16];
  private int2[] _positions;

  public int ImageWidth;

  [Range(0, 1)]
  public float Percent;

  public int TotalSetPixels;
  public int Index = 0;

  public bool Validate;

  private void OnEnable() {
    _array = new byte[ImageWidth * ImageWidth];
    _positions = new int2[ImageWidth * ImageWidth];
    InitPositions(0, _array.Length, 0, 0, ImageWidth);
  }

  private void Update() {
    int imageSize = ImageWidth * ImageWidth;
    if (imageSize != _array.Length) {
      _array = new byte[imageSize];
      _positions = new int2[imageSize];
      InitPositions(0, _array.Length, 0, 0, ImageWidth);
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
  }

  private void OnDrawGizmos() {
    //SetFromPercent(Percent);
    SetFromIndex(Index);

    for (int i = 0; i < _array.Length; i++) {
      int2 pos = _positions[i];
      //int2 pos = new int2(i, 0);

      Gizmos.color = _array[i] == 0 ? Color.black : Color.white;
      Gizmos.DrawCube(new UnityEngine.Vector3(pos.x, pos.y, 0), UnityEngine.Vector3.one);
    }
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

  public void SetFromPercent(float percent) {
    var maxPossibilities = BigInteger.Pow(2, _array.Length);
    var index = (maxPossibilities * new BigInteger(Mathf.RoundToInt(percent * 1000000))) / 1000000;

    if (index == maxPossibilities) {
      index = maxPossibilities - 1;
    }

    SetFromIndex(index);
  }

  void SetFromIndex(BigInteger index) {
    int totalPixels = 0;

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

    (BigInteger leftIndex, BigInteger rightIndex) = EvalCurve(leftCombinations, rightCombinations, index);

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

    BigInteger left, right;

    if (columnIndex == columnCount) {
      left = columnIndex * 2 + (index - columnStart);
      right = (index - columnStart);
    } else {
      left = columnIndex * 2 + (index - columnStart + 1) / 2;
      right = (index - columnStart) / 2;
    }

    left = left % width;

    return (left, right);
  }

  public Dictionary<(int, int), BigInteger> NPermuteKCache = new();
  public BigInteger NPermuteK(int n, int k) {
    if (NPermuteKCache.TryGetValue((n, k), out var early)) {
      return early;
    }

    var q = n - k;

    //Make sure k is largest
    if (q < k) {
      var tmp = q;
      q = k;
      k = tmp;
    }

    var result = new BigInteger(1);

    for (int i = n; i > k; i--) {
      result *= i;
    }

    BigInteger divisor = new BigInteger(1);
    for (int i = 2; i <= q; i++) {
      divisor *= i;
    }

    result /= divisor;

    NPermuteKCache[(n, k)] = result;

    return result;
  }

}
