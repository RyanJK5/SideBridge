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
    private const float Gravity = 1f;

    private const float HorizontalAcceleration = 1f;
    private const float HorizontalAirAcceleration = 0.4f;
    private const float Friction = 1f;
    private const float AirResistance = 0.2f;

    private const float MaxRedHealth = 20;
    private const float SwordDamge = 6f;
    private const float SwordCriticalDamage = 7.5f;
    private const float TileReach = 3.5f;
    private const int TileSize = 40;
    public const int HeightLimit = 8;
    private const int IslandWidths = 10;

    public const float BowCooldown = 3f;
    private const float MaxBowCharge = 45f;

    private const float AppleEatTime = 1.5f;
    private const float AppleCooldown = 0.2f;
    
    private const float VoidTime = 1f;
    private const float VoidCooldown = 0.2f;

    private const float SprintingSoundDelay = 0.3f;
    private const float WalkingSoundDelay = 0.45f;

    private bool _mouseDown;
    private bool _knockedBack;

    private float _bowCharge = 1;
    public float TimeSinceBow = BowCooldown;
    
    private float _appleCharge;
    private float _timeSinceApple;
    
    private float _voidCharge;
    private float _timeSinceVoid;

    public float Health { get; private set; }
    private readonly Keys[] _keyInputs;

    private float _walkTime;
    private (Vector2 pos, SoundEffectInstance soundEffect) _lastTileSound;
    private SoundEffectInstance _eatSound;
    private SoundEffectInstance _voidSound;

    public bool OnGround {
        get => Bounds.Bottom < Game.MapHeight && Bounds.Bottom > 0 &&
            TileTypes.Solid(Game.GetTile(Bounds.X, Bounds.Bottom).Type) ||
            TileTypes.Solid(Game.GetTile(Bounds.Right - 1, Bounds.Bottom).Type);
    }

    public bool Sprinting { get => Velocity.X != 0 && _sprintKeyDown; }
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
            _keyInputs = new Keys[] { Keys.A, Keys.D, Keys.LeftShift, Keys.W, Keys.Q, Keys.D1, Keys.D2, Keys.D3, Keys.D4};
        }
        else {
            _keyInputs = new Keys[] { Keys.Left, Keys.Right, Keys.RightControl, Keys.Up, Keys.NumPad0, Keys.D5, Keys.D6, Keys.D7, Keys.D8};
        }
        
        _hotbar = hotbar;
        _healthBar = healthBar;
        _healthBar.SetPlayer(this);

        var mouseListener = new MouseListener();
        mouseListener.MouseDown += TryUseSword;

        for (var i = (int) PlayerAction.Hotbar1; i <= (int) PlayerAction.Hotbar4; i++) {
            _hotbar.SetSlotBind(i - (int) PlayerAction.Hotbar1, _keyInputs[i]);
        }

        var keyListener = new KeyboardListener();
        keyListener.KeyPressed += (sender, args) => {
            if (args.Key == _keyInputs[(int) PlayerAction.Sprint]) {
                _sprintKeyDown = !Sprinting;
                if (!_sprintKeyDown) {
                    _sprintHit = false;
                }
            }
        };
        _sprintKeyDown = true;

        Game.AddListeners(mouseListener, keyListener);

        Health = MaxRedHealth;
        Team = team;
        Bounds.Position = SpawnPosition;

        _eatSound = Game.GetSoundEffect(SoundEffectID.Eat).CreateInstance();
        _eatSound.Volume = 0.8f;
        _voidSound = Game.GetSoundEffect(SoundEffectID.Void).CreateInstance();
        _voidSound.Volume = 0.5f;
    }

    public override void OnCollision(Entity other) {
        if (other is Arrow arrow && arrow.PlayerTeam != Team && Game.ContainsEntity(arrow)) {
            RegisterArrowKnockback(arrow);
            RegisterDamage(arrow.Damage);
            if (Health != MaxRedHealth) {
                Game.GetSoundEffect(SoundEffectID.ArrowHit).Play();
            }
            Game.RemoveEntity(arrow);
        }
    }

    public override void OnTileCollision(Tile tile) {
        if (tile.Type == TileType.Goal) {
            if (Game.GetGoalTeam(tile) == Team || !Game.ScoreGoal(this)) {
                Game.GetSoundEffect(SoundEffectID.Goal).Play();
                OnDeath();
            }
            return;
        }
        
        var otherBounds = tile.Bounds;
        var intersection = Bounds.Intersection(otherBounds);

        if (intersection.Height > intersection.Width) {
            if (Bounds.X < otherBounds.Position.X) {
                Bounds.X -= intersection.Width;
            }
            else {
                Bounds.X += intersection.Width;
            }
            Velocity.X = 0;
            _walkTime = 0;
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

        if (_appleCharge > 0) {
            var barBounds = new RectangleF(Bounds.X - 2, Bounds.Y - 10, Bounds.Width + 4, 6);
            spriteBatch.FillRectangle(barBounds, Color.DarkRed);
            spriteBatch.DrawPercentageBar(barBounds, _appleCharge / AppleEatTime);
        }
        if (_voidCharge > 0) {
            var barBounds = new RectangleF(Bounds.X - 2, Bounds.Y - (_appleCharge > 0 ? 20 : 10), Bounds.Width + 4, 6);
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
            vec.Y += Gravity;
            spriteBatch.DrawCircle(pos, 5, 100, Color.White, 10f);
        }
    }

    public void RegisterDamage(float dmg) {
        Health -= dmg;
        if (Health <= 0) {
            OnDeath();
            Game.GetSoundEffect(SoundEffectID.Kill).Play();
        }
    }

    private void RegisterArrowKnockback(Arrow arrow) {
        Velocity = arrow.Velocity / 5f;
        Velocity.Y = -2f;
        _sprintKeyDown = false;
        _knockedBack = true;
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
        _knockedBack = true;
    }

    public void OnDeath() {
        Health = MaxRedHealth;
        Velocity = Vector2.Zero;
        Bounds.Position = SpawnPosition;
        _sprintKeyDown = true;
        _bowCharge = 1f;
        TimeSinceBow = BowCooldown;
        _appleCharge = 0;
        _timeSinceApple = 0;
        _eatSound.Stop();
    }

    public override void Update(GameTime gameTime) {
        SetVerticalVelocity();
        SetHorizontalVelocity();
        UpdatePosition();
        ResetPositions();

        _timeSinceApple += gameTime.GetElapsedSeconds();
        _timeSinceVoid += gameTime.GetElapsedSeconds();
        if (OnGround && Velocity.X != 0) {
            _walkTime += gameTime.GetElapsedSeconds();
            if (_walkTime >= (Sprinting ? SprintingSoundDelay : WalkingSoundDelay)) { 
                _walkTime = 0;
                SoundEffectInstance walkSound = Game.GetSoundEffect(SoundEffects.GetRandomWalkSound()).CreateInstance();
                walkSound.Volume = 0.2f;
                walkSound.Play();
            }
        }
        else{
            _walkTime = SprintingSoundDelay;
        }
        if (TimeSinceBow < BowCooldown) {
            TimeSinceBow += gameTime.GetElapsedSeconds();
        }
        if (_hotbar.ActiveSlot != 1 && _bowCharge > 1) {
            _bowCharge = 1;
            _mouseDown = false;
        }
        if (_hotbar.ActiveSlot != 3 && _appleCharge > 0) {
            _appleCharge = 0;
        }

        switch (_hotbar.ActiveSlot) {
            case 1:
                TryShootBow();
                break;
            case 2:
                TryModifyBlock();
                break;
            case 3:
                TryEatApple(gameTime);
                break;
        }
        TryVoid(gameTime);
    }

    private void UpdatePosition() {
        if (!_knockedBack && (_bowCharge > 1 || _appleCharge > 0 || _voidCharge > 0)) {
            Bounds.X += Velocity.X / 4;
        }
        else if (!_knockedBack && Sprinting) {
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
            if (player.Health != MaxRedHealth) {
                Game.GetSoundEffect(SoundEffectID.SwordHit).Play();
            }
        }
        
        (bool intersects, float distanceAlongLine) findIntersection(RectangleF bounds) {
            bool result1 = bounds.IntersectsLine(Bounds.Center, Bounds.Center + vecLine, out var result2);
            return (result1, Vector2.Distance(Bounds.Center, result2));
        }
    }

    private void TryShootBow() {
        if (TimeSinceBow < BowCooldown) {
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
            Game.GetSoundEffect(SoundEffects.GetRandomBowSound()).Play();

            
            var mousePos = Game.ScreenToWorld(mouseState.Position.ToVector2());
            var vec = new Vector2(mousePos.X, mousePos.Y) - spawnPos;
            vec.Normalize();
            vec.X *= _bowCharge;
            vec.Y *= _bowCharge;
            
            arrow.Velocity = vec;
            Game.AddEntity(arrow);

            _mouseDown = false;
            _bowCharge = 1;
            TimeSinceBow = 0;
        }
    }

    private void TryModifyBlock() {
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
    
    private void TryEatApple(GameTime gameTime) {
        var mouseState = Mouse.GetState();
        if (_timeSinceApple > AppleCooldown && mouseState.LeftButton == ButtonState.Pressed) {
            if (_appleCharge == 0) {
                _eatSound.Play();
            }
            _appleCharge += gameTime.GetElapsedSeconds();
            if (_appleCharge >= AppleEatTime) {
                _appleCharge = 0;
                _timeSinceApple = 0;
                Health = 24;
            }
        }
        else {
            _eatSound?.Stop();
            _appleCharge = 0;
        }
    }

    private void TryVoid(GameTime gameTime) {
        if (_timeSinceVoid >= VoidCooldown && Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Void])) {
            if (_voidCharge == 0) {
                _voidSound.Play();
            }
            _voidCharge += gameTime.GetElapsedSeconds();
            if (_voidCharge >= VoidTime) {
                _voidCharge = 0;
                _timeSinceVoid = 0;
                OnDeath();
            }
        }
        else {
            _voidSound.Stop();
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
        
        var addedVelocity = 0f;
        float maxVelocity = MaximumHorizontalVelocity;
        
        if (leftDown == rightDown || MathF.Abs(Velocity.X) > MaximumWalkingVelocity) {
            if (Velocity.X < 0) {
                addedVelocity = OnGround ? Friction : AirResistance;
            }
            else if (Velocity.X > 0) {
                addedVelocity = OnGround ? -Friction : -AirResistance;
            }
            UpdateVelocity(addedVelocity, 0, 0, MaximumVerticalVelocity);
            return;
        }
        else if (_knockedBack && MathF.Abs(Velocity.X) <= MaximumWalkingVelocity) {
            _knockedBack = false;
        }

        if (leftDown && !rightDown && Velocity.X >= -MaximumWalkingVelocity) {
            addedVelocity = OnGround ? -HorizontalAcceleration : -HorizontalAirAcceleration;
            maxVelocity = OnGround ? MaximumWalkingVelocity : MaximumHorizontalVelocity;
        }
        if (rightDown && !leftDown && Velocity.X <= MaximumWalkingVelocity) {
            addedVelocity = OnGround ? HorizontalAcceleration : HorizontalAirAcceleration;
            maxVelocity = OnGround ? MaximumWalkingVelocity : MaximumHorizontalVelocity;
        }
        
        UpdateVelocity(addedVelocity, 0, maxVelocity, MaximumVerticalVelocity);
    }

    private void SetVerticalVelocity() {
        if  (OnGround) {
            if (Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Jump])) {
                Velocity.Y = -11f;
            }
        }
        else {
            UpdateVelocity(0, Gravity);
        }
    }

    private void ResetPositions() {
        Bounds.X = Game.Constrict(Bounds.X, 0, Game.MapWidth - Bounds.Width);
        if (Bounds.Y > Game.MapHeight) {
            Bounds.Y = -Bounds.Height * 4;
            Game.GetSoundEffect(SoundEffectID.Goal).Play();
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