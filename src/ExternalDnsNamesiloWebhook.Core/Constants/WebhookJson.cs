using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExternalDnsNamesiloWebhook.Core.Constants;

public static class WebhookJson
{
    public static JsonSerializerOptions SerializerOptions { get; } = CreateSerializerOptions();

    public static void ApplyTo(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        if (!HasStringEnumConverter(options))
        {
            options.Converters.Add(new JsonStringEnumConverter());
        }
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static bool HasStringEnumConverter(JsonSerializerOptions options)
    {
        foreach (JsonConverter converter in options.Converters)
        {
            if (converter is JsonStringEnumConverter)
            {
                return true;
            }
        }

        return false;
    }
}
