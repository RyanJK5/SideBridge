using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ViewportAdapters;

namespace SideBridge;

internal class Game {

    private static Game s_main;
    private Game() { }

    private TiledWorld _tiledWorld;
    private EntityWorld _entityWorld;
    private GameGraphics _gameGraphics;
    private GameCamera _gameCamera;
    private SoundEffectHandler _soundEffectHandler;
    private ScoringHandler _scoringHandler;
    private ParticleEffectHandler _particleEffectHandler;
    private UIHandler _uiHandler;

    public static TiledWorld TiledWorld { get => s_main._tiledWorld; }
    public static EntityWorld EntityWorld { get => s_main._entityWorld; }
    public static GameGraphics GameGraphics { get => s_main._gameGraphics; }
    public static GameCamera GameCamera { get => s_main._gameCamera; }
    public static SoundEffectHandler SoundEffectHandler { get => s_main._soundEffectHandler; }
    public static ScoringHandler ScoringHandler { get => s_main._scoringHandler; }
    public static ParticleEffectHandler ParticleEffectHandler { get => s_main._particleEffectHandler; }
    public static UIHandler UIHandler { get => s_main._uiHandler; }

    public const float Gravity = 1f;

    public static IDrawable[] Drawables() => 
        new IDrawable[] { TiledWorld, EntityWorld, ParticleEffectHandler, UIHandler }
    ;

    public static IUpdatable[] Updatables() =>
        new IUpdatable[] { TiledWorld, EntityWorld, GameCamera, ParticleEffectHandler, UIHandler }
    ;

    public static void Start() {
        s_main = new Game();
        s_main.InitializeFields();
        GameGraphics.Run();
    }

    private void InitializeFields() {
        _gameGraphics = new GameGraphics();
        ContentManager loader = _gameGraphics.Content;
        GraphicsDevice graphicsDevice = _gameGraphics.GraphicsDevice;

        _entityWorld = new EntityWorld(graphicsDevice);

        _soundEffectHandler = new SoundEffectHandler(loader);
        
        _particleEffectHandler = new ParticleEffectHandler(graphicsDevice);

        var tileSet = new TileSet(loader.Load<Texture2D>("img/blocks"), 3, 3);
        _tiledWorld = new TiledWorld(_gameGraphics.GraphicsDevice, tileSet, 61, 27);
        _tiledWorld.LoadMap(loader, WorldType.Lobby);
        Settings.LobbyMode = true;

        Player[] players = CreatePlayers(loader);
        CreateUI(loader, players);

        var viewportAdapter = new BoxingViewportAdapter(_gameGraphics.Window, graphicsDevice, 1920, 1080);
        var camera = new OrthographicCamera(viewportAdapter) {
            MinimumZoom = GameCamera.MinimumZoom,
            MaximumZoom = GameCamera.MaximumZoom
        };
        _gameCamera = new GameCamera(camera, players[0], players[1]);
    }

    private Player[] CreatePlayers(ContentManager loader) {
        var players = new Player[2];
        var playerTexture = loader.Load<Texture2D>("img/player");
        
        for (var i = 0; i < players.Length; i++) {
            players[i] = new Player(
                playerTexture,
                playerTexture.Bounds,
                (Team) i
            );
            _entityWorld.Add(players[i]);
        }

        return players;
    }

    private void CreateUI(ContentManager loader, Player[] players) {
        var fullHealthBarTexture = loader.Load<Texture2D>("img/healthbar-full"); 
        var emptyHealthBarTexture = loader.Load<Texture2D>("img/healthbar-empty");
        var goldenHealthBarTexture = loader.Load<Texture2D>("img/healthbar-golden");
        var hotbarTexture = loader.Load<Texture2D>("img/hotbar");
        
        var ui = new List<UI>();

        float hotbarX = _gameGraphics.WindowWidth / 2 - hotbarTexture.Width / 2;
        var scoreBar = new ScoreBar(
            loader.Load<Texture2D>("img/scorebar-full"), 
            loader.Load<Texture2D>("img/scorebar-empty"), 
            new(hotbarX, hotbarTexture.Height / 2 + 5)
        );
        ui.Add(scoreBar);
        _scoringHandler = new ScoringHandler(scoreBar, players[0], players[1]);

        for (var i = 0; i < players.Length; i++) {
            ui.Add(new HealthBar(
                players[i], 
                fullHealthBarTexture, 
                emptyHealthBarTexture, 
                goldenHealthBarTexture, 
                (Team) i == Team.Blue 
                    ? new(10, 10)
                    : new(_gameGraphics.WindowWidth - 10 - fullHealthBarTexture.Width, 
                        _gameGraphics.WindowHeight - fullHealthBarTexture.Height - 10)
            ));
            ui.Add(new Hotbar(
                players[i], 
                hotbarTexture, 
                (Team) i == Team.Blue
                    ? new(hotbarX, 0)
                    : new(_gameGraphics.WindowWidth / 2 - hotbarTexture.Width / 2, 
                        _gameGraphics.WindowHeight - hotbarTexture.Height / 2)
            ));
        }
        _uiHandler = new UIHandler(ui.ToArray());
    }
}