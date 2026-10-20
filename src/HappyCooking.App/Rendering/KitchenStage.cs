using System.Numerics;
using HappyCooking.Core.Content;
using ThreeNet;
using ThreeNet.Effects;

namespace HappyCooking.App.Rendering;

/// <summary>
/// The cooking stage: a tiled kitchen with a stove, a chopping board and the
/// island's host chef. It mirrors the mini-game: flames and steam follow the heat
/// meter, the knife chops on every tap, the spoon stirs, toppings land on the dish.
/// </summary>
public sealed class KitchenStage : Stage
{
    private static readonly Vector3 PotTop = new(-1.15f, 1.55f, -0.35f);
    private static readonly Vector3 DishSpot = new(1.25f, 0.93f, 0.15f);

    private readonly CharacterActor _host;
    private readonly Node _pot;
    private readonly Node _knife;
    private readonly Node _spoon;
    private readonly Node _board;
    private readonly ParticleFx? _steam;
    private readonly ParticleFx? _sparkles;
    private readonly FireEffect? _fire;
    private readonly List<Node> _dropped = [];
    private Node? _dish;
    private CharacterActor? _onion;
    private float _onionTimer;
    private float _chop;
    private float _heat;
    private bool _stirring;
    private bool _fireOn;
    private float _stirAngle;

    public KitchenStage(string hostModel)
    {
        IndoorEnvironment("#FFE9C7");
        AddLights(new Vector3(0.4f, 1f, 0.9f), 3f);

        Props.CheckerFloor(12, 0.6f, "#FFFFFF", "#7FD1FF");
        // Back wall with a band of mint tiles and a window of sky.
        Props.Box(new Vector3(0, 1.6f, -2.6f), new Vector3(7.2f, 3.2f, 0.12f), "#FFF4D6");
        Props.Box(new Vector3(0, 1.25f, -2.52f), new Vector3(7.2f, 0.9f, 0.04f), "#5FD3A6");
        var window = Scene.AddMesh(Props.UnitBox, Props.Glow("#9BEAE6", 0.8f));
        window.Position = new Vector3(1.6f, 2.35f, -2.5f);
        window.Scale = new Vector3(1.6f, 0.9f, 0.04f);
        Props.Box(new Vector3(1.6f, 2.35f, -2.49f), new Vector3(0.06f, 0.9f, 0.04f), "#FFFFFF");
        Props.Box(new Vector3(1.6f, 2.35f, -2.49f), new Vector3(1.6f, 0.06f, 0.04f), "#FFFFFF");
        // Shelf with jars.
        Props.Box(new Vector3(-1.4f, 2.3f, -2.4f), new Vector3(2f, 0.08f, 0.35f), "#C98745");
        string[] jars = ["#FF5A4E", "#FFD45C", "#5FD3A6", "#B57BFF", "#FF8FB1"];
        for (var i = 0; i < jars.Length; i++)
        {
            Props.Cylinder(new Vector3(-2.2f + (i * 0.4f), 2.5f, -2.4f), 0.12f, 0.32f, jars[i]);
            Props.Cylinder(new Vector3(-2.2f + (i * 0.4f), 2.69f, -2.4f), 0.13f, 0.06f, "#FFFFFF");
        }

        // Counter: cabinet body, butter top, tomato kick board.
        Props.Box(new Vector3(0, 0.44f, 0), new Vector3(4.6f, 0.88f, 1.1f), "#FFFFFF");
        Props.Box(new Vector3(0, 0.9f, 0), new Vector3(4.8f, 0.08f, 1.25f), "#FFD45C", roughness: 0.35f);
        Props.Box(new Vector3(0, 0.05f, 0.5f), new Vector3(4.6f, 0.1f, 0.12f), "#FF5A4E");
        for (var i = -1; i <= 1; i++)
        {
            Props.Sphere(new Vector3(i * 1.5f, 0.6f, 0.56f), new Vector3(0.1f, 0.1f, 0.05f), "#2B2A6B");
        }

        // Stove with the pot.
        Props.Box(new Vector3(-1.15f, 0.99f, -0.35f), new Vector3(1.0f, 0.1f, 0.8f), "#E6E5F2", roughness: 0.3f);
        Props.Cylinder(new Vector3(-1.15f, 1.05f, -0.35f), 0.3f, 0.03f, "#3A3A4A");
        _pot = ModelLibrary.LoadStatic(Scene, "cooking_pot");
        _pot.Position = new Vector3(-1.15f, 1.05f, -0.35f);
        _pot.Scale = new Vector3(0.75f);

        // Chopping board, knife and spoon.
        _board = Props.Box(new Vector3(0.1f, 0.97f, 0.15f), new Vector3(0.9f, 0.06f, 0.55f), "#E3B57A");
        _knife = Scene.CreateNode(null, "knife");
        Props.Box(new Vector3(0, 0, 0), new Vector3(0.42f, 0.02f, 0.1f), "#E6E5F2", _knife, 0.15f);
        Props.Box(new Vector3(0.3f, 0, 0), new Vector3(0.2f, 0.05f, 0.06f), "#FF5A4E", _knife);
        _knife.Position = new Vector3(0.15f, 1.15f, 0.2f);
        _spoon = Scene.CreateNode(null, "spoon");
        Props.Cylinder(new Vector3(0, 0.25f, 0), 0.025f, 0.55f, "#C98745", _spoon);
        Props.Sphere(new Vector3(0, -0.02f, 0), new Vector3(0.12f, 0.05f, 0.09f), "#C98745", _spoon);
        _spoon.Position = PotTop + new Vector3(0.1f, -0.1f, 0);
        _spoon.EulerAngles = new Vector3(0, 0, 0.35f);

        Props.Plate(DishSpot);

        _host = AddActor(hostModel, new Vector3(0, 0, -1.0f), 0f, 2.15f);

        _steam = TryAddEffect(() => ParticleFx.Steam(Scene, PotTop));
        _sparkles = TryAddEffect(() => ParticleFx.Sparkles(Scene, DishSpot + new Vector3(0, 0.3f, 0)));
        try
        {
            _fire = new FireEffect(Scene, new Vector3(-1.15f, 1.07f, -0.35f), new Vector3(0.5f, 0.25f, 0.5f));
            _fire.Node.Visible = false;
        }
        catch (ThreeNetException)
        {
            _fire = null;
        }

        Camera.Position = new Vector3(0, 2.75f, 4.6f);
        Camera.LookAt(new Vector3(0, 1.2f, -0.6f));
    }

    /// <summary>Called when a new step begins, so the props match the mini-game.</summary>
    public void BeginStep(StepKind kind)
    {
        _stirring = kind == StepKind.Rhythm;
        _heat = 0;
        if (_fire is not null)
        {
            _fireOn = kind == StepKind.Heat;
            _fire.Node.Visible = _fireOn;
        }

        _knife.Visible = kind is StepKind.Tap or StepKind.Pick;
        _host.Talk();
    }

    /// <summary>Heat meter value 0-1 drives flames and steam.</summary>
    public void SetHeat(float value) => _heat = value;

    public void Chop() => _chop = 1f;

    /// <summary>An ingredient drops into the pot (Pick step).</summary>
    public void DropIngredient(string color)
    {
        var n = Props.Sphere(PotTop + new Vector3(0, 0.9f, 0), new Vector3(0.14f), color);
        _dropped.Add(n);
    }

    /// <summary>A topping lands on the dish at a spot chosen on screen (x, y in 0-1).</summary>
    public void PlaceTopping(float x, float y, string color)
    {
        var p = DishSpot + new Vector3((x - 0.5f) * 0.32f, 0.08f + (Random.Shared.NextSingle() * 0.03f), (y - 0.5f) * 0.32f);
        var n = Props.Sphere(p, new Vector3(0.05f), color);
        _dropped.Add(n);
    }

    /// <summary>Shows the finished dish and reacts to how it went.</summary>
    public void Serve(Recipe recipe, int stars, bool burnt)
    {
        _dish?.Remove();
        if (recipe.Model is { } model && ModelLibrary.Exists(model))
        {
            _dish = ModelLibrary.LoadStatic(Scene, model);
            _dish.Scale = new Vector3(0.75f);
        }
        else
        {
            _dish = Props.GenericDish(recipe.Color, seed: recipe.Id.GetHashCode(StringComparison.Ordinal));
        }

        _dish.Position = DishSpot + new Vector3(0, 0.03f, 0);
        _dish.SetShadowsRecursive(true, true);
        if (_fire is not null)
        {
            _fireOn = false;
            _fire.Node.Visible = false;
        }

        if (stars == 3)
        {
            _sparkles?.Effect.Burst(140);
        }

        if (burnt)
        {
            ShowOnion();
        }
        else
        {
            _host.Cheer();
        }
    }

    /// <summary>Mr. Burnt Onion pops up from behind the counter to gloat.</summary>
    public void ShowOnion()
    {
        _onion ??= AddActor("burnt_onion", new Vector3(-2.6f, -1.2f, -0.9f), 0.5f, 1.3f);
        _onionTimer = 4f;
        _onion.Talk();
    }

    public void ResetDish()
    {
        _dish?.Remove();
        _dish = null;
        foreach (var n in _dropped)
        {
            n.Remove();
        }

        _dropped.Clear();
    }

    protected override void OnUpdate(float dt, double total)
    {
        var t = (float)total;
        if (_steam is not null)
        {
            _steam.Effect.Rate = 6f + (_heat * 50f);
        }

        if (_fire is not null && _fireOn)
        {
            _fire.Size = new Vector3(0.45f, 0.15f + (_heat * 0.35f), 0.45f);
            _fire.Update(dt);
        }

        // The pot rattles as it nears the top of the meter.
        var shake = _heat > 0.75f ? (_heat - 0.75f) * 0.08f : 0f;
        _pot.Position = new Vector3(-1.15f + (MathF.Sin(t * 60f) * shake), 1.05f, -0.35f);

        _chop = MathF.Max(0f, _chop - (dt * 7f));
        _knife.Position = new Vector3(0.15f, 1.02f + (_chop * 0.18f), 0.2f);
        _knife.EulerAngles = new Vector3(0, 0.3f, -_chop * 0.5f);
        _board.Scale = new Vector3(0.9f, 0.06f * (1f - (_chop * 0.2f)), 0.55f);

        if (_stirring)
        {
            _stirAngle += dt * 4.5f;
        }

        _spoon.Position = PotTop + new Vector3(MathF.Cos(_stirAngle) * 0.12f, -0.12f, MathF.Sin(_stirAngle) * 0.1f);
        _spoon.EulerAngles = new Vector3(MathF.Sin(_stirAngle) * 0.25f, 0, 0.35f + (MathF.Cos(_stirAngle) * 0.15f));

        // Picked ingredients fall into the pot and vanish.
        for (var i = _dropped.Count - 1; i >= 0; i--)
        {
            var n = _dropped[i];
            if (n.Position.Y > PotTop.Y - 0.1f && MathF.Abs(n.Position.X - PotTop.X) < 0.01f)
            {
                n.Position -= new Vector3(0, dt * 2.2f, 0);
                if (n.Position.Y <= PotTop.Y - 0.1f)
                {
                    n.Remove();
                    _dropped.RemoveAt(i);
                }
            }
        }

        if (_dish is not null)
        {
            _dish.EulerAngles = new Vector3(0, t * 0.6f, 0);
        }

        if (_onion is not null)
        {
            _onionTimer -= dt;
            var targetY = _onionTimer > 0 ? 0f : -1.4f;
            var p = _onion.Root.Position;
            _onion.Root.Position = new Vector3(p.X, p.Y + ((targetY - p.Y) * MathF.Min(1f, dt * 6f)), p.Z);
        }
    }
}
