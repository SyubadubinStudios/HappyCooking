using System.Text.Json.Serialization;
using HappyCooking.Core.Localization;

namespace HappyCooking.Core.Content;

/// <summary>A culinary island on the world map.</summary>
public sealed record Region(
    string Id,
    LocalizedText Name,
    LocalizedText Description,
    int UnlockLevel,
    string Color,
    string Host,
    float MapX,
    float MapY,
    IReadOnlyList<string> Fish);

/// <summary>Where an ingredient normally comes from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<IngredientSource>))]
public enum IngredientSource
{
    Market,
    Farm,
    Sea,
}

public sealed record Ingredient(
    string Id,
    LocalizedText Name,
    string Color,
    IngredientSource Source,
    int Price);

/// <summary>The mini-game a cooking step plays.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StepKind>))]
public enum StepKind
{
    /// <summary>Tap a number of times as quickly as possible (chop, knead, whisk).</summary>
    Tap,

    /// <summary>A needle sweeps up; stop it inside the green zone (fry, bake, boil).</summary>
    Heat,

    /// <summary>Hold to pour or sprinkle; let go when the meter is in the zone (season, sauce).</summary>
    Pour,

    /// <summary>A marker bounces left and right; tap while it is in the zone, several times (stir, flip).</summary>
    Rhythm,

    /// <summary>Pick the right ingredients from a tray that also holds decoys.</summary>
    Pick,

    /// <summary>Place toppings on the dish (plate, decorate).</summary>
    Decorate,
}

/// <summary>
/// One step of a recipe. Which fields matter depends on <see cref="Kind"/>:
/// <c>Count</c> for Tap/Rhythm/Decorate, <c>Target</c>/<c>Zone</c>/<c>Speed</c>
/// for Heat/Pour/Rhythm, <c>Items</c> (+ decoys from the region) for Pick.
/// </summary>
public sealed record RecipeStep(
    StepKind Kind,
    string Verb,
    string? Item = null,
    int Count = 0,
    float Target = 0.7f,
    float Zone = 0.14f,
    float Speed = 0.35f,
    IReadOnlyList<string>? Items = null);

public sealed record Recipe(
    string Id,
    LocalizedText Name,
    string Region,
    LocalizedText Description,
    LocalizedText FunFact,
    int Difficulty,
    int UnlockLevel,
    int Coins,
    int Xp,
    string Color,
    string? Model,
    bool Creative,
    IReadOnlyList<string> Ingredients,
    IReadOnlyList<RecipeStep> Steps);

/// <summary>A story character (chef, helper or the comic villain).</summary>
public sealed record Character(
    string Id,
    LocalizedText Name,
    LocalizedText Role,
    LocalizedText Greeting,
    string Model,
    string Color);

[JsonConverter(typeof(JsonStringEnumConverter<PetBonus>))]
public enum PetBonus
{
    /// <summary>Customers tip more.</summary>
    Tips,

    /// <summary>Heat and pour zones are wider.</summary>
    CookingZone,

    /// <summary>Crops grow faster.</summary>
    Growth,

    /// <summary>Rare fish bite more often.</summary>
    FishLuck,

    /// <summary>More XP from cooking.</summary>
    Xp,

    /// <summary>Customers wait longer.</summary>
    Patience,
}

public sealed record Pet(
    string Id,
    LocalizedText Name,
    LocalizedText Description,
    PetBonus Bonus,
    float Amount,
    int Price,
    bool PriceInGems,
    int UnlockLevel,
    string Color);

public sealed record Crop(
    string Id,
    string Ingredient,
    int SeedPrice,
    int GrowSeconds,
    int Yield,
    int Xp,
    string Color);

[JsonConverter(typeof(JsonStringEnumConverter<Rarity>))]
public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Legendary,
}

public sealed record Fish(
    string Id,
    LocalizedText Name,
    string Ingredient,
    Rarity Rarity,
    bool NightOnly,
    int Coins,
    string Color);

[JsonConverter(typeof(JsonStringEnumConverter<QuestGoal>))]
public enum QuestGoal
{
    CookRecipe,
    CookInRegion,
    CookAny,
    EarnStars,
    ServeCustomers,
    Harvest,
    CatchFish,
    FeedPet,
    AdoptPet,
    PlaceDecoration,
    ReachLevel,
}

public sealed record Quest(
    string Id,
    LocalizedText Title,
    LocalizedText Description,
    string Giver,
    QuestGoal Goal,
    string? Target,
    int Amount,
    int RewardCoins,
    int RewardXp,
    int RewardGems,
    string? Requires);

[JsonConverter(typeof(JsonStringEnumConverter<AchievementStat>))]
public enum AchievementStat
{
    DishesCooked,
    ThreeStarDishes,
    CustomersServed,
    FishCaught,
    CropsHarvested,
    PetsAdopted,
    RegionsUnlocked,
    RecipesDiscovered,
    CreativeDishes,
    DecorationsPlaced,
    GiftsSent,
    Level,
}

public sealed record Achievement(
    string Id,
    LocalizedText Title,
    LocalizedText Description,
    AchievementStat Stat,
    int Threshold,
    int RewardGems,
    string Icon);

public sealed record Decoration(
    string Id,
    LocalizedText Name,
    int Price,
    int Rating,
    string Shape,
    string Color);

public sealed record SeasonalEvent(
    string Id,
    LocalizedText Name,
    LocalizedText Description,
    int StartMonth,
    int StartDay,
    int EndMonth,
    int EndDay,
    float CoinMultiplier,
    IReadOnlyList<string> FeaturedRecipes,
    string Color);
