using Unity.Mathematics;

public static class CellUtils
{
    private const int chunkSize = WorldGrid.ChunkSize;

    public static bool GetRelativeCellPosition(ValueWithNeighbors<Chunk> chunkWithNeighbors, int2 cellPosition, out int2 relativeChunkPosition, out int2 relativeCellPosition)
    {
        var chunkOffset = math.int2(math.floor(math.float2(cellPosition) / math.float2(chunkSize, chunkSize)));
        var relativeTargetCell = (cellPosition - (chunkOffset * chunkSize));
    
        var neighborChunk = chunkWithNeighbors.ChunkFromPosition(chunkOffset);
    
        if (neighborChunk.IsOutOfBounds)
        {
            relativeChunkPosition = default;
            relativeCellPosition = default;
            return false;
        }
                        
        relativeChunkPosition = chunkOffset;
        relativeCellPosition = relativeTargetCell;
        return true;
    }
    
    public static Cell? GetCellAtPosition(ValueWithNeighbors<Chunk> chunkWithNeighbors, int2 cellPosition)
    {
        if (GetRelativeCellPosition(chunkWithNeighbors, cellPosition, out var relativeChunkPosition, out var relativeCellPosition))
        {
            return chunkWithNeighbors.ChunkFromPosition(relativeChunkPosition).GetCell(relativeCellPosition);
        }

        return null;
    }
    
    // public static void SwitchCells(ChunkWithNeighbors chunkWithNeighbors, int2 absoluteOldCellPosition, int2 absoluteNewCellPosition, Cell cell)
    // {
    //     if (GetRelativeCellPosition(chunkWithNeighbors, absoluteNewCellPosition, out var newChunkPosition,
    //         out var newCellPosition))
    //     {
    //         var oldCellChunk = chunkWithNeighbors.Chunk;
    //         var newCellChunk = chunkWithNeighbors.ChunkFromPosition(newChunkPosition);
    //     
    //         var existingCell = oldCellChunk.GetCell(oldCellPosition);
    //         
    //         oldCellChunk.SetCell(oldCellPosition, existingCell);
    //         newCellChunk.SetCell(newCellPosition, cell);
    //     }
    // }
    //
    public static bool SwitchCells(ValueWithNeighbors<Chunk> chunkWithNeighbors, int2 absoluteCurrentCellPosition,
        int2 offset)
    {
        if (!GetRelativeCellPosition(chunkWithNeighbors, absoluteCurrentCellPosition, out var oldChunkPosition,
            out var oldCellPosition)) return false;
        if (!GetRelativeCellPosition(chunkWithNeighbors, absoluteCurrentCellPosition + offset, out var newChunkPosition,
            out var newCellPosition)) return false;
        
        var oldCellChunk = chunkWithNeighbors.Value;
        var newCellChunk = chunkWithNeighbors.ChunkFromPosition(newChunkPosition);
    
        var oldCell = oldCellChunk.GetCell(oldCellPosition);
        var newCell = newCellChunk.GetCell(newCellPosition);

        oldCellChunk.SetCell(oldCellPosition, newCell);
        newCellChunk.SetCell(newCellPosition, oldCell);

        return true;

    }


    
    // public static void SetAndSwitchCell(ChunkWithNeighbors chunkWithNeighbors, int2 oldCellPosition, int2 absoluteNewCellPosition, Cell cell)
    // {
    //     if (GetRelativeCellPosition(chunkWithNeighbors, absoluteNewCellPosition, out var newChunkPosition,
    //         out var newCellPosition))
    //     {
    //         var oldCellChunk = chunkWithNeighbors.Chunk;
    //         var newCellChunk = chunkWithNeighbors.ChunkFromPosition(newChunkPosition);
    //     
    //         var existingCell = oldCellChunk.GetCell(oldCellPosition);
    //         
    //         oldCellChunk.SetCell(oldCellPosition, existingCell);
    //         newCellChunk.SetCell(newCellPosition, cell);
    //     }
    // }

    // public static bool MoveToIfEmpty(ChunkWithNeighbors chunkWithNeighbors, int2 oldCellPosition, int2 newCellPosition, Cell cell)
    // {
    //     if (GetCellAtPosition(chunkWithNeighbors, newCellPosition, out var neighborChunkPosition, out var neighborCellPosition))
    //     {
    //         var neighborChunk = chunkWithNeighbors.ChunkFromPosition(neighborChunkPosition);
    //         var neighborCell = neighborChunk.GetCell(oldCellPosition);
    //                 
    //         if (neighborCell.type == CellType.None)
    //         {
    //             chunkWithNeighbors.Chunk.SetCell(newCellPosition, default);
    //             neighborChunk.SetCell(neighborCellPosition, cell);
    //                             
    //             return true;
    //         }
    //     }
    //                     
    //     return false;
    // }

    // public static bool SwitchCellsIfTargetEmpty(ChunkWithNeighbors chunkWithNeighbors, int2 absoluteCurrentCellPosition,
    //     int2 absoluteTargetCellPosition)
    // {
    //     if (GetCellAtPosition(chunkWithNeighbors, absoluteCurrentCellPosition, out var oldChunkPosition,
    //         out var oldCellPosition))
    //     {
    //         if (GetCellAtPosition(chunkWithNeighbors, absoluteTargetCellPosition, out var newChunkPosition,
    //             out var newCellPosition))
    //         {
    //             var oldCellChunk = chunkWithNeighbors.Chunk;
    //             var newCellChunk = chunkWithNeighbors.ChunkFromPosition(newChunkPosition);
    //
    //             var oldCell = oldCellChunk.GetCell(oldCellPosition);
    //             var newCell = newCellChunk.GetCell(newCellPosition);
    //
    //             if (newCell.type == CellType.None)
    //             {
    //                 oldCellChunk.SetCell(oldCellPosition, newCell);
    //                 newCellChunk.SetCell(newCellPosition, oldCell);
    //
    //                 return true;
    //             }
    //         }
    //     }
    // }

    public static bool SwitchCellsIfTargetEmpty(ValueWithNeighbors<Chunk> chunkWithNeighbors, int2 absoluteCurrentCellPosition, int2 absoluteTargetCellPosition)
    {
        if (GetRelativeCellPosition(chunkWithNeighbors, absoluteCurrentCellPosition, out var oldChunkPosition, out var oldCellPosition))
        {
            if (GetRelativeCellPosition(chunkWithNeighbors, absoluteTargetCellPosition, out var newChunkPosition, out var newCellPosition))
            {
                var oldCellChunk = chunkWithNeighbors.Value;
                var newCellChunk = chunkWithNeighbors.ChunkFromPosition(newChunkPosition);

                var oldCell = oldCellChunk.GetCell(oldCellPosition);
                var newCell = newCellChunk.GetCell(newCellPosition);

                if (newCell.type == CellType.None)
                {
                    oldCellChunk.SetCell(oldCellPosition, newCell);
                    newCellChunk.SetCell(newCellPosition, oldCell);
                    
                    return true;
                }
            }
        }
                        
        return false;
    }
}