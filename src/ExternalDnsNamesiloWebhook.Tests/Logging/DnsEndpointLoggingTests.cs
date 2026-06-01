using ExternalDnsNamesiloWebhook.Core.Enums;
using ExternalDnsNamesiloWebhook.Core.Logging;
using ExternalDnsNamesiloWebhook.Core.Webhook.Models;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Logging;

public sealed class DnsEndpointLoggingTests
{
    [Theory]
    [InlineData(DnsRecordType.A, "tormentz.com", true)]
    [InlineData(DnsRecordType.AAAA, "tormentz.com", true)]
    [InlineData(DnsRecordType.A, "www.tormentz.com", true)]
    [InlineData(DnsRecordType.CNAME, "www.tormentz.com", false)]
    [InlineData(DnsRecordType.TXT, "external-dns-a-tormentz.com", true)]
    [InlineData(DnsRecordType.TXT, "spf.tormentz.com", false)]
    [InlineData(DnsRecordType.A, "other.com", false)]
    public void IsNotableForSync_ClassifiesRecords(
        DnsRecordType recordType,
        string dnsName,
        bool expectedNotable)
    {
        DnsEndpoint endpoint = new()
        {
            DnsName = dnsName,
            RecordType = recordType,
            Targets = ["value"],
        };

        bool notable = DnsEndpointLogging.IsNotableForSync(endpoint, ["tormentz.com"]);

        Assert.Equal(expectedNotable, notable);
    }
}
