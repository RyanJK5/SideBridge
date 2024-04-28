using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SideBridge;

public class HealthBar : UI {

    private Texture2D _emptyTexture;
    private Texture2D _bonusTexture;

    private readonly Player _player;
    
    public int CellWidth { get => _emptyTexture.Width / 20; }

    public HealthBar(Player player, Texture2D fullTexture, Texture2D emptyTexture, Texture2D bonusTexture, 
        Vector2 drawPos) : base(fullTexture, drawPos, true) {
        _player = player;
        _emptyTexture = emptyTexture;
        _bonusTexture = bonusTexture;
    }

    public override void Draw(SpriteBatch spriteBatch) {
        var playerHealth = _player.Health;
        if (playerHealth >= 20) {
            spriteBatch.Draw(Texture, DrawPos, Color.White);
            if (playerHealth > 20) {
                spriteBatch.Draw(
                    _bonusTexture,
                    DrawPos,
                    new Rectangle(0, 0, (int) (CellWidth * (playerHealth - 20)) + 1, Texture.Height),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    Vector2.One,
                    SpriteEffects.None,
                    0f
                );
            }
            return;
        }
        spriteBatch.Draw(_emptyTexture, DrawPos, Color.White);
        spriteBatch.Draw(
            Texture,
            DrawPos,
            new Rectangle(0, 0, (int) (CellWidth * playerHealth) + 1, Texture.Height),
            Color.White,
            0f,
            Vector2.Zero,
            Vector2.One,
            SpriteEffects.None,
            0f
        );
    }

}