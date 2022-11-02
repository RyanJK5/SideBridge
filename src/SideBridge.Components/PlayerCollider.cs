using System;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace SideBridge.Components;

public class PlayerCollider : EntityCollider {

    private Velocity _velocity;

    public override void OnCollision(CollisionEventArgs collisionInfo) => OnPlayerCollision(this, collisionInfo);
    
    public event EventHandler<CollisionEventArgs> OnPlayerCollision;

    public PlayerCollider(RectangleF bounds, Velocity velocity) : base(bounds) {
        _velocity = velocity;
    }
}