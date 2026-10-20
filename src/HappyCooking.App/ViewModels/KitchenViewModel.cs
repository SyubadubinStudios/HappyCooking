using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HappyCooking.App.Rendering;
using HappyCooking.App.Services;
using HappyCooking.Core.Content;
using HappyCooking.Core.Systems;

namespace HappyCooking.App.ViewModels;

public enum KitchenPhase
{
    Prep,
    Cooking,
    Result,
}

public sealed partial class PickItem(Ingredient ingredient) : ObservableObject
{
    public string Id { get; } = ingredient.Id;

    public string Name { get; } = Loc.Instance.Get(ingredient.Name);

    public string Color { get; } = ingredient.Color;

    public IBrush Swatch { get; } = new SolidColorBrush(Avalonia.Media.Color.Parse(ingredient.Color));

    [ObservableProperty]
    private bool _isPicked;

    [ObservableProperty]
    private bool _isWrong;
}

/// <summary>
/// The kitchen: prepare ingredients, play each step's mini-game in front of the 3D
/// counter, and see the result. All rules live in <see cref="CookingSession"/>;
/// this class only translates input and state for the view.
/// </summary>
public sealed partial class KitchenViewModel : PageViewModel
{
    private readonly Recipe _recipe;
    private readonly string _hostModel;
    private CookingSession? _session;
    private int _shownStep = -1;

    [ObservableProperty]
    private KitchenPhase _phase = KitchenPhase.Prep;

    [ObservableProperty]
    private string _stepTitle = string.Empty;

    [ObservableProperty]
    private string _stepHint = string.Empty;

    [ObservableProperty]
    private string _stepCounter = string.Empty;

    [ObservableProperty]
    private string _actionLabel = string.Empty;

    [ObservableProperty]
    private StepKind _kind;

    [ObservableProperty]
    private double _meter;

    [ObservableProperty]
    private double _zoneMin;

    [ObservableProperty]
    private double _zoneMax;

    [ObservableProperty]
    private double _marker;

    [ObservableProperty]
    private double _stepProgress;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private string _feedback = string.Empty;

    [ObservableProperty]
    private int _feedbackPulse;

    [ObservableProperty]
    private bool _inZone;

    // Result
    [ObservableProperty]
    private int _stars;

    [ObservableProperty]
    private string _resultTitle = string.Empty;

    [ObservableProperty]
    private string _rewardCoins = string.Empty;

    [ObservableProperty]
    private string _rewardXp = string.Empty;

    [ObservableProperty]
    private string _resultNote = string.Empty;

    [ObservableProperty]
    private bool _burnt;

    [ObservableProperty]
    private string _missingText = string.Empty;

    [ObservableProperty]
    private bool _hasMissing;

    [ObservableProperty]
    private bool _canAffordMissing;

    public KitchenViewModel(string recipeId)
    {
        _recipe = Game.Content.RecipeById[recipeId];
        var region = Game.Content.RegionById[_recipe.Region];
        var host = Game.Content.CharacterById[region.Host];
        _hostModel = host.Model;
        HostPortrait = Images.Portrait(host.Id);
        HostName = L.Get(host.Name);
        HostGreeting = L.Get(host.Greeting);
        RecipeName = L.Get(_recipe.Name);
        Description = L.Get(_recipe.Description);
        FunFact = L.Get(_recipe.FunFact);
        RecipeSwatch = Brush(_recipe.Color);
        RefreshPantry();
    }

    public override string DockKey => "recipes";

    public string RecipeName { get; }

    public string Description { get; }

    public string FunFact { get; }

    public IBrush RecipeSwatch { get; }

    public Bitmap? HostPortrait { get; }

    public string HostName { get; }

    public string HostGreeting { get; }

    public Bitmap? StarIcon { get; } = Images.Icon("star");

    public Bitmap? CoinIcon { get; } = Images.Icon("coin");

    public ObservableCollection<IngredientChip> Ingredients { get; } = [];

    public ObservableCollection<PickItem> Tray { get; } = [];

    public ObservableCollection<bool> StepDots { get; } = [];

    public bool IsPrep => Phase == KitchenPhase.Prep;

    public bool IsCooking => Phase == KitchenPhase.Cooking;

    public bool IsResult => Phase == KitchenPhase.Result;

    public bool IsTap => IsCooking && Kind == StepKind.Tap;

    public bool IsMeter => IsCooking && Kind is StepKind.Heat or StepKind.Pour;

    public bool IsRhythm => IsCooking && Kind == StepKind.Rhythm;

    public bool IsPick => IsCooking && Kind == StepKind.Pick;

    public bool IsDecorate => IsCooking && Kind == StepKind.Decorate;

    /// <summary>Every kind except Pick is played with the one big action button.</summary>
    public bool ShowActionButton => IsCooking && Kind != StepKind.Pick && Kind != StepKind.Decorate;

    public double Star1 => Stars >= 1 ? 1 : 0.2;

    public double Star2 => Stars >= 2 ? 1 : 0.2;

    public double Star3 => Stars >= 3 ? 1 : 0.2;

    private KitchenStage? KitchenStage => Stage as KitchenStage;

    partial void OnPhaseChanged(KitchenPhase value) => RaiseModeFlags();

    partial void OnKindChanged(StepKind value) => RaiseModeFlags();

    partial void OnStarsChanged(int value)
    {
        OnPropertyChanged(nameof(Star1));
        OnPropertyChanged(nameof(Star2));
        OnPropertyChanged(nameof(Star3));
    }

    private void RaiseModeFlags()
    {
        OnPropertyChanged(nameof(IsPrep));
        OnPropertyChanged(nameof(IsCooking));
        OnPropertyChanged(nameof(IsResult));
        OnPropertyChanged(nameof(IsTap));
        OnPropertyChanged(nameof(IsMeter));
        OnPropertyChanged(nameof(IsRhythm));
        OnPropertyChanged(nameof(IsPick));
        OnPropertyChanged(nameof(IsDecorate));
        OnPropertyChanged(nameof(ShowActionButton));
    }

    public override void OnActivated()
    {
        var stage = new KitchenStage(_hostModel);
        stage.Updated += OnFrame;
        Stage = stage;
        Host.BurntDish += OnBurnt;
    }

    public override void OnDeactivated()
    {
        Host.BurntDish -= OnBurnt;
        if (Stage is { } s)
        {
            s.Updated -= OnFrame;
        }

        base.OnDeactivated();
    }

    private void RefreshPantry()
    {
        Ingredients.Clear();
        foreach (var id in _recipe.Ingredients)
        {
            Ingredients.Add(new IngredientChip(Game.Content.IngredientById[id], Game.Inventory.Count(id)));
        }

        var cost = Game.Inventory.CostOfMissing(_recipe);
        HasMissing = Game.Inventory.MissingFor(_recipe).Count > 0;
        CanAffordMissing = Game.Economy.Coins >= cost;
        MissingText = HasMissing ? L.Format("kitchen.buyMissing", cost) : L["kitchen.allReady"];
        StartCommand.NotifyCanExecuteChanged();
        BuyMissingCommand.NotifyCanExecuteChanged();
    }

    private bool CanBuyMissing() => HasMissing && CanAffordMissing;

    [RelayCommand(CanExecute = nameof(CanBuyMissing))]
    private void BuyMissing()
    {
        if (Game.Inventory.TryBuyMissing(_recipe))
        {
            Host.Sfx.Play(Sound.Coin);
            Host.Touch();
        }

        RefreshPantry();
    }

    private bool CanStart() => !HasMissing && Game.Kitchen.CanCook(_recipe);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        _session = Game.Kitchen.Start(_recipe);
        if (_session is null)
        {
            RefreshPantry();
            return;
        }

        Host.Touch();
        _session.StepFinished += OnStepFinished;
        _session.Finished += OnFinished;
        _shownStep = -1;
        StepDots.Clear();
        for (var i = 0; i < _session.StepCount; i++)
        {
            StepDots.Add(false);
        }

        KitchenStage?.ResetDish();
        Phase = KitchenPhase.Cooking;
        ShowStep();
        Host.Sfx.Play(Sound.Pop);
    }

    private void ShowStep()
    {
        if (_session is null || _session.IsFinished || _shownStep == _session.StepIndex)
        {
            return;
        }

        _shownStep = _session.StepIndex;
        var step = _session.Step;
        var def = step.Definition;
        Kind = def.Kind;
        var item = def.Item is { } id ? L.Get(Game.Content.IngredientById[id].Name) : RecipeName;
        StepTitle = L.Format("step." + def.Verb, item);
        StepHint = L["hint." + def.Kind.ToString().ToLowerInvariant()];
        StepCounter = L.Format("kitchen.stepOf", _session.StepIndex + 1, _session.StepCount);
        ActionLabel = def.Kind switch
        {
            StepKind.Heat => L["action.stop"],
            StepKind.Pour => L["action.hold"],
            StepKind.Rhythm => L["action.now"],
            _ => L.Localizer.Contains("action." + def.Verb) ? L["action." + def.Verb] : L["action.tap"],
        };

        if (step is MeterStep meter)
        {
            ZoneMin = meter.ZoneMin;
            ZoneMax = meter.ZoneMax;
        }
        else if (step is RhythmStep rhythm)
        {
            ZoneMin = rhythm.ZoneMin;
            ZoneMax = rhythm.ZoneMax;
        }

        Tray.Clear();
        if (step is PickStep pick)
        {
            foreach (var trayId in pick.Tray)
            {
                Tray.Add(new PickItem(Game.Content.IngredientById[trayId]));
            }
        }

        Meter = 0;
        Marker = 0;
        StepProgress = 0;
        OverallProgress = (double)_session.StepIndex / _session.StepCount * 100;
        KitchenStage?.BeginStep(def.Kind);
    }

    private void OnFrame(float dt)
    {
        if (_session is null || Phase != KitchenPhase.Cooking)
        {
            return;
        }

        _session.Update(dt);
        if (_session.IsFinished)
        {
            return;
        }

        ShowStep();
        var step = _session.Step;
        StepProgress = step.Progress * 100;
        switch (step)
        {
            case MeterStep m:
                Meter = m.Value;
                InZone = m.IsInZone;
                if (!m.HoldToFill)
                {
                    KitchenStage?.SetHeat(m.Value);
                }

                break;
            case RhythmStep r:
                Marker = r.Marker;
                InZone = r.Marker >= r.ZoneMin && r.Marker <= r.ZoneMax;
                break;
        }
    }

    /// <summary>The big button: tap, stop the heat, or hit the rhythm.</summary>
    [RelayCommand]
    private void Action()
    {
        if (_session is null || Phase != KitchenPhase.Cooking)
        {
            return;
        }

        var kind = _session.Step.Kind;
        if (kind == StepKind.Pour)
        {
            return; // Pour uses press and release, see BeginHold/EndHold.
        }

        if (kind == StepKind.Tap)
        {
            KitchenStage?.Chop();
            Host.Sfx.Play(Sound.Tap, 0.9f + (Random.Shared.NextSingle() * 0.3f));
        }
        else if (kind == StepKind.Rhythm)
        {
            Host.Sfx.Play(InZone ? Sound.Pop : Sound.Oops);
        }

        _session.Tap();
        AfterInput();
    }

    public void BeginHold()
    {
        if (_session is { IsFinished: false } && _session.Step.Kind == StepKind.Pour)
        {
            _session.SetHolding(true);
            Host.Sfx.Play(Sound.Sizzle, 1.4f);
        }
    }

    public void EndHold()
    {
        if (_session is { IsFinished: false } && _session.Step.Kind == StepKind.Pour)
        {
            _session.SetHolding(false);
            AfterInput();
        }
    }

    [RelayCommand]
    private void Pick(PickItem item)
    {
        if (_session is null || item.IsPicked)
        {
            return;
        }

        if (_session.Pick(item.Id))
        {
            item.IsPicked = true;
            KitchenStage?.DropIngredient(item.Color);
            Host.Sfx.Play(Sound.Pop);
        }
        else
        {
            item.IsWrong = true;
            Host.Sfx.Play(Sound.Oops);
            DispatcherTimer.RunOnce(() => item.IsWrong = false, TimeSpan.FromMilliseconds(600));
        }

        AfterInput();
    }

    /// <summary>A topping placed on the plate area at (x, y), both 0-1.</summary>
    public void PlaceTopping(double x, double y)
    {
        if (_session is null || _session.Step.Kind != StepKind.Decorate)
        {
            return;
        }

        var def = _session.Step.Definition;
        var color = def.Item is { } id ? Game.Content.IngredientById[id].Color : _recipe.Color;
        KitchenStage?.PlaceTopping((float)x, (float)y, color);
        Host.Sfx.Play(Sound.Pop, 1.1f + (Random.Shared.NextSingle() * 0.3f));
        _session.Tap((float)x, (float)y);
        AfterInput();
    }

    private void AfterInput()
    {
        if (_session is { IsFinished: false })
        {
            ShowStep();
            StepProgress = _session.Step.Progress * 100;
        }
    }

    private void OnStepFinished(int index, float score)
    {
        if (index < StepDots.Count)
        {
            StepDots[index] = true;
        }

        Feedback = score >= 0.95f ? L["feedback.perfect"] : score >= 0.7f ? L["feedback.great"] : score >= 0.45f ? L["feedback.good"] : L["feedback.oops"];
        FeedbackPulse++;
        Host.Sfx.Play(score >= 0.95f ? Sound.Perfect : score >= 0.45f ? Sound.Good : Sound.Oops);
        KitchenStage?.SetHeat(0);
    }

    private void OnFinished(CookingResult result)
    {
        var reward = Game.Kitchen.Complete(result);
        Host.Store.LogCooking(_recipe.Id, result.Stars, result.Score, Game.Clock.UtcNow);
        Host.Touch();
        KitchenStage?.Serve(_recipe, result.Stars, result.Burnt);
        OverallProgress = 100;

        Stars = result.Stars;
        Burnt = result.Burnt;
        ResultTitle = result.Stars switch
        {
            3 => L["result.amazing"],
            2 => L["result.yummy"],
            _ => L["result.niceTry"],
        };
        RewardCoins = $"+{reward.Coins}";
        RewardXp = $"+{reward.Xp} XP";
        ResultNote = result.Burnt
            ? L["result.burnt"]
            : reward.FirstTime ? L["result.firstTime"] : reward.NewBest ? L["result.newBest"] : L["result.onCounter"];

        // Let the dish land on the plate before the card covers the counter.
        DispatcherTimer.RunOnce(() =>
        {
            Phase = KitchenPhase.Result;
            Host.Sfx.Play(result.Stars == 3 ? Sound.LevelUp : Sound.Good);
        }, TimeSpan.FromSeconds(1.3));
    }

    private void OnBurnt() => Host.Sfx.Play(Sound.Oops, 0.7f);

    [RelayCommand]
    private void CookAgain()
    {
        Phase = KitchenPhase.Prep;
        KitchenStage?.ResetDish();
        RefreshPantry();
    }

    [RelayCommand]
    private void Serve() => Shell.Navigate("restaurant");

    [RelayCommand]
    private void Back() => Shell.Navigate("map");
}
