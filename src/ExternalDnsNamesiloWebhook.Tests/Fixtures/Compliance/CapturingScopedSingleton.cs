namespace ExternalDnsNamesiloWebhook.Tests.Fixtures.Compliance;

internal sealed class CapturingScopedSingleton
{
    private readonly IScopedDependency _scopedDependency;

    public CapturingScopedSingleton(IScopedDependency scopedDependency)
    {
        _scopedDependency = scopedDependency;
    }
}
