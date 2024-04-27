using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Collections;

namespace SideBridge;

#nullable enable

public class EntityWorld : IDrawable, IUpdatable {

    private readonly Bag<Entity> s_entities;
    private readonly SpriteBatch _spriteBatch;
    
    public EntityWorld(GraphicsDevice graphicsDevice) {
        s_entities = new();
        _spriteBatch = new(graphicsDevice);
    }

    public void Update(GameTime gameTime) {
        foreach (Entity entity in s_entities) {
            entity.Update(gameTime);
        }
        foreach (Entity entity in s_entities) {
            foreach (Entity oEntity in s_entities) {
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
        foreach (Entity entity in s_entities) {
            entity.Draw(_spriteBatch);
        }
        _spriteBatch.End();
    }

    public void Add(Entity entity) => s_entities.Add(entity);
    public void Remove(Entity entity) => s_entities.Remove(entity);
    public bool Contains(Entity entity) => s_entities.Contains(entity);

    public T? FindEntity<T>(Predicate<T> testCase) {
        foreach (Entity entity in s_entities) {
            if (entity is T result && testCase.Invoke(result)) {
                return result;
            }
        }
        return default;
    }
}