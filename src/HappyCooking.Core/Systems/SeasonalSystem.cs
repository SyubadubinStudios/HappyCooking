using HappyCooking.Core.Content;
using HappyCooking.Core.Services;

namespace HappyCooking.Core.Systems;

/// <summary>Calendar events (Lunar New Year, 17 August, ...) and the coin bonus on their featured recipes.</summary>
public sealed class SeasonalSystem(GameContent content, IClock clock)
{
    public SeasonalEvent? ActiveEvent => ActiveOn(clock.Today);

    public SeasonalEvent? ActiveOn(DateOnly date)
    {
        foreach (var ev in content.Events)
        {
            if (IsActive(ev, date))
            {
                return ev;
            }
        }

        return null;
    }

    public static bool IsActive(SeasonalEvent ev, DateOnly date)
    {
        var day = (date.Month * 100) + date.Day;
        var start = (ev.StartMonth * 100) + ev.StartDay;
        var end = (ev.EndMonth * 100) + ev.EndDay;
        // Ranges that cross New Year (e.g. 20 Dec - 5 Jan) wrap around.
        return start <= end ? day >= start && day <= end : day >= start || day <= end;
    }

    public float CoinMultiplier(string recipeId) =>
        ActiveEvent is { } ev && ev.FeaturedRecipes.Contains(recipeId) ? ev.CoinMultiplier : 1f;
}
