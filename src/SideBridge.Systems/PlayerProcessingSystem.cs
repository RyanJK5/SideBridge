using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;
using MonoGame.Extended.Input;
using MonoGame.Extended.Input.InputListeners;
using SideBridge.Components;


namespace SideBridge.Systems;

public class PlayerProcessingSystem : EntityProcessingSystem
{
    private ComponentMapper<Hitbox> _hitboxMapper;
    private ComponentMapper<Input> _inputMapper;
    private ComponentMapper<Position> _positionMapper;
    private ComponentMapper<Velocity> _velocityMapper;
    private KeyboardListener _keyListener;

    private const float MaximumVerticalVelocity = 20f;
    private const float VerticalAcceleration = 1f;
    private const float MaximumHorizontalVelocity = 10f;
    private const float HorizontalAcceleration = 1f;

    public PlayerProcessingSystem()
        : base(Aspect.All(typeof(Hitbox), typeof(Input), typeof(Position), typeof(Velocity))) {
        _keyListener = new();
        Game.Main.Components.Add(new InputListenerComponent(Game.Main, _keyListener));
    }

    public override void Initialize(IComponentMapperService mapperService) {
        _hitboxMapper = mapperService.GetMapper<Hitbox>();
        _inputMapper = mapperService.GetMapper<Input>();
        _positionMapper = mapperService.GetMapper<Position>();
        _velocityMapper = mapperService.GetMapper<Velocity>();
    }

    public override void Process(GameTime gameTime, int entityId) {
        move(gameTime, entityId);
        resetPositions(entityId);
    }

    private void move(GameTime gameTime, int entityID) {
        float speed = HorizontalAcceleration;

        var hitbox = _hitboxMapper.Get(entityID);
        var keyInputs = _inputMapper.Get(entityID);
        var velocity = _velocityMapper.Get(entityID);
        var position = _positionMapper.Get(entityID);

        position.X += velocity.DirX;
        position.Y += velocity.DirY;
        if (updateVelocity(velocity, 0, VerticalAcceleration).yDif == 0 && Keyboard.GetState().IsKeyDown(keyInputs[PlayerAction.Jump])) {
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
    }

    private void resetPositions(int entityID) {
        var hitbox = _hitboxMapper.Get(entityID);
        var position = _positionMapper.Get(entityID);
        
        if (position.X < 0) {
            position.X = 0;
        }
        else if (position.X > Game.Main.WindowWidth - hitbox.Width) {
            position.X = Game.Main.WindowWidth - hitbox.Width;
        }
        if (position.Y < 0) {
            position.Y = 0;
        }
        else if (position.Y > Game.Main.WindowHeight - hitbox.Height) {
            position.Y = Game.Main.WindowHeight - hitbox.Height;
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