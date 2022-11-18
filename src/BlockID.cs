namespace SideBridge;

public enum BlockID {
    Air,
    Blue,
    Red,
    White,
    DarkBlue,
    DarkRed
}

public static class Blocks {

    public static bool Breakable(BlockID type) => type != BlockID.Air && type != BlockID.White;

    public static bool Solid(BlockID type) => type != BlockID.Air;
}