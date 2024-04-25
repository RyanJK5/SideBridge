using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SideBridge;

public class GameCamera {
    
    private const float MinYZoom = 1.55f;
    private const float SideBoundWidth = 100f;

    private const float HighestPlayerY = 280f;
    private const float DefaultY = 520f;
    private const float LowerDefaultY = 720f;

    private const float CameraSpeed = 5f;
    private const float ZoomSpeed = 0.02f;

    private readonly OrthographicCamera _camera;

    public float Zoom { get => _camera.Zoom; set => _camera.Zoom = value;}

    public float MinimumZoom { get => _camera.MinimumZoom; set => _camera.MinimumZoom = value; }

    public float MaximumZoom { get => _camera.MaximumZoom; set => _camera.MaximumZoom = value; }

    public Vector2 Center { get => _camera.Center; } 

    public GameCamera(OrthographicCamera camera) => _camera = camera;

    public void Update() {
        float oldZoom = Zoom;
        float oldY = Center.Y;

        RectangleF p1 = Game.Player1.Bounds;
        RectangleF p2 = Game.Player2.Bounds;
        
        Player lowerPlayer = p1.Bottom > p2.Bottom ? Game.Player1 : Game.Player2;
        Player upperPlayer = p1.Top < p2.Top ? Game.Player1 : Game.Player2;
        
        Vector2 lowerBottom = lowerPlayer.Bounds.BottomLeft;
        Vector2 upperTop = upperPlayer.Bounds.TopLeft;

        // set initial zoom to fit player 1 and 2 horizontally
        float sideBounds = SideBoundWidth / Zoom;
        float zoomX = Game.WindowWidth / (MathF.Abs(p1.X - p2.Right) + sideBounds * 2f);
        zoomX = Game.Constrict(zoomX, MinimumZoom, MaximumZoom);
        Zoom = zoomX;
        
        // try looking at the default position
        LookAt(new((p1.X + p2.Right) / 2, DefaultY));
        
        // if default position is too high, try looking at lower default position
        float targetY = DefaultY;
        float targetZoom = zoomX;
        if (WorldToScreen(lowerBottom).Y > Game.WindowHeight) {
            targetY = LowerDefaultY;
        }
        LookAt(new(Center.X, targetY));

        // if neither captures both players, try looking directly between both players
        if (WorldToScreen(upperTop).Y < 0) {
            Console.WriteLine(upperTop.Y);
            if (upperTop.Y < HighestPlayerY) {
                targetY = oldY;
                targetZoom = oldZoom;
            }
            else {
                targetY = (p1.Center.Y + p2.Center.Y) / 2;
                targetZoom = MinYZoom;
            }
        }

        LookAt(new((p1.X + p2.Right) / 2, Game.MoveAtSpeed(oldY, CameraSpeed, targetY)));
        if (targetZoom != zoomX || oldZoom < zoomX) {
            Zoom = Game.MoveAtSpeed(oldZoom, ZoomSpeed, targetZoom);
        }
    }

    public Vector2 ScreenToWorld(float x, float y) => _camera.ScreenToWorld(x, y);
    
    public Vector2 ScreenToWorld(Vector2 pos) => _camera.ScreenToWorld(pos.X, pos.Y);

    public Vector2 WorldToScreen(float x, float y) => _camera.WorldToScreen(x, y);
    
    public Vector2 WorldToScreen(Vector2 pos) => _camera.WorldToScreen(pos.X, pos.Y);

    public Matrix GetViewMatrix() => _camera.GetViewMatrix();

    public void LookAt(Vector2 pos) => _camera.LookAt(pos);
}