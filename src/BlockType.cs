#nullable enable 

namespace SideBridge;

public enum BlockType {
    Air,
    Red,
    Blue
}

public static class Blocks {
    public static bool Solid(BlockType type) => type != BlockType.Air;
}