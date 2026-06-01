using AutoFixture;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Enums;
using ExternalDnsNamesiloWebhook.Core.Namesilo.Models;
using ExternalDnsNamesiloWebhook.Tests.Fixtures;
using System;
using System.Collections.Generic;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Namesilo;

public class NamesiloApiRequestTests
{
    private readonly Fixture _fixture;

    public NamesiloApiRequestTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void ListRecordsRequest_ToQueryParameters_IncludesDomain()
    {
        string domain = TestData.CreateDomain(_fixture);
        ListRecordsRequest request = new() { Domain = domain };

        IReadOnlyDictionary<string, string> parameters = request.ToQueryParameters();

        Assert.Equal(domain, parameters["domain"]);
    }

    [Fact]
    public void AddRecordRequest_ToQueryParameters_IncludesRecordFields()
    {
        string domain = TestData.CreateDomain(_fixture);
        string target = _fixture.Create<string>();
        int ttl = _fixture.Create<int>() + 1;
        AddRecordRequest request = new()
        {
            Domain = domain,
            RecordType = DnsRecordType.TXT,
            RecordHost = NamesiloDns.ApexRecordHost,
            RecordValue = target,
            Ttl = ttl,
        };

        IReadOnlyDictionary<string, string> parameters = request.ToQueryParameters();

        Assert.Equal(domain, parameters["domain"]);
        Assert.Equal("TXT", parameters["rrtype"]);
        Assert.Equal(NamesiloDns.ApexRecordHost, parameters["rrhost"]);
        Assert.Equal(target, parameters["rrvalue"]);
        Assert.Equal(ttl.ToString(), parameters["rrttl"]);
    }

    [Fact]
    public void UpdateRecordRequest_ToQueryParameters_IncludesRecordFields()
    {
        string domain = TestData.CreateDomain(_fixture);
        string recordId = _fixture.Create<Guid>().ToString("N");
        string target = _fixture.Create<string>();
        int ttl = _fixture.Create<int>() + 1;
        UpdateRecordRequest request = new()
        {
            Domain = domain,
            RecordId = recordId,
            RecordHost = NamesiloDns.ApexRecordHost,
            RecordValue = target,
            Ttl = ttl,
        };

        IReadOnlyDictionary<string, string> parameters = request.ToQueryParameters();

        Assert.Equal(domain, parameters["domain"]);
        Assert.Equal(recordId, parameters["rrid"]);
        Assert.Equal(NamesiloDns.ApexRecordHost, parameters["rrhost"]);
        Assert.Equal(target, parameters["rrvalue"]);
        Assert.Equal(ttl.ToString(), parameters["rrttl"]);
    }

    [Fact]
    public void DeleteRecordRequest_ToQueryParameters_IncludesDomainAndRecordId()
    {
        string domain = TestData.CreateDomain(_fixture);
        string recordId = _fixture.Create<Guid>().ToString("N");
        DeleteRecordRequest request = new() { Domain = domain, RecordId = recordId };

        IReadOnlyDictionary<string, string> parameters = request.ToQueryParameters();

        Assert.Equal(domain, parameters["domain"]);
        Assert.Equal(recordId, parameters["rrid"]);
    }
}
