using AutoFixture;
using ExternalDnsNamesiloWebhook.Core.Configuration;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Enums;
using ExternalDnsNamesiloWebhook.Core.Namesilo;
using ExternalDnsNamesiloWebhook.Core.Namesilo.Models;
using ExternalDnsNamesiloWebhook.Tests.Fixtures;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Namesilo;

public class NamesiloApiClientTests
{
    private readonly Fixture _fixture;

    public NamesiloApiClientTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public async Task ListRecordsAsync_ParsesReplyWrapper()
    {
        string domain = TestData.CreateDomain(_fixture);
        NamesiloDnsRecord expected = TestData.CreateNamesiloRecord(_fixture, domain, DnsRecordType.A);
        MockHttpMessageHandler mockHttp = new();
        mockHttp.Expect(HttpMethod.Get, $"*{NamesiloApiOperations.ListRecords}*")
            .Respond(HttpStatusCode.OK, HttpMediaTypes.ApplicationJson, TestData.BuildListRecordsJson(expected));
        NamesiloApiClient sut = CreateSut(mockHttp);

        IReadOnlyList<NamesiloDnsRecord> records = await sut.ListRecordsAsync(
            new ListRecordsRequest { Domain = domain },
            CancellationToken.None);

        Assert.Single(records);
        Assert.Equal(expected.RecordId, records[0].RecordId);
        Assert.Equal(expected.RecordType, records[0].RecordType);
        Assert.Equal(expected.Value, records[0].Value);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ListRecordsAsync_ParsesSingleResourceRecordObject()
    {
        string domain = TestData.CreateDomain(_fixture);
        NamesiloDnsRecord expected = TestData.CreateNamesiloRecord(_fixture, domain, DnsRecordType.TXT);
        MockHttpMessageHandler mockHttp = new();
        mockHttp.Expect(HttpMethod.Get, $"*{NamesiloApiOperations.ListRecords}*")
            .Respond(HttpStatusCode.OK, HttpMediaTypes.ApplicationJson, TestData.BuildSingleObjectListRecordsJson(expected));
        NamesiloApiClient sut = CreateSut(mockHttp);

        IReadOnlyList<NamesiloDnsRecord> records = await sut.ListRecordsAsync(
            new ListRecordsRequest { Domain = domain },
            CancellationToken.None);

        Assert.Single(records);
        Assert.Equal(expected.RecordId, records[0].RecordId);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task AddRecordAsync_ThrowsWhenApiReturnsErrorCode()
    {
        string domain = TestData.CreateDomain(_fixture);
        int errorCode = _fixture.Create<int>() + 301;
        MockHttpMessageHandler mockHttp = new();
        mockHttp.Expect(HttpMethod.Get, $"*{NamesiloApiOperations.AddRecord}*")
            .Respond(HttpStatusCode.OK, HttpMediaTypes.ApplicationJson, TestData.BuildSuccessReplyJson(errorCode, "error"));
        NamesiloApiClient sut = CreateSut(mockHttp);

        await Assert.ThrowsAsync<NamesiloServiceException>(() =>
            sut.AddRecordAsync(
                new AddRecordRequest
                {
                    Domain = domain,
                    RecordType = DnsRecordType.A,
                    RecordHost = NamesiloDns.ApexRecordHost,
                    RecordValue = _fixture.Create<string>(),
                    Ttl = _fixture.Create<int>() + 1,
                },
                CancellationToken.None));

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ListRecordsAsync_ReturnsEmptyWhenNoRecordsInReply()
    {
        string domain = TestData.CreateDomain(_fixture);
        MockHttpMessageHandler mockHttp = new();
        mockHttp.Expect(HttpMethod.Get, $"*{NamesiloApiOperations.ListRecords}*")
            .Respond(HttpStatusCode.OK, HttpMediaTypes.ApplicationJson, TestData.BuildSuccessReplyJson(NamesiloDns.SuccessReplyCode));
        NamesiloApiClient sut = CreateSut(mockHttp);

        IReadOnlyList<NamesiloDnsRecord> records = await sut.ListRecordsAsync(
            new ListRecordsRequest { Domain = domain },
            CancellationToken.None);

        Assert.Empty(records);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ListRecordsAsync_ParsesCaaRecordType()
    {
        string domain = TestData.CreateDomain(_fixture);
        NamesiloDnsRecord expected = TestData.CreateNamesiloRecord(_fixture, domain, DnsRecordType.CAA);
        MockHttpMessageHandler mockHttp = new();
        mockHttp.Expect(HttpMethod.Get, $"*{NamesiloApiOperations.ListRecords}*")
            .Respond(HttpStatusCode.OK, HttpMediaTypes.ApplicationJson, TestData.BuildListRecordsJson(expected));
        NamesiloApiClient sut = CreateSut(mockHttp);

        IReadOnlyList<NamesiloDnsRecord> records = await sut.ListRecordsAsync(
            new ListRecordsRequest { Domain = domain },
            CancellationToken.None);

        Assert.Single(records);
        Assert.Equal(DnsRecordType.CAA, records[0].RecordType);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ListRecordsAsync_ThrowsWhenHttpRequestFails()
    {
        string domain = TestData.CreateDomain(_fixture);
        MockHttpMessageHandler mockHttp = new();
        mockHttp.Expect(HttpMethod.Get, $"*{NamesiloApiOperations.ListRecords}*")
            .Respond(HttpStatusCode.BadGateway, HttpMediaTypes.TextPlain, "error");
        NamesiloApiClient sut = CreateSut(mockHttp);

        await Assert.ThrowsAsync<NamesiloServiceException>(() =>
            sut.ListRecordsAsync(new ListRecordsRequest { Domain = domain }, CancellationToken.None));

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task AddRecordAsync_CallsDnsAddRecord()
    {
        string domain = TestData.CreateDomain(_fixture);
        string target = _fixture.Create<string>();
        int ttl = _fixture.Create<int>() + 1;
        string recordId = _fixture.Create<Guid>().ToString("N");
        MockHttpMessageHandler mockHttp = new();
        mockHttp.Expect(HttpMethod.Get, $"*{NamesiloApiOperations.AddRecord}*")
            .WithQueryString("rrtype", "A")
            .WithQueryString("rrhost", NamesiloDns.ApexRecordHost)
            .WithQueryString("rrvalue", target)
            .WithQueryString("rrttl", ttl.ToString())
            .Respond(
                HttpStatusCode.OK,
                HttpMediaTypes.ApplicationJson,
                TestData.BuildAddRecordSuccessJson(recordId));
        NamesiloApiClient sut = CreateSut(mockHttp);

        string actualRecordId = await sut.AddRecordAsync(
            new AddRecordRequest
            {
                Domain = domain,
                RecordType = DnsRecordType.A,
                RecordHost = NamesiloDns.ApexRecordHost,
                RecordValue = target,
                Ttl = ttl,
            },
            CancellationToken.None);

        Assert.Equal(recordId, actualRecordId);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task AddRecordAsync_DryRun_DoesNotCallHttp()
    {
        string domain = TestData.CreateDomain(_fixture);
        MockHttpMessageHandler mockHttp = new();
        NamesiloApiClient sut = CreateSut(mockHttp, dryRun: true);

        string recordId = await sut.AddRecordAsync(
            new AddRecordRequest
            {
                Domain = domain,
                RecordType = DnsRecordType.A,
                RecordHost = NamesiloDns.ApexRecordHost,
                RecordValue = _fixture.Create<string>(),
                Ttl = _fixture.Create<int>() + 1,
            },
            CancellationToken.None);

        Assert.Equal(NamesiloDns.DryRunRecordId, recordId);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UpdateRecordAsync_CallsDnsUpdateRecord()
    {
        string domain = TestData.CreateDomain(_fixture);
        string recordId = _fixture.Create<Guid>().ToString("N");
        string target = _fixture.Create<string>();
        int ttl = _fixture.Create<int>() + 1;
        MockHttpMessageHandler mockHttp = new();
        mockHttp.Expect(HttpMethod.Get, $"*{NamesiloApiOperations.UpdateRecord}*")
            .WithQueryString("rrid", recordId)
            .Respond(HttpStatusCode.OK, HttpMediaTypes.ApplicationJson, TestData.BuildSuccessReplyJson(NamesiloDns.SuccessReplyCode));
        NamesiloApiClient sut = CreateSut(mockHttp);

        await sut.UpdateRecordAsync(
            new UpdateRecordRequest
            {
                Domain = domain,
                RecordId = recordId,
                RecordHost = NamesiloDns.ApexRecordHost,
                RecordValue = target,
                Ttl = ttl,
            },
            CancellationToken.None);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task UpdateRecordAsync_DryRun_DoesNotCallHttp()
    {
        string domain = TestData.CreateDomain(_fixture);
        MockHttpMessageHandler mockHttp = new();
        NamesiloApiClient sut = CreateSut(mockHttp, dryRun: true);

        await sut.UpdateRecordAsync(
            new UpdateRecordRequest
            {
                Domain = domain,
                RecordId = _fixture.Create<Guid>().ToString("N"),
                RecordHost = NamesiloDns.ApexRecordHost,
                RecordValue = _fixture.Create<string>(),
                Ttl = _fixture.Create<int>() + 1,
            },
            CancellationToken.None);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DeleteRecordAsync_CallsDnsDeleteRecord()
    {
        string domain = TestData.CreateDomain(_fixture);
        string recordId = _fixture.Create<Guid>().ToString("N");
        MockHttpMessageHandler mockHttp = new();
        mockHttp.Expect(HttpMethod.Get, $"*{NamesiloApiOperations.DeleteRecord}*")
            .WithQueryString("rrid", recordId)
            .Respond(HttpStatusCode.OK, HttpMediaTypes.ApplicationJson, TestData.BuildSuccessReplyJson(NamesiloDns.SuccessReplyCode));
        NamesiloApiClient sut = CreateSut(mockHttp);

        await sut.DeleteRecordAsync(
            new DeleteRecordRequest { Domain = domain, RecordId = recordId },
            CancellationToken.None);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task DeleteRecordAsync_DryRun_DoesNotCallHttp()
    {
        string domain = TestData.CreateDomain(_fixture);
        MockHttpMessageHandler mockHttp = new();
        NamesiloApiClient sut = CreateSut(mockHttp, dryRun: true);

        await sut.DeleteRecordAsync(
            new DeleteRecordRequest { Domain = domain, RecordId = _fixture.Create<Guid>().ToString("N") },
            CancellationToken.None);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task ListRecordsAsync_ThrowsWhenApiKeyMissing()
    {
        string domain = TestData.CreateDomain(_fixture);
        MockHttpMessageHandler mockHttp = new();
        NamesiloApiClient sut = CreateSut(mockHttp, apiKey: string.Empty);

        await Assert.ThrowsAsync<NamesiloServiceException>(() =>
            sut.ListRecordsAsync(new ListRecordsRequest { Domain = domain }, CancellationToken.None));

        mockHttp.VerifyNoOutstandingExpectation();
    }

    private NamesiloApiClient CreateSut(MockHttpMessageHandler mockHttp, bool dryRun = false, string? apiKey = null)
    {
        NamesiloOptionsBuilder builder = NamesiloOptionsBuilder.New();
        if (dryRun)
        {
            builder.WithDryRun();
        }

        if (apiKey == string.Empty)
        {
            builder.WithoutApiKey();
        }
        else if (apiKey != null)
        {
            builder.WithApiKey(apiKey);
        }

        IOptions<NamesiloOptions> options = Options.Create(builder.Build());

        Mock<IHttpClientFactory> httpClientFactoryMock = new();
        httpClientFactoryMock
            .Setup(factory => factory.CreateClient(HttpClientNames.NameSilo))
            .Returns(() =>
            {
                HttpClient client = mockHttp.ToHttpClient();
                client.BaseAddress = new Uri(NamesiloApiDefaults.BaseUrl);
                return client;
            });

        return new NamesiloApiClient(httpClientFactoryMock.Object, options, NullLogger<NamesiloApiClient>.Instance);
    }
}
