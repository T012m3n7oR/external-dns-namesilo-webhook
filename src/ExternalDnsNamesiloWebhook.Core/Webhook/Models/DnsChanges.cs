using System.Collections.Generic;

namespace ExternalDnsNamesiloWebhook.Core.Webhook.Models;

public sealed class DnsChanges
{
    public List<DnsEndpoint> Create { get; set; } = [];

    public List<DnsEndpoint> UpdateOld { get; set; } = [];

    public List<DnsEndpoint> UpdateNew { get; set; } = [];

    public List<DnsEndpoint> Delete { get; set; } = [];
}
