using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
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
    private TiledMap _tiledMap;
    private TiledMapRenderer _tiledMapRenderer;

    private OrthographicCamera _camera;
    private Vector2 _cameraVelocity;

    public int WindowWidth { get => _graphics.PreferredBackBufferWidth; }

    public int WindowHeight { get => _graphics.PreferredBackBufferHeight; }

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

        var playerTexture = Content.Load<Texture2D>("player");
        base.Initialize();

        var viewportAdapter = new BoxingViewportAdapter(Window, GraphicsDevice, 1920, 1080);
        _camera = new(viewportAdapter);
        _camera.Move(new(0, -7 * 40));
    }

    protected override void LoadContent() {
        _spriteBatch = new (GraphicsDevice);
        _tiledMap = Content.Load<TiledMap>("Map1");
        _tiledMapRenderer = new(GraphicsDevice, _tiledMap);

        _world = new WorldBuilder()
            .AddSystem(new PlayerProcessingSystem())
            .AddSystem(new RenderSystem(GraphicsDevice))
            .Build();
        var player = _world.CreateEntity();
        var playerSprite = Content.Load<Texture2D>("player");
        player.Attach(new Player(playerSprite, playerSprite.Bounds));
    }

    protected override void Update(GameTime gameTime) {
        GameTime = gameTime;
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape)) {
            Exit();
        }
        _tiledMapRenderer.Update(gameTime);
        updateCamera();
        _camera.Move(_cameraVelocity);

        _world.Update(gameTime);
        base.Update(gameTime);
    }

    private bool _movingRight;
    private void updateCamera() {
        if (_movingRight && _camera.Position.X + WindowWidth >= _tiledMap.WidthInPixels) {
            _movingRight = !_movingRight;
            _cameraVelocity.X = -5;
        }
        else if (!_movingRight && _camera.Position.X <= 0) {
            _movingRight = !_movingRight;
            _cameraVelocity.X = 5;
        }
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.SkyBlue);
        _tiledMapRenderer.Draw(_camera.GetViewMatrix());
        _world.Draw(gameTime);
        base.Draw(gameTime);
    }
}