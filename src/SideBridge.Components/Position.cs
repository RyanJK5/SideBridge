using Microsoft.Xna.Framework;

namespace SideBridge.Components;

public class Position {
    public float X;
    public float Y;

    public static explicit operator Vector2(Position position) => new(position.X, position.Y);
}