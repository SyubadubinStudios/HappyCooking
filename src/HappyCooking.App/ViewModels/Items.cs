using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using HappyCooking.App.Services;
using HappyCooking.Core.Content;

namespace HappyCooking.App.ViewModels;

/// <summary>Small display records shared by several screens.</summary>
public sealed class IngredientChip(Ingredient ingredient, int have, int need = 1)
{
    public string Id { get; } = ingredient.Id;

    public string Name { get; } = Loc.Instance.Get(ingredient.Name);

    public IBrush Swatch { get; } = new SolidColorBrush(Color.Parse(ingredient.Color));

    public int Have { get; } = have;

    public bool IsMissing { get; } = have < need;

    public string HaveText { get; } = $"×{have}";
}

public sealed partial class RecipeCard : ObservableObject
{
    public RecipeCard(Recipe recipe)
    {
        Recipe = recipe;
        Name = Loc.Instance.Get(recipe.Name);
        Swatch = new SolidColorBrush(Color.Parse(recipe.Color));
        var game = GameHost.Current.Session;
        IsLocked = !game.Economy.IsRecipeUnlocked(recipe);
        BestStars = game.Profile.BestStars.GetValueOrDefault(recipe.Id);
        IsNew = BestStars == 0;
        LockText = Loc.Instance.Format("common.unlockAtLevel", recipe.UnlockLevel);
        Difficulty = new string('●', recipe.Difficulty) + new string('○', 3 - recipe.Difficulty);
        IsCreative = recipe.Creative;
        IsFeatured = game.Seasonal.CoinMultiplier(recipe.Id) > 1f;
        Region = Loc.Instance.Get(game.Content.RegionById[recipe.Region].Name);
    }

    public Recipe Recipe { get; }

    public string Name { get; }

    public string Region { get; }

    public IBrush Swatch { get; }

    public bool IsLocked { get; }

    public string LockText { get; }

    public int BestStars { get; }

    public bool IsNew { get; }

    public bool IsCreative { get; }

    public bool IsFeatured { get; }

    public string Difficulty { get; }

    public double Star1 => BestStars >= 1 ? 1 : 0.22;

    public double Star2 => BestStars >= 2 ? 1 : 0.22;

    public double Star3 => BestStars >= 3 ? 1 : 0.22;

    public Bitmap? StarIcon => Images.Icon("star");
}
