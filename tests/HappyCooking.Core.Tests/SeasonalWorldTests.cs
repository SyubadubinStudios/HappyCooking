using HappyCooking.Core.Content;
using HappyCooking.Core.Localization;
using HappyCooking.Core.Services;
using HappyCooking.Core.Systems;

namespace HappyCooking.Core.Tests;

public class SeasonalTests
{
    private static SeasonalEvent Event(int sm, int sd, int em, int ed, params string[] featured) =>
        new("e_test", new LocalizedText("T", "T"), new LocalizedText("D", "D"), sm, sd, em, ed, 2f, featured, "#000000");

    private static ManualClock On(int year, int month, int day) => new(new DateTimeOffset(year, month, day, 0, 12, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(8, 9, false)]
    [InlineData(8, 10, true)] // first day inclusive
    [InlineData(8, 17, true)]
    [InlineData(8, 24, true)] // last day inclusive
    [InlineData(8, 25, false)]
    [InlineData(2, 17, false)]
    public void IsActive_PlainRange(int month, int day, bool active) =>
        Assert.Equal(active, SeasonalSystem.IsActive(Event(8, 10, 8, 24), new DateOnly(2026, month, day)));

    [Theory]
    [InlineData(12, 19, false)]
    [InlineData(12, 20, true)]
    [InlineData(12, 31, true)]
    [InlineData(1, 1, true)]
    [InlineData(1, 5, true)]
    [InlineData(1, 6, false)]
    [InlineData(6, 15, false)]
    public void IsActive_RangeWrappingNewYear(int month, int day, bool active) =>
        Assert.Equal(active, SeasonalSystem.IsActive(Event(12, 20, 1, 5), new DateOnly(2026, month, day)));

    [Theory]
    [InlineData(1, 20, 2, 20)]
    [InlineData(3, 20, 4, 20)]
    public void IsActive_SpansMonthBoundary(int sm, int sd, int em, int ed)
    {
        var ev = Event(sm, sd, em, ed);
        Assert.True(SeasonalSystem.IsActive(ev, new DateOnly(2026, sm, 28)));
        Assert.True(SeasonalSystem.IsActive(ev, new DateOnly(2026, em, 1)));
        Assert.False(SeasonalSystem.IsActive(ev, new DateOnly(2026, em, ed + 1)));
    }

    [Fact]
    public void ActiveEvent_FollowsTheClock()
    {
        Assert.Null(new SeasonalSystem(TestGame.Content, On(2026, 1, 1)).ActiveEvent);
        Assert.Equal("e_merdeka", new SeasonalSystem(TestGame.Content, On(2026, 8, 17)).ActiveEvent?.Id);
        Assert.Equal("e_lunar_new_year", new SeasonalSystem(TestGame.Content, On(2027, 2, 6)).ActiveEvent?.Id);
        Assert.Equal("e_winter", new SeasonalSystem(TestGame.Content, On(2026, 12, 25)).ActiveEvent?.Id);
    }

    [Fact]
    public void CoinMultiplier_OnlyForFeaturedRecipes_DuringEvent()
    {
        var during = new SeasonalSystem(TestGame.Content, On(2026, 8, 17));
        var merdeka = TestGame.Content.Events.Single(e => e.Id == "e_merdeka");
        Assert.Equal(merdeka.CoinMultiplier, during.CoinMultiplier("nasi_goreng"));
        Assert.Equal(1f, during.CoinMultiplier("sushi"));

        var after = new SeasonalSystem(TestGame.Content, On(2026, 9, 1));
        Assert.Equal(1f, after.CoinMultiplier("nasi_goreng"));
    }

    [Fact]
    public void Kitchen_PaysEventBonus_ForFeaturedDish()
    {
        var clock = On(2026, 8, 17);
        var g = TestGame.Create(clock: clock);
        var recipe = g.Content.RecipeById["nasi_goreng"];
        var reward = g.Kitchen.Complete(new CookingResult(recipe, 1f, 3, false, 10f));
        Assert.Equal((int)MathF.Round(recipe.Coins * g.Seasonal.CoinMultiplier(recipe.Id)), reward.Coins);
        Assert.True(reward.Coins > recipe.Coins);
    }

    [Fact]
    public void ContentEvents_DoNotOverlap()
    {
        var date = new DateOnly(2028, 1, 1); // leap year: covers 29 Feb
        for (var i = 0; i < 366; i++, date = date.AddDays(1))
        {
            var active = TestGame.Content.Events.Count(e => SeasonalSystem.IsActive(e, date));
            Assert.True(active <= 1, $"{active} events on {date}");
        }
    }
}

public class WorldTests
{
    [Fact]
    public void TimeOfDay_CyclesEveryInGameDay()
    {
        var clock = new ManualClock(TestGame.Midnight);
        var world = new WorldSystem(clock);
        Assert.Equal(0f, world.TimeOfDay, 4);
        clock.Advance(TimeSpan.FromMinutes(WorldSystem.DayLengthMinutes / 4));
        Assert.Equal(0.25f, world.TimeOfDay, 4);
        clock.Advance(TimeSpan.FromMinutes(WorldSystem.DayLengthMinutes * 3 / 4));
        Assert.Equal(0f, world.TimeOfDay, 4);
    }

    [Fact]
    public void Weather_IsStableWithinABlock_AndDeterministic()
    {
        var clock = new ManualClock(TestGame.Noon);
        var world = new WorldSystem(clock);
        var w = world.WeatherIn("nusantara");
        Assert.Equal(w, new WorldSystem(new ManualClock(TestGame.Noon)).WeatherIn("nusantara"));
        Assert.Equal(w, world.WeatherIn(TestGame.Content.RegionById["nusantara"]));
    }

    [Fact]
    public void Weather_OnlyDessertSnows_AndOthersRain()
    {
        var clock = new ManualClock(TestGame.Noon);
        var world = new WorldSystem(clock);
        var seen = new Dictionary<string, HashSet<Weather>>();
        for (var i = 0; i < 400; i++)
        {
            foreach (var r in TestGame.Content.Regions)
            {
                (seen.TryGetValue(r.Id, out var set) ? set : seen[r.Id] = []).Add(world.WeatherIn(r));
            }

            clock.Advance(TimeSpan.FromMinutes(WorldSystem.WeatherBlockMinutes));
        }

        Assert.Contains(Weather.Snow, seen["dessert"]);
        Assert.DoesNotContain(Weather.Rain, seen["dessert"]);
        Assert.Contains(Weather.Rain, seen["nusantara"]);
        Assert.DoesNotContain(Weather.Snow, seen["nusantara"]);
        Assert.Contains(Weather.Sunny, seen["nusantara"]);
    }
}
