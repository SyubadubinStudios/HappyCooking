using HappyCooking.Core.Localization;
using HappyCooking.Core.Services;
using HappyCooking.Core.State;

namespace HappyCooking.Core.Tests;

public class ProfileTests
{
    private static PlayerProfile RichProfile()
    {
        var g = TestGame.Create();
        g.Farm.TryPlant(2, "tomato"); // spends coins, so before the balance is fixed
        g.Profile.Coins = 1234;
        g.Profile.Level = 4;
        g.Profile.BestStars["nasi_goreng"] = 3;
        g.Profile.Counter["sate"] = 2;
        g.Profile.Pets["kiwi"] = new PetState { Happiness = 42.5f, LastFedUtc = TestGame.Noon, TimesFed = 3 };
        g.Profile.ActivePet = "kiwi";
        g.Profile.Farm[2].BonusSeconds = 7.5f;
        g.Profile.CompletedQuests.Add("q_first_dish");
        g.Profile.Achievements.Add("a_first_dish");
        g.Profile.Friends.Add(new Friend { Code = "ABCD2345", Name = "Bob", AddedUtc = TestGame.Noon });
        g.Profile.RedeemedGifts.Add("ABCD2345N0NCE");
        g.Profile.Decorations.Add("d_piano");
        g.Profile.Stats.DishesCooked = 9;
        g.Profile.Settings.Language = Language.Indonesian;
        g.Profile.Settings.Graphics = GraphicsQuality.Low;
        g.Profile.Settings.CloudFolder = "C:/Cloud";
        return g.Profile;
    }

    [Fact]
    public void NewProfile_Defaults()
    {
        var p = GameSession.NewProfile(TestGame.Content, "  ", new ManualClock(TestGame.Noon));
        Assert.Equal("Chef", p.PlayerName);
        Assert.Equal(PlayerProfile.CurrentVersion, p.Version);
        Assert.Equal(1, p.Level);
        Assert.Equal(TestGame.Noon, p.CreatedUtc);
        Assert.Equal("Ana", GameSession.NewProfile(TestGame.Content, " Ana ").PlayerName);
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesEverything()
    {
        var original = RichProfile();
        var json = ProfileSerializer.Serialize(original);
        var copy = ProfileSerializer.Deserialize(json);

        Assert.Equal(json, ProfileSerializer.Serialize(copy));
        Assert.Equal(original.PlayerCode, copy.PlayerCode);
        Assert.Equal(1234, copy.Coins);
        Assert.Equal(4, copy.Level);
        Assert.Equal(original.Inventory, copy.Inventory);
        Assert.Equal(3, copy.BestStars["nasi_goreng"]);
        Assert.Equal(2, copy.Counter["sate"]);
        Assert.Equal(42.5f, copy.Pets["kiwi"].Happiness);
        Assert.Equal(TestGame.Noon, copy.Pets["kiwi"].LastFedUtc);
        Assert.Equal("kiwi", copy.ActivePet);
        Assert.Equal("tomato", copy.Farm[2].Crop);
        Assert.Equal(7.5f, copy.Farm[2].BonusSeconds);
        Assert.Contains("q_first_dish", copy.CompletedQuests);
        Assert.Contains("a_first_dish", copy.Achievements);
        Assert.Equal("Bob", Assert.Single(copy.Friends).Name);
        Assert.Contains("ABCD2345N0NCE", copy.RedeemedGifts);
        Assert.Equal(["d_piano"], copy.Decorations);
        Assert.Equal(9, copy.Stats.DishesCooked);
        Assert.Equal(Language.Indonesian, copy.Settings.Language);
        Assert.Equal(GraphicsQuality.Low, copy.Settings.Graphics);
        Assert.Equal("C:/Cloud", copy.Settings.CloudFolder);
    }

    [Fact]
    public void Clone_IsDeep()
    {
        var original = RichProfile();
        var clone = ProfileSerializer.Clone(original);
        clone.Inventory["rice"] = 999;
        clone.Friends.Clear();
        Assert.NotEqual(999, original.Inventory["rice"]);
        Assert.Single(original.Friends);
    }

    [Fact]
    public void Deserialize_RejectsSaveFromNewerVersion()
    {
        var p = RichProfile();
        p.Version = PlayerProfile.CurrentVersion + 1;
        var json = ProfileSerializer.Serialize(p);
        var ex = Assert.Throws<InvalidDataException>(() => ProfileSerializer.Deserialize(json));
        Assert.Contains("newer version", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deserialize_EmptyJsonNull_Throws() =>
        Assert.Throws<InvalidDataException>(() => ProfileSerializer.Deserialize("null"));

    [Fact]
    public void Deserialize_UpgradesOldVersionNumber()
    {
        var p = ProfileSerializer.Deserialize("{\"version\":0,\"playerName\":\"Old\",\"coins\":5}");
        Assert.Equal(PlayerProfile.CurrentVersion, p.Version);
        Assert.Equal("Old", p.PlayerName);
        Assert.Equal(5, p.Coins);
        Assert.Equal(1, p.Level); // missing fields keep defaults
        Assert.NotNull(p.Stats);
    }

    [Fact]
    public void MemorySaveStore_SavesAndLoadsCopies()
    {
        var store = new MemorySaveStore();
        Assert.Null(store.Load());
        var p = RichProfile();
        store.Save(p);
        p.Coins = 0; // later changes don't leak into the saved slot
        var loaded = store.Load()!;
        Assert.Equal(1234, loaded.Coins);
        Assert.Null(store.Load("other"));
    }

    [Fact]
    public void MemorySaveStore_RecentCooking_NewestFirst()
    {
        var store = new MemorySaveStore();
        store.LogCooking("a", 1, 0.3f, TestGame.Noon);
        store.LogCooking("b", 2, 0.7f, TestGame.Noon.AddMinutes(1));
        store.LogCooking("c", 3, 0.9f, TestGame.Noon.AddMinutes(2));
        Assert.Equal(["c", "b"], store.RecentCooking(2).Select(e => e.RecipeId));
    }

    [Fact]
    public void SavedGame_ResumesInANewSession()
    {
        var clock = new ManualClock(TestGame.Noon);
        var g = TestGame.Create(clock: clock);
        g.Farm.TryPlant(0, "chili");
        var json = ProfileSerializer.Serialize(g.Profile);

        clock.Advance(TimeSpan.FromMinutes(5));
        var resumed = new GameSession(TestGame.Content, ProfileSerializer.Deserialize(json), clock, 9);
        Assert.Equal(g.Profile.PlayerCode, resumed.Profile.PlayerCode);
        Assert.True(resumed.Farm.IsReady(0)); // grew while the game was closed
    }
}
