using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace SideBridge;

public class GameCamera {
    
    private OrthographicCamera _camera;

    public float Zoom { get => _camera.Zoom; set => _camera.Zoom = value;}

    public float MinimumZoom { get => _camera.MinimumZoom; set => _camera.MinimumZoom = value; }

    public float MaximumZoom { get => _camera.MaximumZoom; set => _camera.MaximumZoom = value; }

    public Vector2 Center { get => _camera.Center; } 

    public GameCamera(OrthographicCamera camera) {
        _camera = camera;
    }

    public Vector2 ScreenToWorld(float x, float y) => _camera.ScreenToWorld(x, y);
    public Vector2 WorldToScreen(float x, float y) => _camera.WorldToScreen(x, y);

    public Matrix GetViewMatrix() => _camera.GetViewMatrix();

    public void LookAt(Vector2 pos) => _camera.LookAt(pos);
}