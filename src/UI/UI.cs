using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Collections;

namespace SideBridge;

public abstract class UI {

    private static Bag<UI> s_allUI = new();

    protected Texture2D Texture;
    protected Vector2 DrawPos;

    public UI(Texture2D texture, Vector2 drawPos) {
        Texture = texture;
        DrawPos = drawPos;
        s_allUI.Add(this);
    }

    public abstract void Draw(SpriteBatch spriteBatch);

    public static void DrawUI(SpriteBatch spriteBatch) {
        foreach (var ui in s_allUI) {
            ui.Draw(spriteBatch);
        }
    }
}