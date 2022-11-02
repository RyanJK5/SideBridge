using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace SideBridge.Components;

public abstract class EntityCollider : ICollisionActor {
    public RectangleF RectBounds;
    public IShapeF Bounds => RectBounds;

    public EntityCollider(RectangleF bounds) =>
        RectBounds = bounds;

    public abstract void OnCollision(CollisionEventArgs collisionInfo);
}