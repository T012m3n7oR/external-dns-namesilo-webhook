using System.Collections.Generic;

namespace ExternalDnsNamesiloWebhook.Core.Webhook.Models;

public sealed class DnsChanges
{
    public IReadOnlyList<DnsEndpoint> Create { get; set; } = [];

    public IReadOnlyList<DnsEndpoint> UpdateOld { get; set; } = [];

    public IReadOnlyList<DnsEndpoint> UpdateNew { get; set; } = [];

    public IReadOnlyList<DnsEndpoint> Delete { get; set; } = [];
}
