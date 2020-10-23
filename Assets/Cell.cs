public enum CellType : uint
{
    None = 0,
    Stone = 1,
    Sand = 2,
    Water = 3
}

public struct Cell
{
    public CellType type;
    public uint data1;
    public uint data2;
    public uint clock;
}