using ExternalDnsNamesiloWebhook.Core.Namesilo;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

internal sealed class NamesiloSingleResourceRecordReply
{
    public int Code { get; set; }

    public string? Detail { get; set; }

    public NamesiloDnsRecord ResourceRecord { get; set; } = new();
}
