using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;
using SideBridge.Components;

namespace SideBridge.Systems;

public class RenderSystem : EntityDrawSystem {

    private GraphicsDevice _graphics;
    private SpriteBatch _spriteBatch;

    private ComponentMapper<Player> _entityMapper;

    public RenderSystem(GraphicsDevice graphicsDevice) 
        : base(Aspect.One(AbstractEntity.SubTypes)) {
            _graphics = graphicsDevice;
            _spriteBatch = new(graphicsDevice);
        }

    public override void Initialize(IComponentMapperService mapperService) {
        _entityMapper = mapperService.GetMapper<Player>();
        
    }
    public override void Draw(GameTime gameTime) {
        _spriteBatch.Begin();
        foreach (var entityInt in ActiveEntities) {
            var entity = _entityMapper.Get(entityInt);
            _spriteBatch.Draw(entity.Sprite, new Vector2(entity.Bounds.X, entity.Bounds.Y), Color.White);
        }
        _spriteBatch.End();
    }
}