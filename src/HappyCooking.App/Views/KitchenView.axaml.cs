using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using HappyCooking.App.ViewModels;

namespace HappyCooking.App.Views;

public partial class KitchenView : UserControl
{
    public KitchenView()
    {
        InitializeComponent();

        // Pour steps are "hold, then let go": listen to raw press/release on the big button,
        // including events the Button itself already handled.
        ActionButton.AddHandler(PointerPressedEvent, (_, _) => (DataContext as KitchenViewModel)?.BeginHold(), RoutingStrategies.Tunnel, handledEventsToo: true);
        ActionButton.AddHandler(PointerReleasedEvent, (_, _) => (DataContext as KitchenViewModel)?.EndHold(), RoutingStrategies.Tunnel, handledEventsToo: true);
        ActionButton.AddHandler(PointerCaptureLostEvent, (_, _) => (DataContext as KitchenViewModel)?.EndHold(), RoutingStrategies.Direct, handledEventsToo: true);

        // Space bar works as the big button for keyboard players.
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;

        PlatePad.PointerPressed += OnPlatePressed;
    }

    private void OnPlatePressed(object? sender, PointerPressedEventArgs e)
    {
        var p = e.GetPosition(PlatePad);
        (DataContext as KitchenViewModel)?.PlaceTopping(p.X / PlatePad.Bounds.Width, p.Y / PlatePad.Bounds.Height);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space || DataContext is not KitchenViewModel vm)
        {
            return;
        }

        vm.BeginHold();
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            vm.ActionCommand.Execute(null);
        }

        e.Handled = true;
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && DataContext is KitchenViewModel vm)
        {
            vm.EndHold();
            e.Handled = true;
        }
    }
}
