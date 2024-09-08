using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{
public class VoxelGridInstantiatorVisitor : IVoxelGridVisitor
{
    public void VisitRectangularVoxelGrid(VoxelGrid originalGrid)
    {
        grid = VoxelGrid.Instantiate(originalGrid);
        VerifyCloneSucceeded(originalGrid);
    }

    public void VisitIrregularVoxelGrid(IrregularVoxelGrid originalGrid)
    {
        grid = IrregularVoxelGrid.Instantiate(originalGrid);
        VerifyCloneSucceeded(originalGrid);
    }

    private void VerifyCloneSucceeded(IVoxelGrid originalGrid)
    {
        Assert.AreEqual(grid != null, originalGrid != null);
        Assert.AreNotEqual(grid, originalGrid);
    }

    public IVoxelGrid grid;
}
}
