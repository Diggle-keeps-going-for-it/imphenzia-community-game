using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    internal static class ModuleMeshSerializer
    {
        internal static void SaveMeshToProperty(SerializedProperty property, ModuleMesh mesh)
        {
            SaveVertices(property, mesh);
            SaveNormals(property, mesh);
            SaveTangents(property, mesh);
            SaveUvs(property, mesh);
            SaveTriangles(property, mesh);
            SaveSubMeshes(property, mesh);
            // TODO save all mesh properties
        }

        private static void SaveVertices(SerializedProperty meshProperty, ModuleMesh mesh)
        {
            var verticesProperty = meshProperty.FindPropertyRelative(nameof(ModuleMesh.vertices));
            WriteArrayToSerializedProperty.Vector3Array(mesh.vertices, verticesProperty);
        }

        private static void SaveNormals(SerializedProperty meshProperty, ModuleMesh mesh)
        {
            if (mesh.normals != null)
            {
                var normalsProperty = meshProperty.FindPropertyRelative(nameof(ModuleMesh.normals));
                WriteArrayToSerializedProperty.Vector3Array(mesh.normals, normalsProperty);
            }
        }

        private static void SaveTangents(SerializedProperty meshProperty, ModuleMesh mesh)
        {
            if (mesh.tangents != null)
            {
                var tangentsProperty = meshProperty.FindPropertyRelative(nameof(ModuleMesh.tangents));
                WriteArrayToSerializedProperty.Vector4Array(mesh.tangents, tangentsProperty);
            }
        }

        private static void SaveUvs(SerializedProperty meshProperty, ModuleMesh mesh)
        {
            var uvsProperty = meshProperty.FindPropertyRelative(nameof(ModuleMesh.uvs));
            WriteArrayToSerializedProperty.GenericArray(mesh.uvs, uvsProperty, 
                (value, prop) => {
                    var fullWidthChannelProperty = prop.FindPropertyRelative(nameof(ModuleMesh.UvChannel.fullWidthChannel));
                    WriteArrayToSerializedProperty.Vector4Array(value.fullWidthChannel, fullWidthChannelProperty);

                    prop.FindPropertyRelative(nameof(ModuleMesh.UvChannel.channelWidth)).intValue = value.channelWidth;
                });
        }

        private static void SaveTriangles(SerializedProperty meshProperty, ModuleMesh mesh)
        {
            var trianglesProperty = meshProperty.FindPropertyRelative(nameof(ModuleMesh.triangles));
            WriteArrayToSerializedProperty.IntArray(mesh.triangles, trianglesProperty);
        }

        private static void SaveSubMeshes(SerializedProperty meshProperty, ModuleMesh mesh)
        {
            var subMeshesProperty = meshProperty.FindPropertyRelative(nameof(ModuleMesh.subMeshes));
            WriteArrayToSerializedProperty.GenericArray(mesh.subMeshes, subMeshesProperty, 
                (subMesh, subMeshProp) => {
                    Assert.IsNotNull(subMeshProp);
                    Assert.IsNotNull(subMesh);
                    subMeshProp.FindPropertyRelative(nameof(ModuleMesh.SubMesh.startIndex)).intValue = subMesh.startIndex;
                    subMeshProp.FindPropertyRelative(nameof(ModuleMesh.SubMesh.indicesCount)).intValue = subMesh.indicesCount;
                    subMeshProp.FindPropertyRelative(nameof(ModuleMesh.SubMesh.material)).objectReferenceValue = subMesh.material;
                });
        }

    }
}
