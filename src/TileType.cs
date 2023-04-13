namespace SideBridge;

public enum TileType {
    Air,
    Blue,
    Red,
    White,
    DarkBlue,
    DarkRed,
    Goal
}

public static class TileTypes {

    public static bool Breakable(TileType type) => type != TileType.Air && type != TileType.White && type != TileType.Goal;

    public static bool Solid(TileType type) => type != TileType.Air && type != TileType.Goal;
}