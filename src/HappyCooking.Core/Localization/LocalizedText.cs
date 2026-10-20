using System.Text.Json.Serialization;

namespace HappyCooking.Core.Localization;

/// <summary>The two languages the game ships in.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Language>))]
public enum Language
{
    English,
    Indonesian,
}

/// <summary>
/// A piece of content text in both shipped languages. Content JSON stores it
/// as <c>{ "en": "...", "id": "..." }</c>.
/// </summary>
public readonly record struct LocalizedText(
    [property: JsonPropertyName("en")] string En,
    [property: JsonPropertyName("id")] string Id)
{
    public string Get(Language language) => language == Language.Indonesian && !string.IsNullOrEmpty(Id) ? Id : En;

    public override string ToString() => En;
}
