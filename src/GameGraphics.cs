using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using MonoGame.Extended.Particles.Profiles;
using MonoGame.Extended.TextureAtlases;
using MonoGame.Extended.Input.InputListeners;
using System.Collections.Generic;

namespace SideBridge;

public class GameGraphics : Microsoft.Xna.Framework.Game {

    private SpriteBatch _spriteBatch;
    private GraphicsDeviceManager _graphics;

    public static SpriteFont Font { get; private set; }

    public int WindowWidth { get => _graphics.PreferredBackBufferWidth; }
    public int WindowHeight { get => _graphics.PreferredBackBufferHeight; }

    public  void AddListeners(params InputListener[] listeners) {
        Components.Add(new InputListenerComponent(this, listeners));
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
        Game.GameCamera.Update();
        base.Update(gameTime);
        UI.UpdateUI(gameTime);
        Game.TiledWorld.Update(gameTime);
        Game.EntityWorld.Update(gameTime);
        Game.ParticleEffectHandler.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.SkyBlue);
        Game.TiledWorld.Draw(gameTime);
        Game.EntityWorld.Draw(gameTime);
        _spriteBatch.Begin();
        UI.DrawUI(_spriteBatch);
        Game.ParticleEffectHandler.Draw(_spriteBatch);
        _spriteBatch.End();
        base.Draw(gameTime);
    }
}