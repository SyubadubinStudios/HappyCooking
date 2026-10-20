namespace HappyCooking.Core.Services;

/// <summary>Wall-clock time, abstracted so farm growth, pets and events can be tested.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Local calendar date, used for seasonal events.</summary>
    DateOnly Today { get; }
}

public sealed class SystemClock : IClock
{
    public static SystemClock Instance { get; } = new();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}

/// <summary>A clock that only moves when told to. Used by tests and the screenshot tour.</summary>
public sealed class ManualClock(DateTimeOffset start) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = start;

    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);

    public void Advance(TimeSpan by) => UtcNow += by;
}
