using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{
internal static class NeighbourQuadJacobiCalculator
{
    public struct EdgeFlow
    {
        public readonly Matrix4x4 jacobi00;
        public readonly Matrix4x4 normal00;

        public readonly Matrix4x4 jacobi01;
        public readonly Matrix4x4 normal01;

        public readonly Matrix4x4 jacobi10;
        public readonly Matrix4x4 normal10;

        public readonly Matrix4x4 jacobi11;
        public readonly Matrix4x4 normal11;

        public static EdgeFlow ConstructFromJacobis
        (
            Matrix4x4 transform00,
            Matrix4x4 transform01,
            Matrix4x4 transform10,
            Matrix4x4 transform11
        )
        {
            return new EdgeFlow(
                transform00,
                transform00.inverse.transpose,

                transform01,
                transform01.inverse.transpose,

                transform10,
                transform10.inverse.transpose,

                transform11,
                transform11.inverse.transpose
            );
        }

        EdgeFlow
        (
            Matrix4x4 jacobi00,
            Matrix4x4 normal00,

            Matrix4x4 jacobi01,
            Matrix4x4 normal01,

            Matrix4x4 jacobi10,
            Matrix4x4 normal10,

            Matrix4x4 jacobi11,
            Matrix4x4 normal11
        )
        {
            this.jacobi00 = jacobi00;
            this.normal00 = normal00;

            this.jacobi01 = jacobi01;
            this.normal01 = normal01;

            this.jacobi10 = jacobi10;
            this.normal10 = normal10;

            this.jacobi11 = jacobi11;
            this.normal11 = normal11;
        }
    }

    public static EdgeFlow CalculateEdgeFlowForQuad(Vector3[] vertices, int[] indices, int quadIndex, float slotWidth, float slotHeight)
    {
        var neighbours = NeighbourQuadFinder.GetNeighbourQuadsForQuad(indices, quadIndex);
        var indicesStartIndex = quadIndex * 4;

        var x00 = CalculateXJacobiForLeftQuadCorner(vertices, indices, neighbours.leftQuad?.IncomingMeshIndexIndex, indicesStartIndex    , indicesStartIndex + 1);
        var x10 = CalculateXJacobiForLeftQuadCorner(vertices, indices, neighbours.leftQuad?.OppositeMeshIndexIndex, indicesStartIndex + 3, indicesStartIndex + 2);

        var x01 = CalculateXJacobiForRightQuadCorner(vertices, indices, indicesStartIndex    , indicesStartIndex + 1, neighbours.rightQuad?.OppositeMeshIndexIndex);
        var x11 = CalculateXJacobiForRightQuadCorner(vertices, indices, indicesStartIndex + 3, indicesStartIndex + 2, neighbours.rightQuad?.IncomingMeshIndexIndex);

        var y00 = CalculateZJacobiForBackQuadCorner(vertices, indices, neighbours.backQuad?.OppositeMeshIndexIndex, indicesStartIndex    , indicesStartIndex + 3);
        var y01 = CalculateZJacobiForBackQuadCorner(vertices, indices, neighbours.backQuad?.IncomingMeshIndexIndex, indicesStartIndex + 1, indicesStartIndex + 2);

        var y10 = CalculateZJacobiForForwardQuadCorner(vertices, indices, indicesStartIndex    , indicesStartIndex + 3, neighbours.forwardQuad?.IncomingMeshIndexIndex);
        var y11 = CalculateZJacobiForForwardQuadCorner(vertices, indices, indicesStartIndex + 1, indicesStartIndex + 2, neighbours.forwardQuad?.OppositeMeshIndexIndex);

        return EdgeFlow.ConstructFromJacobis(
            CreateXZMatrix(x00, y00, slotWidth, slotHeight),
            CreateXZMatrix(x01, y01, slotWidth, slotHeight),
            CreateXZMatrix(x10, y10, slotWidth, slotHeight),
            CreateXZMatrix(x11, y11, slotWidth, slotHeight)
        );
    }

    private static Matrix4x4 CreateXZMatrix(Vector3 x, Vector3 z, float slotWidth, float slotHeight)
    {
        var upJacobi = new Vector4(0f, slotHeight, 0f, 0f);
        var homogenousZRow = new Vector4(0f, 0f, 0f, 1f);

        return new Matrix4x4(x * slotWidth, upJacobi, z * slotWidth, homogenousZRow);
    }

    internal static Vector3 CalculateXJacobiForLeftQuadCorner(Vector3[] vertices, int[] indices, int? leftIndex, int middleIndex, int rightIndex)
    {
        return CalculateJacobiForQuadCorner(vertices, indices, leftIndex, middleIndex, rightIndex);
    }

    internal static Vector3 CalculateXJacobiForRightQuadCorner(Vector3[] vertices, int[] indices, int leftIndex, int middleIndex, int? rightIndex)
    {
        return -CalculateJacobiForQuadCorner(vertices, indices, rightIndex, middleIndex, leftIndex);
    }

    internal static Vector3 CalculateZJacobiForBackQuadCorner(Vector3[] vertices, int[] indices, int? nearIndex, int middleIndex, int farIndex)
    {
        return CalculateJacobiForQuadCorner(vertices, indices, nearIndex, middleIndex, farIndex);
    }

    internal static Vector3 CalculateZJacobiForForwardQuadCorner(Vector3[] vertices, int[] indices, int nearIndex, int middleIndex, int? farIndex)
    {
        return -CalculateJacobiForQuadCorner(vertices, indices, farIndex, middleIndex, nearIndex);
    }

    static Vector3 CalculateJacobiForQuadCorner
    (
        Vector3[] vertices,
        int[] indices,
        int? lowIndex,
        int middleIndex,
        int highIndex
    )
    {
        if (lowIndex is int lowIndexValue)
        {
            return (vertices[indices[highIndex]] - vertices[indices[lowIndexValue]]) * .5f;
        }
        else
        {
            return vertices[indices[highIndex]] - vertices[indices[middleIndex]];
        }
    }
}
}
