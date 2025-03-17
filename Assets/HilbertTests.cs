using Unity.Mathematics;
using UnityEngine;

public class HilbertTests : MonoBehaviour {

  public uint Index;
  public uint A, B, Bits;

  //0000 - 00 00
  //0001 - 01 00
  //0010 - 00 01
  //0011 - 01 01
  //0100 - 10 00
  //0101 - 11 00
  //0110 - 10 01
  //0111 - 11 01

  private void OnDrawGizmos() {
    uint[] axes = new uint[2];

    for (int i = 0; i < 32; i++) {
      bool bit = (Index & (1 << i)) != 0;

      int run = i / axes.Length;
      int axis = (axes.Length - 1) - i % axes.Length;

      if (bit) {
        axes[axis] |= 1u << run;
      }
    }

    var result = Hilbert.HilbertAxes(axes, (int)Bits);

    Gizmos.DrawCube(new Vector3(result[0], result[1], 0), Vector3.one);
  }



}
