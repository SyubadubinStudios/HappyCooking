using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HappyCooking.App.Rendering;
using HappyCooking.App.Services;

namespace HappyCooking.App.ViewModels;

/// <summary>The welcome screen: the cast on stage, the logo, and the Play button (plus a name prompt the first time).</summary>
public sealed partial class TitleViewModel : PageViewModel
{
    [ObservableProperty]
    private string _playerName;

    [ObservableProperty]
    private bool _askName;

    public TitleViewModel()
    {
        _askName = Host.IsNewPlayer;
        _playerName = Host.IsNewPlayer ? string.Empty : Host.Profile.PlayerName;
    }

    public override string DockKey => "title";

    public Bitmap? Logo { get; } = Images.Get("logo.png", 900);

    public string Welcome => Host.IsNewPlayer ? L["title.welcomeNew"] : L.Format("title.welcomeBack", Host.Profile.PlayerName);

    public override void OnActivated()
    {
        Stage = new TitleStage();
        Host.Sfx.StartMusic();
    }

    public override void RefreshTexts() => OnPropertyChanged(nameof(Welcome));

    [RelayCommand]
    private void Play()
    {
        if (AskName)
        {
            Host.SetPlayerName(PlayerName);
        }

        (Stage as TitleStage)?.CheerHost();
        Host.Sfx.Play(Sound.Perfect);
        Shell.Navigate("map");
    }
}
