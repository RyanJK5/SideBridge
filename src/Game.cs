using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
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

    private CollisionComponent _collisionComponent;
    private TiledWorld _tiledWorld;
    private EntityWorld _entityWorld;

    public static int WindowWidth { get => main._graphics.PreferredBackBufferWidth; }
    public static int WindowHeight { get => main._graphics.PreferredBackBufferHeight; }

    public static float MapWidth { get => main._tiledWorld.WidthInPixels; }
    public static float MapHeight { get => main._tiledWorld.HeightInPixels; }

    public static BlockType GetTile(float x, float y) {
        if (x > MapWidth || x < 0 || y > MapHeight || y < 0) {
            return BlockType.Air;
        }
        TiledWorld tiledWorld = main._tiledWorld;
        int tileSize = tiledWorld.TileSize;
        return tiledWorld[(int) (x / tileSize), (int) (y / tileSize)];
    }

    public static void SetTile(BlockType type, float x, float y) {
        TiledWorld tiledWorld = main._tiledWorld;
        int tileSize = tiledWorld.TileSize;
        tiledWorld[(int) (x / tileSize), (int) (y / tileSize)] = type;
    }

    public static void AddTile(Tile tile) =>
        main._collisionComponent.Insert(tile);    

    public static Vector2 ScreenToWorld(Vector2 position) => main._camera.ScreenToWorld(position.X, position.Y);
    public static Vector2 WorldToScreen(Vector2 position) => main._camera.WorldToScreen(position.X, position.Y);

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


        var tileSet = new TileSet(Content.Load<Texture2D>("blocks"), 3, 2);
        _tiledWorld = new TiledWorld(GraphicsDevice, tileSet, 61, 27);
        _tiledWorld.LoadMap(Content, WorldType.Default);

        _collisionComponent = new(new(0, 0, MapWidth, MapHeight));
        _tiledWorld.InsertTiles(_collisionComponent);
        
        var playerTexture = Content.Load<Texture2D>("player");
        var player = new Player(playerTexture, new(WindowWidth / 2 - playerTexture.Width / 2, 100, playerTexture.Width, playerTexture.Height));
        _entityWorld.Add(player);
        _collisionComponent.Insert(player);
    }

    protected override void Update(GameTime gameTime) {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape)) {
            Exit();
        }
        base.Update(gameTime);
        _tiledWorld.Update(gameTime);
        _entityWorld.Update(gameTime);
        _collisionComponent.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.SkyBlue);
        _tiledWorld.Draw(gameTime);
        _entityWorld.Draw(gameTime);
        base.Draw(gameTime);
    }
}