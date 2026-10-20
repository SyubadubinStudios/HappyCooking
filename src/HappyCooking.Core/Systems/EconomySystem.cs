using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.State;

namespace HappyCooking.Core.Systems;

/// <summary>Coins, gems, XP and levels, and the island unlocks that levels bring.</summary>
public sealed class EconomySystem(GameContent content, PlayerProfile profile, GameEvents events)
{
    public const int MaxLevel = 30;

    public int Coins => profile.Coins;

    public int Gems => profile.Gems;

    public int Level => profile.Level;

    public int Xp => profile.Xp;

    /// <summary>XP needed to go from <paramref name="level"/> to the next one. Gentle at first, steeper later.</summary>
    public static int XpForNextLevel(int level) => 60 + (level * 40) + (level * level * 4);

    public int XpToNext => profile.Level >= MaxLevel ? 0 : XpForNextLevel(profile.Level);

    public float LevelProgress => XpToNext == 0 ? 1f : (float)profile.Xp / XpToNext;

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        profile.Coins += amount;
        profile.Stats.CoinsEarned += amount;
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount < 0 || profile.Coins < amount)
        {
            return false;
        }

        profile.Coins -= amount;
        return true;
    }

    public void AddGems(int amount)
    {
        if (amount > 0)
        {
            profile.Gems += amount;
        }
    }

    public bool TrySpendGems(int amount)
    {
        if (amount < 0 || profile.Gems < amount)
        {
            return false;
        }

        profile.Gems -= amount;
        return true;
    }

    /// <summary>Adds XP, levelling up as many times as it pays for. Returns the number of levels gained.</summary>
    public int AddXp(int amount)
    {
        if (amount <= 0 || profile.Level >= MaxLevel)
        {
            return 0;
        }

        profile.Xp += amount;
        var gained = 0;
        while (profile.Level < MaxLevel && profile.Xp >= XpForNextLevel(profile.Level))
        {
            profile.Xp -= XpForNextLevel(profile.Level);
            profile.Level++;
            gained++;
            events.Raise(new GameEvent(GameEventKind.LevelUp, Amount: profile.Level));
            foreach (var region in content.Regions)
            {
                if (region.UnlockLevel == profile.Level)
                {
                    events.Raise(new GameEvent(GameEventKind.RegionUnlocked, region.Id));
                }
            }
        }

        if (profile.Level >= MaxLevel)
        {
            profile.Xp = 0;
        }

        return gained;
    }

    public bool IsRegionUnlocked(Region region) => profile.Level >= region.UnlockLevel;

    public bool IsRegionUnlocked(string regionId) =>
        content.RegionById.TryGetValue(regionId, out var r) && IsRegionUnlocked(r);

    public int UnlockedRegionCount
    {
        get
        {
            var n = 0;
            foreach (var r in content.Regions)
            {
                if (IsRegionUnlocked(r))
                {
                    n++;
                }
            }

            return n;
        }
    }

    public bool IsRecipeUnlocked(Recipe recipe) => profile.Level >= recipe.UnlockLevel && IsRegionUnlocked(recipe.Region);
}
