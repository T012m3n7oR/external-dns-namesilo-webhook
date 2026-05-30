namespace ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

internal sealed class NamesiloSingleResourceRecordResponse
{
    public NamesiloSingleResourceRecordReply Reply { get; set; } = new();
}
