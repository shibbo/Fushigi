namespace Fushigi.util;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class ActorDataDB
{
    static readonly Dictionary<string, string> translations = new();
    static readonly Dictionary<string, string> reverse = new();

    public static string[] GetTranslations() => translations.Values.ToArray();

    public static void Load()
    {
        string json = File.ReadAllText(Path.Combine("res", "ActorData.json"));
        var mappings = JsonSerializer.Deserialize<Dictionary<string, ActorData>>(json)!;

        foreach (var (key, value) in mappings)
        {
            if (value.NameOverride != null)
            {
                translations.Add(key, value.NameOverride);
                reverse.Add(value.NameOverride, key);
            }
            else
            {
                translations.Add(key, key);
                reverse.Add(key, key);
            }
        }
    }

    public static string Translate(string key)
    {
        if (translations.TryGetValue(key, out string? value))
        {
            if (value != null)
            {
                return value;
            }
            else
            {
                return key;
            }
        }

        return key;
    }

    public static string ReverseTranslate(string value)
    {
        if (reverse.TryGetValue(value, out string? key))
        {
            if (key != null)
            {
                return key;
            }
            else
            {
                return value;
            }
        }

        return value;
    }

    class ActorData
    {
        [JsonPropertyName("nameOverride")]
        public string? NameOverride { get; set; }
    }
}
