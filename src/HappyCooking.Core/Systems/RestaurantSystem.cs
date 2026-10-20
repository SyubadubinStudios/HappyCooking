using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.State;

namespace HappyCooking.Core.Systems;

/// <summary>A guest at one of the tables, waiting for one dish.</summary>
public sealed class Customer
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Which character model represents this guest.</summary>
    public required string Look { get; init; }

    public required Recipe Order { get; init; }

    public required int Seat { get; init; }

    public required float Patience { get; init; }

    public float Waited { get; internal set; }

    public float PatienceLeft => Math.Clamp(1f - (Waited / Patience), 0f, 1f);
}

public readonly record struct ServeResult(int Coins, int Tip);

/// <summary>
/// The player's restaurant: guests arrive, order dishes the player has learned,
/// and pay (plus a tip for quick service) when served from the counter.
/// Decorations raise the rating, which brings guests sooner and adds to tips.
/// </summary>
public sealed class RestaurantSystem(
    GameContent content,
    PlayerProfile profile,
    EconomySystem economy,
    PetSystem pets,
    GameEvents events,
    GameRandom random)
{
    private static readonly string[] s_names =
    [
        "Ayu", "Budi", "Kenji", "Sofia", "Wei", "Diego", "Nadia", "Omar", "Hana", "Leo",
        "Putri", "Raka", "Yuki", "Marco", "Mei", "Lucia", "Tari", "Arjun", "Zoe", "Dimas",
    ];

    private static readonly string[] s_looks = ["coco_panda", "jenggo_chicken", "mochi_cat", "pepper_bot", "chef_happy"];

    private readonly List<Customer> _customers = [];
    private float _spawnTimer = 2f;
    private int _nextId = 1;

    public const int Seats = 4;

    public IReadOnlyList<Customer> Customers => _customers;

    public IReadOnlyDictionary<string, int> Counter => profile.Counter;

    /// <summary>1-5 stars: 1 plus one per four rating points of decoration.</summary>
    public int Rating
    {
        get
        {
            var points = 0;
            foreach (var id in profile.Decorations)
            {
                if (content.DecorationById.TryGetValue(id, out var d))
                {
                    points += d.Rating;
                }
            }

            return Math.Min(5, 1 + (points / 4));
        }
    }

    /// <summary>Raised when a guest gives up waiting.</summary>
    public event Action<Customer>? CustomerLeft;

    public event Action<Customer>? CustomerArrived;

    /// <summary>Recipes guests may order: the ones the player has cooked at least once.</summary>
    public IEnumerable<Recipe> Menu => content.Recipes.Where(r => profile.BestStars.ContainsKey(r.Id));

    public void Update(float deltaSeconds)
    {
        for (var i = _customers.Count - 1; i >= 0; i--)
        {
            var c = _customers[i];
            c.Waited += deltaSeconds;
            if (c.Waited >= c.Patience)
            {
                _customers.RemoveAt(i);
                CustomerLeft?.Invoke(c);
            }
        }

        _spawnTimer -= deltaSeconds;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = random.Range(7f, 12f) - Rating;
            TrySpawn();
        }
    }

    /// <summary>Seats a new guest if a seat is free and there is something on the menu.</summary>
    public Customer? TrySpawn()
    {
        if (_customers.Count >= Seats)
        {
            return null;
        }

        var menu = Menu.ToList();
        if (menu.Count == 0)
        {
            return null;
        }

        // Guests usually order something that is already on the counter, so serving feels snappy.
        var inStock = menu.Where(r => profile.Counter.GetValueOrDefault(r.Id) > 0).ToList();
        var order = inStock.Count > 0 && random.Chance(0.7f) ? random.Pick(inStock) : random.Pick(menu);

        var seat = 0;
        while (_customers.Exists(c => c.Seat == seat))
        {
            seat++;
        }

        var customer = new Customer
        {
            Id = _nextId++,
            Name = random.Pick(s_names),
            Look = random.Pick(s_looks),
            Order = order,
            Seat = seat,
            Patience = 45f * (1f + pets.Bonus(PetBonus.Patience)) + (Rating * 3f),
        };
        _customers.Add(customer);
        CustomerArrived?.Invoke(customer);
        return customer;
    }

    public bool CanServe(Customer customer) => profile.Counter.GetValueOrDefault(customer.Order.Id) > 0;

    public ServeResult? TryServe(Customer customer)
    {
        if (!_customers.Contains(customer) || !CanServe(customer))
        {
            return null;
        }

        var left = profile.Counter[customer.Order.Id] - 1;
        if (left == 0)
        {
            profile.Counter.Remove(customer.Order.Id);
        }
        else
        {
            profile.Counter[customer.Order.Id] = left;
        }

        _customers.Remove(customer);
        var price = Math.Max(5, customer.Order.Coins / 2);
        var tip = (int)MathF.Round(price * 0.5f * customer.PatienceLeft * (1f + pets.Bonus(PetBonus.Tips)) * (1f + (Rating * 0.1f)));
        economy.AddCoins(price + tip);
        economy.AddXp(3);
        profile.Stats.CustomersServed++;
        events.Raise(new GameEvent(GameEventKind.CustomerServed, customer.Order.Id));
        return new ServeResult(price, tip);
    }

    public bool Owns(string decorationId) => profile.Decorations.Contains(decorationId);

    public bool TryBuyDecoration(string decorationId)
    {
        if (Owns(decorationId) || !content.DecorationById.TryGetValue(decorationId, out var deco) ||
            !economy.TrySpendCoins(deco.Price))
        {
            return false;
        }

        profile.Decorations.Add(decorationId);
        events.Raise(new GameEvent(GameEventKind.DecorationPlaced, decorationId));
        return true;
    }
}
