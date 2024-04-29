using System;
using MonoGame.Extended;
using MonoGame.Extended.Input.InputListeners;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace SideBridge;

public class Player : Entity {

    private const float MaximumVerticalVelocity = 15f * Game.NativeFPS;
    private const float MaximumWalkingVelocity = 5f * Game.NativeFPS;
    private const float MaximumHorizontalVelocity = 20f * Game.NativeFPS;

    private const float HorizontalAcceleration = 1f * Game.NativeFPS;
    private const float HorizontalAirAcceleration = 0.4f * Game.NativeFPS;
    private const float Friction = 1f * Game.NativeFPS;
    private const float AirResistance = 0.2f * Game.NativeFPS;

    private const float SwordKnockbackX = 5f * Game.NativeFPS;
    private const float SwordKnockbackY = 8f * Game.NativeFPS;
    
    private const float ArrowKnockbackXFactor = 0.2f;
    private const float ArrowKnockbackY = 2f * Game.NativeFPS;
    
    private const float SprintHitKnockbackFactor = 1.5f;
    private const float AirKnockbackFactor = 2f;

    private const float SprintVelocityFactor = 1.5f;
    private const float SlowWalkVelocityFactor = 0.25f;

    private const float MaxRedHealth = 20;
    private const float SwordDamge = 6f;
    private const float SwordCriticalDamage = 7.5f;
    private const float TileReach = 3.5f;

    public const float BowCooldown = 3f;
    private const float BowChargeTime = 1.5f;
    private const float ArrowDamageFactor = 4.5f;
    private const float ArrowVelocityFactor = 45f * Game.NativeFPS;

    private const float AppleEatTime = 1.5f;
    private const float AppleCooldown = 0.2f;
    
    private const float VoidTime = 1f;
    private const float VoidCooldown = 0.2f;

    private const float SprintingSoundDelay = 0.3f;
    private const float WalkingSoundDelay = 0.45f;

    private bool _dropping;
    private bool _mouseDown;
    private bool _knockedBack;

    private float _bowCharge;
    public float TimeSinceBow {get; private set; }
    
    private float _appleCharge;
    private float _timeSinceApple;
    
    private float _voidCharge;
    private float _timeSinceVoid;

    public float Health { get; private set; }
    public int ActiveSlot { get; private set; }

    private readonly Keys[] _keyInputs;

    private float _walkTime;
    private readonly SoundEffectInstance _eatSound;
    private readonly SoundEffectInstance _voidSound;

    public bool OnGround => GetGroundTiles()
        .Any(t => (TileTypes.Solid(t.Type) || (TileTypes.SemiSolid(t.Type) && !_dropping)) && 
        Bounds.Bottom - Velocity.Y / Game.NativeFPS <= t.Bounds.Top)
    ;

    public bool Sprinting { get => Velocity.X != 0 && _sprintKeyDown; }
    private bool _sprintKeyDown;
    private bool _sprintHit;

    public readonly Team Team;
    public Vector2 SpawnPosition => Team == Team.Blue 
        ? new(420 - Bounds.Width / 2, 180) 
        : new(Game.TiledWorld.WidthInPixels - 420 - Bounds.Width / 2, 180);


    public Player(Texture2D texture, RectangleF bounds, Team team) : base(texture, bounds) {
        _keyInputs = team == Team.Blue ? Settings.DefaultPlayer1KeyBinds : Settings.DefaultPlayer2KeyBinds;

        var mouseListener = new MouseListener();
        mouseListener.MouseDown += TryUseSword;
        mouseListener.MouseWheelMoved += (sender, args) => {
            if (args.ScrollWheelDelta > 0) {
                ActiveSlot++;
                ActiveSlot %= Hotbar.SlotCount;
            }
            else if (args.ScrollWheelDelta < 0) {
                ActiveSlot--;
                if (ActiveSlot < 0) {
                    ActiveSlot = Hotbar.SlotCount - 1;
                }
            }
        };

        var keyListener = new KeyboardListener();
        keyListener.KeyPressed += ProcessKeyInput;
        _sprintKeyDown = true;

        Game.GameGraphics.AddListeners(mouseListener, keyListener);

        Health = MaxRedHealth;
        Team = team;
        Bounds.Position = SpawnPosition;

        _eatSound = Game.SoundEffectHandler.CreateInstance(SoundEffectID.Eat);
        _eatSound.Volume = 0.8f;
        _voidSound = Game.SoundEffectHandler.CreateInstance(SoundEffectID.Void);
        _voidSound.Volume = 0.5f;

        TimeSinceBow = BowCooldown;
    }

    private Tile[] GetGroundTiles() => new Tile[] { 
        Game.TiledWorld.GetTile(Bounds.X, Bounds.Bottom), 
        Game.TiledWorld.GetTile(Bounds.Right - 1, Bounds.Bottom) 
    };

    private void ProcessKeyInput(object sender, KeyboardEventArgs args) {
        if (args.Key == _keyInputs[(int) PlayerAction.Sprint]) {
            ToggleSprint();
            return;
        }

        SelectHotbarSlot(args.Key);
    }

    private void ToggleSprint() {
        _sprintKeyDown = !Sprinting;
        if (!_sprintKeyDown) {
            _sprintHit = false;
        }
    }

    private void SelectHotbarSlot(Keys key) {
        PlayerAction[] hotbarSlots = Enum.GetValues<PlayerAction>()[
            (int) PlayerAction.Hotbar1..((int) PlayerAction.Hotbar4 + 1)
        ];
        for (var i = 0; i < hotbarSlots.Length; i++) {
            if (_keyInputs[(int) hotbarSlots[i]] == key) {
                ActiveSlot = i;
            }
        }
    }


    public override void OnCollision(Entity other) {
        if (other is Arrow arrow && arrow.PlayerTeam != Team && Game.EntityWorld.Contains(arrow)) {
            RegisterArrowKnockback(arrow);
            RegisterDamage(arrow.Damage);
            if (Health != MaxRedHealth) {
                Game.SoundEffectHandler.PlaySound(SoundEffectID.ArrowHit);
            }
            Game.EntityWorld.Remove(arrow);
        }
    }

    public override void OnTileCollision(Tile tile) {
        if (tile.Type == TileType.Goal) {
            if (ScoringHandler.GetGoalTeam(tile) == Team || !Game.ScoringHandler.ScoreGoal(this)) {
                Game.SoundEffectHandler.PlaySound(SoundEffectID.Teleport);
                OnDeath();
            }
            return;
        }

        if (TileTypes.SemiSolid(tile.Type) && (!GetGroundTiles().Contains(tile) || !OnGround)) {
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
        Vector2 mousePos = Game.GameCamera.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        if (ActiveSlot == Hotbar.SwordSlot && Settings.GameState == GameState.InGame) {
            Vector2 center = Bounds.Center;
            Vector2 vecLine = mousePos - center;
            vecLine.Normalize();
            vecLine *= TileReach * Game.TiledWorld.TileSize;
            
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

        if (_bowCharge == 0) {
            return;
        }

        var playerPos = Bounds.Center;
        var vec = new Vector2(mousePos.X, mousePos.Y) - (Vector2) Bounds.Center;
        vec.Normalize();
        vec *= _bowCharge / BowChargeTime * ArrowVelocityFactor;

        var pos = new Vector2(playerPos.X, playerPos.Y);
        for (int i = 0; i < 20; i++) {
            pos.X += vec.X / Game.NativeFPS;
            pos.Y += vec.Y / Game.NativeFPS;
            vec.Y += Game.Gravity;
            spriteBatch.DrawCircle(pos, 5, 100, Color.White, 10f);
        }
    }

    public void RegisterDamage(float dmg) {
        Health -= dmg;
        if (Health <= 0) {
            OnDeath();
            Game.SoundEffectHandler.PlaySound(SoundEffectID.Kill);
        }
    }

    private void RegisterArrowKnockback(Arrow arrow) {
        Velocity = arrow.Velocity * ArrowKnockbackXFactor;
        Velocity.Y = -ArrowKnockbackY;
        _sprintKeyDown = false;
        _knockedBack = true;
    }

    private void RegisterSwordKnockback(Player player) {
        var knockback = new Vector2(
            player.Bounds.Center.X < Bounds.Center.X ? SwordKnockbackX : -SwordKnockbackX, 
            -SwordKnockbackY
        );
        if (player.Sprinting && !player._sprintHit) {
            knockback.X *= SprintHitKnockbackFactor;
            player._sprintHit = true;
        }
        if (!OnGround) {
            knockback.X *= AirKnockbackFactor;
        }
        Velocity = knockback;
        _knockedBack = true;
    }

    public void OnDeath() {
        Health = MaxRedHealth;
        Velocity = Vector2.Zero;
        Bounds.Position = SpawnPosition;
        _sprintKeyDown = true;
        TimeSinceBow = BowCooldown;
        _bowCharge = 0;
        _appleCharge = 0;
        _timeSinceApple = 0;
        _eatSound.Stop();
    }

    public override void Update(GameTime gameTime) {
        SetVerticalVelocity();
        SetHorizontalVelocity();
        UpdatePosition(gameTime);
        ResetPositions();

        _dropping = Keyboard.GetState().IsKeyDown(_keyInputs[(int) PlayerAction.Drop]);

        _timeSinceApple += gameTime.GetElapsedSeconds();
        _timeSinceVoid += gameTime.GetElapsedSeconds();
        if (OnGround && Velocity.X != 0) {
            _walkTime += gameTime.GetElapsedSeconds();
            if (_walkTime >= (Sprinting ? SprintingSoundDelay : WalkingSoundDelay)) { 
                _walkTime = 0;
                SoundEffectInstance walkSound = Game.SoundEffectHandler.CreateInstance(SoundEffects.GetRandomWalkSound());
                walkSound.Volume = 0.15f;
                walkSound.Play();
            }
        }
        else{
            _walkTime = SprintingSoundDelay;
        }
        if (TimeSinceBow < BowCooldown) {
            TimeSinceBow += gameTime.GetElapsedSeconds();
        }
        if (ActiveSlot != Hotbar.BowSlot && _bowCharge > 0) {
            _bowCharge = 0;
            _mouseDown = false;
        }
        if (ActiveSlot != Hotbar.AppleSlot && _appleCharge > 0) {
            _appleCharge = 0;
        }

        if (Settings.GameState != GameState.InGame) {
            return;
        }

        switch (ActiveSlot) {
            case Hotbar.BowSlot:
                TryShootBow(gameTime);
                break;
            case Hotbar.BlockSlot:
                TryModifyBlock();
                break;
            case Hotbar.AppleSlot:
                TryEatApple(gameTime);
                break;
        }
        TryVoid(gameTime);
    }

    private void UpdatePosition(GameTime gameTime) {
        if (!_knockedBack && (_bowCharge > 0 || _appleCharge > 0 || _voidCharge > 0)) {
            Bounds.X += Velocity.X * SlowWalkVelocityFactor * gameTime.GetElapsedSeconds();
        }
        else if (!_knockedBack && Sprinting) {
            Bounds.X += Velocity.X * SprintVelocityFactor * gameTime.GetElapsedSeconds();
        }
        else {
            Bounds.X += Velocity.X * gameTime.GetElapsedSeconds();
        }
        Bounds.Y += Velocity.Y * gameTime.GetElapsedSeconds();
    }

    private void TryUseSword(object sender, MouseEventArgs args) {
        if (ActiveSlot != Hotbar.SwordSlot || Settings.GameState != GameState.InGame) {
            return;
        }
        Vector2 mousePos = Game.GameCamera.ScreenToWorld(args.Position.ToVector2());
        Vector2 vecLine = mousePos - (Vector2) Bounds.Center;
        vecLine.Normalize();
        vecLine *= TileReach * Game.TiledWorld.TileSize;

        var player = Game.EntityWorld.FindEntity<Player>(entity => entity != this && findIntersection(entity.Bounds).intersects);
        if (player != null) {
            Tile[] tiles = Game.TiledWorld.FindTiles(tile => findIntersection(tile.Bounds).intersects && TileTypes.Solid(tile.Type));
            foreach (var tile in tiles) {
                if (findIntersection(tile.Bounds).distanceAlongLine < findIntersection(player.Bounds).distanceAlongLine) {
                    return;
                }
            }
            player.RegisterSwordKnockback(this);
            player.RegisterDamage(OnGround ? SwordDamge : SwordCriticalDamage);
            if (player.Health != MaxRedHealth) {
                Game.SoundEffectHandler.PlaySound(SoundEffectID.SwordHit);
            }
        }
        
        (bool intersects, float distanceAlongLine) findIntersection(RectangleF bounds) {
            bool result1 = bounds.IntersectsLine(Bounds.Center, Bounds.Center + vecLine, out var result2);
            return (result1, Vector2.Distance(Bounds.Center, result2));
        }
    }

    private void TryShootBow(GameTime gameTime) {
        if (TimeSinceBow < BowCooldown) {
            return;
        }
        var mouseState = Mouse.GetState();
        if (mouseState.LeftButton == ButtonState.Pressed) {
            _mouseDown = true;
            _bowCharge += gameTime.GetElapsedSeconds();
            if (_bowCharge > BowChargeTime) {
                _bowCharge = BowChargeTime;
            }
        }
        if (_mouseDown && mouseState.LeftButton == ButtonState.Released) {
            var arrowTexture = Arrow.ArrowTexture;
            Vector2 spawnPos = Bounds.Center - new Vector2(arrowTexture.Width / 2, arrowTexture.Height / 2);
            
            var arrow = new Arrow(
                new(spawnPos.X, spawnPos.Y, arrowTexture.Width, 24), 
                _bowCharge / BowChargeTime * ArrowDamageFactor,
                Team
            );
            Game.SoundEffectHandler.PlaySound(SoundEffects.GetRandomBowSound());

            
            var mousePos = Game.GameCamera.ScreenToWorld(mouseState.Position.ToVector2());
            var vec = new Vector2(mousePos.X, mousePos.Y) - spawnPos;
            vec.Normalize();
            vec *= _bowCharge / BowChargeTime * ArrowVelocityFactor;
            arrow.Velocity = vec;
            Game.EntityWorld.Add(arrow);

            _mouseDown = false;
            _bowCharge = 0;
            TimeSinceBow = 0;
        }
    }

    private void TryModifyBlock() {
        int tileSize = Game.TiledWorld.TileSize;
        
        Vector2 mousePos = Game.GameCamera.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        var tileX = (int) (mousePos.X / tileSize) * tileSize;
        var tileY = (int) (mousePos.Y / tileSize) * tileSize;
        bool inRange = Vector2.Distance(new(tileX, tileY), Bounds.Position) < TileReach * tileSize && 
            tileY / tileSize >= TiledWorld.HeightLimit && 
            tileX / tileSize > TiledWorld.IslandWidths - 1 && 
            tileX / tileSize < Game.TiledWorld.WidthInPixels / tileSize - TiledWorld.IslandWidths;
        if (Mouse.GetState().LeftButton == ButtonState.Pressed && inRange && Game.TiledWorld.GetTile(mousePos.X, mousePos.Y).Type == TileType.Air) {
            PlaceTile(tileX, tileY, mousePos);
            return;
        }
        if (Mouse.GetState().RightButton == ButtonState.Pressed && inRange) {
            Game.TiledWorld.DamageTile(Game.TiledWorld.GetTile(mousePos.X, mousePos.Y));
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
        int tileSize = Game.TiledWorld.TileSize;

        if (Game.EntityWorld.FindEntity<Player>(player => player.Bounds.Intersects(new(tileX, tileY, tileSize, tileSize))) != null) {
            return;
        }
        Vector2[] adjacentTiles = {
            new(-tileSize, 0),
            new(0, -tileSize),
            new(tileSize, 0),
            new(0, tileSize)
        };
        foreach (var vec in adjacentTiles) {
            if (mousePos.X + vec.X > Game.TiledWorld.WidthInPixels || mousePos.X + vec.X < 0 || 
                mousePos.Y + vec.Y > Game.TiledWorld.HeightInPixels || mousePos.Y + vec.Y < 0) {
                continue;
            }
            TileType type = Game.TiledWorld.GetTile(mousePos.X + vec.X, mousePos.Y + vec.Y).Type;
            if (TileTypes.Solid(type) || TileTypes.SemiSolid(type)) {
                var normalBlockType = Team == Team.Red ? TileType.Red : TileType.Blue;
                var darkBlockType = Team == Team.Red ? TileType.DarkRed : TileType.DarkBlue;
                Game.TiledWorld.SetTileWithEffects(tileY / tileSize <= TiledWorld.HeightLimit ? darkBlockType : normalBlockType, mousePos.X, mousePos.Y);
                break;
            }
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
                Velocity.Y = -11f * Game.NativeFPS;
            }
        }
        else {
            UpdateVelocity(0, Game.Gravity);
        }
    }

    private void ResetPositions() {
        Bounds.X = Util.Constrict(Bounds.X, 0, Game.TiledWorld.WidthInPixels - Bounds.Width);
        if (Bounds.Y > Game.TiledWorld.HeightInPixels) {
            Bounds.Y = -Bounds.Height * 4;
            Game.SoundEffectHandler.CreateInstance(SoundEffectID.Teleport).Play();
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