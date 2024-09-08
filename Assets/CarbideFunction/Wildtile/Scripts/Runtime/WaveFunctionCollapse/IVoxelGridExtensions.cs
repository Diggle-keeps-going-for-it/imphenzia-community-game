using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{

public static class IVoxelGridExtensions
{
    public static int GetCubeContents(this IVoxelGrid voxelGrid,
        int i000,
        int i001,
        int i010,
        int i011,
        int i100,
        int i101,
        int i110,
        int i111
    )
    {
        return
              (SafeGetVoxelContents(voxelGrid, i000) << 0)
            + (SafeGetVoxelContents(voxelGrid, i001) << 1)
            + (SafeGetVoxelContents(voxelGrid, i010) << 2)
            + (SafeGetVoxelContents(voxelGrid, i011) << 3)
            + (SafeGetVoxelContents(voxelGrid, i100) << 4)
            + (SafeGetVoxelContents(voxelGrid, i101) << 5)
            + (SafeGetVoxelContents(voxelGrid, i110) << 6)
            + (SafeGetVoxelContents(voxelGrid, i111) << 7);
    }

    private static int SafeGetVoxelContents(IVoxelGrid voxelGrid, int index)
    {
        if (index == IVoxelGrid.outOfBoundsVoxelIndex)
        {
            return 0;
        }
        else
        {
            return voxelGrid.GetVoxel(index).contents;
        }
    }

    public static int GetCubeContents(this IVoxelGrid voxelGrid, Slot.SourceVoxels sourceVoxels)
    {
        return GetCubeContents(voxelGrid,
            sourceVoxels.voxel000Index,
            sourceVoxels.voxel001Index,
            sourceVoxels.voxel010Index,
            sourceVoxels.voxel011Index,
            sourceVoxels.voxel100Index,
            sourceVoxels.voxel101Index,
            sourceVoxels.voxel110Index,
            sourceVoxels.voxel111Index
        );
    }

    public static IVoxelGrid Clone(this IVoxelGrid voxelGrid)
    {
        var instantiator = new VoxelGridInstantiatorVisitor();
        voxelGrid.Visit(instantiator);
        Assert.AreEqual(voxelGrid != null, instantiator.grid != null);
        Assert.AreNotEqual(voxelGrid, instantiator.grid);
        return instantiator.grid;
    }
}

}
