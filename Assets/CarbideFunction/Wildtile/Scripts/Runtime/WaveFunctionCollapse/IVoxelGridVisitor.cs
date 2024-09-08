namespace CarbideFunction.Wildtile
{

public interface IVoxelGridVisitor
{
    void VisitRectangularVoxelGrid(VoxelGrid grid);
    void VisitIrregularVoxelGrid(IrregularVoxelGrid grid);
}
}
