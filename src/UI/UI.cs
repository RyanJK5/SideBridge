using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace SideBridge;

public abstract class UI : IDrawable {

    public readonly bool InGameUI;
    protected readonly Texture2D Texture;
    protected readonly Vector2 DrawPos;

    public UI(Texture2D texture, Vector2 drawPos, bool inGameUI) {
        Texture = texture;
        DrawPos = drawPos;
        InGameUI = inGameUI;
    }

    public abstract void Draw(SpriteBatch spriteBatch);
}