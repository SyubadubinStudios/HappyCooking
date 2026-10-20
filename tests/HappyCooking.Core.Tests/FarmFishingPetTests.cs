using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.Systems;

namespace HappyCooking.Core.Tests;

public class FarmTests
{
    [Fact]
    public void NewGame_HasStartingPlots_AllEmpty()
    {
        var g = TestGame.Create();
        Assert.Equal(FarmSystem.StartingPlots, g.Farm.Plots.Count);
        Assert.All(g.Farm.Plots, p => Assert.Null(p.Crop));
        Assert.Equal(0f, g.Farm.Growth(0));
        Assert.Equal(TimeSpan.Zero, g.Farm.TimeLeft(0));
    }

    [Fact]
    public void Plant_Grow_Harvest_OverClockTime()
    {
        var clock = new ManualClock(TestGame.Noon);
        var g = TestGame.Create(clock: clock);
        var crop = g.Content.CropById["tomato"];
        var coins = g.Economy.Coins;
        var before = g.Inventory.Count(crop.Ingredient);

        Assert.True(g.Farm.TryPlant(0, "tomato"));
        Assert.Equal(coins - crop.SeedPrice, g.Economy.Coins);
        Assert.Equal(TimeSpan.FromSeconds(crop.GrowSeconds), g.Farm.TimeLeft(0));

        clock.Advance(TimeSpan.FromSeconds(crop.GrowSeconds / 2.0));
        Assert.Equal(0.5f, g.Farm.Growth(0), 3);
        Assert.False(g.Farm.IsReady(0));
        Assert.Null(g.Farm.TryHarvest(0));

        clock.Advance(TimeSpan.FromSeconds(crop.GrowSeconds));
        Assert.Equal(1f, g.Farm.Growth(0));
        Assert.True(g.Farm.IsReady(0));

        var events = new List<GameEvent>();
        g.Events.Subscribe(events.Add);
        var harvested = g.Farm.TryHarvest(0);

        Assert.Same(crop, harvested);
        Assert.Equal(before + crop.Yield, g.Inventory.Count(crop.Ingredient));
        Assert.Null(g.Farm.Plots[0].Crop);
        Assert.Equal(1, g.Profile.Stats.CropsHarvested);
        Assert.Contains(events, e => e.Kind == GameEventKind.CropHarvested && e.Id == "tomato" && e.Amount == crop.Yield);
        Assert.Null(g.Farm.TryHarvest(0)); // already empty
    }

    [Fact]
    public void Plant_FailsOnOccupiedPlot_BadIndex_UnknownCrop_OrNoCoins()
    {
        var g = TestGame.Create();
        Assert.True(g.Farm.TryPlant(0, "chili"));
        Assert.False(g.Farm.TryPlant(0, "tomato"));
        Assert.Equal("chili", g.Farm.Plots[0].Crop);
        Assert.False(g.Farm.TryPlant(-1, "chili"));
        Assert.False(g.Farm.TryPlant(g.Farm.Plots.Count, "chili"));
        Assert.False(g.Farm.TryPlant(1, "mandrake"));
        g.Profile.Coins = 0;
        Assert.False(g.Farm.TryPlant(1, "chili"));
        Assert.Null(g.Farm.Plots[1].Crop);
    }

    [Fact]
    public void HarvestAll_HarvestsOnlyReadyPlots()
    {
        var clock = new ManualClock(TestGame.Noon);
        var g = TestGame.Create(clock: clock);
        g.Farm.TryPlant(0, "green_onion"); // 35 s
        g.Farm.TryPlant(1, "strawberry"); // 120 s
        clock.Advance(TimeSpan.FromSeconds(40));
        Assert.Equal(1, g.Farm.HarvestAll());
        Assert.Null(g.Farm.Plots[0].Crop);
        Assert.Equal("strawberry", g.Farm.Plots[1].Crop);
    }

    [Fact]
    public void BuyPlot_PriceRises_UpToMax()
    {
        var g = TestGame.Create();
        g.Profile.Coins = 100_000;
        var expectedPrice = 50 + (FarmSystem.StartingPlots * 25);
        Assert.Equal(expectedPrice, g.Farm.NextPlotPrice);
        Assert.True(g.Farm.TryBuyPlot());
        Assert.Equal(100_000 - expectedPrice, g.Profile.Coins);
        Assert.Equal(FarmSystem.StartingPlots + 1, g.Farm.Plots.Count);
        Assert.True(g.Farm.NextPlotPrice > expectedPrice);

        while (g.Farm.Plots.Count < FarmSystem.MaxPlots)
        {
            Assert.True(g.Farm.TryBuyPlot());
        }

        Assert.False(g.Farm.TryBuyPlot());
        Assert.Equal(FarmSystem.MaxPlots, g.Farm.Plots.Count);
    }

    [Fact]
    public void BuyPlot_FailsWithoutCoins()
    {
        var g = TestGame.Create();
        g.Profile.Coins = 0;
        Assert.False(g.Farm.TryBuyPlot());
        Assert.Equal(FarmSystem.StartingPlots, g.Farm.Plots.Count);
    }

    [Fact]
    public void GrowthPet_SpeedsUpCrops()
    {
        var clock = new ManualClock(TestGame.Noon);
        var g = TestGame.Create(clock: clock);
        g.Profile.Level = 2;
        Assert.True(g.Pets.TryAdopt("boba")); // Growth +25%
        Assert.True(g.Farm.TryPlant(0, "tomato")); // 60 s
        clock.Advance(TimeSpan.FromSeconds(48));
        Assert.True(g.Farm.IsReady(0));
    }

    [Fact]
    public void Rain_BanksBonusGrowth_DryWeatherDoesNot()
    {
        var clock = new ManualClock(TestGame.Noon);
        var g = TestGame.Create(clock: clock);
        TestGame.Guard(() => g.World.WeatherIn("nusantara") == Weather.Rain, () => clock.Advance(TimeSpan.FromMinutes(6)));
        g.Farm.TryPlant(0, "rice");
        g.Farm.Update(10f);
        Assert.Equal(5f, g.Farm.Plots[0].BonusSeconds, 3);
        Assert.Equal(0f, g.Farm.Plots[1].BonusSeconds); // empty plots get nothing

        TestGame.Guard(() => g.World.WeatherIn("nusantara") != Weather.Rain, () => clock.Advance(TimeSpan.FromMinutes(6)));
        g.Farm.Update(10f);
        Assert.Equal(5f, g.Farm.Plots[0].BonusSeconds, 3);
    }
}

public class FishingTests
{
    private static (GameSession Game, FishingSession Session) Start(DateTimeOffset? at = null, string region = "nusantara", int seed = 42)
    {
        var g = TestGame.Create(seed, new ManualClock(at ?? TestGame.Noon));
        return (g, g.Fishing.StartSession(g.Content.RegionById[region]));
    }

    [Fact]
    public void WorldClock_TestTimesAreDayAndNight()
    {
        Assert.Equal(0.5f, new WorldSystem(new ManualClock(TestGame.Noon)).TimeOfDay, 3);
        Assert.False(new WorldSystem(new ManualClock(TestGame.Noon)).IsNight);
        Assert.True(new WorldSystem(new ManualClock(TestGame.Midnight)).IsNight);
        Assert.Equal(1f, new WorldSystem(new ManualClock(TestGame.Noon)).Daylight, 2);
        Assert.Equal(0f, new WorldSystem(new ManualClock(TestGame.Midnight)).Daylight);
    }

    [Fact]
    public void FullFlow_Cast_Wait_Bite_Reel_Caught()
    {
        var (g, s) = Start();
        Fish? caught = null;
        s.Caught += f => caught = f;
        var events = new List<GameEvent>();
        g.Events.Subscribe(events.Add);

        Assert.Equal(FishingPhase.Idle, s.Phase);
        s.Tap(); // casts
        Assert.Equal(FishingPhase.Waiting, s.Phase);
        s.Update(1.4f); // minimum wait is 1.5 s
        Assert.Equal(FishingPhase.Waiting, s.Phase);
        s.Update(3.2f);
        Assert.Equal(FishingPhase.Bite, s.Phase);
        Assert.NotNull(s.Hooked);
        Assert.Contains(s.Hooked!.Id, s.Region.Fish);
        Assert.Equal(1f, s.TimerFraction, 3);

        s.Tap(); // hook it
        Assert.Equal(FishingPhase.Reeling, s.Phase);
        var fish = s.Hooked!;
        var required = s.ReelRequired;
        Assert.Equal(5 + ((int)fish.Rarity * 3), required);
        var invBefore = g.Inventory.Count(fish.Ingredient);
        for (var i = 0; i < required - 1; i++)
        {
            s.Tap();
        }

        Assert.Equal(FishingPhase.Reeling, s.Phase);
        s.Tap();

        Assert.Equal(FishingPhase.Caught, s.Phase);
        Assert.Same(fish, caught);
        Assert.Equal(1, g.Fishing.TimesCaught(fish.Id));
        Assert.Equal(1, g.Profile.Stats.FishCaught);
        Assert.Equal(invBefore + 1, g.Inventory.Count(fish.Ingredient));
        Assert.Contains(events, e => e.Kind == GameEventKind.FishCaught && e.Id == fish.Id);

        s.Tap(); // casts again
        Assert.Equal(FishingPhase.Waiting, s.Phase);
        Assert.Null(s.Hooked);
    }

    [Fact]
    public void TappingWhileWaiting_FishEscapes()
    {
        var (g, s) = Start();
        s.Cast();
        s.Tap();
        Assert.Equal(FishingPhase.Escaped, s.Phase);
        Assert.Equal(0, g.Profile.Stats.FishCaught);
    }

    [Fact]
    public void MissingTheBiteWindow_FishEscapes()
    {
        var (_, s) = Start();
        s.Cast();
        s.Update(5f);
        Assert.Equal(FishingPhase.Bite, s.Phase);
        s.Update(FishingSession.BiteWindowSeconds + 0.01f);
        Assert.Equal(FishingPhase.Escaped, s.Phase);
    }

    [Fact]
    public void ReelingTooSlowly_FishEscapes()
    {
        var (g, s) = Start();
        s.Cast();
        s.Update(5f);
        s.Tap();
        s.Tap();
        s.Update(FishingSession.ReelSeconds + 0.01f);
        Assert.Equal(FishingPhase.Escaped, s.Phase);
        Assert.Equal(0, g.Profile.Stats.FishCaught);
    }

    [Fact]
    public void Cast_IsIgnoredMidFlow()
    {
        var (_, s) = Start();
        s.Cast();
        s.Update(5f);
        var hooked = s.Hooked;
        s.Cast();
        Assert.Equal(FishingPhase.Bite, s.Phase);
        Assert.Same(hooked, s.Hooked);
    }

    [Theory]
    [InlineData("nusantara")]
    [InlineData("sakura")]
    [InlineData("pasta")]
    public void RollFish_NeverReturnsNightOnlyFishDuringTheDay(string regionId)
    {
        var g = TestGame.Create(1, new ManualClock(TestGame.Noon));
        Assert.False(g.World.IsNight);
        var region = g.Content.RegionById[regionId];
        Assert.Contains(region.Fish, f => g.Content.FishById[f].NightOnly); // the test is meaningful
        var rng = new GameRandom(5);
        for (var i = 0; i < 5000; i++)
        {
            Assert.False(g.Fishing.RollFish(region, rng).NightOnly);
        }
    }

    [Fact]
    public void RollFish_NightOnlyFishCanBiteAtNight()
    {
        var g = TestGame.Create(1, new ManualClock(TestGame.Midnight));
        Assert.True(g.World.IsNight);
        var region = g.Content.RegionById["nusantara"];
        var rng = new GameRandom(5);
        var seen = new HashSet<string>();
        for (var i = 0; i < 5000; i++)
        {
            seen.Add(g.Fishing.RollFish(region, rng).Id);
        }

        Assert.Contains("galaxy_fish", seen);
        Assert.True(seen.IsSubsetOf(region.Fish));
    }

    [Fact]
    public void Weight_LuckFavoursRareFish()
    {
        Assert.Equal(60f, FishingSystem.Weight(Rarity.Common, 1f));
        Assert.Equal(25f, FishingSystem.Weight(Rarity.Uncommon, 1f));
        Assert.True(FishingSystem.Weight(Rarity.Rare, 0.5f) > FishingSystem.Weight(Rarity.Rare, 0f));
        Assert.True(FishingSystem.Weight(Rarity.Legendary, 0.5f) > FishingSystem.Weight(Rarity.Legendary, 0f));
    }
}

public class PetTests
{
    [Fact]
    public void Adopt_ChargesPrice_BecomesActive_RaisesEvent()
    {
        var g = TestGame.Create();
        var kiwi = g.Content.PetById["kiwi"];
        var coins = g.Economy.Coins;
        var adopted = new List<GameEvent>();
        g.Events.Subscribe(e =>
        {
            if (e.Kind == GameEventKind.PetAdopted)
            {
                adopted.Add(e);
            }
        });

        Assert.True(g.Pets.CanAdopt(kiwi));
        Assert.True(g.Pets.TryAdopt("kiwi"));
        Assert.Equal(coins - kiwi.Price, g.Economy.Coins);
        Assert.True(g.Pets.Owns("kiwi"));
        Assert.Same(kiwi, g.Pets.ActivePet);
        Assert.Equal(90f, g.Pets.Happiness("kiwi"));
        Assert.Single(adopted);
        Assert.False(g.Pets.TryAdopt("kiwi")); // already owned
    }

    [Fact]
    public void Adopt_RespectsLevelAndCurrency()
    {
        var g = TestGame.Create();
        Assert.False(g.Pets.TryAdopt("sambal")); // level 2
        Assert.False(g.Pets.TryAdopt("nobody"));

        g.Profile.Level = 5; // draco unlocks at 5 (6 would also unlock a_regions_all and pay gems)
        g.Profile.Coins = 10_000;
        g.Profile.Gems = 0;
        Assert.False(g.Pets.TryAdopt("draco")); // costs gems
        g.Profile.Gems = 30;
        Assert.True(g.Pets.TryAdopt("draco"));
        Assert.Equal(0, g.Profile.Gems);
        Assert.Equal(10_000, g.Profile.Coins);

        g.Profile.Coins = 10;
        Assert.False(g.Pets.TryAdopt("sambal"));
    }

    [Fact]
    public void SecondPet_DoesNotReplaceActive_UntilSetActive()
    {
        var g = TestGame.Create();
        g.Profile.Level = 2;
        g.Profile.Coins = 1000;
        g.Pets.TryAdopt("kiwi");
        g.Pets.TryAdopt("boba");
        Assert.Equal("kiwi", g.Pets.ActivePet!.Id);
        Assert.True(g.Pets.SetActive("boba"));
        Assert.Equal("boba", g.Pets.ActivePet!.Id);
        Assert.False(g.Pets.SetActive("stella"));
        Assert.Equal(2, g.Pets.Owned.Count());
    }

    [Fact]
    public void Happiness_DecaysOverTime_AndFloorsAtZero()
    {
        var clock = new ManualClock(TestGame.Noon);
        var g = TestGame.Create(clock: clock);
        g.Pets.TryAdopt("kiwi");
        clock.Advance(TimeSpan.FromHours(5));
        Assert.Equal(90f - (5 * PetSystem.DecayPerHour), g.Pets.Happiness("kiwi"), 3);
        clock.Advance(TimeSpan.FromDays(10));
        Assert.Equal(0f, g.Pets.Happiness("kiwi"));
        Assert.Equal(0f, g.Pets.Happiness("not_owned"));
    }

    [Fact]
    public void Feed_RestoresHappiness_CostsCoins_RaisesEvent()
    {
        var clock = new ManualClock(TestGame.Noon);
        var g = TestGame.Create(clock: clock);
        g.Pets.TryAdopt("kiwi");
        clock.Advance(TimeSpan.FromHours(10)); // 90 - 60 = 30
        var coins = g.Economy.Coins;
        var fed = 0;
        g.Events.Subscribe(e => fed += e.Kind == GameEventKind.PetFed ? 1 : 0);

        Assert.True(g.Pets.TryFeed("kiwi"));
        Assert.Equal(55f, g.Pets.Happiness("kiwi"), 3);
        Assert.Equal(coins - PetSystem.FeedCost, g.Economy.Coins);
        Assert.Equal(1, g.Profile.Pets["kiwi"].TimesFed);
        Assert.Equal(1, fed);

        for (var i = 0; i < 5; i++)
        {
            g.Pets.TryFeed("kiwi");
        }

        Assert.Equal(100f, g.Pets.Happiness("kiwi")); // capped

        g.Profile.Coins = 0;
        Assert.False(g.Pets.TryFeed("kiwi"));
        Assert.False(g.Pets.TryFeed("boba"));
    }

    [Fact]
    public void Bonus_OnlyWhenActivePetIsHappy_AndKindMatches()
    {
        var clock = new ManualClock(TestGame.Noon);
        var g = TestGame.Create(clock: clock);
        Assert.Equal(0f, g.Pets.Bonus(PetBonus.Tips)); // no pet
        g.Pets.TryAdopt("kiwi"); // Tips 0.2
        Assert.Equal(0.2f, g.Pets.Bonus(PetBonus.Tips), 3);
        Assert.Equal(0f, g.Pets.Bonus(PetBonus.Growth)); // wrong kind

        clock.Advance(TimeSpan.FromHours(10)); // exactly 30: still happy
        Assert.Equal(0.2f, g.Pets.Bonus(PetBonus.Tips), 3);
        clock.Advance(TimeSpan.FromHours(1)); // 24: sad
        Assert.Equal(0f, g.Pets.Bonus(PetBonus.Tips));

        g.Pets.TryFeed("kiwi");
        Assert.Equal(0.2f, g.Pets.Bonus(PetBonus.Tips), 3);
    }

    [Fact]
    public void CookingZonePet_WidensKitchenZones()
    {
        var g = TestGame.Create();
        g.Profile.Level = 2;
        g.Profile.Coins = 1000;
        Assert.True(g.Pets.TryAdopt("sambal"));
        var recipe = g.Content.RecipeById["nasi_goreng"];
        var withPet = g.Kitchen.Start(recipe)!;
        var plain = new CookingSession(recipe, g.Content, new GameRandom(1));
        while (withPet.Step is not MeterStep)
        {
            TestGame.PlayStepPerfectly(withPet);
            TestGame.PlayStepPerfectly(plain);
        }

        var a = (MeterStep)plain.Step;
        var b = (MeterStep)withPet.Step;
        Assert.True(b.ZoneMax - b.ZoneMin > a.ZoneMax - a.ZoneMin);
    }
}
