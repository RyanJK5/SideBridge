using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Collections;

namespace SideBridge;

public abstract class UI {

    private static Bag<UI> allUI = new();

    protected Texture2D _texture;

    public UI(Texture2D texture) {
        _texture = texture;
        allUI.Add(this);
    }

    public abstract void Draw(SpriteBatch spriteBatch);

    public static void DrawUI(SpriteBatch spriteBatch) {
        foreach (var ui in allUI) {
            ui.Draw(spriteBatch);
        }
    }
}