using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Namesilo;

namespace ExternalDnsNamesiloWebhook.Core.Configuration;

public sealed class NamesiloOptions
{
    public const string SectionName = "Namesilo";

    public string ApiKey { get; set; } = string.Empty;

    public string[] DomainFilter { get; set; } = [];

    public int DefaultTtl { get; set; } = NamesiloRecordTtl.DefaultSeconds;

    public bool DryRun { get; set; }

    public string ApiBaseUrl { get; set; } = NamesiloApiDefaults.BaseUrlWithoutTrailingSlash;
}
