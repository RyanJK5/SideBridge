using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;

namespace SideBridge;

#nullable enable
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
        _spriteBatch.Begin(transformMatrix: Game.GetViewMatrix());

        int tileSize = _tileSet.TileSize;
        foreach (var tile in _tileGrid) {
            if (tile.Type != BlockID.Air) {
                var blockIndex = (int) tile.Type - 1;
                var tilePos = tile.Bounds.Position;
                _spriteBatch.Draw(
                    _tileSet.TileImage,
                    tilePos,
                    new Rectangle((blockIndex + _tileSet.Width) % _tileSet.Width * tileSize, 
                    blockIndex / _tileSet.Width * tileSize, tileSize, tileSize), 
                    Color.White);

                if (tile.Durability < Tile.MaxDurability) {
                    var rectTop = tile.Bounds.Bottom - (float) (tile.Durability + 1) / Tile.MaxDurability * TileSize;
                    _spriteBatch.FillRectangle(
                        tilePos.X, rectTop, tileSize, tilePos.Y + tile.Bounds.Height - rectTop,
                        Color.White * 0.5f);
                }
            }
        }

        _spriteBatch.End();
    }

    public override void Update(GameTime gameTime) {
        Point2 mousePos = Game.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        if (Mouse.GetState().RightButton == ButtonState.Pressed) {
            foreach (var tile in _tileGrid) {
                if (tile.Type != BlockID.Air && !tile.Bounds.Contains(mousePos) && tile.Durability < Tile.MaxDurability) {
                    tile.Durability = Tile.MaxDurability;
                }
            }
        }
        else {
            foreach (var tile in _tileGrid) {
                tile.Durability = Tile.MaxDurability;
            }
        }
    }

    public void CheckTileCollisions(Entity entity) {
        var tileSize = _tileSet.TileSize;
        var bounds = entity.Bounds;
        Tile[] possibleCollisions = {
            this[(int) (bounds.Left / tileSize), (int) (bounds.Top / tileSize)],
            this[(int) (bounds.Right / tileSize), (int) (bounds.Top / tileSize)],
            this[(int) (bounds.Left / tileSize), (int) (bounds.Bottom / tileSize)],
            this[(int) (bounds.Right / tileSize), (int) (bounds.Bottom / tileSize)]
        };
        foreach (Tile tile in possibleCollisions) {
            if (tile.Type != BlockID.Air && tile.Bounds.Intersects(bounds)) {
                entity.OnTileCollision(tile);
            }
        }
    }

    public Tile this[int x, int y] {
        get {
            if (x < 0) {
                x = 0;
            }
            if (x >= Width) {
                x = Width - 1;
            }
            if (y < 0) {
                y = 0;
            }
            if (y >= Height) {
                y = Height - 1;
            }
            return _tileGrid[x + Width * y];
        }
    }

    public Tile[] FindTiles(System.Predicate<Tile> testCase) {
        System.Collections.Generic.List<Tile> result = new();
        foreach (var tile in _tileGrid) {
            if (testCase.Invoke(tile)) {
                result.Add(tile);
            }
        }
        return result.ToArray();
    }

    public void SetTile(BlockID type, int x, int y) {
        if (x < 0 || y < 0 || x >= Width || y >= Height) {
            return;
        }
        int tileSize = _tileSet.TileSize;
        var tile = new Tile(type, new(x * tileSize, y * tileSize, tileSize, tileSize));
        var oldTile = _tileGrid[x + Width * y]; 
        _tileGrid[x + Width * y] = tile;
    }

    public void LoadMap(ContentManager content, WorldType type) {
        var tiledMap = content.Load<TiledMap>("map" + (int) type);
        var tiledMapTiles = tiledMap.TileLayers[0].Tiles;
        var tileSize = _tileSet.TileSize;
        for (var i = 0; i < _tileGrid.Length; i++) {
            var tiledMapTile = tiledMapTiles[i];
            var tile = new Tile((BlockID) tiledMapTile.GlobalIdentifier, 
                new(tiledMapTile.X * tileSize, tiledMapTile.Y * tileSize, tileSize, tileSize));
            _tileGrid[i] = tile;
        }
    }
}