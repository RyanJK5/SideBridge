using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Collections;

namespace SideBridge;

#nullable enable

public class EntityWorld : IDrawable, IUpdatable {

    private readonly Bag<Entity> _entities;
    private readonly SpriteBatch _spriteBatch;
    
    public EntityWorld(GraphicsDevice graphicsDevice) {
        _entities = new();
        _spriteBatch = new(graphicsDevice);
    }

    public void Update(GameTime gameTime) {
        foreach (Entity entity in _entities) {
            entity.Update(gameTime);
        }
        foreach (Entity entity in _entities) {
            foreach (Entity oEntity in _entities) {
                if (entity == oEntity) {
                    continue;
                }
                if (entity.Bounds.Intersects(oEntity.Bounds)) {
                    entity.OnCollision(oEntity);
                    oEntity.OnCollision(entity);
                }
            }
            Game.TiledWorld.CheckTileCollisions(entity);
        }
        
    }

    public void Draw(SpriteBatch spriteBatch) {
        _spriteBatch.Begin(transformMatrix: Game.GameCamera.GetViewMatrix());
        foreach (Entity entity in _entities) {
            entity.Draw(_spriteBatch);
        }
        _spriteBatch.End();
    }

    public void Add(Entity entity) => _entities.Add(entity);
    public void Remove(Entity entity) => _entities.Remove(entity);
    public bool Contains(Entity entity) => _entities.Contains(entity);

    public T? Find<T>(Predicate<T> testCase) where T : Entity {
        foreach (Entity entity in _entities) {
            if (entity is T result && testCase.Invoke(result)) {
                return result;
            }
        }
        return default;
    }
}