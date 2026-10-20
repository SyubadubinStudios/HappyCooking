using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.State;

namespace HappyCooking.Core.Systems;

public enum FishingPhase
{
    Idle,

    /// <summary>Line is in the water, waiting for a bite.</summary>
    Waiting,

    /// <summary>A fish is nibbling: tap now!</summary>
    Bite,

    /// <summary>Hooked: tap quickly to reel it in before time runs out.</summary>
    Reeling,
    Caught,
    Escaped,
}

/// <summary>
/// The fishing mini-game for one island: cast, wait, react to the bite, reel.
/// Which fish bites is decided by rarity, the time of day and the active pet.
/// </summary>
public sealed class FishingSession(Region region, FishingSystem system, GameRandom random)
{
    public const float BiteWindowSeconds = 1.6f;
    public const float ReelSeconds = 5f;

    private float _timer;

    public Region Region { get; } = region;

    public FishingPhase Phase { get; private set; } = FishingPhase.Idle;

    public Fish? Hooked { get; private set; }

    public int ReelTaps { get; private set; }

    public int ReelRequired => Hooked is null ? 1 : 5 + ((int)Hooked.Rarity * 3);

    /// <summary>0-1 progress of the current phase's timer (bite window or reel time left).</summary>
    public float TimerFraction => Phase switch
    {
        FishingPhase.Bite => Math.Clamp(_timer / BiteWindowSeconds, 0f, 1f),
        FishingPhase.Reeling => Math.Clamp(_timer / ReelSeconds, 0f, 1f),
        _ => 0f,
    };

    public float ReelProgress => Math.Clamp((float)ReelTaps / ReelRequired, 0f, 1f);

    public event Action<Fish>? Caught;

    public void Cast()
    {
        if (Phase is FishingPhase.Idle or FishingPhase.Caught or FishingPhase.Escaped)
        {
            Phase = FishingPhase.Waiting;
            Hooked = null;
            ReelTaps = 0;
            _timer = random.Range(1.5f, 4.5f);
        }
    }

    public void Update(float dt)
    {
        switch (Phase)
        {
            case FishingPhase.Waiting:
                _timer -= dt;
                if (_timer <= 0)
                {
                    Hooked = system.RollFish(Region, random);
                    Phase = FishingPhase.Bite;
                    _timer = BiteWindowSeconds;
                }

                break;
            case FishingPhase.Bite:
                _timer -= dt;
                if (_timer <= 0)
                {
                    Phase = FishingPhase.Escaped;
                }

                break;
            case FishingPhase.Reeling:
                _timer -= dt;
                if (_timer <= 0)
                {
                    Phase = FishingPhase.Escaped;
                }

                break;
        }
    }

    /// <summary>The one button: casts, hooks a biting fish, or reels.</summary>
    public void Tap()
    {
        switch (Phase)
        {
            case FishingPhase.Idle:
            case FishingPhase.Caught:
            case FishingPhase.Escaped:
                Cast();
                break;
            case FishingPhase.Waiting:
                // Pulled too early: the line comes back empty.
                Phase = FishingPhase.Escaped;
                break;
            case FishingPhase.Bite:
                Phase = FishingPhase.Reeling;
                _timer = ReelSeconds;
                ReelTaps = 0;
                break;
            case FishingPhase.Reeling:
                ReelTaps++;
                if (ReelTaps >= ReelRequired && Hooked is not null)
                {
                    Phase = FishingPhase.Caught;
                    system.Land(Hooked);
                    Caught?.Invoke(Hooked);
                }

                break;
        }
    }
}

public sealed class FishingSystem(
    GameContent content,
    PlayerProfile profile,
    EconomySystem economy,
    InventorySystem inventory,
    PetSystem pets,
    WorldSystem world,
    GameEvents events,
    GameRandom random)
{
    public FishingSession StartSession(Region region) => new(region, this, random);

    public int TimesCaught(string fishId) => profile.FishLog.GetValueOrDefault(fishId);

    /// <summary>Base weights per rarity; luck multiplies the rare end.</summary>
    public static float Weight(Rarity rarity, float luck) => rarity switch
    {
        Rarity.Common => 60f,
        Rarity.Uncommon => 25f,
        Rarity.Rare => 10f * (1f + luck),
        Rarity.Legendary => 3f * (1f + (luck * 2f)),
        _ => 0f,
    };

    public Fish RollFish(Region region, GameRandom rng)
    {
        var luck = pets.Bonus(PetBonus.FishLuck);
        var night = world.IsNight;
        Span<float> weights = stackalloc float[region.Fish.Count];
        var total = 0f;
        for (var i = 0; i < region.Fish.Count; i++)
        {
            var fish = content.FishById[region.Fish[i]];
            weights[i] = fish.NightOnly && !night ? 0f : Weight(fish.Rarity, luck);
            total += weights[i];
        }

        var roll = rng.NextFloat() * total;
        for (var i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll < 0f && weights[i] > 0f)
            {
                return content.FishById[region.Fish[i]];
            }
        }

        // Only reachable through float rounding: fall back to the first catchable fish.
        for (var i = 0; i < weights.Length; i++)
        {
            if (weights[i] > 0f)
            {
                return content.FishById[region.Fish[i]];
            }
        }

        return content.FishById[region.Fish[0]];
    }

    internal void Land(Fish fish)
    {
        profile.FishLog[fish.Id] = TimesCaught(fish.Id) + 1;
        profile.Stats.FishCaught++;
        inventory.Add(fish.Ingredient);
        economy.AddCoins(fish.Coins);
        economy.AddXp(4 + ((int)fish.Rarity * 6));
        events.Raise(new GameEvent(GameEventKind.FishCaught, fish.Id));
    }
}
