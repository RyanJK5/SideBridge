using MonoGame.Extended;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Collisions;
using System.Collections.Generic;

namespace SideBridge;

public class Player : Entity {

    private const float MaximumVerticalVelocity = 15f;
    private const float VerticalAcceleration = 1f;
    private const float MaximumHorizontalVelocity = 5f;
    private const float HorizontalAcceleration = 1f;

    private const float TileReach = 3.5f;
    private const int TileDurability = 10;
    private const int TileSize = 40;
    private const int HeightLimit = 8;
    private const int IslandWidths = 10;

    private static int NextID;
    public readonly int ID;

    public float Health { get; private set; }
    public Keys[] _keyInputs;


    public Player(Texture2D texture, RectangleF bounds) : base(texture, bounds) {
        _keyInputs = new Keys[] { Keys.A, Keys.D, Keys.LeftShift, Keys.Space };
        Health = 20;
        ID = NextID;
        NextID++;
    }

    public override void OnCollision(CollisionEventArgs args) {
        if (args.Other is Arrow arrow && arrow.PlayerID == ID) {
            RegisterDamage(arrow.Damage);
        }
        if (args.Other is Tile other) {
            var otherBounds = other.Bounds;
            var intersection = Bounds.Intersection(otherBounds);

            var adj = Bounds.Width / otherBounds.Height;
            var adj2 = otherBounds.Height / Bounds.Width;
            if (intersection.Height * adj2 > intersection.Width) {
                if (Bounds.X < otherBounds.Position.X) {
                    Bounds.X -= intersection.Width;
                }
                else {
                    Bounds.X += intersection.Width;
                }
                Velocity.X = 0;
            }
            else {
                if (Bounds.Y < otherBounds.Y) { 
                    Bounds.Y -= intersection.Height;
                }
                else {
                    Bounds.Y += intersection.Height;
                }
                Velocity.Y = 0;
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch) {
        base.Draw(spriteBatch);
        
        if (_mouseCharge == 1) {
            return;
        }

        var mousePos = Game.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        var playerPos = Bounds.Center;
        var vec = new Vector2(mousePos.X, mousePos.Y) - (Vector2) Bounds.Center;
        vec.Normalize();
        vec.X *= _mouseCharge;
        vec.Y *= _mouseCharge;

        var pos = new Vector2(playerPos.X, playerPos.Y);
        var nextPos = new Vector2(pos.X + (vec.X + VerticalAcceleration) / 2, pos.Y + (vec.Y + VerticalAcceleration) / 2);
        for (int i = 0; i < 10; i++) {
            spriteBatch.DrawLine(pos.X, pos.Y, nextPos.X, nextPos.Y, Color.White, 4);

            pos.X += vec.X;
            pos.Y += vec.Y;
            vec.Y += VerticalAcceleration;
            nextPos.X += vec.X;
            nextPos.Y += vec.Y;
        }
    }

    public void RegisterDamage(float dmg) {
        Health -= dmg;
        if (Health <= 0) {
            onDeath();
        }
    }

    private void onDeath() {
        Health = 20;
        Velocity = Vector2.Zero;
        Bounds.Position = new Vector2(200, 100);
    }

    public override void Update(GameTime gameTime) {
        setVerticalVelocity();
        setHorizontalVelocity();
        Bounds.Position += Velocity;
        resetPositions();
        
        if (TimeSinceBowShot < ArrowCooldown) {
            TimeSinceBowShot += gameTime.GetElapsedSeconds();
        }
        if (Game.Hotbar.ActiveSlot != 1 && _mouseCharge > 1) {
            _mouseCharge = 1;
            _mouseDown = false;
        }
        switch (Game.Hotbar.ActiveSlot) {
            case 1:
                tryShootBow(gameTime);
                break;
            case 2:
                tryBlockPlacement();
                break;
            case 3:
                tryDrinkPotion();
                break;
            
        }
    }

    private bool _mouseDown;
    private float _mouseCharge = 1;
    private const float MaxBowCharge = 45f;
    
    public float TimeSinceBowShot = ArrowCooldown;
    public const float ArrowCooldown = 3f;
    
    private void tryShootBow(GameTime gameTime) {
        if (TimeSinceBowShot < ArrowCooldown) {
            return;
        }
        var mouseState = Mouse.GetState();
        if (mouseState.LeftButton == ButtonState.Pressed) {
            _mouseDown = true;
            _mouseCharge += 0.5f;
            if (_mouseCharge > MaxBowCharge) {
                _mouseCharge = MaxBowCharge;
            }
        }
        if (_mouseDown && mouseState.LeftButton == ButtonState.Released) {
            var arrowTexture = Arrow.ArrowTexture;
            Vector2 spawnPos = Bounds.Center - new Vector2(arrowTexture.Width / 2, arrowTexture.Height / 2);
            
            var arrow = new Arrow(
                new(spawnPos.X, spawnPos.Y, arrowTexture.Height, arrowTexture.Width), 
                _mouseCharge / 9f,
                ID
            );
            
            var mousePos = Game.ScreenToWorld(mouseState.Position.ToVector2());
            var vec = new Vector2(mousePos.X, mousePos.Y) - spawnPos;
            vec.Normalize();
            vec.X *= _mouseCharge;
            vec.Y *= _mouseCharge;
            
            arrow.Velocity = vec;
            Game.AddEntity(arrow);

            _mouseDown = false;
            _mouseCharge = 1;
            TimeSinceBowShot = 0;
        }
    }

    private void tryBlockPlacement() {
        Vector2 mousePos = Game.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        var tileX = (int) (mousePos.X / TileSize) * TileSize;
        var tileY = (int) (mousePos.Y / TileSize) * TileSize;
        bool inRange = Vector2.Distance(new(tileX, tileY), Bounds.Position) < TileReach * TileSize && 
            tileY / TileSize >= HeightLimit && tileX / TileSize > IslandWidths - 1 && tileX / TileSize < Game.MapWidth - IslandWidths;
        if (Mouse.GetState().LeftButton == ButtonState.Pressed && inRange && Game.GetTile(mousePos.X, mousePos.Y).Type == BlockType.Air) {
            placeTile(tileX, tileY, mousePos);
            return;
        }
        if (Mouse.GetState().RightButton == ButtonState.Pressed && inRange) {
            damageTile(Game.GetTile(mousePos.X, mousePos.Y));
        }
        else {
            if (Game.GetTile(_lastTileSound.pos.X, _lastTileSound.pos.Y).Type != BlockType.Air) {
                _lastTileSound.soundEffect?.Stop();
            }
        }
    }

    private void tryDrinkPotion() {

    }

    private void placeTile(float tileX, float tileY, Vector2 mousePos) {
        if (new RectangleF(tileX, tileY, TileSize, TileSize).Intersects(Bounds)) {
                return;
        }
        Vector2[] adjacentTiles = {
            new(-TileSize, 0),
            new(0, -TileSize),
            new(TileSize, 0),
            new(0, TileSize)
        };
        foreach (var vec in adjacentTiles) {
            if (mousePos.X + vec.X > Game.MapWidth || mousePos.X + vec.X < 0 || 
                mousePos.Y + vec.Y > Game.MapHeight || mousePos.Y + vec.Y < 0) {
                continue;
            }
            if (Game.GetTile(mousePos.X + vec.X, mousePos.Y + vec.Y).Type != BlockType.Air) {
                Game.SetTile(tileY / TileSize <= HeightLimit ? BlockType.DarkBlue : BlockType.Blue, mousePos.X, mousePos.Y);
                break;
            }
        }
    }

    private (Vector2 pos, SoundEffectInstance soundEffect) _lastTileSound;
    private void damageTile(Tile tile) {
        if (!Blocks.Breakable(tile.Type)) {
            return;
        }
        if (tile.Durability == Tile.MaxDurability) {
            _lastTileSound.soundEffect?.Stop();
            _lastTileSound.soundEffect = Game.GetSoundEffect(SoundEffectID.BreakBlock).CreateInstance();
            _lastTileSound.pos = tile.Bounds.Position;
            _lastTileSound.soundEffect?.Play();
        }
        tile.Durability--;
        if (tile.Durability <= 0) {
            Game.SetTile(BlockType.Air, tile.Bounds.X, tile.Bounds.Y);
        }
    }

    private void setHorizontalVelocity() {
        float speed = HorizontalAcceleration;
        if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Sprint])) {
            speed *= 2;
        }

        bool leftDown = Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Left]);
        bool rightDown = Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Right]);
        if (leftDown && !(rightDown && Velocity.X <= 0)) {
            updateVelocity(-speed, 0);
        }
        else if (!leftDown && Velocity.X <= 0) {
            updateVelocity(speed, 0, 0, MaximumVerticalVelocity);
        }
        if (rightDown && !(leftDown && Velocity.X >= 0)) {
            updateVelocity(speed, 0);
        }
        else if (!rightDown && Velocity.X >= 0) {
            updateVelocity(-speed, 0, 0, MaximumVerticalVelocity);
        }
        if (!leftDown && !rightDown && Velocity.X != 0) {
            if (Velocity.X < 0) {
                updateVelocity(speed, 0, 0, MaximumVerticalVelocity);
            }
            else {
                updateVelocity(-speed, 0, 0, MaximumVerticalVelocity);
            }
        }
    }

    private void setVerticalVelocity() {
        if  (Bounds.Bottom < Game.MapHeight && Bounds.Bottom > 0 && (
            Game.GetTile(Bounds.X, Bounds.Bottom).Type != BlockType.Air ||
            Game.GetTile(Bounds.Right - 1, Bounds.Bottom).Type != BlockType.Air
            )) {
            if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Jump])) {
                Velocity.Y = -11f;
            }
        }
        else {
            updateVelocity(0, VerticalAcceleration);
        }
    }

    private void resetPositions() {
        if (Bounds.X < 0) {
            Bounds.X = 0;
        }
        else if (Bounds.X > Game.MapWidth - Bounds.Width) {
            Bounds.X = Game.MapWidth - Bounds.Width;
        }
        if (Bounds.Y > Game.MapHeight) {
            Bounds.Y = -Bounds.Height * 4;
        }
    }

    private void updateVelocity(float x, float y, float xLimit, float yLimit) {
        if ((x >= 0 && Velocity.X + x < xLimit) || (x < 0 && Velocity.X + x > -xLimit)) {
            Velocity.X += x;
        }
        else {
            if (x >= 0) {
                Velocity.X = xLimit;
            }
            else {
                Velocity.X = -xLimit;
            }
        }
        
        if ((y >= 0 && Velocity.Y + y < yLimit) || (y < 0 && Velocity.Y + y > -yLimit)) {
            Velocity.Y += y;
        }
        else {
            if (y >= 0) {
                Velocity.Y = yLimit;
            }
            else {
                Velocity.Y = -yLimit;
            }
        }
    }

    private void updateVelocity(float x, float y) => 
        updateVelocity(x, y, MaximumHorizontalVelocity, MaximumVerticalVelocity);
}