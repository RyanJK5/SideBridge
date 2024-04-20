namespace SideBridge;

public enum TileType {
    Air,
    Blue,
    Red,
    White,
    DarkBlue,
    DarkRed,
    Goal,
    Glass
}

public static class TileTypes {

    public static bool Breakable(TileType type) => type == TileType.Blue || type == TileType.Red || type == TileType.DarkBlue || type == TileType.DarkRed;

    public static bool Solid(TileType type) => type != TileType.Air && type != TileType.Goal;
}