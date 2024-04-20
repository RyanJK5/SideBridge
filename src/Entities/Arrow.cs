using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using System;

namespace SideBridge;

public class Arrow : Entity {
    private const float VerticalAcceleration = 1f;

    private static Texture2D _arrowTexture;

    public readonly float Damage;
    public readonly Team PlayerTeam;

    public static Texture2D ArrowTexture { get => _arrowTexture; set => _arrowTexture = value; }

    public Arrow(RectangleF bounds, float damage, Team playerTeam) : base(ArrowTexture, bounds) {
        Bounds = bounds;
        Damage = damage;
        PlayerTeam = playerTeam;
    }

    public override void OnCollision(Entity other) { }

    public override void OnTileCollision(Tile tile) {
        Velocity = Vector2.Zero;
        Game.RemoveEntity(this);
    }

    public override void Update(GameTime gameTime) {
        Bounds.Position += Velocity;
        Velocity.Y += VerticalAcceleration;
    }

    public override void Draw(SpriteBatch spriteBatch) {
        var normal = Velocity.NormalizedCopy();
        var rotation = MathF.Atan2(normal.Y, normal.X);
        spriteBatch.Draw(
            Texture, 
            Bounds.Center, 
            null, 
            Color.White,
            rotation,
            Bounds.Center - Bounds.Position,
            Vector2.One,
            SpriteEffects.None,
            0f
        );
        // spriteBatch.DrawRectangle(Bounds, Color.Red);
    }

}