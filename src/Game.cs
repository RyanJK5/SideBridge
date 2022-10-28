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

    public int WindowWidth { get => _graphics.PreferredBackBufferWidth; }

    public int WindowHeight { get => _graphics.PreferredBackBufferHeight; }

    public const float MaxVelocity = 10f;
    public const float Acceleration = 1f;

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
        player = new(Content.Load<Texture2D>("player"), new Vector2(WindowWidth / 2, WindowHeight / 2), 0);
        AddObject(player);
        
        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime) {
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
        foreach (GameObject obj in objects) {
            obj.Draw(_spriteBatch);
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    public void AddObject(GameObject obj) => objects.Add(obj);
    
    public void RemoveObject(GameObject obj) => objects.Remove(obj);
}