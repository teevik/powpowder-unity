using System;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;

public struct SandCellImplementation : ICellImplementation
{
    private readonly static Lch SandStartColor = new Lch(78f, 25f, 92f);
    private readonly static Lch SandEndColor = new Lch(83f, 25f, 92f);

    public static Cell CreateSandCell(ref Random random)
    {
        var color = ColorPlus.LerpInLch(SandStartColor, SandEndColor, random.NextFloat());
        
        return new Cell
        {
            type = CellType.Sand,
            color = color,
        };
    }
    
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