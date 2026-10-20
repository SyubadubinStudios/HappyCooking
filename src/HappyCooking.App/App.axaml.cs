using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HappyCooking.App.Services;
using HappyCooking.App.ViewModels;
using HappyCooking.App.Views;

namespace HappyCooking.App;

public partial class App : Application
{
    /// <summary>Start options set by the platform head before the app runs.</summary>
    public static AppOptions Options { get; set; } = new();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        ThreeNet.ThreeNetRuntime.Initialize();
        var host = GameHost.Start(Options.DatabasePath, Options.Demo);
        var shell = new ShellViewModel(startAtMap: Options.SkipTitle);

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow { DataContext = shell };
                desktop.ShutdownRequested += (_, _) => host.Dispose();
                break;
            case ISingleViewApplicationLifetime single:
                single.MainView = new ShellView { DataContext = shell };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}

/// <summary>How the app was launched (command line on desktop, defaults on mobile).</summary>
public sealed record AppOptions
{
    /// <summary>Use an in-memory, mid-game profile instead of the real save.</summary>
    public bool Demo { get; init; }

    /// <summary>Open straight on the world map.</summary>
    public bool SkipTitle { get; init; }

    public string? DatabasePath { get; init; }
}
