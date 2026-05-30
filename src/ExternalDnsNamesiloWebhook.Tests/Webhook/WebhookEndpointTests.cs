using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Namesilo;
using ExternalDnsNamesiloWebhook.Core.Webhook;
using ExternalDnsNamesiloWebhook.Core.Webhook.Models;
using ExternalDnsNamesiloWebhook.Tests.Constants;
using ExternalDnsNamesiloWebhook.Tests.Fixtures;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Webhook;

public sealed class WebhookEndpointTests : IClassFixture<WebhookApplicationFactory>
{
    private readonly WebhookApplicationFactory _factory;

    public WebhookEndpointTests(WebhookApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Healthz_ReturnsOk()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(WebhookPaths.Healthz, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HealthConstants.OkBody, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Negotiate_ReturnsDomainFilterWithWebhookContentType()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(WebhookPaths.Negotiate, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertWebhookContentType(response);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains(_factory.DomainFilter, body);
    }

    [Fact]
    public async Task GetRecords_ReturnsDnsServiceRecordsWithWebhookContentType()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(WebhookPaths.Records, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertWebhookContentType(response);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains(_factory.SampleEndpoint.DnsName, body);
    }

    [Fact]
    public async Task ApplyChanges_ReturnsNoContent()
    {
        _factory.DnsServiceMock
            .Setup(service => service.ApplyChangesAsync(It.IsAny<DnsChanges>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        HttpClient client = _factory.CreateClient();
        HttpRequestMessage request = new(HttpMethod.Post, WebhookPaths.Records)
        {
            Content = new StringContent(TestConstants.EmptyDnsChangesJson, Encoding.UTF8, HttpMediaTypes.ApplicationJson),
        };

        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ApplyChanges_ReturnsBadRequestForInvalidJson()
    {
        HttpClient client = _factory.CreateClient();
        HttpRequestMessage request = new(HttpMethod.Post, WebhookPaths.Records)
        {
            Content = new StringContent("{not-json", Encoding.UTF8, HttpMediaTypes.ApplicationJson),
        };

        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApplyChanges_ReturnsBadRequestForEmptyBody()
    {
        HttpClient client = _factory.CreateClient();
        HttpRequestMessage request = new(HttpMethod.Post, WebhookPaths.Records)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, HttpMediaTypes.ApplicationJson),
        };

        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApplyChanges_ReturnsInternalServerErrorWhenDnsServiceFails()
    {
        _factory.DnsServiceMock
            .Setup(service => service.ApplyChangesAsync(It.IsAny<DnsChanges>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NamesiloServiceException("dns service failed"));

        HttpClient client = _factory.CreateClient();
        HttpRequestMessage request = new(HttpMethod.Post, WebhookPaths.Records)
        {
            Content = new StringContent(TestConstants.EmptyDnsChangesJson, Encoding.UTF8, HttpMediaTypes.ApplicationJson),
        };

        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task AdjustEndpoints_ReturnsAdjustedTtl()
    {
        DnsEndpoint adjusted = _factory.SampleEndpoint;

        _factory.DnsServiceMock
            .Setup(service => service.AdjustEndpoints(It.IsAny<IReadOnlyList<DnsEndpoint>>()))
            .Returns([adjusted]);

        string requestBody = JsonSerializer.Serialize(
            new[]
            {
                new
                {
                    dnsName = adjusted.DnsName,
                    recordType = adjusted.RecordType,
                    targets = adjusted.Targets,
                    recordTTL = 0,
                },
            });

        HttpClient client = _factory.CreateClient();
        HttpRequestMessage request = new(HttpMethod.Post, WebhookPaths.AdjustEndpoints)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, HttpMediaTypes.ApplicationJson),
        };

        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertWebhookContentType(response);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains($"\"recordTTL\":{adjusted.RecordTtl}", body);
    }

    [Fact]
    public async Task AdjustEndpoints_ReturnsBadRequestForInvalidJson()
    {
        HttpClient client = _factory.CreateClient();
        HttpRequestMessage request = new(HttpMethod.Post, WebhookPaths.AdjustEndpoints)
        {
            Content = new StringContent("{not-json", Encoding.UTF8, HttpMediaTypes.ApplicationJson),
        };

        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static void AssertWebhookContentType(HttpResponseMessage response)
    {
        System.Net.Http.Headers.MediaTypeHeaderValue? contentType = response.Content.Headers.ContentType;
        Assert.NotNull(contentType);
        Assert.Equal(WebhookMediaTypes.WebhookJson, contentType.MediaType);
        Assert.Equal(WebhookMediaTypes.Version1Parameter, contentType.Parameters.Single(parameter => parameter.Name == "version").Value);
    }
}
