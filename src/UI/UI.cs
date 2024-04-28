using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace SideBridge;

public abstract class UI : IDrawable {

    public readonly GameState StateType;
    protected readonly Texture2D Texture;
    protected readonly Vector2 DrawPos;

    public UI(Texture2D texture, Vector2 drawPos, GameState stateType) {
        Texture = texture;
        DrawPos = drawPos;
        StateType = stateType;
    }

    public abstract void Draw(SpriteBatch spriteBatch);
}