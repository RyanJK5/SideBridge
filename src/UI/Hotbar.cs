using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input.InputListeners;

namespace SideBridge;

public class Hotbar : UI {

    private const int SlotNum = 4;

    public int ActiveSlot;
    public int SlotSize { get => _texture.Width / SlotNum; }

    public Hotbar(Texture2D texture) : base(texture) { }

    public override void Draw(SpriteBatch spriteBatch) {
        var x = Game.WindowWidth / 2 - _texture.Width / 2;
        spriteBatch.Draw(_texture, new Vector2(Game.WindowWidth / 2 - _texture.Width / 2, 0), Color.White);

        spriteBatch.DrawPercentageBar(new Rectangle(x, _texture.Height, _texture.Width, 5), Game.Player.TimeSinceBowShot / Player.ArrowCooldown);
        spriteBatch.FillRectangle(new RectangleF(x + ActiveSlot * SlotSize, 0, SlotSize, SlotSize), Color.White * 0.5f);
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