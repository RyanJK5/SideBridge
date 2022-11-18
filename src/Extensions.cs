using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SideBridge;

public static class Extensions {

    public static Vector2 RotatedCopy(this Vector2 point, float angle, Vector2 origin) {
        var x = point.X;
        var y = point.Y;
        var dx = origin.X;
        var dy = origin.Y;
        return new Vector2(
            ((x - dx) * MathF.Cos(angle)) - ((dy - y) * MathF.Sin(angle)) + dx,
            dy - ((dy - y) * MathF.Cos(angle)) + ((x - dx) * MathF.Sin(angle))
        );
    }
    
    public static void DrawPercentageBar(this SpriteBatch spriteBatch, RectangleF bounds, float percentCharged, bool warmColors = true) {
        Color chargeColor;
        if (warmColors) {
            chargeColor = new Color(percentCharged * 2 < 1f ? 1f : 1f - percentCharged + 0.5f, percentCharged * 2 < 1f ? percentCharged * 2 : 1f, 0f);
        }
        else {
            chargeColor = new Color(percentCharged * 2 < 1f ? 1f : 1f - percentCharged + 0.5f, 0f, percentCharged * 2 < 1f ? percentCharged * 2 : 1f);
        }
        spriteBatch.FillRectangle(bounds.X, bounds.Y, percentCharged * bounds.Width, bounds.Height, chargeColor);
    }
}