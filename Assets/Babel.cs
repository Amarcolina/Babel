using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Numerics;
using Unity.Mathematics;
using Unity.VisualScripting;

[ExecuteAlways]
public class Babel : MonoBehaviour {

    public int ImageWidth = 16;
    public int ImageSize = 0;

    public bool CalculateCounts;
    public bool Increment;

    public int ImageDensity;
    public int[] Patterns;
    public int[] PatternCounts;

    private void OnValidate() {
        ImageWidth = Mathf.ClosestPowerOfTwo(ImageWidth);
        ImageSize = ImageWidth * ImageWidth;
    }

    private void OnEnable() {
        //int levels = Mathf.RoundToInt(Mathf.Log(N) / Mathf.Log(2));
        //int curves = 0;
        //int curvesPerLevel = 1;
        //for (int i = 0; i < levels; i++)
        //{
        //    curves += curvesPerLevel;
        //    curvesPerLevel *= 4;
        //}

        Patterns = new int[ImageSize - 1];
        PatternCounts = new int[ImageSize - 1];
    }

    private void Update() {
        if (CalculateCounts) {
            CalculateCounts = false;
            CalculatePixelCounts();
        }

        if (Increment) {
            Increment = false;
            NextImage();
        }
    }

    public void CalculatePixelCounts() {
        void Recurse(int level, int index, int allocated, int maxPerHalve) {
            PatternCounts[index] = N2Count(allocated, maxPerHalve);
            int2 allocation = N2XY(allocated, maxPerHalve, Patterns[index]);

            if (index * 2 + 1 >= Patterns.Length) {
                return;
            }

            Recurse(level + 1, index * 2 + 1, allocation.x, maxPerHalve / 2);
            Recurse(level + 1, index * 2 + 2, allocation.y, maxPerHalve / 2);
        }

        Recurse(0, 0, ImageDensity, ImageSize / 2);
    }

    public void NextImage() {
        CalculatePixelCounts();

        for (int i = Patterns.Length - 1; i >= 0; i--) {
            Patterns[i]++;
            if (Patterns[i] >= PatternCounts[i]) {
                Patterns[i] = 0;
                if (i == 0) {
                    ImageDensity++;
                }
            } else {
                break;
            }
        }

        CalculatePixelCounts();
    }

    public void Draw() {
        void DrawRecursively(int index, int N) {
            if (N == 0) {
                return;
            }


        }

        DrawRecursively(0, ImageSize);
    }


    public bool IsLeaf(int index) {
        return index * 4 + 1 >= Patterns.Length;
    }

    private void OnDrawGizmos() {
    }


    public int N2Count(int allocation, int maxAlloc) {
        if (allocation < maxAlloc) maxAlloc = allocation;

        int minAlloc = allocation - maxAlloc;
        int validCount = maxAlloc - minAlloc + 1;

        return validCount;
    }

    public int2 N2XY(int count, int maxAlloc, int index) {
        if (count < maxAlloc) maxAlloc = count;

        int minAlloc = count - maxAlloc;
        int validCount = maxAlloc - minAlloc + 1;

        if (validCount <= 0) {
            return 0;
        }

        int even = (validCount + 1) / 2;
        int odd = validCount - even;

        int x;
        if (index < even) {
            x = minAlloc + index * 2;
        } else {
            x = minAlloc + even * 2 - 1 - (index - even) * 2;
            if (validCount % 2 == 1) x -= 2;
        }

        return new int2(x, count - x);
    }

    //convert (x,y) to d
    int xy2d(int n, int x, int y) {
        int rx, ry, s, d = 0;
        for (s = n / 2; s > 0; s /= 2) {
            rx = (x & s) > 0 ? 1 : 0;
            ry = (y & s) > 0 ? 1 : 0;
            d += s * s * ((3 * rx) ^ ry);
            rot(n, ref x, ref y, rx, ry);
        }
        return d;
    }

    //convert d to (x,y)
    void d2xy(int n, int d, ref int x, ref int y) {
        int rx, ry, s, t = d;
        x = y = 0;
        for (s = 1; s < n; s *= 2) {
            rx = 1 & (t / 2);
            ry = 1 & (t ^ rx);
            rot(s, ref x, ref y, rx, ry);
            x += s * rx;
            y += s * ry;
            t /= 4;
        }
    }

    int4 d2xyzw(int n, int d) {
        int x = d % n;
        if (d / n % 2 == 1) x = (n - 1) - x;

        int y = d / n % n;
        if (d / n / n % 2 == 1) y = (n - 1) - y;

        int z = d / n / n % n;
        if (d / n / n / n % 2 == 1) z = (n - 1) - z;

        int w = d / n / n / n;

        return new int4(x, y, z, w);
    }

    //rotate/flip a quadrant appropriately
    void rot(int n, ref int x, ref int y, int rx, int ry) {
        if (ry == 0) {
            if (rx == 1) {
                x = n - 1 - x;
                y = n - 1 - y;
            }

            //Swap x and y
            int t = x;
            x = y;
            y = t;
        }
    }
}
