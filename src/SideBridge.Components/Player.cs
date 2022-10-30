using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Input.InputListeners;

namespace SideBridge.Components;

public sealed class Player : AbstractEntity {
    public Vector2 Velocity;

    public Player(Texture2D sprite, RectangleF bounds) : base(sprite, bounds) { }
}