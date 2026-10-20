using System.Numerics;
using HappyCooking.Core.Systems;
using ThreeNet;
using ThreeNet.Effects;

namespace HappyCooking.App.Rendering;

/// <summary>
/// A wooden pier over animated water. The float bobs while waiting, dips and
/// splashes on a bite, and the caught fish leaps out of the water.
/// </summary>
public sealed class FishingStage : Stage
{
    private static readonly Vector3 FloatRest = new(0.4f, 0f, -3.6f);

    private readonly WaterSurface? _water;
    private readonly Node? _flatSea;
    private readonly Node _float;
    private readonly Node _rod;
    private readonly Node _line;
    private readonly Node _fish;
    private readonly CharacterActor _angler;
    private readonly ParticleFx? _sparkles;
    private FishingPhase _phase = FishingPhase.Idle;
    private float _phaseTime;
    private float _jump = -1f;

    public FishingStage(float timeOfDay, bool night)
    {
        // Sun follows the in-game clock; at night a low cool "moon" light takes over.
        var angle = (timeOfDay - 0.25f) * MathF.Tau;
        var sunDir = night ? new Vector3(-0.3f, 0.5f, -0.6f) : new Vector3(MathF.Cos(angle) * 0.6f, MathF.Max(0.15f, MathF.Sin(angle)), -0.7f);
        OutdoorEnvironment(sunDir, night ? 0.1f : 0.3f, 0.3f);
        if (night)
        {
            var env = Scene.Environment;
            env.SkyIntensity = 0.18f;
            env.AmbientIntensity = 0.12f;
            Scene.Environment = env;
        }

        AddLights(sunDir, night ? 0.9f : 3.2f, night ? "#BFD3FF" : "#FFF1DC");

        try
        {
            _water = new WaterSurface(Scene, new Vector3(0, 0, -6), 28f, -2.2f, 128, 160)
            {
                Style = WaterStyle.Ocean,
                SunDirection = Vector3.Normalize(sunDir),
            };
            _water.AddFloor(MaterialOptions.Pbr(Props.Hex("#F6D9A0"), 0f, 0.9f));
        }
        catch (ThreeNetException)
        {
            // No compute support: fall back to a flat, glossy sea.
            _water = null;
            _flatSea = Props.Box(new Vector3(0, -0.05f, -6), new Vector3(28, 0.1f, 28), "#2EC4D6", roughness: 0.1f);
        }

        // Pier: planks on posts, reaching out over the water.
        for (var i = 0; i < 9; i++)
        {
            Props.Box(new Vector3(0, 0.45f, 3.2f - (i * 0.62f)), new Vector3(1.8f, 0.1f, 0.56f), i % 2 == 0 ? "#C98745" : "#B07A4A");
        }

        foreach (var z in new[] { 3.0f, 0.6f, -1.7f })
        {
            foreach (var x in new[] { -0.85f, 0.85f })
            {
                Props.Cylinder(new Vector3(x, -0.3f, z), 0.1f, 1.6f, "#8C5A3C");
            }
        }

        // A distant island on the horizon.
        Props.Cylinder(new Vector3(-9, -0.2f, -16), 3.5f, 0.6f, "#F6D9A0");
        Props.Palm(new Vector3(-9.3f, 0.1f, -16), 1.6f);
        Props.Palm(new Vector3(-8.0f, 0.1f, -15.5f), 1.3f);

        _angler = AddActor("chef_happy", new Vector3(-0.3f, 0.5f, -1.4f), MathF.PI, 1.2f);
        _rod = Scene.CreateNode(null, "rod");
        Props.Cylinder(new Vector3(0, 0.9f, 0), 0.025f, 1.8f, "#5C5B91", _rod);
        _rod.Position = new Vector3(0.2f, 0.95f, -1.7f);
        _rod.EulerAngles = new Vector3(-0.75f, 0, -0.15f);

        _float = Scene.CreateNode(null, "float");
        Props.Sphere(new Vector3(0, 0.06f, 0), new Vector3(0.16f), "#FF5A4E", _float);
        Props.Sphere(new Vector3(0, -0.02f, 0), new Vector3(0.16f, 0.1f, 0.16f), "#FFFFFF", _float);
        _float.Position = FloatRest;

        _line = Props.Cylinder(Vector3.Zero, 0.006f, 1f, "#FFFFFF");

        _fish = Scene.CreateNode(null, "fish");
        Props.Sphere(Vector3.Zero, new Vector3(0.5f, 0.3f, 0.22f), "#FF8C69", _fish);
        var tail = Props.Cone(new Vector3(-0.3f, 0, 0), 0.16f, 0.25f, "#FF8C69", _fish);
        tail.EulerAngles = new Vector3(0, 0, MathF.PI / 2);
        Props.Sphere(new Vector3(0.16f, 0.06f, 0.1f), new Vector3(0.06f), "#2B2A6B", _fish);
        _fish.Visible = false;

        _sparkles = TryAddEffect(() => ParticleFx.Sparkles(Scene, FloatRest + new Vector3(0, 1.2f, 0)));

        Camera.Position = new Vector3(1.6f, 2.6f, 3.6f);
        Camera.LookAt(new Vector3(0, 0.3f, -3.2f));
    }

    public void SetPhase(FishingPhase phase)
    {
        if (phase == _phase)
        {
            return;
        }

        _phase = phase;
        _phaseTime = 0;
        switch (phase)
        {
            case FishingPhase.Waiting:
                _water?.Splash(new Vector3(FloatRest.X, 0, FloatRest.Z), 0.25f, 0.05f);
                break;
            case FishingPhase.Bite:
                _water?.Splash(new Vector3(FloatRest.X, 0, FloatRest.Z), 0.18f, 0.06f);
                break;
            case FishingPhase.Escaped:
                _angler.Talk();
                break;
        }
    }

    public void Caught(string colorHex)
    {
        foreach (var child in _fish.Children)
        {
            if (child.Material is { } m && child.Name != "eye")
            {
                m.BaseColor = Props.Hex(colorHex);
                break;
            }
        }

        _jump = 0f;
        _fish.Visible = true;
        _water?.Splash(new Vector3(FloatRest.X, 0, FloatRest.Z), 0.4f, 0.12f);
        _sparkles?.Effect.Burst(90);
        _angler.Cheer();
    }

    protected override void OnUpdate(float dt, double total)
    {
        _phaseTime += dt;
        _water?.Update(dt);

        var waterY = _water?.HeightAt(FloatRest.X, FloatRest.Z) ?? 0f;
        var y = waterY + 0.02f + Bob(total, 2.4f, 0.03f);
        var x = FloatRest.X;
        switch (_phase)
        {
            case FishingPhase.Idle:
            case FishingPhase.Caught:
            case FishingPhase.Escaped:
                // Line reeled in: float hangs near the rod tip.
                y = 1.8f;
                x = 0.5f;
                break;
            case FishingPhase.Bite:
                y -= 0.12f + (MathF.Abs(MathF.Sin(_phaseTime * 18f)) * 0.08f);
                break;
            case FishingPhase.Reeling:
                y -= 0.06f + (MathF.Sin(_phaseTime * 25f) * 0.05f);
                x += MathF.Sin(_phaseTime * 7f) * 0.25f;
                if (((int)(_phaseTime * 6)) % 2 == 0)
                {
                    _water?.Splash(new Vector3(x, 0, FloatRest.Z), 0.1f, 0.01f);
                }

                break;
        }

        var floatPos = new Vector3(x, y, _phase is FishingPhase.Idle or FishingPhase.Caught or FishingPhase.Escaped ? -2.6f : FloatRest.Z);
        _float.Position = floatPos;

        // Fishing line from rod tip to float.
        var tip = _rod.WorldPosition + Vector3.Transform(new Vector3(0, 1.8f, 0), Quaternion.CreateFromYawPitchRoll(0, -0.75f, -0.15f));
        var mid = (tip + floatPos) / 2f;
        var dir = floatPos - tip;
        var len = dir.Length();
        _line.Position = mid;
        _line.Scale = new Vector3(0.012f, len, 0.012f);
        _line.Rotation = FromTo(Vector3.UnitY, dir / len);

        _rod.EulerAngles = new Vector3(_phase == FishingPhase.Reeling ? -0.95f + (MathF.Sin(_phaseTime * 20f) * 0.06f) : -0.75f, 0, -0.15f);

        if (_jump >= 0f)
        {
            _jump += dt;
            var t = _jump / 1.2f;
            _fish.Position = new Vector3(FloatRest.X - (t * 1.4f), MathF.Sin(t * MathF.PI) * 1.8f, FloatRest.Z + (t * 1.8f));
            _fish.EulerAngles = new Vector3(0, MathF.PI / 2, -t * MathF.PI * 2f);
            if (t >= 1f)
            {
                _jump = -1f;
                _fish.Visible = false;
            }
        }
    }

    private static Quaternion FromTo(Vector3 from, Vector3 to)
    {
        var dot = Vector3.Dot(from, to);
        if (dot > 0.9999f)
        {
            return Quaternion.Identity;
        }

        if (dot < -0.9999f)
        {
            return Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
        }

        var axis = Vector3.Normalize(Vector3.Cross(from, to));
        return Quaternion.CreateFromAxisAngle(axis, MathF.Acos(dot));
    }
}
