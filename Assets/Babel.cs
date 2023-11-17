using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Numerics;
using Unity.Mathematics;

[ExecuteAlways]
public class Babel : MonoBehaviour
{

    public int N = 16;
    public int D = 0;
    public int Pixels = 0;

    public bool Increment;

    public int ImageDensity;
    public int[] Image;

    private void OnValidate()
    {
        N = Mathf.ClosestPowerOfTwo(N);
        Pixels = N * N;
    }

    private void OnEnable()
    {
        //int levels = Mathf.RoundToInt(Mathf.Log(N) / Mathf.Log(2));
        //int curves = 0;
        //int curvesPerLevel = 1;
        //for (int i = 0; i < levels; i++)
        //{
        //    curves += curvesPerLevel;
        //    curvesPerLevel *= 4;
        //}

        Image = new int[Pixels - 1];
    }

    private void Update()
    {
        if (Increment)
        {
            Increment = false;
            NextImage();
        }
    }

    public void NextImage()
    {
        bool Increment(int index, int N)
        {
            if (N == 0)
            {
                return false;
            }

            if (!IsLeaf(index))
            {
                int4 allocation = d2xyzw(N / 4, Image[index]);
                if (Increment(index * 4 + 1, allocation.x)) return true;
                if (Increment(index * 4 + 2, allocation.y)) return true;
                if (Increment(index * 4 + 3, allocation.z)) return true;
                if (Increment(index * 4 + 4, allocation.w)) return true;
            }

            if (Image[index] == (N - 1))
            {
                Image[index] = 0;
                return false;
            }
            else
            {
                Image[index]++;
                return true;
            }
        }

        Increment(0, Pixels);
    }

    public void Draw()
    {
        void DrawRecursively(int index, int N)
        {
            if (N == 0)
            {
                return;
            }


        }

        DrawRecursively(0, Pixels);
    }


    public bool IsLeaf(int index)
    {
        return index * 4 + 1 >= Image.Length;
    }

    [ContextMenu("Try")]
    void OkTry()
    {
        var num = BigInteger.Pow(new BigInteger(2), 256 * 256);
        Debug.Log(num.ToString());
    }

    private void OnDrawGizmos()
    {
        int4 pos = d2xyzw(N, D);

        Gizmos.DrawCube(new UnityEngine.Vector3(pos.x, pos.y, pos.z), UnityEngine.Vector3.one);

        //Gizmos.color = new Color(a, a, a, 1);
        //Gizmos.DrawCube(new Vector3(0, 0, 0), Vector3.one);
        //Gizmos.color = new Color(b, b, b, 1);
        //Gizmos.DrawCube(new Vector3(1, 0, 0), Vector3.one);

        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < N; y++)
            {
                for (int z = 0; z < N; z++)
                {
                    if (x + y + z == N)
                    {
                        Gizmos.DrawCube(new UnityEngine.Vector3(x, y, z), new UnityEngine.Vector3(1, 1, 1));
                    }
                }
            }
        }
    }



    //convert (x,y) to d
    int xy2d(int n, int x, int y)
    {
        int rx, ry, s, d = 0;
        for (s = n / 2; s > 0; s /= 2)
        {
            rx = (x & s) > 0 ? 1 : 0;
            ry = (y & s) > 0 ? 1 : 0;
            d += s * s * ((3 * rx) ^ ry);
            rot(n, ref x, ref y, rx, ry);
        }
        return d;
    }

    //convert d to (x,y)
    void d2xy(int n, int d, ref int x, ref int y)
    {
        int rx, ry, s, t = d;
        x = y = 0;
        for (s = 1; s < n; s *= 2)
        {
            rx = 1 & (t / 2);
            ry = 1 & (t ^ rx);
            rot(s, ref x, ref y, rx, ry);
            x += s * rx;
            y += s * ry;
            t /= 4;
        }
    }

    int4 d2xyzw(int n, int d)
    {
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
    void rot(int n, ref int x, ref int y, int rx, int ry)
    {
        if (ry == 0)
        {
            if (rx == 1)
            {
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
