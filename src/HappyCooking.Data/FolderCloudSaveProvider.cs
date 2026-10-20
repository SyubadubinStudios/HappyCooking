using HappyCooking.Core.Services;
using HappyCooking.Core.State;

namespace HappyCooking.Data;

/// <summary>
/// Cloud save through a folder that a sync client already mirrors (OneDrive,
/// Google Drive, Dropbox, iCloud Drive). No accounts or servers of our own,
/// which keeps a children's game free of logins. Writes go to a temporary file
/// first and are then moved into place, so a half-synced save is never read.
/// </summary>
public sealed class FolderCloudSaveProvider(string folder) : ICloudSaveProvider
{
    private const string FileName = "happycooking-save.json";

    public string Name => "Synced folder";

    public string Folder { get; } = folder;

    private string FilePath => Path.Combine(Folder, FileName);

    public bool IsAvailable => !string.IsNullOrWhiteSpace(Folder) && Directory.Exists(Folder);

    public DateTimeOffset? RemoteUpdatedUtc()
    {
        var remote = Download();
        return remote?.UpdatedUtc;
    }

    public void Upload(PlayerProfile profile)
    {
        if (!IsAvailable)
        {
            throw new DirectoryNotFoundException($"The cloud folder '{Folder}' does not exist.");
        }

        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, ProfileSerializer.Serialize(profile));
        File.Move(temp, FilePath, overwrite: true);
    }

    public PlayerProfile? Download()
    {
        if (!IsAvailable || !File.Exists(FilePath))
        {
            return null;
        }

        try
        {
            return ProfileSerializer.Deserialize(File.ReadAllText(FilePath));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException)
        {
            // A file mid-sync or from a newer build: treat as "no usable cloud copy".
            return null;
        }
    }
}

public enum SyncOutcome
{
    NotConfigured,
    Uploaded,
    Downloaded,
    UpToDate,
    Failed,
}

/// <summary>Two-way sync: whichever copy was updated last wins.</summary>
public static class CloudSync
{
    public static SyncOutcome Sync(ICloudSaveProvider cloud, PlayerProfile local, out PlayerProfile? newer)
    {
        newer = null;
        if (!cloud.IsAvailable)
        {
            return SyncOutcome.NotConfigured;
        }

        try
        {
            var remote = cloud.Download();
            if (remote is null || local.UpdatedUtc > remote.UpdatedUtc)
            {
                cloud.Upload(local);
                return SyncOutcome.Uploaded;
            }

            if (remote.UpdatedUtc > local.UpdatedUtc)
            {
                newer = remote;
                return SyncOutcome.Downloaded;
            }

            return SyncOutcome.UpToDate;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return SyncOutcome.Failed;
        }
    }
}
