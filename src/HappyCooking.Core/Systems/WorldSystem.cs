using HappyCooking.Core.Content;
using HappyCooking.Core.Services;

namespace HappyCooking.Core.Systems;

public enum Weather
{
    Sunny,
    Cloudy,
    Rain,
    Snow,
}

/// <summary>
/// The living world: a day/night cycle and weather. Both are derived from the
/// wall clock (no state to save), so every screen agrees on the time and the
/// weather without talking to each other.
/// </summary>
public sealed class WorldSystem(IClock clock)
{
    /// <summary>One in-game day lasts this many real minutes.</summary>
    public const double DayLengthMinutes = 24;

    /// <summary>Weather changes every this many real minutes.</summary>
    public const double WeatherBlockMinutes = 6;

    /// <summary>0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset.</summary>
    public float TimeOfDay
    {
        get
        {
            var minutes = clock.UtcNow.ToUnixTimeMilliseconds() / 60000.0;
            var t = (minutes % DayLengthMinutes) / DayLengthMinutes;
            return (float)t;
        }
    }

    public bool IsNight => TimeOfDay < 0.22f || TimeOfDay > 0.8f;

    /// <summary>How bright the sun is, 0 at night to 1 at noon.</summary>
    public float Daylight => Math.Clamp(MathF.Sin((TimeOfDay - 0.22f) / 0.58f * MathF.PI), 0f, 1f);

    public Weather WeatherIn(Region region) => WeatherIn(region.Id);

    public Weather WeatherIn(string regionId)
    {
        var block = (long)(clock.UtcNow.ToUnixTimeMilliseconds() / (WeatherBlockMinutes * 60000));
        var roll = Hash(block, regionId) % 100;
        var snowy = regionId == "dessert";
        return roll switch
        {
            < 50 => Weather.Sunny,
            < 75 => Weather.Cloudy,
            _ => snowy ? Weather.Snow : Weather.Rain,
        };
    }

    private static uint Hash(long block, string id)
    {
        // FNV-1a over the block number and region id: stable across runs and platforms.
        var h = 2166136261u;
        for (var i = 0; i < 8; i++)
        {
            h = (h ^ (byte)(block >> (i * 8))) * 16777619u;
        }

        foreach (var c in id)
        {
            h = (h ^ c) * 16777619u;
        }

        return h;
    }
}
