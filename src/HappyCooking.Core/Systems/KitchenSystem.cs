using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.State;

namespace HappyCooking.Core.Systems;

/// <summary>What finishing a dish paid out.</summary>
public readonly record struct DishReward(int Coins, int Xp, int Stars, bool NewBest, bool FirstTime, int LevelsGained);

/// <summary>Starts cooking sessions and pays out finished dishes.</summary>
public sealed class KitchenSystem(
    GameContent content,
    PlayerProfile profile,
    EconomySystem economy,
    InventorySystem inventory,
    PetSystem pets,
    SeasonalSystem seasonal,
    GameEvents events,
    GameRandom random)
{
    public bool CanCook(Recipe recipe) => economy.IsRecipeUnlocked(recipe);

    /// <summary>
    /// Uses the ingredients and starts the mini-games. Returns null when the recipe
    /// is locked or ingredients are missing (buy them first with
    /// <see cref="InventorySystem.TryBuyMissing"/>).
    /// </summary>
    public CookingSession? Start(Recipe recipe)
    {
        if (!CanCook(recipe) || !inventory.TryConsume(recipe))
        {
            return null;
        }

        return new CookingSession(recipe, content, random, pets.Bonus(PetBonus.CookingZone));
    }

    public DishReward Complete(CookingResult result)
    {
        var recipe = result.Recipe;
        var starFactor = result.Stars switch { 3 => 1f, 2 => 0.8f, _ => 0.6f };
        var coins = (int)MathF.Round(recipe.Coins * starFactor * seasonal.CoinMultiplier(recipe.Id));
        var xp = (int)MathF.Round(recipe.Xp * (0.6f + (0.2f * result.Stars)) * (1f + pets.Bonus(PetBonus.Xp)));

        var firstTime = !profile.BestStars.ContainsKey(recipe.Id);
        var best = profile.BestStars.GetValueOrDefault(recipe.Id);
        var newBest = result.Stars > best;
        if (newBest)
        {
            profile.BestStars[recipe.Id] = result.Stars;
        }

        profile.TimesCooked[recipe.Id] = profile.TimesCooked.GetValueOrDefault(recipe.Id) + 1;
        profile.Counter[recipe.Id] = profile.Counter.GetValueOrDefault(recipe.Id) + 1;

        var stats = profile.Stats;
        stats.DishesCooked++;
        stats.TotalStars += result.Stars;
        if (result.Stars == 3)
        {
            stats.ThreeStarDishes++;
        }

        if (recipe.Creative)
        {
            stats.CreativeDishes++;
        }

        if (result.Burnt)
        {
            stats.BurntDishes++;
        }

        economy.AddCoins(coins);
        var levels = economy.AddXp(xp);

        events.Raise(new GameEvent(GameEventKind.DishCooked, recipe.Id, result.Stars, recipe.Region));
        if (result.Burnt)
        {
            events.Raise(new GameEvent(GameEventKind.DishBurnt, recipe.Id));
        }

        return new DishReward(coins, xp, result.Stars, newBest, firstTime, levels);
    }
}
