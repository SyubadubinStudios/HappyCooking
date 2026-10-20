using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using HappyCooking.App.Services;
using HappyCooking.App.ViewModels;
using HappyCooking.Core.Localization;

namespace HappyCooking.Desktop;

/// <summary>
/// Drives the real app through every screen with the demo profile and saves a PNG
/// of each, in English and in Indonesian. Used to produce the README and docs
/// screenshots: <c>HappyCooking --screenshots docs/screenshots</c>.
/// </summary>
internal static class ScreenshotTour
{
    public static void Schedule(string folder)
    {
        Directory.CreateDirectory(folder);
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await Run(folder);
            }
            catch (Exception ex)
            {
                await File.WriteAllTextAsync(Path.Combine(folder, "tour-error.txt"), ex.ToString());
            }
            finally
            {
                (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
            }
        }, DispatcherPriority.Background);
    }

    private static async Task Run(string folder)
    {
        var window = await WaitForWindow();
        // A fixed 16:10 size inside every common screen, so all screenshots match.
        window.WindowState = WindowState.Normal;
        window.Width = 1280;
        window.Height = 720;
        await Delay(500);
        var shell = (ShellViewModel)window.DataContext!;

        foreach (var (lang, suffix) in new[] { (Language.English, "en"), (Language.Indonesian, "id") })
        {
            Loc.Instance.Language = lang;
            GameHost.Current.Profile.Settings.Language = lang;

            shell.Navigate("title");
            await Shot(window, folder, $"01-title-{suffix}", 4500);

            shell.Navigate("map");
            await Shot(window, folder, $"02-world-map-{suffix}");

            shell.OpenKitchen("sate");
            await Shot(window, folder, $"03-kitchen-prep-{suffix}");
            await Cook(window, shell, folder, suffix);

            shell.Navigate("restaurant");
            await Delay(1500);
            GameHost.Current.Session.Restaurant.TrySpawn();
            GameHost.Current.Session.Restaurant.TrySpawn();
            await Shot(window, folder, $"07-restaurant-{suffix}", 3500);

            shell.Navigate("farm");
            await Shot(window, folder, $"08-farm-{suffix}", 3500);

            shell.Navigate("fishing");
            await Delay(1500);
            if (shell.CurrentPage is FishingViewModel fishing)
            {
                fishing.TapCommand.Execute(null);
            }

            await Shot(window, folder, $"09-fishing-{suffix}", 2500);

            shell.Navigate("pets");
            await Shot(window, folder, $"10-pets-{suffix}");

            shell.Navigate("recipes");
            await Shot(window, folder, $"11-recipe-book-{suffix}", 3500);

            shell.Navigate("trophies");
            await Shot(window, folder, $"12-trophies-{suffix}");

            shell.Navigate("friends");
            await Shot(window, folder, $"13-friends-{suffix}");

            shell.Navigate("settings");
            await Shot(window, folder, $"14-settings-{suffix}");
        }
    }

    /// <summary>Plays Satay: chop, skewer, grill (screenshot mid-flame), sauce, result.</summary>
    private static async Task Cook(Window window, ShellViewModel shell, string folder, string suffix)
    {
        if (shell.CurrentPage is not KitchenViewModel kitchen)
        {
            return;
        }

        kitchen.StartCommand.Execute(null);
        await Delay(600);
        for (var i = 0; i < 5; i++)
        {
            kitchen.ActionCommand.Execute(null);
            await Delay(120);
        }

        await Shot(window, folder, $"04-kitchen-chop-{suffix}", 400);
        while (kitchen.IsTap)
        {
            kitchen.ActionCommand.Execute(null);
            await Delay(120);
        }

        for (var i = 0; kitchen.IsDecorate && i < 10; i++)
        {
            kitchen.PlaceTopping(0.3 + (i * 0.1), 0.5);
            await Delay(250);
        }

        // Grill: wait until the needle is inside the zone, then take the picture and stop it.
        for (var i = 0; kitchen.IsMeter && !kitchen.InZone && i < 200; i++)
        {
            await Delay(30);
        }

        await Shot(window, folder, $"05-kitchen-grill-{suffix}", 0);
        kitchen.ActionCommand.Execute(null);
        await Delay(500);

        kitchen.BeginHold();
        for (var i = 0; kitchen.IsMeter && !kitchen.InZone && i < 200; i++)
        {
            await Delay(30);
        }

        await Delay(150);
        kitchen.EndHold();
        await Shot(window, folder, $"06-kitchen-result-{suffix}", 2600);
    }

    private static async Task<Window> WaitForWindow()
    {
        for (var i = 0; i < 200; i++)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { IsVisible: true } w })
            {
                return w;
            }

            await Delay(50);
        }

        throw new TimeoutException("The main window did not open.");
    }

    private static Task Delay(int ms) => Task.Delay(ms);

    private static async Task Shot(Window window, string folder, string name, int settleMs = 2500)
    {
        if (settleMs > 0)
        {
            await Delay(settleMs);
        }

        var scale = window.RenderScaling;
        var size = new PixelSize((int)(window.ClientSize.Width * scale), (int)(window.ClientSize.Height * scale));
        using var bitmap = new RenderTargetBitmap(size, new Vector(96 * scale, 96 * scale));
        bitmap.Render(window);
        bitmap.Save(Path.Combine(folder, name + ".png"), new PngBitmapEncoderOptions());
    }
}
