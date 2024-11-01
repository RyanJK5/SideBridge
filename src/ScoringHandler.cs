using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SideBridge;

public class ScoringHandler : IUpdatable {

    public const float PauseLength = 5f;
    private const float TimerInactive = -1f;

    private readonly ScoreBar _scoreBar;
    private readonly Player _player1;
    private readonly Player _player2;

    private float _timeToGame = TimerInactive;

    public ScoringHandler(ScoreBar scoreBar, Player player1, Player player2) {
        _scoreBar = scoreBar;
        _player1 = player1;
        _player2 = player2;
    }

    public bool ScoreGoal(Player player) {
        if (_scoreBar.RedWon || _scoreBar.BlueWon) {
            return false;
        }
        if (player == _player1) {
            _scoreBar.BlueScore++;
        }
        else {
            _scoreBar.RedScore++;
        }
        if (!CheckWin()) {
            NewRound();
        }
        return true;
    }

    public bool CheckWin() {
        if (_scoreBar.BlueWon) {
            EndGame(Team.Blue);
            return true;
        }
        if (_scoreBar.RedWon) {
            EndGame(Team.Red);
            return true;
        }
        return false;
    }

    public void StartGame() {
        Game.TiledWorld.LoadMap(Game.GameGraphics.Content, WorldType.Default);
        NewRound();
        Settings.GameState = GameState.InGame;
    }

    public void StartGameIn(float seconds) => _timeToGame = seconds;

    public async Task WaitForPlayerTwo() {
        await GameClient.WaitForPlayers(2);
        StartGameIn(PauseLength);
    }

    public void EndGame(Team team) {
        Game.SoundEffectHandler.PlaySound(SoundEffectID.Win);
        StartRound();

        var tiledWord = Game.TiledWorld;
        if (team == Team.Blue) {
            _player1.OnDeath();
            _player2.OnDeath();
            _player2.Bounds.Position = new(
                tiledWord.WidthInPixels / 2 - _player2.Bounds.Width / 2, 
                tiledWord.HeightInPixels / 2 - _player2.Bounds.Height / 2
            );
            Game.EntityWorld.Remove(_player2);
        }
        else {
            _player2.OnDeath();
            _player1.OnDeath();
            _player1.Bounds.Position = new(
                tiledWord.WidthInPixels / 2 - _player1.Bounds.Width / 2, 
                tiledWord.HeightInPixels / 2 - _player1.Bounds.Height / 2
            );
            Game.EntityWorld.Remove(_player1);
        }
    }

    public static Team GetGoalTeam(Tile goal) {
        if (goal.Type != TileType.Goal) {
            throw new ArgumentException("goal must have tile type TileType.Goal");
        }
        return goal.Bounds.X > Game.TiledWorld.WidthInPixels / 2 ? Team.Red : Team.Blue;
    }

    public Player GetOtherPlayer(Team thisTeam) =>
        thisTeam == Team.Red ? _player1 : _player2;

    public void Overtime() {
        Game.TiledWorld.Reset();
        NewRound();
    }

    public void NewRound() {
        _player1.OnDeath();
        _player2.OnDeath();
        SetPlatforms(false);
        _scoreBar.Pause();
    }

    public void StartRound() {
        SetPlatforms(true);
        Game.SoundEffectHandler.PlaySound(SoundEffectID.Kill);
    }

    private void SetPlatforms(bool destroy) {
        var tileType1 = destroy ? TileType.Air : TileType.Glass;
        var tileType2 = destroy ? TileType.Air : TileType.White;
        int tileSize = Game.TiledWorld.TileSize;

        Player[] players = {_player1, _player2};
        foreach (var player in players) {
            for (var i = 0; i < 5; i++) {
                if (i == 0 || i == 4) {
                    CreateWalls(player, i);
                }
                CreateFloor(player, i);
                CreateCeiling(player, i);
            }
        }

        void CreateFloor(Player player, int i) {
            Game.TiledWorld.SetTileWithEffects(
                tileType2, 
                player.SpawnPosition.X - tileSize * 2 + tileSize * i, 
                player.SpawnPosition.Y + tileSize
            );
        }

        void CreateCeiling(Player player, int i) {
            Game.TiledWorld.SetTileWithEffects(
                tileType1, 
                player.SpawnPosition.X - tileSize * 2 + tileSize * i, 
                player.SpawnPosition.Y - tileSize * 2
            );
        }

        void CreateWalls(Player player, int i) {
            for (var j = 0; j < 2; j++) {
                Game.TiledWorld.SetTileWithEffects(
                    tileType1, 
                    player.SpawnPosition.X - tileSize * 2 + tileSize * i, 
                    player.SpawnPosition.Y - tileSize * j
                );
            }
        }
    }

    public void Update(GameTime gameTime) {
        if (_timeToGame == TimerInactive) {
            return;
        }

        if (_timeToGame == 0) {
            _timeToGame = TimerInactive;
            StartGame();
            return;
        }
        
        _timeToGame -= gameTime.GetElapsedSeconds();
        if (_timeToGame % 1 > ((_timeToGame + gameTime.GetElapsedSeconds()) % 1)) {
            Game.SoundEffectHandler.PlaySound(SoundEffectID.Tick);   
        }
        
        if (_timeToGame < 0) {
            _timeToGame = 0;
        }
    }
}