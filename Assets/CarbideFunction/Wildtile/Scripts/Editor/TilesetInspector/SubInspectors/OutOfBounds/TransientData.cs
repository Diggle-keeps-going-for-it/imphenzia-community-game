
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor.SubInspectors.OutOfBounds
{
[Serializable]
internal class TransientData
{
    [SerializeField]
    private List<int> vertexIndicesOutsideOfBounds = new List<int>();
    public IList<int> VertexIndicesOutsideOfBounds => vertexIndicesOutsideOfBounds;

    [SerializeField]
    private List<Vector3> cachedVertexPositions = new List<Vector3>();
    public IList<Vector3> CachedVertexPositions => cachedVertexPositions;

    public void Clear()
    {
        vertexIndicesOutsideOfBounds.Clear();
    }

    public void SetOutOfBoundsVertices(IEnumerable<int> newVertexIndicesOutsideOfBounds)
    {
        vertexIndicesOutsideOfBounds.Clear();
        vertexIndicesOutsideOfBounds.AddRange(newVertexIndicesOutsideOfBounds);
    }

    public void SetVertexPositions(IEnumerable<Vector3> newVertexPositions)
    {
        cachedVertexPositions.Clear();
        cachedVertexPositions.AddRange(newVertexPositions);
    }
}
}
