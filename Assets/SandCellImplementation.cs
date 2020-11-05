using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public struct SandCellImplementation : ICellImplementation
{
    private static readonly HashSet<CellType> AllowedCellTypes = new HashSet<CellType>
    {
        CellType.None,
        CellType.Water
    };
    
    public bool Update(ValueWithNeighbors<Chunk> chunkWithNeighbors, int2 cellPosition, Random random)
    {
        Cell? GetCellAtOffset(int2 offset)
        {
            return CellUtils.GetCellAtPosition(chunkWithNeighbors, cellPosition + offset);
        }
        
        bool MoveTo(int2 offset)
        {
            return CellUtils.SwitchCells(chunkWithNeighbors, cellPosition, offset);
        }
        
        var randomDirection = random.NextBool() ? -1 : 1;

        {
            var cellType = GetCellAtOffset(int2(0, -1))?.type;
            
            if (cellType == CellType.None || cellType == CellType.Water)
            {
                return MoveTo(int2(0, -1));
            }        
        }

        {
            var cellType = GetCellAtOffset(int2(randomDirection, -1))?.type;

            if (cellType == CellType.None || cellType == CellType.Water)
            {
                return MoveTo(int2(randomDirection, -1));
            }
        }
        
        {
            var cellType = GetCellAtOffset(int2(-randomDirection, -1))?.type;

            if (cellType == CellType.None || cellType == CellType.Water)
            {
                return MoveTo(int2(-randomDirection, -1));
            }
        }
        
        return false;
    }
}