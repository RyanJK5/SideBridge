using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SideBridge;

public class HealthBar : UI {

    private Texture2D _emptyTexture;
    private Texture2D _bonusTexture;
    
    public int CellWidth { get => _emptyTexture.Width / 20; }

    public HealthBar(Texture2D fullTexture, Texture2D emptyTexture, Texture2D bonusTexture) : base(fullTexture) {
        _emptyTexture = emptyTexture;
        _bonusTexture = bonusTexture;
    }

    public override void Draw(SpriteBatch spriteBatch) {
        var playerHealth = Game.Player.Health;
        if (playerHealth >= 20) {
            spriteBatch.Draw(_texture, new Vector2(10, 10), Color.White);
            if (playerHealth > 20) {
                spriteBatch.Draw(
                    _bonusTexture,
                    new Vector2(10, 10),
                    new Rectangle(0, 0, (int) (CellWidth * (playerHealth - 20)) + 1, _texture.Height),
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
        spriteBatch.Draw(_emptyTexture, new Vector2(10, 10), Color.White);
        spriteBatch.Draw(
            _texture,
            new Vector2(10, 10),
            new Rectangle(0, 0, (int) (CellWidth * playerHealth) + 1, _texture.Height),
            Color.White,
            0f,
            Vector2.Zero,
            Vector2.One,
            SpriteEffects.None,
            0f
        );
    }
}