using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using CarbideFunction.Wildtile;

[Serializable]
public class MouseFaceHighlighter
{
    [SerializeField]
    private Transform highlightRoot = null;

    [SerializeField]
    private MeshFilter normalModel = null;
    [SerializeField]
    private MeshFilter crossModel = null;

    public bool valid => highlightRoot != null;

    public void HighlightFace(VoxelMesher.FaceData? currentFace, bool canBeExtruded)
    {
        if (currentFace.HasValue)
        {
            var face = currentFace.Value;
            highlightRoot.gameObject.SetActive(true);

            var mesh = ConstructMesh(face.faceVertices, face.faceIndices);

            normalModel.gameObject.SetActive(canBeExtruded);
            normalModel.sharedMesh = mesh;
            crossModel.gameObject.SetActive(!canBeExtruded);
            crossModel.sharedMesh = mesh;
        }
        else
        {
            highlightRoot.gameObject.SetActive(false);
        }
    }

    private Mesh ConstructMesh(VoxelMesher.Vertex[] vertices, int[] indices)
    {
        var mesh = new Mesh();

        mesh.vertices = vertices.Select(vertex => vertex.position).ToArray();
        mesh.normals = vertices.Select(vertex => vertex.normal).ToArray();
        mesh.uv = vertices.Select(vertex => vertex.uv).ToArray();
        mesh.triangles = indices;
        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor{
            indexStart = 0,
            indexCount = indices.Length
        }, MeshUpdateFlags.Default);

        return mesh;
    }

    public void DisableHighlight()
    {
        if (highlightRoot != null && highlightRoot.gameObject != null)
        {
            highlightRoot.gameObject.SetActive(false);
        }
    }
}
