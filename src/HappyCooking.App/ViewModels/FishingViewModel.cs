using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HappyCooking.App.Rendering;
using HappyCooking.App.Services;
using HappyCooking.Core.Content;
using HappyCooking.Core.Systems;

namespace HappyCooking.App.ViewModels;

public sealed partial class SpotChip(Region region) : ObservableObject
{
    public Region Region { get; } = region;

    public string Name { get; } = Loc.Instance.Get(region.Name);

    [ObservableProperty]
    private bool _isSelected;
}

public sealed class FishLogEntry(Fish fish, int caught)
{
    public string Name { get; } = caught > 0 ? Loc.Instance.Get(fish.Name) : "???";

    public string Rarity { get; } = Loc.Instance["rarity." + fish.Rarity.ToString().ToLowerInvariant()];

    public string Caught { get; } = $"×{caught}";

    public bool Known { get; } = caught > 0;

    public bool NightOnly { get; } = fish.NightOnly;

    public IBrush Swatch { get; } = new SolidColorBrush(caught > 0 ? Color.Parse(fish.Color) : Color.Parse("#C9C8E0"));

    public IBrush RarityBrush { get; } = new SolidColorBrush(Color.Parse(fish.Rarity switch
    {
        Core.Content.Rarity.Legendary => "#B57BFF",
        Core.Content.Rarity.Rare => "#FF5A4E",
        Core.Content.Rarity.Uncommon => "#2EC4D6",
        _ => "#5FD3A6",
    }));
}

/// <summary>The pier: one big button casts, hooks and reels. What bites depends on the island and the clock.</summary>
public sealed partial class FishingViewModel : PageViewModel
{
    private FishingSession? _session;

    [ObservableProperty]
    private SpotChip? _spot;

    [ObservableProperty]
    private FishingPhase _phase;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _buttonLabel = string.Empty;

    [ObservableProperty]
    private double _timer;

    [ObservableProperty]
    private double _reel;

    [ObservableProperty]
    private string _lastCatch = string.Empty;

    [ObservableProperty]
    private bool _isBite;

    [ObservableProperty]
    private bool _isReeling;

    public override string DockKey => "fishing";

    public ObservableCollection<SpotChip> Spots { get; } = [];

    public ObservableCollection<FishLogEntry> Log { get; } = [];

    public string ClockText => Game.World.IsNight ? L["fishing.night"] : L["fishing.day"];

    public override void OnActivated()
    {
        var stage = new FishingStage(Game.World.TimeOfDay, Game.World.IsNight);
        stage.Updated += OnFrame;
        Stage = stage;
        Spots.Clear();
        foreach (var r in Game.Content.Regions.Where(Game.Economy.IsRegionUnlocked))
        {
            Spots.Add(new SpotChip(r));
        }

        SelectSpot(Spots[0]);
    }

    public override void OnDeactivated()
    {
        if (Stage is { } s)
        {
            s.Updated -= OnFrame;
        }

        base.OnDeactivated();
    }

    [RelayCommand]
    private void SelectSpot(SpotChip chip)
    {
        foreach (var s in Spots)
        {
            s.IsSelected = s == chip;
        }

        Spot = chip;
        if (_session is not null)
        {
            _session.Caught -= OnCaught;
        }

        _session = Game.Fishing.StartSession(chip.Region);
        _session.Caught += OnCaught;
        RefreshLog();
        Sync();
    }

    private void RefreshLog()
    {
        Log.Clear();
        if (Spot is null)
        {
            return;
        }

        foreach (var id in Spot.Region.Fish)
        {
            var fish = Game.Content.FishById[id];
            Log.Add(new FishLogEntry(fish, Game.Fishing.TimesCaught(id)));
        }
    }

    private void OnFrame(float dt)
    {
        if (_session is null)
        {
            return;
        }

        var before = _session.Phase;
        _session.Update(dt);
        if (before != _session.Phase && _session.Phase == FishingPhase.Bite)
        {
            Host.Sfx.Play(Sound.Splash);
        }

        Sync();
    }

    [RelayCommand]
    private void Tap()
    {
        if (_session is null)
        {
            return;
        }

        var before = _session.Phase;
        _session.Tap();
        if (_session.Phase == FishingPhase.Waiting && before != FishingPhase.Waiting)
        {
            Host.Sfx.Play(Sound.Splash, 1.3f);
        }
        else if (_session.Phase == FishingPhase.Reeling)
        {
            Host.Sfx.Play(Sound.Tap, 0.8f + (float)(_session.ReelProgress * 0.6));
        }
        else if (_session.Phase == FishingPhase.Escaped && before == FishingPhase.Waiting)
        {
            Host.Sfx.Play(Sound.Oops);
        }

        Sync();
    }

    private void OnCaught(Fish fish)
    {
        Host.Sfx.Play(fish.Rarity >= Rarity.Rare ? Sound.LevelUp : Sound.Perfect);
        (Stage as FishingStage)?.Caught(fish.Color);
        LastCatch = L.Format("fishing.caught", L.Get(fish.Name), L["rarity." + fish.Rarity.ToString().ToLowerInvariant()], fish.Coins);
        Host.Touch();
        RefreshLog();
    }

    private void Sync()
    {
        if (_session is null)
        {
            return;
        }

        Phase = _session.Phase;
        (Stage as FishingStage)?.SetPhase(Phase);
        IsBite = Phase == FishingPhase.Bite;
        IsReeling = Phase == FishingPhase.Reeling;
        Timer = _session.TimerFraction * 100;
        Reel = _session.ReelProgress * 100;
        var key = Phase.ToString().ToLowerInvariant();
        Status = L["fishing.status." + key];
        ButtonLabel = L["fishing.button." + key];
    }

    public override void RefreshTexts()
    {
        RefreshLog();
        Sync();
        OnPropertyChanged(nameof(ClockText));
    }
}
