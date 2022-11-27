using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SideBridge;

public class ScoreBar : UI {

    private const int TimerWidth = 48;

    public int RedScore = 2;
    public int BlueScore = 3;

    public int CellWidth { get => (Texture.Width - TimerWidth) / 10; }

    private readonly Texture2D _emptyTexture;

    public ScoreBar(Texture2D fullTexture, Texture2D emptyTexture, Vector2 drawPos) : base(fullTexture, drawPos) {
        _emptyTexture = emptyTexture;
    }

    public override void Draw(SpriteBatch spriteBatch) {
        spriteBatch.Draw(_emptyTexture, DrawPos, Color.White);
        if (RedScore > 0) {
            spriteBatch.Draw(
                Texture,
                DrawPos,
                new Rectangle(0, 0, (int) (CellWidth * RedScore) + 1, Texture.Height),
                Color.White,
                0f,
                Vector2.Zero,
                Vector2.One,
                SpriteEffects.None,
                0f
            );
        }
        if (BlueScore > 0) {
            var scoreWidth = (int) (CellWidth * (5 - BlueScore));
            var scoreX = Texture.Width / 2 + TimerWidth / 2 + scoreWidth;
            spriteBatch.Draw(
                Texture,
                new Vector2(DrawPos.X + scoreX, DrawPos.Y),
                new Rectangle(scoreX, 0, Texture.Width - scoreX, Texture.Height),
                Color.White,
                0f,
                Vector2.Zero,
                Vector2.One,
                SpriteEffects.None,
                0f
            );
        }
    }
}