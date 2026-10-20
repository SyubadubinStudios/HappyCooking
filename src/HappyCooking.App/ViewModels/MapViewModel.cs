using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HappyCooking.App.Services;
using HappyCooking.Core.Content;
using HappyCooking.Core.Systems;

namespace HappyCooking.App.ViewModels;

public sealed partial class RegionPin : ObservableObject
{
    public RegionPin(Region region, bool unlocked)
    {
        Region = region;
        IsUnlocked = unlocked;
        Name = Loc.Instance.Get(region.Name);
        Brush = new SolidColorBrush(Color.Parse(region.Color));
        LockText = Loc.Instance.Format("common.unlockAtLevel", region.UnlockLevel);
    }

    public Region Region { get; }

    public string Name { get; }

    public IBrush Brush { get; }

    public bool IsUnlocked { get; }

    public string LockText { get; }

    /// <summary>Position on the map, as a fraction of its width and height.</summary>
    public double X => Region.MapX;

    public double Y => Region.MapY;

    /// <summary>Pin position on the 1600×893 map canvas (the pin is centred on the island).</summary>
    public double Left => (X * MapViewModel.MapWidth) - 130;

    public double Top => (Y * MapViewModel.MapHeight) - 40;

    [ObservableProperty]
    private bool _isSelected;
}

public sealed class QuestCard(Quest quest, float progress, int current)
{
    public string Title { get; } = Loc.Instance.Get(quest.Title);

    public string Description { get; } = Loc.Instance.Get(quest.Description);

    public double Progress { get; } = progress * 100;

    public string ProgressText { get; } = $"{Math.Min(current, quest.Amount)}/{quest.Amount}";

    public Bitmap? Portrait { get; } = Images.Portrait(quest.Giver);

    public string RewardCoins { get; } = $"+{quest.RewardCoins}";

    public string RewardGems { get; } = $"+{quest.RewardGems}";

    public bool HasGems { get; } = quest.RewardGems > 0;

    public Bitmap? CoinIcon { get; } = Images.Icon("coin");

    public Bitmap? GemIcon { get; } = Images.Icon("gem");
}

/// <summary>The world map: pick an island, see its dishes and the quests in progress.</summary>
public sealed partial class MapViewModel : PageViewModel
{
    [ObservableProperty]
    private RegionPin? _selected;

    [ObservableProperty]
    private string _regionName = string.Empty;

    [ObservableProperty]
    private string _regionDescription = string.Empty;

    [ObservableProperty]
    private Bitmap? _regionImage;

    [ObservableProperty]
    private Bitmap? _hostPortrait;

    [ObservableProperty]
    private string _hostLine = string.Empty;

    [ObservableProperty]
    private string _weatherText = string.Empty;

    public const double MapWidth = 1600;
    public const double MapHeight = 893;

    public MapViewModel() => Build();

    public override string DockKey => "map";

    public Bitmap? MapImage { get; } = Images.Get("world_map.jpg", 1600);

    public ObservableCollection<RegionPin> Pins { get; } = [];

    public ObservableCollection<RecipeCard> Recipes { get; } = [];

    public ObservableCollection<QuestCard> Quests { get; } = [];

    private void Build()
    {
        var keep = Selected?.Region.Id;
        Pins.Clear();
        foreach (var region in Game.Content.Regions)
        {
            Pins.Add(new RegionPin(region, Game.Economy.IsRegionUnlocked(region)));
        }

        Quests.Clear();
        foreach (var q in Game.Quests.Active.Take(2))
        {
            Quests.Add(new QuestCard(q, Game.Quests.ProgressFraction(q), Game.Quests.Progress(q)));
        }

        Select(Pins.FirstOrDefault(p => p.Region.Id == keep) ?? Pins.LastOrDefault(p => p.IsUnlocked) ?? Pins[0]);
    }

    [RelayCommand]
    private void Select(RegionPin pin)
    {
        foreach (var p in Pins)
        {
            p.IsSelected = p == pin;
        }

        Selected = pin;
        var region = pin.Region;
        RegionName = L.Get(region.Name);
        RegionDescription = L.Get(region.Description);
        RegionImage = Images.Region(region.Id);
        var host = Game.Content.CharacterById[region.Host];
        HostPortrait = Images.Portrait(host.Id);
        HostLine = pin.IsUnlocked ? L.Get(host.Greeting) : pin.LockText;
        var weather = Game.World.WeatherIn(region);
        WeatherText = (Game.World.IsNight ? L["world.night"] : L["world.day"]) + " · " + L["weather." + weather.ToString().ToLowerInvariant()];

        Recipes.Clear();
        foreach (var r in Game.Content.RecipesIn(region.Id))
        {
            Recipes.Add(new RecipeCard(r));
        }

        Host.Sfx.Play(Sound.Pop, 0.9f + (Pins.IndexOf(pin) * 0.05f));
    }

    [RelayCommand]
    private void Cook(RecipeCard card)
    {
        if (!card.IsLocked)
        {
            Shell.OpenKitchen(card.Recipe.Id);
        }
    }

    [RelayCommand]
    private void ViewRecipe(RecipeCard card) => Shell.OpenRecipe(card.Recipe.Id);

    public override void RefreshTexts() => Build();
}
