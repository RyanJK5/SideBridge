using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SideBridge;

public abstract class GameObject {

    protected Texture2D texture;
    protected Vector2 position;

    public GameObject(Texture2D texture, Vector2 pos) {
        this.texture = texture;
        position = pos;
    }

    public GameObject(Texture2D texture) : this(texture, new Vector2()) { }

    public virtual void Draw(SpriteBatch spriteBatch) {
        spriteBatch.Draw(texture, position, Color.White);
    }

    public Vector2 GetPosition() => new Vector2(position.X, position.Y);

    public virtual void Update(GameTime gameTime) {}
}