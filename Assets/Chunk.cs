using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public struct Chunk : IDisposable
{
    private const int chunkSize = WorldGrid.ChunkSize;

    private readonly bool isOutOfBounds;
    [NativeDisableContainerSafetyRestriction]
    private NativeArray<Cell> cells;

    public NativeArray<Cell> Cells => cells;

    public bool IsOutOfBounds => isOutOfBounds;

    public Chunk(Allocator allocator, bool isOutOfBounds = false)
    {
        cells = new NativeArray<Cell>(isOutOfBounds ? 0 : (chunkSize * chunkSize), allocator);
        this.isOutOfBounds = isOutOfBounds;
    }

    public static Chunk CreateOutOfBoundsChunk(Allocator allocator)
    {
        return new Chunk(allocator, true);
    }

    public readonly Cell GetCell(int2 cellPosition)
    {
        return cells[cellPosition.x + (cellPosition.y * chunkSize)];
    }
    
    public readonly void SetCell(int2 cellPosition, Cell cell)
    {
        var cellsCopy = cells;
        cellsCopy[cellPosition.x + (cellPosition.y * chunkSize)] = cell;
    }

    public void Dispose()
    {
        cells.Dispose();
    }
}