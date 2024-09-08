using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// This editor window will open while the user selects a VoxelGrid. It handles the cases where the user changes the
/// dimensions of the grid, maintaining the contents while changing their dimensions to match.
/// </summary>
[CustomEditor(typeof(VoxelGrid))]
internal class VoxelGridEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Undo.RecordObject(target, changeDimensionsUndoName);
        DisplayDimensionsFieldAndRecreateGridIfChanged(nameof(VoxelGrid.dimensionX), Vector3Int.right);
        DisplayDimensionsFieldAndRecreateGridIfChanged(nameof(VoxelGrid.dimensionY), Vector3Int.up);
        DisplayDimensionsFieldAndRecreateGridIfChanged(nameof(VoxelGrid.dimensionZ), Vector3Int.forward);
        DisplayBoolField(nameof(VoxelGrid.createBottomTiles));
        DisplayBoolField(nameof(VoxelGrid.createSideTiles));
        DisplayBoolField(nameof(VoxelGrid.createTopTiles));
        serializedObject.ApplyModifiedProperties();
    }

    private void DisplayDimensionsFieldAndRecreateGridIfChanged(string propertyName, Vector3Int direction)
    {
        var targetProperty = serializedObject.FindProperty(propertyName);
        int previousValue = targetProperty.intValue;
        EditorGUILayout.DelayedIntField(targetProperty);
        int newValue = targetProperty.intValue;

        if (previousValue != newValue)
        {
            Undo.RecordObject(target, "Change grid dimensions");
            var newDimensions = GetSerializedDimensions(serializedObject);
            var previousDimensions = Vector3Int.Scale(newDimensions, Vector3Int.one - direction) + direction * previousValue;
            SafeResizeArray(previousDimensions, serializedObject);
        }
    }

    private void DisplayBoolField(string propertyName)
    {
        var targetProperty = serializedObject.FindProperty(propertyName);
        EditorGUILayout.PropertyField(targetProperty);
    }

    private static Vector3Int GetSerializedDimensions(SerializedObject targetObject)
    {
        return new Vector3Int(
            targetObject.FindProperty(nameof(VoxelGrid.dimensionX)).intValue,
            targetObject.FindProperty(nameof(VoxelGrid.dimensionY)).intValue,
            targetObject.FindProperty(nameof(VoxelGrid.dimensionZ)).intValue
        );
    }

    private static void SafeResizeArray(Vector3Int previousDimensions, SerializedObject targetObject)
    {
        if (IsExpectedPreviousSize(previousDimensions, targetObject))
        {
            CopyResizeArray(previousDimensions, targetObject);
        }
        else
        {
            Debug.LogError($"Emergency clean up of voxel grid, when changing dimensions it was not the expected previous size of {previousDimensions} and was actually {GetSerializedDimensions(targetObject)}");
            PopulateWithCleanArray(targetObject);
        }
    }

    private static bool IsExpectedPreviousSize(Vector3Int previousDimensions, SerializedObject existingObject)
    {
        var expectedArraySize = previousDimensions.GetFlatArraySize();
        return existingObject.FindProperty(VoxelGrid.voxelDataName).arraySize == expectedArraySize;
    }

    private static void CopyResizeArray(Vector3Int previousDimensions, SerializedObject targetObject)
    {
        // Cannot place directly into the serializedObject, we'd be overwriting our source data.
        // Store in this cache first and apply to the serializedObject when we've finished reading from the source.
        var newDimensions = GetSerializedDimensions(targetObject);
        var newDataCached = new int[newDimensions.GetFlatArraySize()];

        var copyDimensions = new Vector3Int(
            Math.Min(previousDimensions.x, newDimensions.x),
            Math.Min(previousDimensions.y, newDimensions.y),
            Math.Min(previousDimensions.z, newDimensions.z)
        );

        var arrayRootProperty = targetObject.FindProperty(VoxelGrid.voxelDataName);

        for (var x = 0; x < copyDimensions.x; ++x)
        {
            for (var y = 0; y < copyDimensions.y; ++y)
            {
                for (var z = 0; z < copyDimensions.z; ++z)
                {
                    var gridIndex = new Vector3Int(x,y,z);
                    newDataCached[gridIndex.ToFlatArrayIndex(newDimensions)] =
                        arrayRootProperty.GetArrayElementAtIndex(gridIndex.ToFlatArrayIndex(previousDimensions)).intValue;
                }
            }
        }

        arrayRootProperty.arraySize = newDimensions.GetFlatArraySize();

        for (var x = 0; x < newDimensions.x; ++x)
        {
            for (var y = 0; y < newDimensions.y; ++y)
            {
                for (var z = 0; z < newDimensions.z; ++z)
                {
                    var gridIndex = new Vector3Int(x,y,z);
                    var arrayIndex = gridIndex.ToFlatArrayIndex(newDimensions);
                    arrayRootProperty.GetArrayElementAtIndex(arrayIndex).intValue = newDataCached[arrayIndex];
                }
            }
        }
    }

    private static void PopulateWithCleanArray(SerializedObject targetObject)
    {
        var newDimensions = new Vector3Int(
            targetObject.FindProperty(nameof(VoxelGrid.dimensionX)).intValue,
            targetObject.FindProperty(nameof(VoxelGrid.dimensionY)).intValue,
            targetObject.FindProperty(nameof(VoxelGrid.dimensionZ)).intValue
        );
        var arrayRootProperty = targetObject.FindProperty(VoxelGrid.voxelDataName);
        arrayRootProperty.arraySize = newDimensions.GetFlatArraySize();
    }

    private const string changeDimensionsUndoName = "Change voxel grid dimensions";
}

}
