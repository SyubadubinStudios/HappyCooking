using HappyCooking.Core.State;

namespace HappyCooking.Core.Services;

/// <summary>Durable local storage for the player profile (SQLite on every platform).</summary>
public interface ISaveStore
{
    /// <summary>The saved profile for <paramref name="slot"/>, or null when there is none yet.</summary>
    PlayerProfile? Load(string slot = "main");

    void Save(PlayerProfile profile, string slot = "main");

    /// <summary>Appends one line to the cooking history.</summary>
    void LogCooking(string recipeId, int stars, float score, DateTimeOffset whenUtc);

    IReadOnlyList<CookingLogEntry> RecentCooking(int max);
}

public readonly record struct CookingLogEntry(string RecipeId, int Stars, float Score, DateTimeOffset WhenUtc);

/// <summary>Keeps everything in memory. Used by tests and as a fallback when storage is unavailable.</summary>
public sealed class MemorySaveStore : ISaveStore
{
    private readonly Dictionary<string, string> _slots = new(StringComparer.Ordinal);
    private readonly List<CookingLogEntry> _log = [];

    public PlayerProfile? Load(string slot = "main") =>
        _slots.TryGetValue(slot, out var json) ? ProfileSerializer.Deserialize(json) : null;

    public void Save(PlayerProfile profile, string slot = "main") => _slots[slot] = ProfileSerializer.Serialize(profile);

    public void LogCooking(string recipeId, int stars, float score, DateTimeOffset whenUtc) =>
        _log.Add(new CookingLogEntry(recipeId, stars, score, whenUtc));

    public IReadOnlyList<CookingLogEntry> RecentCooking(int max) =>
        _log.AsEnumerable().Reverse().Take(max).ToArray();
}

/// <summary>
/// Somewhere outside the device that a save can be mirrored to and restored from.
/// The shipped provider writes to a synced folder (OneDrive, Google Drive, Dropbox, iCloud Drive).
/// </summary>
public interface ICloudSaveProvider
{
    string Name { get; }

    bool IsAvailable { get; }

    /// <summary>When the cloud copy was last written, or null when there is no cloud copy.</summary>
    DateTimeOffset? RemoteUpdatedUtc();

    void Upload(PlayerProfile profile);

    PlayerProfile? Download();
}
