using System.Numerics;
using HappyCooking.Core.State;
using ThreeNet;

namespace HappyCooking.App.Rendering;

/// <summary>
/// One 3D scene shown behind a screen. A stage owns its <see cref="Scene"/> and
/// everything in it; it is created when the screen opens and disposed when it
/// closes, so only one scene's GPU memory is alive at a time.
/// </summary>
public abstract class Stage : IDisposable
{
    private readonly List<CharacterActor> _actors = [];
    private readonly List<ParticleFx> _effects = [];
    private bool _disposed;

    protected Stage()
    {
        Scene = new Scene();
        Props = new Props(Scene);
        Camera = Scene.AddCamera(ThreeNet.Camera.Perspective(MathHelpers.ToRadians(45f), 0.1f, 200f), new Vector3(0, 3, 8));
    }

    public Scene Scene { get; }

    public Props Props { get; }

    public Node Camera { get; }

    public Node? Sun { get; private set; }

    public double TotalSeconds { get; private set; }

    protected IReadOnlyList<CharacterActor> Actors => _actors;

    /// <summary>Renderer settings for a graphics quality level. Lower levels trade effects for battery and frame rate.</summary>
    public static RendererOptions OptionsFor(GraphicsQuality quality)
    {
        var o = RendererOptions.Default;
        o.ToneMapping = ToneMapping.Aces;
        o.Exposure = 1.05f;
        o.Saturation = 1.12f;
        o.Contrast = 1.04f;
        o.Bloom = quality != GraphicsQuality.Low;
        o.BloomIntensity = 0.35f;
        o.BloomThreshold = 1.1f;
        o.Shadows = quality != GraphicsQuality.Low;
        o.ShadowMapSize = quality == GraphicsQuality.High ? 2048 : 1024;
        o.ShadowCascades = quality == GraphicsQuality.High ? 3 : 2;
        o.ShadowSoftness = 2;
        o.ShadowDistance = 30f;
        o.Ssao = quality == GraphicsQuality.High;
        o.SsaoRadius = 0.4f;
        o.SsaoIntensity = 1.2f;
        o.MsaaSamples = quality switch { GraphicsQuality.High => 4, GraphicsQuality.Medium => 2, _ => 1 };
        o.Vignette = 0.18f;
        o.FrustumCulling = true;
        return o;
    }

    public static double RenderScaleFor(GraphicsQuality quality) => quality switch
    {
        GraphicsQuality.High => 1.0,
        GraphicsQuality.Medium => 0.85,
        _ => 0.65,
    };

    /// <summary>Warm studio lighting: a sky/ground hemisphere fill plus a shadow-casting sun.</summary>
    protected void AddLights(Vector3 sunDirection, float sunIntensity = 3.2f, string sunColor = "#FFF1DC")
    {
        Scene.AddLight(Light.Hemisphere(Props.Rgb("#CFEFFF"), Props.Rgb("#F6D9B8"), 1.1f));
        var sun = Light.Directional(Props.Rgb(sunColor), sunIntensity);
        sun.CastShadow = true;
        sun.ShadowStrength = 0.55f;
        sun.ShadowBias = 0.002f;
        sun.ShadowNormalBias = 1.5f;
        Sun = Scene.AddLight(sun);
        Sun.Position = sunDirection * 10f;
        Sun.LookAt(Vector3.Zero);
    }

    protected void IndoorEnvironment(string background)
    {
        var env = SceneEnvironment.Default;
        env.Background = Props.Hex(background);
        env.Sky = SkyMode.Color;
        env.AmbientColor = Props.Rgb("#FFF4E6");
        env.AmbientIntensity = 0.25f;
        Scene.Environment = env;
    }

    protected void OutdoorEnvironment(Vector3 sunDirection, float haze = 0.25f, float clouds = 0.35f)
    {
        var env = SceneEnvironment.Default;
        env.Sky = SkyMode.Procedural;
        env.SunDirection = Vector3.Normalize(sunDirection);
        env.SkyIntensity = 1f;
        env.SkyHaze = haze;
        env.SkyClouds = clouds;
        env.AmbientColor = Props.Rgb("#E8F6FF");
        env.AmbientIntensity = 0.2f;
        env.FogColor = Props.Rgb("#BFEFFF");
        env.FogDensity = 0.004f;
        Scene.Environment = env;
    }

    protected CharacterActor AddActor(string model, Vector3 position, float yaw = 0f, float scale = 1f)
    {
        var actor = new CharacterActor(Scene, model, position, yaw, scale);
        actor.Root.SetShadowsRecursive(true, true);
        _actors.Add(actor);
        return actor;
    }

    protected void RemoveActor(CharacterActor actor)
    {
        _actors.Remove(actor);
        actor.Remove();
    }

    /// <summary>Effects are optional polish: a GPU without the needed features simply shows none.</summary>
    protected ParticleFx? TryAddEffect(Func<ParticleFx> create)
    {
        try
        {
            var fx = create();
            _effects.Add(fx);
            return fx;
        }
        catch (ThreeNetException ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Effect disabled: {ex.Message}");
            return null;
        }
    }

    public void Update(float dt, double total)
    {
        if (_disposed)
        {
            return;
        }

        TotalSeconds = total;
        Updated?.Invoke(dt);
        OnUpdate(dt, total);
        for (var i = 0; i < _actors.Count; i++)
        {
            _actors[i].Update(dt);
        }

        Scene.UpdateAnimations(dt);
        for (var i = 0; i < _effects.Count; i++)
        {
            _effects[i].Update(dt, total, Camera);
        }
    }

    protected abstract void OnUpdate(float dt, double total);

    /// <summary>Raised after every frame's update, so a page can step its game logic in sync with the picture.</summary>
    public event Action<float>? Updated;

    /// <summary>Bobbing for floating things (food on the title screen, the fishing float).</summary>
    protected static float Bob(double t, float speed = 2f, float height = 0.05f, float phase = 0f) =>
        MathF.Sin(((float)t * speed) + phase) * height;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Scene.Dispose();
        GC.SuppressFinalize(this);
    }
}
