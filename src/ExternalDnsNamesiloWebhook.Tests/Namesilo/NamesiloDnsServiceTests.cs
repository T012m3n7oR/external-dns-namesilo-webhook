using AutoFixture;
using ExternalDnsNamesiloWebhook.Core.Configuration;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Contracts.NameSilo;
using ExternalDnsNamesiloWebhook.Core.Enums;
using ExternalDnsNamesiloWebhook.Core.Namesilo;
using ExternalDnsNamesiloWebhook.Core.Namesilo.Models;
using ExternalDnsNamesiloWebhook.Core.Webhook.Models;
using ExternalDnsNamesiloWebhook.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Namesilo;

public class NamesiloDnsServiceTests
{
    private readonly Fixture _fixture;
    private readonly Mock<INamesiloApiClient> _apiClientMock;

    public NamesiloDnsServiceTests()
    {
        _fixture = new Fixture();
        _apiClientMock = new Mock<INamesiloApiClient>();
    }

    [Fact]
    public void AdjustEndpoints_AppliesDefaultTtl()
    {
        int defaultTtl = TestData.CreateNameSiloRecordTtl(_fixture);
        string domain = TestData.CreateDomain(_fixture);
        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDefaultTtl(defaultTtl).Build());
        DnsEndpoint input = TestData.CreateARecord(_fixture, domain, recordTtl: 0);

        IReadOnlyList<DnsEndpoint> adjusted = sut.AdjustEndpoints([input]);

        Assert.Equal(defaultTtl, adjusted[0].RecordTtl);
    }

    [Fact]
    public void AdjustEndpoints_NormalizesTtlBelowNameSiloRange()
    {
        int invalidTtl = TestData.CreateBelowNameSiloRecordTtl(_fixture);
        string domain = TestData.CreateDomain(_fixture);
        NamesiloDnsService sut = CreateSut(
            NamesiloOptionsBuilder.New().WithDefaultTtl(TestData.CreateBelowNameSiloRecordTtl(_fixture)).Build());
        DnsEndpoint input = TestData.CreateARecord(_fixture, domain, recordTtl: invalidTtl);

        IReadOnlyList<DnsEndpoint> adjusted = sut.AdjustEndpoints([input]);

        Assert.Equal(NamesiloRecordTtl.Normalize(invalidTtl), adjusted[0].RecordTtl);
    }

    [Fact]
    public void AdjustEndpoints_NormalizesTtlAboveNameSiloRange()
    {
        int invalidTtl = TestData.CreateAboveNameSiloRecordTtl(_fixture);
        string domain = TestData.CreateDomain(_fixture);
        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().Build());
        DnsEndpoint input = TestData.CreateARecord(_fixture, domain, recordTtl: invalidTtl);

        IReadOnlyList<DnsEndpoint> adjusted = sut.AdjustEndpoints([input]);

        Assert.Equal(NamesiloRecordTtl.Normalize(invalidTtl), adjusted[0].RecordTtl);
    }

    [Fact]
    public async Task ApplyChangesAsync_CreatesRecord()
    {
        string domain = TestData.CreateDomain(_fixture);
        int recordTtl = TestData.CreateNameSiloRecordTtl(_fixture);
        DnsEndpoint endpoint = TestData.CreateARecord(_fixture, domain, recordTtl: recordTtl);
        string recordId = _fixture.Create<Guid>().ToString("N");

        _apiClientMock
            .Setup(client => client.AddRecordAsync(
                It.Is<AddRecordRequest>(request =>
                    request.Domain == domain
                    && request.RecordType == endpoint.RecordType
                    && request.RecordHost == NamesiloDns.ApexRecordHost
                    && request.RecordValue == endpoint.Targets[0]
                    && request.Ttl == recordTtl),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(recordId)
            .Verifiable();

        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build());

        await sut.ApplyChangesAsync(DnsChangesBuilder.CreateOnly(endpoint), CancellationToken.None);

        _apiClientMock.Verify();
    }

    [Fact]
    public void GetDomainFilter_ReturnsConfiguredDomains()
    {
        string domain = TestData.CreateDomain(_fixture);
        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build());

        DomainFilterResponse filter = sut.GetDomainFilter();

        Assert.Equal([domain], filter.Filters);
    }

    [Fact]
    public async Task GetRecordsAsync_SkipsUnsupportedNameSiloRecordTypes()
    {
        string domain = TestData.CreateDomain(_fixture);
        NamesiloDnsRecord supported = TestData.CreateNamesiloRecord(_fixture, domain, DnsRecordType.A);
        NamesiloDnsRecord unsupported = TestData.CreateNamesiloRecord(_fixture, domain, DnsRecordType.CAA);

        _apiClientMock
            .Setup(client => client.ListRecordsAsync(
                It.Is<ListRecordsRequest>(request => request.Domain == domain),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([supported, unsupported]);

        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build());

        IReadOnlyList<DnsEndpoint> records = await sut.GetRecordsAsync(CancellationToken.None);

        Assert.Single(records);
        Assert.Equal(DnsRecordType.A, records[0].RecordType);
    }

    [Fact]
    public async Task GetRecordsAsync_MapsMultiLabelRelativeHostToFqdn()
    {
        string domain = TestData.CreateDomain(_fixture);
        string nestedHost = TestData.CreateDomainLabel(_fixture) + "." + TestData.CreateDomainLabel(_fixture);
        string target = domain;
        NamesiloDnsRecord nestedCname = TestData.CreateNamesiloRecord(
            _fixture,
            domain,
            DnsRecordType.CNAME,
            host: nestedHost,
            value: target);

        _apiClientMock
            .Setup(client => client.ListRecordsAsync(
                It.Is<ListRecordsRequest>(request => request.Domain == domain),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([nestedCname]);

        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build());

        IReadOnlyList<DnsEndpoint> records = await sut.GetRecordsAsync(CancellationToken.None);

        Assert.Single(records);
        Assert.Equal(nestedHost + "." + domain, records[0].DnsName);
        Assert.Equal(DnsRecordType.CNAME, records[0].RecordType);
        Assert.Equal([target], records[0].Targets);
    }

    [Fact]
    public async Task ApplyChangesAsync_DeletesRecord()
    {
        string domain = TestData.CreateDomain(_fixture);
        string label = TestData.CreateDomainLabel(_fixture);
        string dnsName = label + "." + domain;
        string target = _fixture.Create<string>();
        NamesiloDnsRecord existing = TestData.CreateNamesiloRecord(
            _fixture,
            domain,
            DnsRecordType.TXT,
            host: dnsName,
            value: target);

        _apiClientMock
            .Setup(client => client.ListRecordsAsync(
                It.Is<ListRecordsRequest>(request => request.Domain == domain),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);
        _apiClientMock
            .Setup(client => client.DeleteRecordAsync(
                It.Is<DeleteRecordRequest>(request =>
                    request.Domain == domain && request.RecordId == existing.RecordId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build());

        DnsEndpoint deleteEndpoint = TestData.CreateDnsEndpoint(_fixture, dnsName, DnsRecordType.TXT, target, existing.Ttl);

        await sut.ApplyChangesAsync(DnsChangesBuilder.DeleteOnly(deleteEndpoint), CancellationToken.None);

        _apiClientMock.Verify(
            client => client.DeleteRecordAsync(
                It.Is<DeleteRecordRequest>(request =>
                    request.Domain == domain && request.RecordId == existing.RecordId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyChangesAsync_UpdatesRecord()
    {
        string domain = TestData.CreateDomain(_fixture);
        string oldTarget = _fixture.Create<string>();
        string newTarget = _fixture.Create<string>();
        NamesiloDnsRecord existing = TestData.CreateNamesiloRecord(
            _fixture,
            domain,
            DnsRecordType.A,
            host: domain,
            value: oldTarget);

        _apiClientMock
            .Setup(client => client.ListRecordsAsync(
                It.Is<ListRecordsRequest>(request => request.Domain == domain),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);
        _apiClientMock
            .Setup(client => client.UpdateRecordAsync(
                It.Is<UpdateRecordRequest>(request =>
                    request.Domain == domain
                    && request.RecordId == existing.RecordId
                    && request.RecordHost == NamesiloDns.ApexRecordHost
                    && request.RecordValue == newTarget),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        int defaultTtl = TestData.CreateNameSiloRecordTtl(_fixture);
        NamesiloDnsService sut = CreateSut(
            NamesiloOptionsBuilder.New().WithDomainFilter(domain).WithDefaultTtl(defaultTtl).Build());

        DnsEndpoint updateOld = TestData.CreateARecord(_fixture, domain, oldTarget);
        DnsEndpoint updateNew = TestData.CreateARecord(_fixture, domain, newTarget);

        await sut.ApplyChangesAsync(
            DnsChangesBuilder.Update(updateOld, updateNew),
            CancellationToken.None);

        _apiClientMock.Verify();
    }

    [Fact]
    public async Task ApplyChangesAsync_CreatesSubdomainRecord()
    {
        string domain = TestData.CreateDomain(_fixture);
        int recordTtl = TestData.CreateNameSiloRecordTtl(_fixture);
        DnsEndpoint endpoint = TestData.CreateSubdomainARecord(_fixture, domain, recordTtl: recordTtl);
        string label = DnsNameMapper.ToRecordHost(domain, endpoint.DnsName);

        _apiClientMock
            .Setup(client => client.AddRecordAsync(
                It.Is<AddRecordRequest>(request =>
                    request.Domain == domain
                    && request.RecordType == endpoint.RecordType
                    && request.RecordHost == label
                    && request.RecordValue == endpoint.Targets[0]
                    && request.Ttl == recordTtl),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<Guid>().ToString("N"))
            .Verifiable();

        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build());

        await sut.ApplyChangesAsync(DnsChangesBuilder.CreateOnly(endpoint), CancellationToken.None);

        _apiClientMock.Verify();
    }

    [Fact]
    public async Task ApplyChangesAsync_ThrowsWhenDnsNameOutsideDomainFilter()
    {
        string domain = TestData.CreateDomain(_fixture);
        string otherDomain = TestData.CreateDomain(_fixture);
        DnsEndpoint endpoint = TestData.CreateARecord(_fixture, otherDomain);
        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build());

        await Assert.ThrowsAsync<NamesiloServiceException>(() =>
            sut.ApplyChangesAsync(DnsChangesBuilder.CreateOnly(endpoint), CancellationToken.None));
    }

    [Fact]
    public async Task ApplyChangesAsync_ThrowsWhenDeleteTargetNotFound()
    {
        string domain = TestData.CreateDomain(_fixture);
        DnsEndpoint deleteEndpoint = TestData.CreateARecord(_fixture, domain);

        _apiClientMock
            .Setup(client => client.ListRecordsAsync(
                It.Is<ListRecordsRequest>(request => request.Domain == domain),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build());

        await Assert.ThrowsAsync<NamesiloServiceException>(() =>
            sut.ApplyChangesAsync(DnsChangesBuilder.DeleteOnly(deleteEndpoint), CancellationToken.None));
    }

    [Fact]
    public async Task GetRecordsAsync_ListsEachConfiguredDomain()
    {
        string firstDomain = TestData.CreateDomain(_fixture);
        string secondDomain = TestData.CreateDomain(_fixture);
        NamesiloDnsRecord firstRecord = TestData.CreateNamesiloRecord(_fixture, firstDomain, DnsRecordType.A);
        NamesiloDnsRecord secondRecord = TestData.CreateNamesiloRecord(_fixture, secondDomain, DnsRecordType.A);

        _apiClientMock
            .Setup(client => client.ListRecordsAsync(
                It.Is<ListRecordsRequest>(request => request.Domain == firstDomain),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([firstRecord]);
        _apiClientMock
            .Setup(client => client.ListRecordsAsync(
                It.Is<ListRecordsRequest>(request => request.Domain == secondDomain),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([secondRecord]);

        NamesiloDnsService sut = CreateSut(
            NamesiloOptionsBuilder.New().WithDomainFilter(firstDomain, secondDomain).Build());

        IReadOnlyList<DnsEndpoint> records = await sut.GetRecordsAsync(CancellationToken.None);

        Assert.Equal(2, records.Count);
        _apiClientMock.Verify(
            client => client.ListRecordsAsync(
                It.Is<ListRecordsRequest>(request => request.Domain == firstDomain),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _apiClientMock.Verify(
            client => client.ListRecordsAsync(
                It.Is<ListRecordsRequest>(request => request.Domain == secondDomain),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRecordsAsync_MapsFqdnHostUnderDomain()
    {
        string domain = TestData.CreateDomain(_fixture);
        string nestedHost = TestData.CreateDomainLabel(_fixture) + "." + TestData.CreateDomainLabel(_fixture);
        string fqdnHost = nestedHost + "." + domain;
        string target = domain;
        NamesiloDnsRecord nestedCname = TestData.CreateNamesiloRecord(
            _fixture,
            domain,
            DnsRecordType.CNAME,
            host: fqdnHost,
            value: target);

        _apiClientMock
            .Setup(client => client.ListRecordsAsync(
                It.Is<ListRecordsRequest>(request => request.Domain == domain),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([nestedCname]);

        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build());

        IReadOnlyList<DnsEndpoint> records = await sut.GetRecordsAsync(CancellationToken.None);

        Assert.Single(records);
        Assert.Equal(fqdnHost, records[0].DnsName);
        Assert.Equal(DnsRecordType.CNAME, records[0].RecordType);
        Assert.Equal([target], records[0].Targets);
    }

    [Fact]
    public async Task ApplyChangesAsync_LogsChangeBatchCounts()
    {
        string domain = TestData.CreateDomain(_fixture);
        Mock<ILogger<NamesiloDnsService>> loggerMock = new();
        NamesiloDnsService sut = CreateSut(
            NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build(),
            loggerMock.Object);

        await sut.ApplyChangesAsync(DnsChangesBuilder.Empty(), CancellationToken.None);

        loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("empty change set", StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ApplyChangesAsync_DeletesRecord_WhenApiReturnsFqdnHost()
    {
        string domain = TestData.CreateDomain(_fixture);
        string target = _fixture.Create<string>();
        NamesiloDnsRecord existing = TestData.CreateNamesiloRecord(
            _fixture,
            domain,
            DnsRecordType.A,
            host: domain,
            value: target);

        _apiClientMock
            .Setup(client => client.ListRecordsAsync(
                It.Is<ListRecordsRequest>(request => request.Domain == domain),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);
        _apiClientMock
            .Setup(client => client.DeleteRecordAsync(
                It.Is<DeleteRecordRequest>(request =>
                    request.Domain == domain && request.RecordId == existing.RecordId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build());
        DnsEndpoint deleteEndpoint = TestData.CreateARecord(_fixture, domain, target);

        await sut.ApplyChangesAsync(DnsChangesBuilder.DeleteOnly(deleteEndpoint), CancellationToken.None);

        _apiClientMock.Verify(
            client => client.DeleteRecordAsync(
                It.Is<DeleteRecordRequest>(request =>
                    request.Domain == domain && request.RecordId == existing.RecordId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void AdjustEndpoints_PreservesPositiveTtl()
    {
        int defaultTtl = TestData.CreateNameSiloRecordTtl(_fixture);
        int explicitTtl = TestData.CreateNameSiloRecordTtl(_fixture);
        string domain = TestData.CreateDomain(_fixture);
        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDefaultTtl(defaultTtl).Build());
        DnsEndpoint input = TestData.CreateARecord(_fixture, domain, recordTtl: explicitTtl);

        IReadOnlyList<DnsEndpoint> adjusted = sut.AdjustEndpoints([input]);

        Assert.Equal(explicitTtl, adjusted[0].RecordTtl);
    }

    [Fact]
    public async Task ApplyChangesAsync_NormalizesLowTtlOnCreate()
    {
        string domain = TestData.CreateDomain(_fixture);
        int invalidTtl = TestData.CreateBelowNameSiloRecordTtl(_fixture);
        int expectedTtl = NamesiloRecordTtl.Normalize(invalidTtl);
        DnsEndpoint endpoint = TestData.CreateARecord(_fixture, domain, recordTtl: invalidTtl);
        string recordId = _fixture.Create<Guid>().ToString("N");

        _apiClientMock
            .Setup(client => client.AddRecordAsync(
                It.Is<AddRecordRequest>(request => request.Ttl == expectedTtl),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(recordId)
            .Verifiable();

        NamesiloDnsService sut = CreateSut(NamesiloOptionsBuilder.New().WithDomainFilter(domain).Build());

        await sut.ApplyChangesAsync(DnsChangesBuilder.CreateOnly(endpoint), CancellationToken.None);

        _apiClientMock.Verify();
    }

    private NamesiloDnsService CreateSut(
        NamesiloOptions options,
        ILogger<NamesiloDnsService>? logger = null)
    {
        return new NamesiloDnsService(
            _apiClientMock.Object,
            Options.Create(options),
            logger ?? NullLogger<NamesiloDnsService>.Instance);
    }
}
