using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExternalDnsNamesiloWebhook.Core.Configuration;

public sealed class NamesiloOptionsPostConfigure : IPostConfigureOptions<NamesiloOptions>
{
    private const string ApiKeyMissingMessage = "NameSilo API key is not configured.";

    private readonly IConfiguration _configuration;
    private readonly ILogger<NamesiloOptionsPostConfigure> _logger;

    public NamesiloOptionsPostConfigure(
        IConfiguration configuration,
        ILogger<NamesiloOptionsPostConfigure> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void PostConfigure(string? name, NamesiloOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            options.ApiKey = _configuration["namesilo-api-key"] ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _logger.LogError(ApiKeyMissingMessage);
            throw new InvalidOperationException(ApiKeyMissingMessage);
        }
    }
}
