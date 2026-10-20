using System.Numerics;
using HappyCooking.Core.Content;
using ThreeNet;

namespace HappyCooking.App.Rendering;

/// <summary>
/// The dining room: four tables, a serving counter with today's dishes, the
/// player's decorations, and guests who appear at their seats and cheer when served.
/// </summary>
public sealed class RestaurantStage : Stage
{
    public static readonly Vector3[] SeatPositions =
    [
        new(-2.2f, 0, 0.2f),
        new(2.2f, 0, 0.2f),
        new(-2.2f, 0, 2.6f),
        new(2.2f, 0, 2.6f),
    ];

    private static readonly Vector3[] DecorSpots =
    [
        new(-3.6f, 0, -2.2f), new(3.6f, 0, -2.2f), new(-3.7f, 0, 3.8f), new(-3.7f, 0, 1.4f),
        new(3.7f, 0, 1.4f), new(-3.2f, 0, -0.9f), new(3.7f, 0, 3.8f), new(3.2f, 0, -0.9f),
    ];

    private readonly Dictionary<int, CharacterActor> _guests = [];
    private readonly Dictionary<string, Node> _decor = new(StringComparer.Ordinal);
    private readonly List<Node> _counterDishes = [];
    private readonly ParticleFx? _sparkles;

    public RestaurantStage()
    {
        IndoorEnvironment("#FFE3EC");
        AddLights(new Vector3(0.3f, 1f, 0.7f), 2.8f);

        Props.CheckerFloor(16, 0.6f, "#FFF4D6", "#FFB4C8");
        Props.Box(new Vector3(0, 1.7f, -3.0f), new Vector3(9.6f, 3.4f, 0.12f), "#FFFFFF");
        Props.Box(new Vector3(0, 0.55f, -2.92f), new Vector3(9.6f, 1.1f, 0.04f), "#FF8FB1");
        Props.Box(new Vector3(-4.8f, 1.7f, 0.6f), new Vector3(0.12f, 3.4f, 7.2f), "#FFFFFF");
        Props.Box(new Vector3(4.8f, 1.7f, 0.6f), new Vector3(0.12f, 3.4f, 7.2f), "#FFFFFF");
        // Striped awning over the serving counter.
        for (var i = 0; i < 9; i++)
        {
            Props.Box(new Vector3(-2.4f + (i * 0.6f), 2.75f, -2.55f), new Vector3(0.6f, 0.08f, 0.9f), i % 2 == 0 ? "#FF5A4E" : "#FFFFFF");
        }

        // Serving counter.
        Props.Box(new Vector3(0, 0.5f, -2.2f), new Vector3(5.2f, 1.0f, 0.8f), "#FF5A4E");
        Props.Box(new Vector3(0, 1.03f, -2.2f), new Vector3(5.4f, 0.07f, 0.95f), "#FFF4D6", roughness: 0.3f);

        foreach (var seat in SeatPositions)
        {
            var tableAt = seat + new Vector3(seat.X < 0 ? 1.0f : -1.0f, 0, 0);
            Props.Table(tableAt, "#FFFFFF", "#C98745");
            Props.Chair(seat, seat.X < 0 ? MathF.PI / 2 : -MathF.PI / 2, "#7FD1FF");
            Props.Plate(tableAt + new Vector3(0, 0.77f, 0));
        }

        _sparkles = TryAddEffect(() => ParticleFx.Sparkles(Scene, Vector3.Zero));

        Camera.Position = new Vector3(0, 5.2f, 8.4f);
        Camera.LookAt(new Vector3(0, 0.6f, 0.2f));
    }

    public void SyncDecorations(IEnumerable<Decoration> owned)
    {
        var i = 0;
        foreach (var deco in owned)
        {
            if (!_decor.ContainsKey(deco.Id))
            {
                var node = Props.Decoration(deco.Shape, deco.Color, DecorSpots[i % DecorSpots.Length]);
                node.SetShadowsRecursive(true, true);
                _decor[deco.Id] = node;
            }

            i++;
        }
    }

    /// <summary>Shows up to eight dishes from the counter in a row.</summary>
    public void SyncCounter(IReadOnlyDictionary<string, int> counter, GameContent content)
    {
        foreach (var n in _counterDishes)
        {
            n.Remove();
        }

        _counterDishes.Clear();
        var x = -2.2f;
        foreach (var (id, count) in counter)
        {
            for (var k = 0; k < count && _counterDishes.Count < 8; k++)
            {
                var recipe = content.RecipeById[id];
                Node dish;
                if (recipe.Model is { } m && ModelLibrary.Exists(m))
                {
                    dish = ModelLibrary.LoadStatic(Scene, m);
                    dish.Scale = new Vector3(0.75f);
                }
                else
                {
                    dish = Props.GenericDish(recipe.Color, seed: id.Length + k);
                }

                dish.Position = new Vector3(x, 1.07f, -2.2f);
                _counterDishes.Add(dish);
                x += 0.62f;
            }
        }
    }

    public void AddGuest(int id, string look, int seat)
    {
        if (_guests.ContainsKey(id))
        {
            return;
        }

        var pos = SeatPositions[seat % SeatPositions.Length];
        var actor = AddActor(look, pos + new Vector3(0, 0.05f, 0), pos.X < 0 ? MathF.PI / 2 : -MathF.PI / 2, 1.05f);
        _guests[id] = actor;
    }

    public void ServeGuest(int id)
    {
        if (_guests.TryGetValue(id, out var actor))
        {
            actor.Cheer();
            if (_sparkles is not null)
            {
                _sparkles.Effect.Origin = actor.Root.WorldPosition + new Vector3(0, 1.1f, 0);
                _sparkles.Effect.Burst(80);
            }
        }
    }

    public void RemoveGuest(int id)
    {
        if (_guests.Remove(id, out var actor))
        {
            RemoveActor(actor);
        }
    }

    public IEnumerable<int> GuestIds => _guests.Keys;

    protected override void OnUpdate(float dt, double total)
    {
        for (var i = 0; i < _counterDishes.Count; i++)
        {
            var d = _counterDishes[i];
            d.EulerAngles = new Vector3(0, (float)total * 0.4f + i, 0);
        }
    }
}
