using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.ViewportAdapters;

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

    private Player _player;
    private Hotbar _hotbar;

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
        tiledWorld.SetTile(type, (int) (x / tileSize), (int) (y / tileSize));
    }

    public static void AddTileCollider(Tile tile) =>
        main._collisionComponent.Insert(tile);    

    public static void RemoveTileCollider(Tile tile) =>
        main._collisionComponent.Remove(tile);

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
        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new(GraphicsDevice);

        _hotbar = new(Content.Load<Texture2D>("hotbar"), GraphicsDevice);
        Components.Add(new InputListenerComponent(this, _hotbar.CreateInputListeners()));

        var tileSet = new TileSet(Content.Load<Texture2D>("blocks"), 3, 2);
        _tiledWorld = new TiledWorld(GraphicsDevice, tileSet, 61, 27);
        _tiledWorld.LoadMap(Content, WorldType.Default);

        _collisionComponent = new(new(0, 0, MapWidth, MapHeight));
        _tiledWorld.InsertTiles(_collisionComponent);
        
        var playerTexture = Content.Load<Texture2D>("player");
        _player = new Player(playerTexture, new(WindowWidth / 2 - playerTexture.Width / 2, 100, playerTexture.Width, playerTexture.Height));
        _entityWorld.Add(_player);
        _collisionComponent.Insert(_player);

        _camera.LookAt(new(MapWidth / 2, MapHeight / 2));
    }

    protected override void Update(GameTime gameTime) {
        updateCamera();
        base.Update(gameTime);
        _tiledWorld.Update(gameTime);
        _entityWorld.Update(gameTime);
        _collisionComponent.Update(gameTime);
    }

    private void updateCamera() {
        const float Acceleration = 0.2f;
        const float MaxVelocity = 7.5f;
        float sideBounds = 500 / _camera.Zoom;

        var camRight = _camera.Center.X + WindowWidth / _camera.Zoom / 2;
        var camLeft = _camera.Center.X - WindowWidth / _camera.Zoom / 2;
        
        if (camRight > MapWidth + sideBounds / 2) {
            _camera.Move(new(MapWidth + sideBounds / 2 - camRight, 0));
        }
        else if (camLeft < -sideBounds / 2) {
            _camera.Move(new(-camLeft - sideBounds / 2, 0));
        }
        _camera.Move(_cameraVelocity);

        if (camRight - _player.Bounds.X <= sideBounds) {
            _cameraVelocity.X += Acceleration;
        }
        else if (_player.Bounds.X - camLeft <= sideBounds) {
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
        _hotbar.Draw(gameTime);
        base.Draw(gameTime);
    }
}