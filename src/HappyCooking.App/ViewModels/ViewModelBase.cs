using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HappyCooking.App.Rendering;
using HappyCooking.App.Services;
using HappyCooking.Core;

namespace HappyCooking.App.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    protected static GameHost Host => GameHost.Current;

    protected static GameSession Game => GameHost.Current.Session;

    protected static Loc L => Loc.Instance;

    protected static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

/// <summary>
/// A full screen. The shell activates it when shown and deactivates it when
/// leaving, which is where 3D stages are created and released.
/// </summary>
public abstract partial class PageViewModel : ViewModelBase
{
    /// <summary>Dock id this page highlights ("map", "recipes", ...).</summary>
    public abstract string DockKey { get; }

    /// <summary>The 3D scene behind this page, if any. Created on activation.</summary>
    [ObservableProperty]
    private Stage? _stage;

    public ShellViewModel Shell { get; internal set; } = null!;

    public virtual void OnActivated()
    {
    }

    public virtual void OnDeactivated()
    {
        var stage = Stage;
        Stage = null;
        stage?.Dispose();
    }

    /// <summary>Re-reads every localised text after the language changes.</summary>
    public virtual void RefreshTexts()
    {
    }
}
