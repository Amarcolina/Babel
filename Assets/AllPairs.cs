using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AllPairs : MonoBehaviour {

  public int A = 1, B = 1;

  public bool IsComplete;

  public List<string> Pairs = new();
  public List<string> TruePairs = new();
  public List<string> Missing = new();

  [Serializable]
  public struct Pair {
    public int A, B;
  }

  private void OnValidate() {
    Pairs.Clear();
    TruePairs.Clear();

    int combos = A * B;

    for (int i = 0; i < combos; i++) {
      int a = i % A;
      int b = i / A;
      TruePairs.Add($"{a} - {b}");
    }

    for (int i = 0; i < combos; i++) {
      int a = i % A;
      int b = i / A;

      if (b % 2 == 1) {
        a = (A - 1) - a;
      }

      Pairs.Add($"{a} - {b}");
    }

    IsComplete = TruePairs.Intersect(Pairs).Count() == combos;
    Missing = TruePairs.Except(Pairs).ToList();
  }


}
