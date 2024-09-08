using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{
internal static class NeighbourQuadFinder
{
    public struct NeighbourQuad
    {
        public int quadIndex;
        public int inQuadVertexIndex;

        public int MeshIndexIndex => inQuadVertexIndex + quadIndex * 4;
        public int OutgoingMeshIndexIndex => (inQuadVertexIndex + 1) % 4 + quadIndex * 4;
        public int OppositeMeshIndexIndex => (inQuadVertexIndex + 2) % 4 + quadIndex * 4;
        public int IncomingMeshIndexIndex => (inQuadVertexIndex + 3) % 4 + quadIndex * 4;

        public override string ToString()
        {
            return $"quad {quadIndex}, with quad index {inQuadVertexIndex} matching the target vertex";
        }
    }

    public struct NeighbourQuadAndRelations
    {
        public NeighbourQuad quad;
        public bool breakBetweenThisQuadAndNext;

        public override string ToString()
        {
            return $"{quad.ToString()}{(breakBetweenThisQuadAndNext ? " (break after)" : "")}";
        }
    }

    public struct NeighbourQuadsForQuad
    {
        public NeighbourQuad? backQuad;
        public NeighbourQuad? rightQuad;
        public NeighbourQuad? forwardQuad;
        public NeighbourQuad? leftQuad;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="quadIndex">The index of the quad. This should be 1/4 of the quad's start index.</param>
    public static NeighbourQuadsForQuad GetNeighbourQuadsForQuad(int[] indices, int quadIndex)
    {
        Assert.IsTrue(indices.Length % 4 == 0, "Indices needs to have a multiple of 4 indices in it to be treated as a quad submesh");
        Assert.IsTrue(quadIndex >= 0, "Quad index must be 0 or greater.");
        Assert.IsTrue(indices.Length >= quadIndex * 4 + 4, $"Quad index out of range of the indices. There are {indices.Length} indices and the quad finishes at index {quadIndex * 4 + 3}. Is it a vertex index instead of a quad index?");

        var quadStartIndex = quadIndex * 4;
        var index0 = indices[quadStartIndex    ];
        var index1 = indices[quadStartIndex + 1];
        var index2 = indices[quadStartIndex + 2];
        var index3 = indices[quadStartIndex + 3];
        return new NeighbourQuadsForQuad{
            backQuad = FindQuadWithIndices(indices, index1, index0),
            rightQuad = FindQuadWithIndices(indices, index2, index1),
            forwardQuad = FindQuadWithIndices(indices, index3, index2),
            leftQuad = FindQuadWithIndices(indices, index0, index3),
        };
    }

    public static NeighbourQuad? FindQuadWithIndices(int[] indices, int firstVertexIndex, int secondVertexIndex)
    {
        return Enumerable.Range(0, indices.Length / 4).Select<int, NeighbourQuad?>(
            quadIndex => {
                var startIndex = quadIndex * 4;
                var foundIndex = Array.FindIndex(clockwiseWinding, inQuadVertexIndex => indices[startIndex + inQuadVertexIndex] == firstVertexIndex);
                if (foundIndex != -1)
                {
                    var nextLocalIndex = (foundIndex + 1) % 4;
                    if (indices[startIndex + nextLocalIndex] == secondVertexIndex)
                    {
                        return new NeighbourQuad{
                            quadIndex = quadIndex,
                            inQuadVertexIndex = foundIndex,
                        };
                    }
                }

                return null;
            }).Where(neighbour => neighbour != null)
            .FirstOrDefault();
    }

    public static IEnumerable<NeighbourQuadAndRelations> GetNeighbourQuads(int[] indices, int vertexIndex)
    {
        Assert.IsTrue(indices.Length % 4 == 0, "Indices needs to have a multiple of 4 indices in it to be treated as a quad submesh");
        var neighbourQuads = Enumerable.Range(0, indices.Length / 4).Select<int, NeighbourQuad?>(quadIndex => {
                var startIndex = quadIndex * 4;
                var foundIndex = Array.FindIndex(clockwiseWinding, inQuadVertexIndex => indices[startIndex + inQuadVertexIndex] == vertexIndex);
                if (foundIndex != -1)
                {
                    return new NeighbourQuad{
                        quadIndex = quadIndex,
                        inQuadVertexIndex = foundIndex,
                    };
                }
                else
                {
                    return null;
                }
            }).Where(neighbour => neighbour != null)
            .Select(neighbour => neighbour.Value)
            .ToArray();

        var orderedQuads = OrderQuadsAroundVertex(indices, neighbourQuads, vertexIndex);

        return orderedQuads;
    }

    private static NeighbourQuadAndRelations[] OrderQuadsAroundVertex(int[] indices, NeighbourQuad[] neighbourQuads, int vertexIndex)
    {
        Assert.IsNotNull(neighbourQuads);
        if (neighbourQuads.Length == 0)
        {
            // already trivially ordered
            return new NeighbourQuadAndRelations[0];
        }

        var result = new NeighbourQuadAndRelations[neighbourQuads.Length];

        var i = 0;
        while (i < neighbourQuads.Length)
        {
            var quadIslandStartIndex = FindFirstQuadInUnvisitedIsland(indices, neighbourQuads, result, i);

            var currentLocalQuadIndex = quadIslandStartIndex;
            do
            {
                var currentQuadNextVertex = GetOutgoingVertexIndexForQuad(indices, neighbourQuads[currentLocalQuadIndex]);
                var nextLocalQuadIndex = FindLocalQuadIndexWithIncomingVertex(indices, neighbourQuads, currentQuadNextVertex);

                result[i] = new NeighbourQuadAndRelations{
                    quad = neighbourQuads[currentLocalQuadIndex],
                    breakBetweenThisQuadAndNext = nextLocalQuadIndex == -1,
                };
                ++i;
                Assert.IsTrue(i <= neighbourQuads.Length);

                currentLocalQuadIndex = nextLocalQuadIndex;
            } while (currentLocalQuadIndex != quadIslandStartIndex && currentLocalQuadIndex != -1);
        }

        return result;
    }

    private static int FindFirstQuadInUnvisitedIsland(int[] indices, NeighbourQuad[] neighbourQuads, NeighbourQuadAndRelations[] outputQuads, int numberOfSavedQuadIndices)
    {
        var arbitraryUnvisitedQuadIndex = FindUnvisitedQuadIndex(neighbourQuads, outputQuads.Take(numberOfSavedQuadIndices));
        Assert.AreNotEqual(arbitraryUnvisitedQuadIndex, -1);
        return FindFirstQuadInIsland(indices, neighbourQuads, arbitraryUnvisitedQuadIndex);
    }

    private static int FindUnvisitedQuadIndex(NeighbourQuad[] neighbourQuads, IEnumerable<NeighbourQuadAndRelations> outputQuads)
    {
        for (var i = 0; i < neighbourQuads.Length; ++i)
        {
            if (!outputQuads.Any(quad => quad.quad.quadIndex == neighbourQuads[i].quadIndex))
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindFirstQuadInIsland(int[] indices, NeighbourQuad[] neighbourQuads, int startingQuadIndex)
    {
        var currentLocalQuadIndex = startingQuadIndex;
        do
        {
            var currentQuadNextVertex = indices[neighbourQuads[currentLocalQuadIndex].IncomingMeshIndexIndex];
            var nextLocalQuadIndex = FindLocalQuadIndexWithOutgoingVertex(indices, neighbourQuads, currentQuadNextVertex);
            if (nextLocalQuadIndex == -1)
            {
                // found start of the island
                return currentLocalQuadIndex;
            }

            currentLocalQuadIndex = nextLocalQuadIndex;
        } while (currentLocalQuadIndex != startingQuadIndex);

        // island is completely surrounded by quads
        return currentLocalQuadIndex;
    }

    private static int GetOutgoingVertexIndexForQuad(int[] indices, NeighbourQuad quad)
    {
        var outgoingVertexIndex = indices[quad.OutgoingMeshIndexIndex];
        return outgoingVertexIndex;
    }

    private static int FindLocalQuadIndexWithIncomingVertex(int[] indices, NeighbourQuad[] quads, int incomingVertexIndex)
    {
        return FindLocalQuadIndexWithNeighbourVertex(indices, quads, incomingVertexIndex, quad => quad.IncomingMeshIndexIndex);
    }

    private static int FindLocalQuadIndexWithOutgoingVertex(int[] indices, NeighbourQuad[] quads, int outgoingVertexIndex)
    {
        return FindLocalQuadIndexWithNeighbourVertex(indices, quads, outgoingVertexIndex, quad => quad.OutgoingMeshIndexIndex);
    }


    private static int FindLocalQuadIndexWithNeighbourVertex(int[] indices, NeighbourQuad[] quads, int nextVertexIndex, Func<NeighbourQuad, int> getNeighbourVertex)
    {
        return Array.FindIndex(quads, quad => {
            var meshIncomingIndexIndex = getNeighbourVertex(quad);
            var quadIncomingVertexIndex = indices[meshIncomingIndexIndex];
            return quadIncomingVertexIndex == nextVertexIndex;
        });
    }

    private static readonly int[] clockwiseWinding = new int[]{0,1,2,3};
}
}
