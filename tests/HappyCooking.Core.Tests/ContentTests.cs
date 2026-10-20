using HappyCooking.Core.Content;
using HappyCooking.Core.Services;
using HappyCooking.Core.Systems;

namespace HappyCooking.Core.Tests;

public class ContentTests
{
    private static GameContent C => GameContent.Default;

    [Fact]
    public void EmbeddedContent_Loads_AllCollectionsNonEmpty()
    {
        Assert.NotEmpty(C.Regions);
        Assert.NotEmpty(C.Ingredients);
        Assert.NotEmpty(C.Recipes);
        Assert.NotEmpty(C.Characters);
        Assert.NotEmpty(C.Pets);
        Assert.NotEmpty(C.Crops);
        Assert.NotEmpty(C.Fish);
        Assert.NotEmpty(C.Quests);
        Assert.NotEmpty(C.Achievements);
        Assert.NotEmpty(C.Decorations);
        Assert.NotEmpty(C.Events);
    }

    [Fact]
    public void Default_IsCachedSingleton() => Assert.Same(GameContent.Default, GameContent.Default);

    [Fact]
    public void Validate_ReportsNoProblems()
    {
        var problems = C.Validate();
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void Strings_Load() => Assert.NotNull(ContentLoader.LoadStrings());

    [Fact]
    public void EveryRegion_HasRecipesAndFish()
    {
        foreach (var region in C.Regions)
        {
            Assert.True(C.RecipesIn(region.Id).Count > 0, $"region {region.Id} has no recipes");
            Assert.NotEmpty(region.Fish);
        }
    }

    [Fact]
    public void FirstRegion_UnlocksAtLevelOne_WithLevelOneRecipes()
    {
        Assert.Contains(C.Regions, r => r.UnlockLevel == 1);
        Assert.Contains(C.Recipes, r => r.UnlockLevel == 1 && C.RegionById[r.Region].UnlockLevel == 1);
    }

    [Fact]
    public void RecipesIn_UnknownRegion_IsEmpty() => Assert.Empty(C.RecipesIn("atlantis"));

    [Fact]
    public void Ids_AreUniqueInEveryCollection()
    {
        AssertUnique("region", C.Regions.Select(x => x.Id));
        AssertUnique("ingredient", C.Ingredients.Select(x => x.Id));
        AssertUnique("recipe", C.Recipes.Select(x => x.Id));
        AssertUnique("character", C.Characters.Select(x => x.Id));
        AssertUnique("pet", C.Pets.Select(x => x.Id));
        AssertUnique("crop", C.Crops.Select(x => x.Id));
        AssertUnique("fish", C.Fish.Select(x => x.Id));
        AssertUnique("quest", C.Quests.Select(x => x.Id));
        AssertUnique("achievement", C.Achievements.Select(x => x.Id));
        AssertUnique("decoration", C.Decorations.Select(x => x.Id));
        AssertUnique("event", C.Events.Select(x => x.Id));
    }

    private static void AssertUnique(string what, IEnumerable<string> ids)
    {
        var dupes = ids.GroupBy(i => i, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.True(dupes.Count == 0, $"duplicate {what} ids: {string.Join(", ", dupes)}");
    }

    [Fact]
    public void EveryRecipeStep_HasTheParametersItsKindNeeds()
    {
        var problems = new List<string>();
        foreach (var recipe in C.Recipes)
        {
            Assert.NotEmpty(recipe.Ingredients);
            foreach (var step in recipe.Steps)
            {
                var where = $"{recipe.Id}/{step.Verb}";
                Assert.False(string.IsNullOrWhiteSpace(step.Verb), $"{recipe.Id} has a step without a verb");
                switch (step.Kind)
                {
                    case StepKind.Tap or StepKind.Decorate:
                        if (step.Count <= 0) problems.Add($"{where}: count");
                        break;
                    case StepKind.Rhythm:
                        if (step.Count <= 0) problems.Add($"{where}: count");
                        if (step.Zone <= 0 || step.Zone >= 1) problems.Add($"{where}: zone");
                        if (step.Speed <= 0) problems.Add($"{where}: speed");
                        break;
                    case StepKind.Heat or StepKind.Pour:
                        if (step.Target <= 0 || step.Target >= 1) problems.Add($"{where}: target");
                        if (step.Zone <= 0 || step.Zone >= 1) problems.Add($"{where}: zone");
                        if (step.Kind == StepKind.Heat && step.Speed <= 0) problems.Add($"{where}: speed");
                        break;
                    case StepKind.Pick:
                        if (step.Items is not { Count: > 0 }) problems.Add($"{where}: items");
                        break;
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void PickSteps_OnlyAskForTheRecipesOwnIngredients()
    {
        // The tray's decoys are drawn from ingredients NOT in the recipe, so a pick item
        // outside the recipe would still work but would read as a mistake in the UI.
        var problems = C.Recipes
            .SelectMany(r => r.Steps.Where(s => s.Kind == StepKind.Pick)
                .SelectMany(s => s.Items!.Where(i => !r.Ingredients.Contains(i)).Select(i => $"{r.Id}: {i}")))
            .ToList();
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void EveryRecipe_CanBePlayedToThreeStars()
    {
        foreach (var recipe in C.Recipes)
        {
            var session = new CookingSession(recipe, C, new GameRandom(1));
            TestGame.PlayPerfectly(session);
            Assert.True(session.Result.Stars == 3, $"{recipe.Id} scored {session.Result.Score}");
            Assert.False(session.Result.Burnt, recipe.Id);
        }
    }

    [Fact]
    public void Regions_ReachableWithinMaxLevel_AndRecipesUnlockNoEarlierThanTheirRegion()
    {
        foreach (var recipe in C.Recipes)
        {
            Assert.InRange(recipe.UnlockLevel, 1, EconomySystem.MaxLevel);
            Assert.True(recipe.UnlockLevel >= C.RegionById[recipe.Region].UnlockLevel, recipe.Id);
        }
    }

    [Fact]
    public void Fish_RegionsAlwaysHaveADaytimeCatch()
    {
        foreach (var region in C.Regions)
        {
            Assert.Contains(region.Fish, f => !C.FishById[f].NightOnly);
        }
    }
}
