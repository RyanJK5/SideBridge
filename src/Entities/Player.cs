using System;
using MonoGame.Extended;
using MonoGame.Extended.Input.InputListeners;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace SideBridge;

public class Player : Entity {

    private const float MaximumVerticalVelocity = 15f;
    private const float MaximumWalkingVelocity = 5f;
    private const float MaximumHorizontalVelocity = 20f;
    private const float VerticalAcceleration = 1f;
    private const float HorizontalAcceleration = 1f;

    private const float SwordDamge = 6f;
    private const float SwordCriticalDamage = 7.5f;
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
    private readonly Keys[] _keyInputs;

    public bool OnGround {
        get => Bounds.Bottom < Game.MapHeight && Bounds.Bottom > 0 &&
            TileTypes.Solid(Game.GetTile(Bounds.X, Bounds.Bottom).Type) ||
            TileTypes.Solid(Game.GetTile(Bounds.Right - 1, Bounds.Bottom).Type);
    }

    public bool Sprinting { get => Velocity.X != 0 && OnGround && _sprintKeyDown; }
    private bool _sprintKeyDown;
    private bool _sprintHit;

    public readonly Team Team;
    public Vector2 SpawnPosition { 
        get => Team == Team.Blue ? new(420 - Bounds.Width / 2, 180) : new(Game.MapWidth - 420 - Bounds.Width / 2, 180);
    }

    private readonly Hotbar _hotbar;
    private readonly HealthBar _healthBar;

    public Player(Texture2D texture, Hotbar hotbar, HealthBar healthBar, RectangleF bounds, Team team) : base(texture, bounds) {
        if (team == Team.Blue) {
            _keyInputs = new Keys[] { Keys.A, Keys.D, Keys.LeftShift, Keys.W, Keys.Q };
        }
        else {
            _keyInputs = new Keys[] { Keys.Left, Keys.Right, Keys.RightControl, Keys.Up, Keys.NumPad0};
        }
        
        _hotbar = hotbar;
        _healthBar = healthBar;
        _healthBar.SetPlayer(this);

        var mouseListener = new MouseListener();
        mouseListener.MouseDown += TryUseSword;
        var keyListener = new KeyboardListener();
        keyListener.KeyPressed += (sender, args) => {
            if (args.Key == _keyInputs[(int) PlayerAction.Sprint]) {
                _sprintKeyDown = !Sprinting;
                if (!_sprintKeyDown) {
                    _sprintHit = false;
                }
            }
        };
        Game.AddListeners(mouseListener, keyListener);

        Health = 20;
        Team = team;
        Bounds.Position = SpawnPosition;
    }

    public override void OnCollision(Entity other) {
        if (other is Arrow arrow && arrow.PlayerTeam != Team && Game.ContainsEntity(arrow)) {
            RegisterArrowKnockback(arrow);
            RegisterDamage(arrow.Damage);
            Game.GetSoundEffect(SoundEffectID.ArrowHit).CreateInstance().Play();
            Game.RemoveEntity(arrow);
        }
    }

    public override void OnTileCollision(Tile tile) {
        if (tile.Type == TileType.Goal) {
            if (Game.GetGoalTeam(tile) != Team) {
                Game.NewRound();
                Game.ScoreGoal(this);
            } else {
                OnDeath();
            }
            return;
        }
        
        var otherBounds = tile.Bounds;
        var intersection = Bounds.Intersection(otherBounds);

        var adj = Bounds.Width / otherBounds.Height;
        var adj2 = otherBounds.Height / Bounds.Width;
        if (intersection.Height > intersection.Width) {
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
        Vector2 mousePos = Game.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        if (_hotbar.ActiveSlot == 0) {
            Vector2 center = Bounds.Center;
            Vector2 vecLine = mousePos - center;
            vecLine.Normalize();
            vecLine *= TileReach * TileSize;
            
            spriteBatch.DrawLine(center.X, center.Y, center.X + vecLine.X, center.Y + vecLine.Y, Color.GhostWhite * 0.5f, 20f);
        }

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

        var playerPos = Bounds.Center;
        var vec = new Vector2(mousePos.X, mousePos.Y) - (Vector2) Bounds.Center;
        vec.Normalize();
        vec.X *= _bowCharge;
        vec.Y *= _bowCharge;

        var pos = new Vector2(playerPos.X, playerPos.Y);
        for (int i = 0; i < 20; i++) {
            pos.X += vec.X;
            pos.Y += vec.Y;
            vec.Y += VerticalAcceleration;
            spriteBatch.DrawCircle(pos, 5, 100, Color.White, 10f);
        }
    }

    public void RegisterDamage(float dmg) {
        Health -= dmg;
        if (Health <= 0) {
            OnDeath();
        }
    }

    private void RegisterArrowKnockback(Arrow arrow) {
        Velocity = arrow.Velocity / 5f;
        Velocity.Y = -2f;
        _sprintKeyDown = false;
    }

    private void RegisterSwordKnockback(Player player) {
        var knockback = new Vector2(player.Bounds.Center.X < Bounds.Center.X ? 5f : -5f, -8f);
        if (player.Sprinting && !player._sprintHit) {
            knockback.X *= 1.5f;
            player._sprintHit = true;
        }
        if (!OnGround) {
            knockback.X *= 2f;
        }
        Velocity = knockback;
    }

    public void OnDeath() {
        Health = 20;
        Velocity = Vector2.Zero;
        Bounds.Position = SpawnPosition;
        _sprintKeyDown = false;
    }

    public override void Update(GameTime gameTime) {
        SetVerticalVelocity();
        SetHorizontalVelocity();
        UpdatePosition();
        ResetPositions();

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
                TryShootBow(gameTime);
                break;
            case 2:
                TryModifyBlock(gameTime);
                break;
            case 3:
                TryDrinkPotion(gameTime);
                break;
        }
        TryVoid(gameTime);
    }

    private void UpdatePosition() {
        if (_bowCharge > 1 || _potionCharge > 0 || _voidCharge > 0) {
            Bounds.X += Velocity.X / 4;
        }
        else if (OnGround && Sprinting) {
            Bounds.X += Velocity.X * 1.5f;
        }
        else {
            Bounds.X += Velocity.X;
        }
        Bounds.Y += Velocity.Y;
    }

    private void TryUseSword(object sender, MouseEventArgs args) {
        if (_hotbar.ActiveSlot != 0) {
            return;
        }
        Vector2 mousePos = Game.ScreenToWorld(args.Position.ToVector2());
        Vector2 vecLine = mousePos - (Vector2) Bounds.Center;
        vecLine.Normalize();
        vecLine *= TileReach * TileSize;

        var player = Game.FindEntity<Player>(entity => entity != this && findIntersection(entity.Bounds).intersects);
        if (player != null) {
            Tile[] tiles = Game.FindTiles(tile => findIntersection(tile.Bounds).intersects && TileTypes.Solid(tile.Type));
            foreach (var tile in tiles) {
                if (findIntersection(tile.Bounds).distanceAlongLine < findIntersection(player.Bounds).distanceAlongLine) {
                    return;
                }
            }
            player.RegisterSwordKnockback(this);
            player.RegisterDamage(OnGround ? SwordDamge : SwordCriticalDamage);
            Game.GetSoundEffect(SoundEffectID.SwordHit).CreateInstance().Play();
        }
        
        (bool intersects, float distanceAlongLine) findIntersection(RectangleF bounds) {
            bool result1 = bounds.IntersectsLine(Bounds.Center, Bounds.Center + vecLine, out var result2);
            return (result1, Vector2.Distance(Bounds.Center, result2));
        }
    }

    private void TryShootBow(GameTime gameTime) {
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

    private void TryModifyBlock(GameTime gameTime) {
        Vector2 mousePos = Game.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        var tileX = (int) (mousePos.X / TileSize) * TileSize;
        var tileY = (int) (mousePos.Y / TileSize) * TileSize;
        bool inRange = Vector2.Distance(new(tileX, tileY), Bounds.Position) < TileReach * TileSize && 
            tileY / TileSize >= HeightLimit && tileX / TileSize > IslandWidths - 1 && tileX / TileSize < Game.MapWidth / TileSize - IslandWidths;
        if (Mouse.GetState().LeftButton == ButtonState.Pressed && inRange && Game.GetTile(mousePos.X, mousePos.Y).Type == TileType.Air) {
            PlaceTile(tileX, tileY, mousePos);
            return;
        }
        if (Mouse.GetState().RightButton == ButtonState.Pressed && inRange) {
            DamageTile(Game.GetTile(mousePos.X, mousePos.Y));
        }
        else if (TileTypes.Solid(Game.GetTile(_lastTileSound.pos.X, _lastTileSound.pos.Y).Type)) {
            _lastTileSound.soundEffect?.Stop();
        }
    }
    
    private void TryDrinkPotion(GameTime gameTime) {
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

    private void TryVoid(GameTime gameTime) {
        if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Void])) {
            _voidCharge += gameTime.GetElapsedSeconds();
            if (_voidCharge >= VoidTime) {
                _voidCharge = 0;
                OnDeath();
            }
        }
        else {
            _voidCharge = 0;
        }
    }

    private void PlaceTile(float tileX, float tileY, Vector2 mousePos) {
        if (Game.FindEntity<Player>(player => player.Bounds.Intersects(new(tileX, tileY, TileSize, TileSize))) != null) {
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
            if (TileTypes.Solid(Game.GetTile(mousePos.X + vec.X, mousePos.Y + vec.Y).Type)) {
                var normalBlockType = Team == Team.Red ? TileType.Red : TileType.Blue;
                var darkBlockType = Team == Team.Red ? TileType.DarkRed : TileType.DarkBlue;
                Game.SetTile(tileY / TileSize <= HeightLimit ? darkBlockType : normalBlockType, mousePos.X, mousePos.Y);
                break;
            }
        }
    }

    private (Vector2 pos, SoundEffectInstance soundEffect) _lastTileSound;
    private void DamageTile(Tile tile) {
        if (!TileTypes.Breakable(tile.Type)) {
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
            Game.SetTile(TileType.Air, tile.Bounds.X, tile.Bounds.Y);
        }
    }

    private void SetHorizontalVelocity() {
        bool leftDown = Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Left]);
        bool rightDown = Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Right]);
        if (leftDown && !(rightDown && Velocity.X <= 0)) {
            UpdateVelocity(-HorizontalAcceleration, 0, MaximumWalkingVelocity, MaximumVerticalVelocity);
        }
        else if (!leftDown && Velocity.X <= 0 && OnGround) {
            UpdateVelocity(HorizontalAcceleration, 0, 0, MaximumVerticalVelocity);
        }
        if (rightDown && !(leftDown && Velocity.X >= 0)) {
            UpdateVelocity(HorizontalAcceleration, 0, MaximumWalkingVelocity, MaximumVerticalVelocity);
        }
        else if (!rightDown && Velocity.X >= 0 && OnGround) {
            UpdateVelocity(-HorizontalAcceleration, 0, 0, MaximumVerticalVelocity);
        }
        if (!leftDown && !rightDown && Velocity.X != 0 && OnGround) {
            if (Velocity.X < 0) {
                UpdateVelocity(HorizontalAcceleration, 0, 0, MaximumVerticalVelocity);
            }
            else {
                UpdateVelocity(-HorizontalAcceleration, 0, 0, MaximumVerticalVelocity);
            }
        }
    }

    private void SetVerticalVelocity() {
        if  (OnGround) {
            if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Jump])) {
                Velocity.Y = -11f;
            }
        }
        else {
            UpdateVelocity(0, VerticalAcceleration);
        }
    }

    private void ResetPositions() {
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

    private void UpdateVelocity(float x, float y, float xLimit, float yLimit) {
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

    private void UpdateVelocity(float x, float y) => 
        UpdateVelocity(x, y, MaximumHorizontalVelocity, MaximumVerticalVelocity);
}