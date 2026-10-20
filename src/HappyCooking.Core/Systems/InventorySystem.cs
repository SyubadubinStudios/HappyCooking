using HappyCooking.Core.Content;
using HappyCooking.Core.State;

namespace HappyCooking.Core.Systems;

/// <summary>The pantry: ingredients the player owns, and buying what a recipe is missing.</summary>
public sealed class InventorySystem(GameContent content, PlayerProfile profile, EconomySystem economy)
{
    public int Count(string ingredientId) => profile.Inventory.GetValueOrDefault(ingredientId);

    public void Add(string ingredientId, int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        profile.Inventory[ingredientId] = Count(ingredientId) + amount;
    }

    public bool TryRemove(string ingredientId, int amount = 1)
    {
        var have = Count(ingredientId);
        if (amount <= 0 || have < amount)
        {
            return false;
        }

        if (have == amount)
        {
            profile.Inventory.Remove(ingredientId);
        }
        else
        {
            profile.Inventory[ingredientId] = have - amount;
        }

        return true;
    }

    /// <summary>Ingredients of <paramref name="recipe"/> the pantry does not have (one of each is needed).</summary>
    public IReadOnlyList<Ingredient> MissingFor(Recipe recipe)
    {
        List<Ingredient>? missing = null;
        foreach (var id in recipe.Ingredients)
        {
            if (Count(id) < 1)
            {
                (missing ??= []).Add(content.IngredientById[id]);
            }
        }

        return missing ?? (IReadOnlyList<Ingredient>)[];
    }

    public int CostOfMissing(Recipe recipe)
    {
        var cost = 0;
        foreach (var ing in MissingFor(recipe))
        {
            cost += ing.Price;
        }

        return cost;
    }

    /// <summary>Buys every missing ingredient at market price. False (and nothing bought) when coins are short.</summary>
    public bool TryBuyMissing(Recipe recipe)
    {
        var missing = MissingFor(recipe);
        if (missing.Count == 0)
        {
            return true;
        }

        if (!economy.TrySpendCoins(CostOfMissing(recipe)))
        {
            return false;
        }

        foreach (var ing in missing)
        {
            Add(ing.Id);
        }

        return true;
    }

    public bool TryBuy(string ingredientId, int amount)
    {
        if (!content.IngredientById.TryGetValue(ingredientId, out var ing) || amount <= 0)
        {
            return false;
        }

        if (!economy.TrySpendCoins(ing.Price * amount))
        {
            return false;
        }

        Add(ingredientId, amount);
        return true;
    }

    /// <summary>Uses one of each ingredient. All or nothing.</summary>
    public bool TryConsume(Recipe recipe)
    {
        if (MissingFor(recipe).Count > 0)
        {
            return false;
        }

        foreach (var id in recipe.Ingredients)
        {
            TryRemove(id);
        }

        return true;
    }
}
