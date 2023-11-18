using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Numerics;
using Unity.Mathematics;
using Unity.VisualScripting;

public static class BigIntExtensions {


}

[ExecuteAlways]
public class Babel : MonoBehaviour {

    public int ImageWidth = 16;
    public int ImageSize = 0;

    [Range(0, 100)]
    public float Percent;
    public int Index;

    public bool CalculateCounts;
    public bool Increment;
    public bool AutoIncrement;
    public int IncrementCount;

    public int ImageDensity;
    public int[] Patterns;
    public int[] PatternCounts;
    public int[] Allocated;

    private float _prevPercent;
    private int _prevIndex;

    private void OnValidate() {
        ImageWidth = Mathf.ClosestPowerOfTwo(ImageWidth);
        ImageSize = ImageWidth * ImageWidth;

        //Debug.Log(result.GetByteCount());
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
        Allocated = new int[ImageSize - 1];
    }

    private void Update() {
        if (Increment) {
            Increment = false;
            Index++;
        }

        if (Percent != _prevPercent) {
            SetFromPercent(Percent / 100f);
            _prevPercent = Percent;
        }

        if (Index != _prevIndex) {
            SetFromIndex(Index);
            _prevIndex = Index;

            var maxPossibilities = BigInteger.Pow(2, ImageSize);
            Percent = _prevPercent = (float)((double)Index / (double)maxPossibilities);
        }

        if (CalculateCounts) {
            CalculateCounts = false;
            CalculatePixelCounts();
        }
    }

    public void SetFromPercent(float percent) {
        var maxPossibilities = BigInteger.Pow(2, ImageSize);
        var index = (maxPossibilities * new BigInteger(Mathf.RoundToInt(percent * 1000000))) / 1000000;

        if (index == maxPossibilities) {
            index = maxPossibilities - 1;
        }

        //Index = _prevIndex = (int)index;

        SetFromIndex(index);
    }

    public void SetFromIndex(BigInteger index) {
        for (int i = 0; i < Patterns.Length; i++) {
            Patterns[i] = 0;
        }

        var residual = index;
        ImageDensity = 0;
        while (true) {
            if (ImageDensity >= ImageSize) {
                break;
            }

            var ways = NPermuteK(ImageSize, ImageDensity);

            if (residual < ways) {
                break;
            }

            ImageDensity++;
            residual -= ways;
        }

        CalculatePixelCounts();

        int level = 0;
        int levelStart = 0;
        int levelEnd = 1;
        int regionSize = ImageSize / 2;

        int what = 10000;

        if (residual == 0) {
            return;
        }

        int iterations = 0;

        while (true) {
            //for (int i = levelEnd - 2; i >= levelStart; i--) {
            //    if (residual >= JumpCosts[i]) {
            //        int times = (int)(residual / JumpCosts[i]);
            //        Patterns[i + 1] += times;
            //        residual -= times * JumpCosts[i];
            //    }
            //}

            if (residual <= 0) {
                break;
            }


            BigInteger levelCost = new BigInteger(1);

            for (int i = levelStart; i < levelEnd; i++) {
                var allocation = N2XY(Allocated[i], regionSize, Patterns[i]);
                levelCost *= NPermuteK(regionSize, allocation.x);
                levelCost *= NPermuteK(regionSize, allocation.y);
            }

            if (residual < levelCost) {
                CalculatePixelCounts();
                level++;
                levelStart = levelStart * 2 + 1;
                levelEnd = levelEnd * 2 + 1;
                regionSize /= 2;
                continue;
            }

            int toInc = levelStart;
            while (true) {
                Patterns[toInc]++;
                if (Patterns[toInc] == PatternCounts[toInc]) {
                    Patterns[toInc] = 0;
                    toInc++;
                } else {
                    break;
                }
            }

            residual -= levelCost;

            if (residual <= 0) {
                break;
            }

            //if (Patterns[levelStart] == PatternCounts[levelStart]) {
            //    break;
            //}
            iterations++;
        }

        CalculatePixelCounts();
    }

    public Dictionary<(int, int), BigInteger> Cache = new();

    public BigInteger NPermuteK(int n, int k) {
        if (Cache.TryGetValue((n, k), out var early)) {
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

        Cache[(n, k)] = result;

        return result;
    }

    public BigInteger NChooseK(BigInteger n, BigInteger k) {
        if (k < 0 || k > n)
            return 0;
        if (k == 0 || k == n)
            return 1;
        k = BigInteger.Min(k, n - k);

        BigInteger c = 1;
        for (BigInteger i = 0; i < k; i++) {
            c = c * (n - i);
        }
        return c;
    }

    public void GenerateImage() {
        void Recurse(int level, int delta, int index, int allocated, int maxPerHalve, int x, int y) {
            int2 allocation = N2XY(allocated, maxPerHalve, Patterns[index]);

            int dx = 0;
            int dy = 0;

            if (level % 2 == 0) {
                dx += delta;
            } else {
                dy += delta;
                delta /= 2;
            }

            if (index * 2 + 1 >= Patterns.Length) {
                if (allocation.x > 0)
                    Gizmos.DrawCube(new UnityEngine.Vector3(x, y, 0), new UnityEngine.Vector3(1, 1, 1));
                if (allocation.y > 0)
                    Gizmos.DrawCube(new UnityEngine.Vector3(x + dx, y + dy, 0), new UnityEngine.Vector3(1, 1, 1));
            } else {
                Recurse(level + 1, delta, index * 2 + 1, allocation.x, maxPerHalve / 2, x, y);
                Recurse(level + 1, delta, index * 2 + 2, allocation.y, maxPerHalve / 2, x + dx, y + dy);
            }

        }
        Recurse(0, ImageWidth / 2, 0, ImageDensity, ImageSize / 2, 0, 0);
    }

    public void CalculatePixelCounts() {
        void Recurse(int level, int index, int allocated, int maxPerHalve) {
            PatternCounts[index] = N2Count(allocated, maxPerHalve);
            Allocated[index] = allocated;
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
        GenerateImage();
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
