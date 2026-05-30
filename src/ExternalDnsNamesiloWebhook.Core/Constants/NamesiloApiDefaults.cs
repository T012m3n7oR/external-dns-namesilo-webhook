namespace ExternalDnsNamesiloWebhook.Core.Constants;

public static class NamesiloApiDefaults
{
    public const string BaseUrl = "https://www.namesilo.com/api/";

    public const string BaseUrlWithoutTrailingSlash = "https://www.namesilo.com/api";

    public const string ApiVersion = "1";

    public const string JsonFormat = "json";

    public const string SuccessDetail = "success";

    public const int ErrorBodyMaxLength = 500;
}
