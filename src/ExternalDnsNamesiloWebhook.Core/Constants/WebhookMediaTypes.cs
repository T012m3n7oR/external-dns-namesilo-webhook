namespace ExternalDnsNamesiloWebhook.Core.Constants;

public static class WebhookMediaTypes
{
    public const string WebhookJson = "application/external.dns.webhook+json";

    public const string Version1Parameter = "1";

    public const string Version1 = WebhookJson + ";version=" + Version1Parameter;
}
