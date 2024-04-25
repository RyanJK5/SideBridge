using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using MonoGame.Extended.Particles.Profiles;
using MonoGame.Extended.TextureAtlases;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.ViewportAdapters;
using System.Collections.Generic;
using MonoGame.Extended.Entities;

namespace SideBridge;

public class Game : Microsoft.Xna.Framework.Game {

    private static Game s_main;

    private static void Main() {
        s_main = new();
        using var game = s_main;
        s_main.Run();
    }

    private readonly GraphicsDeviceManager _graphics;
    private readonly GameCamera _camera;
    private SpriteBatch _spriteBatch;
    private TiledWorld _tiledWorld;
    private EntityWorld _entityWorld;
    private ScoreBar _scoreBar;

    public static Player Player1 { get; private set; }
    public static Player Player2 { get; private set; }

    private SoundEffect[] _soundEffects;
    private ParticleEffect[] _blockParticleEffects;

    public static SoundEffect GetSoundEffect(SoundEffectID id) =>
        s_main._soundEffects[(int) id];

    public static SpriteFont Font { get; private set; }

    public static int WindowWidth { get => s_main._graphics.PreferredBackBufferWidth; }
    public static int WindowHeight { get => s_main._graphics.PreferredBackBufferHeight; }

    public static float MapWidth { get => s_main._tiledWorld.WidthInPixels; }
    public static float MapHeight { get => s_main._tiledWorld.HeightInPixels; }

    public static Tile GetTile(float x, float y) {
        TiledWorld tiledWorld = s_main._tiledWorld;
        int tileSize = tiledWorld.TileSize;
        if (x > MapWidth || x < 0 || y > MapHeight || y < 0) {
            return new Tile(TileType.Air, new((int) (x / tileSize) * tileSize, (int) (y / tileSize) * tileSize, tileSize, tileSize));
        }
        return tiledWorld[(int) (x / tileSize), (int) (y / tileSize)];
    }

    public static void CheckTileCollisions(Entity entity) => s_main._tiledWorld.CheckTileCollisions(entity);

    public static void SetTile(TileType type, float x, float y) {
        TiledWorld tiledWorld = s_main._tiledWorld;
        int tileSize = tiledWorld.TileSize;
        int intX = (int) (x / tileSize);
        int intY = (int) (y / tileSize);
        if (intX < 0 || intY < 0 || intX >= tiledWorld.Width || intY >= tiledWorld.Height) {
            return;
        }
        if (type == TileType.Air) {
            s_main._blockParticleEffects[(int) GetTile(x, y).Type]
                ?.Trigger(WorldToScreen(new Vector2(x + tileSize / 2, y + tileSize / 2)));
        }
        else if (TileTypes.Breakable(type)) {
            s_main._soundEffects[(int) SoundEffects.GetRandomBlockSound()].Play();
        }
        tiledWorld.SetTile(type, intX, intY);
    }

    public static void AddEntity(Entity entity) => s_main._entityWorld.Add(entity);
    public static void RemoveEntity(Entity entity) => s_main._entityWorld.Remove(entity);
    public static bool ContainsEntity(Entity entity) => s_main._entityWorld.Contains(entity);

    #nullable enable
    public static T? FindEntity<T>(Predicate<T> testCase) => s_main._entityWorld.FindEntity(testCase);

    public static Tile[] FindTiles(Predicate<Tile> testCase) => s_main._tiledWorld.FindTiles(testCase);
    #nullable disable

    public static Vector2 ScreenToWorld(Vector2 position) => s_main._camera.ScreenToWorld(position.X, position.Y);
    public static Vector2 WorldToScreen(Vector2 position) => s_main._camera.WorldToScreen(position.X, position.Y);

    public static float Constrict(float val, float min, float max) => MathF.Max(MathF.Min(val, max), min);

    public static float MoveAtSpeed(float val, float speed, float target) {
        if (val < target) {
            val += speed;
        }
        else if (val > target) {
            val -= speed;
        }
        if (MathF.Abs(val - target) < speed) {
            val = target;
        }
        return val;
    }

    public static void AddListeners(params InputListener[] listeners) {
        s_main.Components.Add(new InputListenerComponent(s_main, listeners));
    }

    public static Matrix GetViewMatrix() => s_main._camera.GetViewMatrix();

    public static bool ScoreGoal(Player player) {
        var scoreBar = s_main._scoreBar;
        if (scoreBar.RedWon || scoreBar.BlueWon) {
            return false;
        }
        if (player == Player1) {
            scoreBar.BlueScore++;
        }
        else {
            scoreBar.RedScore++;
        }
        if (!CheckWin()) {
            NewRound();
        }
        return true;
    }

    public static bool CheckWin() {
       var scoreBar = s_main._scoreBar;
        if (scoreBar.BlueWon) {
            EndGame(Team.Blue);
            return true;
        }
        if (scoreBar.RedWon) {
            EndGame(Team.Red);
            return true;
        }
        return false;
    }

    public static void EndGame(Team team) {
        GetSoundEffect(SoundEffectID.Win).CreateInstance().Play();
        StartRound();

        var tiledWord = s_main._tiledWorld;
        if (team == Team.Blue) {
            Player1.OnDeath();
            Player2.OnDeath();
            Player2.Bounds.Position = new(
                tiledWord.WidthInPixels / 2 - Player2.Bounds.Width / 2, 
                tiledWord.HeightInPixels / 2 - Player2.Bounds.Height / 2
            );
            RemoveEntity(Player2);
        }
        else {
            Player2.OnDeath();
            Player1.OnDeath();
            Player1.Bounds.Position = new(
                tiledWord.WidthInPixels / 2 - Player1.Bounds.Width / 2, 
                tiledWord.HeightInPixels / 2 - Player1.Bounds.Height / 2
            );
            RemoveEntity(Player1);
        }
    }

    public static Team GetGoalTeam(Tile goal) {
        if (goal.Type != TileType.Goal) {
            throw new ArgumentException();
        }
        return goal.Bounds.X > s_main._tiledWorld.WidthInPixels / 2 ? Team.Red : Team.Blue;
    }

    public static Player GetOtherPlayer(Team thisTeam) =>
        thisTeam == Team.Red ? Player1 : Player2;

    public static void Overtime() {
        s_main._tiledWorld.Reset();
        NewRound();
    }

    public static void NewRound() {
        Player1.OnDeath();
        Player2.OnDeath();
        s_main.SetPlatforms(false);
        s_main._scoreBar.Pause();
    }

    public static void StartRound() {
        s_main.SetPlatforms(true);
        GetSoundEffect(SoundEffectID.Kill).CreateInstance().Play();
    }

    private void SetPlatforms(bool destroy) {
        var tileType1 = destroy ? TileType.Air : TileType.Glass;
        var tileType2 = destroy ? TileType.Air : TileType.White;
        
        Player[] players = {Player1, Player2};
        foreach (var player in players) {
            for (var i = 0; i < 5; i++) {
                if (i == 0 || i == 4) {
                    for (var j = 0; j < 2; j++) {
                        SetTile(
                            tileType1, 
                            player.SpawnPosition.X - _tiledWorld.TileSize * 2 + _tiledWorld.TileSize * i, 
                            player.SpawnPosition.Y - _tiledWorld.TileSize * j
                        );
                    }
                }
                SetTile(
                    tileType1, 
                    player.SpawnPosition.X - _tiledWorld.TileSize * 2 + _tiledWorld.TileSize * i, 
                    player.SpawnPosition.Y - _tiledWorld.TileSize * 2
                );
                SetTile(
                    tileType2, 
                    player.SpawnPosition.X - _tiledWorld.TileSize * 2 + _tiledWorld.TileSize * i, 
                    player.SpawnPosition.Y + _tiledWorld.TileSize
                );
            }
        }
    }

    private Game() {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.ToggleFullScreen();

        var viewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, 1920, 1080);
        var camera = new OrthographicCamera(viewportAdapter)
        {
            MinimumZoom = 0.75f,
            MaximumZoom = 2f
        };
        _camera = new GameCamera(camera);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize() {
        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        _graphics.ApplyChanges();
        _entityWorld = new(GraphicsDevice);
        _soundEffects = new SoundEffect[Enum.GetValues<SoundEffectID>().Length];
        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new(GraphicsDevice);
        
        SoundEffectID[] soundEffectIDs = Enum.GetValues<SoundEffectID>();
        for (var i = 0; i < soundEffectIDs.Length; i++) {
            _soundEffects[i] = Content.Load<SoundEffect>("sfx/" + soundEffectIDs[i].ToString().ToLower());
        }

        Font = Content.Load<SpriteFont>("font");

        var hotbarTexture = Content.Load<Texture2D>("img/hotbar");
        var fullTexture = Content.Load<Texture2D>("img/healthbar-full"); 
        var emptyTexture = Content.Load<Texture2D>("img/healthbar-empty");
        var bonusTexture = Content.Load<Texture2D>("img/healthbar-golden");

        var tileSet = new TileSet(Content.Load<Texture2D>("img/blocks"), 3, 3);
        _tiledWorld = new TiledWorld(GraphicsDevice, tileSet, 61, 27);
        _tiledWorld.LoadMap(Content, WorldType.Default);

        Arrow.ArrowTexture = Content.Load<Texture2D>("img/arrow");

        var playerTexture = Content.Load<Texture2D>("img/player");
        
        var hotbarDrawPos = new Vector2(WindowWidth / 2 - hotbarTexture.Width / 2, 0);
        var hotbar1 = new Hotbar(hotbarTexture, hotbarDrawPos, Team.Blue);
        var healthBar1 = new HealthBar(fullTexture, emptyTexture, bonusTexture, new(10, 10));
        Components.Add(new InputListenerComponent(this, hotbar1.CreateInputListeners()));
        Player1 = new Player(playerTexture, hotbar1, healthBar1, new(0, 0, playerTexture.Width, playerTexture.Height), Team.Blue);
        AddEntity(Player1);

        
        var hotbar2 = new Hotbar(hotbarTexture, new(WindowWidth / 2 - hotbarTexture.Width / 2, WindowHeight - hotbarTexture.Height / 2), Team.Red);
        var healthBar2 = new HealthBar(fullTexture, emptyTexture, bonusTexture, new(WindowWidth - 10 - fullTexture.Width, WindowHeight - fullTexture.Height - 10));
        Components.Add(new InputListenerComponent(this, hotbar2.CreateInputListeners()));
        Player2 = new Player(playerTexture, hotbar2, healthBar2, new(0, 0, playerTexture.Width, playerTexture.Height), Team.Red);
        AddEntity(Player2);

        _scoreBar = new ScoreBar(Content.Load<Texture2D>("img/scorebar-full"), Content.Load<Texture2D>("img/scorebar-empty"), 
            new(hotbarDrawPos.X, hotbarDrawPos.Y + hotbarTexture.Height / 2 + 5));

        _camera.LookAt(new(MapWidth / 2, MapHeight / 2));

        _blockParticleEffects = new ParticleEffect[Enum.GetValues(typeof(TileType)).Length];
        _blockParticleEffects[(int) TileType.Blue] = CreateParticleEffect(new Color(61, 50, 76));
        _blockParticleEffects[(int) TileType.Red] = CreateParticleEffect(new Color(124, 54, 39));
        _blockParticleEffects[(int) TileType.DarkBlue] = CreateParticleEffect(new Color(20, 16, 25));
        _blockParticleEffects[(int) TileType.DarkRed] = CreateParticleEffect(new Color(73, 32, 23));

        NewRound();
    }

    protected override void UnloadContent() {
        foreach (var particleEffect in _blockParticleEffects) {
            particleEffect?.Dispose();
        }
        base.UnloadContent();
    }

    private ParticleEffect CreateParticleEffect(Color color) {
        var particleTexture = new Texture2D(GraphicsDevice, 1, 1);
        particleTexture.SetData(new[] { color });
        var textureRegion = new TextureRegion2D(particleTexture);
        return new ParticleEffect(autoTrigger: false) {
            Emitters = new List<ParticleEmitter> {
                new(textureRegion, 10, TimeSpan.FromSeconds(0.5f), Profile.BoxFill(40, 40)) {
                    AutoTrigger = false,
                    Parameters = new ParticleReleaseParameters {
                        Speed = new Range<float>(200f),
                        Opacity = new Range<float>(1f),
                        Quantity = 3,
                        Rotation = new Range<float>(0f, 1f),
                    },
                    Modifiers = {
                        new LinearGravityModifier {Direction = Vector2.UnitY, Strength = 1500f},
                        new AgeModifier() {
                            Interpolators = new List<Interpolator>() {
                                new ScaleInterpolator { StartValue = new Vector2(10f, 10f), EndValue = new Vector2(0f, 0f) }
                            }
                        }
                    }
                }
            }
        };
    }

    protected override void Update(GameTime gameTime) {
        UpdateCamera();
        base.Update(gameTime);
        UI.UpdateUI(gameTime);
        _tiledWorld.Update(gameTime);
        _entityWorld.Update(gameTime);
        foreach (var particleEffect in _blockParticleEffects) {
            particleEffect?.Update((float) gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    private void UpdateCamera() {

        float oldZoom = _camera.Zoom;
        float oldY = _camera.Center.Y;

        RectangleF p1 = Player1.Bounds;
        RectangleF p2 = Player2.Bounds;
        
        Player lowerPlayer = p1.Bottom > p2.Bottom ? Player1 : Player2;
        Player upperPlayer = p1.Top < p2.Top ? Player1 : Player2;
        
        Vector2 lowerBottom = lowerPlayer.Bounds.BottomLeft;
        Vector2 upperTop = upperPlayer.Bounds.TopLeft;


        // set initial zoom to fit player 1 and 2 horizontally
        float sideBounds = 100f / _camera.Zoom;
        float zoomX = WindowWidth / (MathF.Abs(p1.X - p2.Right) + sideBounds * 2f);
        zoomX = Constrict(zoomX, _camera.MinimumZoom, _camera.MaximumZoom);
        _camera.Zoom = zoomX;
        
        // try looking at the default position
        var defaultY = 520f;
        _camera.LookAt(new((p1.X + p2.Right) / 2, defaultY));
        
        // if default position is too high, try looking at lower default position
        float targetY = defaultY;
        if (WorldToScreen(lowerBottom).Y > WindowHeight) {
            targetY = 720f;
        }
        _camera.LookAt(new(_camera.Center.X, targetY));

        // if neither captures both players, try looking directly between both players
        float highestPlayerY = 280f;
        if (WorldToScreen(upperTop).Y < 0) {
            if (upperTop.Y < highestPlayerY) {
                targetY = oldY;
            }
            else {
                targetY = (p1.Center.Y + p2.Center.Y) / 2;
            }
        }
        _camera.LookAt(new(_camera.Center.X, targetY));

        // if still can't see both players, zoom out to fit both
        float targetZoom = zoomX;
        if (WorldToScreen(lowerBottom).Y > WindowHeight || WorldToScreen(upperTop).Y < 0) {
            targetZoom = 1.55f;
        }

        var cameraSpeed = 5f;
        var zoomSpeed = 0.02f;

        _camera.LookAt(new((p1.X + p2.Right) / 2, MoveAtSpeed(oldY, cameraSpeed, targetY)));
        _camera.Zoom = MoveAtSpeed(oldZoom, zoomSpeed, targetZoom);
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.SkyBlue);
        _tiledWorld.Draw(gameTime);
        _entityWorld.Draw(gameTime);
        _spriteBatch.Begin();
        foreach (var particleEffect in _blockParticleEffects) {
            if (particleEffect != null) {
                _spriteBatch.Draw(particleEffect);
            }
        }
        UI.DrawUI(_spriteBatch);
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}