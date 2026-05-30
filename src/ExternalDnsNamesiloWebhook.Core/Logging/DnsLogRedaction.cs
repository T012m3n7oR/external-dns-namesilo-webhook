using ExternalDnsNamesiloWebhook.Core.Enums;

namespace ExternalDnsNamesiloWebhook.Core.Logging;

public static class DnsLogRedaction
{
    public static string FormatRecordTarget(DnsRecordType recordType, string target)
    {
        if (recordType == DnsRecordType.TXT)
        {
            return "[redacted]";
        }

        return target;
    }
}
