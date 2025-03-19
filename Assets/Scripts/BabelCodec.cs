using System;
using System.Numerics;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The BabelCodec defines a sequence of bit-vectors that has the following properties:
///  - The first big-vector in the sequence is a vector of all zeros
///  - The last bit-vector in the sequence is a vector of all ones
///  - Every possible combination of bit-vectors appears exactly once in the sequence
///  - Vectors with fewer set bits are ordered before vectors with more set bits
///  
///  - Adjacent vectors usually differ by exactly one bit
///  - The differing bit between two adjacent vectors is roughly uniformly distributed
///  - Vectors within a neighborhood are perceptually "similar"
/// 
/// The BabelCodec class provides useful methods for exploring the codec and finding bit vectors
///  - Decode returns the bit-vector at the given index in the sequence
///  - Encode returns the index in the sequence of a given big-vector
/// </summary>
public class BabelCodec {

  public readonly int Bits;
  public readonly BigInteger MaxIndex;

  private readonly BigInteger[] _factorialLookup;
  private readonly BigInteger[] _prefixCombinations;

  private Dictionary<(int, int), BigInteger> _npkCache = new();
  private Dictionary<(int, int), byte> _leafCache = new();
  private Dictionary<(int, int, int), (BigInteger, BigInteger, BigInteger)> _prefixCache = new();

  public BabelCodec(int bits) {
    if (bits <= 0) {
      throw new ArgumentException($"Bit count must be positive and non-zero but was {bits}");
    }
    if (!Mathf.IsPowerOfTwo(bits)) {
      throw new ArgumentException($"Bit count must be a power of two but was {bits}");
    }

    Bits = bits;
    MaxIndex = BigInteger.Pow(2, bits) - 1;

    //Init factorial lookup
    {
      _factorialLookup = new BigInteger[bits + 1];
      BigInteger factorial = 1;
      _factorialLookup[0] = 1;
      for (int i = 1; i < _factorialLookup.Length; i++) {
        factorial *= i;
        _factorialLookup[i] = factorial;
      }
    }

    //Init prefix lookup
    {
      _prefixCombinations = new BigInteger[bits + 1];
      BigInteger totalCombinations = 0;
      for (int i = 0; i <= bits; i++) {
        totalCombinations += NPermuteK(bits, i);
        _prefixCombinations[i] = totalCombinations;
      }
    }
  }

  public void ClearCache() {
    _npkCache.Clear();
    _leafCache.Clear();
    _prefixCache.Clear();
  }

  /// <summary>
  /// Output the bit-set found at the given index in the babel sequence
  /// </summary>
  public void Decode(BigInteger index, byte[] bitSet) {
    int setCount = 0;

    while (true) {
      BigInteger possibilities = _prefixCombinations[setCount];

      if (index < possibilities) {
        break;
      }

      if (setCount >= Bits) {
        break;
      }

      setCount++;
    }

    BigInteger localIndex = index;
    localIndex -= _prefixCombinations[setCount];
    localIndex += NPermuteK(Bits, setCount);

    DecodeRecursive(0, Bits, setCount, localIndex, bitSet);
  }

  /// <summary>
  /// Calculate the index of the given bit-set in the babel sequence
  /// </summary>
  public BigInteger Encode(byte[] bitSet) {
    int setCount = 0;
    for (int i = 0; i < Bits; i++) {
      setCount += bitSet[i];
    }

    BigInteger index = 0;

    for (int i = 0; i < setCount; i++) {
      index += NPermuteK(Bits, i);
    }

    return index + EncodeRecursive(0, Bits, setCount, bitSet);
  }

  public BigInteger CalculateIndexFromPercent(float percent) {
    return Fraction(MaxIndex, Mathf.Clamp01(percent));
  }

  public BigInteger CalculateNormalizedIndexFromPercent(float percent) {
    int slots = Bits + 1;
    int slot = Mathf.FloorToInt(percent * slots);
    slot = Mathf.Clamp(slots, 0, slot - 1);

    float inSlotT = Mathf.InverseLerp(slot, slot + 1, percent * slots);

    BigInteger startIndex = 0;
    for (int i = 0; i < slot; i++) {
      startIndex += NPermuteK(Bits, i);
    }

    BigInteger endIndex = startIndex + NPermuteK(Bits, slot);
    BigInteger delta = (endIndex - startIndex);

    return startIndex + Fraction(delta, inSlotT);
  }

  public float CalculatePercent(BigInteger index) {
    return Fraction(index, MaxIndex);
  }

  public float CalculateNormalizedPercent(BigInteger value) {
    int slots = Bits + 1;

    BigInteger index = 0;
    for (int i = 0; i < slots; i++) {
      BigInteger nextIndex = index + NPermuteK(Bits, i);
      if (value < nextIndex) {
        float slotA = (i + 0f) / slots;
        float slotB = (i + 1f) / slots;
        float slotT = Fraction(value - index, nextIndex - index);
        return Mathf.Lerp(slotA, slotB, slotT);
      }
      index = nextIndex;
    }

    return 1f;
  }

  private void DecodeRecursive(int start, int end, int setCount, BigInteger index, byte[] bitSet) {
    int length = end - start;

    if (length == 1) {
      bitSet[start] = (byte)setCount;
      return;
    }

    if (length == 8 && _leafCache.TryGetValue((setCount, (int)index), out var cachedByte)) {
      for (int i = 0; i < 8; i++) {
        bitSet[start + i] = (cachedByte & (1 << i)) == 0 ? (byte)0 : (byte)1;
      }
      return;
    }

    int subLength = length / 2;

    int minSubOccupied = Mathf.Max(0, setCount - subLength);
    int maxSubOccupied = setCount - minSubOccupied;
    int allocationCount = maxSubOccupied - minSubOccupied + 1;

    int leftOccupied = minSubOccupied;
    int rightOccupied = maxSubOccupied;

    BigInteger leftCombinations;
    BigInteger rightCombinations;
    BigInteger totalCombinations = 0;

    BigInteger localIndex = index;

    for (int i = 0; i < allocationCount; i++) {
      leftOccupied = minSubOccupied + i;
      rightOccupied = maxSubOccupied - i;

      if (_prefixCache.TryGetValue((setCount, subLength, i), out var tuple)) {
        leftCombinations = tuple.Item1;
        rightCombinations = tuple.Item2;
        totalCombinations = tuple.Item3;
      } else {
        leftCombinations = NPermuteK(subLength, leftOccupied);
        rightCombinations = NPermuteK(subLength, rightOccupied);

        totalCombinations += leftCombinations * rightCombinations;

        _prefixCache[(setCount, subLength, i)] = (leftCombinations, rightCombinations, totalCombinations);
      }

      if (localIndex < totalCombinations) {
        break;
      }
    }

    localIndex -= totalCombinations;
    localIndex += leftCombinations * rightCombinations;

    (BigInteger leftIndex, BigInteger rightIndex) = EvalDiagonalWrapCurve(leftCombinations, rightCombinations, localIndex);

    int middle = start + (end - start) / 2;

    DecodeRecursive(start, middle, leftOccupied, leftIndex, bitSet);
    DecodeRecursive(middle, end, rightOccupied, rightIndex, bitSet);

    if (length == 8) {
      cachedByte = 0;
      for (int i = 0; i < 8; i++) {
        cachedByte |= (byte)((bitSet[start + i] == 0 ? 0 : 1) << i);
      }
      _leafCache[(setCount, (int)index)] = cachedByte;
    }
  }

  private BigInteger EncodeRecursive(int start, int end, int setCount, byte[] bitSet) {
    int length = end - start;
    int middle = start + (end - start) / 2;

    if (length == 1) {
      return 0;
    }

    int subLength = length / 2;

    int leftOccupied = 0;
    int rightOccupied = 0;
    for (int i = start; i < middle; i++) {
      leftOccupied += bitSet[i];
    }
    for (int i = middle; i < end; i++) {
      rightOccupied += bitSet[i];
    }

    int minSubOccupied = Mathf.Max(0, setCount - subLength);
    int maxSubOccupied = setCount - minSubOccupied;

    BigInteger leftIndex = EncodeRecursive(start, middle, leftOccupied, bitSet);
    BigInteger rightIndex = EncodeRecursive(middle, end, rightOccupied, bitSet);

    BigInteger index = 0;
    BigInteger leftCombinations = NPermuteK(subLength, leftOccupied);
    BigInteger rightCombinations = NPermuteK(subLength, rightOccupied);

    index += EvalDiagonalWrapCurveInverse(leftIndex, rightIndex, leftCombinations, rightCombinations);

    for (int i = minSubOccupied, j = 0; i < leftOccupied; i++, j++) {
      leftCombinations = NPermuteK(subLength, minSubOccupied + j);
      rightCombinations = NPermuteK(subLength, maxSubOccupied - j);
      index += leftCombinations * rightCombinations;
    }

    return index;
  }

  private (BigInteger left, BigInteger right) EvalDiagonalWrapCurve(BigInteger width, BigInteger height, BigInteger index) {
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

  private BigInteger EvalDiagonalWrapCurveInverse(BigInteger left, BigInteger right, BigInteger width, BigInteger height) {
    BigInteger columnCount = width / 2;
    BigInteger columnSize = 2 * height;

    BigInteger baseX = (left - right);

    if (width == 0) {
      throw new Exception();
    }

    if (baseX < 0) {
      BigInteger widthsLarge = -baseX / width;
      widthsLarge += 5;

      baseX = (baseX + widthsLarge * width) % width;
    }

    BigInteger columnIndex = baseX / 2;
    BigInteger columnStart = columnIndex * columnSize;

    BigInteger inColumnIndex;
    if (columnIndex == columnCount) {
      if (columnIndex.IsEven) {
        inColumnIndex = right;
      } else {
        inColumnIndex = (height - 1) - right;
      }
    } else {
      if (columnIndex.IsEven) {
        inColumnIndex = right * 2;
        if (!baseX.IsEven) {
          inColumnIndex++;
        }
      } else {
        inColumnIndex = ((height - 1) - right) * 2;
        if (baseX.IsEven) {
          inColumnIndex++;
        }
      }
    }

    return columnStart + inColumnIndex;
  }

  private BigInteger Fraction(BigInteger value, float fraction) {
    return value * ((long)fraction * int.MaxValue) / int.MaxValue;
  }

  private float Fraction(BigInteger numerator, BigInteger denominator) {
    return (float)(numerator * int.MaxValue / denominator) / int.MaxValue;
  }

  private BigInteger NPermuteK(int n, int k) {
    if (_npkCache.TryGetValue((n, k), out var cachedValue)) {
      return cachedValue;
    }

    var result = _factorialLookup[n] / _factorialLookup[k] / _factorialLookup[n - k];

    _npkCache[(n, k)] = result;

    return result;
  }
}
