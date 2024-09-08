using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Profiling;

using CarbideFunction.Wildtile.Postprocessing;

namespace CarbideFunction.Wildtile.WelderPostprocessing
{

/// <summary>
/// Postprocessor that creates a single mesh containing all models of tiles in the tileset, including welding vertices that are close to one another.
///
/// This is more expensive to generate, but cheaper to render.
/// </summary>
public class WelderPostprocessor : Postprocessor
{
    [SerializeField]
    internal List<Material> manifoldMaterials = null;

    [SerializeField]
    internal float weldToleranceSquare = 0f;

    private class AccumulatingMesh
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector4> tangents = new List<Vector4>();
        public List<Vector2> uv0 = new List<Vector2>();

        public Dictionary<Material, AccumulatingSubMesh> subMeshes = new Dictionary<Material, AccumulatingSubMesh>();
    }

    private class AccumulatingSubMesh
    {
        public List<int> triIndices = new List<int>();
    }

    public override IEnumerable<int> Postprocess(GameObject root, PostprocessableMap map, Vector3 tileDimensions)
    {
        var inverseTileDimensions = new Vector3(1f / tileDimensions.x, 1f / tileDimensions.y, 1f / tileDimensions.z);

        postprocessorPerfMarker.Begin();
        var accumulatingMesh = new AccumulatingMesh();

        foreach (var slot in map.slots)
        {
            var warper = new VertexWarper(
                slot.v000, slot.v001,
                slot.v010, slot.v011,
                slot.v100, slot.v101,
                slot.v110, slot.v111
            );

            CollectGeometry(warper, slot.normalWarper, slot.flipIndices, slot.mesh, accumulatingMesh);

            postprocessorPerfMarker.End();
            yield return 0;
            postprocessorPerfMarker.Begin();

            if (slot.prefab != null)
            {
                var instance = GameObject.Instantiate(slot.prefab, root.transform);
                WarpDirectChildrenPositions(warper, slot.normalWarper, instance.transform, tileDimensions, inverseTileDimensions);
            }
        }

        foreach (var yieldResult in WeldGeometry(accumulatingMesh))
        {
            postprocessorPerfMarker.End();
            yield return yieldResult;
            postprocessorPerfMarker.Begin();
        }

        var unityMesh = CreateMesh(accumulatingMesh);
        postprocessorPerfMarker.End();
        yield return 0;
        postprocessorPerfMarker.Begin();
        Mesh physicsMesh = null;
        foreach (var meshBuilderResult in CreatePhysicsMesh(accumulatingMesh, manifoldMaterials, newMesh => physicsMesh = newMesh))
        {
            postprocessorPerfMarker.End();
            yield return 0;
            postprocessorPerfMarker.Begin();
        }

        var meshGameObject = new GameObject("Wildtile welded mesh");
        meshGameObject.transform.SetParent(root.transform, worldPositionStays:false);

        var meshFilter = meshGameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = unityMesh;

        var meshRenderer = meshGameObject.AddComponent<MeshRenderer>();
        meshRenderer.materials = accumulatingMesh.subMeshes.Select(materialSubMeshPair => materialSubMeshPair.Key).ToArray();

        var meshCollider = meshGameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = physicsMesh;
        postprocessorPerfMarker.End();
    }

    private void CollectGeometry(VertexWarper vertexWarper, NormalWarper normalWarper, bool shouldFlipWinding, ModuleMesh mesh, AccumulatingMesh outputMesh)
    {
        if (mesh != null)
        {
            var tempUvs = new List<Vector2>();

            // copy mesh verts over
            var vertexIndexStart = outputMesh.vertices.Count;
            Func<IEnumerable<int>, IEnumerable<int>> vertexIndexBiaser = indices => indices.Select(index => index + vertexIndexStart);

            outputMesh.vertices.AddRange(mesh.vertices.Select(position => vertexWarper.WarpPosition(position)));
            Assert.AreEqual(mesh.normals.Length, mesh.vertices.Length);
            Assert.AreEqual(mesh.tangents.Length, mesh.vertices.Length);
            outputMesh.normals.AddRange(Enumerable.Range(0, mesh.normals.Length).Select(vertexIndex => normalWarper.WarpNormal(mesh.vertices[vertexIndex], mesh.normals[vertexIndex])));
            outputMesh.tangents.AddRange(Enumerable.Range(0, mesh.tangents.Length).Select(vertexIndex => (Vector4)(normalWarper.WarpNormal(mesh.vertices[vertexIndex], mesh.tangents[vertexIndex]))));

            if (mesh.uvs.Length > 0)
            {
                Assert.AreEqual(mesh.uvs[0].fullWidthChannel.Count, mesh.vertices.Length);
                outputMesh.uv0.AddRange(mesh.uvs[0].fullWidthChannel.Select(vec4 => new Vector2(vec4.x, vec4.y)));
            }
            else
            {
                outputMesh.uv0.AddRange(Enumerable.Repeat(Vector2.zero, mesh.vertices.Length));
            }

            // copy sub mesh indices over
            for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshes.Length; ++subMeshIndex)
            {
                var subMesh = mesh.subMeshes[subMeshIndex];
                var material = subMesh.material;
                var accumulatingSubMesh = EnsureAndGetAccumulatingSubMesh(outputMesh.subMeshes, material);

                if (shouldFlipWinding)
                {
                    var flippedTriIndices = Enumerable
                        .Range(0, subMesh.indicesCount / 3)
                        .SelectMany(triIndex => {
                            var triangleStartIndex = triIndex * 3;
                            return new[]{
                                mesh.triangles[subMesh.startIndex + triangleStartIndex],
                                mesh.triangles[subMesh.startIndex + triangleStartIndex+2],
                                mesh.triangles[subMesh.startIndex + triangleStartIndex+1],
                            };
                        });
                    accumulatingSubMesh.triIndices.AddRange(vertexIndexBiaser(flippedTriIndices));
                }
                else
                {
                    var triIndices = mesh.triangles.Skip(subMesh.startIndex).Take(subMesh.indicesCount);
                    accumulatingSubMesh.triIndices.AddRange(vertexIndexBiaser(triIndices));
                }
            }
        }
    }

    private bool IsMatrixFlipped(Matrix4x4 matrix)
    {
        return matrix.determinant < 0;
    }

    private Material SafeGetMaterial(MeshRenderer renderer, int index)
    {
        var mats = renderer.sharedMaterials;
        if (index >= 0 && index < mats.GetLength(0))
        {
            return mats[index];
        }
        else
        {
            return null;
        }
    }

    private AccumulatingSubMesh EnsureAndGetAccumulatingSubMesh(Dictionary<Material, AccumulatingSubMesh> subMeshes, Material material)
    {
        if (subMeshes.TryGetValue(material, out var existingSubMesh))
        {
            return existingSubMesh;
        }
        else
        {
            var newSubMesh = new AccumulatingSubMesh();
            subMeshes.Add(material, newSubMesh);
            return newSubMesh;
        }
    }

    private Mesh CreateMesh(AccumulatingMesh meshData)
    {
        buildRenderMeshPerfMarker.Begin();
        var mesh = new Mesh();
        mesh.vertices = meshData.vertices.ToArray();
        mesh.normals = meshData.normals.ToArray();
        mesh.tangents = meshData.tangents.ToArray();
        mesh.SetUVs(0, meshData.uv0.ToArray());

        mesh.subMeshCount = meshData.subMeshes.Count;
        for (var subMeshIndex = 0; subMeshIndex < meshData.subMeshes.Count; ++subMeshIndex)
        {
            mesh.SetIndices(meshData.subMeshes.ElementAt(subMeshIndex).Value.triIndices.ToArray(), MeshTopology.Triangles, subMeshIndex);
        }

        buildRenderMeshPerfMarker.End();
        return mesh;
    }

    private delegate void OnMeshCreated(Mesh mesh);
    private IEnumerable<int> CreatePhysicsMesh(AccumulatingMesh meshData, List<Material> manifoldMaterials, OnMeshCreated onMeshCreated)
    {
        buildPhysicsMeshPerfMarker.Begin();
        var mesh = new Mesh();

        mesh.subMeshCount = 1;
        var subMeshArray = meshData.subMeshes.ToList();

        var vertices = new List<Vector3>();
        var indices = new List<int>();

        if (manifoldMaterials.Count != 0)
        {
            foreach (var material in manifoldMaterials)
            {
                var sourceSubMeshIndex = subMeshArray.FindIndex(subMeshPair => subMeshPair.Key == material);
                if (sourceSubMeshIndex != -1)
                {
                    var subMesh = subMeshArray[sourceSubMeshIndex].Value;
                    foreach (var yieldResult in AddVerticesToPhysicsMesh(subMesh.triIndices, meshData.vertices, vertices, indices))
                    {
                        buildPhysicsMeshPerfMarker.End();
                        yield return yieldResult;
                        buildPhysicsMeshPerfMarker.Begin();
                    }
                }
            }
        }
        else
        {
            foreach (var subMeshPair in subMeshArray)
            {
                var subMesh = subMeshPair.Value;
                foreach (var yieldResult in AddVerticesToPhysicsMesh(subMesh.triIndices, meshData.vertices, vertices, indices))
                {
                    buildPhysicsMeshPerfMarker.End();
                    yield return yieldResult;
                    buildPhysicsMeshPerfMarker.Begin();
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);

        onMeshCreated(mesh);
        buildPhysicsMeshPerfMarker.End();
    }

    private IEnumerable<int> AddVerticesToPhysicsMesh(IEnumerable<int> triIndices, IList<Vector3> sourceVertices, List<Vector3> inOutVertices, List<int> inOutIndices)
    {
        foreach (var index in triIndices)
        {
            var vertexValue = sourceVertices[index];
            var closeExistingVertexIndex = FindCloseVertexIndex(inOutVertices, vertexValue, weldToleranceSquare);
            if (closeExistingVertexIndex != -1)
            {
                inOutIndices.Add(closeExistingVertexIndex);
            }
            else
            {
                inOutVertices.Add(vertexValue);
                inOutIndices.Add(inOutVertices.Count - 1);
            }

            yield return 0;
        }
    }

    static private int FindCloseVertexIndex(List<Vector3> vertices, Vector3 targetVertex, float toleranceSquared)
    {
        return vertices.FindLastIndex(vertex => (vertex - targetVertex).sqrMagnitude < toleranceSquared);
    }

    private IEnumerable<int> WeldGeometry(AccumulatingMesh mesh)
    {
        weldPerfMarker.Begin();
        foreach (var vertexIndex in Enumerable.Range(0, mesh.vertices.Count))
        {
            var position = mesh.vertices[vertexIndex];
            foreach (var searchVertexIndex in Enumerable.Range(0, vertexIndex))
            {
                var searchPosition = mesh.vertices[searchVertexIndex];

                if ((searchPosition - position).sqrMagnitude < weldToleranceSquare)
                {
                    mesh.vertices[vertexIndex] = searchPosition;
                }
            }

            weldPerfMarker.End();
            yield return 0;
            weldPerfMarker.Begin();
        }
        weldPerfMarker.End();
    }

    static readonly ProfilerMarker postprocessorPerfMarker = new ProfilerMarker("CarbideFunction.Wildtile.WelderPostprocessor");
    static readonly ProfilerMarker weldPerfMarker = new ProfilerMarker("CarbideFunction.Wildtile.WelderPostprocessor.Weld");
    static readonly ProfilerMarker buildRenderMeshPerfMarker = new ProfilerMarker("CarbideFunction.Wildtile.WelderPostprocessor.BuildRenderMesh");
    static readonly ProfilerMarker buildPhysicsMeshPerfMarker = new ProfilerMarker("CarbideFunction.Wildtile.WelderPostprocessor.BuildPhysicsMesh");

    public override UnityEngine.Object[] CloneAndReferenceTransientObjects(GameObject rootOfAlreadyPostprocessedWorld)
    {
        var meshFilter = rootOfAlreadyPostprocessedWorld.GetComponentInChildren<MeshFilter>();
        Assert.IsNotNull(meshFilter);
        var clonedMesh = Mesh.Instantiate(meshFilter.sharedMesh);
        meshFilter.sharedMesh = clonedMesh;
        return new UnityEngine.Object[]{meshFilter.sharedMesh};
    }

    private void WarpDirectChildrenPositions(VertexWarper positionWarper, NormalWarper normalWarper, Transform rootTransform, Vector3 tileDimensions, Vector3 inverseTileDimensions)
    {
        foreach (Transform child in rootTransform)
        {
            // make sure to fully read input values before writing to any of the values
            // i.e. do not write the local position immediately after getting it
            var unitCubePosition = Vector3.Scale(child.localPosition, inverseTileDimensions);
            var mapSpacePosition = positionWarper.WarpPosition(unitCubePosition);

            var unitCubeForward = Vector3.Scale(child.localRotation * Vector3.forward, tileDimensions);
            var unitCubeUp = Vector3.Scale(child.localRotation * Vector3.up, tileDimensions);
            var forwardFacing = normalWarper.WarpNormal(unitCubePosition, unitCubeForward);
            var upFacing = normalWarper.WarpNormal(unitCubePosition, unitCubeUp);

            child.localPosition = mapSpacePosition;
            child.localRotation = Quaternion.LookRotation(forwardFacing, upFacing);;
        }
    }
}

}
