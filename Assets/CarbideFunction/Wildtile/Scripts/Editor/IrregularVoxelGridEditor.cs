using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using CarbideFunction.Wildtile;
using CarbideFunction.Wildtile.Sylves;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// This editor window will open while the user selects a VoxelGrid. It handles the cases where the user changes the
/// dimensions of the grid, maintaining the contents while changing their dimensions to match.
/// </summary>
[CustomEditor(typeof(IrregularVoxelGrid))]
internal class IrregularVoxelGridEditor : UnityEditor.Editor
{
    [SerializeField] private bool foldedOut = false;
    [SerializeField] private int startingHexDimension = 6;
    [SerializeField] private float relaxStrength = 1e-3f;
    [SerializeField] private int relaxIterations = 10;

    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();
        serializedObject.Update();

        foldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(foldedOut, "Generate Grid");
        if (foldedOut)
        {
            startingHexDimension = EditorGUILayout.IntField("Number of Hexes Along Border", startingHexDimension);
            relaxStrength = EditorGUILayout.FloatField("Relax Strength", relaxStrength);
            relaxIterations = EditorGUILayout.IntField("Relax Iterations", relaxIterations);

            if (GUILayout.Button("Populate Grid"))
            {
                GenerateAndSaveGrid();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void GenerateAndSaveGrid()
    {
        var meshData = GenerateTownscaperCell();
        var verticesProperty = serializedObject.FindProperty(nameof(IrregularVoxelGrid.vertices));
        WriteArrayToSerializedProperty.Vector3Array(meshData.vertices, verticesProperty);
        Assert.AreEqual(meshData.indices.Length, 1);
        var indices = meshData.indices[0];
        WriteArrayToSerializedProperty.IntArray(indices.Reverse().ToList(), serializedObject.FindProperty(nameof(IrregularVoxelGrid.indices)));

        var height = serializedObject.FindProperty(nameof(IrregularVoxelGrid.height)).intValue;
        var voxelCount = meshData.vertices.Length * height;

        WriteArrayToSerializedProperty.IntArray(new int[voxelCount], serializedObject.FindProperty(nameof(IrregularVoxelGrid.voxelContents)));
    }

    private Sylves.MeshData GenerateTownscaperCell()
    {
        var triangleGrid = new TriangleGrid(1f, Sylves.TriangleOrientation.FlatSides, bound: Sylves.TriangleBound.Hexagon(startingHexDimension));
        var meshData = triangleGrid.ToMeshData(triangleGrid.GetCells());

        meshData = meshData.RandomPairing();

        meshData = ConwayOperators.Ortho(meshData);

        meshData = meshData.Weld(tolerance:1e-1f);

        TownscaperRelaxer.Relax(meshData, relaxIterations, relaxStrength);

        meshData = Matrix4x4.Rotate(Quaternion.Euler(-90f, 0f, 0f)) * meshData;

        meshData = ScaleToAverageSizeOne(meshData);

        return meshData;
    }

    private static Sylves.MeshData ScaleToAverageSizeOne(Sylves.MeshData meshData)
    {
        var averageSize = CalculateAverageSize(meshData);

        var scaleToReachSizeOne = 1f / Mathf.Sqrt(averageSize);

        return Matrix4x4.Scale(Vector3.one * scaleToReachSizeOne) * meshData;
    }

    private static float CalculateAverageSize(Sylves.MeshData meshData)
    {
        var totalSize = 0f;

        var indices = meshData.indices[0];
        for (var faceStartIndex = 0; faceStartIndex < indices.Length; faceStartIndex += 4)
        {
            var vertex0 = meshData.vertices[indices[faceStartIndex    ]];
            var vertex1 = meshData.vertices[indices[faceStartIndex + 1]];
            var vertex2 = meshData.vertices[indices[faceStartIndex + 2]];
            var vertex3 = meshData.vertices[indices[faceStartIndex + 3]];

            var side0 = (vertex1 - vertex0);
            var side1 = (vertex2 - vertex1);
            var side2 = (vertex3 - vertex2);
            var side3 = (vertex0 - vertex3);

            var length0 = side0.magnitude;
            var length1 = side1.magnitude;
            var length2 = side2.magnitude;
            var length3 = side3.magnitude;

            var dir01 = side0 / length0;
            var dir12 = side1 / length1;
            var dir23 = side2 / length2;
            var dir30 = side3 / length3;

            var perimiter = length0 + length1 + length2 + length3;
            var semiPerimiter = perimiter * 0.5f;

            var semiPerimiterMinusLengthsProduct = 
                (semiPerimiter - length0) *
                (semiPerimiter - length1) *
                (semiPerimiter - length2) *
                (semiPerimiter - length3);

            var lengthsProduct = length0 * length1 * length2 * length3;

            var cosSquaredTheta = CalculateCosSquaredTheta(dir01, dir12, dir23, dir30);

            var faceSize = Mathf.Sqrt(semiPerimiterMinusLengthsProduct - lengthsProduct * cosSquaredTheta);

            totalSize += faceSize;
        }
        var averageSize = totalSize / (indices.Length / 4);

        return averageSize;
    }

    private static float CalculateTheta(Vector3 inDir, Vector3 outDir)
    {
        var cross = Vector3.Cross(inDir, outDir);
        var theta = Mathf.Asin(cross.y);
        return theta;
    }

    private static float CalculateCosSquaredTheta
    (
        Vector3 dir01,
        Vector3 dir12,
        Vector3 dir23,
        Vector3 dir30
    )
    {
        var theta301 = CalculateTheta(dir30, dir01);
        var theta123 = CalculateTheta(dir12, dir23);

        var cosAdd = Mathf.Cos((theta301 + theta123) * 0.5f);

        return cosAdd * cosAdd;
    }
}

}
