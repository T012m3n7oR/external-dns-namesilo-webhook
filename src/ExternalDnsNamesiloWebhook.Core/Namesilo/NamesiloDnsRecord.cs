using System.Text.Json.Serialization;
using ExternalDnsNamesiloWebhook.Core.Enums;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo;

public sealed class NamesiloDnsRecord
{
    public string RecordId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public DnsRecordType RecordType { get; set; }

    public string Host { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public int Ttl { get; set; }

    public int Distance { get; set; }
}
