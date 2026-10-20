using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.State;

namespace HappyCooking.Core.Systems;

/// <summary>Trophies for lifetime milestones, checked after every game event.</summary>
public sealed class AchievementSystem
{
    private readonly GameContent _content;
    private readonly PlayerProfile _profile;
    private readonly EconomySystem _economy;
    private readonly GameEvents _events;

    public AchievementSystem(GameContent content, PlayerProfile profile, EconomySystem economy, GameEvents events)
    {
        _content = content;
        _profile = profile;
        _economy = economy;
        _events = events;
        events.Subscribe(OnEvent);
    }

    public bool IsUnlocked(string achievementId) => _profile.Achievements.Contains(achievementId);

    public int Value(AchievementStat stat) => stat switch
    {
        AchievementStat.DishesCooked => _profile.Stats.DishesCooked,
        AchievementStat.ThreeStarDishes => _profile.Stats.ThreeStarDishes,
        AchievementStat.CustomersServed => _profile.Stats.CustomersServed,
        AchievementStat.FishCaught => _profile.Stats.FishCaught,
        AchievementStat.CropsHarvested => _profile.Stats.CropsHarvested,
        AchievementStat.PetsAdopted => _profile.Pets.Count,
        AchievementStat.RegionsUnlocked => _economy.UnlockedRegionCount,
        AchievementStat.RecipesDiscovered => _profile.BestStars.Count,
        AchievementStat.CreativeDishes => _profile.Stats.CreativeDishes,
        AchievementStat.DecorationsPlaced => _profile.Decorations.Count,
        AchievementStat.GiftsSent => _profile.Stats.GiftsSent,
        AchievementStat.Level => _economy.Level,
        _ => 0,
    };

    public float ProgressFraction(Achievement a) => Math.Clamp((float)Value(a.Stat) / Math.Max(1, a.Threshold), 0f, 1f);

    /// <summary>Unlocks everything whose threshold is met. Safe to call any time.</summary>
    public void CheckAll()
    {
        foreach (var a in _content.Achievements)
        {
            if (!IsUnlocked(a.Id) && Value(a.Stat) >= a.Threshold)
            {
                _profile.Achievements.Add(a.Id);
                _economy.AddGems(a.RewardGems);
                _events.Raise(new GameEvent(GameEventKind.AchievementUnlocked, a.Id));
            }
        }
    }

    private void OnEvent(GameEvent e)
    {
        if (e.Kind != GameEventKind.AchievementUnlocked)
        {
            CheckAll();
        }
    }
}
