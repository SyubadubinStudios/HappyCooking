using HappyCooking.Core.Content;
using HappyCooking.Core.Services;

namespace HappyCooking.Core.Systems;

/// <summary>
/// One play-through of a recipe: a sequence of small mini-games, one per step.
/// Engine agnostic — the UI calls <see cref="Update"/> every frame and forwards
/// input through <see cref="Tap"/>, <see cref="SetHolding"/>, <see cref="Stop"/>
/// and <see cref="Pick"/>, then reads the current <see cref="Step"/> to draw it.
/// </summary>
/// <remarks>
/// Built for young children: nothing can be failed. A poor step still earns a
/// little score, and every finished dish gets at least one star.
/// </remarks>
public sealed class CookingSession
{
    private readonly float[] _scores;
    private readonly GameRandom _random;
    private readonly GameContent _content;
    private readonly float _zoneBonus;

    public CookingSession(Recipe recipe, GameContent content, GameRandom random, float zoneBonus = 0f)
    {
        Recipe = recipe;
        _content = content;
        _random = random;
        _zoneBonus = zoneBonus;
        _scores = new float[recipe.Steps.Count];
        Step = CreateStep(0);
    }

    public Recipe Recipe { get; }

    public int StepIndex { get; private set; }

    public int StepCount => Recipe.Steps.Count;

    public StepState Step { get; private set; }

    public bool IsFinished { get; private set; }

    /// <summary>True when any heat step was left far too long. Mr. Burnt Onion notices.</summary>
    public bool Burnt { get; private set; }

    public float ElapsedSeconds { get; private set; }

    /// <summary>Raised when a step completes, with its index and score (0-1).</summary>
    public event Action<int, float>? StepFinished;

    public event Action<CookingResult>? Finished;

    public void Update(float deltaSeconds)
    {
        if (IsFinished || deltaSeconds <= 0)
        {
            return;
        }

        ElapsedSeconds += deltaSeconds;
        Step.Update(deltaSeconds);
        CheckStep();
    }

    public void Tap(float x = 0.5f, float y = 0.5f)
    {
        if (!IsFinished)
        {
            Step.Tap(x, y);
            CheckStep();
        }
    }

    public void SetHolding(bool holding)
    {
        if (!IsFinished)
        {
            Step.SetHolding(holding);
            CheckStep();
        }
    }

    public void Stop()
    {
        if (!IsFinished)
        {
            Step.Stop();
            CheckStep();
        }
    }

    /// <summary>Picks an ingredient from the tray. Returns whether it was one of the right ones.</summary>
    public bool Pick(string ingredientId)
    {
        if (IsFinished)
        {
            return false;
        }

        var ok = Step.Pick(ingredientId);
        CheckStep();
        return ok;
    }

    public float Score
    {
        get
        {
            var sum = 0f;
            for (var i = 0; i < _scores.Length; i++)
            {
                sum += _scores[i];
            }

            return _scores.Length == 0 ? 1f : sum / _scores.Length;
        }
    }

    public static int StarsFor(float score) => score >= 0.85f ? 3 : score >= 0.6f ? 2 : 1;

    private void CheckStep()
    {
        if (!Step.IsComplete)
        {
            return;
        }

        var score = Math.Clamp(Step.Score, 0f, 1f);
        _scores[StepIndex] = score;
        Burnt |= Step.Burnt;
        StepFinished?.Invoke(StepIndex, score);

        if (StepIndex + 1 >= StepCount)
        {
            IsFinished = true;
            Finished?.Invoke(new CookingResult(Recipe, Score, StarsFor(Score), Burnt, ElapsedSeconds));
            return;
        }

        StepIndex++;
        Step = CreateStep(StepIndex);
    }

    public CookingResult Result =>
        IsFinished
            ? new CookingResult(Recipe, Score, StarsFor(Score), Burnt, ElapsedSeconds)
            : throw new InvalidOperationException("The dish is not finished yet.");

    private StepState CreateStep(int index)
    {
        var def = Recipe.Steps[index];
        return def.Kind switch
        {
            StepKind.Tap => new TapStep(def),
            StepKind.Heat => new MeterStep(def, _zoneBonus, holdToFill: false),
            StepKind.Pour => new MeterStep(def, _zoneBonus, holdToFill: true),
            StepKind.Rhythm => new RhythmStep(def, _zoneBonus),
            StepKind.Pick => new PickStep(def, BuildTray(def)),
            StepKind.Decorate => new DecorateStep(def),
            _ => throw new NotSupportedException(def.Kind.ToString()),
        };
    }

    /// <summary>The right ingredients plus a few decoys that are not in this recipe, shuffled.</summary>
    private List<string> BuildTray(RecipeStep def)
    {
        var tray = new List<string>(def.Items ?? []);
        var decoysWanted = Math.Clamp(tray.Count - 1, 2, 3);
        var guard = 0;
        while (decoysWanted > 0 && guard++ < 200)
        {
            var candidate = _random.Pick(_content.Ingredients).Id;
            if (!Recipe.Ingredients.Contains(candidate) && !tray.Contains(candidate))
            {
                tray.Add(candidate);
                decoysWanted--;
            }
        }

        _random.Shuffle(tray);
        return tray;
    }
}

public readonly record struct CookingResult(Recipe Recipe, float Score, int Stars, bool Burnt, float Seconds);

/// <summary>State of the step being played. Subclasses implement one mini-game each.</summary>
public abstract class StepState(RecipeStep definition)
{
    public RecipeStep Definition { get; } = definition;

    public StepKind Kind => Definition.Kind;

    public bool IsComplete { get; protected set; }

    /// <summary>0-1 once complete.</summary>
    public float Score { get; protected set; }

    public bool Burnt { get; protected set; }

    /// <summary>0-1 how far through the step the player is, for a progress ring.</summary>
    public abstract float Progress { get; }

    public virtual void Update(float dt)
    {
    }

    public virtual void Tap(float x, float y)
    {
    }

    public virtual void SetHolding(bool holding)
    {
    }

    public virtual void Stop()
    {
    }

    public virtual bool Pick(string ingredientId) => false;
}

/// <summary>Tap N times. Quick is great, slow is still fine.</summary>
public sealed class TapStep(RecipeStep def) : StepState(def)
{
    public const float SecondsPerTap = 0.3f;

    public int Taps { get; private set; }

    public int Required => Math.Max(1, Definition.Count);

    public float Elapsed { get; private set; }

    public override float Progress => (float)Taps / Required;

    public override void Update(float dt)
    {
        if (Taps > 0 && !IsComplete)
        {
            Elapsed += dt;
        }
    }

    public override void Tap(float x, float y)
    {
        if (IsComplete)
        {
            return;
        }

        Taps++;
        if (Taps >= Required)
        {
            var par = Required * SecondsPerTap;
            Score = Elapsed <= par ? 1f : Math.Max(0.45f, 1f - ((Elapsed - par) / (par * 2f)));
            IsComplete = true;
        }
    }
}

/// <summary>
/// A needle that rises on its own (Heat: press Stop) or while held (Pour: let go).
/// Landing inside the zone is perfect; the further outside, the lower the score.
/// </summary>
public sealed class MeterStep : StepState
{
    private const float PourRate = 0.42f;
    private bool _holding;

    public MeterStep(RecipeStep def, float zoneBonus, bool holdToFill)
        : base(def)
    {
        HoldToFill = holdToFill;
        var half = def.Zone * 0.5f * (1f + zoneBonus);
        ZoneMin = Math.Clamp(def.Target - half, 0.02f, 0.98f);
        ZoneMax = Math.Clamp(def.Target + half, 0.02f, 0.98f);
    }

    public bool HoldToFill { get; }

    public float Value { get; private set; }

    public float ZoneMin { get; }

    public float ZoneMax { get; }

    public bool IsInZone => Value >= ZoneMin && Value <= ZoneMax;

    public bool IsHolding => _holding;

    public override float Progress => Value;

    public override void Update(float dt)
    {
        if (IsComplete)
        {
            return;
        }

        if (HoldToFill)
        {
            if (_holding)
            {
                Value += PourRate * dt;
            }
        }
        else
        {
            Value += Math.Max(0.05f, Definition.Speed) * dt;
        }

        if (Value >= 1f)
        {
            // Left on the stove (or poured) until the meter ran out.
            Value = 1f;
            Finish();
        }
    }

    public override void SetHolding(bool holding)
    {
        if (!HoldToFill || IsComplete)
        {
            return;
        }

        var released = _holding && !holding;
        _holding = holding;
        if (released && Value > 0.01f)
        {
            Finish();
        }
    }

    public override void Stop()
    {
        if (!IsComplete && (!HoldToFill || Value > 0.01f))
        {
            Finish();
        }
    }

    public override void Tap(float x, float y)
    {
        // A tap on a heat meter means "stop now", so a single big button works on touch screens.
        if (!HoldToFill)
        {
            Stop();
        }
    }

    private void Finish()
    {
        float distance = Value < ZoneMin ? ZoneMin - Value : Value > ZoneMax ? Value - ZoneMax : 0f;
        Score = distance <= 0f ? 1f : Math.Max(0.2f, 1f - (distance / 0.35f));
        Burnt = !HoldToFill && Value > ZoneMax + 0.15f;
        IsComplete = true;
    }
}

/// <summary>A marker sweeps back and forth; tap while it is inside the zone, Count times.</summary>
public sealed class RhythmStep : StepState
{
    private float _phase;

    public RhythmStep(RecipeStep def, float zoneBonus)
        : base(def)
    {
        var half = Math.Max(0.06f, def.Zone * 0.5f * (1f + zoneBonus));
        ZoneMin = 0.5f - half;
        ZoneMax = 0.5f + half;
    }

    public float ZoneMin { get; }

    public float ZoneMax { get; }

    /// <summary>Marker position 0-1, moving like a pendulum.</summary>
    public float Marker { get; private set; }

    public int Hits { get; private set; }

    public int Misses { get; private set; }

    public int Required => Math.Max(1, Definition.Count);

    public override float Progress => (float)Hits / Required;

    public override void Update(float dt)
    {
        if (IsComplete)
        {
            return;
        }

        _phase += dt * Math.Max(0.2f, Definition.Speed);
        // Triangle wave 0→1→0 once per cycle.
        var f = _phase - MathF.Floor(_phase);
        Marker = f < 0.5f ? f * 2f : 2f - (f * 2f);
    }

    public override void Tap(float x, float y)
    {
        if (IsComplete)
        {
            return;
        }

        if (Marker >= ZoneMin && Marker <= ZoneMax)
        {
            Hits++;
        }
        else
        {
            Misses++;
        }

        if (Hits >= Required)
        {
            Score = Math.Max(0.35f, Required / (Required + (Misses * 0.5f)));
            IsComplete = true;
        }
    }
}

/// <summary>Choose the right ingredients from a tray with decoys.</summary>
public sealed class PickStep(RecipeStep def, IReadOnlyList<string> tray) : StepState(def)
{
    private readonly HashSet<string> _picked = new(StringComparer.Ordinal);

    public IReadOnlyList<string> Tray { get; } = tray;

    public int Mistakes { get; private set; }

    public bool IsPicked(string id) => _picked.Contains(id);

    public bool IsCorrect(string id) => Definition.Items?.Contains(id) == true;

    public override float Progress => Definition.Items is { Count: > 0 } items ? (float)_picked.Count / items.Count : 1f;

    public override bool Pick(string ingredientId)
    {
        if (IsComplete)
        {
            return false;
        }

        if (!IsCorrect(ingredientId))
        {
            Mistakes++;
            return false;
        }

        _picked.Add(ingredientId);
        if (_picked.Count >= (Definition.Items?.Count ?? 0))
        {
            Score = Math.Max(0.3f, 1f - (0.2f * Mistakes));
            IsComplete = true;
        }

        return true;
    }
}

/// <summary>Place toppings anywhere on the dish. Pure creativity, always full marks.</summary>
public sealed class DecorateStep(RecipeStep def) : StepState(def)
{
    private readonly List<(float X, float Y)> _placements = [];

    public IReadOnlyList<(float X, float Y)> Placements => _placements;

    public int Required => Math.Max(1, Definition.Count);

    public override float Progress => (float)_placements.Count / Required;

    public override void Tap(float x, float y)
    {
        if (IsComplete)
        {
            return;
        }

        _placements.Add((Math.Clamp(x, 0f, 1f), Math.Clamp(y, 0f, 1f)));
        if (_placements.Count >= Required)
        {
            Score = 1f;
            IsComplete = true;
        }
    }
}
