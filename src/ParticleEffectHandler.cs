using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using MonoGame.Extended.Particles.Profiles;
using MonoGame.Extended.TextureAtlases;

namespace SideBridge;

public class ParticleEffectHandler {

    private readonly ParticleEffect[] _blockParticleEffects;

    public ParticleEffectHandler(GraphicsDevice graphics) {
        _blockParticleEffects = new ParticleEffect[Enum.GetValues(typeof(TileType)).Length];
        
        TileType[] types = TileTypes.GetParticleTypes();
        foreach (TileType type in types) {
            _blockParticleEffects[(int) type] = CreateParticleEffect(graphics, TileTypes.GetParticleColor(type));
        }
    }

    private static ParticleEffect CreateParticleEffect(GraphicsDevice graphics, Color color) {
        var particleTexture = new Texture2D(graphics, 1, 1);
        particleTexture.SetData(new[] { color });
        var textureRegion = new TextureRegion2D(particleTexture);
        return new ParticleEffect(autoTrigger: false) {
            Emitters = new List<ParticleEmitter> {
                new(textureRegion, 10, TimeSpan.FromSeconds(0.5f), Profile.BoxFill(40, 40)) {
                    AutoTrigger = false,
                    Parameters = new ParticleReleaseParameters {
                        Speed = new Range<float>(200f),
                        Opacity = new Range<float>(1f),
                        Quantity = 3,
                        Rotation = new Range<float>(0f, 1f),
                    },
                    Modifiers = {
                        new LinearGravityModifier {Direction = Vector2.UnitY, Strength = 1500f},
                        new AgeModifier() {
                            Interpolators = new List<Interpolator>() {
                                new ScaleInterpolator { StartValue = new Vector2(10f, 10f), EndValue = new Vector2(0f, 0f) }
                            }
                        }
                    }
                }
            }
        };
    }

    public void Draw(SpriteBatch spriteBatch) {
        foreach (var particleEffect in _blockParticleEffects) {
            if (particleEffect != null) {
                spriteBatch.Draw(particleEffect);
            }
        }
    }

    public void Update(GameTime gameTime) {
        foreach (var particleEffect in _blockParticleEffects) {
            particleEffect?.Update((float) gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    public void SpawnParticles(TileType type, float x, float y) => SpawnParticles(type, new Vector2(x, y));

    public void SpawnParticles(TileType type, Vector2 pos) => _blockParticleEffects[(int) type]?.Trigger(pos);
    

}