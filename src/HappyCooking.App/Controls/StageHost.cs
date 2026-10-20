using Avalonia;
using Avalonia.Controls;
using HappyCooking.App.Rendering;
using HappyCooking.App.Services;
using ThreeNet.Avalonia;

namespace HappyCooking.App.Controls;

/// <summary>
/// Shows a <see cref="Rendering.Stage"/> inside the Avalonia layout through a
/// <see cref="ThreeNetView"/>, ticking the stage once per rendered frame. Settings
/// come from the player's graphics quality.
/// </summary>
public sealed class StageHost : Decorator
{
    public static readonly StyledProperty<Stage?> StageProperty =
        AvaloniaProperty.Register<StageHost, Stage?>(nameof(Stage));

    private readonly ThreeNetView _view;

    public StageHost()
    {
        var quality = GameHost.Current?.Profile.Settings.Graphics ?? Core.State.GraphicsQuality.High;
        _view = new ThreeNetView
        {
            RendererOptions = Stage.OptionsFor(quality),
            RenderScale = Stage.RenderScaleFor(quality),
            MaxFramesPerSecond = quality == Core.State.GraphicsQuality.Low ? 30 : 60,
        };
        _view.Frame += OnFrame;
        _view.RenderFailed += (_, message) => System.Diagnostics.Trace.TraceError($"3D view failed: {message}");
        Child = _view;
    }

    public Stage? Stage
    {
        get => GetValue(StageProperty);
        set => SetValue(StageProperty, value);
    }

    public ThreeNetView View => _view;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StageProperty)
        {
            Attach(change.GetNewValue<Stage?>());
        }
    }

    private void Attach(Stage? stage)
    {
        _view.Scene = stage?.Scene;
        _view.Camera = stage?.Camera;
        _view.IsRendering = stage is not null;
        if (stage is IInteractiveStage interactive)
        {
            interactive.Bind(_view.Interaction);
        }
    }

    private void OnFrame(object? sender, FrameEventArgs e)
    {
        // Clamp so a stall (window drag, GC) does not make everything jump.
        Stage?.Update(MathF.Min(e.DeltaSeconds, 0.1f), e.TotalSeconds);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _view.IsRendering = false;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _view.IsRendering = Stage is not null;
    }
}

/// <summary>A stage that wants pointer events on its 3D objects.</summary>
public interface IInteractiveStage
{
    void Bind(ThreeNet.InteractionManager? interaction);
}
