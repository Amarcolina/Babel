using Unity.Mathematics;
using UnityEngine;

public class HilbertTests : MonoBehaviour {

  public uint Index;
  public uint Axes;
  public uint A, B, Bits;

  public uint[] Result;

  //0000 - 00 00
  //0001 - 01 00
  //0010 - 00 01
  //0011 - 01 01
  //0100 - 10 00
  //0101 - 11 00
  //0110 - 10 01
  //0111 - 11 01

  private void OnDrawGizmos() {
    uint[] axes = new uint[Axes];

    for (int i = 0; i < 32; i++) {
      bool bit = (Index & (1 << i)) != 0;

      int run = i / axes.Length;
      int axis = (axes.Length - 1) - i % axes.Length;

      if (bit) {
        axes[axis] |= 1u << run;
      }
    }

    Result = Hilbert.HilbertAxes(axes, (int)Bits);

    for (int i = 0; i < Result.Length; i++) {
      Gizmos.color = Result[i] == 0 ? Color.black : Color.white;
      Gizmos.DrawCube(new Vector3(i, 0, 0), Vector3.one);
    }

    //Gizmos.DrawCube(new Vector3(Result[0], Result[1], Result[1]), Vector3.one);
  }



}
