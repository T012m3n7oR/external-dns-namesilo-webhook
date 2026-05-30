using System.Collections.Generic;
using System.Text.Json.Serialization;
using ExternalDnsNamesiloWebhook.Core.Namesilo;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

public sealed class NamesiloApiReply
{
    public int Code { get; set; }

    public string? Detail { get; set; }

    [JsonPropertyName("resource_record")]
    public IReadOnlyList<NamesiloDnsRecord>? ResourceRecords { get; set; }

    public string? RecordId { get; set; }
}
