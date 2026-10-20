using Avalonia.Controls;

namespace HappyCooking.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => FitToScreen();
    }

    /// <summary>
    /// Keeps the requested size inside the screen's working area. A window asked to be
    /// larger than the screen is clamped by the OS, but layout would still be measured
    /// at the requested size and content would spill under the dock.
    /// </summary>
    private void FitToScreen()
    {
        if (Screens.ScreenFromWindow(this) is not { } screen)
        {
            return;
        }

        var area = screen.WorkingArea.Size.ToSize(screen.Scaling);
        if (Width > area.Width * 0.96 || Height > area.Height * 0.94)
        {
            WindowState = WindowState.Maximized;
        }
    }
}
