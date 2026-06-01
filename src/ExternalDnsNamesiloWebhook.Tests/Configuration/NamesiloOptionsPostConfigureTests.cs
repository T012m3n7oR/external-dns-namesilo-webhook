using AutoFixture;
using ExternalDnsNamesiloWebhook.Core.Configuration;
using ExternalDnsNamesiloWebhook.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Configuration;

public class NamesiloOptionsPostConfigureTests
{
    private readonly Fixture _fixture;

    public NamesiloOptionsPostConfigureTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void PostConfigure_LoadsApiKeyFromConfigurationWhenOptionsEmpty()
    {
        string apiKey = _fixture.Create<string>();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?> { ["namesilo-api-key"] = apiKey });
        NamesiloOptions options = new();
        NamesiloOptionsPostConfigure sut = CreateSut(configuration);

        sut.PostConfigure(null, options);

        Assert.Equal(apiKey, options.ApiKey);
    }

    [Fact]
    public void PostConfigure_DoesNotOverwriteExistingApiKey()
    {
        string configuredKey = _fixture.Create<string>();
        string secretKey = _fixture.Create<string>();
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?> { ["namesilo-api-key"] = secretKey });
        NamesiloOptions options = new() { ApiKey = configuredKey };
        NamesiloOptionsPostConfigure sut = CreateSut(configuration);

        sut.PostConfigure(null, options);

        Assert.Equal(configuredKey, options.ApiKey);
    }

    [Fact]
    public void PostConfigure_WhenApiKeyMissing_ThrowsAndLogsError()
    {
        IConfiguration configuration = CreateConfiguration([]);
        Mock<ILogger<NamesiloOptionsPostConfigure>> loggerMock = new();
        NamesiloOptionsPostConfigure sut = new(configuration, loggerMock.Object);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => sut.PostConfigure(null, new NamesiloOptions()));

        Assert.Equal("NameSilo API key is not configured.", exception.Message);
        loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == "NameSilo API key is not configured."),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static NamesiloOptionsPostConfigure CreateSut(IConfiguration configuration)
    {
        return new NamesiloOptionsPostConfigure(configuration, NullLogger<NamesiloOptionsPostConfigure>.Instance);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
