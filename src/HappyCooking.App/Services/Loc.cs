using System.ComponentModel;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using HappyCooking.Core.Localization;

namespace HappyCooking.App.Services;

/// <summary>
/// Bindable front for the core <see cref="Localizer"/>: XAML binds to its indexer,
/// and switching language refreshes every bound string at once.
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    private Localizer _localizer = Localizer.FromEmbeddedContent();

    public static Loc Instance { get; } = new();

    public Localizer Localizer => _localizer;

    public Language Language
    {
        get => _localizer.Language;
        set
        {
            if (_localizer.Language == value)
            {
                return;
            }

            _localizer.Language = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
            LanguageChanged?.Invoke(value);
        }
    }

    public string this[string key] => _localizer[key];

    public string Format(string key, params object?[] args) => _localizer.Format(key, args);

    public string Get(LocalizedText text) => _localizer.Get(text);

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action<Language>? LanguageChanged;
}

/// <summary>
/// <c>{svc:Tr key}</c> — a localised string that updates when the language changes.
/// </summary>
public sealed class TrExtension : MarkupExtension
{
    public TrExtension()
    {
    }

    public TrExtension(string key) => Key = key;

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new ReflectionBinding($"[{Key}]") { Source = Loc.Instance, Mode = BindingMode.OneWay };
}
