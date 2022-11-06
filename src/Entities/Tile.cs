using MonoGame.Extended;
using MonoGame.Extended.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Collisions;

namespace SideBridge;

public class Tile : ICollisionActor {

    public readonly BlockType Type;
    public readonly RectangleF Bounds;

    public Tile(BlockType type, RectangleF bounds) {
        Type = type;
        Bounds = bounds;
    }

    IShapeF ICollisionActor.Bounds => Bounds;
    public void OnCollision(CollisionEventArgs collisionInfo) { }
}