using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Collisions;

namespace SideBridge;

public class TiledWorld : SimpleDrawableGameComponent {
    private readonly TileSet _tileSet;
    private readonly SpriteBatch _spriteBatch;
    
    public readonly int Width;
    public readonly int Height;
    public int WidthInPixels { get => Width * _tileSet.TileSize; }
    public int HeightInPixels { get => Height * _tileSet.TileSize; }
    public int TileSize { get => _tileSet.TileSize; }

    private Tile[] _tileGrid;

    public TiledWorld(GraphicsDevice graphicsDevice, TileSet tileSet, int width, int height) {
        Width = width;
        Height = height;
        _tileSet = tileSet;
        _spriteBatch = new(graphicsDevice);
        _tileGrid = new Tile[width * height];
    }
    
    public override void Draw(GameTime gameTime) {
        _spriteBatch.Begin();

        int tileSize = _tileSet.TileSize;
        foreach (var tile in _tileGrid) {
            if (tile.Type != BlockType.Air) {
                var blockIndex = (int) tile.Type - 1;
                if (tile.Type == BlockType.DarkBlue) {
                }
                _spriteBatch.Draw(
                    _tileSet.TileImage,
                    tile.Bounds.Position,
                    new Rectangle((blockIndex + _tileSet.Width) % _tileSet.Width * tileSize, 
                    blockIndex / _tileSet.Width * tileSize, tileSize, tileSize), 
                    Color.White);
            }
        }

        _spriteBatch.End();
    }

    public override void Update(GameTime gameTime) { }

    public BlockType this[int x, int y] {
        get {
            return _tileGrid[x + Width * y].Type;
        }
        set {
            int tileSize = _tileSet.TileSize;
            var tile = new Tile(value, new(x * tileSize, y * tileSize, tileSize, tileSize));
            _tileGrid[x + Width * y] = tile;
            Game.AddTile(tile);
        }
    }

    public void LoadMap(ContentManager content, WorldType type) {
        var tiledMap = content.Load<TiledMap>("map" + (int) type);
        var tiledMapTiles = tiledMap.TileLayers[0].Tiles;
        var tileSize = _tileSet.TileSize;
        for (var i = 0; i < tiledMapTiles.Length; i++) {
            var tiledMapTile = tiledMapTiles[i];
            var tile = new Tile((BlockType) tiledMapTile.GlobalIdentifier, 
                new(tiledMapTile.X * tileSize, tiledMapTile.Y * tileSize, tileSize, tileSize));
            _tileGrid[i] = tile;
        }
    }

    public void InsertTiles(CollisionComponent collisionComponent) {
        foreach (Tile tile in _tileGrid) {
            if (tile.Type != BlockType.Air) {
                collisionComponent.Insert(tile);
            }
        }
    }
}