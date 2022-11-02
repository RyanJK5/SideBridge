using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using MonoGame.Extended.Entities;
using MonoGame.Extended.ViewportAdapters;
using MonoGame.Extended.Entities.Systems;
using SideBridge.Components;

namespace SideBridge.Systems;

public class RenderSystem : EntityUpdateSystem, IDrawSystem {

    public readonly TiledMap TiledMap;
    private readonly TiledMapRenderer _tiledMapRenderer;

    private readonly SpriteBatch _spriteBatch;
    private readonly OrthographicCamera _camera;

    private ComponentMapper<Position> _positionMapper;
    private ComponentMapper<Sprite> _spriteMapper;

    public RenderSystem(GameWindow window, GraphicsDevice graphicsDevice, TiledMap tiledMap) 
        : base(Aspect.All(typeof(Position), typeof(Sprite))) {
        _spriteBatch = new(graphicsDevice);
        var viewportAdapter = new BoxingViewportAdapter(window, graphicsDevice, 1920, 1080);
        _camera = new(viewportAdapter);
        _camera.Move(new(0, -7 * 40));
        TiledMap = tiledMap;
        _tiledMapRenderer = new(graphicsDevice, tiledMap);
        for (ushort x = 0; x < TiledMap.Width; x++) {
            for (ushort y = 0; y < TiledMap.Height; y++) {
                if (!TiledMap.TileLayers[0].GetTile(x, y).IsBlank) {
                    Game.Main.CollisionComponent.Insert(new StaticCollider(
                        new(x * TiledMap.TileWidth, y * TiledMap.TileHeight, TiledMap.TileWidth, TiledMap.TileHeight)));
                }
            }
        }
    }

    public void Draw(GameTime gameTime) {
        _tiledMapRenderer.Draw(_camera.GetViewMatrix());
        _spriteBatch.Begin();

        var transformedBounds = new Rectangle((int) _camera.Position.X, (int) _camera.Position.Y, Game.Main.WindowWidth, Game.Main.WindowHeight);
        foreach (var entityID in ActiveEntities) {
            var sprite = _spriteMapper.Get(entityID);
            var position = _positionMapper.Get(entityID);
            _spriteBatch.Draw(sprite.Texture, _camera.WorldToScreen(position.X, position.Y), Color.White);
        }
        _spriteBatch.End();
    }

    public override void Initialize(IComponentMapperService mapperService) {
        _positionMapper = mapperService.GetMapper<Position>();
        _spriteMapper = mapperService.GetMapper<Sprite>();
    }

    public override void Update(GameTime gameTime) {
        _tiledMapRenderer.Update(gameTime);
        updateCamera();
    }

    private void updateCamera() { }
}