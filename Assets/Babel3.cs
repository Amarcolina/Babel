using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class Babel3 : MonoBehaviour {

  private byte[] _array = new byte[16];

  public int TotalSetPixels;
  public int Index = 0;

  private void OnDrawGizmos() {
    SetFromIndex(0, _array.Length, TotalSetPixels, Index);

    for (int i = 0; i < _array.Length; i++) {
      Gizmos.color = _array[i] == 0 ? Color.black : Color.white;
      Gizmos.DrawCube(new UnityEngine.Vector3(i, 0, 0), UnityEngine.Vector3.one);
    }
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

      if (totalCombinations > index) {
        break;
      }

      index -= totalCombinations;
      leftOccupied++;
      rightOccupied--;

      if (rightOccupied < minSubOccupied) {
        throw new System.Exception();
      }
    }

    BigInteger leftIndex = index % leftCombinations;
    BigInteger rightIndex = index / leftCombinations;

    if (rightIndex % 2 == 1) {
      leftIndex = (leftCombinations - 1) - leftIndex;
    }

    int middle = start + (end - start) / 2;
    SetFromIndex(start, middle, leftOccupied, leftIndex);
    SetFromIndex(middle, end, rightOccupied, rightIndex);
  }

  //Idea for space filling curve
  //  Create snake blocks of N width by 2 height
  //  Choose best N>=2 to make N/2 best approximate the ratio of enclosing space

  //  Layout out columns of N-wide snake blocks, last block will have to handle
  //  the residual width remaining N%width
  //
  //  Each alternating column travels up and down
  //
  // If total width is even, the entire path starts with a single path from
  // on the bottom that starts the 






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
