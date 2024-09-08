using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// Contains CPU-side information that describes a mesh. This isn't as comprehensive as a Unity Mesh, e.g. this only supports triangle topology.
///
/// All vertex info is stored in struct-of-array form to make it easier to upload to the GPU through Unity.
/// </summary>
[Serializable]
public class ModuleMesh
{
    /// <summary>
    /// Position data for each vertex.
    /// </summary>
    public Vector3[] vertices;
    /// <summary>
    /// Normal data for each vertex.
    /// </summary>
    public Vector3[] normals;
    /// <summary>
    /// Tangent data for each vertex.
    /// </summary>
    public Vector4[] tangents;
    /// <summary>
    /// Stores between 0 and 8 UV channels. 
    /// </summary>
    public UvChannel[] uvs;

    /// <summary>
    /// Contains vertex indices describing all triangles in the mesh. Each triangle is made up of 3 consecutive indices in this array.
    ///
    /// Always a multiple of 3. This array is not the same size as the vertex arrays (e.g. vertices).
    /// </summary>
    public int[] triangles;

    /// <summary>
    /// Information about which triangles from <see cref="triangles"/> map to which material.
    /// </summary>
    public SubMesh[] subMeshes;

    /// <summary>
    /// Information about which triangles from <see cref="triangles"/> map to which material.
    /// </summary>
    [Serializable]
    public class SubMesh
    {
        /// <summary>
        /// Which *index* to start drawing triangles from in the <see cref="triangles"/> array.
        ///
        /// Is a multiple of 3.
        /// </summary>
        public int startIndex;

        /// <summary>
        /// How many *indices* to consume when drawing triangles from in the <see cref="triangles"/> array.
        ///
        /// Is a multiple of 3. Divide by 3 to get the number of triangles in this submesh.
        /// </summary>
        public int indicesCount;

        /// <summary>
        /// A reference to the Unity Material that should be used when rendering this submesh.
        /// </summary>
        public Material material;
    }

    /// <summary>
    /// Contains data and meta-data for uploading a UV channel to a Unity Mesh.
    /// </summary>
    [Serializable]
    public class UvChannel
    {
        /// <summary>
        /// All UV data for a channel. This data is stored in Vector4s, the largest width UV channel supported by Unity.
        /// </summary>
        public List<Vector4> fullWidthChannel = new List<Vector4>();
        /// <summary>
        /// The actual size for the UV channel. Should be 2, 3, or 4. When uploading to a mesh, remove the extra dimensions from each element in <see cref="fullWidthChannel"/>.
        /// </summary>
        public int channelWidth = 0;
    }

    /// <summary>
    /// Create an empty but valid ModuleMesh.
    /// </summary>
    public static ModuleMesh CreateEmpty()
    {
        var emptyMesh = new ModuleMesh();
        emptyMesh.vertices = new Vector3[0];
        emptyMesh.normals = new Vector3[0];
        emptyMesh.tangents = new Vector4[0];
        emptyMesh.uvs = new UvChannel[0];
        emptyMesh.triangles = new int[0];
        emptyMesh.subMeshes = new SubMesh[0];
        return emptyMesh;
    }

    /// <summary>
    /// Check if a mesh constructed from this ModuleMesh would not actually be rendered.
    /// </summary>
    /// <returns>
    /// False if constructing a mesh from this ModuleMesh would render at least one triangle.
    ///
    /// True otherwise.
    /// </returns>
    public bool IsEmpty()
    {
        return triangles.Length == 0;
    }
}

}
