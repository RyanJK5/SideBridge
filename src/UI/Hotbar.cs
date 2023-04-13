using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input.InputListeners;

namespace SideBridge;

public class Hotbar : UI {

    static int i;
    int thisI;

    private const int SlotNum = 4;
    public int ActiveSlot;
    public int SlotSize { get => Texture.Width / SlotNum; }

    public Hotbar(Texture2D texture, Vector2 drawPos) : base(texture, drawPos) { i++; thisI = i; }

    public override void Draw(SpriteBatch spriteBatch) {
        spriteBatch.Draw(Texture, DrawPos, Color.White);

        var offset = (DrawPos.Y + Texture.Height >= Game.WindowHeight) ? -5 : Texture.Height;
        spriteBatch.DrawPercentageBar(new RectangleF(DrawPos.X, DrawPos.Y + offset, Texture.Width, 5), 
            Game.Player1.TimeSinceBowShot / Player.ArrowCooldown);
        spriteBatch.FillRectangle(new RectangleF(DrawPos.X + ActiveSlot * SlotSize, DrawPos.Y, SlotSize, SlotSize), Color.White * 0.5f);
    }

    public InputListener[] CreateInputListeners() {
        if (thisI <= 1) return new InputListener[0];
        var keyListener = new KeyboardListener();
        keyListener.KeyPressed += (sender, args) => {
            if (args.Key > Keys.D0 && args.Key <= Keys.D9) {
                ActiveSlot = args.Key - Keys.D1;
                if (ActiveSlot > SlotNum) {
                    ActiveSlot = SlotNum;
                }
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