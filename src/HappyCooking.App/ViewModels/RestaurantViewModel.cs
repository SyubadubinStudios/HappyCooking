using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HappyCooking.App.Rendering;
using HappyCooking.App.Services;
using HappyCooking.Core.Content;
using HappyCooking.Core.Systems;

namespace HappyCooking.App.ViewModels;

public sealed partial class GuestCard : ObservableObject
{
    public GuestCard(Customer customer)
    {
        Customer = customer;
        Name = customer.Name;
        Order = Loc.Instance.Get(customer.Order.Name);
        Portrait = Images.Portrait(customer.Look);
        Swatch = new SolidColorBrush(Color.Parse(customer.Order.Color));
    }

    public Customer Customer { get; }

    public string Name { get; }

    public string Order { get; }

    public Bitmap? Portrait { get; }

    public IBrush Swatch { get; }

    [ObservableProperty]
    private double _patience = 100;

    [ObservableProperty]
    private bool _canServe;

    [ObservableProperty]
    private bool _isHurrying;
}

public sealed class CounterItem(Recipe recipe, int count)
{
    public string Name { get; } = Loc.Instance.Get(recipe.Name);

    public string Count { get; } = $"×{count}";

    public IBrush Swatch { get; } = new SolidColorBrush(Color.Parse(recipe.Color));
}

public sealed partial class DecorationCard(Decoration decoration, bool owned) : ObservableObject
{
    public Decoration Decoration { get; } = decoration;

    public string Name { get; } = Loc.Instance.Get(decoration.Name);

    public string Price { get; } = decoration.Price.ToString(System.Globalization.CultureInfo.CurrentCulture);

    public IBrush Swatch { get; } = new SolidColorBrush(Color.Parse(decoration.Color));

    public string Rating { get; } = "+" + decoration.Rating;

    [ObservableProperty]
    private bool _owned = owned;
}

/// <summary>Your restaurant: seat guests, serve dishes from the counter, decorate to raise the rating.</summary>
public sealed partial class RestaurantViewModel : PageViewModel
{
    private float _uiTimer;

    [ObservableProperty]
    private int _rating;

    [ObservableProperty]
    private string _lastServe = string.Empty;

    [ObservableProperty]
    private bool _showShop;

    public override string DockKey => "restaurant";

    public ObservableCollection<GuestCard> Guests { get; } = [];

    public ObservableCollection<CounterItem> Counter { get; } = [];

    public ObservableCollection<DecorationCard> Shop { get; } = [];

    public bool CounterEmpty => Counter.Count == 0;

    public bool MenuEmpty => !Game.Restaurant.Menu.Any();

    public Bitmap? CoinIcon { get; } = Images.Icon("coin");

    public Bitmap? StarIcon { get; } = Images.Icon("star");

    public override void OnActivated()
    {
        var stage = new RestaurantStage();
        stage.Updated += OnFrame;
        Stage = stage;
        Game.Restaurant.CustomerArrived += OnArrived;
        Game.Restaurant.CustomerLeft += OnLeft;
        foreach (var c in Game.Restaurant.Customers)
        {
            OnArrived(c);
        }

        // Open with a guest already waiting so the room never starts empty.
        if (Game.Restaurant.Customers.Count == 0)
        {
            Game.Restaurant.TrySpawn();
        }

        RefreshAll();
    }

    public override void OnDeactivated()
    {
        Game.Restaurant.CustomerArrived -= OnArrived;
        Game.Restaurant.CustomerLeft -= OnLeft;
        if (Stage is { } s)
        {
            s.Updated -= OnFrame;
        }

        base.OnDeactivated();
    }

    private RestaurantStage? Room => Stage as RestaurantStage;

    private void RefreshAll()
    {
        Rating = Game.Restaurant.Rating;
        Counter.Clear();
        foreach (var (id, count) in Game.Restaurant.Counter)
        {
            Counter.Add(new CounterItem(Game.Content.RecipeById[id], count));
        }

        OnPropertyChanged(nameof(CounterEmpty));
        OnPropertyChanged(nameof(MenuEmpty));

        Shop.Clear();
        foreach (var d in Game.Content.Decorations)
        {
            Shop.Add(new DecorationCard(d, Game.Restaurant.Owns(d.Id)));
        }

        Room?.SyncDecorations(Game.Content.Decorations.Where(d => Game.Restaurant.Owns(d.Id)));
        Room?.SyncCounter(Game.Restaurant.Counter, Game.Content);
        UpdateGuests();
    }

    private void OnArrived(Customer c)
    {
        if (Guests.Any(g => g.Customer == c))
        {
            return;
        }

        Guests.Add(new GuestCard(c));
        Room?.AddGuest(c.Id, c.Look, c.Seat);
        Host.Sfx.Play(Sound.Pop, 1.2f);
    }

    private void OnLeft(Customer c)
    {
        var card = Guests.FirstOrDefault(g => g.Customer == c);
        if (card is not null)
        {
            Guests.Remove(card);
        }

        Room?.RemoveGuest(c.Id);
        LastServe = L.Format("restaurant.left", c.Name);
    }

    private void OnFrame(float dt)
    {
        Game.Restaurant.Update(dt);
        _uiTimer += dt;
        if (_uiTimer >= 0.2f)
        {
            _uiTimer = 0;
            UpdateGuests();
        }
    }

    private void UpdateGuests()
    {
        foreach (var g in Guests)
        {
            g.Patience = g.Customer.PatienceLeft * 100;
            g.IsHurrying = g.Customer.PatienceLeft < 0.3f;
            g.CanServe = Game.Restaurant.CanServe(g.Customer);
        }
    }

    [RelayCommand]
    private void Serve(GuestCard card)
    {
        var result = Game.Restaurant.TryServe(card.Customer);
        if (result is null)
        {
            Host.Sfx.Play(Sound.Oops);
            return;
        }

        Room?.ServeGuest(card.Customer.Id);
        Host.Sfx.Play(Sound.Coin);
        LastServe = L.Format("restaurant.served", card.Name, result.Value.Coins + result.Value.Tip, result.Value.Tip);
        Guests.Remove(card);
        var id = card.Customer.Id;
        Avalonia.Threading.DispatcherTimer.RunOnce(() => Room?.RemoveGuest(id), TimeSpan.FromSeconds(1.4));
        Host.Touch();
        RefreshAll();
    }

    [RelayCommand]
    private void Buy(DecorationCard card)
    {
        if (Game.Restaurant.TryBuyDecoration(card.Decoration.Id))
        {
            Host.Sfx.Play(Sound.Coin);
            Host.Touch();
            RefreshAll();
        }
        else
        {
            Host.Sfx.Play(Sound.Oops);
        }
    }

    [RelayCommand]
    private void ToggleShop() => ShowShop = !ShowShop;

    [RelayCommand]
    private void GoCook() => Shell.Navigate("recipes");

    public override void RefreshTexts() => RefreshAll();
}
