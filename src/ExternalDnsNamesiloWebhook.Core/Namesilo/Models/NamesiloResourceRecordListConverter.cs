using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExternalDnsNamesiloWebhook.Core.Namesilo;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

public sealed class NamesiloResourceRecordListConverter : JsonConverter<IReadOnlyList<NamesiloDnsRecord>?>
{
    public override IReadOnlyList<NamesiloDnsRecord>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            return JsonSerializer.Deserialize<List<NamesiloDnsRecord>>(ref reader, options);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            NamesiloDnsRecord? record = JsonSerializer.Deserialize<NamesiloDnsRecord>(ref reader, options);
            return record is null ? null : [record];
        }

        throw new JsonException("Expected NameSilo resource_record to be an object or array.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<NamesiloDnsRecord>? value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (NamesiloDnsRecord record in value)
        {
            JsonSerializer.Serialize(writer, record, options);
        }

        writer.WriteEndArray();
    }
}
