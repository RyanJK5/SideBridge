using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using System;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended.Sprites;

namespace SideBridge;

#nullable enable
public class TiledWorld : IDrawable, IUpdatable {
    private readonly TileSet _tileSet;
    private readonly SpriteBatch _spriteBatch;

    public readonly int Width;
    public readonly int Height;
    public int WidthInPixels { get => Width * _tileSet.TileSize; }
    public int HeightInPixels { get => Height * _tileSet.TileSize; }
    public int TileSize { get => _tileSet.TileSize; }

    private readonly Tile[] _initTileGrid;
    private Tile[] _tileGrid;

    public TiledWorld(GraphicsDevice graphicsDevice, TileSet tileSet, int width, int height) {
        Width = width;
        Height = height;
        _tileSet = tileSet;
        _spriteBatch = new(graphicsDevice);
        _initTileGrid = new Tile[width * height];
        _tileGrid = new Tile[width * height];
    }
    
    public void Draw(SpriteBatch spriteBatch) {
        _spriteBatch.Begin(transformMatrix: Game.GameCamera.GetViewMatrix());

        foreach (var tile in _tileGrid) {
            if (tile.Type == TileType.Air) {
                continue;
            }
            var blockIndex = (int) tile.Type - 1;
            var tilePos = tile.Bounds.Position;
            _spriteBatch.Draw(
                _tileSet.TileImage,
                tilePos,
                new Rectangle((blockIndex + _tileSet.Width) % _tileSet.Width * TileSize, 
                blockIndex / _tileSet.Width * TileSize, TileSize, TileSize), 
                Color.White
            );

            if (tile.Durability < Tile.MaxDurability) {
                var rectTop = tile.Bounds.Bottom - (float) (tile.Durability + 1) / Tile.MaxDurability * this.TileSize;
                _spriteBatch.FillRectangle(
                    tilePos.X, rectTop, TileSize, tilePos.Y + tile.Bounds.Height - rectTop,
                    Color.White * 0.5f);
            }
        }

        _spriteBatch.End();
    }

    public void Update(GameTime gameTime) {
        Point2 mousePos = Game.GameCamera.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        if (Mouse.GetState().RightButton == ButtonState.Pressed) {
            foreach (var tile in _tileGrid) {
                if (TileTypes.Solid(tile.Type) && !tile.Bounds.Contains(mousePos) && tile.Durability < Tile.MaxDurability) {
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
        var bounds = entity.Bounds;
        Tile[] possibleCollisions = {
            this[(int) (bounds.Left / TileSize), (int) (bounds.Top / TileSize)],
            this[(int) (bounds.Right / TileSize), (int) (bounds.Top / TileSize)],
            this[(int) (bounds.Left / TileSize), (int) (bounds.Bottom / TileSize)],
            this[(int) (bounds.Right / TileSize), (int) (bounds.Bottom / TileSize)]
        };
        foreach (Tile tile in possibleCollisions) {
            if (tile.Type != TileType.Air && tile.Bounds.Intersects(bounds)) {
                entity.OnTileCollision(tile);
                if (tile.Type == TileType.Goal) {
                    return;
                }
            }
        }
    }

    public Tile this[int row, int col] {
        get {
            if (row < 0) {
                row = 0;
            }
            if (row >= Width) {
                row = Width - 1;
            }
            if (col < 0) {
                col = 0;
            }
            if (col >= Height) {
                col = Height - 1;
            }
            return _tileGrid[row + Width * col];
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

    public void SetTile(TileType type, int x, int y) {
        int tileSize = _tileSet.TileSize;
        var tile = new Tile(type, new(x * tileSize, y * tileSize, tileSize, tileSize));
        _tileGrid[x + Width * y] = tile;
    }

    public void SetTileWithEffects(TileType type, float x, float y) {
        int intX = (int) (x / TileSize);
        int intY = (int) (y / TileSize);
        if (intX < 0 || intY < 0 || intX >= Width || intY >= Height) {
            return;
        }
        if (type == TileType.Air) {
            Game.ParticleEffectHandler.SpawnParticles(
                GetTile(x, y).Type, 
                Game.GameCamera.WorldToScreen(new Vector2(x + TileSize / 2, y + TileSize / 2))
            );
        }
        else if (TileTypes.Breakable(type)) {
            Game.SoundEffectHandler.PlaySound(SoundEffects.GetRandomBlockSound());
        }
        SetTile(type, intX, intY);
    }

    public void DamageTile(Tile tile) {
        if (!TileTypes.Breakable(tile.Type)) {
            return;
        }
        if (tile.Durability == Tile.MaxDurability) {
            SoundEffectID walkingID = SoundEffects.GetRandomWalkSound();
            SoundEffectInstance instance = Game.SoundEffectHandler.CreateInstance(walkingID);
            instance.Volume = 0.5f;
            instance.Pitch = -0.5f;
            instance.Play();
        }
        tile.Durability--;
        if (tile.Durability <= 0) {
            SetTileWithEffects(TileType.Air, tile.Bounds.X, tile.Bounds.Y);
            Game.SoundEffectHandler.PlaySound(SoundEffects.GetRandomBlockSound());
        }
    }

    public Tile GetTile(float x, float y) {
        if (x > WidthInPixels || x < 0 || y > HeightInPixels || y < 0) {
            return new Tile(TileType.Air, new((int) (x / TileSize) * TileSize, (int) (y / TileSize) * TileSize, TileSize, TileSize));
        }
        return this[(int) (x / TileSize), (int) (y / TileSize)];
    }

    public void LoadMap(ContentManager loader, WorldType type) {
        var tiledMap = loader.Load<TiledMap>("map" + (int) type);
        var tiledMapTiles = tiledMap.TileLayers[0].Tiles;
        var tileSize = _tileSet.TileSize;
        for (var i = 0; i < _tileGrid.Length; i++) {
            var tiledMapTile = tiledMapTiles[i];
            var tile = new Tile((TileType) tiledMapTile.GlobalIdentifier, 
                new(tiledMapTile.X * tileSize, tiledMapTile.Y * tileSize, tileSize, tileSize));
            _tileGrid[i] = tile;
            _initTileGrid[i] = tile;
        }
    }

    public void Reset() => _tileGrid = _initTileGrid;
}