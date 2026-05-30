using AutoFixture;
using ExternalDnsNamesiloWebhook.Core.Configuration;
using ExternalDnsNamesiloWebhook.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Configuration;

public class KeyPerFileSecretsConfigurationExtensionsTests
{
    private readonly Fixture _fixture;

    public KeyPerFileSecretsConfigurationExtensionsTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void AddKeyPerFileSecrets_LoadsSecretFile()
    {
        string secretValue = _fixture.Create<string>();
        string secretsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(secretsDirectory);

        try
        {
            File.WriteAllText(Path.Combine(secretsDirectory, "namesilo-api-key"), secretValue);
            Environment.SetEnvironmentVariable(KeyPerFileSecretsConfigurationExtensions.SecretsPathEnvironmentVariableName, secretsDirectory);

            ConfigurationBuilder builder = CreateConfigurationBuilder();
            IConfigurationRoot configuration = builder.Build();

            Assert.Equal(secretValue, configuration["namesilo-api-key"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(KeyPerFileSecretsConfigurationExtensions.SecretsPathEnvironmentVariableName, null);
            Directory.Delete(secretsDirectory, recursive: true);
        }
    }

    private static ConfigurationBuilder CreateConfigurationBuilder()
    {
        ConfigurationBuilder builder = new();
        builder.AddKeyPerFileSecrets();
        return builder;
    }
}
