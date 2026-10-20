using HappyCooking.Core.Content;
using HappyCooking.Core.Localization;
using HappyCooking.Core.Services;
using HappyCooking.Core.Systems;

namespace HappyCooking.Core.Tests;

/// <summary>Shared helpers: deterministic sessions, hand-made recipes and a "perfect player".</summary>
internal static class TestGame
{
    /// <summary>
    /// 1 Jan 2026 00:12 UTC. Unix minutes at UTC midnight are a multiple of 1440 (and so of the
    /// 24-minute in-game day), so 12 minutes past midnight is in-game noon. No seasonal event runs on 1 Jan.
    /// </summary>
    public static readonly DateTimeOffset Noon = new(2026, 1, 1, 0, 12, 0, TimeSpan.Zero);

    /// <summary>In-game midnight (TimeOfDay = 0) on the same day.</summary>
    public static readonly DateTimeOffset Midnight = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static GameContent Content => GameContent.Default;

    public static GameSession Create(int seed = 42, ManualClock? clock = null, string name = "Tester")
    {
        clock ??= new ManualClock(Noon);
        var profile = GameSession.NewProfile(Content, name, clock);
        return new GameSession(Content, profile, clock, seed);
    }

    public static Recipe Recipe(string id = "test_dish", params RecipeStep[] steps) =>
        Recipe(id, ["rice", "egg"], steps);

    public static Recipe Recipe(string id, IReadOnlyList<string> ingredients, params RecipeStep[] steps) =>
        new(
            id,
            new LocalizedText("Test", "Tes"),
            "nusantara",
            new LocalizedText("d", "d"),
            new LocalizedText("f", "f"),
            Difficulty: 1,
            UnlockLevel: 1,
            Coins: 20,
            Xp: 10,
            Color: "#FFFFFF",
            Model: null,
            Creative: false,
            ingredients,
            steps);

    public static CookingSession Session(params RecipeStep[] steps) =>
        new(Recipe("test_dish", steps), Content, new GameRandom(7));

    /// <summary>Plays the current step as well as possible.</summary>
    public static void PlayStepPerfectly(CookingSession s)
    {
        var index = s.StepIndex;
        var step = s.Step;
        switch (step)
        {
            case TapStep tap:
                for (var i = 0; i < tap.Required; i++)
                {
                    s.Tap();
                }

                break;
            case DecorateStep deco:
                for (var i = 0; i < deco.Required; i++)
                {
                    s.Tap(0.3f, 0.6f);
                }

                break;
            case MeterStep meter when meter.HoldToFill:
                s.SetHolding(true);
                Guard(() => meter.Value >= meter.Definition.Target, () => s.Update(0.01f));
                s.SetHolding(false);
                break;
            case MeterStep meter:
                Guard(() => meter.Value >= meter.Definition.Target, () => s.Update(0.01f));
                s.Stop();
                break;
            case RhythmStep rhythm:
                while (!rhythm.IsComplete)
                {
                    Guard(() => rhythm.Marker >= rhythm.ZoneMin + 0.01f && rhythm.Marker <= rhythm.ZoneMax - 0.01f, () => s.Update(0.005f));
                    s.Tap();
                    s.Update(0.005f);
                }

                break;
            case PickStep pick:
                foreach (var id in pick.Definition.Items!)
                {
                    s.Pick(id);
                }

                break;
            default:
                throw new InvalidOperationException($"Unknown step {step.GetType()}");
        }

        Assert.True(s.IsFinished || s.StepIndex == index + 1, $"step {index} ({step.Kind}) did not complete");
    }

    public static void PlayPerfectly(CookingSession s)
    {
        while (!s.IsFinished)
        {
            PlayStepPerfectly(s);
        }
    }

    /// <summary>Runs <paramref name="tick"/> until <paramref name="done"/> holds, failing after 100k ticks.</summary>
    public static void Guard(Func<bool> done, Action tick)
    {
        var n = 0;
        while (!done())
        {
            Assert.True(n++ < 100_000, "condition never reached");
            tick();
        }
    }
}
