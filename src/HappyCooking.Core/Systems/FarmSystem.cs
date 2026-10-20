using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.State;

namespace HappyCooking.Core.Systems;

/// <summary>
/// Garden plots that grow ingredients in real time. Rain and a gardening pet make
/// crops grow faster; the boost is banked per plot so it survives restarts.
/// </summary>
public sealed class FarmSystem(
    GameContent content,
    PlayerProfile profile,
    EconomySystem economy,
    InventorySystem inventory,
    PetSystem pets,
    WorldSystem world,
    GameEvents events,
    IClock clock)
{
    public const int StartingPlots = 6;
    public const int MaxPlots = 12;

    public IReadOnlyList<FarmPlot> Plots => profile.Farm;

    public int NextPlotPrice => 50 + (profile.Farm.Count * 25);

    public void EnsurePlots()
    {
        while (profile.Farm.Count < StartingPlots)
        {
            profile.Farm.Add(new FarmPlot());
        }
    }

    public bool TryBuyPlot()
    {
        if (profile.Farm.Count >= MaxPlots || !economy.TrySpendCoins(NextPlotPrice))
        {
            return false;
        }

        profile.Farm.Add(new FarmPlot());
        return true;
    }

    public bool TryPlant(int plot, string cropId)
    {
        if ((uint)plot >= (uint)profile.Farm.Count || profile.Farm[plot].Crop is not null ||
            !content.CropById.TryGetValue(cropId, out var crop) || !economy.TrySpendCoins(crop.SeedPrice))
        {
            return false;
        }

        profile.Farm[plot] = new FarmPlot { Crop = cropId, PlantedUtc = clock.UtcNow };
        return true;
    }

    /// <summary>0-1 growth of the crop on <paramref name="plot"/>; 0 when empty.</summary>
    public float Growth(int plot)
    {
        var p = profile.Farm[plot];
        if (p.Crop is null || !content.CropById.TryGetValue(p.Crop, out var crop))
        {
            return 0f;
        }

        var seconds = (float)(clock.UtcNow - p.PlantedUtc).TotalSeconds;
        var grown = (Math.Max(0f, seconds) * (1f + pets.Bonus(PetBonus.Growth))) + p.BonusSeconds;
        return Math.Clamp(grown / crop.GrowSeconds, 0f, 1f);
    }

    public bool IsReady(int plot) => profile.Farm[plot].Crop is not null && Growth(plot) >= 1f;

    public TimeSpan TimeLeft(int plot)
    {
        var p = profile.Farm[plot];
        if (p.Crop is null || !content.CropById.TryGetValue(p.Crop, out var crop))
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(Math.Max(0, (1f - Growth(plot)) * crop.GrowSeconds / (1f + pets.Bonus(PetBonus.Growth))));
    }

    /// <summary>Called while the game runs: rain waters the farm and banks extra growth.</summary>
    public void Update(float deltaSeconds)
    {
        if (world.WeatherIn("nusantara") != Weather.Rain)
        {
            return;
        }

        foreach (var p in profile.Farm)
        {
            if (p.Crop is not null)
            {
                p.BonusSeconds += deltaSeconds * 0.5f;
            }
        }
    }

    public Crop? TryHarvest(int plot)
    {
        if ((uint)plot >= (uint)profile.Farm.Count || !IsReady(plot))
        {
            return null;
        }

        var crop = content.CropById[profile.Farm[plot].Crop!];
        profile.Farm[plot] = new FarmPlot();
        inventory.Add(crop.Ingredient, crop.Yield);
        economy.AddXp(crop.Xp);
        profile.Stats.CropsHarvested++;
        events.Raise(new GameEvent(GameEventKind.CropHarvested, crop.Id, crop.Yield));
        return crop;
    }

    /// <summary>Harvests every ready plot. Returns how many were harvested.</summary>
    public int HarvestAll()
    {
        var n = 0;
        for (var i = 0; i < profile.Farm.Count; i++)
        {
            if (TryHarvest(i) is not null)
            {
                n++;
            }
        }

        return n;
    }
}
