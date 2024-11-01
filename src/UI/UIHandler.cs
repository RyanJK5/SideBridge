using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Collections;

namespace SideBridge;

public class UIHandler : IDrawable, IUpdatable {
    
    private readonly Bag<UI> _allUI;

    private static bool CanDrawUI(UI ui) => Settings.GameState == ui.StateType;

    public UIHandler(params UI[] allUI) {
        _allUI = new Bag<UI>();
        foreach (UI ui in allUI) {
            _allUI.Add(ui);
        }
    }

    public void Draw(SpriteBatch spriteBatch) {
        foreach (UI ui in _allUI) {
            if (CanDrawUI(ui)) {
                ui.Draw(spriteBatch);
            }
        }
    }

    public void Update(GameTime gameTime) {
        foreach (UI ui in _allUI) {
            if (ui is IUpdatable updatable && CanDrawUI(ui)) {
                updatable.Update(gameTime);
            }
        }
    }
}