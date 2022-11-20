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

namespace SideBridge;

public class Game : Microsoft.Xna.Framework.Game {

    private static Game s_main;

    private static void Main() {
        s_main = new();
        using var game = s_main;
        s_main.Run();
    }

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private OrthographicCamera _camera;
    private Vector2 _cameraVelocity;
    private TiledWorld _tiledWorld;
    private EntityWorld _entityWorld;

    public static Player Player1 { get; private set; }
    public static Player Player2 { get; private set; }

    private SoundEffect[] _soundEffects;
    private ParticleEffect[] _blockParticleEffects;

    public static SoundEffect GetSoundEffect(SoundEffectID id) =>
        s_main._soundEffects[(int) id];

    public static int WindowWidth { get => s_main._graphics.PreferredBackBufferWidth; }
    public static int WindowHeight { get => s_main._graphics.PreferredBackBufferHeight; }

    public static float MapWidth { get => s_main._tiledWorld.WidthInPixels; }
    public static float MapHeight { get => s_main._tiledWorld.HeightInPixels; }

    public static Tile GetTile(float x, float y) {
        TiledWorld tiledWorld = s_main._tiledWorld;
        int tileSize = tiledWorld.TileSize;
        if (x > MapWidth || x < 0 || y > MapHeight || y < 0) {
            return new Tile(BlockID.Air, new((int) (x / tileSize) * tileSize, (int) (y / tileSize) * tileSize, tileSize, tileSize));
        }
        return tiledWorld[(int) (x / tileSize), (int) (y / tileSize)];
    }

    public static void CheckTileCollisions(Entity entity) {
        s_main._tiledWorld.CheckTileCollisions(entity);
    }

    public static void SetTile(BlockID type, float x, float y) {
        TiledWorld tiledWorld = s_main._tiledWorld;
        int tileSize = tiledWorld.TileSize;
        if (type != BlockID.Air) {
            s_main._soundEffects[0].Play();
        }
        else {
            s_main._blockParticleEffects[(int) GetTile(x, y).Type].Trigger(WorldToScreen(new Vector2(x + tileSize / 2, y + tileSize / 2)));
        }
        tiledWorld.SetTile(type, (int) (x / tileSize), (int) (y / tileSize));
    }

    public static void AddEntity(Entity entity) => s_main._entityWorld.Add(entity);
    public static void RemoveEntity(Entity entity) => s_main._entityWorld.Remove(entity);
    public static bool ContainsEntity(Entity entity) => s_main._entityWorld.Contains(entity);

    #nullable enable
    public static T? FindEntity<T>(System.Predicate<T> testCase) => s_main._entityWorld.FindEntity(testCase);

    public static Tile[] FindTiles(System.Predicate<Tile> testCase) => s_main._tiledWorld.FindTiles(testCase);
    #nullable disable

    public static Vector2 ScreenToWorld(Vector2 position) => s_main._camera.ScreenToWorld(position.X, position.Y);
    public static Vector2 WorldToScreen(Vector2 position) => s_main._camera.WorldToScreen(position.X, position.Y);

    public static void AddListeners(params InputListener[] listeners) {
        s_main.Components.Add(new InputListenerComponent(s_main, listeners));
    }

    public static Matrix GetViewMatrix() => s_main._camera.GetViewMatrix();

    private Game() {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.ToggleFullScreen();

        var viewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, 1920, 1080);
        _camera = new(viewportAdapter);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }


    protected override void Initialize() {
        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        _graphics.ApplyChanges();
        _entityWorld = new(GraphicsDevice);
        _soundEffects = new SoundEffect[System.Enum.GetValues(typeof(SoundEffectID)).Length];
        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new(GraphicsDevice);

        
        _soundEffects[(int) SoundEffectID.PlaceBlock] = Content.Load<SoundEffect>("placeblock");
        _soundEffects[(int) SoundEffectID.BreakBlock] = Content.Load<SoundEffect>("breakblock");
        

        var hotbarTexture = Content.Load<Texture2D>("hotbar");
        var fullTexture = Content.Load<Texture2D>("healthbar"); 
        var emptyTexture = Content.Load<Texture2D>("empty-healthbar");
        var bonusTexture = Content.Load<Texture2D>("golden-healthbar");

        var tileSet = new TileSet(Content.Load<Texture2D>("blocks"), 3, 2);
        _tiledWorld = new TiledWorld(GraphicsDevice, tileSet, 61, 27);
        _tiledWorld.LoadMap(Content, WorldType.Default);

        Arrow.ArrowTexture = Content.Load<Texture2D>("arrow");

        var playerTexture = Content.Load<Texture2D>("player");
        
        var hotbar1 = new Hotbar(hotbarTexture, new(WindowWidth / 2 - hotbarTexture.Width / 2, 0));
        var healthBar1 = new HealthBar(fullTexture, emptyTexture, bonusTexture, new(10, 10));
        Components.Add(new InputListenerComponent(this, hotbar1.CreateInputListeners()));
        Player1 = new Player(playerTexture, hotbar1, healthBar1, new(0, 0, playerTexture.Width, playerTexture.Height), Team.Blue);
        AddEntity(Player1);
        
        var hotbar2 = new Hotbar(hotbarTexture, new(WindowWidth / 2 - hotbarTexture.Width / 2, WindowHeight - hotbarTexture.Height));
        var healthBar2 = new HealthBar(fullTexture, emptyTexture, bonusTexture, new(WindowWidth - 10 - fullTexture.Width, 10));
        Components.Add(new InputListenerComponent(this, hotbar2.CreateInputListeners()));
        var player2 = new Player(playerTexture, hotbar2, healthBar2, new(0, 0, playerTexture.Width, playerTexture.Height), Team.Red);
        AddEntity(player2);

        _camera.LookAt(new(MapWidth / 2, MapHeight / 2));

        _blockParticleEffects = new ParticleEffect[BlockID.GetValues(typeof(BlockID)).Length];
        _blockParticleEffects[(int) BlockID.Blue] = createParticleEffect(new Color(61, 50, 76));
        _blockParticleEffects[(int) BlockID.Red] = createParticleEffect(new Color(124, 54, 39));
        _blockParticleEffects[(int) BlockID.White] = createParticleEffect(new Color(136, 115, 105));
        _blockParticleEffects[(int) BlockID.DarkBlue] = createParticleEffect(new Color(20, 16, 25));
        _blockParticleEffects[(int) BlockID.DarkRed] = createParticleEffect(new Color(73, 32, 23));
    }

    protected override void UnloadContent() {
        foreach (var particleEffect in _blockParticleEffects) {
            particleEffect?.Dispose();
        }
        base.UnloadContent();
    }

    private ParticleEffect createParticleEffect(Color color) {
        var particleTexture = new Texture2D(GraphicsDevice, 1, 1);
        particleTexture.SetData(new[] { color });
        var textureRegion = new TextureRegion2D(particleTexture);
        return new ParticleEffect(autoTrigger: false) {
            Emitters = new List<ParticleEmitter> {
                new ParticleEmitter(textureRegion, 10, System.TimeSpan.FromSeconds(0.5f), Profile.BoxFill(40, 40)) {
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
        updateCamera();
        base.Update(gameTime);
        _tiledWorld.Update(gameTime);
        _entityWorld.Update(gameTime);
        foreach (var particleEffect in _blockParticleEffects) {
            particleEffect?.Update((float) gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    private void updateCamera() {
        const float Acceleration = 0.2f;
        const float MaxVelocity = 7.5f;
        float sideBounds = 500 / _camera.Zoom;

        var camRight = _camera.Center.X + WindowWidth / _camera.Zoom / 2;
        var camLeft = _camera.Center.X - WindowWidth / _camera.Zoom / 2;
        
        if (camRight > MapWidth) {
            _camera.Move(new(MapWidth - camRight, 0));
        }
        else if (camLeft < 0) {
            _camera.Move(new(-camLeft, 0));
        }
        _camera.Move(_cameraVelocity);

        if (camRight - Player1.Bounds.X <= sideBounds) {
            _cameraVelocity.X += Acceleration;
        }
        else if (Player1.Bounds.X - camLeft <= sideBounds) {
            _cameraVelocity.X -= Acceleration;
        }
        else if (_cameraVelocity.X > 0) {
            _cameraVelocity.X -= Acceleration;
            if (_cameraVelocity.X < 0) {
                _cameraVelocity.X = 0;
            }
        }
        else if (_cameraVelocity.X < 0) {
            _cameraVelocity.X += Acceleration;
            if (_cameraVelocity.X > 0) {
                _cameraVelocity.X = 0;
            }
        }
        if (_cameraVelocity.X > MaxVelocity) {
            _cameraVelocity.X = MaxVelocity;
        }
        else if (_cameraVelocity.X < -MaxVelocity) {
            _cameraVelocity.X = -MaxVelocity;
        }
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