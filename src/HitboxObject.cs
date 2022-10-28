using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SideBridge;

public abstract class HitboxObject : GameObject {

    private Rectangle hitbox;

    public HitboxObject(Texture2D texture) : this(texture, texture.Bounds) { }

    public HitboxObject(Texture2D texture, Vector2 pos) : this(texture, pos, texture.Bounds) { }

    public HitboxObject(Texture2D texture, Rectangle hitbox) : this(texture, new Vector2(), hitbox) { }

    public HitboxObject(Texture2D texture, Vector2 pos, Rectangle hitbox) : base(texture, pos) {
        this.hitbox = hitbox;
    }
}