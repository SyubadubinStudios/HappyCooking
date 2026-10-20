using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.State;

namespace HappyCooking.Core.Systems;

/// <summary>
/// Story quests from the characters. A quest becomes available once the quest it
/// requires is done, tracks progress from game events, and pays out on completion.
/// </summary>
public sealed class QuestSystem
{
    private readonly GameContent _content;
    private readonly PlayerProfile _profile;
    private readonly EconomySystem _economy;
    private readonly GameEvents _events;

    public QuestSystem(GameContent content, PlayerProfile profile, EconomySystem economy, GameEvents events)
    {
        _content = content;
        _profile = profile;
        _economy = economy;
        _events = events;
        events.Subscribe(OnEvent);
    }

    public bool IsCompleted(string questId) => _profile.CompletedQuests.Contains(questId);

    public bool IsAvailable(Quest quest) =>
        !IsCompleted(quest.Id) && (quest.Requires is null || IsCompleted(quest.Requires));

    public IEnumerable<Quest> Active => _content.Quests.Where(IsAvailable);

    public int Progress(Quest quest) =>
        quest.Goal == QuestGoal.ReachLevel ? _economy.Level : _profile.QuestProgress.GetValueOrDefault(quest.Id);

    public float ProgressFraction(Quest quest) => Math.Clamp((float)Progress(quest) / Math.Max(1, quest.Amount), 0f, 1f);

    private void OnEvent(GameEvent e)
    {
        // Completing a quest raises QuestCompleted, which may unlock and advance another;
        // ToList() keeps that from mutating the sequence we iterate.
        foreach (var quest in Active.ToList())
        {
            if (IsCompleted(quest.Id))
            {
                // Finished by a nested event while this loop was running.
                continue;
            }

            var add = Contribution(quest, e);
            if (add > 0)
            {
                _profile.QuestProgress[quest.Id] = _profile.QuestProgress.GetValueOrDefault(quest.Id) + add;
            }

            if (Progress(quest) >= quest.Amount)
            {
                Complete(quest);
            }
        }
    }

    private static int Contribution(Quest quest, GameEvent e) => (quest.Goal, e.Kind) switch
    {
        (QuestGoal.CookRecipe, GameEventKind.DishCooked) when e.Id == quest.Target => 1,
        (QuestGoal.CookInRegion, GameEventKind.DishCooked) when e.Extra == quest.Target => 1,
        (QuestGoal.CookAny, GameEventKind.DishCooked) => 1,
        (QuestGoal.EarnStars, GameEventKind.DishCooked) => e.Amount,
        (QuestGoal.ServeCustomers, GameEventKind.CustomerServed) => 1,
        (QuestGoal.Harvest, GameEventKind.CropHarvested) => 1,
        (QuestGoal.CatchFish, GameEventKind.FishCaught) => 1,
        (QuestGoal.FeedPet, GameEventKind.PetFed) => 1,
        (QuestGoal.AdoptPet, GameEventKind.PetAdopted) => 1,
        (QuestGoal.PlaceDecoration, GameEventKind.DecorationPlaced) => 1,
        _ => 0,
    };

    private void Complete(Quest quest)
    {
        if (!_profile.CompletedQuests.Add(quest.Id))
        {
            return;
        }

        _profile.QuestProgress.Remove(quest.Id);
        _economy.AddCoins(quest.RewardCoins);
        _economy.AddGems(quest.RewardGems);
        _events.Raise(new GameEvent(GameEventKind.QuestCompleted, quest.Id));
        _economy.AddXp(quest.RewardXp);
    }
}
