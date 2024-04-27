using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input.InputListeners;

namespace SideBridge;

public class Hotbar : UI {

    private const int SlotNum = 4;
    public int ActiveSlot;
    public int SlotSize { get => Texture.Width / SlotNum; }

    private Player _player;

    public Hotbar(Texture2D texture, Vector2 drawPos) : base(texture, drawPos) { }

    public override void Draw(SpriteBatch spriteBatch) {
        spriteBatch.Draw(
            Texture, 
            DrawPos,
            new Rectangle(0, _player.Team == Team.Red ? Texture.Height / 2 : 0, Texture.Width, Texture.Height / 2),
            Color.White
        );

        var offset = (DrawPos.Y + Texture.Height / 2 >= Game.GameGraphics.WindowHeight) ? -5 : Texture.Height / 2;
        spriteBatch.DrawPercentageBar(new RectangleF(DrawPos.X, DrawPos.Y + offset, Texture.Width, 5), 
            _player.TimeSinceBow / Player.BowCooldown);
        spriteBatch.FillRectangle(new RectangleF(DrawPos.X + ActiveSlot * SlotSize, DrawPos.Y, SlotSize, SlotSize), Color.White * 0.5f);
    }

    public void SetSlotBind(int slot, Keys key) {
        var keyListener = new KeyboardListener();
        keyListener.KeyPressed += (sender, args) => {
            if (args.Key == key) {
                ActiveSlot = slot;
            }
        };
        Game.GameGraphics.AddListeners(keyListener);
    }

    public InputListener CreateInputListeners() {
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

        return mouseListener;
    }

    public void SetPlayer(Player player) => _player = player;
}