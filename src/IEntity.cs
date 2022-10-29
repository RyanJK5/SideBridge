using MonoGame.Extended.Collisions;
using Microsoft.Xna.Framework;

namespace SideBridge;

public interface IEntity : ICollisionActor {
    public Vector2 Velocity { get; }
}