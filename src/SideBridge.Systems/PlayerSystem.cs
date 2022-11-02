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

            position.X -= args.PenetrationVector.X;
            position.Y -= args.PenetrationVector.Y;
            if (args.PenetrationVector.X > args.PenetrationVector.Y) {
                velocity.DirX = 0;
            }
            else if (args.PenetrationVector.Y > args.PenetrationVector.X) {
                velocity.DirY = 0;
            }
            else if (args.PenetrationVector == Vector2.Zero) {
                var intersection = collider.RectBounds.Intersection((RectangleF) args.Other.Bounds);
                Console.WriteLine(intersection);
                    // if (collider.RectBounds.X < args.Other.Bounds.Position.X) {
                    //     position.X -= intersection.Width;
                    // }
                    // else {
                    //     position.X += intersection.Width;
                    // }
                    if (collider.RectBounds.Y < args.Other.Bounds.Position.Y) {
                        position.Y -= intersection.Height;
                    }
                    else {
                        position.Y += intersection.Height;
                    }
                // else {
                //     if (lastVector.Y > 0) {
                //         position.Y -= intersection.Height;
                //     }
                //     else {
                //         position.Y += intersection.Height;
                //     }
                // }
            }
            collider.RectBounds.X = position.X;
            collider.RectBounds.Y = position.Y;
        }
    }

    private void move(GameTime gameTime, int entityID) {
        float speed = HorizontalAcceleration;

        var hitbox = (RectangleF) _colliderMapper.Get(entityID).Bounds;
        var keyInputs = _inputMapper.Get(entityID);
        var velocity = _velocityMapper.Get(entityID);
        var position = _positionMapper.Get(entityID);
        updateVelocity(velocity, 0, VerticalAcceleration);
        if (Keyboard.GetState().IsKeyDown(keyInputs[PlayerAction.Jump])) {
            velocity.DirY = -12.5f;
        }
        if (Keyboard.GetState().IsKeyDown(keyInputs[PlayerAction.Sprint])) {
            speed *= 2;
        }
        if (Keyboard.GetState().IsKeyDown(keyInputs[PlayerAction.Left])) {
            if (!Keyboard.GetState().IsKeyDown(keyInputs[PlayerAction.Right])) {
                updateVelocity(velocity, -speed, 0);
                _keyListener.KeyReleased += (sender, args) => {
                    if (args.Key == keyInputs[(int) PlayerAction.Left]) {
                        updateVelocity(velocity, speed, 0, 0, 0);
                    }
                };
            }
            else {
                updateVelocity(velocity, speed, 0, 0, 0);
            }
        }
        if (Keyboard.GetState().IsKeyDown(keyInputs[PlayerAction.Right])) {
            if (!Keyboard.GetState().IsKeyDown(keyInputs[PlayerAction.Left])) {
                updateVelocity(velocity, speed, 0);
                _keyListener.KeyReleased += (sender, args) => {
                    if (args.Key == keyInputs[PlayerAction.Right]) {
                        updateVelocity(velocity, -speed, 0, 0, 0);
                    }
                };
            }
            else {
                updateVelocity(velocity, -speed, 0, 0, 0);
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
        if (position.Y < 0) {
            position.Y = 0;
        }
        else if (position.Y > Game.Main.MapHeight - hitbox.Height) {
            position.Y = Game.Main.MapHeight - hitbox.Height;
        }
    }

    private (float xDif, float yDif) updateVelocity(Velocity velocity, float x, float y, float xLimit, float yLimit) {
        var origVec = new Velocity { DirX = velocity.DirX, DirY = velocity.DirY };
        
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

        return (Math.Abs(velocity.DirX - origVec.DirX), Math.Abs(velocity.DirY - origVec.DirY));
    }

    private (float xDif, float yDif) updateVelocity(Velocity velocity, float x, float y) => 
        updateVelocity(velocity, x, y, MaximumHorizontalVelocity, MaximumVerticalVelocity);
}