using Avalonia;
using HappyCooking.App;

namespace HappyCooking.Desktop;

internal static class Program
{
    /// <summary>
    /// Command line:
    ///   --demo                 play with a mid-game profile that is never saved
    ///   --skip-title           open on the world map
    ///   --db &lt;path&gt;            use a specific save database
    ///   --screenshots &lt;dir&gt;    run the screenshot tour (implies --demo) and exit
    /// </summary>
    [STAThread]
    public static int Main(string[] args)
    {
        var screenshots = ValueOf(args, "--screenshots");
        App.App.Options = new AppOptions
        {
            Demo = screenshots is not null || args.Contains("--demo"),
            SkipTitle = args.Contains("--skip-title"),
            DatabasePath = ValueOf(args, "--db"),
        };

        if (screenshots is not null)
        {
            ScreenshotTour.Schedule(Path.GetFullPath(screenshots));
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static string? ValueOf(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App.App>()
            .UsePlatformDetect()
            .LogToTrace();
}
