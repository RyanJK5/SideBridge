using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;

namespace SideBridge;

public static class IShapeFExtended {

    public static void Rotate(this IShapeF shape, float radians) {
        Vector2 pos = shape.Position;
        pos.Rotate(radians);
        shape.Position = pos;
    }

    private class RotatedShapeF : IShapeF {
        public Point2 Position { get; set; }

        public RotatedShapeF(Vector2 pos) =>
            Position = pos.ToPoint();
    }
}