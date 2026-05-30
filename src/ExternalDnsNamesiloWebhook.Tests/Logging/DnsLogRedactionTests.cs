using ExternalDnsNamesiloWebhook.Core.Enums;
using ExternalDnsNamesiloWebhook.Core.Logging;
using Xunit;

namespace ExternalDnsNamesiloWebhook.Tests.Logging;

public class DnsLogRedactionTests
{
    [Theory]
    [InlineData(DnsRecordType.TXT, "secret-token", "[redacted]")]
    [InlineData(DnsRecordType.A, "203.0.113.1", "203.0.113.1")]
    public void FormatRecordTarget_ReturnsExpected(DnsRecordType recordType, string target, string expected)
    {
        Assert.Equal(expected, DnsLogRedaction.FormatRecordTarget(recordType, target));
    }
}
