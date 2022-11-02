using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace SideBridge.Components;

public class StaticCollider : EntityCollider {
    public override void OnCollision(CollisionEventArgs collisionInfo) { }
    public StaticCollider(RectangleF bounds) : base(bounds) { }
}