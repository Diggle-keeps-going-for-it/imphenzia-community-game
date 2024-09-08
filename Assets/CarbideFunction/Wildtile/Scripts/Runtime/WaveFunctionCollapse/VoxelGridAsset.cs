using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarbideFunction.Wildtile
{
public abstract class VoxelGridAsset : ScriptableObject, IVoxelGrid
{
    public abstract void Visit(IVoxelGridVisitor voxelGridVisitor);

    public abstract IVoxelGrid.Voxel GetVoxel(int index);
    public abstract void SetVoxelContents(int index, int newContents);

    public abstract IReadOnlyList<IVoxelGrid.Voxel> Voxels {get;}
}
}
