using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst.Intrinsics;

public static class NativeBigUInt {

  public static void Add(NativeArray<ulong> target, int value) {
    var prevDigit = target[0];
    target[0] += (ulong)value;
    if (target[0] < prevDigit && target.Length > 1) {
      target[1]++;
    }
  }

  public static void Add(NativeArray<ulong> target, NativeArray<ulong> arg) {
    int minIndex = Mathf.Min(target.Length, arg.Length);

    ulong carry = 0;
    for (int i = 0; i < minIndex; i++) {
      ulong prevDigit = target[i];
      target[i] += arg[i] + carry;
      if (target[i] < prevDigit || (target[i] == prevDigit && carry != 0)) {
        carry = 1;
      } else {
        carry = 0;
      }
    }

    if (carry != 0 && (minIndex + 1) < target.Length) {
      target[minIndex + 1]++;
    }
  }

  public static void Sub(NativeArray<ulong> target, int value) {

  }

  public static void Sub(NativeArray<ulong> target, NativeArray<ulong> arg) {

  }

  public static void Mul(NativeArray<ulong> target, int value) {
    
  }

}
