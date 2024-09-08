using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace CarbideFunction.Wildtile
{
    /// <summary>
    /// Contains methods for creating Unity Meshes from <see cref="ModuleMesh">ModuleMeshes</see>.
    /// </summary>
    public static class ModuleMeshInstantiator
    {
        /// <summary>
        /// Create a Unity Mesh from a Wildtile <see cref="ModuleMesh"/>.
        /// </summary>
        public static Mesh InstantiateMesh(ModuleMesh moduleMesh, bool flipIndices)
        {
            return InstantiateMeshAndCageWarp(moduleMesh,
                flipIndices,
                new Vector3(-.5f,-.5f,-.5f), new Vector3( .5f,-.5f,-.5f),
                new Vector3(-.5f, .5f,-.5f), new Vector3( .5f, .5f,-.5f),
                new Vector3(-.5f,-.5f, .5f), new Vector3( .5f,-.5f, .5f),
                new Vector3(-.5f, .5f, .5f), new Vector3( .5f, .5f, .5f),
                NormalWarper.identityWarper
            );
        }

        /// <summary>
        /// Create a Unity Mesh from a Wildtile <see cref="ModuleMesh"/> and warp it using basic interpolation.
        /// </summary>
        public static Mesh InstantiateMeshAndCageWarp
        (
            ModuleMesh moduleMesh,
            bool flipIndices,
            Vector3 v000, Vector3 v001,
            Vector3 v010, Vector3 v011,
            Vector3 v100, Vector3 v101,
            Vector3 v110, Vector3 v111,
            NormalWarper normalWarper
        )
        {
            if (moduleMesh == null)
            {
                throw new ArgumentNullException($"{nameof(moduleMesh)} was null");
            }

            var warper = new VertexWarper(
                v000, v001,
                v010, v011,
                v100, v101,
                v110, v111
            );

            var result = new Mesh();
            result.SetVertices(moduleMesh.vertices.Select(vertex => warper.WarpPosition(vertex)).ToArray());
            Assert.AreEqual(moduleMesh.vertices.Length, moduleMesh.normals.Length);
            Assert.AreEqual(moduleMesh.vertices.Length, moduleMesh.tangents.Length);
            result.SetNormals(Enumerable.Range(0, moduleMesh.normals.Length).Select(vertexIndex => normalWarper.WarpNormal(moduleMesh.vertices[vertexIndex], moduleMesh.normals[vertexIndex]).normalized).ToArray());
            result.SetTangents(Enumerable.Range(0, moduleMesh.tangents.Length).Select(vertexIndex => (Vector4)(normalWarper.WarpNormal(moduleMesh.vertices[vertexIndex], moduleMesh.tangents[vertexIndex]).normalized)).ToArray());
            for (var uvIndex = 0; uvIndex < moduleMesh.uvs.Length; ++uvIndex)
            {
                UploadUvs(result, uvIndex, moduleMesh.uvs[uvIndex]);
            }

            if (flipIndices)
            {
                var localIndices = (int[])moduleMesh.triangles.Clone();
                Assert.AreEqual(localIndices.Length, moduleMesh.triangles.Length);
                Assert.AreEqual(localIndices.Length % 3, 0);
                for (var triIndex = 0; triIndex * 3 < localIndices.Length; ++triIndex)
                {
                    var startIndex = triIndex * 3;
                    (localIndices[startIndex], localIndices[startIndex+1]) = (localIndices[startIndex+1], localIndices[startIndex]);
                }
                result.triangles = localIndices;
            }
            else
            {
                result.triangles = moduleMesh.triangles;
            }

            result.SetSubMeshes(ConstructUnitySubMeshDescriptors(moduleMesh.subMeshes));
            return result;
        }

        /// <summary>
        /// Set up a Unity GameObject with the required components to render a Mesh that was previously generated by ModuleMeshInstantiator.
        /// </summary>
        /// <param name="rootObject">The GameObject you want to add the Mesh to.</param>
        /// <param name="moduleMesh">A ModuleMesh. This should be the same ModuleMesh that was used to generate the <paramref name="mesh"/> parameter.</param>
        /// <param name="mesh">A Mesh that was previously generated by a call to <see cref="InstantiateMesh"/> using <paramref name="moduleMesh"/>.</param>
        public static void AddMeshToObject(GameObject rootObject, ModuleMesh moduleMesh, Mesh mesh)
        {
            var filter = rootObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = rootObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = moduleMesh.subMeshes.Select(subMesh => subMesh.material).ToArray();
        }

        private static void UploadUvs(Mesh destinationMesh, int uvChannelIndex, ModuleMesh.UvChannel uvChannel)
        {
            switch (uvChannel.channelWidth)
            {
                case 2:
                    destinationMesh.SetUVs(uvChannelIndex, uvChannel.fullWidthChannel.Select(uv => new Vector2(uv.x, uv.y)).ToArray());
                    break;
                case 3:
                    destinationMesh.SetUVs(uvChannelIndex, uvChannel.fullWidthChannel.Select(uv => new Vector3(uv.x, uv.y, uv.z)).ToArray());
                    break;
                case 4:
                    destinationMesh.SetUVs(uvChannelIndex, uvChannel.fullWidthChannel);
                    break;
                default:
                    Assert.IsTrue(false, $"Unhandled UV width. Unity only supports UVs with width 2, 3, or 4. This UV channel had a width of {uvChannel.channelWidth}");
                    break;
            }
        }

        private static SubMeshDescriptor[] ConstructUnitySubMeshDescriptors(ModuleMesh.SubMesh[] subMeshes)
        {
            return subMeshes.Select(subMesh => new SubMeshDescriptor(subMesh.startIndex, subMesh.indicesCount, MeshTopology.Triangles)).ToArray();
        }
    }
}
