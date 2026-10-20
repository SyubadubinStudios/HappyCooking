using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.State;

namespace HappyCooking.Core.Systems;

/// <summary>
/// Pet collection. The active pet gives a bonus while it is happy enough; happiness
/// slowly drops in real time and comes back with a snack.
/// </summary>
public sealed class PetSystem(GameContent content, PlayerProfile profile, EconomySystem economy, GameEvents events, IClock clock)
{
    public const int FeedCost = 5;
    public const float HappyThreshold = 30f;

    /// <summary>Happiness points lost per real hour without food.</summary>
    public const float DecayPerHour = 6f;

    public bool Owns(string petId) => profile.Pets.ContainsKey(petId);

    public Pet? ActivePet => profile.ActivePet is { } id && content.PetById.TryGetValue(id, out var pet) ? pet : null;

    public IEnumerable<Pet> Owned => content.Pets.Where(p => Owns(p.Id));

    public bool CanAdopt(Pet pet) =>
        !Owns(pet.Id) && economy.Level >= pet.UnlockLevel && (pet.PriceInGems ? economy.Gems >= pet.Price : economy.Coins >= pet.Price);

    public bool TryAdopt(string petId)
    {
        if (!content.PetById.TryGetValue(petId, out var pet) || !CanAdopt(pet))
        {
            return false;
        }

        var paid = pet.PriceInGems ? economy.TrySpendGems(pet.Price) : economy.TrySpendCoins(pet.Price);
        if (!paid)
        {
            return false;
        }

        profile.Pets[petId] = new PetState { Happiness = 90, LastFedUtc = clock.UtcNow };
        profile.ActivePet ??= petId;
        events.Raise(new GameEvent(GameEventKind.PetAdopted, petId));
        return true;
    }

    public bool SetActive(string petId)
    {
        if (!Owns(petId))
        {
            return false;
        }

        profile.ActivePet = petId;
        return true;
    }

    public float Happiness(string petId)
    {
        if (!profile.Pets.TryGetValue(petId, out var state))
        {
            return 0f;
        }

        var hours = (float)(clock.UtcNow - state.LastFedUtc).TotalHours;
        return Math.Clamp(state.Happiness - (Math.Max(0f, hours) * DecayPerHour), 0f, 100f);
    }

    public bool TryFeed(string petId)
    {
        if (!profile.Pets.TryGetValue(petId, out var state) || !economy.TrySpendCoins(FeedCost))
        {
            return false;
        }

        state.Happiness = Math.Min(100f, Happiness(petId) + 25f);
        state.LastFedUtc = clock.UtcNow;
        state.TimesFed++;
        events.Raise(new GameEvent(GameEventKind.PetFed, petId));
        return true;
    }

    /// <summary>The active pet's bonus of this kind (e.g. 0.25 for +25%), or 0.</summary>
    public float Bonus(PetBonus kind)
    {
        var pet = ActivePet;
        return pet is not null && pet.Bonus == kind && Happiness(pet.Id) >= HappyThreshold ? pet.Amount : 0f;
    }
}
