using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile.Editor
{

internal static class ModuleMeshExtractor
{
    /// <summary>
    /// Calculate the mesh from a game object.
    ///
    /// If <paramref name="modulePrefab"/> is null, throws an ArgumentNullException.
    /// </summary>
    /// <returns>
    /// A ModuleMesh filled with information read from the game object. In successful operation, errorMessage will be null.
    ///
    /// If there are problems with the game object setup, the returned ModuleMesh will be null and errorMessage will contain the information
    /// </returns>
    public static ModuleMesh ExtractModelAndDeleteModelObjects(GameObject moduleInstance, Vector3 tileDimensions, Vector3 inverseTileDimensions, out string errorMessage)
    {
        return ExtractModelAndActOnObjects(moduleInstance, tileDimensions, inverseTileDimensions, out errorMessage,
            wildtileMeshObject => {
                Component.DestroyImmediate(wildtileMeshObject.GetComponent<WildtileMesh>());
                Component.DestroyImmediate(wildtileMeshObject.GetComponent<MeshRenderer>());
                Component.DestroyImmediate(wildtileMeshObject.GetComponent<MeshFilter>());
            },
            (wildtileMeshObject, shouldKeep) => {
                // first check if another call has already destroyed this game object's parent
                // and therefore this object has been destroyed too
                if (   wildtileMeshObject.gameObject != null
                    && !shouldKeep
                    && !DoesGameObjectStillHaveComponents(wildtileMeshObject)
                )
                {
                    GameObject.DestroyImmediate(wildtileMeshObject);
                }
            },
            rootObject => {
                DestroyObjectIfEmpty(rootObject);
            });
    }

    internal static ModuleMesh ExtractModelOnly(GameObject moduleInstance, Vector3 tileDimensions, Vector3 inverseTileDimensions, out string errorMessage)
    {
        return ExtractModelAndActOnObjects(moduleInstance, tileDimensions, inverseTileDimensions, out errorMessage,
            _ => {
            },
            (_, _) => {
            },
            _ => {
            });
    }

    private static ModuleMesh ExtractModelAndActOnObjects
    (
        GameObject moduleInstance,
        Vector3 tileDimensions,
        Vector3 inverseTileDimensions,
        out string errorMessage,
        Action<GameObject> doWithWildtileMeshObjectFirstPass,
        Action<GameObject, bool /*should keep*/ > doWithWildtileMeshObjectSecondPass,
        Action<GameObject> doWithRootObjectAfterPasses
    )
    {
        Assert.IsNotNull(moduleInstance);

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var tangents = new List<Vector4>();
        var uvs = new List<ModuleMesh.UvChannel>();

        var triangles = new List<int>();
        var subMeshes = new List<ModuleMesh.SubMesh>();

        var wildtileMeshes = moduleInstance.GetComponentsInChildren<WildtileMesh>();

        var errorMessages = new List<string>();

        foreach (var wildtileMesh in wildtileMeshes)
        {
            var vertexIndexStart = vertices.Count;
            Func<IEnumerable<int>, IEnumerable<int>> vertexIndexBiaser = indices => indices.Select(index => index + vertexIndexStart);

            var transform = Matrix4x4.Scale(inverseTileDimensions) * wildtileMesh.transform.localToWorldMatrix;
            var normalTransform = Matrix4x4.Scale(tileDimensions) * wildtileMesh.transform.localToWorldMatrix;

            var maybeObjectMeshData = ValidateAndGetData(wildtileMesh.gameObject, errorMessages);
            if (maybeObjectMeshData is ObjectMeshData objectMeshData)
            {
                var meshFilter = wildtileMesh.GetComponent<MeshFilter>();
                var meshRenderer = wildtileMesh.GetComponent<MeshRenderer>();
                var mesh = meshFilter.sharedMesh;

                var meshNormals = mesh.normals;
                vertices.AddRange(mesh.vertices.Select(vertex => transform.MultiplyPoint(vertex)));
                normals.AddRange(meshNormals.Select(normal => normalTransform.MultiplyVector(normal).normalized));
                var meshTangents = mesh.tangents;
                if (mesh.tangents.Length == 0)
                {
                    // construct tangents
                    tangents.AddRange(meshNormals.Select(normal => {
                        var simpleTangent = Vector3.Cross(Vector3.up, normalTransform.MultiplyVector(normal));
                        // if the normal is facing straight up, the calculated tangent will be 0
                        if (simpleTangent.sqrMagnitude < 1e-5f)
                        {
                            return new Vector4(1f, 0f, 0f, 0f);
                        }
                        else
                        {
                            return (Vector4)simpleTangent.normalized;
                        }
                    }));
                }
                else
                {
                    Assert.AreEqual(mesh.tangents.Length, meshNormals.Length);
                    tangents.AddRange(mesh.tangents.Select(tangent => (Vector4)normalTransform.MultiplyVector(tangent).normalized));
                }
                EnsureUvChannelsExist(mesh, uvs, vertices.Count);
                UploadUvData(mesh, uvs);

                var isFlipped = transform.determinant < 0f;

                var sourceTriangles = mesh.triangles;

                for (var triangleStartVertexIndex = 0; triangleStartVertexIndex < sourceTriangles.Length; triangleStartVertexIndex += 3)
                {
                    var vert0Index = sourceTriangles[triangleStartVertexIndex    ];
                    var vert1Index = sourceTriangles[triangleStartVertexIndex + 1];
                    var vert2Index = sourceTriangles[triangleStartVertexIndex + 2];

                    var flipAwareVert0Index = vert0Index;
                    var flipAwareVert1Index = isFlipped ? vert2Index : vert1Index;
                    var flipAwareVert2Index = isFlipped ? vert1Index : vert2Index;

                    triangles.Add(flipAwareVert0Index);
                    triangles.Add(flipAwareVert1Index);
                    triangles.Add(flipAwareVert2Index);
                }

                var sourceMaterials = meshRenderer.sharedMaterials;
                Assert.AreEqual(mesh.subMeshCount, sourceMaterials.Length);

                for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
                {
                    var sourceSubMesh = mesh.GetSubMesh(subMeshIndex);
                    var sourceMaterial = sourceMaterials[subMeshIndex];
                    var newSubMesh = new ModuleMesh.SubMesh();
                    newSubMesh.startIndex = sourceSubMesh.indexStart;
                    newSubMesh.indicesCount = sourceSubMesh.indexCount;
                    newSubMesh.material = sourceMaterial;
                    subMeshes.Add(newSubMesh);
                }
            }
        }

        if (errorMessages.Count != 0)
        {
            errorMessage = String.Join("\n", errorMessages);
            return null;
        }

        // need to convert to array otherwise the second loop will rerun the enumeration after I've deleted the WildtileMeshes
        // and attempt to access those deleted components.
        var wildtileGameObjects = wildtileMeshes.Select(mesh => new {gameObject = mesh.gameObject, forceKeep = mesh.keepObjectAfterMeshStripping}).ToArray();

        // We need to separate these loops because a child with a WildtileMesh could come later and prevent a parent from destroying itself,
        // then the child is destroyed, but then the parent is never rechecked.
        foreach (var gameObjectAndKeep in wildtileGameObjects)
        {
            var gameObject = gameObjectAndKeep.gameObject;
            doWithWildtileMeshObjectFirstPass(gameObject);
        }

        foreach (var gameObjectAndKeep in wildtileGameObjects)
        {
            doWithWildtileMeshObjectSecondPass(gameObjectAndKeep.gameObject, gameObjectAndKeep.forceKeep);
        }

        doWithRootObjectAfterPasses(moduleInstance);

        errorMessage = null;
        var moduleMesh = new ModuleMesh{
            vertices = vertices.ToArray(),
            normals = normals.ToArray(),
            tangents = tangents.ToArray(),
            uvs = uvs.ToArray(),
            triangles = triangles.ToArray(),
            subMeshes = subMeshes.ToArray(),
        };

        return moduleMesh;
    }

    private static void EnsureUvChannelsExist(Mesh mesh, List<ModuleMesh.UvChannel> uvChannels, int numberOfVertices)
    {
        var numberOfChannels = GetNumberOfUvChannels(mesh);

        for (var extraChannelIndex = uvChannels.Count; extraChannelIndex < numberOfChannels; ++extraChannelIndex)
        {
            uvChannels.Add(new ModuleMesh.UvChannel{
                fullWidthChannel = new List<Vector4>(Enumerable.Repeat(Vector4.zero, numberOfVertices)),
                channelWidth = 0,
            });
        }
    }

    private static int GetNumberOfUvChannels(Mesh mesh)
    {
        for (var i = 0; i < 8; ++i)
        {
            if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0 + i))
            {
                return i;
            }
        }

        return 8;
    }

    private static void UploadUvData(Mesh mesh, List<ModuleMesh.UvChannel> uvChannels)
    {
        var numberOfChannels = GetNumberOfUvChannels(mesh);
        Assert.AreEqual(numberOfChannels, uvChannels.Count);

        for (var channelIndex = 0; channelIndex < numberOfChannels; ++channelIndex)
        {
            var channelWidth = mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord0 + channelIndex);
            var destinationChannel = uvChannels[channelIndex];
            switch (channelWidth)
            {
                case 2:
                {
                    var sourceUvs = new List<Vector2>();
                    mesh.GetUVs(channelIndex, sourceUvs);
                    CopyUvChannelFromMeshToChannel(sourceUvs, destinationChannel.fullWidthChannel, ConvertVector2ToVector4);
                    break;
                }
                case 3:
                {
                    var sourceUvs = new List<Vector3>();
                    mesh.GetUVs(channelIndex, sourceUvs);
                    CopyUvChannelFromMeshToChannel(sourceUvs, destinationChannel.fullWidthChannel, ConvertVector3ToVector4);
                    break;
                }
                case 4:
                {
                    var sourceUvs = new List<Vector4>();
                    mesh.GetUVs(channelIndex, sourceUvs);
                    CopyUvChannelFromMeshToChannel(sourceUvs, destinationChannel.fullWidthChannel, ConvertVector4ToVector4);
                    break;
                }
                default:
                    Assert.IsTrue(false, $"Unexpected channel width. Wildtile assumes Unity only supports UVs with channel widths of 2, 3, or 4. Channel width was {channelWidth}.");
                    break;
            }
            destinationChannel.channelWidth = Math.Max(destinationChannel.channelWidth, channelWidth);
        }
    }

    private static void CopyUvChannelFromMeshToChannel<SourceVectorType>
    (
        List<SourceVectorType> sourceUvs,
        List<Vector4> destinationUvs,
        Func<SourceVectorType, Vector4> vectorConverter
    )
    {
        destinationUvs.Clear();
        destinationUvs.AddRange(sourceUvs.Select(anyWidthVector => vectorConverter(anyWidthVector)));
    }

    private static Vector4 ConvertVector2ToVector4(Vector2 vector)
    {
        return new Vector4(vector.x, vector.y, 0f, 0f);
    }

    private static Vector4 ConvertVector3ToVector4(Vector3 vector)
    {
        return new Vector4(vector.x, vector.y, vector.z, 0f);
    }

    private static Vector4 ConvertVector4ToVector4(Vector4 vector)
    {
        return vector;
    }
    
    private static bool DoesGameObjectStillHaveComponents(GameObject gameObject)
    {
        foreach (var component in gameObject.GetComponentsInChildren<Component>())
        {
            if (component.GetType() != typeof(Transform))
            {
                return true;
            }
        }

        return false;
    }

    private struct ObjectMeshData
    {
        public MeshRenderer renderer;
        public MeshFilter filter;
        public Mesh mesh;
    }

    private static ObjectMeshData? ValidateAndGetData(GameObject meshObject, List<string> errorMessages)
    {
        if (!meshObject.TryGetComponent<MeshRenderer>(out var siblingRenderer))
        {
            errorMessages.Add($"There was a WildtileMesh on \"{GetHierarchy(meshObject.gameObject)}\" but there was no sibling MeshRenderer");
            return null;
        }
        if (!meshObject.TryGetComponent<MeshFilter>(out var siblingFilter))
        {
            errorMessages.Add($"There was a WildtileMesh on \"{GetHierarchy(meshObject.gameObject)}\" but there was no sibling MeshFilter");
            return null;
        }
        var mesh = siblingFilter.sharedMesh;
        if (mesh == null)
        {
            errorMessages.Add($"There was a WildtileMesh on \"{GetHierarchy(meshObject.gameObject)}\" but the sibling MeshFilter had a null mesh");
            return null;
        }

        var objectMeshData = new ObjectMeshData{
            renderer = siblingRenderer,
            filter = siblingFilter,
            mesh = mesh,
        };

        if (!ValidateObjectMeshData(objectMeshData, errorMessages))
        {
            return null;
        }

        return objectMeshData;
    }

    private static bool ValidateObjectMeshData(ObjectMeshData objectMeshData, List<string> errorMessages)
    {
        // Do not &&-shortcut validation so that all messages are reported to the user.
        // This respects the user's time by allowing them to fix all problems rather than fixing one batch before moving on to the next batch.
        var matCountMatchesSubMeshCount = ValidateMaterialCountMatchesSubMeshCount(objectMeshData.renderer, objectMeshData.mesh, errorMessages);
        var materialsNonNull = ValidateMaterialsNonNull(objectMeshData.renderer, errorMessages);
        var subMeshTopology = ValidateSubMeshTopology(objectMeshData.renderer.gameObject, objectMeshData.mesh, errorMessages);

        return matCountMatchesSubMeshCount
            && materialsNonNull
            && subMeshTopology;
    }

    private static bool ValidateMaterialCountMatchesSubMeshCount(MeshRenderer renderer, Mesh mesh, List<string> errorMessages)
    {
        if (mesh.subMeshCount != renderer.sharedMaterials.Length)
        {
            errorMessages.Add($"On \"{GetHierarchy(renderer.gameObject)}\" there were {mesh.subMeshCount} sub mesh(es) and {renderer.sharedMaterials.Length} material(s). These must be equal.");
            return false;
        }

        return true;
    }

    private static bool ValidateMaterialsNonNull(MeshRenderer renderer, List<string> errorMessages)
    {
        var anyMaterialsNull = false;

        for (var i = 0; i < renderer.sharedMaterials.Length; ++i)
        {
            if (renderer.sharedMaterials[i] == null)
            {
                errorMessages.Add($"On \"{GetHierarchy(renderer.gameObject)}\", material {i} was null. All materials must be non-null.");
                anyMaterialsNull = true;
            }
        }

        return !anyMaterialsNull;
    }

    private static bool ValidateSubMeshTopology(GameObject owningObject, Mesh mesh, List<string> errorMessages)
    {
        var allTopologiesAreTriangles = true;

        for (var i = 0; i < mesh.subMeshCount; ++i)
        {
            var subMesh = mesh.GetSubMesh(i);
            if (subMesh.topology != MeshTopology.Triangles)
            {
                errorMessages.Add($"On \"{GetHierarchy(owningObject)}\" - mesh \"{mesh.name}\", sub mesh {i} had topology {subMesh.topology}. Wildtile only supports {MeshTopology.Triangles} topology.");
                allTopologiesAreTriangles = false;
            }
        }

        return allTopologiesAreTriangles;
    }

    private static string GetHierarchy(GameObject child)
    {
        return GetHierarchy(child.transform);
    }

    private static string GetHierarchy(Transform child)
    {
        var hierarchy = new List<string>();
        var currentChild = child;
        while (currentChild != null)
        {
            hierarchy.Add(currentChild.gameObject.name);
            currentChild = currentChild.parent;
        }

        return String.Join("->", Enumerable.Reverse(hierarchy));
    }

    private static List<ModuleMesh.SubMesh> ReadSubMeshes(Mesh mesh, MeshRenderer renderer)
    {
        Assert.AreEqual(mesh.subMeshCount, renderer.sharedMaterials.Length);

        var result = new List<ModuleMesh.SubMesh>();

        for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
        {
            var sourceSubMesh = mesh.GetSubMesh(subMeshIndex);
            result.Add(new ModuleMesh.SubMesh{
                startIndex = sourceSubMesh.indexStart,
                indicesCount = sourceSubMesh.indexCount,
                material = renderer.sharedMaterials[subMeshIndex],
            });
        }

        return result;
    }

    private static void DestroyObjectIfEmpty(GameObject moduleInstance)
    {
        var hasChildren = moduleInstance.transform.childCount > 0;

        // objects will always have a transform component. Check if there are more than one component.
        var hasComponents = moduleInstance.GetComponents<Component>().Length > 1;

        var shouldDestroy = !hasChildren && !hasComponents;

        if (shouldDestroy)
        {
            GameObject.DestroyImmediate(moduleInstance);
        }
    }
}

}
