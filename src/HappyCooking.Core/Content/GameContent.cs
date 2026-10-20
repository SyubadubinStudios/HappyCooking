using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using HappyCooking.Core.Localization;

namespace HappyCooking.Core.Content;

/// <summary>
/// All static, data-driven game content. Loaded once from the JSON files embedded
/// in this assembly and immutable afterwards, so it can be shared freely between
/// systems and threads. Adding a recipe, fish or pet is a JSON edit, not a code change.
/// </summary>
public sealed class GameContent
{
    public GameContent(
        IReadOnlyList<Region> regions,
        IReadOnlyList<Ingredient> ingredients,
        IReadOnlyList<Recipe> recipes,
        IReadOnlyList<Character> characters,
        IReadOnlyList<Pet> pets,
        IReadOnlyList<Crop> crops,
        IReadOnlyList<Fish> fish,
        IReadOnlyList<Quest> quests,
        IReadOnlyList<Achievement> achievements,
        IReadOnlyList<Decoration> decorations,
        IReadOnlyList<SeasonalEvent> events)
    {
        Regions = regions;
        Ingredients = ingredients;
        Recipes = recipes;
        Characters = characters;
        Pets = pets;
        Crops = crops;
        Fish = fish;
        Quests = quests;
        Achievements = achievements;
        Decorations = decorations;
        Events = events;

        RegionById = regions.ToFrozenDictionary(r => r.Id, StringComparer.Ordinal);
        IngredientById = ingredients.ToFrozenDictionary(i => i.Id, StringComparer.Ordinal);
        RecipeById = recipes.ToFrozenDictionary(r => r.Id, StringComparer.Ordinal);
        CharacterById = characters.ToFrozenDictionary(c => c.Id, StringComparer.Ordinal);
        PetById = pets.ToFrozenDictionary(p => p.Id, StringComparer.Ordinal);
        CropById = crops.ToFrozenDictionary(c => c.Id, StringComparer.Ordinal);
        FishById = fish.ToFrozenDictionary(f => f.Id, StringComparer.Ordinal);
        QuestById = quests.ToFrozenDictionary(q => q.Id, StringComparer.Ordinal);
        AchievementById = achievements.ToFrozenDictionary(a => a.Id, StringComparer.Ordinal);
        DecorationById = decorations.ToFrozenDictionary(d => d.Id, StringComparer.Ordinal);
        RecipesByRegion = recipes
            .GroupBy(r => r.Region, StringComparer.Ordinal)
            .ToFrozenDictionary(g => g.Key, g => (IReadOnlyList<Recipe>)g.ToArray(), StringComparer.Ordinal);
    }

    public IReadOnlyList<Region> Regions { get; }
    public IReadOnlyList<Ingredient> Ingredients { get; }
    public IReadOnlyList<Recipe> Recipes { get; }
    public IReadOnlyList<Character> Characters { get; }
    public IReadOnlyList<Pet> Pets { get; }
    public IReadOnlyList<Crop> Crops { get; }
    public IReadOnlyList<Fish> Fish { get; }
    public IReadOnlyList<Quest> Quests { get; }
    public IReadOnlyList<Achievement> Achievements { get; }
    public IReadOnlyList<Decoration> Decorations { get; }
    public IReadOnlyList<SeasonalEvent> Events { get; }

    public FrozenDictionary<string, Region> RegionById { get; }
    public FrozenDictionary<string, Ingredient> IngredientById { get; }
    public FrozenDictionary<string, Recipe> RecipeById { get; }
    public FrozenDictionary<string, Character> CharacterById { get; }
    public FrozenDictionary<string, Pet> PetById { get; }
    public FrozenDictionary<string, Crop> CropById { get; }
    public FrozenDictionary<string, Fish> FishById { get; }
    public FrozenDictionary<string, Quest> QuestById { get; }
    public FrozenDictionary<string, Achievement> AchievementById { get; }
    public FrozenDictionary<string, Decoration> DecorationById { get; }
    public FrozenDictionary<string, IReadOnlyList<Recipe>> RecipesByRegion { get; }

    public IReadOnlyList<Recipe> RecipesIn(string regionId) =>
        RecipesByRegion.TryGetValue(regionId, out var list) ? list : [];

    private static readonly Lazy<GameContent> s_default = new(ContentLoader.LoadEmbedded);

    /// <summary>The content shipped with the game, loaded on first use.</summary>
    public static GameContent Default => s_default.Value;

    /// <summary>
    /// Checks every cross reference (recipe → ingredient, region → fish, quest → recipe, ...).
    /// Returns the problems found; an empty list means the content is consistent.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();
        foreach (var recipe in Recipes)
        {
            if (!RegionById.ContainsKey(recipe.Region))
            {
                problems.Add($"recipe {recipe.Id}: unknown region {recipe.Region}");
            }

            if (recipe.Steps.Count == 0)
            {
                problems.Add($"recipe {recipe.Id}: has no steps");
            }

            foreach (var ing in recipe.Ingredients)
            {
                if (!IngredientById.ContainsKey(ing))
                {
                    problems.Add($"recipe {recipe.Id}: unknown ingredient {ing}");
                }
            }

            foreach (var step in recipe.Steps)
            {
                if (step.Item is { } item && !IngredientById.ContainsKey(item))
                {
                    problems.Add($"recipe {recipe.Id}: step {step.Verb} uses unknown ingredient {item}");
                }

                foreach (var pick in step.Items ?? [])
                {
                    if (!IngredientById.ContainsKey(pick))
                    {
                        problems.Add($"recipe {recipe.Id}: pick step lists unknown ingredient {pick}");
                    }
                }

                if (step.Kind is StepKind.Tap or StepKind.Rhythm or StepKind.Decorate && step.Count <= 0)
                {
                    problems.Add($"recipe {recipe.Id}: {step.Kind} step needs a positive count");
                }

                if (step.Kind == StepKind.Pick && (step.Items is null || step.Items.Count == 0))
                {
                    problems.Add($"recipe {recipe.Id}: pick step needs items");
                }
            }
        }

        foreach (var region in Regions)
        {
            if (!CharacterById.ContainsKey(region.Host))
            {
                problems.Add($"region {region.Id}: unknown host {region.Host}");
            }

            foreach (var fishId in region.Fish)
            {
                if (!FishById.ContainsKey(fishId))
                {
                    problems.Add($"region {region.Id}: unknown fish {fishId}");
                }
            }
        }

        foreach (var crop in Crops)
        {
            if (!IngredientById.ContainsKey(crop.Ingredient))
            {
                problems.Add($"crop {crop.Id}: unknown ingredient {crop.Ingredient}");
            }
        }

        foreach (var fish in Fish)
        {
            if (!IngredientById.ContainsKey(fish.Ingredient))
            {
                problems.Add($"fish {fish.Id}: unknown ingredient {fish.Ingredient}");
            }
        }

        foreach (var quest in Quests)
        {
            if (!CharacterById.ContainsKey(quest.Giver))
            {
                problems.Add($"quest {quest.Id}: unknown giver {quest.Giver}");
            }

            if (quest.Requires is { } req && !QuestById.ContainsKey(req))
            {
                problems.Add($"quest {quest.Id}: requires unknown quest {req}");
            }

            var targetOk = quest.Goal switch
            {
                QuestGoal.CookRecipe => quest.Target is { } r && RecipeById.ContainsKey(r),
                QuestGoal.CookInRegion => quest.Target is { } g && RegionById.ContainsKey(g),
                _ => true,
            };
            if (!targetOk)
            {
                problems.Add($"quest {quest.Id}: bad target {quest.Target}");
            }
        }

        foreach (var ev in Events)
        {
            foreach (var r in ev.FeaturedRecipes)
            {
                if (!RecipeById.ContainsKey(r))
                {
                    problems.Add($"event {ev.Id}: unknown recipe {r}");
                }
            }
        }

        return problems;
    }
}

/// <summary>Reads the embedded content JSON with source generated serializers (trim/AOT safe).</summary>
public static class ContentLoader
{
    private const string Prefix = "HappyCooking.Content.";

    public static GameContent LoadEmbedded()
    {
        var ctx = ContentJsonContext.Default;
        return new GameContent(
            Read("regions", ctx.ListRegion),
            Read("ingredients", ctx.ListIngredient),
            Read("recipes", ctx.ListRecipe),
            Read("characters", ctx.ListCharacter),
            Read("pets", ctx.ListPet),
            Read("crops", ctx.ListCrop),
            Read("fish", ctx.ListFish),
            Read("quests", ctx.ListQuest),
            Read("achievements", ctx.ListAchievement),
            Read("decorations", ctx.ListDecoration),
            Read("events", ctx.ListSeasonalEvent));
    }

    public static IReadOnlyDictionary<string, LocalizedText> LoadStrings() =>
        Read("strings", ContentJsonContext.Default.DictionaryStringLocalizedText);

    private static T Read<T>(string name, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> info)
    {
        var asm = typeof(ContentLoader).Assembly;
        using var stream = asm.GetManifestResourceStream(Prefix + name + ".json")
            ?? throw new InvalidOperationException($"Embedded content '{name}.json' is missing.");
        return JsonSerializer.Deserialize(stream, info)
            ?? throw new InvalidOperationException($"Embedded content '{name}.json' is empty.");
    }
}

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(List<Region>))]
[JsonSerializable(typeof(List<Ingredient>))]
[JsonSerializable(typeof(List<Recipe>))]
[JsonSerializable(typeof(List<Character>))]
[JsonSerializable(typeof(List<Pet>))]
[JsonSerializable(typeof(List<Crop>))]
[JsonSerializable(typeof(List<Fish>))]
[JsonSerializable(typeof(List<Quest>))]
[JsonSerializable(typeof(List<Achievement>))]
[JsonSerializable(typeof(List<Decoration>))]
[JsonSerializable(typeof(List<SeasonalEvent>))]
[JsonSerializable(typeof(Dictionary<string, LocalizedText>))]
internal sealed partial class ContentJsonContext : JsonSerializerContext;
