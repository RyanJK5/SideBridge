using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SideBridge.Components;

public abstract class AbstractEntity {
    
    public static Type[] SubTypes;
    static AbstractEntity() {
        SubTypes = new Type[] { typeof(Player) };
    }

    public RectangleF Bounds;
    public Texture2D Sprite;

    public AbstractEntity(Texture2D sprite, RectangleF bounds) {
        Bounds = bounds;
        Sprite = sprite;
    }
}