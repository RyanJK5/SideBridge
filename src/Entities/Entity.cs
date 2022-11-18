using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SideBridge;

public abstract class Entity {

    public Texture2D Texture;
    public RectangleF Bounds;
    public Vector2 Velocity;
    

    public Entity(Texture2D texture, RectangleF bounds) {
        Texture = texture;
        Bounds = bounds;
    }


    public abstract void OnCollision(Entity other);
    
    public abstract void OnTileCollision(Tile tile);

    public abstract void Update(GameTime gameTime);

    public virtual void Draw(SpriteBatch spriteBatch) =>
        spriteBatch.Draw(Texture, Bounds.Position, Color.White);
}