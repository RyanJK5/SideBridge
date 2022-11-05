using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace SideBridge.Components;

public class StaticCollider : EntityCollider {
    public override void OnCollision(CollisionEventArgs collisionInfo) { }

    public override bool Equals(object obj) => obj is StaticCollider collider && RectBounds.Equals(collider.RectBounds);
    public override int GetHashCode() => 0;

    public StaticCollider(RectangleF bounds) : base(bounds) { }

}