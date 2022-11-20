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

    public static bool IntersectsLine(this RectangleF rect, Vector2 pointA, Vector2 pointB, out Vector2 intersectionPoint) {
        LineF line1 = new LineF(pointA, pointB);

        LineF[] rectCorners = {
            new LineF(rect.TopLeft, rect.TopRight),
            new LineF(rect.TopRight, rect.BottomRight),
            new LineF(rect.BottomRight, rect.BottomLeft),
            new LineF(rect.BottomLeft, rect.TopLeft)
        };

        foreach (var line2 in rectCorners) {
            if (line1.Intersects(line2, out intersectionPoint)) {
                return true;
            }
        }
        intersectionPoint = new(-1, -1);
        return false;
    }

    private struct LineF {
        public Vector2 A;
        public Vector2 B;

        public LineF(Vector2 a, Vector2 b) {
            A = a;
            B = b;
        }

        public bool Intersects(LineF o, out Vector2 intersectionPoint) {
            float s1X = B.X - A.X;     
            float s1Y = B.Y - A.Y;
            float s2X = o.B.X - o.A.X;
            float s2Y = o.B.Y - o.A.Y;

            float s = (-s1Y * (A.X - o.A.X) + s1X * (A.Y - o.A.Y)) / (-s2X * s1Y + s1X * s2Y);
            float t = ( s2X * (A.Y - o.A.Y) - s2Y * (A.X - o.A.X)) / (-s2X * s1Y + s1X * s2Y);

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1) {
                intersectionPoint = new(A.X + (t * s1X), A.Y + (t * s1Y));
                return true;
            }
            intersectionPoint = new(-1, 1);
            return false;
        }
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