using System.Text.Json.Serialization;
using HappyCooking.Core.Localization;

namespace HappyCooking.Core.State;

/// <summary>
/// Everything that is saved about one player. Plain mutable data with no logic:
/// the systems in <c>HappyCooking.Core.Systems</c> own the rules, this class only
/// holds the numbers. Serialised with <see cref="ProfileJsonContext"/>.
/// </summary>
public sealed class PlayerProfile
{
    /// <summary>Bumped whenever the shape of the save changes; see <see cref="ProfileMigrations"/>.</summary>
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public string PlayerName { get; set; } = "Chef";

    /// <summary>Short shareable code that identifies this player for friends and gifts.</summary>
    public string PlayerCode { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }

    public int Coins { get; set; }

    public int Gems { get; set; }

    public int Level { get; set; } = 1;

    public int Xp { get; set; }

    public Dictionary<string, int> Inventory { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Cooked dishes waiting on the counter to be served, by recipe id.</summary>
    public Dictionary<string, int> Counter { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Best star rating (1-3) per recipe id that has been cooked at least once.</summary>
    public Dictionary<string, int> BestStars { get; set; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> TimesCooked { get; set; } = new(StringComparer.Ordinal);

    public Dictionary<string, PetState> Pets { get; set; } = new(StringComparer.Ordinal);

    public string? ActivePet { get; set; }

    public List<FarmPlot> Farm { get; set; } = [];

    public Dictionary<string, int> FishLog { get; set; } = new(StringComparer.Ordinal);

    public List<string> Decorations { get; set; } = [];

    public Dictionary<string, int> QuestProgress { get; set; } = new(StringComparer.Ordinal);

    public HashSet<string> CompletedQuests { get; set; } = new(StringComparer.Ordinal);

    public HashSet<string> Achievements { get; set; } = new(StringComparer.Ordinal);

    public List<Friend> Friends { get; set; } = [];

    public HashSet<string> RedeemedGifts { get; set; } = new(StringComparer.Ordinal);

    public PlayerStats Stats { get; set; } = new();

    public GameSettings Settings { get; set; } = new();
}

public sealed class PetState
{
    public float Happiness { get; set; } = 80;

    public DateTimeOffset LastFedUtc { get; set; }

    public int TimesFed { get; set; }
}

public sealed class FarmPlot
{
    public string? Crop { get; set; }

    public DateTimeOffset PlantedUtc { get; set; }

    /// <summary>Growth already banked from earlier boosts (rain, pets), in seconds.</summary>
    public float BonusSeconds { get; set; }
}

public sealed class Friend
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset AddedUtc { get; set; }
}

/// <summary>Lifetime counters, used by achievements and the profile screen.</summary>
public sealed class PlayerStats
{
    public int DishesCooked { get; set; }

    public int ThreeStarDishes { get; set; }

    public int TotalStars { get; set; }

    public int CustomersServed { get; set; }

    public int FishCaught { get; set; }

    public int CropsHarvested { get; set; }

    public int CreativeDishes { get; set; }

    public int GiftsSent { get; set; }

    public int CoinsEarned { get; set; }

    public int BurntDishes { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter<GraphicsQuality>))]
public enum GraphicsQuality
{
    Low,
    Medium,
    High,
}

public sealed class GameSettings
{
    public Language Language { get; set; } = Language.English;

    public float MusicVolume { get; set; } = 0.6f;

    public float EffectsVolume { get; set; } = 0.8f;

    public GraphicsQuality Graphics { get; set; } = GraphicsQuality.High;

    /// <summary>Folder that cloud save mirrors to (a OneDrive, Google Drive or Dropbox folder), or null when off.</summary>
    public string? CloudFolder { get; set; }

    public bool ReducedMotion { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PlayerProfile))]
public sealed partial class ProfileJsonContext : JsonSerializerContext;
