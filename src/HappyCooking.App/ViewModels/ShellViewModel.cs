using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HappyCooking.App.Services;
using HappyCooking.Core.Localization;

namespace HappyCooking.App.ViewModels;

public sealed partial class DockItem(string key, string icon, string labelKey) : ObservableObject
{
    public string Key { get; } = key;

    public Bitmap? Icon { get; } = Images.Icon(icon);

    public string LabelKey { get; } = labelKey;

    [ObservableProperty]
    private string _label = Loc.Instance[labelKey];

    [ObservableProperty]
    private bool _isActive;
}

public sealed class ToastViewModel(Toast toast)
{
    public string Title { get; } = toast.Title;

    public string Message { get; } = toast.Message;

    public Bitmap? Icon { get; } = Images.Icon(toast.Icon);
}

/// <summary>
/// The frame around every screen: the player HUD, the dock, toasts and navigation.
/// Pages are created fresh on each visit so their 3D scenes and state are released
/// when the player leaves.
/// </summary>
public sealed partial class ShellViewModel : ViewModelBase
{
    [ObservableProperty]
    private PageViewModel _currentPage = null!;

    [ObservableProperty]
    private bool _showChrome;

    [ObservableProperty]
    private string _playerName = string.Empty;

    [ObservableProperty]
    private int _level;

    [ObservableProperty]
    private double _levelProgress;

    [ObservableProperty]
    private string _coins = "0";

    [ObservableProperty]
    private string _gems = "0";

    [ObservableProperty]
    private string? _eventName;

    [ObservableProperty]
    private IBrush? _eventBrush;

    [ObservableProperty]
    private string? _eventText;

    [ObservableProperty]
    private Bitmap? _petImage;

    [ObservableProperty]
    private string _languageLabel = "ID";

    public ShellViewModel(bool startAtMap = false)
    {
        Dock =
        [
            new DockItem("map", "map", "nav.map"),
            new DockItem("recipes", "recipes", "nav.recipes"),
            new DockItem("restaurant", "restaurant", "nav.restaurant"),
            new DockItem("farm", "farm", "nav.farm"),
            new DockItem("fishing", "fishing", "nav.fishing"),
            new DockItem("pets", "pets", "nav.pets"),
            new DockItem("trophies", "trophy", "nav.trophies"),
            new DockItem("friends", "friends", "nav.friends"),
            new DockItem("settings", "settings", "nav.settings"),
        ];

        Host.Changed += RefreshHud;
        Host.ToastRaised += OnToast;
        Loc.Instance.LanguageChanged += _ => OnLanguageChanged();
        RefreshHud();
        Navigate(startAtMap ? "map" : "title");
    }

    public ObservableCollection<DockItem> Dock { get; }

    public ObservableCollection<ToastViewModel> Toasts { get; } = [];

    public Bitmap? CoinIcon { get; } = Images.Icon("coin");

    public Bitmap? GemIcon { get; } = Images.Icon("gem");

    public Bitmap? StarIcon { get; } = Images.Icon("star");

    [RelayCommand]
    public void Navigate(string key) => Show(Create(key));

    public void OpenKitchen(string recipeId)
    {
        Host.Sfx.Play(Sound.Pop);
        Show(new KitchenViewModel(recipeId));
    }

    public void OpenRecipe(string recipeId) => Show(new RecipeBookViewModel(recipeId));

    private PageViewModel Create(string key) => key switch
    {
        "title" => new TitleViewModel(),
        "map" => new MapViewModel(),
        "recipes" => new RecipeBookViewModel(),
        "restaurant" => new RestaurantViewModel(),
        "farm" => new FarmViewModel(),
        "fishing" => new FishingViewModel(),
        "pets" => new PetsViewModel(),
        "trophies" => new TrophiesViewModel(),
        "friends" => new FriendsViewModel(),
        "settings" => new SettingsViewModel(),
        _ => new MapViewModel(),
    };

    private void Show(PageViewModel page)
    {
        CurrentPage?.OnDeactivated();
        page.Shell = this;
        CurrentPage = page;
        ShowChrome = page is not TitleViewModel;
        foreach (var item in Dock)
        {
            item.IsActive = item.Key == page.DockKey;
        }

        page.OnActivated();
        if (page is not TitleViewModel)
        {
            Host.Sfx.Play(Sound.Tap);
        }
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        var next = Loc.Instance.Language == Language.English ? Language.Indonesian : Language.English;
        Loc.Instance.Language = next;
        Host.Profile.Settings.Language = next;
        Host.Touch();
    }

    private void OnLanguageChanged()
    {
        foreach (var item in Dock)
        {
            item.Label = Loc.Instance[item.LabelKey];
        }

        RefreshHud();
        CurrentPage?.RefreshTexts();
    }

    private void RefreshHud()
    {
        var p = Host.Profile;
        PlayerName = p.PlayerName;
        Level = p.Level;
        LevelProgress = Game.Economy.LevelProgress * 100;
        Coins = p.Coins.ToString("N0", System.Globalization.CultureInfo.CurrentCulture);
        Gems = p.Gems.ToString("N0", System.Globalization.CultureInfo.CurrentCulture);
        var ev = Game.Seasonal.ActiveEvent;
        EventName = ev is null ? null : L.Get(ev.Name);
        EventBrush = ev is null ? null : Brush(ev.Color);
        EventText = ev is null ? null : L.Get(ev.Description);
        PetImage = p.ActivePet is { } pet ? Images.Pet(pet) : null;
        LanguageLabel = Loc.Instance.Language == Language.English ? "ID" : "EN";
    }

    private void OnToast(Toast toast)
    {
        var vm = new ToastViewModel(toast);
        Toasts.Add(vm);
        DispatcherTimer.RunOnce(() => Toasts.Remove(vm), TimeSpan.FromSeconds(3.5));
    }
}
