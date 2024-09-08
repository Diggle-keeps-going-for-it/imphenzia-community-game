using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarbideFunction.Wildtile
{

/// <summary>
/// A 3D grid of voxels that can be either "filled" or "empty".
///
/// Not necessarily square, this can take any form.
/// </summary>
[CreateAssetMenu(fileName="New Irregular Voxel Grid", menuName= MenuConstants.topMenuName+"Irregular Voxel Grid", order=MenuConstants.orderBase+3)]
public class IrregularVoxelGrid : VoxelGridAsset
{
    [SerializeField]
    internal int[] voxelContents;

    [SerializeField]
    internal int height;

    [SerializeField]
    internal Vector3[] vertices;

    [SerializeField]
    internal int[] indices;

    override public void Visit(IVoxelGridVisitor visitor)
    {
        visitor.VisitIrregularVoxelGrid(this);
    }

    override public IVoxelGrid.Voxel GetVoxel(int index)
    {
        Assert.IsTrue(index < voxelContents.Length);
        var verticalLayer = index / vertices.Length;
        Assert.IsTrue(verticalLayer < height);
        var horizontalIndex = index - verticalLayer * vertices.Length;
        var upIndex = (verticalLayer + 1 < height) ? index + vertices.Length : IVoxelGrid.outOfBoundsVoxelIndex;
        var downIndex = (verticalLayer - 1 >= 0) ? index - vertices.Length : IVoxelGrid.outOfBoundsVoxelIndex;
        var horizontalQuads = NeighbourQuadFinder.GetNeighbourQuads(indices, horizontalIndex).ToArray();

        var voxelFaces = new List<IVoxelGrid.VoxelFace>();
        var zeroLayerVertexIndex = index % vertices.Length;
        var voxelCenter3d = vertices[zeroLayerVertexIndex];
        var voxelCenter = new Vector2(voxelCenter3d.x, voxelCenter3d.z);
        for (var neighbourQuadIndex = 0; neighbourQuadIndex < horizontalQuads.Length; ++neighbourQuadIndex)
        {
            var neighbourQuad = horizontalQuads[neighbourQuadIndex];

            var indexStart = neighbourQuad.quad.quadIndex * 4;
            var quadCentroid = indices.Skip(indexStart).Take(4).Aggregate(Vector3.zero, (runningTotal, vertexIndex) => runningTotal + vertices[vertexIndex]) / 4f;
            var incomingZeroLayerVertexIndex = indices[neighbourQuad.quad.IncomingMeshIndexIndex];
            var incomingVertexIndex = incomingZeroLayerVertexIndex + verticalLayer * vertices.Length;
            var incomingVertexPosition3d = vertices[incomingZeroLayerVertexIndex];
            var incomingVertexPosition = new Vector2(incomingVertexPosition3d.x, incomingVertexPosition3d.z);
            var edgeMidpoint = Vector2.Lerp(voxelCenter, incomingVertexPosition, 0.5f);

            voxelFaces.Add(new IVoxelGrid.VoxelFace{
                edgeMidpoint = edgeMidpoint,
                corner = new Vector2(quadCentroid.x, quadCentroid.z),
                facingVoxelIndex = incomingVertexIndex,
                breakBetweenThisFaceAndNext = false,
            });

            if (neighbourQuad.breakBetweenThisQuadAndNext)
            {
                var outgoingZeroLayerVertexIndex = indices[neighbourQuad.quad.OutgoingMeshIndexIndex];
                var outgoingVertexIndex = outgoingZeroLayerVertexIndex + verticalLayer * vertices.Length;
                var outgoingVertexPosition3d = vertices[outgoingZeroLayerVertexIndex];
                var outgoingVertexPosition = new Vector2(outgoingVertexPosition3d.x, outgoingVertexPosition3d.z);
                var nextFaceMidpoint = Vector2.Lerp(voxelCenter, outgoingVertexPosition, 0.5f);
                voxelFaces.Add(new IVoxelGrid.VoxelFace{
                    edgeMidpoint = nextFaceMidpoint,
                    corner = voxelCenter,
                    facingVoxelIndex = outgoingVertexIndex,
                    breakBetweenThisFaceAndNext = true,
                });
            }
        }

        var lowerUvY = (verticalLayer == 0) ? 0.5f : 0f;
        var upperUvY = (verticalLayer == height - 1) ? 0.5f : 1f;

        var voxelElevation = (float)verticalLayer;

        var lowerYCoordinate = voxelElevation + lowerUvY;
        var upperYCoordinate = voxelElevation + upperUvY;

        return new IVoxelGrid.Voxel{
            contents = voxelContents[index],
            upNeighbourVoxelIndex = upIndex,
            downNeighbourVoxelIndex = downIndex,
            lowerYCoordinate = lowerYCoordinate,
            upperYCoordinate = upperYCoordinate,
            lowerUvY = lowerUvY,
            upperUvY = upperUvY,
            voxelCenter = voxelCenter,
            horizontalVoxelFaces = voxelFaces.ToArray(),
        };
    }

    override public void SetVoxelContents(int index, int newContents)
    {
        Assert.IsTrue(index >= 0);
        Assert.IsTrue(index < voxelContents.Length);
        voxelContents[index] = newContents;
    }

    override public IReadOnlyList<IVoxelGrid.Voxel> Voxels 
    { get {
        return Enumerable.Range(0, voxelContents.Length).Select(voxelIndex =>
                this.GetVoxel(voxelIndex)
            ).ToList();
    }}
}
}
