using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.Collisions;
using SideBridge.Components;


namespace SideBridge.Systems;

public class BlockSystem : EntityUpdateSystem {

    private ComponentMapper<Position> _positionMapper;

    public BlockSystem() : base(Aspect.All(typeof(Input), typeof(Position))) { }

    public override void Initialize(IComponentMapperService mapperService) {
        _positionMapper = mapperService.GetMapper<Position>();
    }

    public override void Update(GameTime gameTime) {
        const int TileSize = 40;
        foreach (var entity in ActiveEntities) {
            if (Mouse.GetState().LeftButton == ButtonState.Pressed) {
                var position = _positionMapper.Get(entity);
                Vector2 mousePos = Game.Main.ScreenToWorld(Mouse.GetState().Position.ToVector2());
                
                if (Vector2.Distance(mousePos, (Vector2) position) < 5 * TileSize) {
                    Vector2[] adjacentTiles = {
                        new(-TileSize, 0),
                        new(0, -TileSize),
                        new(TileSize, 0),
                        new(0, TileSize)
                    };
                    foreach (var vec in adjacentTiles) {
                        if (!Game.Main.GetTile(mousePos.X + vec.X, mousePos.Y + vec.Y).IsBlank) {
                            Game.Main.SetTile(mousePos.X, mousePos.Y, BlockType.Blue);
                            break;
                        }
                    }
                }
            }
        }
    }
}