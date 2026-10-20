using Avalonia;
using Avalonia.iOS;
using Foundation;

namespace HappyCooking.iOS;

[Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App.App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder).LogToTrace();
}
