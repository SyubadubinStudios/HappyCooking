using System.Collections.Concurrent;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace HappyCooking.App.Services;

/// <summary>
/// Loads images bundled under <c>Assets/Images</c> once and shares them. Bitmaps are
/// decoded at their display size where it is known, which keeps memory low on phones.
/// </summary>
public static class Images
{
    private static readonly ConcurrentDictionary<(string, int), Bitmap?> s_cache = new();

    /// <param name="path">Relative to Assets/Images, e.g. <c>Icons/coin.png</c>.</param>
    /// <param name="decodeWidth">Decode to this width (0 = full size).</param>
    public static Bitmap? Get(string path, int decodeWidth = 0) =>
        s_cache.GetOrAdd((path, decodeWidth), static key =>
        {
            var uri = new Uri($"avares://HappyCooking.App/Assets/Images/{key.Item1}");
            if (!AssetLoader.Exists(uri))
            {
                return null;
            }

            using var stream = AssetLoader.Open(uri);
            return key.Item2 > 0
                ? Bitmap.DecodeToWidth(stream, key.Item2, BitmapInterpolationMode.HighQuality)
                : new Bitmap(stream);
        });

    public static Bitmap? Icon(string name) => Get($"Icons/{name}.png", 128);

    public static Bitmap? Region(string id) => Get($"Regions/{id}.jpg", 560);

    public static Bitmap? Portrait(string characterId) => Get($"Portraits/{characterId}.jpg", 192);

    public static Bitmap? Pet(string petId) => Get($"Pets/{petId}.png", 256);
}

/// <summary>XAML converter: an icon name (e.g. "coin") to its bitmap.</summary>
public sealed class IconConverter : IValueConverter
{
    public static IconConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string name ? Images.Icon(name) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
