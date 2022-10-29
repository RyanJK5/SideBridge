#nullable enable 

namespace SideBridge;

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public enum BlockType {
    Air,
    Red,
    Blue
}

public static class Blocks {
    
    private static Dictionary<BlockType, Texture2D?> Textures = new();

    static Blocks() {
        Textures.Add(BlockType.Air, null);
        Textures.Add(BlockType.Red, Game.Main.Content.Load<Texture2D>("redblock"));
        Textures.Add(BlockType.Blue, Game.Main.Content.Load<Texture2D>("blueblock"));
    }

    public static Texture2D? GetTextureFrom(BlockType type) => Textures[type];

    public static void Draw(SpriteBatch spriteBatch, BlockType type, Vector2 position) {
        if (Textures[type] == null) {
            return;
        }
        spriteBatch.Draw(Textures[type], position, Color.White);
    }

    public static bool Solid(BlockType type) => type != BlockType.Air;
}