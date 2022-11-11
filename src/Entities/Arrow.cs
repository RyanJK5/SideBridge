using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using System;

namespace SideBridge;

public class Arrow : Entity, ICollisionActor {
    private const float VerticalAcceleration = 1f;
    
    public static Texture2D ArrowTexture;

    public readonly float Damage;
    public readonly int PlayerID;
    private bool _collidedOnce = false;

    public Arrow(RectangleF bounds, float damage, int playerID) : base(ArrowTexture, bounds) {
        Bounds = bounds;
        Damage = damage;
        PlayerID = playerID;
    }

    public override void OnCollision(CollisionEventArgs args) {
        if (args.Other is Tile) {
            if (_collidedOnce) {
                Game.RemoveEntity(this);
            }
            _collidedOnce = true;
        }
        if (args.Other is Player player && player.ID != PlayerID) {
            Game.RemoveEntity(this);
        }
    }

    public override void Update(GameTime gameTime) {
        Bounds.Position += Velocity;
        Velocity.Y += VerticalAcceleration;
    }

    public override void Draw(SpriteBatch spriteBatch) {
        var normal = Velocity.NormalizedCopy();
        var rotation = MathF.Atan2(normal.Y, normal.X) + (MathF.PI / 2);

        ((ICollisionActor) this).Bounds.Rotate(rotation);

        spriteBatch.Draw(
            Texture, 
            Bounds.Position, 
            null, 
            Color.White,
            rotation,
            Vector2.Zero,
            Vector2.One,
            SpriteEffects.None,
            0f
        );
    }

    IShapeF ICollisionActor.Bounds => Bounds;
}