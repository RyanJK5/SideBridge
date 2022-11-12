using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SideBridge;

public static class Extensions {

    public static void DrawPercentageBar(this SpriteBatch spriteBatch, Rectangle bounds, float percentCharged) {
        var chargeColor = new Color(percentCharged * 2 < 1f ? 1f : 1f - percentCharged + 0.5f, percentCharged * 2 < 1f ? percentCharged * 2 : 1f, 0f);
        spriteBatch.FillRectangle(bounds.X, bounds.Y, percentCharged * bounds.Width, bounds.Height, chargeColor);
    }
}