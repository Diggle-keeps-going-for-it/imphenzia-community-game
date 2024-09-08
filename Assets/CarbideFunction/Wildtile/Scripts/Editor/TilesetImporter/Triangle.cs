using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This class contains three 3D vectors that comprise a triangle. It is bespoke for CarbideFunction.Wildtile and used in the insideness calculator.
/// </summary>
internal class Triangle : IEnumerable<Vector3>, IEnumerable
{
    public Vector3 vertex0;
    public Vector3 vertex1;
    public Vector3 vertex2;

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
    public IEnumerator<Vector3> GetEnumerator()
    {
        yield return vertex0;
        yield return vertex1;
        yield return vertex2;
    }
}

}
