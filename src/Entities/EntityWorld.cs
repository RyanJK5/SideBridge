using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Collections;
using MonoGame.Extended;

namespace SideBridge;

public class EntityWorld : SimpleDrawableGameComponent {

    private readonly Bag<Entity> _entities;
    private readonly SpriteBatch _spriteBatch;
    
    public EntityWorld(GraphicsDevice graphicsDevice) {
        _entities = new();
        _spriteBatch = new(graphicsDevice);
    }

    public override void Update(GameTime gameTime) {
        foreach (Entity entity in _entities) {
            entity.Update(gameTime);
        }
    }

    public override void Draw(GameTime gameTime) {
        _spriteBatch.Begin(transformMatrix: Game.GetViewMatrix());
        foreach (Entity entity in _entities) {
            entity.Draw(_spriteBatch);
        }
        _spriteBatch.End();
    }

    public void Add(Entity entity) => _entities.Add(entity);
    public void Remove(Entity entity) => _entities.Remove(entity);
    public bool Contains(Entity entity) => _entities.Contains(entity);

}