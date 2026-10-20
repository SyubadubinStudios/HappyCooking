using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using HappyCooking.App.Services;
using HappyCooking.Core.Content;

namespace HappyCooking.App.ViewModels;

public sealed class TrophyCard(Achievement a, bool unlocked, float progress, int value)
{
    public string Title { get; } = Loc.Instance.Get(a.Title);

    public string Description { get; } = Loc.Instance.Get(a.Description);

    public Bitmap? Icon { get; } = Images.Icon(a.Icon);

    public bool Unlocked { get; } = unlocked;

    public double Opacity => Unlocked ? 1 : 0.45;

    public double Progress { get; } = progress * 100;

    public string ProgressText { get; } = unlocked ? Loc.Instance["trophy.done"] : $"{Math.Min(value, a.Threshold)}/{a.Threshold}";

    public string Reward { get; } = $"+{a.RewardGems}";
}

public sealed class StatTile(string label, string value, string icon)
{
    public string Label { get; } = label;

    public string Value { get; } = value;

    public Bitmap? Icon { get; } = Images.Icon(icon);
}

/// <summary>Trophies, story quests and lifetime stats in one place.</summary>
public sealed class TrophiesViewModel : PageViewModel
{
    public TrophiesViewModel() => Build();

    public override string DockKey => "trophies";

    public ObservableCollection<TrophyCard> Trophies { get; } = [];

    public ObservableCollection<QuestCard> Quests { get; } = [];

    public ObservableCollection<StatTile> Stats { get; } = [];

    public string Summary { get; private set; } = string.Empty;

    public Bitmap? GemIcon { get; } = Images.Icon("gem");

    private void Build()
    {
        Trophies.Clear();
        var ach = Game.Achievements;
        foreach (var a in Game.Content.Achievements)
        {
            Trophies.Add(new TrophyCard(a, ach.IsUnlocked(a.Id), ach.ProgressFraction(a), ach.Value(a.Stat)));
        }

        Quests.Clear();
        foreach (var q in Game.Quests.Active)
        {
            Quests.Add(new QuestCard(q, Game.Quests.ProgressFraction(q), Game.Quests.Progress(q)));
        }

        var s = Game.Profile.Stats;
        Stats.Clear();
        Stats.Add(new StatTile(L["stat.dishes"], s.DishesCooked.ToString(System.Globalization.CultureInfo.CurrentCulture), "kitchen"));
        Stats.Add(new StatTile(L["stat.stars"], s.TotalStars.ToString(System.Globalization.CultureInfo.CurrentCulture), "star"));
        Stats.Add(new StatTile(L["stat.customers"], s.CustomersServed.ToString(System.Globalization.CultureInfo.CurrentCulture), "restaurant"));
        Stats.Add(new StatTile(L["stat.fish"], s.FishCaught.ToString(System.Globalization.CultureInfo.CurrentCulture), "fishing"));
        Stats.Add(new StatTile(L["stat.harvests"], s.CropsHarvested.ToString(System.Globalization.CultureInfo.CurrentCulture), "farm"));
        Stats.Add(new StatTile(L["stat.coins"], s.CoinsEarned.ToString("N0", System.Globalization.CultureInfo.CurrentCulture), "coin"));

        Summary = L.Format("trophy.summary", Game.Profile.Achievements.Count, Game.Content.Achievements.Count, Game.Profile.CompletedQuests.Count, Game.Content.Quests.Count);
        OnPropertyChanged(nameof(Summary));
    }

    public override void RefreshTexts() => Build();
}
