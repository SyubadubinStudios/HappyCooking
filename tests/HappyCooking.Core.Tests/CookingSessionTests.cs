using HappyCooking.Core.Content;
using HappyCooking.Core.Services;
using HappyCooking.Core.Systems;

namespace HappyCooking.Core.Tests;

public class CookingSessionTests
{
    private static RecipeStep TapDef(int count = 4) => new(StepKind.Tap, "chop", Count: count);

    private static RecipeStep HeatDef(float target = 0.5f, float zone = 0.14f, float speed = 0.4f) =>
        new(StepKind.Heat, "fry", Target: target, Zone: zone, Speed: speed);

    private static RecipeStep PourDef(float target = 0.5f, float zone = 0.14f) =>
        new(StepKind.Pour, "season", Target: target, Zone: zone);

    private static RecipeStep RhythmDef(int count = 3) => new(StepKind.Rhythm, "stir", Count: count, Zone: 0.2f, Speed: 0.75f);

    // ---------- Tap ----------

    [Fact]
    public void Tap_QuickTaps_ScorePerfect()
    {
        var s = TestGame.Session(TapDef(4));
        var step = Assert.IsType<TapStep>(s.Step);
        for (var i = 0; i < 3; i++)
        {
            s.Tap();
        }

        Assert.False(step.IsComplete);
        Assert.Equal(0.75f, step.Progress, 3);
        s.Tap();
        Assert.True(s.IsFinished);
        Assert.Equal(1f, s.Score, 3);
    }

    [Fact]
    public void Tap_SlowTaps_LowerScoreButNeverBelowFloor()
    {
        var s = TestGame.Session(TapDef(2));
        var step = (TapStep)s.Step;
        s.Tap();
        s.Update(100f); // much slower than par
        s.Tap();
        Assert.True(step.IsComplete);
        Assert.Equal(0.45f, step.Score, 3);
    }

    [Fact]
    public void Tap_ElapsedOnlyCountsAfterFirstTap()
    {
        var s = TestGame.Session(TapDef(2));
        s.Update(50f); // idling before the first tap is free
        s.Tap();
        s.Tap();
        Assert.Equal(1f, s.Score, 3);
    }

    // ---------- Heat ----------

    [Fact]
    public void Heat_StopInZone_IsPerfect()
    {
        var s = TestGame.Session(HeatDef());
        var meter = Assert.IsType<MeterStep>(s.Step);
        Assert.False(meter.HoldToFill);
        TestGame.Guard(() => meter.Value >= 0.5f, () => s.Update(0.01f));
        Assert.True(meter.IsInZone);
        s.Stop();
        Assert.True(s.IsFinished);
        Assert.Equal(1f, s.Score, 3);
        Assert.False(s.Burnt);
    }

    [Fact]
    public void Heat_TapActsAsStop()
    {
        var s = TestGame.Session(HeatDef());
        var meter = (MeterStep)s.Step;
        TestGame.Guard(() => meter.IsInZone, () => s.Update(0.01f));
        s.Tap();
        Assert.True(s.IsFinished);
        Assert.Equal(1f, s.Score, 3);
    }

    [Fact]
    public void Heat_StopTooEarly_ScoresLessByDistance()
    {
        var s = TestGame.Session(HeatDef(target: 0.5f, zone: 0.14f, speed: 0.4f));
        var meter = (MeterStep)s.Step;
        s.Update(0.5f); // value 0.2, zone starts at 0.43
        Assert.Equal(0.2f, meter.Value, 3);
        s.Stop();
        var expected = 1f - ((meter.ZoneMin - 0.2f) / 0.35f);
        Assert.Equal(expected, s.Score, 3);
        Assert.True(s.Score < 1f);
        Assert.False(s.Burnt);
    }

    [Fact]
    public void Heat_StopSlightlyLate_NotBurnt_ButFarLateIsBurnt()
    {
        var late = TestGame.Session(HeatDef());
        var m1 = (MeterStep)late.Step;
        TestGame.Guard(() => m1.Value > m1.ZoneMax + 0.05f, () => late.Update(0.01f));
        late.Stop();
        Assert.False(late.Burnt);
        Assert.True(late.Score < 1f);

        var burnt = TestGame.Session(HeatDef());
        var m2 = (MeterStep)burnt.Step;
        TestGame.Guard(() => m2.Value > m2.ZoneMax + 0.2f, () => burnt.Update(0.01f));
        burnt.Stop();
        Assert.True(burnt.Burnt);
    }

    [Fact]
    public void Heat_MeterRunningOut_FinishesBurntWithMinimumScore()
    {
        var s = TestGame.Session(HeatDef(target: 0.5f));
        CookingResult? result = null;
        s.Finished += r => result = r;
        s.Update(10f);
        Assert.True(s.IsFinished);
        Assert.Equal(1f, ((MeterStep)s.Step).Value);
        Assert.True(s.Burnt);
        Assert.NotNull(result);
        Assert.True(result!.Value.Burnt);
        Assert.Equal(0.2f, result.Value.Score, 3);
        Assert.Equal(1, result.Value.Stars); // every finished dish still gets a star
    }

    // ---------- Pour ----------

    [Fact]
    public void Pour_OnlyFillsWhileHeld()
    {
        var s = TestGame.Session(PourDef());
        var meter = Assert.IsType<MeterStep>(s.Step);
        Assert.True(meter.HoldToFill);
        s.Update(1f);
        Assert.Equal(0f, meter.Value);
        s.SetHolding(true);
        Assert.True(meter.IsHolding);
        s.Update(0.5f);
        Assert.Equal(0.21f, meter.Value, 3);
    }

    [Fact]
    public void Pour_ReleaseInZone_IsPerfect()
    {
        var s = TestGame.Session(PourDef(target: 0.5f));
        var meter = (MeterStep)s.Step;
        s.SetHolding(true);
        TestGame.Guard(() => meter.Value >= 0.5f, () => s.Update(0.01f));
        s.SetHolding(false);
        Assert.True(s.IsFinished);
        Assert.Equal(1f, s.Score, 3);
    }

    [Fact]
    public void Pour_ReleaseOutOfZone_ScoresLess_AndNeverBurns()
    {
        var s = TestGame.Session(PourDef(target: 0.5f));
        var meter = (MeterStep)s.Step;
        s.SetHolding(true);
        s.Update(2f); // 0.84, past the zone
        s.SetHolding(false);
        Assert.True(s.IsFinished);
        Assert.True(s.Score < 1f);
        Assert.False(s.Burnt);
        Assert.True(meter.Value > meter.ZoneMax);
    }

    [Fact]
    public void Pour_ReleaseBeforeAnythingPoured_DoesNotFinish()
    {
        var s = TestGame.Session(PourDef());
        s.SetHolding(true);
        s.SetHolding(false);
        Assert.False(s.IsFinished);
        s.Stop();
        Assert.False(s.IsFinished);
        s.Tap(); // a tap does nothing on a pour meter
        Assert.False(s.IsFinished);
    }

    [Fact]
    public void Pour_HeldUntilFull_FinishesNotBurnt()
    {
        var s = TestGame.Session(PourDef());
        s.SetHolding(true);
        s.Update(5f);
        Assert.True(s.IsFinished);
        Assert.False(s.Burnt);
    }

    // ---------- Rhythm ----------

    [Fact]
    public void Rhythm_HitsInZone_CompleteWithPerfectScore()
    {
        var s = TestGame.Session(RhythmDef(3));
        var r = Assert.IsType<RhythmStep>(s.Step);
        for (var i = 0; i < 3; i++)
        {
            TestGame.Guard(() => r.Marker > r.ZoneMin && r.Marker < r.ZoneMax, () => s.Update(0.005f));
            s.Tap();
            TestGame.Guard(() => r.Marker < r.ZoneMin || r.Marker > r.ZoneMax || r.IsComplete, () => s.Update(0.005f));
        }

        Assert.Equal(3, r.Hits);
        Assert.Equal(0, r.Misses);
        Assert.True(s.IsFinished);
        Assert.Equal(1f, s.Score, 3);
    }

    [Fact]
    public void Rhythm_MissesLowerScore()
    {
        var s = TestGame.Session(RhythmDef(2));
        var r = (RhythmStep)s.Step;
        Assert.Equal(0f, r.Marker); // starts at the edge, outside the zone
        s.Tap();
        s.Tap();
        Assert.Equal(2, r.Misses);
        Assert.False(r.IsComplete);
        for (var i = 0; i < 2; i++)
        {
            TestGame.Guard(() => r.Marker > r.ZoneMin && r.Marker < r.ZoneMax, () => s.Update(0.005f));
            s.Tap();
        }

        Assert.True(r.IsComplete);
        Assert.Equal(2f / (2f + 1f), r.Score, 3); // Required / (Required + Misses * 0.5)
    }

    [Fact]
    public void Rhythm_MarkerIsTriangleWave()
    {
        var s = TestGame.Session(new RecipeStep(StepKind.Rhythm, "flip", Count: 1, Zone: 0.2f, Speed: 1f));
        var r = (RhythmStep)s.Step;
        s.Update(0.25f);
        Assert.Equal(0.5f, r.Marker, 3);
        s.Update(0.25f);
        Assert.Equal(1f, r.Marker, 3);
        s.Update(0.25f);
        Assert.Equal(0.5f, r.Marker, 3);
    }

    // ---------- Pick ----------

    [Fact]
    public void Pick_TrayHasItemsPlusDecoysNotInRecipe()
    {
        var recipe = TestGame.Recipe("pick_dish", ["rice", "egg", "chili"],
            new RecipeStep(StepKind.Pick, "gather", Items: ["rice", "egg", "chili"]));
        var s = new CookingSession(recipe, TestGame.Content, new GameRandom(3));
        var pick = Assert.IsType<PickStep>(s.Step);
        Assert.Equal(5, pick.Tray.Count); // 3 items + clamp(3 - 1, 2, 3) decoys
        Assert.Equal(pick.Tray.Count, pick.Tray.Distinct().Count());
        foreach (var id in recipe.Ingredients)
        {
            Assert.Contains(id, pick.Tray);
        }

        Assert.All(pick.Tray.Where(i => !pick.IsCorrect(i)), d => Assert.DoesNotContain(d, recipe.Ingredients));
    }

    [Fact]
    public void Pick_CorrectOnly_IsPerfect_AndMistakesCost()
    {
        var recipe = TestGame.Recipe("pick_dish", ["rice", "egg"],
            new RecipeStep(StepKind.Pick, "gather", Items: ["rice", "egg"]));

        var good = new CookingSession(recipe, TestGame.Content, new GameRandom(3));
        Assert.True(good.Pick("rice"));
        Assert.True(((PickStep)good.Step).IsPicked("rice"));
        Assert.True(good.Pick("egg"));
        Assert.Equal(1f, good.Score, 3);

        var bad = new CookingSession(recipe, TestGame.Content, new GameRandom(3));
        var step = (PickStep)bad.Step;
        var decoy = step.Tray.First(i => !step.IsCorrect(i));
        Assert.False(bad.Pick(decoy));
        Assert.False(bad.Pick(decoy));
        Assert.Equal(2, step.Mistakes);
        bad.Pick("rice");
        bad.Pick("rice"); // picking the same one twice doesn't count double
        Assert.False(bad.IsFinished);
        bad.Pick("egg");
        Assert.True(bad.IsFinished);
        Assert.Equal(0.6f, bad.Score, 3);
        Assert.False(bad.Pick("rice")); // finished sessions ignore input
    }

    [Fact]
    public void Pick_ManyMistakes_HasFloor()
    {
        var recipe = TestGame.Recipe("pick_dish", ["rice"], new RecipeStep(StepKind.Pick, "gather", Items: ["rice"]));
        var s = new CookingSession(recipe, TestGame.Content, new GameRandom(3));
        for (var i = 0; i < 20; i++)
        {
            s.Pick("chocolate");
        }

        s.Pick("rice");
        Assert.Equal(0.3f, s.Score, 3);
    }

    // ---------- Decorate ----------

    [Fact]
    public void Decorate_PlacesClampedToppings_AlwaysFullMarks()
    {
        var s = TestGame.Session(new RecipeStep(StepKind.Decorate, "plate", Count: 3));
        var d = Assert.IsType<DecorateStep>(s.Step);
        s.Tap(-1f, 2f);
        s.Tap(0.5f, 0.5f);
        Assert.Equal((0f, 1f), d.Placements[0]);
        Assert.False(s.IsFinished);
        s.Tap(0.2f, 0.2f);
        Assert.True(s.IsFinished);
        Assert.Equal(1f, s.Score);
    }

    // ---------- Session ----------

    [Theory]
    [InlineData(1f, 3)]
    [InlineData(0.85f, 3)]
    [InlineData(0.849f, 2)]
    [InlineData(0.6f, 2)]
    [InlineData(0.599f, 1)]
    [InlineData(0f, 1)]
    public void StarsFor_Thresholds(float score, int stars) => Assert.Equal(stars, CookingSession.StarsFor(score));

    [Fact]
    public void Session_AdvancesThroughSteps_RaisesEvents_AndFinishes()
    {
        var s = TestGame.Session(TapDef(2), HeatDef(), PourDef(), RhythmDef(2),
            new RecipeStep(StepKind.Decorate, "plate", Count: 2));
        var stepEvents = new List<(int Index, float Score)>();
        var finished = 0;
        CookingResult result = default;
        s.StepFinished += (i, sc) => stepEvents.Add((i, sc));
        s.Finished += r =>
        {
            finished++;
            result = r;
        };

        Assert.Equal(5, s.StepCount);
        Assert.Throws<InvalidOperationException>(() => s.Result);
        TestGame.PlayPerfectly(s);

        Assert.True(s.IsFinished);
        Assert.Equal(1, finished);
        Assert.Equal([0, 1, 2, 3, 4], stepEvents.Select(e => e.Index));
        Assert.Equal(3, result.Stars);
        Assert.Equal(s.Result, result);
        Assert.True(result.Seconds > 0);
        Assert.Equal(4, s.StepIndex);

        // Input after the end is ignored and does not raise Finished again.
        s.Tap();
        s.Update(1f);
        Assert.Equal(1, finished);
    }

    [Fact]
    public void Session_ScoreIsAverageOfSteps()
    {
        var s = TestGame.Session(TapDef(1), HeatDef(target: 0.5f));
        s.Tap();
        s.Update(10f); // heat runs out -> 0.2
        Assert.Equal((1f + 0.2f) / 2f, s.Score, 3);
        Assert.Equal(2, s.Result.Stars); // 0.6 is exactly the 2-star threshold
    }

    [Fact]
    public void Update_IgnoresNonPositiveDelta()
    {
        var s = TestGame.Session(HeatDef());
        s.Update(0f);
        s.Update(-1f);
        Assert.Equal(0f, s.ElapsedSeconds);
        Assert.Equal(0f, ((MeterStep)s.Step).Value);
    }

    [Fact]
    public void ZoneBonus_WidensMeterAndRhythmZones()
    {
        var steps = new[] { HeatDef(target: 0.5f, zone: 0.14f), RhythmDef() };
        var plain = new CookingSession(TestGame.Recipe("z", steps), TestGame.Content, new GameRandom(1));
        var bonus = new CookingSession(TestGame.Recipe("z", steps), TestGame.Content, new GameRandom(1), zoneBonus: 0.5f);

        var m0 = (MeterStep)plain.Step;
        var m1 = (MeterStep)bonus.Step;
        Assert.Equal(0.14f, m0.ZoneMax - m0.ZoneMin, 3);
        Assert.Equal(0.21f, m1.ZoneMax - m1.ZoneMin, 3);

        plain.Update(0.01f);
        bonus.Update(0.01f);
        plain.Stop();
        bonus.Stop();
        var r0 = Assert.IsType<RhythmStep>(plain.Step);
        var r1 = Assert.IsType<RhythmStep>(bonus.Step);
        Assert.True(r1.ZoneMax - r1.ZoneMin > r0.ZoneMax - r0.ZoneMin);
    }

    [Fact]
    public void ZoneBonus_TurnsANearMissIntoAPerfectStop()
    {
        var def = HeatDef(target: 0.5f, zone: 0.1f, speed: 0.4f);
        var plain = new CookingSession(TestGame.Recipe("z", def), TestGame.Content, new GameRandom(1));
        var bonus = new CookingSession(TestGame.Recipe("z", def), TestGame.Content, new GameRandom(1), zoneBonus: 1f);
        plain.Update(1.1f); // value 0.44: below the plain zone (0.45) but inside the widened one (0.40)
        bonus.Update(1.1f);
        plain.Stop();
        bonus.Stop();
        Assert.True(plain.Score < 1f);
        Assert.Equal(1f, bonus.Score, 3);
    }

    [Fact]
    public void MeterZone_IsClampedInsideTheMeter()
    {
        var s = TestGame.Session(HeatDef(target: 0.95f, zone: 0.3f));
        var m = (MeterStep)s.Step;
        Assert.Equal(0.98f, m.ZoneMax, 3);
    }
}
