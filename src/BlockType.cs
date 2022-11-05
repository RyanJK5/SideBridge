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

    public static bool Breakable(BlockType type) {
        return type != BlockType.White;
    }
}