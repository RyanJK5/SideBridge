using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.ViewportAdapters;
using MonoGame.Extended.Entities;
using SideBridge.Systems;
using SideBridge.Components;

namespace SideBridge;

public class Game : Microsoft.Xna.Framework.Game {

    private static Game mainGame;

    public static Game Main { 
        get {
            if (mainGame == null) {
                mainGame = new Game();
            }
            return mainGame;
        }

        private set {
            mainGame = value;
        }
    }

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private World _world;
    public CollisionComponent CollisionComponent;

    private RenderSystem _renderSystem;

    public int WindowWidth { get => _graphics.PreferredBackBufferWidth; }
    public int WindowHeight { get => _graphics.PreferredBackBufferHeight; }

    public float MapWidth { get => _renderSystem.TiledMap.WidthInPixels; }
    public float MapHeight { get => _renderSystem.TiledMap.HeightInPixels; }

    public GameTime GameTime { get; private set; }

    private Game() {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.ToggleFullScreen();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }


    protected override void Initialize() {
        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        _graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new(GraphicsDevice);

        var tiledMap = Content.Load<TiledMap>("TestMap");
        CollisionComponent = new(new(0, 0, tiledMap.WidthInPixels, tiledMap.HeightInPixels));
        _renderSystem = new(Window, GraphicsDevice, tiledMap);
        _world = new WorldBuilder()
            .AddSystem(new PlayerSystem())
            .AddSystem(_renderSystem)
            .Build();

        var player = _world.CreateEntity();
        var playerTexture = Content.Load<Texture2D>("player");
        var playerPosition = new Position { X = MapWidth / 2 - playerTexture.Width / 2, Y = 100 };
        var playerVelocity = new Velocity();
        var playerCollider = new PlayerCollider(new(playerPosition.X, playerPosition.Y, playerTexture.Width, playerTexture.Height), playerVelocity);
        player.Attach(new Sprite { Texture = playerTexture });
        player.Attach(playerPosition);
        player.Attach(playerVelocity);
        player.Attach(new Input(Keys.A, Keys.D, Keys.LeftShift, Keys.Space));
        player.Attach(playerCollider);
        CollisionComponent.Insert(playerCollider);
        
    }

    protected override void Update(GameTime gameTime) {
        GameTime = gameTime;
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape)) {
            Exit();
        }

        _world.Update(gameTime);
        CollisionComponent.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.SkyBlue);
        _world.Draw(gameTime);
        base.Draw(gameTime);
    }
}