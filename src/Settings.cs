using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;

namespace SideBridge;

public static class Settings {

    public readonly static Keys[] DefaultPlayer1KeyBinds = new Keys[] {
        Keys.A,
        Keys.D,
        Keys.LeftShift,
        Keys.W,
        Keys.S,
        Keys.Q,
        Keys.D1,
        Keys.D2,
        Keys.D3,
        Keys.D4
    };

    public readonly static Keys[] DefaultPlayer2KeyBinds = new Keys[] {
        Keys.Left, 
        Keys.Right, 
        Keys.RightControl, 
        Keys.Up, 
        Keys.Down,
        Keys.NumPad0, 
        Keys.D5, 
        Keys.D6, 
        Keys.D7, 
        Keys.D8
    };

    public static GameState GameState { get; set; }

    public static float MasterVolume { 
        get => SoundEffect.MasterVolume; 
        set => SoundEffect.MasterVolume = value; 
    }

    public static bool FullScreen { 
        get => Game.GameGraphics.IsFullScreen; 
        set => Game.GameGraphics.SetFullScreen(value); 
    }
}