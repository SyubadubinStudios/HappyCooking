using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.Systems;

namespace HappyCooking.Core.Tests;

public class KitchenEconomyTests
{
    private static GameSession NewGame() => TestGame.Create();

    // ---------- Economy ----------

    [Fact]
    public void XpForNextLevel_IsPositiveAndStrictlyIncreasing()
    {
        for (var level = 1; level < EconomySystem.MaxLevel; level++)
        {
            Assert.True(EconomySystem.XpForNextLevel(level) > 0);
            Assert.True(EconomySystem.XpForNextLevel(level + 1) > EconomySystem.XpForNextLevel(level), $"level {level}");
        }
    }

    [Fact]
    public void Coins_SpendOnlyWhatYouHave()
    {
        var g = NewGame();
        var start = g.Economy.Coins;
        Assert.False(g.Economy.TrySpendCoins(start + 1));
        Assert.False(g.Economy.TrySpendCoins(-5));
        Assert.Equal(start, g.Economy.Coins);
        Assert.True(g.Economy.TrySpendCoins(start));
        Assert.Equal(0, g.Economy.Coins);

        g.Economy.AddCoins(-10); // ignored
        g.Economy.AddCoins(30);
        Assert.Equal(30, g.Economy.Coins);
        Assert.Equal(30, g.Profile.Stats.CoinsEarned);
    }

    [Fact]
    public void Gems_SpendOnlyWhatYouHave()
    {
        var g = NewGame();
        var start = g.Economy.Gems;
        Assert.False(g.Economy.TrySpendGems(start + 1));
        g.Economy.AddGems(-3);
        Assert.Equal(start, g.Economy.Gems);
        Assert.True(g.Economy.TrySpendGems(start));
        Assert.Equal(0, g.Economy.Gems);
    }

    [Fact]
    public void AddXp_LevelsUpRepeatedly_AndRaisesLevelAndRegionEvents()
    {
        var g = NewGame();
        var events = new List<GameEvent>();
        g.Events.Subscribe(events.Add);

        var xp = EconomySystem.XpForNextLevel(1) + EconomySystem.XpForNextLevel(2) + 7;
        var gained = g.Economy.AddXp(xp);

        Assert.Equal(2, gained);
        Assert.Equal(3, g.Economy.Level);
        Assert.Equal(7, g.Economy.Xp);
        Assert.Equal([2, 3], events.Where(e => e.Kind == GameEventKind.LevelUp).Select(e => e.Amount));
        Assert.Equal(["sakura", "pasta"], events.Where(e => e.Kind == GameEventKind.RegionUnlocked).Select(e => e.Id));
        Assert.True(g.Economy.IsRegionUnlocked("pasta"));
        Assert.False(g.Economy.IsRegionUnlocked("dragonwok"));
        Assert.False(g.Economy.IsRegionUnlocked("nowhere"));
        Assert.Equal(3, g.Economy.UnlockedRegionCount);
    }

    [Fact]
    public void AddXp_BelowThreshold_JustAccumulates()
    {
        var g = NewGame();
        Assert.Equal(0, g.Economy.AddXp(10));
        Assert.Equal(0, g.Economy.AddXp(0));
        Assert.Equal(1, g.Economy.Level);
        Assert.Equal(10, g.Economy.Xp);
        Assert.Equal(10f / EconomySystem.XpForNextLevel(1), g.Economy.LevelProgress, 4);
    }

    [Fact]
    public void AddXp_StopsAtMaxLevel()
    {
        var g = NewGame();
        g.Economy.AddXp(10_000_000);
        Assert.Equal(EconomySystem.MaxLevel, g.Economy.Level);
        Assert.Equal(0, g.Economy.Xp);
        Assert.Equal(0, g.Economy.XpToNext);
        Assert.Equal(1f, g.Economy.LevelProgress);
        Assert.Equal(0, g.Economy.AddXp(100));
        Assert.Equal(g.Content.Regions.Count, g.Economy.UnlockedRegionCount);
    }

    // ---------- Inventory ----------

    [Fact]
    public void NewProfile_HasStarterPantryForEveryLevelOneDish()
    {
        var g = NewGame();
        foreach (var recipe in g.Content.Recipes.Where(r => g.Economy.IsRecipeUnlocked(r)))
        {
            Assert.True(g.Inventory.MissingFor(recipe).Count == 0, $"missing ingredients for {recipe.Id}");
        }

        Assert.DoesNotContain(g.Profile.Inventory, kv => kv.Value <= 0);
    }

    [Fact]
    public void Inventory_AddRemove()
    {
        var g = NewGame();
        g.Profile.Inventory.Remove("chocolate"); // the starter pantry may hold some
        g.Inventory.Add("chocolate", 2);
        g.Inventory.Add("chocolate", 0);
        g.Inventory.Add("chocolate", -4);
        Assert.Equal(2, g.Inventory.Count("chocolate"));
        Assert.False(g.Inventory.TryRemove("chocolate", 3));
        Assert.False(g.Inventory.TryRemove("chocolate", 0));
        Assert.True(g.Inventory.TryRemove("chocolate", 2));
        Assert.Equal(0, g.Inventory.Count("chocolate"));
        Assert.False(g.Profile.Inventory.ContainsKey("chocolate")); // empty entries are dropped
    }

    [Fact]
    public void TryBuy_ChargesMarketPrice()
    {
        var g = NewGame();
        var price = g.Content.IngredientById["egg"].Price;
        var before = g.Inventory.Count("egg");
        var coins = g.Economy.Coins;
        Assert.True(g.Inventory.TryBuy("egg", 3));
        Assert.Equal(before + 3, g.Inventory.Count("egg"));
        Assert.Equal(coins - (3 * price), g.Economy.Coins);
        Assert.False(g.Inventory.TryBuy("unobtainium", 1));
        Assert.False(g.Inventory.TryBuy("egg", 0));
    }

    // ---------- Kitchen ----------

    [Fact]
    public void Start_ConsumesOneOfEachIngredient()
    {
        var g = NewGame();
        var recipe = g.Content.RecipeById["nasi_goreng"];
        var before = recipe.Ingredients.ToDictionary(i => i, g.Inventory.Count);

        var session = g.Kitchen.Start(recipe);

        Assert.NotNull(session);
        Assert.Same(recipe, session!.Recipe);
        foreach (var id in recipe.Ingredients)
        {
            Assert.Equal(before[id] - 1, g.Inventory.Count(id));
        }
    }

    [Fact]
    public void Start_ReturnsNull_AndConsumesNothing_WhenAnIngredientIsMissing()
    {
        var g = NewGame();
        var recipe = g.Content.RecipeById["nasi_goreng"];
        g.Profile.Inventory.Remove("egg");
        var before = recipe.Ingredients.ToDictionary(i => i, g.Inventory.Count);

        Assert.Null(g.Kitchen.Start(recipe));
        Assert.Equal(["egg"], g.Inventory.MissingFor(recipe).Select(i => i.Id));
        foreach (var id in recipe.Ingredients)
        {
            Assert.Equal(before[id], g.Inventory.Count(id));
        }
    }

    [Fact]
    public void Start_ReturnsNull_ForLockedRecipe()
    {
        var g = NewGame();
        var locked = g.Content.Recipes.First(r => r.UnlockLevel > 1);
        foreach (var id in locked.Ingredients)
        {
            g.Inventory.Add(id, 5);
        }

        Assert.False(g.Kitchen.CanCook(locked));
        Assert.Null(g.Kitchen.Start(locked));
    }

    [Fact]
    public void TryBuyMissing_BuysExactlyTheMissing_ThenStartWorks()
    {
        var g = NewGame();
        var recipe = g.Content.RecipeById["nasi_goreng"];
        g.Profile.Inventory.Remove("egg");
        g.Profile.Inventory.Remove("chili");
        var cost = g.Content.IngredientById["egg"].Price + g.Content.IngredientById["chili"].Price;
        Assert.Equal(cost, g.Inventory.CostOfMissing(recipe));
        var coins = g.Economy.Coins;

        Assert.True(g.Inventory.TryBuyMissing(recipe));
        Assert.Equal(coins - cost, g.Economy.Coins);
        Assert.Empty(g.Inventory.MissingFor(recipe));
        Assert.True(g.Inventory.TryBuyMissing(recipe)); // nothing missing: free
        Assert.Equal(coins - cost, g.Economy.Coins);
        Assert.NotNull(g.Kitchen.Start(recipe));
    }

    [Fact]
    public void TryBuyMissing_FailsWithoutCoins_AndBuysNothing()
    {
        var g = NewGame();
        var recipe = g.Content.RecipeById["nasi_goreng"];
        g.Profile.Inventory.Remove("egg");
        g.Profile.Coins = 0;
        Assert.False(g.Inventory.TryBuyMissing(recipe));
        Assert.Equal(0, g.Inventory.Count("egg"));
        Assert.Null(g.Kitchen.Start(recipe));
    }

    [Fact]
    public void Complete_PaysCoinsAndXp_FillsCounter_RecordsBest()
    {
        var g = NewGame();
        var recipe = g.Content.RecipeById["nasi_goreng"];
        var coins = g.Economy.Coins;
        var events = new List<GameEvent>();
        g.Events.Subscribe(events.Add);

        var reward = g.Kitchen.Complete(new CookingResult(recipe, 1f, 3, false, 12f));

        Assert.Equal(recipe.Coins, reward.Coins);
        Assert.Equal((int)MathF.Round(recipe.Xp * 1.2f), reward.Xp); // 0.6 + (0.2 * 3 stars)
        Assert.True(reward.FirstTime);
        Assert.True(reward.NewBest);
        Assert.Equal(3, g.Profile.BestStars[recipe.Id]);
        Assert.Equal(1, g.Profile.Counter[recipe.Id]);
        Assert.Equal(1, g.Profile.TimesCooked[recipe.Id]);
        Assert.Equal(1, g.Profile.Stats.DishesCooked);
        Assert.Equal(1, g.Profile.Stats.ThreeStarDishes);
        Assert.Equal(3, g.Profile.Stats.TotalStars);
        Assert.True(g.Economy.Coins >= coins + recipe.Coins); // plus any quest/achievement rewards
        Assert.Contains(events, e => e.Kind == GameEventKind.DishCooked && e.Id == recipe.Id && e.Amount == 3 && e.Extra == recipe.Region);
    }

    [Theory]
    [InlineData(3, 1.0f, 1.2f)]
    [InlineData(2, 0.8f, 1.0f)]
    [InlineData(1, 0.6f, 0.8f)]
    public void Complete_RewardScalesWithStars(int stars, float coinFactor, float xpFactor)
    {
        var g = NewGame();
        var recipe = g.Content.RecipeById["nasi_goreng"];
        var reward = g.Kitchen.Complete(new CookingResult(recipe, 0.5f, stars, false, 10f));
        Assert.Equal((int)MathF.Round(recipe.Coins * coinFactor), reward.Coins);
        Assert.Equal((int)MathF.Round(recipe.Xp * xpFactor), reward.Xp);
    }

    [Fact]
    public void Complete_BestStarsOnlyImproves_AndCounterStacks()
    {
        var g = NewGame();
        var recipe = g.Content.RecipeById["nasi_goreng"];
        var first = g.Kitchen.Complete(new CookingResult(recipe, 0.9f, 3, false, 10f));
        var second = g.Kitchen.Complete(new CookingResult(recipe, 0.3f, 1, true, 10f));

        Assert.True(first.NewBest);
        Assert.False(second.NewBest);
        Assert.False(second.FirstTime);
        Assert.Equal(3, g.Profile.BestStars[recipe.Id]);
        Assert.Equal(2, g.Profile.Counter[recipe.Id]);
        Assert.Equal(1, g.Profile.Stats.BurntDishes);
    }

    [Fact]
    public void Complete_RaisesDishBurnt_ForBurntDish()
    {
        var g = NewGame();
        var burnt = new List<GameEvent>();
        g.Events.Subscribe(e =>
        {
            if (e.Kind == GameEventKind.DishBurnt)
            {
                burnt.Add(e);
            }
        });
        g.Kitchen.Complete(new CookingResult(g.Content.RecipeById["nasi_goreng"], 0.2f, 1, true, 10f));
        Assert.Single(burnt);
    }

    [Fact]
    public void Complete_ReportsLevelsGained()
    {
        var g = NewGame();
        g.Profile.Xp = EconomySystem.XpForNextLevel(1) - 1;
        var reward = g.Kitchen.Complete(new CookingResult(g.Content.RecipeById["nasi_goreng"], 1f, 3, false, 10f));
        Assert.True(reward.LevelsGained >= 1);
        Assert.True(g.Economy.Level >= 2);
    }

    [Fact]
    public void FullDish_EndToEnd_ThroughTheFacade()
    {
        var g = NewGame();
        var recipe = g.Content.RecipeById["nasi_goreng"];
        var session = g.Kitchen.Start(recipe)!;
        TestGame.PlayPerfectly(session);
        var reward = g.Kitchen.Complete(session.Result);
        Assert.Equal(3, reward.Stars);
        Assert.Equal(1, g.Profile.Counter[recipe.Id]);
    }

    [Fact]
    public void SameSeed_SameTray()
    {
        var recipe = TestGame.Content.Recipes.First(r => r.Steps.Any(s => s.Kind == HappyCooking.Core.Content.StepKind.Pick));
        var a = new CookingSession(recipe, TestGame.Content, new GameRandom(99));
        var b = new CookingSession(recipe, TestGame.Content, new GameRandom(99));
        while (a.Step is not PickStep)
        {
            TestGame.PlayStepPerfectly(a);
            TestGame.PlayStepPerfectly(b);
        }

        Assert.Equal(((PickStep)a.Step).Tray, ((PickStep)b.Step).Tray);
    }
}
