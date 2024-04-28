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

    public bool OnGround {
        get => Bounds.Bottom < Game.TiledWorld.HeightInPixels && Bounds.Bottom > 0 &&
            TileTypes.Solid(Game.TiledWorld.GetTile(Bounds.X, Bounds.Bottom).Type) ||
            TileTypes.Solid(Game.TiledWorld.GetTile(Bounds.Right - 1, Bounds.Bottom).Type);
    }

    public bool Sprinting { get => Velocity.X != 0 && _sprintKeyDown; }
    private bool _sprintKeyDown;
    private bool _sprintHit;

    public readonly Team Team;
    public Vector2 SpawnPosition { 
        get => Team == Team.Blue ? new(420 - Bounds.Width / 2, 180) : new(Game.TiledWorld.WidthInPixels - 420 - Bounds.Width / 2, 180);
    }


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
        keyListener.KeyPressed += (sender, args) => {
            if (args.Key == _keyInputs[(int) PlayerAction.Sprint]) {
                _sprintKeyDown = !Sprinting;
                if (!_sprintKeyDown) {
                    _sprintHit = false;
                }
            }
            else {
                PlayerAction[] hotbarSlots = Enum.GetValues<PlayerAction>()[
                    (int) PlayerAction.Hotbar1..((int) PlayerAction.Hotbar4 + 1)
                ];
                for (var i = 0; i < hotbarSlots.Length; i++) {
                    if (_keyInputs[(int) hotbarSlots[i]] == args.Key) {
                        ActiveSlot = i;
                    }
                }
            }
        };
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
        if (ActiveSlot == Hotbar.SwordSlot && !Settings.LobbyMode) {
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
        if (ActiveSlot != Hotbar.BowSlot && _bowCharge > 1) {
            _bowCharge = 1;
            _mouseDown = false;
        }
        if (ActiveSlot != Hotbar.AppleSlot && _appleCharge > 0) {
            _appleCharge = 0;
        }

        if (Settings.LobbyMode) {
            return;
        }

        switch (ActiveSlot) {
            case Hotbar.BowSlot:
                TryShootBow();
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
        if (ActiveSlot != Hotbar.SwordSlot || Settings.LobbyMode) {
            return;
        }
        Vector2 mousePos = Game.GameCamera.ScreenToWorld(args.Position.ToVector2());
        Vector2 vecLine = mousePos - (Vector2) Bounds.Center;
        vecLine.Normalize();
        vecLine *= TileReach * TileSize;

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
            Game.SoundEffectHandler.PlaySound(SoundEffects.GetRandomBowSound());

            
            var mousePos = Game.GameCamera.ScreenToWorld(mouseState.Position.ToVector2());
            var vec = new Vector2(mousePos.X, mousePos.Y) - spawnPos;
            vec.Normalize();
            vec.X *= _bowCharge;
            vec.Y *= _bowCharge;
            
            arrow.Velocity = vec;
            Game.EntityWorld.Add(arrow);

            _mouseDown = false;
            _bowCharge = 1;
            TimeSinceBow = 0;
        }
    }

    private void TryModifyBlock() {
        Vector2 mousePos = Game.GameCamera.ScreenToWorld(Mouse.GetState().Position.ToVector2());
        var tileX = (int) (mousePos.X / TileSize) * TileSize;
        var tileY = (int) (mousePos.Y / TileSize) * TileSize;
        bool inRange = Vector2.Distance(new(tileX, tileY), Bounds.Position) < TileReach * TileSize && 
            tileY / TileSize >= HeightLimit && tileX / TileSize > IslandWidths - 1 && tileX / TileSize < Game.TiledWorld.WidthInPixels / TileSize - IslandWidths;
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
        if (Game.EntityWorld.FindEntity<Player>(player => player.Bounds.Intersects(new(tileX, tileY, TileSize, TileSize))) != null) {
            return;
        }
        Vector2[] adjacentTiles = {
            new(-TileSize, 0),
            new(0, -TileSize),
            new(TileSize, 0),
            new(0, TileSize)
        };
        foreach (var vec in adjacentTiles) {
            if (mousePos.X + vec.X > Game.TiledWorld.WidthInPixels || mousePos.X + vec.X < 0 || 
                mousePos.Y + vec.Y > Game.TiledWorld.HeightInPixels || mousePos.Y + vec.Y < 0) {
                continue;
            }
            if (TileTypes.Solid(Game.TiledWorld.GetTile(mousePos.X + vec.X, mousePos.Y + vec.Y).Type)) {
                var normalBlockType = Team == Team.Red ? TileType.Red : TileType.Blue;
                var darkBlockType = Team == Team.Red ? TileType.DarkRed : TileType.DarkBlue;
                Game.TiledWorld.SetTileWithEffects(tileY / TileSize <= HeightLimit ? darkBlockType : normalBlockType, mousePos.X, mousePos.Y);
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
                Velocity.Y = -11f;
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