using Unity.Mathematics;
using static Unity.Mathematics.math;
using int2 = Unity.Mathematics.int2;

public struct WaterCellImplementation : ICellImplementation
{
    private readonly static Lch WaterStartColor = new Lch(65f, 37f, 249f);
    private readonly static Lch WaterEndColor = new Lch(70f, 37f, 249f);

    public static Cell CreateWaterCell(ref Random random)
    {
        var color = ColorPlus.LerpInLch(WaterStartColor, WaterEndColor, random.NextFloat());
        
        return new Cell
        {
            type = CellType.Water,
            color = color,
        };
    }
    
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

        if (GetCellAtOffset(int2(0, -1))?.type == CellType.None)
        {
            return MoveTo(int2(0, -1));
        }
        else if (GetCellAtOffset(int2(randomDirection, -1))?.type == CellType.None)
        {
            if (GetCellAtOffset(int2(randomDirection * 2, -2))?.type == CellType.None)
            {
                return MoveTo(int2(randomDirection * 2, -2));
            }
            else
            {
                return MoveTo(int2(randomDirection, -1));
            }
        }
        else if (GetCellAtOffset(int2(-randomDirection, -1))?.type == CellType.None)
        {
            if (GetCellAtOffset(int2(-randomDirection * 2, -2))?.type == CellType.None)
            {
                return MoveTo(int2(-randomDirection * 2, -2));
            }
            else
            {
                return MoveTo(int2(-randomDirection, -1));
            }
        }
        else if (GetCellAtOffset(int2(randomDirection, 0))?.type == CellType.None)
        {
            if (GetCellAtOffset(int2(randomDirection * 2, 0))?.type == CellType.None)
            {
                return MoveTo(int2(randomDirection * 2, 0));
            }
            else
            {
                return MoveTo(int2(randomDirection, 0));
            }
        }
        else if (GetCellAtOffset(int2(-randomDirection, 0))?.type == CellType.None)
        {
            if (GetCellAtOffset(int2(-randomDirection * 2, 0))?.type == CellType.None)
            {
                return MoveTo(int2(-randomDirection * 2, 0));
            }
            else
            {
                return MoveTo(int2(-randomDirection, 0));
            }
        }

        // return
        //     TryMoveTo(math.int2(0, -1)) ||
        //     TryMoveTo(math.int2(randomDirection * 2, -1)) ||
        //     TryMoveTo(math.int2(-randomDirection * 2, -1)) ||
        //     TryMoveTo(math.int2(randomDirection * 2, 0)) ||
        //     TryMoveTo(math.int2(-randomDirection * 2, 0)) ||
        //     TryMoveTo(math.int2(randomDirection, -1)) ||
        //     TryMoveTo(math.int2(-randomDirection, -1)) ||
        //     TryMoveTo(math.int2(randomDirection, 0)) ||
        //     TryMoveTo(math.int2(-randomDirection, 0));

        return false;
    }
}