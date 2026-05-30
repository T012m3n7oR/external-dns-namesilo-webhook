using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace ExternalDnsNamesiloWebhook.Core.Configuration;

public static class KeyPerFileSecretsConfigurationExtensions
{
    public const string SecretsPathEnvironmentVariableName = "SECRETS_PATH";
    public const string DefaultSecretsPath = "/run/secrets";

    public static IConfigurationBuilder AddKeyPerFileSecrets(this IConfigurationBuilder configurationBuilder)
    {
        string? secretsPath = Environment.GetEnvironmentVariable(SecretsPathEnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(secretsPath))
        {
            secretsPath = DefaultSecretsPath;
        }

        string absolutePath = Path.GetFullPath(secretsPath);
        configurationBuilder.AddKeyPerFile(absolutePath, optional: true);
        return configurationBuilder;
    }
}
