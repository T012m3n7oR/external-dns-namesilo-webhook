namespace ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

public sealed class NamesiloApiResponse
{
    public NamesiloApiReply? Reply { get; set; }

    public int? Code { get; set; }
}
