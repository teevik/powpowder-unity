using UnityEngine;

public enum CellType : uint
{
    None = 0,
    Stone = 1,
    Sand = 2,
    Water = 3
}

public struct Cell
{
    public static readonly Cell EmptyCell = new Cell
    {
        type = CellType.None,
        isStale = true
    };

    public CellType type;
    public uint data1;
    public uint data2;
    public uint clock;
    public bool isStale;
    public Color color;
}