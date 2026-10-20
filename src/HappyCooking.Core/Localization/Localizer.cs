using System.Collections.Frozen;
using HappyCooking.Core.Content;

namespace HappyCooking.Core.Localization;

/// <summary>
/// UI string lookup for the current language. Strings live in
/// <c>Content/Data/strings.json</c>; content text (recipes, regions, ...) carries
/// its own <see cref="LocalizedText"/>.
/// </summary>
public sealed class Localizer
{
    private readonly FrozenDictionary<string, LocalizedText> _strings;
    private Language _language;

    public Localizer(IReadOnlyDictionary<string, LocalizedText> strings, Language language = Language.English)
    {
        _strings = strings.ToFrozenDictionary(StringComparer.Ordinal);
        _language = language;
    }

    public static Localizer FromEmbeddedContent(Language language = Language.English) =>
        new(ContentLoader.LoadStrings(), language);

    public Language Language
    {
        get => _language;
        set
        {
            if (_language == value)
            {
                return;
            }

            _language = value;
            LanguageChanged?.Invoke(value);
        }
    }

    public event Action<Language>? LanguageChanged;

    /// <summary>The string for <paramref name="key"/>, or the key itself so a missing entry is visible, not blank.</summary>
    public string this[string key] => _strings.TryGetValue(key, out var text) ? text.Get(_language) : key;

    public string Format(string key, params object?[] args) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, this[key], args);

    public string Get(LocalizedText text) => text.Get(_language);

    public bool Contains(string key) => _strings.ContainsKey(key);

    public IEnumerable<string> Keys => _strings.Keys;
}
