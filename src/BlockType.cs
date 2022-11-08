namespace SideBridge;

public enum BlockType {
    Air,
    Blue,
    Red,
    White,
    DarkBlue,
    DarkRed
}

public static class Blocks {

    public static bool Breakable(BlockType type) => type != BlockType.Air && type != BlockType.White;

    public static bool Solid(BlockType type) => type != BlockType.Air;
}