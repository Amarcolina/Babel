using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class Babel2 : MonoBehaviour {

    public int ImageWidth;
    public int ImageArea;


    private void OnValidate() {
        ImageArea = ImageWidth * ImageWidth;
    }

    public void GenerateFromIndex(BigInteger index) {
        var residual = index;
        int totalPixels = 0;

        while (true) {
            if (totalPixels >= ImageArea) {
                break;
            }

            var ways = NPermuteK(ImageArea, totalPixels);

            if (residual < ways) {
                break;
            }

            totalPixels++;
            residual -= ways;
        }











    }

    public BigInteger RecursivelySplitPixels(int pixels, int area, BigInteger index) {
        var ways = NPermuteK(area, pixels);
        var leftOver = new BigInteger(0);
        if (index > ways) {
            var forUs = index % ways;
            leftOver = index - forUs;
            index = forUs;
        } else {
            leftOver = 0;
        }



        return leftOver;
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



}
