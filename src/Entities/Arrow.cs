using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;

namespace SideBridge;

public class Arrow : Entity {
    public static Texture2D ArrowTexture { get; private set; }

    public readonly float Damage;
    public readonly Team PlayerTeam;

    public static void LoadArrowTexture(ContentManager loader) => ArrowTexture = loader.Load<Texture2D>("img/arrow");

    public Arrow(RectangleF bounds, float damage, Team playerTeam) : base(ArrowTexture, bounds) {
        Bounds = bounds;
        Damage = damage;
        PlayerTeam = playerTeam;
    }

    public override void OnCollision(Entity other) { }

    public override void OnTileCollision(Tile tile) {
        if (TileTypes.Solid(tile.Type) || 
            (TileTypes.SemiSolid(tile.Type) && Bounds.Bottom - Velocity.Y / Game.NativeFPS <= tile.Bounds.Top)) {
            Game.EntityWorld.Remove(this);
        }
    } 

    public override void Update(GameTime gameTime) {
        Bounds.Position += Velocity * gameTime.GetElapsedSeconds();
        Velocity.Y += Game.Gravity;
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
    }
}