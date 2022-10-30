using Microsoft.Xna.Framework.Input;

namespace SideBridge.Components;

public class Input {
    public Keys[] ActionKeys;

    public Input(params Keys[] actionKeys) {
        ActionKeys = actionKeys;
    }

    public Keys this[PlayerAction index] {
        get => ActionKeys[(int) index];
        set => ActionKeys[(int) index] = value;
    }
}