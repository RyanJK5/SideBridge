using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input.InputListeners;

namespace SideBridge;

public class Hotbar {

    private const int SlotNum = 4;

    private Texture2D _texture;
    private SpriteBatch _spriteBatch;
    public int ActiveSlot;
    public int SlotSize { get => _texture.Width / SlotNum; }

    public Hotbar(Texture2D texture, GraphicsDevice graphicsDevice) {
        _texture = texture;
        _spriteBatch = new(graphicsDevice);
    }

    public void Draw(GameTime gameTime) {
        _spriteBatch.Begin();
        var x = Game.WindowWidth / 2 - _texture.Width / 2;
        _spriteBatch.Draw(_texture, new Vector2(Game.WindowWidth / 2 - _texture.Width / 2, 0), Color.White);

        // 0.00 - red 1f, green 0f
        // 0.50 - red 1f, green 1f
        // 1.00 - red 0f, green 1f
        var percentCharged = Game.Player.TimeSinceBowShot / Player.ArrowCooldown;
        var chargeColor = new Color(percentCharged * 2 < 1f ? 1f : 1f - percentCharged + 0.5f, percentCharged * 2 < 1f ? percentCharged * 2 : 1f, 0f);
        _spriteBatch.FillRectangle(x, _texture.Height, percentCharged * _texture.Width, 5, chargeColor);
        
        _spriteBatch.FillRectangle(new RectangleF(x + ActiveSlot * SlotSize, 0, SlotSize, SlotSize), Color.White * 0.5f);
        _spriteBatch.End();
    }

    public InputListener[] CreateInputListeners() {
        var keyListener = new KeyboardListener();
        keyListener.KeyPressed += (sender, args) => {
            if (args.Key > Keys.D0 && args.Key <= Keys.D9) {
                ActiveSlot = args.Key - Keys.D1;
            }
        };
        
        var mouseListener = new MouseListener();
        mouseListener.MouseWheelMoved += (sender, args) => {
            if (args.ScrollWheelDelta > 0) {
                ActiveSlot++;
                ActiveSlot %= SlotNum;
            }
            else if (args.ScrollWheelDelta < 0) {
                ActiveSlot--;
                if (ActiveSlot < 0) {
                    ActiveSlot = SlotNum - 1;
                }
            }
        };

        return new InputListener[] { keyListener, mouseListener };
    }
}