using System.Globalization;
using HappyCooking.Core.Services;
using HappyCooking.Core.State;
using Microsoft.Data.Sqlite;

namespace HappyCooking.Data;

/// <summary>
/// Saves the profile to a local SQLite database. The profile is stored as one JSON
/// document per slot (it is small and always loaded whole); the cooking history
/// is a real table so it can grow without rewriting the save.
/// </summary>
public sealed class SqliteSaveStore : ISaveStore, IDisposable
{
    private const int SchemaVersion = 1;
    private readonly SqliteConnection _connection;
    private readonly Lock _gate = new();

    public SqliteSaveStore(string databasePath)
    {
        DatabasePath = databasePath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
        _connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString());
        _connection.Open();
        Migrate();
    }

    public string DatabasePath { get; }

    /// <summary>The default per-user database location for the current platform.</summary>
    public static string DefaultPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
        return Path.Combine(root, "HappyCooking", "happycooking.db");
    }

    private void Migrate()
    {
        Execute("PRAGMA journal_mode=WAL;");
        Execute("PRAGMA synchronous=NORMAL;");
        Execute("""
            CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS saves (
                slot        TEXT PRIMARY KEY,
                json        TEXT NOT NULL,
                version     INTEGER NOT NULL,
                updated_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS cooking_log (
                id        INTEGER PRIMARY KEY AUTOINCREMENT,
                recipe_id TEXT NOT NULL,
                stars     INTEGER NOT NULL,
                score     REAL NOT NULL,
                when_utc  TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_cooking_log_when ON cooking_log (when_utc);
            """);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO meta (key, value) VALUES ('schema', $v) ON CONFLICT(key) DO UPDATE SET value = excluded.value;";
        cmd.Parameters.AddWithValue("$v", SchemaVersion.ToString(CultureInfo.InvariantCulture));
        cmd.ExecuteNonQuery();
    }

    public PlayerProfile? Load(string slot = "main")
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT json FROM saves WHERE slot = $slot;";
            cmd.Parameters.AddWithValue("$slot", slot);
            return cmd.ExecuteScalar() is string json ? ProfileSerializer.Deserialize(json) : null;
        }
    }

    public void Save(PlayerProfile profile, string slot = "main")
    {
        var json = ProfileSerializer.Serialize(profile);
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO saves (slot, json, version, updated_utc) VALUES ($slot, $json, $version, $updated)
                ON CONFLICT(slot) DO UPDATE SET json = excluded.json, version = excluded.version, updated_utc = excluded.updated_utc;
                """;
            cmd.Parameters.AddWithValue("$slot", slot);
            cmd.Parameters.AddWithValue("$json", json);
            cmd.Parameters.AddWithValue("$version", profile.Version);
            cmd.Parameters.AddWithValue("$updated", profile.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture));
            cmd.ExecuteNonQuery();
        }
    }

    public void LogCooking(string recipeId, int stars, float score, DateTimeOffset whenUtc)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO cooking_log (recipe_id, stars, score, when_utc) VALUES ($r, $s, $sc, $w);";
            cmd.Parameters.AddWithValue("$r", recipeId);
            cmd.Parameters.AddWithValue("$s", stars);
            cmd.Parameters.AddWithValue("$sc", score);
            cmd.Parameters.AddWithValue("$w", whenUtc.ToString("O", CultureInfo.InvariantCulture));
            cmd.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<CookingLogEntry> RecentCooking(int max)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT recipe_id, stars, score, when_utc FROM cooking_log ORDER BY id DESC LIMIT $max;";
            cmd.Parameters.AddWithValue("$max", max);
            using var reader = cmd.ExecuteReader();
            var list = new List<CookingLogEntry>(max);
            while (reader.Read())
            {
                list.Add(new CookingLogEntry(
                    reader.GetString(0),
                    reader.GetInt32(1),
                    reader.GetFloat(2),
                    DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
            }

            return list;
        }
    }

    private void Execute(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();
}
