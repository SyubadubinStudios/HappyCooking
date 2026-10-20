using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.State;
using HappyCooking.Core.Systems;

namespace HappyCooking.Core;

/// <summary>
/// One running game: the content, the player's profile and every gameplay system,
/// wired to a shared event hub. The UI talks to the game only through this object.
/// </summary>
public sealed class GameSession
{
    public GameSession(GameContent content, PlayerProfile profile, IClock? clock = null, int? seed = null)
    {
        Content = content;
        Profile = profile;
        Clock = clock ?? SystemClock.Instance;
        Random = seed is { } s ? new GameRandom(s) : new GameRandom();
        Events = new GameEvents();

        World = new WorldSystem(Clock);
        Seasonal = new SeasonalSystem(content, Clock);
        Economy = new EconomySystem(content, profile, Events);
        Inventory = new InventorySystem(content, profile, Economy);
        Pets = new PetSystem(content, profile, Economy, Events, Clock);
        Kitchen = new KitchenSystem(content, profile, Economy, Inventory, Pets, Seasonal, Events, Random);
        Farm = new FarmSystem(content, profile, Economy, Inventory, Pets, World, Events, Clock);
        Fishing = new FishingSystem(content, profile, Economy, Inventory, Pets, World, Events, Random);
        Restaurant = new RestaurantSystem(content, profile, Economy, Pets, Events, Random);
        Quests = new QuestSystem(content, profile, Economy, Events);
        Achievements = new AchievementSystem(content, profile, Economy, Events);
        Social = new SocialSystem(content, profile, Economy, Inventory, Events, Clock, Random);

        Farm.EnsurePlots();
        if (string.IsNullOrEmpty(profile.PlayerCode))
        {
            profile.PlayerCode = SocialSystem.NewPlayerCode(Random);
        }

        // A save from an older build may already meet thresholds added since.
        Achievements.CheckAll();
    }

    public GameContent Content { get; }

    public PlayerProfile Profile { get; }

    public IClock Clock { get; }

    public GameRandom Random { get; }

    public GameEvents Events { get; }

    public WorldSystem World { get; }

    public SeasonalSystem Seasonal { get; }

    public EconomySystem Economy { get; }

    public InventorySystem Inventory { get; }

    public PetSystem Pets { get; }

    public KitchenSystem Kitchen { get; }

    public FarmSystem Farm { get; }

    public FishingSystem Fishing { get; }

    public RestaurantSystem Restaurant { get; }

    public QuestSystem Quests { get; }

    public AchievementSystem Achievements { get; }

    public SocialSystem Social { get; }

    /// <summary>Advances everything that runs in real time. Call once per frame or tick.</summary>
    public void Update(float deltaSeconds)
    {
        Restaurant.Update(deltaSeconds);
        Farm.Update(deltaSeconds);
    }

    /// <summary>A fresh profile with a starter pantry, enough to cook every level-1 dish a few times.</summary>
    public static PlayerProfile NewProfile(GameContent content, string playerName, IClock? clock = null)
    {
        var now = (clock ?? SystemClock.Instance).UtcNow;
        var profile = new PlayerProfile
        {
            PlayerName = string.IsNullOrWhiteSpace(playerName) ? "Chef" : playerName.Trim(),
            CreatedUtc = now,
            UpdatedUtc = now,
            Coins = 200,
            Gems = 10,
        };

        foreach (var ing in content.Ingredients)
        {
            profile.Inventory[ing.Id] = ing.Source switch
            {
                IngredientSource.Market => 3,
                IngredientSource.Farm => 4,
                _ => ing.Id is "mackerel" or "shrimp" ? 2 : 0,
            };
        }

        foreach (var key in profile.Inventory.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList())
        {
            profile.Inventory.Remove(key);
        }

        return profile;
    }
}
