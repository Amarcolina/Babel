using System.Collections;
using System.Numerics;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

public class NewTestScript {

  public static readonly (BigInteger a, BigInteger b)[] AddCases = {
    (1, 1),
    (100, 0),
    (0, 200),
    (uint.MaxValue, uint.MaxValue),
    (ulong.MaxValue, ulong.MaxValue),
    (ulong.MaxValue, 1),
    (BigInteger.Pow(2, 200) - 1, 1),
    (BigInteger.Pow(3, 200), BigInteger.Pow(5, 200))
  };

  public static readonly (BigInteger a, BigInteger b)[] SubCases = {
    (0, 0),
    (1, 0),
    (1, 1),
    (10, 2),
    (uint.MaxValue, uint.MaxValue),
    (ulong.MaxValue, ulong.MaxValue),
    (BigInteger.Pow(1, 64), 1),
    (BigInteger.Pow(1, 128), 1),
    (BigInteger.Pow(5, 200), BigInteger.Pow(3, 200))
  };

  public NativeArray<ulong> A, B;

  [SetUp]
  public void SetUp() {
    A = new NativeArray<ulong>(10, Allocator.Persistent);
    B = new NativeArray<ulong>(10, Allocator.Persistent);
  }

  [TearDown]
  public void TearDown() {
    A.Dispose();
    B.Dispose();
  }

  [Test]
  public void TestAdd([ValueSource("AddCases")] (BigInteger a, BigInteger b) arg) {
    NativeBigUInt.Init(A, arg.a);
    NativeBigUInt.Init(B, arg.b);
    NativeBigUInt.Add(A, B);

    var sw = new System.Diagnostics.Stopwatch();

    sw.Start();
    for (int i = 0; i < 100; i++) {
      NativeBigUInt.Add(A, B);
    }
    sw.Stop();
    Debug.Log(sw.Elapsed.TotalMilliseconds);

    sw.Reset();
    sw.Start();
    for (int i = 0; i < 100; i++) {
      var bigResult = arg.a + arg.b;
    }
    sw.Stop();
    Debug.Log(sw.Elapsed.TotalMilliseconds);

    //Assert.That(NativeBigUInt.Equals(A, bigResult));
  }

  [Test]
  public void TestSub([ValueSource("SubCases")] (BigInteger a, BigInteger b) arg) {
    NativeBigUInt.Init(A, arg.a);
    NativeBigUInt.Init(B, arg.b);

    var sw = new System.Diagnostics.Stopwatch();

    sw.Start();
    NativeBigUInt.Sub(A, B);
    sw.Stop();
    Debug.Log(sw.Elapsed.TotalMilliseconds);

    sw.Reset();
    sw.Start();
    var bigResult = arg.a - arg.b;
    sw.Stop();
    Debug.Log(sw.Elapsed.TotalMilliseconds);

    Assert.That(NativeBigUInt.Equals(A, bigResult));
  }
}
