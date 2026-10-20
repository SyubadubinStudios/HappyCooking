using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HappyCooking.App.Rendering;
using HappyCooking.App.Services;
using HappyCooking.Core.Content;

namespace HappyCooking.App.ViewModels;

public sealed partial class PlotCard(int index) : ObservableObject
{
    public int Index { get; } = index;

    public string Number { get; } = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private double _growth;

    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private bool _isReady;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private IBrush? _swatch;
}

public sealed class SeedCard(Crop crop, Ingredient ingredient)
{
    public Crop Crop { get; } = crop;

    public string Name { get; } = Loc.Instance.Get(ingredient.Name);

    public string Price { get; } = crop.SeedPrice.ToString(System.Globalization.CultureInfo.CurrentCulture);

    public string Detail { get; } = Loc.Instance.Format("farm.seedDetail", crop.GrowSeconds, crop.Yield);

    public IBrush Swatch { get; } = new SolidColorBrush(Color.Parse(crop.Color));
}

/// <summary>The garden: plant seeds, watch them grow in real time, harvest ingredients.</summary>
public sealed partial class FarmViewModel : PageViewModel
{
    private float _uiTimer;

    [ObservableProperty]
    private PlotCard? _selected;

    [ObservableProperty]
    private string _weatherText = string.Empty;

    [ObservableProperty]
    private string _plotPrice = string.Empty;

    [ObservableProperty]
    private bool _canBuyPlot;

    public override string DockKey => "farm";

    public ObservableCollection<PlotCard> Plots { get; } = [];

    public ObservableCollection<SeedCard> Seeds { get; } = [];

    public Avalonia.Media.Imaging.Bitmap? CoinIcon { get; } = Images.Icon("coin");

    public bool SelectedIsEmpty => Selected?.IsEmpty == true;

    public bool SelectedIsReady => Selected?.IsReady == true;

    partial void OnSelectedChanged(PlotCard? value)
    {
        foreach (var p in Plots)
        {
            p.IsSelected = p == value;
        }

        (Stage as FarmStage)?.SetSelected(value?.Index ?? -1);
        OnPropertyChanged(nameof(SelectedIsEmpty));
        OnPropertyChanged(nameof(SelectedIsReady));
    }

    public override void OnActivated()
    {
        var stage = new FarmStage();
        stage.Updated += OnFrame;
        stage.PlotClicked += i => Selected = Plots.ElementAtOrDefault(i);
        Stage = stage;
        BuildPlots();
        Seeds.Clear();
        foreach (var crop in Game.Content.Crops)
        {
            Seeds.Add(new SeedCard(crop, Game.Content.IngredientById[crop.Ingredient]));
        }

        Selected = Plots.FirstOrDefault(p => p.IsEmpty) ?? Plots.FirstOrDefault();
    }

    public override void OnDeactivated()
    {
        if (Stage is { } s)
        {
            s.Updated -= OnFrame;
        }

        base.OnDeactivated();
    }

    private void BuildPlots()
    {
        var keep = Selected?.Index ?? 0;
        Plots.Clear();
        for (var i = 0; i < Game.Farm.Plots.Count; i++)
        {
            Plots.Add(new PlotCard(i));
        }

        (Stage as FarmStage)?.BuildPlots(Plots.Count);
        Refresh();
        Selected = Plots.ElementAtOrDefault(keep);
    }

    private void OnFrame(float dt)
    {
        _uiTimer += dt;
        if (_uiTimer >= 0.25f)
        {
            _uiTimer = 0;
            Refresh();
        }
    }

    private void Refresh()
    {
        var farm = Game.Farm;
        var stage = Stage as FarmStage;
        for (var i = 0; i < Plots.Count && i < farm.Plots.Count; i++)
        {
            var card = Plots[i];
            var cropId = farm.Plots[i].Crop;
            var crop = cropId is null ? null : Game.Content.CropById[cropId];
            var growth = farm.Growth(i);
            var ready = farm.IsReady(i);
            card.IsEmpty = crop is null;
            card.IsReady = ready;
            card.Growth = growth * 100;
            card.Swatch = crop is null ? null : Brush(crop.Color);
            card.Title = crop is null ? L["farm.empty"] : L.Get(Game.Content.IngredientById[crop.Ingredient].Name);
            card.Status = crop is null ? L["farm.tapToPlant"] : ready ? L["farm.ready"] : L.Format("farm.timeLeft", (int)Math.Ceiling(farm.TimeLeft(i).TotalSeconds));
            stage?.UpdatePlot(i, crop, growth, ready);
        }

        var weather = Game.World.WeatherIn("nusantara");
        stage?.SetWeather(weather);
        WeatherText = L["weather." + weather.ToString().ToLowerInvariant()] + (weather == Core.Systems.Weather.Rain ? " · " + L["farm.rainBonus"] : string.Empty);
        PlotPrice = farm.NextPlotPrice.ToString(System.Globalization.CultureInfo.CurrentCulture);
        CanBuyPlot = farm.Plots.Count < Core.Systems.FarmSystem.MaxPlots && Game.Economy.Coins >= farm.NextPlotPrice;
        OnPropertyChanged(nameof(SelectedIsEmpty));
        OnPropertyChanged(nameof(SelectedIsReady));
    }

    [RelayCommand]
    private void SelectPlot(PlotCard card) => Selected = card;

    [RelayCommand]
    private void Plant(SeedCard seed)
    {
        if (Selected is null)
        {
            return;
        }

        if (Game.Farm.TryPlant(Selected.Index, seed.Crop.Id))
        {
            Host.Sfx.Play(Sound.Pop);
            Host.Touch();
            Refresh();
            Selected = Plots.FirstOrDefault(p => p.IsEmpty) ?? Selected;
        }
        else
        {
            Host.Sfx.Play(Sound.Oops);
        }
    }

    [RelayCommand]
    private void Harvest()
    {
        if (Selected is not null && Game.Farm.TryHarvest(Selected.Index) is { } crop)
        {
            Host.Sfx.Play(Sound.Good);
            Host.Toast(L["farm.harvested"], $"+{crop.Yield} {L.Get(Game.Content.IngredientById[crop.Ingredient].Name)}", "farm");
            (Stage as FarmStage)?.Celebrate();
            Host.Touch();
            Refresh();
        }
    }

    [RelayCommand]
    private void HarvestAll()
    {
        var n = Game.Farm.HarvestAll();
        if (n > 0)
        {
            Host.Sfx.Play(Sound.Perfect);
            (Stage as FarmStage)?.Celebrate();
            Host.Touch();
        }

        Refresh();
    }

    [RelayCommand]
    private void BuyPlot()
    {
        if (Game.Farm.TryBuyPlot())
        {
            Host.Sfx.Play(Sound.Coin);
            Host.Touch();
            BuildPlots();
        }
    }

    public override void RefreshTexts() => Refresh();
}
