using System.Numerics;
using ThreeNet;
using ThreeNet.Effects;

namespace HappyCooking.App.Rendering;

/// <summary>
/// A CPU particle system drawn as camera-facing sprites in one draw call. The
/// geometry buffer is allocated once at <c>capacity</c> and rewritten each frame,
/// so effects never allocate while running.
/// </summary>
public sealed class ParticleFx
{
    private readonly Geometry _geometry;

    private ParticleFx(Scene scene, ParticleEffect effect, MaterialOptions material)
    {
        Effect = effect;
        _geometry = effect.CreateGeometry(scene);
        var mat = scene.CreateMaterial(in material);
        Node = scene.AddMesh(_geometry, mat);
        Node.CastShadow = false;
        Node.ReceiveShadow = false;
    }

    public ParticleEffect Effect { get; }

    public Node Node { get; }

    public void Update(float dt, double total, Node camera)
    {
        Effect.Update(dt, total);
        Effect.Upload(_geometry, camera.WorldMatrix);
    }

    private static Shader SpriteShader(Scene scene) => scene.CreateShader(EffectShaders.Sprite, ShaderLanguage.Wgsl, "sprite");

    private static Shader SmokeShader(Scene scene) => scene.CreateShader(EffectShaders.Smoke, ShaderLanguage.Wgsl, "smoke");

    /// <summary>Soft white steam rising from a pot; raise <see cref="ParticleEffect.Rate"/> as it heats up.</summary>
    public static ParticleFx Steam(Scene scene, Vector3 origin)
    {
        var fx = new ParticleEffect(160)
        {
            Rate = 10,
            Lifetime = 2.2f,
            Variation = 0.4f,
            Size = 0.16f,
            Growth = 3.2f,
            Shape = EmitterShape.Disc,
            Origin = origin,
            EmitterRadius = 0.16f,
            Rise = 0.55f,
            Spread = 0.08f,
            Direction = Vector3.UnitY,
            Gravity = new Vector3(0, 0.12f, 0),
            Drag = 0.3f,
            Turbulence = 0.35f,
            TurbulenceScale = 1.6f,
        };
        return new ParticleFx(scene, fx, ParticleEffect.SmokeMaterial(SmokeShader(scene), new Vector3(1f, 1f, 1f), 0.32f));
    }

    /// <summary>Golden sparkles for a perfect dish; call <see cref="ParticleEffect.Burst"/>.</summary>
    public static ParticleFx Sparkles(Scene scene, Vector3 origin)
    {
        var fx = new ParticleEffect(220)
        {
            Rate = 0,
            Lifetime = 1.3f,
            Variation = 0.5f,
            Size = 0.07f,
            Growth = 0.2f,
            Shape = EmitterShape.Sphere,
            Origin = origin,
            EmitterRadius = 0.15f,
            Radial = true,
            Rise = 1.6f,
            Spread = 0.8f,
            Gravity = new Vector3(0, -1.8f, 0),
            Drag = 0.8f,
            SpinRate = 4f,
        };
        return new ParticleFx(scene, fx, ParticleEffect.GlowMaterial(SpriteShader(scene), new Vector3(1f, 0.85f, 0.3f), new Vector3(1f, 0.4f, 0.6f), 3f));
    }

    /// <summary>Gentle rain over an area; set <see cref="ParticleEffect.Rate"/> to 0 to stop.</summary>
    public static ParticleFx Rain(Scene scene, Vector3 center, Vector3 halfSize)
    {
        var fx = new ParticleEffect(900)
        {
            Rate = 0,
            Lifetime = 1.1f,
            Variation = 0.2f,
            Size = 0.03f,
            Growth = 1f,
            Shape = EmitterShape.Box,
            Origin = center,
            EmitterSize = halfSize,
            Rise = -6f,
            Direction = Vector3.UnitY,
            Gravity = new Vector3(0, -4f, 0),
            StretchBySpeed = 0.08f,
            FloorHeight = 0f,
            Bounce = 0f,
        };
        return new ParticleFx(scene, fx, ParticleEffect.GlowMaterial(SpriteShader(scene), new Vector3(0.75f, 0.9f, 1f), new Vector3(0.6f, 0.8f, 1f), 0.8f));
    }

    /// <summary>Snow for Dessert Wonderland (powdered sugar, officially).</summary>
    public static ParticleFx Snow(Scene scene, Vector3 center, Vector3 halfSize)
    {
        var fx = new ParticleEffect(600)
        {
            Rate = 0,
            Lifetime = 5f,
            Variation = 0.3f,
            Size = 0.05f,
            Growth = 1f,
            Shape = EmitterShape.Box,
            Origin = center,
            EmitterSize = halfSize,
            Rise = -0.4f,
            Direction = Vector3.UnitY,
            Gravity = new Vector3(0, -0.2f, 0),
            Turbulence = 0.6f,
            FloorHeight = 0f,
        };
        return new ParticleFx(scene, fx, ParticleEffect.GlowMaterial(SpriteShader(scene), new Vector3(1f), new Vector3(0.9f, 0.95f, 1f), 1.2f));
    }
}
