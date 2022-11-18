using MonoGame.Extended;
using MonoGame.Extended .Collisions;

namespace SideBridge;

public class Tile : ICollisionActor {

    public const int MaxDurability = 15;

    public readonly BlockType Type;
    public readonly RectangleF Bounds;
    public int Durability;

    public Tile(BlockType type, RectangleF bounds) {
        Type = type;
        Bounds = bounds;
        Durability = MaxDurability;
    }

    IShapeF ICollisionActor.Bounds => Bounds;
    public void OnCollision(CollisionEventArgs collisionInfo) { }
}