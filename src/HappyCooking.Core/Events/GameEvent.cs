namespace HappyCooking.Core.Events;

public enum GameEventKind
{
    DishCooked,
    DishBurnt,
    CustomerServed,
    CropHarvested,
    FishCaught,
    PetAdopted,
    PetFed,
    DecorationPlaced,
    GiftSent,
    LevelUp,
    RegionUnlocked,
    QuestCompleted,
    AchievementUnlocked,
}

/// <summary>
/// Something that happened in the game. A small readonly struct so raising one
/// never allocates; <see cref="Id"/> and <see cref="Extra"/> carry context such as
/// the recipe and region of a cooked dish.
/// </summary>
public readonly record struct GameEvent(GameEventKind Kind, string? Id = null, int Amount = 1, string? Extra = null);

/// <summary>
/// Minimal synchronous publish/subscribe hub that lets systems react to each other
/// (quests and achievements listen to cooking, fishing, ...) without references
/// between them.
/// </summary>
public sealed class GameEvents
{
    private Action<GameEvent>[] _handlers = [];

    public void Subscribe(Action<GameEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers = [.. _handlers, handler];
    }

    public void Unsubscribe(Action<GameEvent> handler) =>
        _handlers = Array.FindAll(_handlers, h => h != handler);

    public void Raise(in GameEvent e)
    {
        // Iterate a snapshot so a handler may subscribe or unsubscribe while we run.
        var handlers = _handlers;
        for (var i = 0; i < handlers.Length; i++)
        {
            handlers[i](e);
        }
    }
}
