using ExternalDnsNamesiloWebhook.Core.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Compliance;

/// <summary>
/// Validates that the same DI scoping validation that runs when Core services are registered
/// (<see cref="DependencyInjectionScopingValidator"/>) passes for the production Core service collection.
/// </summary>
public class DependencyInjectionScopingTests
{
    [Fact]
    public void Validate_WhenCoreServiceCollectionConfigured_DoesNotThrow()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();
        services.AddNamesiloWebhookCore(configuration);
    }
}
