using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SideBridge;

public sealed class Player : HitboxObject {

    private float speed;
    private float fallingSpeed;
    private Keys[] keyInputs;

    public Player(Texture2D texture, Vector2 position, int playerNum) : base(texture, position) {
        speed = 150f;
        if (playerNum == 0) {
            keyInputs = new Keys[] { Keys.A, Keys.D, Keys.LeftShift, Keys.Space };
        }
    }

    public override void Update(GameTime gameTime) {
        float distanceTraveled = speed * (float) gameTime.ElapsedGameTime.TotalSeconds;
        if (Keyboard.GetState().IsKeyDown(keyInputs[(int) PlayerAction.Jump])) {
            Jump();
        }
        if (Keyboard.GetState().IsKeyDown(keyInputs[(int) PlayerAction.Sprint])) {
            distanceTraveled *= 2;
        }
        if (Keyboard.GetState().IsKeyDown(keyInputs[(int) PlayerAction.Left])) {
            position.X -= distanceTraveled;
        }
        if (Keyboard.GetState().IsKeyDown(keyInputs[(int) PlayerAction.Right])) {
            position.X += distanceTraveled;
        }

        if (position.X < 0) {
            position.X = 0;
        }
        else if (position.X > Game.Main.WindowWidth - texture.Width) {
            position.X = Game.Main.WindowWidth - texture.Width;
        }

        Fall();
    }

    private void Fall() {
        position.Y += fallingSpeed;
        if (fallingSpeed < Game.MaxVelocity) {
            fallingSpeed += Game.Acceleration;
        }

        if (position.Y < 0) {
            position.Y = 0;
        }
        else if (position.Y > Game.Main.WindowHeight - texture.Height) {
            position.Y = Game.Main.WindowHeight - texture.Height;
        }
    }

    private void Jump() {
        if (position.Y >= Game.Main.WindowHeight - texture.Height) {
            fallingSpeed = -12.5f;
        }
    }
}