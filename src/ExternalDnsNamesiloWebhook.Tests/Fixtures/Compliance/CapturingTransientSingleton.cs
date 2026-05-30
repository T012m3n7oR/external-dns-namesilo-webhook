namespace ExternalDnsNamesiloWebhook.Tests.Fixtures.Compliance;

internal sealed class CapturingTransientSingleton
{
    private readonly ITransientDependency _transientDependency;

    public CapturingTransientSingleton(ITransientDependency transientDependency)
    {
        _transientDependency = transientDependency;
    }
}
