using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace HappyCooking.App.Controls;

/// <summary>
/// The candy-stick meter used by the Heat, Pour and Rhythm mini-games: a rounded
/// track with the target zone in mint, a fill (or a bouncing marker) and a needle.
/// Drawn directly, so it costs one cheap render per frame and no layout.
/// </summary>
public sealed class MeterBar : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MeterBar, double>(nameof(Value));

    public static readonly StyledProperty<double> ZoneMinProperty =
        AvaloniaProperty.Register<MeterBar, double>(nameof(ZoneMin), 0.4);

    public static readonly StyledProperty<double> ZoneMaxProperty =
        AvaloniaProperty.Register<MeterBar, double>(nameof(ZoneMax), 0.6);

    /// <summary>Marker mode draws a bouncing puck instead of a fill (Rhythm).</summary>
    public static readonly StyledProperty<bool> IsMarkerProperty =
        AvaloniaProperty.Register<MeterBar, bool>(nameof(IsMarker));

    private static readonly IBrush Track = new SolidColorBrush(Color.Parse("#EDEBF7"));
    private static readonly IBrush Zone = new SolidColorBrush(Color.Parse("#5FD3A6"));
    private static readonly IPen Outline = new Pen(new SolidColorBrush(Color.Parse("#2B2A6B")), 3);
    private static readonly IBrush Ink = new SolidColorBrush(Color.Parse("#2B2A6B"));
    private static readonly IBrush Puck = new SolidColorBrush(Color.Parse("#FFD45C"));
    private static readonly IBrush Fill = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops =
        [
            new GradientStop(Color.Parse("#7FD1FF"), 0),
            new GradientStop(Color.Parse("#FFD45C"), 0.55),
            new GradientStop(Color.Parse("#FF5A4E"), 1),
        ],
    };

    static MeterBar()
    {
        AffectsRender<MeterBar>(ValueProperty, ZoneMinProperty, ZoneMaxProperty, IsMarkerProperty);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double ZoneMin
    {
        get => GetValue(ZoneMinProperty);
        set => SetValue(ZoneMinProperty, value);
    }

    public double ZoneMax
    {
        get => GetValue(ZoneMaxProperty);
        set => SetValue(ZoneMaxProperty, value);
    }

    public bool IsMarker
    {
        get => GetValue(IsMarkerProperty);
        set => SetValue(IsMarkerProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        var r = h / 2;
        var track = new Rect(0, 0, w, h);
        context.DrawRectangle(Track, null, track, r, r);

        var zx = Math.Clamp(ZoneMin, 0, 1) * w;
        var zw = Math.Max(4, (Math.Clamp(ZoneMax, 0, 1) - Math.Clamp(ZoneMin, 0, 1)) * w);
        context.DrawRectangle(Zone, null, new Rect(zx, 3, zw, h - 6), 6, 6);

        var v = Math.Clamp(Value, 0, 1);
        if (IsMarker)
        {
            var cx = r + (v * (w - (2 * r)));
            context.DrawEllipse(Puck, Outline, new Point(cx, h / 2), r - 3, r - 3);
        }
        else
        {
            if (v > 0.001)
            {
                using (context.PushClip(new RoundedRect(track, r)))
                {
                    context.DrawRectangle(Fill, null, new Rect(0, 0, v * w, h));
                }
            }

            var nx = v * w;
            context.DrawRectangle(Ink, null, new Rect(nx - 2.5, -6, 5, h + 12), 2.5, 2.5);
        }

        context.DrawRectangle(null, Outline, track.Deflate(1.5), r, r);
    }
}
