using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.Systems;

namespace HappyCooking.Core.Tests;

public class QuestTests
{
    private static void Cook(GameSession g, string recipeId, int stars = 3) =>
        g.Kitchen.Complete(new CookingResult(g.Content.RecipeById[recipeId], 1f, stars, false, 10f));

    private static Quest Q(GameSession g, string id) => g.Content.QuestById[id];

    [Fact]
    public void NewGame_OnlyRootQuestsAreActive()
    {
        var g = TestGame.Create();
        Assert.All(g.Quests.Active, q => Assert.Null(q.Requires));
        Assert.Contains(g.Quests.Active, q => q.Id == "q_first_dish");
        Assert.False(g.Quests.IsAvailable(Q(g, "q_first_customers")));
    }

    [Fact]
    public void CookRecipeQuest_CompletesAndPaysRewards()
    {
        var g = TestGame.Create();
        var quest = Q(g, "q_first_dish");
        var completed = new List<string?>();
        g.Events.Subscribe(e =>
        {
            if (e.Kind == GameEventKind.QuestCompleted)
            {
                completed.Add(e.Id);
            }
        });
        var coins = g.Economy.Coins;
        var gems = g.Economy.Gems;

        Cook(g, "sate"); // wrong dish: no progress
        Assert.False(g.Quests.IsCompleted(quest.Id));
        Assert.Equal(0, g.Quests.Progress(quest));
        var coinsAfterSate = g.Economy.Coins;
        var gemsAfterSate = g.Economy.Gems;

        var dish = g.Kitchen.Complete(new CookingResult(g.Content.RecipeById["nasi_goreng"], 1f, 3, false, 10f));

        Assert.True(g.Quests.IsCompleted(quest.Id));
        Assert.Contains(quest.Id, completed);
        Assert.Equal(coinsAfterSate + dish.Coins + quest.RewardCoins, g.Economy.Coins);
        Assert.Equal(gemsAfterSate + quest.RewardGems, g.Economy.Gems);
        Assert.False(g.Profile.QuestProgress.ContainsKey(quest.Id));
        Assert.DoesNotContain(g.Quests.Active, q => q.Id == quest.Id);
        Assert.True(g.Economy.Coins > coins && g.Economy.Gems > gems);
    }

    [Fact]
    public void QuestXpReward_IsPaid()
    {
        var g = TestGame.Create();
        var quest = Q(g, "q_first_dish");
        var dishXp = g.Kitchen.Complete(new CookingResult(g.Content.RecipeById["nasi_goreng"], 1f, 3, false, 10f)).Xp;
        var total = (g.Economy.Level == 1 ? 0 : EconomySystem.XpForNextLevel(1)) + g.Economy.Xp;
        Assert.Equal(dishXp + quest.RewardXp, total);
    }

    [Fact]
    public void ChainedQuests_BecomeAvailable_AfterPrerequisite()
    {
        var g = TestGame.Create();
        Cook(g, "nasi_goreng");
        foreach (var id in new[] { "q_first_customers", "q_little_farm", "q_gone_fishing" })
        {
            Assert.True(g.Quests.IsAvailable(Q(g, id)), id);
        }

        Assert.False(g.Quests.IsAvailable(Q(g, "q_rendang")));
    }

    [Fact]
    public void ServeCustomersQuest_ProgressesPerEvent_ThenUnlocksNext()
    {
        var g = TestGame.Create();
        Cook(g, "nasi_goreng");
        var quest = Q(g, "q_first_customers");
        g.Profile.Counter["nasi_goreng"] = 10;

        for (var i = 1; i < quest.Amount; i++)
        {
            g.Restaurant.TryServe(g.Restaurant.TrySpawn()!);
            Assert.Equal(i, g.Quests.Progress(quest));
            Assert.Equal((float)i / quest.Amount, g.Quests.ProgressFraction(quest), 3);
        }

        g.Restaurant.TryServe(g.Restaurant.TrySpawn()!);
        Assert.True(g.Quests.IsCompleted(quest.Id));
        Assert.True(g.Quests.IsAvailable(Q(g, "q_rendang")));
        Assert.True(g.Quests.IsAvailable(Q(g, "q_pet_friend")));
        Assert.True(g.Quests.IsAvailable(Q(g, "q_decorate")));
    }

    [Fact]
    public void ProgressIsOnlyCountedForActiveQuests()
    {
        var g = TestGame.Create();
        g.Profile.BestStars["nasi_goreng"] = 3;
        g.Profile.Counter["nasi_goreng"] = 1;
        g.Restaurant.TryServe(g.Restaurant.TrySpawn()!); // q_first_customers is still locked
        Assert.Equal(0, g.Quests.Progress(Q(g, "q_first_customers")));
    }

    [Fact]
    public void CookInRegionQuest_CountsOnlyThatRegion()
    {
        var g = TestGame.Create();
        g.Profile.CompletedQuests.UnionWith(["q_first_dish", "q_first_customers", "q_rendang"]);
        var quest = Q(g, "q_nusantara_tour");
        Assert.True(g.Quests.IsAvailable(quest));
        g.Profile.Level = 2;
        Cook(g, "sushi");
        Assert.Equal(0, g.Quests.Progress(quest));
        Cook(g, "sate");
        Cook(g, "nasi_goreng");
        Assert.Equal(2, g.Quests.Progress(quest));
    }

    [Fact]
    public void EarnStarsQuest_AddsStarsPerDish()
    {
        var g = TestGame.Create();
        g.Profile.CompletedQuests.UnionWith(["q_first_dish", "q_first_customers", "q_rendang", "q_nusantara_tour"]);
        var quest = Q(g, "q_star_chef");
        Cook(g, "sate", 2);
        Cook(g, "sate", 3);
        Assert.Equal(5, g.Quests.Progress(quest));
    }

    [Fact]
    public void ReachLevelQuest_TracksLevel_AndCompletesOnLevelUp()
    {
        var g = TestGame.Create();
        g.Profile.CompletedQuests.Add("q_dessert");
        var quest = Q(g, "q_master");
        Assert.Equal(1, g.Quests.Progress(quest));

        var xp = 0;
        for (var l = 1; l < quest.Amount; l++)
        {
            xp += EconomySystem.XpForNextLevel(l);
        }

        var gems = g.Economy.Gems;
        g.Economy.AddXp(xp);
        Assert.True(g.Quests.IsCompleted(quest.Id));
        Assert.True(g.Economy.Gems >= gems + quest.RewardGems);
    }

    [Fact]
    public void AdoptAndFeedPetQuests()
    {
        var g = TestGame.Create();
        g.Profile.CompletedQuests.UnionWith(["q_first_dish", "q_first_customers"]);
        g.Profile.Coins = 1000;
        g.Pets.TryAdopt("kiwi");
        Assert.True(g.Quests.IsCompleted("q_pet_friend"));
        var feed = Q(g, "q_feed_pet");
        for (var i = 0; i < feed.Amount; i++)
        {
            g.Pets.TryFeed("kiwi");
        }

        Assert.True(g.Quests.IsCompleted(feed.Id));
    }
}

public class AchievementTests
{
    [Fact]
    public void NewGame_HasNoAchievements()
    {
        var g = TestGame.Create();
        Assert.Empty(g.Profile.Achievements);
    }

    [Fact]
    public void FirstDish_UnlocksAchievement_OnceWithGems()
    {
        var g = TestGame.Create();
        var a = g.Content.AchievementById["a_first_dish"];
        var unlocked = new List<string?>();
        g.Events.Subscribe(e =>
        {
            if (e.Kind == GameEventKind.AchievementUnlocked)
            {
                unlocked.Add(e.Id);
            }
        });
        var quest = g.Content.QuestById["q_first_dish"];
        var gems = g.Economy.Gems;

        g.Kitchen.Complete(new CookingResult(g.Content.RecipeById["nasi_goreng"], 1f, 3, false, 10f));

        Assert.True(g.Achievements.IsUnlocked(a.Id));
        Assert.Equal(gems + a.RewardGems + quest.RewardGems, g.Economy.Gems);
        Assert.Single(unlocked, a.Id);

        g.Kitchen.Complete(new CookingResult(g.Content.RecipeById["nasi_goreng"], 1f, 3, false, 10f));
        Assert.Single(unlocked, a.Id);
    }

    [Fact]
    public void Progress_ReflectsStat()
    {
        var g = TestGame.Create();
        var a = g.Content.AchievementById["a_dishes_25"];
        g.Profile.Stats.DishesCooked = 5;
        Assert.Equal(5, g.Achievements.Value(AchievementStat.DishesCooked));
        Assert.Equal(0.2f, g.Achievements.ProgressFraction(a), 3);
        Assert.Equal(1, g.Achievements.Value(AchievementStat.RegionsUnlocked));
        Assert.Equal(1, g.Achievements.Value(AchievementStat.Level));
    }

    [Fact]
    public void LoadingAnOldSave_UnlocksAlreadyEarnedAchievements()
    {
        var clock = new ManualClock(TestGame.Noon);
        var profile = GameSession.NewProfile(TestGame.Content, "Old", clock);
        profile.Stats.DishesCooked = 30;
        var g = new GameSession(TestGame.Content, profile, clock, 1);
        Assert.True(g.Achievements.IsUnlocked("a_first_dish"));
        Assert.True(g.Achievements.IsUnlocked("a_dishes_25"));
        Assert.False(g.Achievements.IsUnlocked("a_dishes_100"));
    }

    [Fact]
    public void Level10_Unlocks_ViaLevelUpEvent()
    {
        var g = TestGame.Create();
        g.Economy.AddXp(100_000);
        Assert.True(g.Achievements.IsUnlocked("a_level_10"));
        Assert.True(g.Achievements.IsUnlocked("a_regions_all"));
    }

    [Fact]
    public void Decorations_CountTowardDecorAchievement()
    {
        var g = TestGame.Create();
        g.Profile.Coins = 100_000;
        foreach (var d in g.Content.Decorations.Take(5))
        {
            g.Restaurant.TryBuyDecoration(d.Id);
        }

        Assert.True(g.Achievements.IsUnlocked("a_decor_5"));
    }
}
