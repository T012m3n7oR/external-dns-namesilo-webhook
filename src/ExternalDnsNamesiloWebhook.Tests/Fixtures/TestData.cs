using AutoFixture;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Enums;
using ExternalDnsNamesiloWebhook.Core.Namesilo;
using ExternalDnsNamesiloWebhook.Core.Namesilo.Models;
using ExternalDnsNamesiloWebhook.Core.Webhook.Models;
using ExternalDnsNamesiloWebhook.Tests.Constants;
using System;

namespace ExternalDnsNamesiloWebhook.Tests.Fixtures;

internal static class TestData
{
    public static string CreateDomainLabel(Fixture fixture)
    {
        return fixture.Create<Guid>().ToString("N")[..12];
    }

    public static string CreateDomain(Fixture fixture)
    {
        return CreateDomainLabel(fixture) + TestConstants.DomainSuffix;
    }

    public static string CreateSubdomain(Fixture fixture, string domain)
    {
        return CreateDomainLabel(fixture) + "." + domain;
    }

    public static DnsEndpoint CreateARecord(Fixture fixture, string domain, string? target = null, long recordTtl = 0)
    {
        return CreateDnsEndpoint(fixture, domain, DnsRecordType.A, target, recordTtl);
    }

    public static DnsEndpoint CreateSubdomainARecord(
        Fixture fixture,
        string domain,
        string? target = null,
        long recordTtl = 0)
    {
        string dnsName = CreateSubdomain(fixture, domain);
        return CreateDnsEndpoint(fixture, dnsName, DnsRecordType.A, target, recordTtl);
    }

    public static DnsEndpoint CreateDnsEndpoint(
        Fixture fixture,
        string dnsName,
        DnsRecordType recordType,
        string? target = null,
        long recordTtl = 0)
    {
        return fixture.Build<DnsEndpoint>()
            .With(endpoint => endpoint.DnsName, dnsName)
            .With(endpoint => endpoint.RecordType, recordType)
            .With(endpoint => endpoint.Targets, (IFixture f) => [target ?? f.Create<string>()])
            .With(endpoint => endpoint.RecordTtl, recordTtl)
            .Create();
    }

    public static NamesiloDnsRecord CreateNamesiloRecord(
        Fixture fixture,
        string domain,
        DnsRecordType recordType,
        string? host = null,
        string? value = null,
        string? recordId = null)
    {
        return fixture.Build<NamesiloDnsRecord>()
            .With(record => record.RecordId, (IFixture f) => recordId ?? f.Create<Guid>().ToString("N"))
            .With(record => record.RecordType, recordType)
            .With(record => record.Host, host ?? domain)
            .With(record => record.Value, (IFixture f) => value ?? f.Create<string>())
            .With(record => record.Ttl, (IFixture f) => f.Create<int>() + 1)
            .Create();
    }

    public static string BuildSuccessReplyJson(int code, string detail = "success")
    {
        return NamesiloApiJson.SerializeReply(code, detail);
    }

    public static string BuildAddRecordSuccessJson(string recordId)
    {
        return NamesiloApiJson.SerializeResponse(new NamesiloApiResponse
        {
            Reply = new NamesiloApiReply
            {
                Code = NamesiloDnsConstants.SuccessReplyCode,
                Detail = NamesiloApiDefaults.SuccessDetail,
                RecordId = recordId,
            },
        });
    }

    public static string BuildListRecordsJson(params NamesiloDnsRecord[] records)
    {
        return NamesiloApiJson.SerializeListRecordsReply(records);
    }

    public static string BuildSingleObjectListRecordsJson(NamesiloDnsRecord record)
    {
        return NamesiloApiJson.SerializeSingleObjectListRecordsReply(record);
    }
}
