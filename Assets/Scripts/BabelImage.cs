using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// The BabelImage class assists with takign the 1-dimensional bit-vectors generated from the BabelCodec
/// class, and turning them into coherent 2-dimensional images that retain the same properties.
/// </summary>
public class BabelImage {

  public readonly int Bits;
  public readonly int SideLength;

  private readonly int2[] _bitPositionToImagePosition;
  private readonly int[,] _imagePositionToBitPosition;

  public BabelImage(int bits) {
    if (bits <= 0) {
      throw new ArgumentException($"Bit count must be positive and non-zero but was {bits}");
    }
    if (!Mathf.IsPowerOfTwo(bits)) {
      throw new ArgumentException($"Bit count must be a power of two but was {bits}");
    }

    Bits = bits;
    SideLength = (int)math.sqrt(bits);

    if (SideLength * SideLength != bits) {
      throw new ArgumentException($"Bit count must be a square number but was {bits}");
    }

    _bitPositionToImagePosition = new int2[bits];
    _imagePositionToBitPosition = new int[SideLength, SideLength];

    InitPositionLoopupRecursive(0, bits, 0, 0, SideLength);
  }

  public int ImagePositionToBitPosition(int2 imagePosition) {
    return _imagePositionToBitPosition[imagePosition.x, imagePosition.y];
  }

  public int ImagePositionToBitPosition(int imageX, int imageY) {
    return _imagePositionToBitPosition[imageX, imageY];
  }

  public void EncodeToImage(byte[] bitVector, byte[,] image) {
    Assert.AreEqual(Bits, bitVector.Length);
    Assert.AreEqual(image.GetLength(0), SideLength);
    Assert.AreEqual(image.GetLength(1), SideLength);

    for (int i = 0; i < bitVector.Length; i++) {
      var pos = _bitPositionToImagePosition[i];
      image[pos.x, pos.y] = bitVector[i];
    }
  }

  public void EncodeToImage(byte[] bitVector, byte[] image) {
    Assert.AreEqual(Bits, bitVector.Length);
    Assert.AreEqual(image.GetLength(0), SideLength);
    Assert.AreEqual(image.GetLength(1), SideLength);

    for (int i = 0; i < bitVector.Length; i++) {
      var pos = _bitPositionToImagePosition[i];
      image[pos.x + pos.y * SideLength] = bitVector[i];
    }
  }

  public void DecodeFromImage(byte[,] image, byte[] bitVector) {
    Assert.AreEqual(Bits, bitVector.Length);
    Assert.AreEqual(image.GetLength(0), SideLength);
    Assert.AreEqual(image.GetLength(1), SideLength);

    for (int y = 0; y < SideLength; y++) {
      for (int x = 0; x < SideLength; x++) {
        bitVector[_imagePositionToBitPosition[x, y]] = image[x, y];
      }
    }
  }

  public void DecodeFromImage(byte[] image, byte[] bitVector) {
    Assert.AreEqual(Bits, bitVector.Length);
    Assert.AreEqual(image.GetLength(0), SideLength);
    Assert.AreEqual(image.GetLength(1), SideLength);

    for (int y = 0; y < SideLength; y++) {
      for (int x = 0; x < SideLength; x++) {
        bitVector[_imagePositionToBitPosition[x, y]] = image[x + y * SideLength];
      }
    }
  }

  public Texture2D GenerateLookupTexture() {
    var lookup = new Texture2D(SideLength, SideLength, TextureFormat.RGHalf, mipChain: false, linear: true);
    lookup.filterMode = FilterMode.Point;

    var data = lookup.GetPixelData<half2>(mipLevel: 0);

    for (int i = 0; i < Bits; i++) {
      int2 dstPos = _bitPositionToImagePosition[i];
      int srcX = i % SideLength;
      int srcY = i / SideLength;
      float2 srcUv = new float2(srcX + 0.5f, srcY + 0.5f) / SideLength;
      data[dstPos.x + dstPos.y * SideLength] = (half2)srcUv;
    }

    lookup.Apply(updateMipmaps: false, makeNoLongerReadable: false);

    return lookup;
  }

  private void InitPositionLoopupRecursive(int start, int end, int sign, int2 min, int2 max) {
    int length = end - start;
    if (length == 1) {
      _bitPositionToImagePosition[start] = min;
      _imagePositionToBitPosition[min.x, min.y] = start;
      return;
    }

    int middleIndex = start + (end - start) / 2;
    int middleAxis = (min + (max - min) / 2)[sign];

    int2 leftMax = max;
    int2 rightMin = min;

    leftMax[sign] = middleAxis;
    rightMin[sign] = middleAxis;

    InitPositionLoopupRecursive(start, middleIndex, 1 - sign, min, leftMax);
    InitPositionLoopupRecursive(middleIndex, end, 1 - sign, rightMin, max);
  }
}
