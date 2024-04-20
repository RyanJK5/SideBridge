using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SideBridge;

public class ScoreBar : UI {

    private const int TimerWidth = 58;
    private const int GameLength = 60 * 5;
    private const int PauseLength = 5;
    public const int MaxScore = 5;

    public int RedScore { 
        get => _redScore; 
        set {
            if (value < 5) {
                _redScore = value;
            }
            else {
                _redScore = MaxScore;
                _timeSincePause = 0;
            }
        }
    }
    private int _redScore;

    public int BlueScore { 
        get => _blueScore; 
        set {
            if (value < 5) {
                _blueScore = value;
            }
            else {
                _blueScore = MaxScore;
                _timeSincePause = 0;
            }
        }
    }

    public bool RedWon {
        get => (_overtime && _redScore > _blueScore) || _redScore == MaxScore;
    }

    public bool BlueWon {
        get => (_overtime && _blueScore > _redScore) || _blueScore == MaxScore;
    }

    private int _blueScore;

    public int CellWidth { get => (Texture.Width - TimerWidth) / 10; }

    public string StringTime { 
        get => (int) (_elapsedTime / 60) + ":" + ((int) (_elapsedTime % 60) < 10 ? "0" : "") + (int) (_elapsedTime % 60);
    }

    private float _timeSincePause;

    private float _elapsedTime;
    private bool _overtime;

    private readonly Texture2D _emptyTexture;

    public ScoreBar(Texture2D fullTexture, Texture2D emptyTexture, Vector2 drawPos) : base(fullTexture, drawPos) {
        _emptyTexture = emptyTexture;
        _elapsedTime = GameLength;
    }

    public void Pause() => _timeSincePause = PauseLength;

    public override void Update(GameTime gameTime) {
        if (RedWon || BlueWon) {
            return;
        }
        if (_timeSincePause > 0) {
            _timeSincePause -= gameTime.GetElapsedSeconds();
            if (_timeSincePause % 1 > (_timeSincePause + gameTime.GetElapsedSeconds()) % 1) {
                Game.GetSoundEffect(SoundEffectID.Tick).CreateInstance().Play();
            }
            if (_timeSincePause <= 0) {
                Game.StartRound();
            }
            return;
        }
        if (_overtime) {
            _elapsedTime += gameTime.GetElapsedSeconds();
            return;
        }
        _elapsedTime -= gameTime.GetElapsedSeconds();
        if (_elapsedTime < 1 && _elapsedTime + gameTime.GetElapsedSeconds() > 1) {
            Game.GetSoundEffect(SoundEffectID.Tick).CreateInstance().Play();
        }
        if (_elapsedTime < 0) {
            _elapsedTime = 0;
            _overtime = true;
            if (!Game.CheckWin()) {
                Game.Overtime();
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch) {
        spriteBatch.Draw(_emptyTexture, DrawPos, Color.White);
        
        Vector2 strDimensions = Game.Font.MeasureString(StringTime);
        const int xAdj = 1;
        const int yAdj = -2;
        spriteBatch.DrawString(
            Game.Font, 
            StringTime, 
            new(DrawPos.X + Texture.Width / 2 - strDimensions.X / 2 + xAdj, DrawPos.Y + Texture.Height / 2 - strDimensions.Y / 2 + yAdj), 
            _overtime ? Color.Red : Color.White

        );
        if (BlueScore > 0) {
            spriteBatch.Draw(
                Texture,
                DrawPos,
                new Rectangle(0, 0, CellWidth * BlueScore + 1, Texture.Height),
                Color.White,
                0f,
                Vector2.Zero,
                Vector2.One,
                SpriteEffects.None,
                0f
            );
        }
        if (RedScore > 0) {
            var scoreWidth = CellWidth * (5 - RedScore);
            var scoreX = Texture.Width / 2 + TimerWidth / 2 + scoreWidth;
            spriteBatch.Draw(
                Texture,
                new Vector2(DrawPos.X + scoreX, DrawPos.Y),
                new Rectangle(scoreX, 0, Texture.Width - scoreX, Texture.Height),
                Color.White,
                0f,
                Vector2.Zero,
                Vector2.One,
                SpriteEffects.None,
                0f
            );
        }
    }
}