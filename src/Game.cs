using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

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
    private List<GameObject> objects;
    private BlockType[,] world = new BlockType[48, 27];

    public int WindowWidth { get => _graphics.PreferredBackBufferWidth; }

    public int WindowHeight { get => _graphics.PreferredBackBufferHeight; }

    public GameTime GameTime { get; private set; }

    private Player player;

    private Game() {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        _graphics.ToggleFullScreen();
        Content.RootDirectory = "Content";
        objects = new();
        IsMouseVisible = true;
    }

    protected override void Initialize() {
        var playerTexture = Content.Load<Texture2D>("player");
        player = new(playerTexture, new Vector2(WindowWidth / 2 - playerTexture.Width / 2, 50), 0);
        AddObject(player);
        for (var i = 0; i < world.GetLength(0); i++) {
            for (var j = world.GetLength(1) / 2; j < world.GetLength(1); j++) {
                world[i,j] = i < (world.GetLength(0) / 2) ? BlockType.Red : BlockType.Blue;
            }
        }
        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime) {
        GameTime = gameTime;
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape)) {
            Exit();
        }
        foreach (GameObject obj in objects) {
            obj.Update(gameTime);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.SkyBlue);

        _spriteBatch.Begin();
        int width = Blocks.GetTextureFrom(BlockType.Red).Width;
        for (var i = 0; i < world.GetLength(0); i++) {
            for (var j = 0; j < world.GetLength(1); j++) {
                Blocks.Draw(_spriteBatch, world[i,j], 
                    new Vector2(width * i, width * j));
            }
        }
        foreach (GameObject obj in objects) {
            obj.Draw(_spriteBatch);
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    public BlockType GetTile(float x, float y) {
        int width = Blocks.GetTextureFrom(BlockType.Red).Width;
        return world[(int) (x / width), (int) (y / width)];
    }

    public void AddObject(GameObject obj) => objects.Add(obj);
    
    public void RemoveObject(GameObject obj) => objects.Remove(obj);
}