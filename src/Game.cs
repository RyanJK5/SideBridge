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
    public TiledMap TiledMap;

    private RenderSystem _renderSystem;

    public int WindowWidth { get => _graphics.PreferredBackBufferWidth; }
    public int WindowHeight { get => _graphics.PreferredBackBufferHeight; }

    public float MapWidth { get => TiledMap.WidthInPixels; }
    public float MapHeight { get => TiledMap.HeightInPixels; }

    public TiledMapTile GetTile(float x, float y) =>
        TiledMap.TileLayers[0].GetTile
        ((ushort) (x / TiledMap.TileWidth),  (ushort) (y / TiledMap.TileHeight));
    
    public void SetTile(float x, float y, BlockType blockType) {
        var tileX = (ushort) (x / TiledMap.TileWidth);
        var tileY = (ushort) (y / TiledMap.TileHeight);
        TiledMap.TileLayers[0].SetTile(tileX, tileY, (uint) blockType);
        CollisionComponent.Insert
            (new StaticCollider(new(tileX * TiledMap.TileWidth, tileY * TiledMap.TileHeight, TiledMap.TileWidth, TiledMap.TileHeight)));
        _renderSystem.MapUpdated();
    }

    public Vector2 ScreenToWorld(Vector2 position) => _renderSystem.ScreenToWorld(position.X, position.Y);

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

        TiledMap = Content.Load<TiledMap>("Map1");
        CollisionComponent = new(new(0, 0, TiledMap.WidthInPixels, TiledMap.HeightInPixels));
        insertTileHitboxes();
        
        _renderSystem = new(Window, GraphicsDevice, TiledMap);
        TiledMap.TileLayers[0].SetTile(0, 0, (uint) (BlockType.Blue));
        _world = new WorldBuilder()
            .AddSystem(new PlayerSystem())
            .AddSystem(_renderSystem)
            .AddSystem(new BlockSystem())
            .Build();

        var player = _world.CreateEntity();
        var playerTexture = Content.Load<Texture2D>("player");
        var playerPosition = new Position { X = MapWidth / 2, Y = 100 };
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

    private void insertTileHitboxes() {
        for (ushort x = 0; x < TiledMap.Width; x++) {
            for (ushort y = 0; y < TiledMap.Height; y++) {
                if (!TiledMap.TileLayers[0].GetTile(x, y).IsBlank) {
                    Game.Main.CollisionComponent.Insert(
                        new StaticCollider(new(x * TiledMap.TileWidth, y * TiledMap.TileHeight, 
                        TiledMap.TileWidth, TiledMap.TileHeight)));
                }
            }
        }
    }
}