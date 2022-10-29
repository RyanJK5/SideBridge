using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Input.InputListeners;

namespace SideBridge;

public sealed class Player : GameObject, IEntity {

    private const float MaximumVerticalVelocity = 20f;
    private const float VerticalAcceleration = 1f;
    private const float MaximumHorizontalVelocity = 10f;
    private const float HorizontalAcceleration = 1f;
    
    public Vector2 Velocity { get => _internalVelocity; }
    private Vector2 _internalVelocity;

    private Keys[] _keyInputs;

    private KeyboardListener _keyListener;

    public IShapeF Bounds => new RectangleF( position.X, position.Y, texture.Width, texture.Height);

    public Player(Texture2D texture, Vector2 position, int playerNum) : base(texture, position) {
        if (playerNum == 0) {
            _keyInputs = new Keys[] { Keys.A, Keys.D, Keys.LeftShift, Keys.Space };
        }
        _keyListener = new();
        Game.Main.Components.Add(new InputListenerComponent(Game.Main, _keyListener));
    }

    public override void Update(GameTime gameTime) => move(gameTime);

    public void OnCollision(CollisionEventArgs collisionInfo) {
        throw new System.NotImplementedException();
    }

    private void move(GameTime gameTime) {
        var origVelocity = new Vector2(_internalVelocity.X, _internalVelocity.Y);
        float speed = HorizontalAcceleration;

        position.X += _internalVelocity.X;
        position.Y += _internalVelocity.Y;
        if (updateVelocity(0, VerticalAcceleration).yDif == 0 && Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Jump])) {
            jump();
        }
        if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Sprint])) {
            speed *= 2;
        }
        if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Left])) {
            if (!Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Right])) {
                updateVelocity(-speed, 0);
                _keyListener.KeyReleased += (sender, args) => {
                    if (args.Key == _keyInputs[(int) PlayerAction.Left]) {
                        updateVelocity(speed, 0, 0, 0);
                    }
                };
            }
            else {
                updateVelocity(speed, 0, 0, 0);
            }
        }
        if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Right])) {
            if (!Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Left])) {
                updateVelocity(speed, 0);
                _keyListener.KeyReleased += (sender, args) => {
                    if (args.Key == _keyInputs[(int) PlayerAction.Right]) {
                        updateVelocity(-speed, 0, 0, 0);
                    }
                };
            }
            else {
                updateVelocity(-speed, 0, 0, 0);
            }
        }
        resetPositions();
    }

    private (float xDif, float yDif) updateVelocity(float x, float y, float xLimit, float yLimit) {
        var origVec = new Vector2(_internalVelocity.X, _internalVelocity.Y);
        
        if ((x >= 0 && _internalVelocity.X + x < xLimit) || (x < 0 && _internalVelocity.X + x > -xLimit)) {
            _internalVelocity.X += x;
        }
        else {
            if (x >= 0) {
                _internalVelocity.X = xLimit;
            }
            else {
                _internalVelocity.X = -xLimit;
            }
        }
        
        if ((y >= 0 && _internalVelocity.Y + y < yLimit) || (y < 0 && _internalVelocity.Y + y > -yLimit)) {
            _internalVelocity.Y += y;
        }
        else {
            if (y >= 0) {
                _internalVelocity.Y = yLimit;
            }
            else {
                _internalVelocity.Y = -yLimit;
            }
        }

        return (Math.Abs(_internalVelocity.X - origVec.X), Math.Abs(_internalVelocity.Y - origVec.Y));
    }

    private (float xDif, float yDif) updateVelocity(float x, float y) => updateVelocity(x, y, MaximumHorizontalVelocity, MaximumVerticalVelocity);

    private void resetPositions() {
        if (position.X < 0) {
            position.X = 0;
        }
        else if (position.X > Game.Main.WindowWidth - texture.Width) {
            position.X = Game.Main.WindowWidth - texture.Width;
        }
        if (position.Y < 0) {
            position.Y = 0;
        }
        else if (position.Y > Game.Main.WindowHeight - texture.Height) {
            position.Y = Game.Main.WindowHeight - texture.Height;
        }
    }

    private void jump() => _internalVelocity.Y = -12.5f;
}