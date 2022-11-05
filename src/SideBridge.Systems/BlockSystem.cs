using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;
using SideBridge.Components;


namespace SideBridge.Systems;

public class BlockSystem : EntityUpdateSystem {

    private ComponentMapper<PlayerCollider> _colliderMapper;
    private Dictionary<TiledMapTile, int> _tileDamage;

    private const int TileDurability = 10;
    private const int TileSize = 40;
    private const float TileReach = 3.5f;
    private const int HeightLimit = 4;
    private const int IslandWidths = 10;

    public BlockSystem() : base(Aspect.All(typeof(Input), typeof(Position), typeof(PlayerCollider))) {
        _tileDamage = new();
    }

    public override void Initialize(IComponentMapperService mapperService) {
        _colliderMapper = mapperService.GetMapper<PlayerCollider>();
    }

    public override void Update(GameTime gameTime) {
        Vector2 mousePos = Game.Main.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        foreach (var entity in ActiveEntities) {
            var collider = _colliderMapper.Get(entity);
            var tileX = (int) (mousePos.X / TileSize) * TileSize;
            var tileY = (int) (mousePos.Y / TileSize) * TileSize;
            bool inRange = Vector2.Distance(new(tileX, tileY), collider.RectBounds.Position) < TileReach * TileSize && 
                tileY / TileSize >= HeightLimit && tileX / TileSize > IslandWidths - 1 && tileX / TileSize < Game.Main.TiledMap.Width - IslandWidths;
            if (Mouse.GetState().LeftButton == ButtonState.Pressed && inRange && Game.Main.GetTile(mousePos.X, mousePos.Y).IsBlank) {
                if (new RectangleF(tileX, tileY, TileSize, TileSize).Intersects(collider.RectBounds)) {
                    continue;
                }

                Vector2[] adjacentTiles = {
                    new(-TileSize, 0),
                    new(0, -TileSize),
                    new(TileSize, 0),
                    new(0, TileSize)
                };
                foreach (var vec in adjacentTiles) {
                    if (mousePos.X + vec.X > Game.Main.MapWidth || mousePos.X + vec.X < 0 || 
                        mousePos.Y + vec.Y > Game.Main.MapHeight || mousePos.Y + vec.Y < 0) {
                        continue;
                    }
                    if (!Game.Main.GetTile(mousePos.X + vec.X, mousePos.Y + vec.Y).IsBlank) {
                        Game.Main.SetTile(mousePos.X, mousePos.Y, tileY / TileSize == HeightLimit ? BlockType.DarkBlue : BlockType.Blue);
                        break;
                    }
                }
            }
            else if (Mouse.GetState().RightButton == ButtonState.Pressed && inRange) {
                damageTile(Game.Main.GetTile(mousePos.X, mousePos.Y));
            }
        }

    }

    private void damageTile(TiledMapTile tile) {
        if (!Blocks.Breakable((BlockType) tile.GlobalIdentifier)) {
            return;
        }
        if (_tileDamage.ContainsKey(tile)) {
            _tileDamage[tile]--;
        }
        else {
            _tileDamage.Add(tile, TileDurability - 1);
        }
        if (_tileDamage[tile] <= 0) {
            Game.Main.SetTile(tile.X * TileSize, tile.Y * TileSize, BlockType.Air);
        }
    }
}