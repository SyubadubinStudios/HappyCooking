using System.Globalization;
using System.Numerics;
using ThreeNet;

namespace HappyCooking.App.Rendering;

/// <summary>
/// Procedural set dressing built from primitives: counters, tables, plates,
/// trees, plots. Geometry and materials are cached per scene, so a room full of
/// chairs shares one cylinder and one wood material on the GPU.
/// </summary>
public sealed class Props(Scene scene)
{
    private readonly Dictionary<string, Material> _materials = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Geometry> _geometry = new(StringComparer.Ordinal);

    public Scene Scene { get; } = scene;

    /// <summary>"#RRGGBB" (sRGB, as designers write it) to linear RGBA for the renderer.</summary>
    public static Vector4 Hex(string hex, float alpha = 1f)
    {
        var v = uint.Parse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new Vector4(
            MathHelpers.SrgbToLinear(((v >> 16) & 0xFF) / 255f),
            MathHelpers.SrgbToLinear(((v >> 8) & 0xFF) / 255f),
            MathHelpers.SrgbToLinear((v & 0xFF) / 255f),
            alpha);
    }

    public static Vector3 Rgb(string hex)
    {
        var c = Hex(hex);
        return new Vector3(c.X, c.Y, c.Z);
    }

    /// <summary>A soft, slightly glossy "clay toy" material, the house style for props.</summary>
    public Material Mat(string hex, float roughness = 0.62f, float metallic = 0f)
    {
        var key = string.Create(CultureInfo.InvariantCulture, $"{hex}|{roughness}|{metallic}");
        if (!_materials.TryGetValue(key, out var mat))
        {
            var o = MaterialOptions.Pbr(Hex(hex), metallic, roughness);
            o.Clearcoat = 0.25f;
            o.ClearcoatRoughness = 0.4f;
            mat = Scene.CreateMaterial(in o);
            _materials[key] = mat;
        }

        return mat;
    }

    public Material Glow(string hex, float intensity = 2f)
    {
        var key = "glow|" + hex;
        if (!_materials.TryGetValue(key, out var mat))
        {
            var o = MaterialOptions.Pbr(Hex(hex), 0f, 0.5f);
            o.Emissive = Rgb(hex);
            o.EmissiveIntensity = intensity;
            mat = Scene.CreateMaterial(in o);
            _materials[key] = mat;
        }

        return mat;
    }

    public Material Glass(string hex)
    {
        var key = "glass|" + hex;
        if (!_materials.TryGetValue(key, out var mat))
        {
            var o = MaterialOptions.Pbr(Hex(hex, 0.35f), 0f, 0.05f);
            o.AlphaMode = AlphaMode.Blend;
            o.Transmission = 0.6f;
            o.Ior = 1.33f;
            mat = Scene.CreateMaterial(in o);
            _materials[key] = mat;
        }

        return mat;
    }

    private Geometry Geo(string key, Func<Geometry> create)
    {
        if (!_geometry.TryGetValue(key, out var g))
        {
            g = create();
            _geometry[key] = g;
        }

        return g;
    }

    public Geometry UnitBox => Geo("box", () => Scene.CreateBoxGeometry(1, 1, 1, 1));

    public Geometry UnitSphere => Geo("sphere", () => Scene.CreateSphereGeometry(0.5f, 24, 16));

    public Geometry UnitCylinder => Geo("cyl", () => Scene.CreateCylinderGeometry(0.5f, 0.5f, 1f, 28));

    public Geometry UnitCone => Geo("cone", () => Scene.CreateConeGeometry(0.5f, 1f, 24));

    public Geometry Torus => Geo("torus", () => Scene.CreateTorusGeometry(0.5f, 0.18f, 16, 40));

    public Geometry Capsule => Geo("capsule", () => Scene.CreateCapsuleGeometry(0.5f, 1f, 16, 6));

    public Node Box(Vector3 position, Vector3 size, string hex, Node? parent = null, float roughness = 0.62f)
    {
        var n = Scene.AddMesh(UnitBox, Mat(hex, roughness), parent);
        n.Position = position;
        n.Scale = size;
        return n;
    }

    public Node Sphere(Vector3 position, Vector3 size, string hex, Node? parent = null)
    {
        var n = Scene.AddMesh(UnitSphere, Mat(hex), parent);
        n.Position = position;
        n.Scale = size;
        return n;
    }

    public Node Cylinder(Vector3 position, float radius, float height, string hex, Node? parent = null)
    {
        var n = Scene.AddMesh(UnitCylinder, Mat(hex), parent);
        n.Position = position;
        n.Scale = new Vector3(radius * 2, height, radius * 2);
        return n;
    }

    public Node Cone(Vector3 position, float radius, float height, string hex, Node? parent = null)
    {
        var n = Scene.AddMesh(UnitCone, Mat(hex), parent);
        n.Position = position;
        n.Scale = new Vector3(radius * 2, height, radius * 2);
        return n;
    }

    /// <summary>A checkerboard floor drawn as two instanced tile meshes (two draw calls for any size).</summary>
    public Node CheckerFloor(int tiles, float tileSize, string a, string b, float y = 0f)
    {
        var root = Scene.CreateNode(null, "floor");
        var even = new List<Matrix4x4>();
        var odd = new List<Matrix4x4>();
        var half = tiles * tileSize / 2f;
        for (var x = 0; x < tiles; x++)
        {
            for (var z = 0; z < tiles; z++)
            {
                var m = Matrix4x4.CreateScale(tileSize * 0.98f, 0.06f, tileSize * 0.98f) *
                        Matrix4x4.CreateTranslation(-half + ((x + 0.5f) * tileSize), y - 0.03f, -half + ((z + 0.5f) * tileSize));
                ((x + z) % 2 == 0 ? even : odd).Add(m);
            }
        }

        var n1 = Scene.AddMesh(UnitBox, Mat(a, 0.4f), root);
        n1.SetInstances(even.ToArray());
        var n2 = Scene.AddMesh(UnitBox, Mat(b, 0.4f), root);
        n2.SetInstances(odd.ToArray());
        Box(new Vector3(0, y - 0.09f, 0), new Vector3(tiles * tileSize, 0.1f, tiles * tileSize), "#2B2A6B", root);
        return root;
    }

    public Node Table(Vector3 position, string top = "#FFFFFF", string leg = "#C98745")
    {
        var root = Scene.CreateNode(null, "table");
        root.Position = position;
        Cylinder(new Vector3(0, 0.72f, 0), 0.62f, 0.07f, top, root);
        Cylinder(new Vector3(0, 0.36f, 0), 0.07f, 0.72f, leg, root);
        Cylinder(new Vector3(0, 0.02f, 0), 0.3f, 0.04f, leg, root);
        return root;
    }

    public Node Chair(Vector3 position, float yaw, string seat)
    {
        var root = Scene.CreateNode(null, "chair");
        root.Position = position;
        root.EulerAngles = new Vector3(0, yaw, 0);
        Cylinder(new Vector3(0, 0.42f, 0), 0.24f, 0.08f, seat, root);
        Box(new Vector3(0, 0.7f, -0.2f), new Vector3(0.44f, 0.5f, 0.07f), seat, root);
        foreach (var (x, z) in new[] { (-0.16f, -0.16f), (0.16f, -0.16f), (-0.16f, 0.16f), (0.16f, 0.16f) })
        {
            Cylinder(new Vector3(x, 0.2f, z), 0.03f, 0.4f, "#8C5A3C", root);
        }

        return root;
    }

    public Node Plate(Vector3 position, Node? parent = null, float radius = 0.22f)
    {
        var root = Scene.CreateNode(parent, "plate");
        root.Position = position;
        var n = Scene.AddMesh(UnitCylinder, Mat("#FFFFFF", 0.25f), root);
        n.Scale = new Vector3(radius * 2, 0.025f, radius * 2);
        var rim = Scene.AddMesh(Torus, Mat("#7FD1FF", 0.3f), root);
        rim.Scale = new Vector3(radius * 1.85f, radius * 1.85f, radius * 0.6f);
        rim.EulerAngles = new Vector3(MathF.PI / 2, 0, 0);
        rim.Position = new Vector3(0, 0.02f, 0);
        return root;
    }

    /// <summary>A stylised dish for recipes without a sculpted model: plate, a mound in the recipe colour and garnish.</summary>
    public Node GenericDish(string color, Node? parent = null, int seed = 1)
    {
        var root = Scene.CreateNode(parent, "dish");
        Plate(Vector3.Zero, root, 0.2f);
        var mound = Scene.AddMesh(UnitSphere, Mat(color, 0.45f), root);
        mound.Scale = new Vector3(0.3f, 0.14f, 0.3f);
        mound.Position = new Vector3(0, 0.05f, 0);
        var rng = new Random(seed);
        string[] garnish = ["#5DBB4A", "#FFD45C", "#FF5A4E", "#FFFFFF"];
        for (var i = 0; i < 5; i++)
        {
            var a = rng.NextSingle() * MathF.Tau;
            var r = 0.05f + (rng.NextSingle() * 0.07f);
            var g = Scene.AddMesh(UnitSphere, Mat(garnish[rng.Next(garnish.Length)], 0.4f), root);
            g.Scale = new Vector3(0.035f);
            g.Position = new Vector3(MathF.Cos(a) * r, 0.105f + (rng.NextSingle() * 0.02f), MathF.Sin(a) * r);
        }

        return root;
    }

    public Node Tree(Vector3 position, float scale = 1f, string leaves = "#3FB37F")
    {
        var root = Scene.CreateNode(null, "tree");
        root.Position = position;
        root.Scale = new Vector3(scale);
        Cylinder(new Vector3(0, 0.45f, 0), 0.09f, 0.9f, "#8C5A3C", root);
        Sphere(new Vector3(0, 1.05f, 0), new Vector3(0.75f, 0.65f, 0.75f), leaves, root);
        Sphere(new Vector3(0.22f, 1.3f, 0.1f), new Vector3(0.45f), leaves, root);
        Sphere(new Vector3(-0.2f, 1.25f, -0.12f), new Vector3(0.5f), leaves, root);
        return root;
    }

    public Node Palm(Vector3 position, float scale = 1f)
    {
        var root = Scene.CreateNode(null, "palm");
        root.Position = position;
        root.Scale = new Vector3(scale);
        for (var i = 0; i < 6; i++)
        {
            Cylinder(new Vector3(i * 0.03f, 0.2f + (i * 0.32f), 0), 0.08f - (i * 0.006f), 0.34f, i % 2 == 0 ? "#B07A4A" : "#9C6B3E", root);
        }

        for (var i = 0; i < 6; i++)
        {
            var leaf = Scene.AddMesh(UnitSphere, Mat("#3C9A3C"), root);
            var a = i * MathF.Tau / 6f;
            leaf.Position = new Vector3(0.15f + (MathF.Cos(a) * 0.45f), 2.05f, MathF.Sin(a) * 0.45f);
            leaf.Scale = new Vector3(0.9f, 0.08f, 0.28f);
            leaf.EulerAngles = new Vector3(0, -a, -0.35f);
        }

        return root;
    }

    /// <summary>A decoration for the restaurant, built from its content "shape" name.</summary>
    public Node Decoration(string shape, string color, Vector3 position)
    {
        var root = Scene.CreateNode(null, "decor-" + shape);
        root.Position = position;
        switch (shape)
        {
            case "pot":
                Cylinder(new Vector3(0, 0.18f, 0), 0.18f, 0.36f, "#C9824F", root);
                for (var i = 0; i < 5; i++)
                {
                    var a = i * MathF.Tau / 5f;
                    Sphere(new Vector3(MathF.Cos(a) * 0.1f, 0.48f, MathF.Sin(a) * 0.1f), new Vector3(0.14f), color, root);
                }

                Sphere(new Vector3(0, 0.5f, 0), new Vector3(0.1f), "#FFD45C", root);
                break;
            case "lantern":
                Cylinder(new Vector3(0, 2.1f, 0), 0.01f, 0.5f, "#2B2A6B", root);
                var lamp = Scene.AddMesh(UnitSphere, Glow(color, 1.5f), root);
                lamp.Position = new Vector3(0, 1.7f, 0);
                lamp.Scale = new Vector3(0.42f, 0.5f, 0.42f);
                Cylinder(new Vector3(0, 1.97f, 0), 0.1f, 0.05f, "#FFD45C", root);
                Cylinder(new Vector3(0, 1.43f, 0), 0.1f, 0.05f, "#FFD45C", root);
                break;
            case "plant":
                Cylinder(new Vector3(0, 0.2f, 0), 0.2f, 0.4f, "#FFFFFF", root);
                for (var i = 0; i < 7; i++)
                {
                    var a = i * MathF.Tau / 7f;
                    var leaf = Scene.AddMesh(UnitSphere, Mat(color), root);
                    leaf.Position = new Vector3(MathF.Cos(a) * 0.22f, 0.75f + ((i % 3) * 0.12f), MathF.Sin(a) * 0.22f);
                    leaf.Scale = new Vector3(0.36f, 0.06f, 0.24f);
                    leaf.EulerAngles = new Vector3(0.5f, -a, 0);
                }

                break;
            case "balloons":
                string[] colors = ["#FF5A4E", "#FFD45C", "#7FD1FF", "#5FD3A6", "#B57BFF"];
                for (var i = 0; i < colors.Length; i++)
                {
                    var x = (i - 2) * 0.16f;
                    var h = 1.3f + ((i % 2) * 0.25f);
                    Cylinder(new Vector3(x * 0.5f, h / 2f, 0), 0.006f, h, "#FFFFFF", root);
                    Sphere(new Vector3(x, h + 0.15f, 0), new Vector3(0.26f, 0.32f, 0.26f), colors[i], root);
                }

                break;
            case "fountain":
                Cylinder(new Vector3(0, 0.1f, 0), 0.45f, 0.2f, "#FFFFFF", root);
                Cylinder(new Vector3(0, 0.4f, 0), 0.05f, 0.6f, "#FFD45C", root);
                Cylinder(new Vector3(0, 0.55f, 0), 0.28f, 0.08f, color, root);
                Cylinder(new Vector3(0, 0.8f, 0), 0.16f, 0.08f, color, root);
                Sphere(new Vector3(0, 0.95f, 0), new Vector3(0.14f), color, root);
                break;
            case "aquarium":
                Box(new Vector3(0, 0.35f, 0), new Vector3(0.9f, 0.7f, 0.25f), "#8C5A3C", root);
                var water = Scene.AddMesh(UnitBox, Glass(color), root);
                water.Position = new Vector3(0, 0.95f, 0);
                water.Scale = new Vector3(0.85f, 0.5f, 0.22f);
                Sphere(new Vector3(-0.15f, 0.95f, 0), new Vector3(0.12f, 0.08f, 0.04f), "#FF8C69", root);
                Sphere(new Vector3(0.18f, 1.05f, 0), new Vector3(0.1f, 0.07f, 0.04f), "#FFD45C", root);
                break;
            case "rug":
                Cylinder(new Vector3(0, 0.01f, 0), 1.0f, 0.02f, color, root);
                Cylinder(new Vector3(0, 0.02f, 0), 0.7f, 0.02f, "#FFD45C", root);
                Cylinder(new Vector3(0, 0.03f, 0), 0.4f, 0.02f, color, root);
                break;
            case "piano":
                Box(new Vector3(0, 0.55f, 0), new Vector3(1.0f, 0.5f, 0.45f), color, root);
                Box(new Vector3(0, 0.82f, 0.12f), new Vector3(0.95f, 0.04f, 0.18f), "#FFFFFF", root);
                Box(new Vector3(0, 1.0f, -0.18f), new Vector3(1.0f, 0.4f, 0.08f), color, root);
                foreach (var x in new[] { -0.45f, 0.45f })
                {
                    Cylinder(new Vector3(x, 0.15f, 0), 0.04f, 0.3f, color, root);
                }

                break;
            default:
                Sphere(new Vector3(0, 0.3f, 0), new Vector3(0.4f), color, root);
                break;
        }

        return root;
    }
}
