using System.Text.Json;

namespace HappyCooking.Core.State;

/// <summary>Turns a <see cref="PlayerProfile"/> into JSON and back, upgrading old saves on the way in.</summary>
public static class ProfileSerializer
{
    public static string Serialize(PlayerProfile profile) =>
        JsonSerializer.Serialize(profile, ProfileJsonContext.Default.PlayerProfile);

    public static PlayerProfile Deserialize(string json) =>
        ProfileMigrations.Upgrade(JsonSerializer.Deserialize(json, ProfileJsonContext.Default.PlayerProfile)
            ?? throw new InvalidDataException("The save file is empty."));

    /// <summary>A deep copy, made by a serialisation round trip.</summary>
    public static PlayerProfile Clone(PlayerProfile profile) => Deserialize(Serialize(profile));
}

/// <summary>
/// Upgrades saves written by older versions. Each step takes a profile at
/// version N to N+1, so a save from any release reaches <see cref="PlayerProfile.CurrentVersion"/>.
/// </summary>
public static class ProfileMigrations
{
    public static PlayerProfile Upgrade(PlayerProfile profile)
    {
        if (profile.Version > PlayerProfile.CurrentVersion)
        {
            throw new InvalidDataException(
                $"This save was made by a newer version of Happy Cooking (save v{profile.Version}, game v{PlayerProfile.CurrentVersion}).");
        }

        // Version 1 is the first public format. Future steps go here, for example:
        // if (profile.Version == 1) { /* move data */ profile.Version = 2; }
        profile.Version = PlayerProfile.CurrentVersion;
        return profile;
    }
}
