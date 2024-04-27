using System;
using System.Net.Mime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Input.InputListeners;
using MonoGame.Extended.ViewportAdapters;

namespace SideBridge;

internal class Game {

    private static Game s_current;

    private TiledWorld _tiledWorld;
    private EntityWorld _entityWorld;
    private GameGraphics _gameGraphics;
    private GameCamera _gameCamera;
    private SoundEffectHandler _soundEffectHandler;
    private ScoringHandler _scoringHandler;
    private ParticleEffectHandler _particleEffectHandler;

    public static TiledWorld TiledWorld { get => s_current._tiledWorld; }
    public static EntityWorld EntityWorld { get => s_current._entityWorld; }
    public static GameGraphics GameGraphics { get => s_current._gameGraphics; }
    public static GameCamera GameCamera { get => s_current._gameCamera; }
    public static SoundEffectHandler SoundEffectHandler { get => s_current._soundEffectHandler; }
    public static ScoringHandler ScoringHandler { get => s_current._scoringHandler; }
    public static ParticleEffectHandler ParticleEffectHandler { get => s_current._particleEffectHandler; }

    public static void Start() {
        s_current = new Game();
        s_current.InitializeFields();
        GameGraphics.Run();
        ScoringHandler.NewRound();
    }

    private Game() { }

    private void InitializeFields() {
        _gameGraphics = new GameGraphics();
        ContentManager loader = _gameGraphics.Content;
        GraphicsDevice graphicsDevice = _gameGraphics.GraphicsDevice;

        _entityWorld = new EntityWorld(graphicsDevice);

        _soundEffectHandler = new SoundEffectHandler(loader);
        _particleEffectHandler = new ParticleEffectHandler(graphicsDevice);

        var tileSet = new TileSet(loader.Load<Texture2D>("img/blocks"), 3, 3);
        _tiledWorld = new TiledWorld(_gameGraphics.GraphicsDevice, tileSet, 61, 27);
        _tiledWorld.LoadMap(loader, WorldType.Default);

        var scoringInfo = CreatePlayersAndUI(loader);
        _scoringHandler = new ScoringHandler(scoringInfo.ScoreBar, scoringInfo.Player1, scoringInfo.Player2);

        var viewportAdapter = new BoxingViewportAdapter(_gameGraphics.Window, graphicsDevice, 1920, 1080);
        var camera = new OrthographicCamera(viewportAdapter) {
            MinimumZoom = 0.75f,
            MaximumZoom = 2f
        };
        _gameCamera = new GameCamera(camera, scoringInfo.Player1, scoringInfo.Player2);
    }

    private (ScoreBar ScoreBar, Player Player1, Player Player2) CreatePlayersAndUI(ContentManager loader) {
        var hotbarTexture = loader.Load<Texture2D>("img/hotbar");
        var fullTexture = loader.Load<Texture2D>("img/healthbar-full"); 
        var emptyTexture = loader.Load<Texture2D>("img/healthbar-empty");
        var bonusTexture = loader.Load<Texture2D>("img/healthbar-golden");
        var playerTexture = loader.Load<Texture2D>("img/player");
        
        var hotbarDrawPos = new Vector2(_gameGraphics.WindowWidth / 2 - hotbarTexture.Width / 2, 0);
        var healthBar1 = new HealthBar(fullTexture, emptyTexture, bonusTexture, new(10, 10));
        var hotbar1 = new Hotbar(hotbarTexture, hotbarDrawPos);
        var player1 = new Player(playerTexture, hotbar1, healthBar1, new(0, 0, playerTexture.Width, playerTexture.Height), Team.Blue);
        
        hotbar1.SetPlayer(player1);
        _gameGraphics.Components.Add(new InputListenerComponent(_gameGraphics, hotbar1.CreateInputListeners()));
        _entityWorld.Add(player1);

        var hotbar2 = new Hotbar(hotbarTexture, new(_gameGraphics.WindowWidth / 2 - hotbarTexture.Width / 2, _gameGraphics.WindowHeight - hotbarTexture.Height / 2));
        var healthBar2 = new HealthBar(fullTexture, emptyTexture, bonusTexture, new(_gameGraphics.WindowWidth - 10 - fullTexture.Width, _gameGraphics.WindowHeight - fullTexture.Height - 10));
        var player2 = new Player(playerTexture, hotbar2, healthBar2, new(0, 0, playerTexture.Width, playerTexture.Height), Team.Red);
        
        hotbar2.SetPlayer(player2);
        _gameGraphics.Components.Add(new InputListenerComponent(_gameGraphics, hotbar2.CreateInputListeners()));
        _entityWorld.Add(player2);

        var scoreBar = new ScoreBar(loader.Load<Texture2D>("img/scorebar-full"), loader.Load<Texture2D>("img/scorebar-empty"), 
            new(hotbarDrawPos.X, hotbarDrawPos.Y + hotbarTexture.Height / 2 + 5));

        return (scoreBar, player1, player2);
    }

    public static float Constrict(float val, float min, float max) => MathF.Max(MathF.Min(val, max), min);

    public static float MoveAtSpeed(float val, float speed, float target) {
        if (val < target) {
            val += speed;
        }
        else if (val > target) {
            val -= speed;
        }
        if (MathF.Abs(val - target) < speed) {
            val = target;
        }
        return val;
    }
}