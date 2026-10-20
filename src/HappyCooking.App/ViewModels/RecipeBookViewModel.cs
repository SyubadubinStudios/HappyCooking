using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HappyCooking.App.Rendering;
using HappyCooking.App.Services;

namespace HappyCooking.App.ViewModels;

public sealed partial class RegionTab(string id, string name) : ObservableObject
{
    public string Id { get; } = id;

    public string Name { get; } = name;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>The recipe book: every dish, its story and fun fact, with a 3D turntable preview.</summary>
public sealed partial class RecipeBookViewModel : PageViewModel
{
    private readonly string? _initialRecipe;

    [ObservableProperty]
    private RecipeCard? _selected;

    [ObservableProperty]
    private string _detailName = string.Empty;

    [ObservableProperty]
    private string _detailRegion = string.Empty;

    [ObservableProperty]
    private string _detailDescription = string.Empty;

    [ObservableProperty]
    private string _detailFact = string.Empty;

    [ObservableProperty]
    private string _detailCooked = string.Empty;

    [ObservableProperty]
    private string _detailRewards = string.Empty;

    public RecipeBookViewModel(string? recipeId = null)
    {
        _initialRecipe = recipeId;
        BuildTabs();
    }

    public override string DockKey => "recipes";

    public ObservableCollection<RegionTab> Tabs { get; } = [];

    public ObservableCollection<RecipeCard> Recipes { get; } = [];

    public ObservableCollection<IngredientChip> Ingredients { get; } = [];

    public ObservableCollection<string> Steps { get; } = [];

    public string Progress => L.Format("book.progress", Game.Profile.BestStars.Count, Game.Content.Recipes.Count);

    private void BuildTabs()
    {
        Tabs.Clear();
        Tabs.Add(new RegionTab("all", L["book.all"]));
        foreach (var r in Game.Content.Regions)
        {
            Tabs.Add(new RegionTab(r.Id, L.Get(r.Name)));
        }

        Tabs.Add(new RegionTab("creative", L["book.creative"]));
        var start = _initialRecipe is { } id ? Game.Content.RecipeById[id].Region : "all";
        SelectTab(Tabs.First(t => t.Id == start));
    }

    public override void OnActivated()
    {
        var stage = new DishPreviewStage();
        Stage = stage;
        if (Selected is not null)
        {
            stage.Show(Selected.Recipe);
        }
    }

    [RelayCommand]
    private void SelectTab(RegionTab tab)
    {
        foreach (var t in Tabs)
        {
            t.IsSelected = t == tab;
        }

        Recipes.Clear();
        foreach (var r in Game.Content.Recipes)
        {
            if (tab.Id == "all" || r.Region == tab.Id || (tab.Id == "creative" && r.Creative))
            {
                Recipes.Add(new RecipeCard(r));
            }
        }

        Select(Recipes.FirstOrDefault(c => c.Recipe.Id == _initialRecipe) ?? Recipes.FirstOrDefault());
    }

    [RelayCommand]
    private void Select(RecipeCard? card)
    {
        Selected = card;
        if (card is null)
        {
            return;
        }

        var r = card.Recipe;
        DetailName = card.Name;
        DetailRegion = card.Region;
        DetailDescription = L.Get(r.Description);
        DetailFact = L.Get(r.FunFact);
        var times = Game.Profile.TimesCooked.GetValueOrDefault(r.Id);
        DetailCooked = times == 0 ? L["book.notCooked"] : L.Format("book.cooked", times);
        DetailRewards = L.Format("book.rewards", r.Coins, r.Xp);

        Ingredients.Clear();
        foreach (var id in r.Ingredients)
        {
            Ingredients.Add(new IngredientChip(Game.Content.IngredientById[id], Game.Inventory.Count(id)));
        }

        Steps.Clear();
        foreach (var step in r.Steps)
        {
            var item = step.Item is { } i ? L.Get(Game.Content.IngredientById[i].Name) : card.Name;
            Steps.Add(L.Format("step." + step.Verb, item));
        }

        (Stage as DishPreviewStage)?.Show(r);
    }

    [RelayCommand]
    private void Cook()
    {
        if (Selected is { IsLocked: false } card)
        {
            Shell.OpenKitchen(card.Recipe.Id);
        }
    }

    public override void RefreshTexts()
    {
        var keep = Selected?.Recipe.Id;
        BuildTabs();
        Select(Recipes.FirstOrDefault(c => c.Recipe.Id == keep) ?? Recipes.FirstOrDefault());
        OnPropertyChanged(nameof(Progress));
    }
}
