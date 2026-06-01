using AutoFixture;
using ExternalDnsNamesiloWebhook.Core.Contracts.NameSilo;
using ExternalDnsNamesiloWebhook.Core.Webhook.Models;
using ExternalDnsNamesiloWebhook.Tests.Constants;
using ExternalDnsNamesiloWebhook.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExternalDnsNamesiloWebhook.Tests.Fixtures;

public sealed class WebhookApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Fixture _fixture = new();
    private readonly Mock<INamesiloDnsService> _dnsServiceMock = new();

    public WebhookApplicationFactory()
    {
        DomainFilter = TestData.CreateDomain(_fixture);
        SampleEndpoint = TestData.CreateARecord(_fixture, DomainFilter, recordTtl: _fixture.Create<int>() + 1);

        _dnsServiceMock.Setup(service => service.GetDomainFilter())
            .Returns(new DomainFilterResponse { Filters = [DomainFilter] });
        _dnsServiceMock.Setup(service => service.GetRecordsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([SampleEndpoint]);
        _dnsServiceMock.Setup(service => service.AdjustEndpoints(It.IsAny<IReadOnlyList<DnsEndpoint>>()))
            .Returns((IReadOnlyList<DnsEndpoint> endpoints) => endpoints);
    }

    public Mock<INamesiloDnsService> DnsServiceMock => _dnsServiceMock;

    public string DomainFilter { get; }

    public DnsEndpoint SampleEndpoint { get; }

    public void ReplaceDnsServiceWithMock(IServiceCollection services)
    {
        ServiceDescriptor? descriptor = null;
        foreach (ServiceDescriptor service in services)
        {
            if (service.ServiceType == typeof(INamesiloDnsService))
            {
                descriptor = service;
                break;
            }
        }

        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        services.AddScoped(_ => _dnsServiceMock.Object);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestConstants.TestingEnvironment);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Namesilo:ApiKey"] = "integration-test-api-key",
            });
        });
        builder.ConfigureServices(ReplaceDnsServiceWithMock);
    }
}
