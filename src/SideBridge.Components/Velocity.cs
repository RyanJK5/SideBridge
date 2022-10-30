using Microsoft.Xna.Framework;

namespace SideBridge.Components;

public class Velocity {
    public float DirX;
    public float DirY;

    public static explicit operator Vector2(Velocity velocity) => new(velocity.DirX, velocity.DirY);
}