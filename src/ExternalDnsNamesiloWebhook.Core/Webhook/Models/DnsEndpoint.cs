using System.Collections.Generic;
using System.Text.Json.Serialization;
using ExternalDnsNamesiloWebhook.Core.Enums;

namespace ExternalDnsNamesiloWebhook.Core.Webhook.Models;

public sealed class DnsEndpoint
{
    public string DnsName { get; set; } = string.Empty;

    public List<string> Targets { get; set; } = [];

    public DnsRecordType RecordType { get; set; }

    public string? SetIdentifier { get; set; }

    [JsonPropertyName("recordTTL")]
    public long RecordTtl { get; set; }

    public Dictionary<string, string>? Labels { get; set; }

    public List<ProviderSpecificProperty>? ProviderSpecific { get; set; }
}
