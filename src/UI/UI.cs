using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Collections;

namespace SideBridge;

public abstract class UI : IDrawable {

    protected Texture2D Texture;
    protected Vector2 DrawPos;

    public UI(Texture2D texture, Vector2 drawPos) {
        Texture = texture;
        DrawPos = drawPos;
    }

    public abstract void Draw(SpriteBatch spriteBatch);
}