using ExternalDnsNamesiloWebhook.Core.DependencyInjection;
using ExternalDnsNamesiloWebhook.Tests.Fixtures.Compliance;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Compliance;

public class DependencyInjectionScopingValidatorTests
{
    [Fact]
    public void Validate_SingletonDependingOnScoped_Throws()
    {
        ServiceCollection services = new();
        services.AddScoped<IScopedDependency, ScopedDependency>();
        services.AddSingleton<CapturingScopedSingleton>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => DependencyInjectionScopingValidator.Validate(services));

        Assert.Contains("scoped service IScopedDependency", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_SingletonDependingOnTransient_Throws()
    {
        ServiceCollection services = new();
        services.AddTransient<ITransientDependency, TransientDependency>();
        services.AddSingleton<CapturingTransientSingleton>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => DependencyInjectionScopingValidator.Validate(services));

        Assert.Contains("transient service ITransientDependency", exception.Message, StringComparison.Ordinal);
    }
}
