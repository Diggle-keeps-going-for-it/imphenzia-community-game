using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// Custom editor for <see cref="DualGridVoxelMeshBackupTiles"/>.
///
/// Provides the ability to generate and populate the backup model game objects.
/// </summary>
[CustomEditor(typeof(DualGridVoxelMeshBackupTiles))]
internal class DualGridVoxelMeshBackupTilesEditor : UnityEditor.Editor
{
    [SerializeField]
    private Material wildcardFaceMaterial = null;
    [SerializeField]
    private Material wildcardEmptyMaterial = null;
    [SerializeField]
    private Material wildcardFilledMaterial = null;

    private const float modelScale = 0.9f;

    private const string undoGenerateName = "Generate default backup tiles models";
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        if (GUILayout.Button("Generate default models"))
        {
            GenerateAndSaveDefaultModelsToAsset();
        }
    }

    private void GenerateAndSaveDefaultModelsToAsset()
    {
        Undo.RecordObject(target, undoGenerateName);

        var destinationArray = serializedObject.FindProperty(DualGridVoxelMeshBackupTiles.tilesName);
        destinationArray.arraySize = 0x100;

        for (var cubeConfig = 0; cubeConfig < 0x100; ++cubeConfig)
        {
            var mesh = GenerateMesh(cubeConfig, wildcardFaceMaterial, wildcardEmptyMaterial, wildcardFilledMaterial);
            SaveMeshToArrayProperty(destinationArray, cubeConfig, mesh);
        }

        serializedObject.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
    }

    internal static void SaveMeshToArrayProperty(SerializedProperty destinationArray, int arrayIndex, ModuleMesh mesh)
    {
        Assert.IsTrue(destinationArray.arraySize > arrayIndex);
        var destinationElement = destinationArray.GetArrayElementAtIndex(arrayIndex);
        ModuleMeshSerializer.SaveMeshToProperty(destinationElement, mesh);
    }

    public static void StorePrefabInSerializedPropertyArray(SerializedProperty destinationArray, int arrayIndex, GameObject prefab)
    {
        Assert.IsTrue(destinationArray.arraySize > arrayIndex);
        var destinationElement = destinationArray.GetArrayElementAtIndex(arrayIndex);
        destinationElement.objectReferenceValue = prefab;
    }

    public static ModuleMesh GenerateMesh(int cubeConfig, Material faceMaterial, Material emptyMaterial, Material filledMaterial)
    {
        Assert.IsTrue(cubeConfig >= 0);
        Assert.IsTrue(cubeConfig <= 0xFF);

        var mesh = new ModuleMesh();

        // the can use one of 3 materials:
        //  surface plane (conventional rendering)
        //  empty lines
        //  filled lines
        mesh.subMeshes = Enumerable.Range(0, 3).Select(_ => new ModuleMesh.SubMesh()).ToArray();

        if (cubeConfig == 0xFF)
        {
            PopulateFilledDualVoxel(mesh, filledMaterial);
        }
        else if (cubeConfig == 0x00)
        {
            PopulateEmptyDualVoxel(mesh, emptyMaterial);
        }
        else
        {
            PopulatePartialDualVoxel(mesh, cubeConfig, faceMaterial);
        }

        return mesh;
    }

    private const int surfaceSubmeshIndex = 0;
    private const int emptyLinesSubmeshIndex = 1;
    private const int filledLinesSubmeshIndex = 2;

    private static void PopulateFilledDualVoxel(ModuleMesh mesh, Material material)
    {
        GenerateCrossedLines(mesh, filledLinesSubmeshIndex, material);
    }

    private static void PopulateEmptyDualVoxel(ModuleMesh mesh, Material material)
    {
        GenerateCrossedLines(mesh, emptyLinesSubmeshIndex, material);
    }

    private static readonly int[] crossedLinesIndices = GenerateCrossedLinesIndices();
    private static int[] GenerateCrossedLinesIndices()
    {
        return Enumerable.Range(0, 4).SelectMany(lineIndex =>
            Enumerable.Range(0, 3).SelectMany(quadIndex => 
                Enumerable.Range(0, 4).Select(vertIndex => 
                    lineIndex * 8 + quadIndex * 2 + vertIndex
                )
            )
        ).ToArray();
    }

    private static void GenerateCrossedLines(ModuleMesh mesh, int submeshIndex, Material material)
    {
        mesh.vertices = new []{
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f, -0.5f,  0.5f),
        }.Select(vertex => vertex * modelScale).SelectMany(vertex => Enumerable.Repeat(vertex, 4)).ToArray();

        // these are the pseudo positions on the line
        mesh.uvs = new ModuleMesh.UvChannel[]{
            new ModuleMesh.UvChannel{
                fullWidthChannel = Enumerable.Repeat(new []{
                    new Vector4(0f, 0f, 0f, 0f),
                    new Vector4(0f, 0f, 1f, 0f),
                    new Vector4(0f, 1f, 1f, 0f),
                    new Vector4(0f, 1f, 0f, 0f),

                    new Vector4(1f, 0f, 0f, 0f),
                    new Vector4(1f, 0f, 1f, 0f),
                    new Vector4(1f, 1f, 1f, 0f),
                    new Vector4(1f, 1f, 0f, 0f),
                }, 4).SelectMany(uvs => uvs).ToList(),
                channelWidth = 3,
            },
            new ModuleMesh.UvChannel{
                fullWidthChannel = new []{
                    new Vector4( 1f,  1f, 1f, 0f),
                    new Vector4(-1f,  1f, 1f, 0f),
                    new Vector4( 1f, -1f, 1f, 0f),
                    new Vector4(-1f, -1f, 1f, 0f),
                }.SelectMany(uvs => Enumerable.Repeat(uvs, 8)).ToList(),
                channelWidth = 3,
            },
        };

        mesh.normals = mesh.vertices.Select(_ => Vector3.zero).ToArray();
        mesh.tangents = mesh.vertices.Select(_ => Vector4.zero).ToArray();

        mesh.triangles = crossedLinesIndices;

        mesh.subMeshes[submeshIndex].startIndex = 0;
        mesh.subMeshes[submeshIndex].indicesCount = mesh.triangles.Length;
        mesh.subMeshes[submeshIndex].material = material;
    }

    private static int ContentsIndex(int x, int y, int z)
    {
        return new Vector3Int(x,y,z).ToFlatArrayIndex(new Vector3Int(2,2,2));
    }

    private static void AddFaceAlongEdgeIfContentsChange(List<Vector3> verts, List<Vector3> normals, int u, int v, Vector3 uPositiveDirection, Vector3 vPositiveDirection, bool contentsLow, bool contentsHigh)
    {
        if (contentsLow == contentsHigh)
        {
            // no change, so no face is needed
            return;
        }

        var bottomLeft =  ((u-1) * uPositiveDirection + (v-1) * vPositiveDirection) * 0.5f * modelScale;
        var bottomRight = ((u  ) * uPositiveDirection + (v-1) * vPositiveDirection) * 0.5f * modelScale;
        var topLeft =     ((u-1) * uPositiveDirection + (v  ) * vPositiveDirection) * 0.5f * modelScale;
        var topRight =    ((u  ) * uPositiveDirection + (v  ) * vPositiveDirection) * 0.5f * modelScale;
        
        if (contentsLow && !contentsHigh)
        {
            Assert.IsFalse(contentsHigh, "If the values were the same the function should have exited out early");

            verts.AddRange(new []{bottomLeft, bottomRight, topRight, topLeft});
            var normal = Vector3.Cross(uPositiveDirection, vPositiveDirection).normalized;
            normals.AddRange(Enumerable.Range(0,4).Select(dummy => normal));
        }
        else
        {
            Assert.IsFalse(contentsLow, "If the values were the same the function should have exited out early");
            Assert.IsTrue(contentsHigh, "If the values were the same the function should have exited out early");

            verts.AddRange(new []{bottomLeft, topLeft, topRight, bottomRight});
            var normal = -Vector3.Cross(uPositiveDirection, vPositiveDirection).normalized;
            normals.AddRange(Enumerable.Range(0,4).Select(dummy => normal));
        }
    }

    private static bool IsCubeCornerFilled(int cubeConfig, int x, int y, int z)
    {
        return (cubeConfig & (1 << ContentsIndex(x,y,z))) != 0;
    }

    private static void PopulatePartialDualVoxel(ModuleMesh mesh, int cubeConfig, Material material)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();

        for (var x = 0; x < 2; ++x)
        {
            for (var y = 0; y < 2; ++y)
            {
                var contentsLow = IsCubeCornerFilled(cubeConfig, x,y,0);
                var contentsHigh = IsCubeCornerFilled(cubeConfig, x,y,1);

                AddFaceAlongEdgeIfContentsChange(vertices, normals, x, y, Vector3.right, Vector3.up, contentsLow, contentsHigh);
            }
        }

        for (var x = 0; x < 2; ++x)
        {
            for (var z = 0; z < 2; ++z)
            {
                var contentsLow = IsCubeCornerFilled(cubeConfig, x,0,z);
                var contentsHigh = IsCubeCornerFilled(cubeConfig, x,1,z);

                AddFaceAlongEdgeIfContentsChange(vertices, normals, z, x, Vector3.forward, Vector3.right, contentsLow, contentsHigh);
            }
        }

        for (var y = 0; y < 2; ++y)
        {
            for (var z = 0; z < 2; ++z)
            {
                var contentsLow = IsCubeCornerFilled(cubeConfig, 0,y,z);
                var contentsHigh = IsCubeCornerFilled(cubeConfig, 1,y,z);

                AddFaceAlongEdgeIfContentsChange(vertices, normals, y, z, Vector3.up, Vector3.forward, contentsLow, contentsHigh);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.tangents = normals.Select(_ => new Vector4(1f, 0f, 0f, 0f)).ToArray();
        mesh.uvs = new ModuleMesh.UvChannel[]{
            new ModuleMesh.UvChannel{
                fullWidthChannel = Enumerable.Range(0, vertices.Count / 4).SelectMany(quadIndex => new []{
                    new Vector4(0f, 0f, 0f, 0f),
                    new Vector4(0f, 1f, 0f, 0f),
                    new Vector4(1f, 1f, 0f, 0f),
                    new Vector4(1f, 0f, 0f, 0f),
                }).ToList(),
                channelWidth = 2,
            },
        };
        mesh.triangles = Enumerable.Range(0, vertices.Count / 4).SelectMany(quadIndex => quadTriangles.Select(inQuadIndex => quadIndex * 4 + inQuadIndex)).ToArray();

        mesh.subMeshes[surfaceSubmeshIndex].startIndex = 0;
        mesh.subMeshes[surfaceSubmeshIndex].indicesCount = mesh.triangles.Length;
        mesh.subMeshes[surfaceSubmeshIndex].material = material;
    }

    static readonly int[] quadTriangles = new []{0,1,2, 0,2,3};
}

}
