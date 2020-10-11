public enum CellType : uint
{
    Stone = 0,
    Sand = 1
}

public struct Cell
{
    public CellType type;
    public uint data1;
    public uint data2;
    public uint clock;
}