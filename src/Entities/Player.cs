using System;
using MonoGame.Extended;
using MonoGame.Extended.Input.InputListeners;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using MonoGame.Extended.Input;

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
    private const float ArrowKnockbackY = 2f * Game.NativeFPS;
    
    private const float ArrowKnockbackXFactor = 0.2f;
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

    private const float AppleChargeTime = 1.5f;
    private const float AppleCooldown = 0.2f;
    
    private const float VoidChargeTime = 1f;
    private const float VoidCooldown = 0.2f;

    private const float SprintingSoundDelay = 0.3f;
    private const float WalkingSoundDelay = 0.45f;

    public readonly ulong UUID;

    private Vector2 _mousePos;
    private RectangleF _lastBounds;
    private bool _knockedBack;

    public float Health { get; private set; }
    
    private int _activeSlot;
    
    private readonly Keys[] _keyInputs;
    private readonly bool[] _activeActions;

    private readonly ChargedValue _apple;
    private readonly ChargedValue _bow;
    private readonly ChargedValue _void;

    private float _walkTime;
    private readonly SoundEffectInstance _eatSound;
    private readonly SoundEffectInstance _voidSound;

    private bool _sprintHit;

    public readonly Team Team;
    
    public Player(Texture2D texture, RectangleF bounds, Team team, bool thisClient) : base(texture, bounds) {
        UUID = team == Team.Blue ? Settings.Player1UUID : Settings.Player2UUID;

        _keyInputs = team == Team.Blue ? Settings.DefaultPlayer1KeyBinds : Settings.DefaultPlayer2KeyBinds;
        _activeActions = new bool[Enum.GetValues<PlayerAction>().Length];
        
        _bow = new ChargedValue(BowChargeTime, BowCooldown);
        _apple = new ChargedValue(AppleChargeTime, AppleCooldown);
        _void = new ChargedValue(VoidChargeTime, VoidCooldown);

        Health = MaxRedHealth;
        Team = team;
        Bounds.Position = SpawnPosition;

        _eatSound = Game.SoundEffectHandler.CreateInstance(SoundEffectID.Eat);
        _eatSound.Volume = 0.8f;
        _voidSound = Game.SoundEffectHandler.CreateInstance(SoundEffectID.Void);
        _voidSound.Volume = 0.5f;

        if (thisClient) {
            CreateListeners();
        }
    }

    public int ActiveSlot { 
        get => _activeSlot; 

        private set {
            _activeSlot = value;
            if (ActiveSlot != Hotbar.BowSlot) {
                _bow.Charge = 0;
            }
            if (ActiveSlot != Hotbar.AppleSlot) {
                _apple.Charge = 0;
                _eatSound.Stop();
            }
        } 
    }

    public bool OnGround => GetGroundTiles()
        .Any(t => (TileTypes.Solid(t.Type) || (TileTypes.SemiSolid(t.Type) && !_activeActions[(int) PlayerAction.Drop])) && 
        _lastBounds.Bottom <= t.Bounds.Top)
    ;

    public float TimeSinceBow => _bow.TimeSince;

    public bool Sprinting { get => Velocity.X != 0 && _activeActions[(int) PlayerAction.Sprint]; }

    private ChargedValue[] ChargedValues => new ChargedValue[] { _apple, _bow, _void };

    public Vector2 SpawnPosition => Team == Team.Blue 
        ? new(420 - Bounds.Width / 2, 160) 
        : new(Game.TiledWorld.WidthInPixels - 420 - Bounds.Width / 2, 160);

    public Vector2 MousePos { get => Game.GameCamera.ScreenToWorld(_mousePos); private set => _mousePos = value; }

    public override void Draw(SpriteBatch spriteBatch) {
        base.Draw(spriteBatch);
        if (ActiveSlot == Hotbar.SwordSlot && Settings.GameState == GameState.InGame) {
            Vector2 center = Bounds.Center;
            Vector2 vecLine = MousePos - center;
            vecLine.Normalize();
            vecLine *= TileReach * Game.TiledWorld.TileSize;
            
            spriteBatch.DrawLine(center.X, center.Y, center.X + vecLine.X, center.Y + vecLine.Y, Color.GhostWhite * 0.5f, 20f);
        }

        if (_apple.Charge > 0) {
            var barBounds = new RectangleF(Bounds.X - 2, Bounds.Y - 10, Bounds.Width + 4, 6);
            spriteBatch.FillRectangle(barBounds, Color.DarkRed);
            spriteBatch.DrawPercentageBar(barBounds, _apple.Charge / AppleChargeTime);
        }
        if (_void.Charge > 0) {
            var barBounds = new RectangleF(Bounds.X - 2, Bounds.Y - (_apple.Charge > 0 ? 20 : 10), Bounds.Width + 4, 6);
            spriteBatch.FillRectangle(barBounds, Color.Purple);
            spriteBatch.DrawPercentageBar(barBounds, _void.Charge / VoidChargeTime, false);
        }
        if (_bow.Charge == 0) {
            return;
        }

        Vector2 vec = CalculateArrowLaunchVelocity(Bounds.Center);
        Vector2 pos = Bounds.Center;
        for (int i = 0; i < 20; i++) {
            pos.X += vec.X / Game.NativeFPS;
            pos.Y += vec.Y / Game.NativeFPS;
            vec.Y += Game.Gravity;
            spriteBatch.DrawCircle(pos, 5, 100, Color.White, 10f);
        }
    }

    public override void Update(GameTime gameTime) {
        SetVerticalVelocity();
        SetHorizontalVelocity();
        UpdatePosition(gameTime);
        ResetPositions();

        foreach (ChargedValue value in ChargedValues) {
            value.Increment(gameTime.GetElapsedSeconds(), false);
        }

        PlayWalkSounds(gameTime);

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

        var tileBounds = tile.Bounds;
        var intersection = Bounds.Intersection(tileBounds);

        if (_lastBounds.Bottom <= tileBounds.Top) {
            Bounds.Y -= intersection.Height;
            Velocity.Y = 0;
        }
        else if (_lastBounds.Top >= tileBounds.Bottom) {
            Bounds.Y += intersection.Height;
            Velocity.Y = 0;
        }
        else if (_lastBounds.Right <= tileBounds.Left) {
            Bounds.X -= intersection.Width;
            Velocity.X = 0;
            _walkTime = 0;
        }
        else if (_lastBounds.Left >= tileBounds.Right) {
            Bounds.X += intersection.Width;
            Velocity.X = 0;
            _walkTime = 0;
        }
    }

    public void OnDeath() {
        Health = MaxRedHealth;
        
        Velocity = Vector2.Zero;
        Bounds.Position = SpawnPosition;
        
        _eatSound.Stop();
        
        _bow.Reset();
        _apple.Restart();

        ProcessActionInternal(PlayerAction.LeftMouse, false);
        ProcessActionInternal(PlayerAction.RightMouse, false);
    }

    public void RegisterDamage(float dmg) {
        Health -= dmg;
        ProcessActionInternal(PlayerAction.Sprint, false);
        if (Health <= 0) {
            OnDeath();
            Game.SoundEffectHandler.PlaySound(SoundEffectID.Kill);
        }
    }

    private void RegisterArrowKnockback(Arrow arrow) {
        Velocity = arrow.Velocity * ArrowKnockbackXFactor;
        Velocity.Y = -ArrowKnockbackY;
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

    private void UpdatePosition(GameTime gameTime) {
        _lastBounds = Bounds;
        if (!_knockedBack && ChargedValues.Any(val => val.Charge > 0)) {
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

    private void TryUseSword() {
        if (ActiveSlot != Hotbar.SwordSlot || Settings.GameState != GameState.InGame) {
            return;
        }
        Vector2 vecLine = (MousePos - (Vector2) Bounds.Center).NormalizedCopy() * (TileReach * Game.TiledWorld.TileSize);

        var player = Game.EntityWorld.Find<Player>(entity => entity != this && findIntersection(entity.Bounds).intersects);
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
        if (_bow.OnCooldown) {
            return;
        }
        if (_activeActions[(int) PlayerAction.LeftMouse]) {
            _bow.Increment(gameTime.GetElapsedSeconds(), true);
        }
        else if (_bow.Charge > 0) {
            CreateArrow();
            _bow.Restart();
        }
    }

    private void TryModifyBlock() {
        int tileSize = Game.TiledWorld.TileSize;
        
        var tileX = (int) (MousePos.X / tileSize) * tileSize;
        var tileY = (int) (MousePos.Y / tileSize) * tileSize;
        bool inRange = Vector2.Distance(new(tileX, tileY), Bounds.Position) < TileReach * tileSize && 
            tileY / tileSize >= TiledWorld.HeightLimit && 
            tileX / tileSize > TiledWorld.IslandWidths - 1 && 
            tileX / tileSize < Game.TiledWorld.WidthInPixels / tileSize - TiledWorld.IslandWidths;
        if (_activeActions[(int) PlayerAction.LeftMouse] && inRange && Game.TiledWorld.GetTile(MousePos.X, MousePos.Y).Type == TileType.Air) {
            PlaceTile(tileX, tileY);
            return;
        }
        if (_activeActions[(int) PlayerAction.RightMouse] && inRange) {
            Game.TiledWorld.DamageTile(Game.TiledWorld.GetTile(MousePos.X, MousePos.Y));
        }
    }
    
    private void TryEatApple(GameTime gameTime) {
        if (_apple.OnCooldown || !_activeActions[(int) PlayerAction.LeftMouse]) {
            _eatSound.Stop();
            _apple.Charge = 0;
            return;
        }

        if (_apple.Charge == 0) {
            _eatSound.Play();
        }
        if (_apple.Increment(gameTime.GetElapsedSeconds(), true)) {
            _apple.Restart();
            Health = 24;
        }
    }

    private void TryVoid(GameTime gameTime) {
        if (_void.OnCooldown || !_activeActions[(int) PlayerAction.Void]) {
            _voidSound.Stop();
            _void.Charge = 0;
            return;
        }

        if (_void.Charge == 0) {
            _voidSound.Play();
        }
        if (_void.Increment(gameTime.GetElapsedSeconds(), true)) {
            _void.Restart();
            OnDeath();
        }
    }

    private void TrySelectHotbarSlot(PlayerAction action) {
        int newSlot = Array.IndexOf(Enum.GetValues<PlayerAction>()[
            (int) PlayerAction.Hotbar1..((int) PlayerAction.Hotbar4 + 1)
        ], action);
        if (newSlot >= 0) {
            ActiveSlot = newSlot;
        }
    }

    private void CreateArrow() {
        var arrowTexture = Arrow.ArrowTexture;
        Vector2 spawnPos = Bounds.Center - new Vector2(arrowTexture.Width / 2, arrowTexture.Height / 2);
        
        var arrow = new Arrow(
            new(spawnPos.X, spawnPos.Y, arrowTexture.Width, 24), 
            _bow.Charge / BowChargeTime * ArrowDamageFactor,
            Team
        );
        Game.SoundEffectHandler.PlaySound(SoundEffects.GetRandomBowSound());

        arrow.Velocity = CalculateArrowLaunchVelocity(spawnPos);
        Game.EntityWorld.Add(arrow);
    }

    private Vector2 CalculateArrowLaunchVelocity(Vector2 spawnPos) {
        return (new Vector2(MousePos.X, MousePos.Y) - spawnPos).NormalizedCopy() * 
            (_bow.Charge / BowChargeTime * ArrowVelocityFactor);
    }

    private void PlayWalkSounds(GameTime gameTime) {
        if (OnGround && Velocity.X != 0) {
            _walkTime += gameTime.GetElapsedSeconds();
            if (_walkTime >= (Sprinting ? SprintingSoundDelay : WalkingSoundDelay)) { 
                _walkTime = 0;
                SoundEffectInstance walkSound = Game.SoundEffectHandler.CreateInstance(SoundEffects.GetRandomWalkSound());
                walkSound.Volume = 0.15f;
                walkSound.Play();
            }
        }
        else {
            _walkTime = SprintingSoundDelay;
        }
    }

    private void PlaceTile(float tileX, float tileY) {
        int tileSize = Game.TiledWorld.TileSize;

        if (Game.EntityWorld.Find<Player>(player => player.Bounds.Intersects(new(tileX, tileY, tileSize, tileSize))) != null) {
            return;
        }
        Vector2[] adjacentTiles = {
            new(-tileSize, 0),
            new(0, -tileSize),
            new(tileSize, 0),
            new(0, tileSize)
        };
        foreach (var vec in adjacentTiles) {
            if (MousePos.X + vec.X > Game.TiledWorld.WidthInPixels || MousePos.X + vec.X < 0 || 
                MousePos.Y + vec.Y > Game.TiledWorld.HeightInPixels || MousePos.Y + vec.Y < 0) {
                continue;
            }
            TileType type = Game.TiledWorld.GetTile(MousePos.X + vec.X, MousePos.Y + vec.Y).Type;
            if (TileTypes.Solid(type) || TileTypes.SemiSolid(type)) {
                var normalBlockType = Team == Team.Red ? TileType.Red : TileType.Blue;
                var darkBlockType = Team == Team.Red ? TileType.DarkRed : TileType.DarkBlue;
                Game.TiledWorld.SetTileWithEffects(tileY / tileSize <= TiledWorld.HeightLimit 
                    ? darkBlockType 
                    : normalBlockType, 
                MousePos.X, MousePos.Y);
                break;
            }
        }
    }

    private Tile[] GetGroundTiles() => new Tile[] { 
        Game.TiledWorld.GetTile(Bounds.X, Bounds.Bottom), 
        Game.TiledWorld.GetTile(Bounds.Right - 1, Bounds.Bottom) 
    };

    private void SetHorizontalVelocity() {
        bool leftDown = _activeActions[(int) PlayerAction.Left];
        bool rightDown = _activeActions[(int) PlayerAction.Right];
        
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
            if (_activeActions[(int) PlayerAction.Jump]) {
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

    private void CreateListeners() {
        var mouseListener = new MouseListener();
        mouseListener.MouseDown += (sender, args) => {
            if (args.Button == MouseButton.Left) {
                ProcessActionInternal(PlayerAction.LeftMouse, true);
            }
            else if (args.Button == MouseButton.Right) {
                ProcessActionInternal(PlayerAction.RightMouse, true);
            }
            TryUseSword();
        };
        mouseListener.MouseUp += (sender, args) => {
            if (args.Button == MouseButton.Left) {
                ProcessActionInternal(PlayerAction.LeftMouse, false);
            }
            else if (args.Button == MouseButton.Right) {
                ProcessActionInternal(PlayerAction.RightMouse, false);
            }
        };
        mouseListener.MouseWheelMoved += async (sender, args) => {
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
            await GameClient.SendAction(UUID, (PlayerAction) (ActiveSlot + (int) PlayerAction.Hotbar1 - 1), true);
        };
        mouseListener.MouseMoved += async (sender, args) => {
            _mousePos = args.Position.ToVector2();
            _mousePos.X = Util.Constrict(_mousePos.X, 0, Game.GameGraphics.WindowWidth);
            _mousePos.Y = Util.Constrict(_mousePos.Y, 0, Game.GameGraphics.WindowHeight);
            await GameClient.SendAction(UUID, _mousePos);
        };

        var keyListener = new KeyboardListener();
        keyListener.KeyPressed += (sender, args) => ProcessActionInternal((PlayerAction) Array.IndexOf(_keyInputs, args.Key), true);
        keyListener.KeyReleased += (sender, args) => ProcessActionInternal((PlayerAction) Array.IndexOf(_keyInputs, args.Key), false);

        Game.GameGraphics.AddListeners(mouseListener, keyListener);
    }

    public void ProcessAction(PlayerAction action, bool active) {
        _activeActions[(int) action] = active;
        
        if (!active) {
            return;
        }
        TrySelectHotbarSlot(action);
    }

    private async void ProcessActionInternal(PlayerAction action, bool active) {
        if ((int) action == -1 || _activeActions[(int) action] == active) {
            return;
        }
        ProcessAction(action, active);
        await GameClient.SendAction(UUID, action, active);
    }

    public void ProcessMouseMove(Vector2 mousePos) => _mousePos = mousePos;
}