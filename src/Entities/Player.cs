using MonoGame.Extended;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace SideBridge;

public class Player : Entity {

    private const float MaximumVerticalVelocity = 15f;
    private const float VerticalAcceleration = 1f;
    private const float MaximumHorizontalVelocity = 5f;
    private const float HorizontalAcceleration = 1f;

    private const float TileReach = 3.5f;
    private const int TileSize = 40;
    private const int HeightLimit = 8;
    private const int IslandWidths = 10;

    private const float MaxBowCharge = 45f;
    private const float PotionDrinkTime = 1.61f;
    private const float VoidTime = 1f;
    public const float ArrowCooldown = 3f;

    public float TimeSinceBowShot = ArrowCooldown;
    private bool _mouseDown;
    private float _bowCharge = 1;
    private float _potionCharge;
    private float _voidCharge;


    public float Health { get; private set; }
    private Keys[] _keyInputs;

    public readonly Team Team;
    public Vector2 SpawnPosition { 
        get => Team == Team.Blue ? new(200, 100) : new(Game.MapWidth - 200, 100);
    }

    private Hotbar _hotbar;
    private HealthBar _healthBar;

    public Player(Texture2D texture, Hotbar hotbar, HealthBar healthBar, RectangleF bounds, Team team) : base(texture, bounds) {
        if (team == Team.Blue) {
            _keyInputs = new Keys[] { Keys.A, Keys.D, Keys.LeftShift, Keys.W, Keys.Q };
        }
        else {
            _keyInputs = new Keys[] { Keys.L, Keys.OemQuotes, Keys.RightShift, Keys.P, Keys.OemOpenBrackets };
        }
        
        _hotbar = hotbar;
        _healthBar = healthBar;
        _healthBar.SetPlayer(this);

        Health = 20;
        Team = team;
        Bounds.Position = SpawnPosition;
    }

    public override void OnCollision(Entity other) {
        if (other is Arrow arrow && arrow.PlayerTeam != Team && Game.ContainsEntity(arrow)) {
            RegisterDamage(arrow.Damage);
            Game.RemoveEntity(arrow);
        }
    }

    public override void OnTileCollision(Tile tile) {
        var otherBounds = tile.Bounds;
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

    public override void Draw(SpriteBatch spriteBatch) {
        base.Draw(spriteBatch);
        
        if (_potionCharge > 0) {
            var barBounds = new RectangleF(Bounds.X - 2, Bounds.Y - 10, Bounds.Width + 4, 6);
            spriteBatch.FillRectangle(barBounds, Color.DarkRed);
            spriteBatch.DrawPercentageBar(barBounds, _potionCharge / PotionDrinkTime);
        }
        if (_voidCharge > 0) {
            var barBounds = new RectangleF(Bounds.X - 2, Bounds.Y - (_potionCharge > 0 ? 20 : 10), Bounds.Width + 4, 6);
            spriteBatch.FillRectangle(barBounds, Color.Purple);
            spriteBatch.DrawPercentageBar(barBounds, _voidCharge / VoidTime, false);
        }

        if (_bowCharge == 1) {
            return;
        }

        var mousePos = Game.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        var playerPos = Bounds.Center;
        var vec = new Vector2(mousePos.X, mousePos.Y) - (Vector2) Bounds.Center;
        vec.Normalize();
        vec.X *= _bowCharge;
        vec.Y *= _bowCharge;

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
        Bounds.Position = SpawnPosition;
    }

    public override void Update(GameTime gameTime) {
        setVerticalVelocity();
        setHorizontalVelocity();
        updatePosition();
        resetPositions();

        if (TimeSinceBowShot < ArrowCooldown) {
            TimeSinceBowShot += gameTime.GetElapsedSeconds();
        }
        if (_hotbar.ActiveSlot != 1 && _bowCharge > 1) {
            _bowCharge = 1;
            _mouseDown = false;
        }
        if (_hotbar.ActiveSlot != 3 && _potionCharge > 0) {
            _potionCharge = 0;
        }

        switch (_hotbar.ActiveSlot) {
            case 1:
                tryShootBow(gameTime);
                break;
            case 2:
                tryBlockPlacement(gameTime);
                break;
            case 3:
                tryDrinkPotion(gameTime);
                break;
        }
        tryVoid(gameTime);
    }

    private void updatePosition() {
        if (_bowCharge > 1 || _potionCharge > 0 || _voidCharge > 0) {
            Bounds.X += Velocity.X / 4;
        }
        else if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Sprint])) {
            Bounds.X += Velocity.X * 1.5f;
        }
        else {
            Bounds.X += Velocity.X;
        }
        Bounds.Y += Velocity.Y;
    }

    private void tryShootBow(GameTime gameTime) {
        if (TimeSinceBowShot < ArrowCooldown) {
            return;
        }
        var mouseState = Mouse.GetState();
        if (mouseState.LeftButton == ButtonState.Pressed) {
            _mouseDown = true;
            _bowCharge += 0.5f;
            if (_bowCharge > MaxBowCharge) {
                _bowCharge = MaxBowCharge;
            }
        }
        if (_mouseDown && mouseState.LeftButton == ButtonState.Released) {
            var arrowTexture = Arrow.ArrowTexture;
            Vector2 spawnPos = Bounds.Center - new Vector2(arrowTexture.Width / 2, arrowTexture.Height / 2);
            
            var arrow = new Arrow(
                new(spawnPos.X, spawnPos.Y, arrowTexture.Width, 24), 
                _bowCharge / 9f,
                Team
            );
            
            var mousePos = Game.ScreenToWorld(mouseState.Position.ToVector2());
            var vec = new Vector2(mousePos.X, mousePos.Y) - spawnPos;
            vec.Normalize();
            vec.X *= _bowCharge;
            vec.Y *= _bowCharge;
            
            arrow.Velocity = vec;
            Game.AddEntity(arrow);

            _mouseDown = false;
            _bowCharge = 1;
            TimeSinceBowShot = 0;
        }
    }

    private void tryBlockPlacement(GameTime gameTime) {
        Vector2 mousePos = Game.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        var tileX = (int) (mousePos.X / TileSize) * TileSize;
        var tileY = (int) (mousePos.Y / TileSize) * TileSize;
        bool inRange = Vector2.Distance(new(tileX, tileY), Bounds.Position) < TileReach * TileSize && 
            tileY / TileSize >= HeightLimit && tileX / TileSize > IslandWidths - 1 && tileX / TileSize < Game.MapWidth - IslandWidths;
        if (Mouse.GetState().LeftButton == ButtonState.Pressed && inRange && Game.GetTile(mousePos.X, mousePos.Y).Type == BlockID.Air) {
            placeTile(tileX, tileY, mousePos);
            return;
        }
        if (Mouse.GetState().RightButton == ButtonState.Pressed && inRange) {
            damageTile(Game.GetTile(mousePos.X, mousePos.Y));
        }
        else {
            if (Game.GetTile(_lastTileSound.pos.X, _lastTileSound.pos.Y).Type != BlockID.Air) {
                _lastTileSound.soundEffect?.Stop();
            }
        }
    }

    private void tryDrinkPotion(GameTime gameTime) {
        var mouseState = Mouse.GetState();
        if (mouseState.LeftButton == ButtonState.Pressed) {
            _potionCharge += gameTime.GetElapsedSeconds();
            if (_potionCharge >= PotionDrinkTime) {
                _potionCharge = 0;
                Health = 24;
            }
        }
        else {
            _potionCharge = 0;
        }
    }

    private void tryVoid(GameTime gameTime) {
        if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Void])) {
            _voidCharge += gameTime.GetElapsedSeconds();
            if (_voidCharge >= VoidTime) {
                _voidCharge = 0;
                onDeath();
            }
        }
        else {
            _voidCharge = 0;
        }
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
            if (Game.GetTile(mousePos.X + vec.X, mousePos.Y + vec.Y).Type != BlockID.Air) {
                var normalBlockType = Team == Team.Red ? BlockID.Red : BlockID.Blue;
                var darkBlockType = Team == Team.Red ? BlockID.DarkRed : BlockID.DarkBlue;
                Game.SetTile(tileY / TileSize <= HeightLimit ? darkBlockType : normalBlockType, mousePos.X, mousePos.Y);
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
            Game.SetTile(BlockID.Air, tile.Bounds.X, tile.Bounds.Y);
        }
    }

    private void setHorizontalVelocity() {
        bool leftDown = Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Left]);
        bool rightDown = Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Right]);
        if (leftDown && !(rightDown && Velocity.X <= 0)) {
            updateVelocity(-HorizontalAcceleration, 0);
        }
        else if (!leftDown && Velocity.X <= 0) {
            updateVelocity(HorizontalAcceleration, 0, 0, MaximumVerticalVelocity);
        }
        if (rightDown && !(leftDown && Velocity.X >= 0)) {
            updateVelocity(HorizontalAcceleration, 0);
        }
        else if (!rightDown && Velocity.X >= 0) {
            updateVelocity(-HorizontalAcceleration, 0, 0, MaximumVerticalVelocity);
        }
        if (!leftDown && !rightDown && Velocity.X != 0) {
            if (Velocity.X < 0) {
                updateVelocity(HorizontalAcceleration, 0, 0, MaximumVerticalVelocity);
            }
            else {
                updateVelocity(-HorizontalAcceleration, 0, 0, MaximumVerticalVelocity);
            }
        }
    }

    private void setVerticalVelocity() {
        if  (Bounds.Bottom < Game.MapHeight && Bounds.Bottom > 0 && (
            Game.GetTile(Bounds.X, Bounds.Bottom).Type != BlockID.Air ||
            Game.GetTile(Bounds.Right - 1, Bounds.Bottom).Type != BlockID.Air
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