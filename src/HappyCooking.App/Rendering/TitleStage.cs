using System.Numerics;
using ThreeNet;

namespace HappyCooking.App.Rendering;

/// <summary>
/// The welcome stage: the whole cast on a round cake-stand stage, floating dishes
/// circling above, and Mr. Burnt Onion sulking at the back.
/// </summary>
public sealed class TitleStage : Stage
{
    private readonly List<(Node Node, float Radius, float Speed, float Height, float Phase)> _orbiters = [];
    private readonly CharacterActor _host;
    private float _cheerTimer = 3f;
    private int _cheerIndex;

    public TitleStage()
    {
        IndoorEnvironment("#2EC4D6");
        AddLights(new Vector3(0.5f, 1f, 0.8f));

        // Cake-stand stage: three stacked discs with a frosting rim.
        Props.Cylinder(new Vector3(0, -0.35f, 0), 3.3f, 0.5f, "#FF8FB1");
        Props.Cylinder(new Vector3(0, -0.05f, 0), 3.0f, 0.12f, "#FFF4D6");
        Props.Cylinder(new Vector3(0, -0.85f, 0), 3.7f, 0.5f, "#FFD45C");
        for (var i = 0; i < 28; i++)
        {
            var a = i * MathF.Tau / 28f;
            Props.Sphere(new Vector3(MathF.Cos(a) * 3.28f, -0.08f, MathF.Sin(a) * 3.28f), new Vector3(0.22f), i % 2 == 0 ? "#FFFFFF" : "#FF5A4E");
        }

        _host = AddActor("chef_happy", new Vector3(0, 0.02f, 0.6f), 0f, 1.5f);
        AddActor("coco_panda", new Vector3(-1.5f, 0.02f, 0.2f), 0.35f, 1.4f);
        AddActor("jenggo_chicken", new Vector3(1.5f, 0.02f, 0.2f), -0.35f, 1.4f);
        AddActor("mochi_cat", new Vector3(-2.4f, 0.02f, -0.7f), 0.6f, 1.35f);
        AddActor("pepper_bot", new Vector3(2.4f, 0.02f, -0.7f), -0.6f, 1.35f);
        AddActor("burnt_onion", new Vector3(0.2f, 0.02f, -2.0f), 0f, 1.1f);

        string[] foods = ["pizza", "galaxy_donut", "rainbow_cupcake", "sushi", "taco", "dim_sum", "rendang"];
        for (var i = 0; i < foods.Length; i++)
        {
            var node = ModelLibrary.LoadStatic(Scene, foods[i]);
            node.Scale = new Vector3(1.6f);
            _orbiters.Add((node, 4.2f, 0.12f, 2.6f + ((i % 3) * 0.35f), i * MathF.Tau / foods.Length));
        }

        Camera.Position = new Vector3(0, 2.4f, 7.2f);
        Camera.LookAt(new Vector3(0, 1.1f, 0));
    }

    protected override void OnUpdate(float dt, double total)
    {
        var t = (float)total;
        foreach (var (node, radius, speed, height, phase) in _orbiters)
        {
            var a = phase + (t * speed);
            node.Position = new Vector3(MathF.Cos(a) * radius, height + Bob(total, 1.6f, 0.15f, phase), (MathF.Sin(a) * radius) - 1f);
            node.EulerAngles = new Vector3(0.3f, t * 0.6f, 0);
        }

        // Gentle camera sway so the stage feels alive without spinning anyone's head.
        Camera.Position = new Vector3(MathF.Sin(t * 0.15f) * 0.8f, 2.4f + (MathF.Sin(t * 0.2f) * 0.1f), 7.2f);
        Camera.LookAt(new Vector3(0, 1.1f, 0));

        _cheerTimer -= dt;
        if (_cheerTimer <= 0)
        {
            _cheerTimer = 2.5f;
            var actor = Actors[_cheerIndex++ % Actors.Count];
            if (actor.Model == "burnt_onion")
            {
                actor.Talk();
            }
            else
            {
                actor.Cheer();
            }
        }
    }

    public void CheerHost() => _host.Cheer();
}
