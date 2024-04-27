using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Input.InputListeners;

namespace SideBridge;

public class GameGraphics : Microsoft.Xna.Framework.Game {

    private SpriteBatch _spriteBatch;
    private GraphicsDeviceManager _graphics;

    public static SpriteFont Font { get; private set; }

    public int WindowWidth { get => _graphics.PreferredBackBufferWidth; }
    public int WindowHeight { get => _graphics.PreferredBackBufferHeight; }

    public void AddListeners(params InputListener[] listeners) => 
        Components.Add(new InputListenerComponent(this, listeners))
    ;

    public bool IsFullScreen => _graphics.IsFullScreen;

    public void SetFullScreen(bool fullScreen) {
        if (fullScreen != IsFullScreen) {
            _graphics.ToggleFullScreen();   
        }
    }

    public GameGraphics() {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.ToggleFullScreen();
        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize() {
        _graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new(GraphicsDevice);
        
        Font = Content.Load<SpriteFont>("font");
        Arrow.ArrowTexture = Content.Load<Texture2D>("img/arrow");
    }

    protected override void Update(GameTime gameTime) {
        base.Update(gameTime);
        foreach (IUpdatable element in Game.Updatables()) {
            element.Update(gameTime);
        }
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.SkyBlue);
        _spriteBatch.Begin();
        foreach (IDrawable element in Game.Drawables()) {
            element.Draw(_spriteBatch);
        }
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}