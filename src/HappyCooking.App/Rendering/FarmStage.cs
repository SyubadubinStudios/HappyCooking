using System.Numerics;
using HappyCooking.Core.Content;
using HappyCooking.Core.Systems;
using ThreeNet;

namespace HappyCooking.App.Rendering;

/// <summary>
/// A grassy island garden. Each plot is a soil bed whose crop grows (scales up)
/// with the real growth value; ready crops bounce. Tapping a plot in 3D selects it.
/// </summary>
public sealed class FarmStage : Stage, Controls.IInteractiveStage
{
    private InteractionManager? _interaction;

    private readonly List<PlotVisual> _plots = [];
    private readonly ParticleFx? _rain;
    private readonly ParticleFx? _snow;
    private readonly CharacterActor _helper;

    private sealed class PlotVisual
    {
        public required Node Soil { get; init; }

        public required Node Crop { get; init; }

        public string? CropId { get; set; }

        public float Growth { get; set; }

        public bool Ready { get; set; }

        public bool Selected { get; set; }

        public required Node Marker { get; init; }
    }

    public FarmStage()
    {
        var sunDir = new Vector3(0.4f, 1f, 0.5f);
        OutdoorEnvironment(sunDir);
        AddLights(sunDir, 3.4f);

        // Island: sea disc, sand ring, grass top.
        Props.Cylinder(new Vector3(0, -0.6f, 0), 30f, 0.2f, "#2EC4D6");
        Props.Cylinder(new Vector3(0, -0.35f, 0), 8.2f, 0.5f, "#F6D9A0");
        Props.Cylinder(new Vector3(0, -0.15f, 0), 7.4f, 0.32f, "#7CCB5A");

        // White fence around the garden.
        for (var i = 0; i < 26; i++)
        {
            var a = i * MathF.Tau / 26f;
            Props.Box(new Vector3(MathF.Cos(a) * 5.4f, 0.3f, MathF.Sin(a) * 4.6f), new Vector3(0.12f, 0.6f, 0.12f), "#FFFFFF");
        }

        Props.Tree(new Vector3(-5.8f, 0, -3.2f), 1.3f);
        Props.Tree(new Vector3(5.6f, 0, -3.5f), 1.1f, "#5DBB4A");
        Props.Palm(new Vector3(6.4f, 0, 1.8f), 1.2f);
        Props.Palm(new Vector3(-6.2f, 0, 2.2f), 1.1f);

        // Little red barn.
        Props.Box(new Vector3(-4.0f, 0.8f, -4.6f), new Vector3(2.0f, 1.6f, 1.4f), "#E8423F");
        var roof = Props.Cone(new Vector3(-4.0f, 2.0f, -4.6f), 1.5f, 0.9f, "#FFFFFF");
        roof.EulerAngles = new Vector3(0, MathF.PI / 4, 0);
        Props.Box(new Vector3(-4.0f, 0.55f, -3.88f), new Vector3(0.7f, 1.1f, 0.04f), "#FFFFFF");

        _helper = AddActor("pepper_bot", new Vector3(3.8f, 0, -1.2f), -0.8f, 1.3f);

        _rain = TryAddEffect(() => ParticleFx.Rain(Scene, new Vector3(0, 7, 0), new Vector3(7, 0.2f, 5)));
        _snow = TryAddEffect(() => ParticleFx.Snow(Scene, new Vector3(0, 6, 0), new Vector3(7, 0.2f, 5)));

        Camera.Position = new Vector3(0, 7.6f, 8.8f);
        Camera.LookAt(new Vector3(0, 0, 0.4f));
    }

    /// <summary>Raised when a plot is clicked in the 3D view.</summary>
    public event Action<int>? PlotClicked;

    public void Bind(InteractionManager? interaction)
    {
        _interaction = interaction;
        for (var i = 0; i < _plots.Count; i++)
        {
            var index = i;
            interaction?.OnClick(_plots[i].Soil, _ => PlotClicked?.Invoke(index));
        }
    }

    public void BuildPlots(int count)
    {
        foreach (var p in _plots)
        {
            p.Soil.Remove();
        }

        _plots.Clear();
        const int columns = 4;
        for (var i = 0; i < count; i++)
        {
            var col = i % columns;
            var row = i / columns;
            var pos = new Vector3((col - ((columns - 1) / 2f)) * 2.0f, 0.02f, (row * 1.9f) - 1.6f);
            var soil = Props.Box(pos, new Vector3(1.6f, 0.22f, 1.4f), "#8C5A3C");
            var crop = Scene.CreateNode(null, "crop");
            crop.Position = pos + new Vector3(0, 0.12f, 0);
            var marker = Props.Cylinder(pos + new Vector3(0, -0.06f, 0), 0.95f, 0.06f, "#FFD45C");
            marker.Visible = false;
            _plots.Add(new PlotVisual { Soil = soil, Crop = crop, Marker = marker });

            var index = i;
            _interaction?.OnClick(soil, _ => PlotClicked?.Invoke(index));
        }
    }

    public void SetSelected(int index)
    {
        for (var i = 0; i < _plots.Count; i++)
        {
            _plots[i].Marker.Visible = i == index;
        }
    }

    public void SetWeather(Weather weather)
    {
        if (_rain is not null)
        {
            _rain.Effect.Rate = weather == Weather.Rain ? 500 : 0;
        }

        if (_snow is not null)
        {
            _snow.Effect.Rate = weather == Weather.Snow ? 90 : 0;
        }
    }

    /// <summary>Rebuilds a plot's crop when the planted crop changes and rescales it with growth.</summary>
    public void UpdatePlot(int index, Crop? crop, float growth, bool ready)
    {
        if ((uint)index >= (uint)_plots.Count)
        {
            return;
        }

        var p = _plots[index];
        if (p.CropId != crop?.Id)
        {
            foreach (var child in p.Crop.Children.ToList())
            {
                child.Remove();
            }

            p.CropId = crop?.Id;
            if (crop is not null)
            {
                BuildCrop(p.Crop, crop.Color);
            }
        }

        p.Growth = growth;
        p.Ready = ready;
    }

    /// <summary>Three little plants per bed: leafy stems with the produce on top.</summary>
    private void BuildCrop(Node parent, string color)
    {
        for (var k = 0; k < 3; k++)
        {
            var x = (k - 1) * 0.45f;
            Props.Cylinder(new Vector3(x, 0.25f, 0), 0.03f, 0.5f, "#3C9A3C", parent);
            var leaf = Props.Sphere(new Vector3(x + 0.08f, 0.3f, 0), new Vector3(0.22f, 0.05f, 0.12f), "#5DBB4A", parent);
            leaf.EulerAngles = new Vector3(0, 0, 0.5f);
            Props.Sphere(new Vector3(x, 0.55f, 0), new Vector3(0.26f), color, parent);
        }
    }

    protected override void OnUpdate(float dt, double total)
    {
        for (var i = 0; i < _plots.Count; i++)
        {
            var p = _plots[i];
            if (p.CropId is null)
            {
                continue;
            }

            var s = 0.15f + (0.85f * p.Growth);
            var bounce = p.Ready ? 1f + (MathF.Abs(MathF.Sin(((float)total * 4f) + i)) * 0.12f) : 1f;
            p.Crop.Scale = new Vector3(s, s * bounce, s);
        }
    }

    public void Celebrate() => _helper.Cheer();
}
