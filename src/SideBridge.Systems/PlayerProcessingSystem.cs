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
    private ComponentMapper<Player> _playerMapper;
    private Keys[] _keyInputs;
    private KeyboardListener _keyListener;

    private const float MaximumVerticalVelocity = 20f;
    private const float VerticalAcceleration = 1f;
    private const float MaximumHorizontalVelocity = 10f;
    private const float HorizontalAcceleration = 1f;

    public PlayerProcessingSystem()
        : base(Aspect.All(typeof(Player))) {
        _keyInputs = new Keys[] { Keys.A, Keys.D, Keys.LeftShift, Keys.Space};
        _keyListener = new();
        Game.Main.Components.Add(new InputListenerComponent(Game.Main, _keyListener));
    }

    public override void Initialize(IComponentMapperService mapperService) {
        _playerMapper = mapperService.GetMapper<Player>();
    }

    public override void Process(GameTime gameTime, int entityId) {
        var player = _playerMapper.Get(entityId);
        move(gameTime, player);
        resetPositions(player);
    }

    private void move(GameTime gameTime, Player player) {
        float speed = HorizontalAcceleration;

        player.Bounds.X += player.Velocity.X;
        player.Bounds.Y += player.Velocity.Y;
        if (updateVelocity(player, 0, VerticalAcceleration).yDif == 0 && Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Jump])) {
            player.Velocity.Y = -12.5f;
        }
        if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Sprint])) {
            speed *= 2;
        }
        if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Left])) {
            if (!Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Right])) {
                updateVelocity(player, -speed, 0);
                _keyListener.KeyReleased += (sender, args) => {
                    if (args.Key == _keyInputs[(int) PlayerAction.Left]) {
                        updateVelocity(player, speed, 0, 0, 0);
                    }
                };
            }
            else {
                updateVelocity(player, speed, 0, 0, 0);
            }
        }
        if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Right])) {
            if (!Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Left])) {
                updateVelocity(player, speed, 0);
                _keyListener.KeyReleased += (sender, args) => {
                    if (args.Key == _keyInputs[(int) PlayerAction.Right]) {
                        updateVelocity(player, -speed, 0, 0, 0);
                    }
                };
            }
            else {
                updateVelocity(player, -speed, 0, 0, 0);
            }
        }
    }

    private void resetPositions(Player player) {
        if (player.Bounds.X < 0) {
            player.Bounds.X = 0;
        }
        else if (player.Bounds.X > Game.Main.WindowWidth - player.Bounds.Width) {
            player.Bounds.X = Game.Main.WindowWidth - player.Bounds.Width;
        }
        if (player.Bounds.Y < 0) {
            player.Bounds.Y = 0;
        }
        else if (player.Bounds.Y > Game.Main.WindowHeight - player.Bounds.Height) {
            player.Bounds.Y = Game.Main.WindowHeight - player.Bounds.Height;
        }
    }

    private (float xDif, float yDif) updateVelocity(Player player, float x, float y, float xLimit, float yLimit) {
        var origVec = new Vector2(player.Velocity.X, player.Velocity.Y);
        
        if ((x >= 0 && player.Velocity.X + x < xLimit) || (x < 0 && player.Velocity.X + x > -xLimit)) {
            player.Velocity.X += x;
        }
        else {
            if (x >= 0) {
                player.Velocity.X = xLimit;
            }
            else {
                player.Velocity.X = -xLimit;
            }
        }
        
        if ((y >= 0 && player.Velocity.Y + y < yLimit) || (y < 0 && player.Velocity.Y + y > -yLimit)) {
            player.Velocity.Y += y;
        }
        else {
            if (y >= 0) {
                player.Velocity.Y = yLimit;
            }
            else {
                player.Velocity.Y = -yLimit;
            }
        }

        return (Math.Abs(player.Velocity.X - origVec.X), Math.Abs(player.Velocity.Y - origVec.Y));
    }

    private (float xDif, float yDif) updateVelocity(Player player, float x, float y) => 
        updateVelocity(player, x, y, MaximumHorizontalVelocity, MaximumVerticalVelocity);
}