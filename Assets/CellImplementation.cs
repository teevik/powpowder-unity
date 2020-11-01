using Unity.Mathematics;

public interface ICellImplementation
{
    public bool Update(ChunkWithNeighbors chunkWithNeighbors, int2 cellPosition, Random random);
}