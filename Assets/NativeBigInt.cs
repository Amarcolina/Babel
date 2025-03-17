using System.Numerics;
using Unity.Collections;
using UnityEngine;

public static class NativeBigUInt {

  public static void Init(NativeArray<ulong> target, BigInteger value) {
    for (int i = 0; i < target.Length; i++) {
      ulong digit = (ulong)(value & ulong.MaxValue);
      target[i] = digit;
      value = value >> 64;
    }
  }

  public static void Init(NativeArray<ulong> target, ulong value) {
    target[0] = value;
    for (int i = 1; i < target.Length; i++) {
      target[i] = 0;
    }
  }

  public static bool Equals(NativeArray<ulong> target, ulong value) {
    for (int i = 1; i < target.Length; i++) {
      if (target[i] != 0) {
        return false;
      }
    }
    return target[0] == value;
  }

  public static bool Equals(NativeArray<ulong> target, BigInteger value) {
    if (value < 0) {
      return false;
    }

    for (int i = 0; i < target.Length; i++) {
      ulong digit = (ulong)(value & ulong.MaxValue);
      if (digit != target[i]) {
        return false;
      }
      value = value >> 64;
    }

    return value == 0;
  }

  public static void Add(NativeArray<ulong> target, ulong value) {
    var prevDigit = target[0];
    target[0] += value;
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

  public static void Sub(NativeArray<ulong> target, ulong value) {
    if (target[0] < value) {
      int i = 1;
      while (i < target.Length) {
        if (target[i] != 0) {
          target[i]--;
          break;
        } else {
          target[i]--;
        }
      }
    }

    target[0] -= value;
  }

  public static void Sub(NativeArray<ulong> target, NativeArray<ulong> arg) {
    int minIndex = Mathf.Min(target.Length, arg.Length);

    for (int i = 0; i < minIndex; i++) {
      if (target[i] < arg[i]) {
        int j = i + 1;
        while (j < target.Length) {
          if (target[j] != 0) {
            target[j]--;
            break;
          } else {
            target[j]--;
          }
        }
      }

      target[i] -= arg[i];
    }
  }

  public static void Mul(NativeArray<ulong> target, int value) {

  }

}
