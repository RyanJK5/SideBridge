using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;
using SideBridge.Components;

namespace SideBridge.Systems;

public class RenderSystem : EntityDrawSystem {

    private GraphicsDevice _graphics;
    private SpriteBatch _spriteBatch;

    private ComponentMapper<Sprite> _spriteMapper;
    private ComponentMapper<Position> _positionMapper;

    public RenderSystem(GraphicsDevice graphicsDevice) 
        : base(Aspect.All(typeof(Sprite), typeof(Position))) {
            _graphics = graphicsDevice;
            _spriteBatch = new(graphicsDevice);
        }

    public override void Initialize(IComponentMapperService mapperService) {
        _spriteMapper = mapperService.GetMapper<Sprite>();
        _positionMapper = mapperService.GetMapper<Position>();
    }

    public override void Draw(GameTime gameTime) {
        _spriteBatch.Begin();
        foreach (var entityID in ActiveEntities) {
            var sprite = _spriteMapper.Get(entityID);
            var position = _positionMapper.Get(entityID);
            _spriteBatch.Draw(sprite.Texture, new Vector2(position.X, position.Y), Color.White);
        }
        _spriteBatch.End();
    }
}