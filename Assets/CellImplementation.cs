using Unity.Mathematics;

public interface ICellImplementation
{
    public bool Update(ValueWithNeighbors<Chunk> chunkWithNeighbors, int2 cellPosition, Random random);
}