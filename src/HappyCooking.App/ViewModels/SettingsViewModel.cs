using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HappyCooking.App.Services;
using HappyCooking.Core.Localization;
using HappyCooking.Core.State;
using HappyCooking.Data;

namespace HappyCooking.App.ViewModels;

/// <summary>Language, sound, graphics, cloud save and the About card with the credits.</summary>
public sealed partial class SettingsViewModel : PageViewModel
{
    [ObservableProperty]
    private double _musicVolume;

    [ObservableProperty]
    private double _effectsVolume;

    [ObservableProperty]
    private int _graphicsIndex;

    [ObservableProperty]
    private bool _isEnglish;

    [ObservableProperty]
    private string _cloudFolder = string.Empty;

    [ObservableProperty]
    private string _cloudStatus = string.Empty;

    [ObservableProperty]
    private string _playerName = string.Empty;

    public SettingsViewModel()
    {
        var s = Host.Profile.Settings;
        _musicVolume = s.MusicVolume * 100;
        _effectsVolume = s.EffectsVolume * 100;
        _graphicsIndex = (int)s.Graphics;
        _isEnglish = s.Language == Language.English;
        _cloudFolder = s.CloudFolder ?? string.Empty;
        _playerName = Host.Profile.PlayerName;
    }

    public override string DockKey => "settings";

    public string Version { get; } = typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public string Credit => L["about.credit"];

    public string SaveLocation => (Host.Store as SqliteSaveStore)?.DatabasePath ?? L["settings.memoryOnly"];

    partial void OnMusicVolumeChanged(double value)
    {
        Host.Profile.Settings.MusicVolume = (float)(value / 100);
        Host.Sfx.MusicVolume = (float)(value / 100);
        Host.Touch();
    }

    partial void OnEffectsVolumeChanged(double value)
    {
        Host.Profile.Settings.EffectsVolume = (float)(value / 100);
        Host.Sfx.EffectsVolume = (float)(value / 100);
        Host.Sfx.Play(Sound.Pop);
        Host.Touch();
    }

    public bool IsLow => GraphicsIndex == 0;

    public bool IsMedium => GraphicsIndex == 1;

    public bool IsHigh => GraphicsIndex == 2;

    partial void OnGraphicsIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsLow));
        OnPropertyChanged(nameof(IsMedium));
        OnPropertyChanged(nameof(IsHigh));
        Host.Profile.Settings.Graphics = (GraphicsQuality)Math.Clamp(value, 0, 2);
        Host.Touch();
    }

    [RelayCommand]
    private void SetLanguage(string code)
    {
        var lang = code == "id" ? Language.Indonesian : Language.English;
        IsEnglish = lang == Language.English;
        Host.Profile.Settings.Language = lang;
        Loc.Instance.Language = lang;
        Host.Touch();
    }

    [RelayCommand]
    private void SetGraphics(string level) => GraphicsIndex = int.Parse(level, System.Globalization.CultureInfo.InvariantCulture);

    [RelayCommand]
    private void SaveName()
    {
        Host.SetPlayerName(PlayerName);
        PlayerName = Host.Profile.PlayerName;
    }

    [RelayCommand]
    private void SyncCloud()
    {
        var folder = CloudFolder.Trim();
        Host.Profile.Settings.CloudFolder = folder.Length == 0 ? null : folder;
        if (folder.Length == 0)
        {
            CloudStatus = L["cloud.off"];
            Host.Touch();
            return;
        }

        Host.Save();
        var outcome = CloudSync.Sync(new FolderCloudSaveProvider(folder), Host.Profile, out var newer);
        if (outcome == SyncOutcome.Downloaded && newer is not null)
        {
            // A newer save from another device: store it, and stop saving the older one in
            // memory so it cannot overwrite it. It is loaded on the next start.
            Host.Store.Save(newer);
            Host.LockSaves();
            CloudStatus = L["cloud.downloaded"];
        }
        else
        {
            CloudStatus = L["cloud." + outcome.ToString().ToLowerInvariant()];
        }
    }

    public override void RefreshTexts()
    {
        OnPropertyChanged(nameof(Credit));
        OnPropertyChanged(nameof(SaveLocation));
    }
}
