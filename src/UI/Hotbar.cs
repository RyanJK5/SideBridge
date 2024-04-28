using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SideBridge;

public class Hotbar : UI {

    public const int SlotCount = 4;

    public const int HotbarDisabled = -1;
    public const int SwordSlot = 0;
    public const int BowSlot = 1;
    public const int BlockSlot = 2;
    public const int AppleSlot = 3;

    public int SlotSize { get => Texture.Width / SlotCount; }

    private readonly Player _player;

    public Hotbar(Player player, Texture2D texture, Vector2 drawPos) : 
        base(texture, drawPos, GameState.InGame) => _player = player
    ;

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
        spriteBatch.FillRectangle(new RectangleF(DrawPos.X + _player.ActiveSlot * SlotSize, DrawPos.Y, SlotSize, SlotSize), Color.White * 0.5f);
    }
}