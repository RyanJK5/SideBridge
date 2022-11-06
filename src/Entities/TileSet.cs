using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Collections;
using MonoGame.Extended;

namespace SideBridge;

public class TileSet {
    public readonly Texture2D TileImage;
    public readonly int Width;
    public readonly int Height;
    public int TileSize { get => TileImage.Width / Width; }
    public int Length { get => Width * Height; }

    public TileSet(Texture2D tileImage, int width, int height) {
        TileImage = tileImage;
        Width = width;
        Height = height;
    }
}