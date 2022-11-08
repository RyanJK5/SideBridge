using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Modifiers.Containers;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using MonoGame.Extended.Particles.Profiles;
using MonoGame.Extended.TextureAtlases;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.ViewportAdapters;
using System.Collections.Generic;

namespace SideBridge;

public class Game : Microsoft.Xna.Framework.Game {

    private static Game main;

    private static void Main() {
        main = new();
        using var game = main;
        main.Run();
    }

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private OrthographicCamera _camera;
    private Vector2 _cameraVelocity;

    private CollisionComponent _collisionComponent;
    private TiledWorld _tiledWorld;
    private EntityWorld _entityWorld;

    public static Player Player { get; private set; }
    private Hotbar _hotbar;

    private List<SoundEffect> _soundEffects;
    
    private ParticleEffect[] _blockParticleEffects;

    public static SoundEffect GetSoundEffect(SoundEffectID id) =>
        main._soundEffects[(int) id];

    public static Hotbar Hotbar { get => main._hotbar; }

    public static int WindowWidth { get => main._graphics.PreferredBackBufferWidth; }
    public static int WindowHeight { get => main._graphics.PreferredBackBufferHeight; }

    public static float MapWidth { get => main._tiledWorld.WidthInPixels; }
    public static float MapHeight { get => main._tiledWorld.HeightInPixels; }

    public static Tile GetTile(float x, float y) {
        TiledWorld tiledWorld = main._tiledWorld;
        int tileSize = tiledWorld.TileSize;
        if (x > MapWidth || x < 0 || y > MapHeight || y < 0) {
            return new Tile(BlockType.Air, new((int) (x / tileSize) * tileSize, (int) (y / tileSize) * tileSize, tileSize, tileSize));
        }
        return tiledWorld[(int) (x / tileSize), (int) (y / tileSize)];
    }

    public static void SetTile(BlockType type, float x, float y) {
        TiledWorld tiledWorld = main._tiledWorld;
        int tileSize = tiledWorld.TileSize;
        if (type != BlockType.Air) {
            main._soundEffects[0].Play();
        }
        else {
            main._blockParticleEffects[(int) GetTile(x, y).Type].Trigger(WorldToScreen(new Vector2(x + tileSize / 2, y + tileSize / 2)));
        }
        tiledWorld.SetTile(type, (int) (x / tileSize), (int) (y / tileSize));
    }

    public static void AddCollider(ICollisionActor entity) =>
        main._collisionComponent.Insert(entity);    

    public static void RemoveCollider(ICollisionActor entity) =>
        main._collisionComponent.Remove(entity);
    
    public static void AddEntity(Entity entity) {
        main._entityWorld.Add(entity);
        main._collisionComponent.Insert(entity);
    }

    public static void RemoveEntity(Entity entity) {
        main._entityWorld.Remove(entity);
        main._collisionComponent.Remove(entity);
    }

    public static Vector2 ScreenToWorld(Vector2 position) => main._camera.ScreenToWorld(position.X, position.Y);
    public static Vector2 WorldToScreen(Vector2 position) => main._camera.WorldToScreen(position.X, position.Y);

    public static Matrix GetViewMatrix() => main._camera.GetViewMatrix();

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
        _soundEffects = new();
        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new(GraphicsDevice);

        _soundEffects.Add(Content.Load<SoundEffect>("placeblock"));

        _hotbar = new(Content.Load<Texture2D>("hotbar"), GraphicsDevice);
        Components.Add(new InputListenerComponent(this, _hotbar.CreateInputListeners()));

        var tileSet = new TileSet(Content.Load<Texture2D>("blocks"), 3, 2);
        _tiledWorld = new TiledWorld(GraphicsDevice, tileSet, 61, 27);
        _tiledWorld.LoadMap(Content, WorldType.Default);

        _collisionComponent = new(new(0, 0, MapWidth, MapHeight));
        _tiledWorld.InsertTiles(_collisionComponent);
        
        Arrow.ArrowTexture = Content.Load<Texture2D>("arrow");

        var playerTexture = Content.Load<Texture2D>("player");
        Player = new Player(playerTexture, new(WindowWidth / 2 - playerTexture.Width / 2, 100, playerTexture.Width, playerTexture.Height));
        _entityWorld.Add(Player);
        _collisionComponent.Insert(Player);

        _camera.LookAt(new(MapWidth / 2, MapHeight / 2));

        _blockParticleEffects = new ParticleEffect[BlockType.GetValues(typeof(BlockType)).Length];
        _blockParticleEffects[(int) BlockType.Blue] = createParticleEffect(new Color(61, 50, 76));
        _blockParticleEffects[(int) BlockType.Red] = createParticleEffect(new Color(124, 54, 39));
        _blockParticleEffects[(int) BlockType.White] = createParticleEffect(new Color(136, 115, 105));
        _blockParticleEffects[(int) BlockType.DarkBlue] = createParticleEffect(new Color(20, 16, 25));
        _blockParticleEffects[(int) BlockType.DarkRed] = createParticleEffect(new Color(73, 32, 23));


    }

    protected override void UnloadContent() {
        foreach (var particleEffect in _blockParticleEffects) {
            particleEffect.Dispose();
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
        _collisionComponent.Update(gameTime);
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

        if (camRight - Player.Bounds.X <= sideBounds) {
            _cameraVelocity.X += Acceleration;
        }
        else if (Player.Bounds.X - camLeft <= sideBounds) {
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
        _spriteBatch.End();
        _hotbar.Draw(gameTime);
        base.Draw(gameTime);
    }
}