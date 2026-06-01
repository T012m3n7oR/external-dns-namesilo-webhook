using AutoFixture;
using ExternalDnsNamesiloWebhook.Core.Enums;
using ExternalDnsNamesiloWebhook.Core.Namesilo;
using ExternalDnsNamesiloWebhook.Core.Namesilo.Models;
using ExternalDnsNamesiloWebhook.Tests.Fixtures;
using System;
using System.Text.Json;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Namesilo;

public class NamesiloApiJsonTests
{
    private readonly Fixture _fixture;

    public NamesiloApiJsonTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void DeserializeResponse_MapsTypePropertyToRecordType()
    {
        string domain = TestData.CreateDomain(_fixture);
        string recordId = _fixture.Create<Guid>().ToString("N");
        string target = _fixture.Create<string>();
        int ttl = _fixture.Create<int>() + 1;
        string body = $$"""
            {
              "reply": {
                "code": 300,
                "detail": "success",
                "resource_record": [
                  {
                    "record_id": "{{recordId}}",
                    "type": "A",
                    "host": "{{domain}}",
                    "value": "{{target}}",
                    "ttl": {{ttl}}
                  }
                ]
              }
            }
            """;

        NamesiloApiResponse response = NamesiloApiJson.DeserializeResponse(body);

        Assert.NotNull(response.Reply?.ResourceRecords);
        NamesiloDnsRecord record = Assert.Single(response.Reply.ResourceRecords);
        Assert.Equal(recordId, record.RecordId);
        Assert.Equal(DnsRecordType.A, record.RecordType);
        Assert.Equal(domain, record.Host);
        Assert.Equal(target, record.Value);
        Assert.Equal(ttl, record.Ttl);
    }

    [Fact]
    public void SerializeListRecordsReply_WritesTypePropertyName()
    {
        string domain = TestData.CreateDomain(_fixture);
        NamesiloDnsRecord record = TestData.CreateNamesiloRecord(_fixture, domain, DnsRecordType.CNAME);

        string json = NamesiloApiJson.SerializeListRecordsReply(record);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement resourceRecord = document.RootElement
            .GetProperty("reply")
            .GetProperty("resource_record")[0];
        Assert.True(resourceRecord.TryGetProperty("type", out JsonElement typeProperty));
        Assert.Equal("CNAME", typeProperty.GetString());
        Assert.False(resourceRecord.TryGetProperty("record_type", out _));
    }
}
