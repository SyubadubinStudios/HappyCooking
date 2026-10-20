using Avalonia.Threading;
using HappyCooking.Core;
using HappyCooking.Core.Content;
using HappyCooking.Core.Events;
using HappyCooking.Core.Services;
using HappyCooking.Core.State;
using HappyCooking.Data;

namespace HappyCooking.App.Services;

/// <summary>A message for the toast stack (level up, quest done, trophy, ...).</summary>
public sealed record Toast(string Title, string Message, string Icon);

/// <summary>
/// Owns the running <see cref="GameSession"/>, saving and the app-wide services.
/// One instance for the life of the app (see <see cref="Current"/>).
/// </summary>
public sealed class GameHost : IDisposable
{
    private readonly DispatcherTimer _autosave;
    private readonly DispatcherTimer _tick;
    private DateTime _lastTick = DateTime.UtcNow;
    private bool _dirty;
    private bool _savesLocked;

    private GameHost(ISaveStore store, PlayerProfile profile, bool isNewPlayer, IClock clock, int? seed)
    {
        Store = store;
        IsNewPlayer = isNewPlayer;
        Session = new GameSession(GameContent.Default, profile, clock, seed);
        Session.Events.Subscribe(OnGameEvent);
        Loc.Instance.Language = profile.Settings.Language;

        Sfx = new Sfx
        {
            EffectsVolume = profile.Settings.EffectsVolume,
            MusicVolume = profile.Settings.MusicVolume,
        };

        _autosave = new DispatcherTimer(TimeSpan.FromSeconds(15), DispatcherPriority.Background, (_, _) => SaveIfDirty());
        _autosave.Start();

        // The farm keeps growing (and rain keeps watering it) while any screen is open.
        _tick = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => Tick());
        _tick.Start();
    }

    public static GameHost Current { get; private set; } = null!;

    public GameSession Session { get; }

    public GameContent Content => Session.Content;

    public PlayerProfile Profile => Session.Profile;

    public ISaveStore Store { get; }

    public Sfx Sfx { get; }

    public bool IsNewPlayer { get; private set; }

    /// <summary>Raised on the UI thread whenever the profile changed in a way the HUD shows.</summary>
    public event Action? Changed;

    public event Action<Toast>? ToastRaised;

    /// <summary>Raised when Mr. Burnt Onion should make an appearance.</summary>
    public event Action? BurntDish;

    /// <param name="demo">Starts from an in-memory, well-progressed profile (used by the screenshot tour).</param>
    public static GameHost Start(string? databasePath = null, bool demo = false)
    {
        ISaveStore store;
        PlayerProfile? profile = null;
        IClock clock = SystemClock.Instance;
        int? seed = null;

        if (demo)
        {
            store = new MemorySaveStore();
            profile = DemoProfile.Create(GameContent.Default, clock);
            seed = 42;
        }
        else
        {
            try
            {
                store = new SqliteSaveStore(databasePath ?? SqliteSaveStore.DefaultPath());
                profile = store.Load();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException or InvalidDataException)
            {
                // Storage is broken or the save is unreadable: keep playing in memory rather than crash.
                System.Diagnostics.Trace.TraceError($"Save unavailable, playing without saving: {ex.Message}");
                store = new MemorySaveStore();
            }
        }

        var isNew = profile is null;
        profile ??= GameSession.NewProfile(GameContent.Default, "Chef", clock);
        Current = new GameHost(store, profile, isNew, clock, seed);
        return Current;
    }

    public void SetPlayerName(string name)
    {
        Profile.PlayerName = string.IsNullOrWhiteSpace(name) ? "Chef" : name.Trim()[..Math.Min(name.Trim().Length, 16)];
        IsNewPlayer = false;
        Touch();
        Save();
    }

    /// <summary>Marks the profile as changed: refreshes the HUD and schedules an autosave.</summary>
    public void Touch()
    {
        _dirty = true;
        Profile.UpdatedUtc = Session.Clock.UtcNow;
        Changed?.Invoke();
    }

    /// <summary>
    /// Stops all further saving for this run. Used after a newer cloud save was written
    /// to storage, so the older profile in memory cannot overwrite it before a restart.
    /// </summary>
    public void LockSaves() => _savesLocked = true;

    public bool SavesLocked => _savesLocked;

    public void Save()
    {
        if (_savesLocked)
        {
            return;
        }

        Profile.UpdatedUtc = Session.Clock.UtcNow;
        Store.Save(Profile);
        _dirty = false;
    }

    private void SaveIfDirty()
    {
        if (_dirty)
        {
            Save();
        }
    }

    private void Tick()
    {
        var now = DateTime.UtcNow;
        var dt = (float)Math.Min(1.0, (now - _lastTick).TotalSeconds);
        _lastTick = now;
        Session.Farm.Update(dt);
    }

    public void Toast(string title, string message, string icon) => ToastRaised?.Invoke(new Toast(title, message, icon));

    private void OnGameEvent(GameEvent e)
    {
        var loc = Loc.Instance;
        var content = Content;
        switch (e.Kind)
        {
            case GameEventKind.LevelUp:
                Sfx.Play(Sound.LevelUp);
                Toast(loc.Format("toast.levelUp", e.Amount), loc["toast.levelUpBody"], "star");
                break;
            case GameEventKind.RegionUnlocked when content.RegionById.TryGetValue(e.Id!, out var region):
                Toast(loc["toast.newIsland"], loc.Get(region.Name), "map");
                break;
            case GameEventKind.QuestCompleted when content.QuestById.TryGetValue(e.Id!, out var quest):
                Sfx.Play(Sound.Perfect);
                Toast(loc["toast.questDone"], loc.Get(quest.Title), "recipes");
                break;
            case GameEventKind.AchievementUnlocked when content.AchievementById.TryGetValue(e.Id!, out var trophy):
                Toast(loc["toast.trophy"], loc.Get(trophy.Title), "trophy");
                break;
            case GameEventKind.DishBurnt:
                BurntDish?.Invoke();
                break;
        }

        Touch();
    }

    public void Dispose()
    {
        _autosave.Stop();
        _tick.Stop();
        try
        {
            Save();
        }
        catch (Exception ex) when (ex is IOException or Microsoft.Data.Sqlite.SqliteException)
        {
            System.Diagnostics.Trace.TraceError($"Final save failed: {ex.Message}");
        }

        Sfx.Dispose();
        (Store as IDisposable)?.Dispose();
    }
}
