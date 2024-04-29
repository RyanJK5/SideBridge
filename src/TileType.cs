using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SideBridge;

public enum TileType {
    Air,
    Blue,
    Red,
    White,
    DarkBlue,
    DarkRed,
    Goal,
    Glass,
    Platform
}

public static class TileTypes {

    private static readonly Color BlueTileColor = new(61, 50, 76);
    private static readonly Color RedTileColor = new(124, 54, 39);
    private static readonly Color DarkBlueTileColor = new(20, 16, 25);
    private static readonly Color DarkRedTileColor = new(73, 32, 23);

    public static bool Breakable(TileType type) => GetBreakableTypes().Contains(type);

    public static bool Solid(TileType type) => type != TileType.Air && type != TileType.Goal && !SemiSolid(type);
    
    public static bool SemiSolid(TileType type) => type == TileType.Platform;

    public static TileType[] GetBreakableTypes() => 
        new TileType[] { TileType.Blue, TileType.Red, TileType.DarkBlue, TileType.DarkRed}
    ;

    public static Color GetParticleColor(TileType type) {
        return type switch {
            TileType.Blue => BlueTileColor,
            TileType.Red => RedTileColor,
            TileType.DarkBlue => DarkBlueTileColor,
            TileType.DarkRed => DarkRedTileColor,
            _ => throw new ArgumentException("type " + type + " does not produce particles"),
        };
    }
}