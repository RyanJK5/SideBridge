using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.Collisions;
using SideBridge.Components;


namespace SideBridge.Systems;

public class PlayerSystem : EntityProcessingSystem {
    private ComponentMapper<Input> _inputMapper;
    private ComponentMapper<Position> _positionMapper;
    private ComponentMapper<Velocity> _velocityMapper;
    private ComponentMapper<PlayerCollider> _colliderMapper;
    private KeyboardListener _keyListener;

    private Dictionary<int, Vector2> _entityIdToLastPenetrationVector;

    private const float MaximumVerticalVelocity = 15f;
    private const float VerticalAcceleration = 1f;
    private const float MaximumHorizontalVelocity = 10f;
    private const float HorizontalAcceleration = 1f;

    public PlayerSystem()
        : base(Aspect.All(typeof(Input), typeof(Position), typeof(Velocity), typeof(PlayerCollider))) {
        _keyListener = new();
        _entityIdToLastPenetrationVector = new();
        Game.Main.Components.Add(new InputListenerComponent(Game.Main, _keyListener));
    }

    public override void Initialize(IComponentMapperService mapperService) {
        _inputMapper = mapperService.GetMapper<Input>();
        _positionMapper = mapperService.GetMapper<Position>();
        _velocityMapper = mapperService.GetMapper<Velocity>();
        _colliderMapper = mapperService.GetMapper<PlayerCollider>();
    }

    public override void Process(GameTime gameTime, int entityId) {
        move(gameTime, entityId);
        resetPositions(entityId);
        _colliderMapper.Get(entityId).RectBounds.Position = (Vector2) _positionMapper.Get(entityId);
        if (!_entityIdToLastPenetrationVector.ContainsKey(entityId)) {
            _colliderMapper.Get(entityId).OnPlayerCollision += (sender, args) => onCollision(entityId, sender, args);
            _entityIdToLastPenetrationVector.Add(entityId, new());
        }
    }

    private void onCollision(int entityID, object sender, CollisionEventArgs args) {
        if (args.Other is StaticCollider) {
            var velocity = _velocityMapper.Get(entityID);
            var position = _positionMapper.Get(entityID);
            var collider = _colliderMapper.Get(entityID);
            var colliderBounds = collider.RectBounds;
            var otherBounds = (RectangleF) args.Other.Bounds;
            var intersection = collider.RectBounds.Intersection((RectangleF) args.Other.Bounds);

            var adj = colliderBounds.Width / otherBounds.Height;
            var adj2 = otherBounds.Height / colliderBounds.Width;
            if (intersection.Height * adj2 > intersection.Width) {
                if (colliderBounds.X < otherBounds.Position.X) {
                    position.X -= intersection.Width;
                }
                else {
                    position.X += intersection.Width;
                }
                velocity.DirX = 0;
            }
            else {
                if (colliderBounds.Y < otherBounds.Y) { 
                    position.Y -= intersection.Height;
                }
                else {
                    position.Y += intersection.Height;
                }
                velocity.DirY = 0;
            }
            collider.RectBounds.X = position.X;
            collider.RectBounds.Y = position.Y;
        }
    }

    private void move(GameTime gameTime, int entityID) {
        float speed = HorizontalAcceleration; // 1f
        var hitbox = (RectangleF) _colliderMapper.Get(entityID).Bounds;
        var keyInputs = _inputMapper.Get(entityID);
        var velocity = _velocityMapper.Get(entityID);
        var position = _positionMapper.Get(entityID);

        if  (hitbox.Bottom < Game.Main.MapHeight && hitbox.Bottom > 0 && (
            !Game.Main.GetTile(position.X, hitbox.Bottom).IsBlank ||
            !Game.Main.GetTile(hitbox.Right - 1, hitbox.Bottom).IsBlank
            )) {
            if (Keyboard.GetState().IsKeyDown(keyInputs[PlayerAction.Jump])) {
                velocity.DirY = -11f;
            }
        }
        else {
            updateVelocity(velocity, 0, VerticalAcceleration);
        }

        if (Keyboard.GetState().IsKeyDown(keyInputs[PlayerAction.Sprint])) {
            speed *= 2;
        }

        bool leftDown = Keyboard.GetState().IsKeyDown(keyInputs[PlayerAction.Left]);
        bool rightDown = Keyboard.GetState().IsKeyDown(keyInputs[PlayerAction.Right]);
        if (leftDown && !(rightDown && velocity.DirX <= 0)) {
            updateVelocity(velocity, -speed, 0);
        }
        else if (!leftDown && velocity.DirX <= 0) {
            updateVelocity(velocity, speed, 0, 0, MaximumVerticalVelocity);
        }
        if (rightDown && !(leftDown && velocity.DirX >= 0)) {
            updateVelocity(velocity, speed, 0);
        }
        else if (!rightDown && velocity.DirX >= 0) {
            updateVelocity(velocity, -speed, 0, 0, MaximumVerticalVelocity);
        }
        if (!leftDown && !rightDown && velocity.DirX != 0) {
            if (velocity.DirX < 0) {
                updateVelocity(velocity, speed, 0, 0, MaximumVerticalVelocity);
            }
            else {
                updateVelocity(velocity, -speed, 0, 0, MaximumVerticalVelocity);
            }
        }
        position.X += velocity.DirX;
        position.Y += velocity.DirY;
    }

    private void resetPositions(int entityID) {
        var hitbox = _colliderMapper.Get(entityID).RectBounds;
        var position = _positionMapper.Get(entityID);
        
        if (position.X < 0) {
            position.X = 0;
        }
        else if (position.X > Game.Main.MapWidth - hitbox.Width) {
            position.X = Game.Main.MapWidth - hitbox.Width;
        }
        if (position.Y > Game.Main.MapHeight) {
            position.Y = -hitbox.Height * 4;
        }
    }

    private void updateVelocity(Velocity velocity, float x, float y, float xLimit, float yLimit) {
        if ((x >= 0 && velocity.DirX + x < xLimit) || (x < 0 && velocity.DirX + x > -xLimit)) {
            velocity.DirX += x;
        }
        else {
            if (x >= 0) {
                velocity.DirX = xLimit;
            }
            else {
                velocity.DirX = -xLimit;
            }
        }
        
        if ((y >= 0 && velocity.DirY + y < yLimit) || (y < 0 && velocity.DirY + y > -yLimit)) {
            velocity.DirY += y;
        }
        else {
            if (y >= 0) {
                velocity.DirY = yLimit;
            }
            else {
                velocity.DirY = -yLimit;
            }
        }
    }

    private void updateVelocity(Velocity velocity, float x, float y) => 
        updateVelocity(velocity, x, y, MaximumHorizontalVelocity, MaximumVerticalVelocity);
}